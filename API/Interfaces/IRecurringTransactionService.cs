namespace API.Interfaces;

public interface IRecurringTransactionService
{
    /// Génère toutes les transactions dues, en rattrapant chaque échéance en
    /// retard par transaction récurrente (pas seulement la plus récente), et
    /// avance NextDueDate en conséquence. Appelé par
    /// RecurringTransactionGenerationJob ; retourne le nombre de transactions
    /// créées.
    Task<int> GenerateDueTransactionsAsync();
}
