using System;
using API.Entities;

namespace API.Interfaces;

/// Projections internes à la couche applicative : une agrégation n'a pas d'entité
/// à renvoyer, mais le repository ne connaît pas les DTOs pour autant. Le mapping
/// vers le contrat public reste au service.
public record MonthlyTotalProjection(int Year, int Month, TransactionType Type, decimal Total);

public record CategoryTotalProjection(
    Guid CategoryId,
    string Name,
    string? TranslationKey,
    string? Color,
    string? Icon,
    decimal Total);

public interface IDashboardRepository
{
    /// Une seule agrégation sur la fenêtre glissante : elle produit la courbe 12 mois,
    /// les totaux du mois affiché et ceux du mois précédent.
    Task<IReadOnlyList<MonthlyTotalProjection>> GetMonthlyTotalsAsync(
        Guid userId, DateOnly from, DateOnly toExclusive);

    Task<IReadOnlyList<CategoryTotalProjection>> GetExpenseBreakdownAsync(
        Guid userId, DateOnly from, DateOnly toExclusive);

    Task<IReadOnlyList<Transaction>> GetRecentAsync(
        Guid userId, DateOnly from, DateOnly toExclusive, int take);
}