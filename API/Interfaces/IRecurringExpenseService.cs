namespace API.Interfaces;

public interface IRecurringExpenseService
{
    /// Génère toutes les transactions dues, en rattrapant chaque échéance en
    /// retard par charge (pas seulement la plus récente), et avance NextDueDate
    /// en conséquence. Appelé par RecurringExpenseGenerationJob ; retourne le
    /// nombre de transactions créées.
    Task<int> GenerateDueTransactionsAsync();
}
