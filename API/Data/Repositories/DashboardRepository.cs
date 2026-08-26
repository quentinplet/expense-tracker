using System;
using API.Entities;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data.Repositories;

public class DashboardRepository(AppDbContext context) : IDashboardRepository
{
    /// Les plages sont semi-ouvertes plutôt que `Date.Month == m && Date.Year == y` :
    /// une comparaison sur des parties de date empêche l'usage de l'index (UserId, Date).
    
    public async Task<IReadOnlyList<MonthlyTotalProjection>> GetMonthlyTotalsAsync(Guid userId)
    {
        var rows = await context.Transactions
            .Where(t => t.UserId == userId)
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

    public async Task<IReadOnlyList<DailyTotalProjection>> GetDailyTotalsAsync(
        Guid userId, DateOnly from, DateOnly toExclusive)
    {
        var rows = await context.Transactions
            .Where(t => t.UserId == userId && t.Date >= from && t.Date < toExclusive)
            .GroupBy(t => new { t.Date, t.Type })
            .Select(g => new
            {
                g.Key.Date,
                g.Key.Type,
                Total = g.Sum(t => t.Amount),
            })
            .ToListAsync();

        return [.. rows.Select(r => new DailyTotalProjection(r.Date, r.Type, r.Total))];
    }

    public async Task<OverallTotalsProjection> GetOverallTotalsAsync(Guid userId)
    {
        var rows = await context.Transactions
            .Where(t => t.UserId == userId)
            .GroupBy(t => t.Type)
            .Select(g => new { Type = g.Key, Total = g.Sum(t => t.Amount) })
            .ToListAsync();

        return new OverallTotalsProjection(
            Income: rows.FirstOrDefault(r => r.Type == TransactionType.Income)?.Total ?? 0,
            Expenses: rows.FirstOrDefault(r => r.Type == TransactionType.Expense)?.Total ?? 0);
    }

    public async Task<IReadOnlyList<CategoryTotalProjection>> GetExpenseBreakdownAsync(
        Guid userId, DateOnly? from, DateOnly? toExclusive)
    {
        var query = context.Transactions
            .Where(t => t.UserId == userId && t.Type == TransactionType.Expense);

        if (from.HasValue) query = query.Where(t => t.Date >= from.Value);
        if (toExclusive.HasValue) query = query.Where(t => t.Date < toExclusive.Value);

        var rows = await query
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

    public async Task<IReadOnlyList<Transaction>> GetRecentAsync(Guid userId, int take)
    {
        return await context.Transactions
            .Where(t => t.UserId == userId)
            .Include(t => t.Category)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Take(take)
            .ToListAsync();
    }
}