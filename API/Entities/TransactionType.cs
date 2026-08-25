namespace API.Entities;

/// Le sens d'un flux. Porté par la transaction elle-même (§3.C) et par la
/// catégorie, qui filtre les catégories proposées à la saisie.
public enum TransactionType
{
    Expense,
    Income
}