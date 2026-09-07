using API.Entities;
using API.Interfaces;

namespace API.Services;

public class RecurringTransactionService(
    IUnitOfWork uow, INotificationService notificationService, ILogger<RecurringTransactionService> logger)
    : IRecurringTransactionService
{
    /// Prémunit contre une boucle anormalement longue sur une donnée corrompue ou
    /// une transaction récurrente orpheline depuis des années — un cas qui ne
    /// devrait jamais arriver en usage normal, mais qui ne doit pas bloquer le job
    /// pour tous les autres utilisateurs s'il arrive. Une transaction récurrente
    /// qui dépasse ce plafond dans une même exécution reprend son rattrapage au
    /// prochain passage du job.
    private const int MaxCatchUpIterationsPerRecurringTransaction = 24;

    public async Task<int> GenerateDueTransactionsAsync()
    {
        // Le job tourne hors contexte HTTP : le jour courant se lit en UTC, comme
        // BudgetService.RenewBudgetsAsync.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var due = await uow.RecurringTransactionRepository.GetDueAsync(today);

        var created = 0;
        foreach (var recurringTransaction in due)
        {
            var dueDates = new List<DateOnly>();
            var cursor = recurringTransaction.NextDueDate;
            for (var i = 0; i < MaxCatchUpIterationsPerRecurringTransaction && cursor <= today; i++)
            {
                dueDates.Add(cursor);
                cursor = Advance(cursor, recurringTransaction.Frequency);
            }

            if (dueDates.Count == 0) continue;

            // Une seule requête pour toute la fenêtre de rattrapage de cette
            // transaction récurrente, pas un aller-retour par échéance —
            // l'idempotence ne repose jamais sur l'index unique pour absorber un
            // doublon.
            var alreadyGenerated = await uow.TransactionRepository.GetGeneratedDatesAsync(
                recurringTransaction.Id, dueDates[0], dueDates[^1]);

            foreach (var dueDate in dueDates)
            {
                if (alreadyGenerated.Contains(dueDate)) continue;

                var transaction = new Transaction
                {
                    Amount = recurringTransaction.Amount,
                    Type = recurringTransaction.Type,
                    Date = dueDate,
                    Label = recurringTransaction.Label,
                    CategoryId = recurringTransaction.CategoryId,
                    UserId = recurringTransaction.UserId,
                    RecurringTransactionId = recurringTransaction.Id,
                };
                uow.TransactionRepository.AddTransaction(transaction);

                // Sauvegardée une par une (pas en lot à la fin) : la vérification
                // de seuil ci-dessous recalcule Spent sur les transactions déjà en
                // base, donc doit voir celle-ci persistée avant de passer à la
                // suivante — sous peine de manquer un seuil franchi puis dépassé
                // entre deux échéances rattrapées dans la même exécution.
                await uow.Complete();
                created++;

                await NotifyGeneratedAsync(transaction, dueDate);
            }

            recurringTransaction.NextDueDate = cursor;
            uow.RecurringTransactionRepository.Update(recurringTransaction);
        }

        if (due.Count > 0) await uow.Complete();
        return created;
    }

    /// Ne doit jamais faire échouer la génération : toute exception est
    /// journalisée et avalée ici, jamais propagée (même philosophie défensive
    /// que le catch du BackgroundService lui-même).
    private async Task NotifyGeneratedAsync(Transaction transaction, DateOnly dueDate)
    {
        try
        {
            await notificationService.NotifyRecurringTransactionGeneratedAsync(transaction.UserId, transaction.Id);

            // Seules les dépenses ont un budget à surveiller — une transaction
            // récurrente Income générée notifie bien ci-dessus, mais ne déclenche
            // jamais de vérification de seuil.
            if (transaction.Type == TransactionType.Expense)
            {
                var month = dueDate.ToString("yyyy-MM");
                await notificationService.CheckBudgetThresholdsAsync(transaction.UserId, transaction.CategoryId, month);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Notification failed for generated transaction {TransactionId}.", transaction.Id);
        }
    }

    /// DateOnly.AddMonths/AddYears gèrent déjà le débordement de fin de mois et le
    /// 29 février (31 janvier + 1 mois → 28 ou 29 février, pas 3 mars).
    private static DateOnly Advance(DateOnly date, Frequency frequency) => frequency switch
    {
        Frequency.Daily => date.AddDays(1),
        Frequency.Weekly => date.AddDays(7),
        Frequency.Monthly => date.AddMonths(1),
        Frequency.Yearly => date.AddYears(1),
        _ => throw new ArgumentOutOfRangeException(nameof(frequency)),
    };
}
