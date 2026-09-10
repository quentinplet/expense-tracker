namespace API.Entities;

public enum ImportStatus
{
    Pending,
    Committed,
    Failed
}

public class ImportBatch
{
    public Guid Id { get; set; }
    public required string FileName { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    public int RowCount { get; set; }

    /// En v1 un batch n'est créé qu'au succès de la confirmation, donc toujours
    /// Committed — Pending/Failed restent dans le schéma pour un futur traitement
    /// asynchrone (gros fichier, job en arrière-plan), non atteignables par ce flux
    /// synchrone.
    public ImportStatus Status { get; set; } = ImportStatus.Committed;

    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    /// Transactions insérées par cet import. Supprimées en cascade si le batch est
    /// annulé — contrairement à RecurringTransaction.Transactions (SetNull), une
    /// transaction importée n'a plus de sens à conserver sans son batch.
    public ICollection<Transaction> Transactions { get; set; } = [];
}
