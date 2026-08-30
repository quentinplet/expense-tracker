using System;
using API.DTOs.Responses;
using API.Entities;
using API.Extensions;
using API.Interfaces;

namespace API.Services;

public class BudgetService(IUnitOfWork uow) : IBudgetService
{
    public async Task<List<BudgetResponseDto>> GetBudgetsForMonthAsync(Guid userId, string month)
    {
        var budgets = await uow.BudgetRepository.GetAllByUserIdAndMonthAsync(userId, month);

        var (from, toExclusive) = ResolveMonthRange(month);
        var spentByCategory = await uow.BudgetRepository.GetSpentTotalsAsync(userId, from, toExclusive);

        // Le Spent d'un budget global agrège toutes les catégories de dépense du mois ;
        // celui d'un budget catégorie ne filtre que la sienne (§ Key Gotchas backend).
        var totalSpent = spentByCategory.Sum(s => s.Total);

        // AutoRenewConflict (§ BudgetResponseDto) : un aller-retour de plus, seulement
        // si au moins un budget du mois a AutoRenew actif — sinon la question ne se
        // pose pour personne et l'appel est sauté.
        var nextMonthCategoryIds = budgets.Any(b => b.AutoRenew)
            ? (await uow.BudgetRepository.GetAllByUserIdAndMonthAsync(userId, NextMonthKey(month)))
                .Select(b => b.CategoryId)
                .ToHashSet()
            : [];

        return
        [
            .. budgets
                .OrderBy(b => b.CategoryId == null ? 0 : 1) // budget global en tête
                .Select(b => b.ToBudgetResponseDto(
                    Spent(b, spentByCategory, totalSpent),
                    b.AutoRenew && nextMonthCategoryIds.Contains(b.CategoryId))),
        ];
    }

    private static decimal Spent(
        Budget budget, IReadOnlyList<CategorySpentProjection> spentByCategory, decimal totalSpent)
    {
        if (budget.CategoryId == null) return totalSpent;
        return spentByCategory.Where(s => s.CategoryId == budget.CategoryId).Sum(s => s.Total);
    }

    public async Task<decimal> GetSpentAsync(Guid userId, Budget budget)
    {
        var (from, toExclusive) = ResolveMonthRange(budget.Month);
        var totals = await uow.BudgetRepository.GetSpentTotalsAsync(userId, from, toExclusive);

        return budget.CategoryId == null
            ? totals.Sum(t => t.Total)
            : totals.Where(t => t.CategoryId == budget.CategoryId).Sum(t => t.Total);
    }

    public async Task<int> RenewBudgetsAsync()
    {
        // Le job tourne hors contexte HTTP : le mois courant se lit en UTC, comme la
        // courbe mensuelle du dashboard (§ DashboardService.BuildTrend).
        var currentMonth = ToMonthKey(DateTime.UtcNow);
        var nextMonth = ToMonthKey(DateTime.UtcNow.AddMonths(1));

        var candidates = await uow.BudgetRepository.GetAutoRenewCandidatesAsync(currentMonth);

        var created = 0;
        foreach (var budget in candidates)
        {
            // Idempotence : on vérifie l'absence de la ligne suivante avant d'insérer,
            // on ne s'appuie jamais sur l'index unique pour absorber un doublon.
            if (await uow.BudgetRepository.ExistsAsync(budget.UserId, nextMonth, budget.CategoryId))
                continue;

            uow.BudgetRepository.Add(new Budget
            {
                AmountLimit = budget.AmountLimit,
                Month = nextMonth,
                AutoRenew = budget.AutoRenew,
                CategoryId = budget.CategoryId,
                UserId = budget.UserId,
            });
            created++;
        }

        if (created > 0) await uow.Complete();
        return created;
    }

    private static string ToMonthKey(DateTime date) => date.ToString("yyyy-MM");

    private static string NextMonthKey(string month)
    {
        var (from, _) = ResolveMonthRange(month);
        return from.AddMonths(1).ToString("yyyy-MM");
    }

    private static (DateOnly From, DateOnly ToExclusive) ResolveMonthRange(string month)
    {
        var from = new DateOnly(int.Parse(month[..4]), int.Parse(month[5..]), 1);
        return (from, from.AddMonths(1));
    }
}