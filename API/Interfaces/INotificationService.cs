using API.DTOs.Responses;

namespace API.Interfaces;

public interface INotificationService
{
    /// Compteur non lues + les `take` plus récentes — un seul appel, comme
    /// GetBudgetsForMonthAsync.
    Task<NotificationsResponseDto> GetForUserAsync(Guid userId, int take);

    /// Appelée par RecurringTransactionService juste après chaque transaction
    /// générée.
    Task NotifyRecurringTransactionGeneratedAsync(Guid userId, Guid transactionId);

    /// Seul point d'entrée pour vérifier les seuils de budget, appelé après toute
    /// écriture d'une transaction Expense (saisie manuelle, génération
    /// automatique). N'échoue jamais l'appelant : toute exception doit être
    /// capturée et journalisée par qui appelle cette méthode, jamais propagée.
    Task CheckBudgetThresholdsAsync(Guid userId, Guid categoryId, string month);
}
