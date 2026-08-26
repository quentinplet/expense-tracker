using System;
using API.Entities;

namespace API.Interfaces;

/// Projections internes à la couche applicative : une agrégation n'a pas d'entité
/// à renvoyer, mais le repository ne connaît pas les DTOs pour autant. Le mapping
/// vers le contrat public reste au service.
public record MonthlyTotalProjection(int Year, int Month, TransactionType Type, decimal Total);

public record DailyTotalProjection(DateOnly Date, TransactionType Type, decimal Total);

public record CategoryTotalProjection(
    Guid CategoryId,
    string Name,
    string? TranslationKey,
    string? Color,
    string? Icon,
    decimal Total);

public record OverallTotalsProjection(decimal Income, decimal Expenses);

public interface IDashboardRepository
{
    /// Une seule agrégation, sans borne : elle produit la courbe mensuelle sur tout
    /// l'historique ainsi que les totaux du mois affiché et ceux du mois précédent.
    Task<IReadOnlyList<MonthlyTotalProjection>> GetMonthlyTotalsAsync(Guid userId);

    /// Alimente la courbe à l'échelle du jour, sur le mois affiché.
    Task<IReadOnlyList<DailyTotalProjection>> GetDailyTotalsAsync(
        Guid userId, DateOnly from, DateOnly toExclusive);

    /// Alimente le cumul et le total de référence de la répartition « tout l'historique ».
    Task<OverallTotalsProjection> GetOverallTotalsAsync(Guid userId);

    /// Bornes nulles pour couvrir tout l'historique.
    Task<IReadOnlyList<CategoryTotalProjection>> GetExpenseBreakdownAsync(
        Guid userId, DateOnly? from, DateOnly? toExclusive);

    /// Volontairement décorrélé de la période affichée : « récentes » veut dire récentes,
    /// pas « récentes à l'intérieur du mois sélectionné ».
    Task<IReadOnlyList<Transaction>> GetRecentAsync(Guid userId, int take);
}