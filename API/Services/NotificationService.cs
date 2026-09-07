using API.DTOs.Responses;
using API.Entities;
using API.Extensions;
using API.Interfaces;

namespace API.Services;

public class NotificationService(IUnitOfWork uow, IBudgetService budgetService) : INotificationService
{
    /// 90 puis 100 — deux notifications indépendantes, pas un état à deux valeurs :
    /// un saut direct de 80 % à 105 % crée les deux dans la même exécution.
    private static readonly int[] Thresholds = [90, 100];

    public async Task<NotificationsResponseDto> GetForUserAsync(Guid userId, int take)
    {
        var unreadCount = await uow.NotificationRepository.GetUnreadCountAsync(userId);
        var items = await uow.NotificationRepository.GetRecentAsync(userId, take);
        return new NotificationsResponseDto(unreadCount, [.. items.Select(n => n.ToNotificationDto())]);
    }

    public async Task NotifyRecurringTransactionGeneratedAsync(Guid userId, Guid transactionId)
    {
        uow.NotificationRepository.Add(new Notification
        {
            Type = NotificationType.RecurringTransactionGenerated,
            TransactionId = transactionId,
            UserId = userId,
        });
        await uow.Complete();
    }

    public async Task CheckBudgetThresholdsAsync(Guid userId, Guid categoryId, string month)
    {
        // Le budget catégorie concerné et le budget global du même mois, s'ils
        // existent — deux lectures au plus, jamais une par transaction du mois.
        var budgetsForMonth = await uow.BudgetRepository.GetAllByUserIdAndMonthAsync(userId, month);
        var categoryBudget = budgetsForMonth.FirstOrDefault(b => b.CategoryId == categoryId);
        var globalBudget = budgetsForMonth.FirstOrDefault(b => b.CategoryId == null);

        var created = false;
        foreach (var budget in new[] { categoryBudget, globalBudget })
        {
            if (budget == null) continue;

            // Spent recalculé à la volée via BudgetService (même GroupBy que
            // GET /api/budgets), jamais stocké ni passé en paramètre pour
            // incrémenter un total en mémoire.
            var spent = await budgetService.GetSpentAsync(userId, budget);

            // Un plafond à 0 € n'a rien à diviser : toute dépense est un
            // franchissement immédiat des deux seuils (même règle que
            // BudgetCard.percentage() côté frontend).
            var percent = budget.AmountLimit <= 0
                ? (spent > 0 ? 100m : 0m)
                : spent / budget.AmountLimit * 100m;

            foreach (var threshold in Thresholds)
            {
                if (percent < threshold) continue;
                if (await uow.NotificationRepository.ThresholdNotificationExistsAsync(budget.Id, threshold)) continue;

                uow.NotificationRepository.Add(new Notification
                {
                    Type = NotificationType.BudgetThresholdReached,
                    BudgetId = budget.Id,
                    ThresholdPercent = threshold,
                    UserId = userId,
                });
                created = true;
            }
        }

        if (created) await uow.Complete();
    }
}
