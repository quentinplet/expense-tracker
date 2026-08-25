using System;

namespace API.DTOs.Responses;

public record DashboardResponseDto(
    string Month,
    MonthTotalsDto Totals,
    IReadOnlyList<CategoryBreakdownDto> Breakdown,
    IReadOnlyList<MonthlyPointDto> Trend,
    IReadOnlyList<TransactionResponseDto> RecentTransactions);

/// L'API renvoie les valeurs brutes du mois précédent, pas un écart calculé :
/// c'est au client de décider comment présenter la variation, notamment quand
/// la valeur précédente vaut zéro et qu'un pourcentage n'a aucun sens.
public record MonthTotalsDto(
    decimal Expenses,
    decimal Income,
    decimal Net,
    decimal PreviousExpenses,
    decimal PreviousIncome,
    decimal PreviousNet);

public record CategoryBreakdownDto(
    Guid CategoryId,
    string Name,
    string? TranslationKey,
    string? Color,
    string? Icon,
    decimal Total,
    /// Part du total des dépenses du mois, entre 0 et 1.
    decimal Share);

public record MonthlyPointDto(string Month, decimal Expenses, decimal Income);