using API.Entities;

namespace API.Interfaces;

public interface INotificationRepository
{
    Task<int> GetUnreadCountAsync(Guid userId);

    /// Les `take` plus récentes, lues et non lues confondues, triées par date
    /// décroissante — Transaction et Budget.Category inclus pour le mapping DTO.
    Task<List<Notification>> GetRecentAsync(Guid userId, int take);

    Task<Notification?> GetByIdAsync(Guid id);

    Task<List<Notification>> GetUnreadAsync(Guid userId);

    /// Idempotence de NotificationService.CheckBudgetThresholdsAsync : la table
    /// Notifications elle-même sert d'état interrogé avant d'insérer, pas une
    /// table de suivi séparée.
    Task<bool> ThresholdNotificationExistsAsync(Guid budgetId, int thresholdPercent);

    void Add(Notification notification);
    void Update(Notification notification);
    void Delete(Notification notification);
}
