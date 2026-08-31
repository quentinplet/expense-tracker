using API.Entities;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data.Repositories;

public class RecurringTransactionRepository(AppDbContext context) : IRecurringTransactionRepository
{
    public async Task<List<RecurringTransaction>> GetAllAsync(Guid userId)
    {
        return await context.RecurringTransactions
            .Include(r => r.Category)
            .Where(r => r.UserId == userId)
            .OrderBy(r => r.NextDueDate)
            .ToListAsync();
    }

    public async Task<RecurringTransaction?> GetByIdAsync(Guid id)
    {
        return await context.RecurringTransactions
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<RecurringTransaction>> GetByCategoryIdAsync(Guid userId, Guid categoryId)
    {
        return await context.RecurringTransactions
            .Where(r => r.UserId == userId && r.CategoryId == categoryId)
            .ToListAsync();
    }

    public async Task<List<RecurringTransaction>> GetDueAsync(DateOnly today)
    {
        return await context.RecurringTransactions
            .Where(r => r.Active && r.NextDueDate <= today)
            .ToListAsync();
    }

    public void Add(RecurringTransaction recurringTransaction)
    {
        context.RecurringTransactions.Add(recurringTransaction);
    }

    public void Update(RecurringTransaction recurringTransaction)
    {
        context.RecurringTransactions.Update(recurringTransaction);
    }

    public void Delete(RecurringTransaction recurringTransaction)
    {
        context.RecurringTransactions.Remove(recurringTransaction);
    }
}
