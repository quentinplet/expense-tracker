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
    public async Task<IReadOnlyList<MonthlyTotalProjection>> GetMonthlyTotalsAsync(
        Guid userId, DateOnly from, DateOnly toExclusive)
    {
        return await context.Transactions
            .Where(t => t.UserId == userId && t.Date >= from && t.Date < toExclusive)
            .GroupBy(t => new { t.Date.Year, t.Date.Month, t.Type })
            .Select(g => new MonthlyTotalProjection(
                g.Key.Year,
                g.Key.Month,
                g.Key.Type,
                g.Sum(t => t.Amount)))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<CategoryTotalProjection>> GetExpenseBreakdownAsync(
        Guid userId, DateOnly from, DateOnly toExclusive)
    {
        return await context.Transactions
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
            .Select(g => new CategoryTotalProjection(
                g.Key.CategoryId,
                g.Key.Name,
                g.Key.TranslationKey,
                g.Key.Color,
                g.Key.Icon,
                g.Sum(t => t.Amount)))
            .OrderByDescending(c => c.Total)
            .ToListAsync();
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