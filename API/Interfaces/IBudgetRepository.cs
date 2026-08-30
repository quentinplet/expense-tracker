using System;
using API.Entities;

namespace API.Interfaces;

/// Projection interne : le dépensé par catégorie sur une plage de dates, toutes
/// catégories de dépense confondues. Le repository ne connaît pas les DTOs, ce
/// mapping-là reste au service (voir IDashboardRepository pour le même principe).
public record CategorySpentProjection(Guid CategoryId, decimal Total);

public interface IBudgetRepository
{
    Task<List<Budget>> GetAllByUserIdAndMonthAsync(Guid userId, string month);
    Task<Budget?> GetByIdAsync(Guid id);
    Task<List<Budget>> GetByCategoryIdAsync(Guid userId, Guid categoryId);

    /// Unicité par (UserId, Month, CategoryId) — un CategoryId null vaut pour le
    /// budget global. Exclut `excludeId` sur un update (jamais utile en pratique
    /// ici puisque CategoryId/Month sont immuables au PUT, mais garde la même
    /// forme que ICategoryRepository.ExistsAsync).
    Task<bool> ExistsAsync(Guid userId, string month, Guid? categoryId, Guid? excludeId = null);

    /// Dépenses du mois groupées par catégorie, pour un utilisateur. Alimente le
    /// Spent d'un budget catégorie (filtré sur la sienne) et d'un budget global
    /// (somme de toutes) en un seul aller-retour.
    Task<IReadOnlyList<CategorySpentProjection>> GetSpentTotalsAsync(
        Guid userId, DateOnly from, DateOnly toExclusive);

    /// Tous les budgets AutoRenew pour un mois donné, tous utilisateurs confondus —
    /// seule méthode de ce repository qui ne filtre pas par UserId : le job de
    /// duplication tourne hors contexte HTTP et boucle directement sur les
    /// utilisateurs via cette liste, sans passer par ICurrentUserService.
    Task<List<Budget>> GetAutoRenewCandidatesAsync(string month);

    void Add(Budget budget);
    void Update(Budget budget);
    void Delete(Budget budget);
}