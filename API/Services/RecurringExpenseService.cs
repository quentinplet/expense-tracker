using API.Entities;
using API.Interfaces;

namespace API.Services;

public class RecurringExpenseService(IUnitOfWork uow) : IRecurringExpenseService
{
    /// Prémunit contre une boucle anormalement longue sur une donnée corrompue ou
    /// une charge orpheline depuis des années — un cas qui ne devrait jamais
    /// arriver en usage normal, mais qui ne doit pas bloquer le job pour tous les
    /// autres utilisateurs s'il arrive. Une charge qui dépasse ce plafond dans une
    /// même exécution reprend son rattrapage au prochain passage du job.
    private const int MaxCatchUpIterationsPerExpense = 24;

    public async Task<int> GenerateDueTransactionsAsync()
    {
        // Le job tourne hors contexte HTTP : le jour courant se lit en UTC, comme
        // BudgetService.RenewBudgetsAsync.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var due = await uow.RecurringExpenseRepository.GetDueAsync(today);

        var created = 0;
        foreach (var expense in due)
        {
            var dueDates = new List<DateOnly>();
            var cursor = expense.NextDueDate;
            for (var i = 0; i < MaxCatchUpIterationsPerExpense && cursor <= today; i++)
            {
                dueDates.Add(cursor);
                cursor = Advance(cursor, expense.Frequency);
            }

            if (dueDates.Count == 0) continue;

            // Une seule requête pour toute la fenêtre de rattrapage de cette charge,
            // pas un aller-retour par échéance — l'idempotence ne repose jamais sur
            // l'index unique pour absorber un doublon.
            var alreadyGenerated = await uow.TransactionRepository.GetGeneratedDatesAsync(
                expense.Id, dueDates[0], dueDates[^1]);

            foreach (var dueDate in dueDates)
            {
                if (alreadyGenerated.Contains(dueDate)) continue;

                uow.TransactionRepository.AddTransaction(new Transaction
                {
                    Amount = expense.Amount,
                    Type = expense.Type,
                    Date = dueDate,
                    Label = expense.Label,
                    CategoryId = expense.CategoryId,
                    UserId = expense.UserId,
                    RecurringExpenseId = expense.Id,
                });
                created++;
            }

            expense.NextDueDate = cursor;
            uow.RecurringExpenseRepository.Update(expense);
        }

        if (due.Count > 0) await uow.Complete();
        return created;
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
