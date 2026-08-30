using System;

namespace API.Entities;

public class Category
{
    public Guid Id { get; set; }

    /// Libellé. Pour une catégorie non renommée depuis le seed, sert de valeur
    /// de secours ; l'affichage passe par TranslationKey.
    public required string Name { get; set; }

    /// Non null tant que la catégorie n'a pas été renommée, ex. "category.system.housing".
    public string? TranslationKey { get; set; }

    public string Icon { get; set; } = null!;   // PrimeIcons, ex. "pi-bolt"
    public string Color { get; set; } = null!;  // hex

    /// Évite de proposer « Salaire » dans le sélecteur d'une dépense.
    public TransactionType Type { get; set; } = TransactionType.Expense;

    /// True uniquement pour les deux lignes "Other" (une par Type) posées au
    /// seed de chaque utilisateur. Bloque uniquement la suppression, jamais
    /// le renommage/la recoloration.
    public bool IsLocked { get; set; }

    /// Absent de la spec, conservé : l'endpoint de bascule existe déjà et le retirer
    /// serait une régression fonctionnelle, pas un alignement de modèle.
    public bool Enabled { get; set; } = true;

    /// Toujours renseigné — une catégorie appartient à un seul utilisateur,
    /// jamais partagée entre comptes.
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public ICollection<Transaction> Transactions { get; set; } = [];
    public ICollection<Budget> Budgets { get; set; } = [];
    public ICollection<RecurringExpense> RecurringExpenses { get; set; } = [];
}