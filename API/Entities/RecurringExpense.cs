namespace API.Entities;

public enum Frequency
{
    Daily,
    Weekly,
    Monthly,
    Yearly
}

public class RecurringExpense
{
    public Guid Id { get; set; }
    public required string Label { get; set; }
    public required decimal Amount { get; set; }
    public required TransactionType Type { get; set; }
    public Frequency Frequency { get; set; }

    /// Prochaine échéance à générer. Avancée par RecurringExpenseGenerationJob
    /// après chaque transaction générée (§ Génération automatique).
    public DateOnly NextDueDate { get; set; }

    /// Met en pause sans perdre la configuration (montant, catégorie,
    /// fréquence) — le job ignore toute charge Active == false.
    public bool Active { get; set; } = true;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    /// Transactions générées par cette charge. Détachées (RecurringExpenseId à
    /// null), jamais supprimées, si la charge est supprimée.
    public ICollection<Transaction> Transactions { get; set; } = [];
}
