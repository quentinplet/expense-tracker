using System;
using API.Entities;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data.Repositories;

public class DashboardRepository(AppDbContext context) : IDashboardRepository
{
    /// Toutes les requêtes filtrent sur une plage semi-ouverte plutôt que sur
    /// `Date.Month == m && Date.Year == y` : une comparaison sur des parties de date
    /// empêche l'usage de l'index (UserId, Date).
    ///
    /// L'agrégation se projette en type anonyme, puis les projections sont construites
    /// en mémoire : EF Core ne sait pas traduire un constructeur de record positionnel
    /// dans le Select qui suit un GroupBy.
    public async Task<IReadOnlyList<MonthlyTotalProjection>> GetMonthlyTotalsAsync(
        Guid userId, DateOnly from, DateOnly toExclusive)
    {
        var rows = await context.Transactions
            .Where(t => t.UserId == userId && t.Date >= from && t.Date < toExclusive)
            .GroupBy(t => new { t.Date.Year, t.Date.Month, t.Type })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Type,
                Total = g.Sum(t => t.Amount),
            })
            .ToListAsync();

        return [.. rows.Select(r => new MonthlyTotalProjection(r.Year, r.Month, r.Type, r.Total))];
    }

    public async Task<IReadOnlyList<CategoryTotalProjection>> GetExpenseBreakdownAsync(
        Guid userId, DateOnly from, DateOnly toExclusive)
    {
        var rows = await context.Transactions
            .Where(t => t.UserId == userId
                        && t.Date >= from
                        && t.Date < toExclusive
                        && t.Type == TransactionType.Expense)
            .GroupBy(t => new
            {
                t.CategoryId,
                t.Category.Name,
                t.Category.TranslationKey,
                t.Category.Color,
                t.Category.Icon,
            })
            .Select(g => new
            {
                g.Key.CategoryId,
                g.Key.Name,
                g.Key.TranslationKey,
                g.Key.Color,
                g.Key.Icon,
                Total = g.Sum(t => t.Amount),
            })
            .OrderByDescending(r => r.Total)
            .ToListAsync();

        return
        [
            .. rows.Select(r => new CategoryTotalProjection(
                r.CategoryId, r.Name, r.TranslationKey, r.Color, r.Icon, r.Total)),
        ];
    }

    public async Task<IReadOnlyList<Transaction>> GetRecentAsync(
        Guid userId, DateOnly from, DateOnly toExclusive, int take)
    {
        return await context.Transactions
            .Where(t => t.UserId == userId && t.Date >= from && t.Date < toExclusive)
            .Include(t => t.Category)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Take(take)
            .ToListAsync();
    }
}