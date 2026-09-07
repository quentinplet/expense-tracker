using System;

namespace API.Entities;

public enum NotificationType
{
    RecurringTransactionGenerated,
    BudgetThresholdReached
}

public class Notification
{
    public Guid Id { get; set; }
    public required NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// Renseigné si Type == RecurringTransactionGenerated. Cascade delete : une
    /// notification sur une transaction qui n'existe plus n'a pas de sens à
    /// afficher (contrairement à Transaction.RecurringTransactionId, qui survit
    /// à la suppression de son modèle).
    public Guid? TransactionId { get; set; }
    public Transaction? Transaction { get; set; }

    /// Renseignés si Type == BudgetThresholdReached. Cascade delete, même
    /// raisonnement que TransactionId ci-dessus.
    public Guid? BudgetId { get; set; }
    public Budget? Budget { get; set; }
    public int? ThresholdPercent { get; set; }

    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
}
