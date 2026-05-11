using System;
using API.Entities;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data.Repositories;

public class BudgetRepository(AppDbContext context) : IBudgetRepository
{
    public async Task<IReadOnlyList<Budget>> GetAllByUserIdAsync(string userId)
    {
        return await context.Budgets
            .Include(b => b.Category)
            .Include(b => b.Category.TransactionType)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.Year)
            .ThenByDescending(b => b.Month)
            .ToListAsync();
    }
    public async Task<List<Budget>> GetAllByUserIdAndMonthAsync(string userId, int month, int year)
    {
        return await context.Budgets
            .Include(b => b.Category)
            .Include(b => b.Category.TransactionType)
            .Where(b => b.UserId == userId && b.Month == month && b.Year == year)
            .ToListAsync();
    }

    public async Task<Budget?> GetByIdAsync(int id)
    {
        return await context.Budgets
            .Include(b => b.Category)
            .Include(b => b.Category.TransactionType)
            .FirstOrDefaultAsync(b => b.Id == id);
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
