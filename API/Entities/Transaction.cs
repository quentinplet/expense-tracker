using System;

namespace API.Entities;

public class Transaction
{
    public Guid Id { get; set; }

    /// TOUJOURS positif. Le sens est porté par Type (§3.C, règle 15).
    public required decimal Amount { get; set; }
    public required TransactionType Type { get; set; }

    /// Date de l'opération, sans heure.
    public required DateOnly Date { get; set; }

    /// Libellé court, obligatoire.
    public string Label { get; set; } = null!;

    /// Texte libre, optionnel.
    public string? Note { get; set; }

    //navigation properties for category
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    //navigation properties for user
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    /// Renseigné si cette transaction a été générée par une transaction récurrente.
    /// Détaché (mis à null) si la charge est supprimée — la transaction reste
    /// un fait financier indépendant une fois créée.
    public Guid? RecurringTransactionId { get; set; }
    public RecurringTransaction? RecurringTransaction { get; set; }

    /// Renseigné si cette transaction vient d'un import CSV. Supprimée en cascade
    /// si le batch est annulé (contrairement à RecurringTransactionId ci-dessus).
    public Guid? ImportBatchId { get; set; }
    public ImportBatch? ImportBatch { get; set; }

    /// SHA-256 de (date, montant, libellé brut) — import CSV seulement. Sert au
    /// dédoublonnage ; unique par utilisateur (pas d'entité Account dans ce projet,
    /// donc pas de portée par compte comme documenté à l'origine).
    public string? DedupHash { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}