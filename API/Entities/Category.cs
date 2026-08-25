using System;

namespace API.Entities;

public class Category
{
    public Guid Id { get; set; }

    /// Libellé. Pour une catégorie système, sert de valeur de secours ;
    /// l'affichage passe par TranslationKey.
    public required string Name { get; set; }

    /// Non null UNIQUEMENT pour les catégories système, ex. "category.system.housing".
    public string? TranslationKey { get; set; }

    public string Icon { get; set; } = null!;   // PrimeIcons, ex. "pi-bolt"
    public string Color { get; set; } = null!;  // hex

    /// Évite de proposer « Salaire » dans le sélecteur d'une dépense.
    public TransactionType Type { get; set; } = TransactionType.Expense;

    public bool IsSystem { get; set; }

    /// Absent de la spec, conservé : l'endpoint de bascule existe déjà et le retirer
    /// serait une régression fonctionnelle, pas un alignement de modèle.
    public bool Enabled { get; set; } = true;

    /// Null pour les catégories système.
    public Guid? UserId { get; set; }
    public AppUser? User { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = [];
    public ICollection<Budget> Budgets { get; set; } = [];
}