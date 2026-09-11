using System;

namespace API.DTOs.Responses;

public record DashboardResponseDto(
    /// `YYYY-MM` du mois affiché.
    string Month,
    MonthTotalsDto Totals,
    /// Σ revenus − Σ dépenses sur tout l'historique enregistré, indépendamment du mois
    /// affiché. Ce n'est PAS un solde : l'application ignore ce que l'utilisateur
    /// possède, elle ne connaît que les flux qu'il a saisis.
    decimal CumulativeNet,
    /// Répartition des dépenses du mois affiché.
    IReadOnlyList<CategoryBreakdownDto> Breakdown,
    /// La même, sur les douze derniers mois glissants (ancrés sur le mois courant
    /// réel, indépendant du mois affiché). Ses parts sont calculées sur `YearExpenses`.
    IReadOnlyList<CategoryBreakdownDto> BreakdownYear,
    decimal YearExpenses,
    /// Un point par jour du mois affiché, zéros compris.
    IReadOnlyList<DailyPointDto> DailyTrend,
    /// Un point par mois, du premier mois enregistré jusqu'au mois affiché.
    IReadOnlyList<MonthlyPointDto> Trend,
    IReadOnlyList<TransactionResponseDto> RecentTransactions);

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
    /// Part du total des dépenses de la portée, entre 0 et 1.
    decimal Share);

public record MonthlyPointDto(string Month, decimal Expenses, decimal Income);

/// `Date` au format `YYYY-MM-DD`.
public record DailyPointDto(string Date, decimal Expenses, decimal Income);