using API.Entities;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data.Repositories;

public class RecurringExpenseRepository(AppDbContext context) : IRecurringExpenseRepository
{
    public async Task<List<RecurringExpense>> GetAllAsync(Guid userId)
    {
        return await context.RecurringExpenses
            .Include(r => r.Category)
            .Where(r => r.UserId == userId)
            .OrderBy(r => r.NextDueDate)
            .ToListAsync();
    }

    public async Task<RecurringExpense?> GetByIdAsync(Guid id)
    {
        return await context.RecurringExpenses
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<RecurringExpense>> GetByCategoryIdAsync(Guid userId, Guid categoryId)
    {
        return await context.RecurringExpenses
            .Where(r => r.UserId == userId && r.CategoryId == categoryId)
            .ToListAsync();
    }

    public async Task<List<RecurringExpense>> GetDueAsync(DateOnly today)
    {
        return await context.RecurringExpenses
            .Where(r => r.Active && r.NextDueDate <= today)
            .ToListAsync();
    }

    public void Add(RecurringExpense recurringExpense)
    {
        context.RecurringExpenses.Add(recurringExpense);
    }

    public void Update(RecurringExpense recurringExpense)
    {
        context.RecurringExpenses.Update(recurringExpense);
    }

    public void Delete(RecurringExpense recurringExpense)
    {
        context.RecurringExpenses.Remove(recurringExpense);
    }
}
