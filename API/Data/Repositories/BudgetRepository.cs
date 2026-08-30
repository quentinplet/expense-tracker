using System;
using API.Entities;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data.Repositories;

public class BudgetRepository(AppDbContext context) : IBudgetRepository
{
    public async Task<List<Budget>> GetAllByUserIdAndMonthAsync(Guid userId, string month)
    {
        return await context.Budgets
            .Include(b => b.Category)
            .Where(b => b.UserId == userId && b.Month == month)
            .ToListAsync();
    }

    public async Task<Budget?> GetByIdAsync(Guid id)
    {
        return await context.Budgets
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<List<Budget>> GetByCategoryIdAsync(Guid userId, Guid categoryId)
    {
        return await context.Budgets
            .Where(b => b.UserId == userId && b.CategoryId == categoryId)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(Guid userId, string month, Guid? categoryId, Guid? excludeId = null)
    {
        return await context.Budgets.AnyAsync(b =>
            b.UserId == userId && b.Month == month && b.CategoryId == categoryId && b.Id != excludeId);
    }

    public async Task<IReadOnlyList<CategorySpentProjection>> GetSpentTotalsAsync(
        Guid userId, DateOnly from, DateOnly toExclusive)
    {
        var rows = await context.Transactions
            .Where(t => t.UserId == userId
                && t.Type == TransactionType.Expense
                && t.Date >= from && t.Date < toExclusive)
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key, Total = g.Sum(t => t.Amount) })
            .ToListAsync();

        return [.. rows.Select(r => new CategorySpentProjection(r.CategoryId, r.Total))];
    }

    public async Task<List<Budget>> GetAutoRenewCandidatesAsync(string month)
    {
        return await context.Budgets
            .Where(b => b.AutoRenew && b.Month == month)
            .ToListAsync();
    }

    public void Add(Budget budget)
    {
        context.Budgets.Add(budget);
    }

    public void Update(Budget budget)
    {
        context.Budgets.Update(budget);
    }

    public void Delete(Budget budget)
    {
        context.Budgets.Remove(budget);
    }
}