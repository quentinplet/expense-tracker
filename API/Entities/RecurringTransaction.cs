namespace API.Entities;

public enum Frequency
{
    Daily,
    Weekly,
    Monthly,
    Yearly
}

public class RecurringTransaction
{
    public Guid Id { get; set; }
    public required string Label { get; set; }
    public required decimal Amount { get; set; }
    public required TransactionType Type { get; set; }
    public Frequency Frequency { get; set; }

    /// Prochaine échéance à générer. Avancée par RecurringTransactionGenerationJob
    /// après chaque transaction générée (§ Génération automatique).
    public DateOnly NextDueDate { get; set; }

    /// Met en pause sans perdre la configuration (montant, catégorie,
    /// fréquence) — le job ignore toute transaction récurrente Active == false.
    public bool Active { get; set; } = true;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    /// Transactions générées par ce modèle. Détachées (RecurringTransactionId à
    /// null), jamais supprimées, si ce modèle est supprimé.
    public ICollection<Transaction> Transactions { get; set; } = [];
}
