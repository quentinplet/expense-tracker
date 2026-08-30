using System;

namespace API.Entities;

public class Budget
{
    public Guid Id { get; set; }
    public required decimal AmountLimit { get; set; }

    /// "YYYY-MM", même format que le mois du dashboard.
    public required string Month { get; set; }

    /// Duplique ce budget sur le mois suivant (même plafond, sans report) tant
    /// que le mois suivant n'a pas déjà sa propre ligne.
    public bool AutoRenew { get; set; }

    /// Null = budget global (toutes catégories de dépense confondues).
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }

    //navigation properties for user
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

}