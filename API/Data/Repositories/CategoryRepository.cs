using System;
using API.Entities;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data.Repositories;

public class CategoryRepository(AppDbContext context) : ICategoryRepository
{
    public async Task<List<Category>> GetAllAsync()
    {
        return await context.Categories
            .Include(c => c.TransactionType)
            .ToListAsync();
    }

    public async Task<List<Category>> GetByTypeAsync(TransactionTypeName transactionTypeName)
    {
        return await context.Categories
            .Include(c => c.TransactionType)
            .Where(c => c.TransactionType.Name == transactionTypeName)
            .ToListAsync();
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        return await context.Categories
            .Include(c => c.TransactionType)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public void Add(Category category)
    {
        context.Categories.Add(category);
    }

    public void Update(Category category)
    {
        context.Categories.Update(category);
    }

    public void Delete(Category category)
    {
        context.Categories.Remove(category);
    }

    public async Task<TransactionType?> GetTransactionTypeByNameAsync(TransactionTypeName name)
    {
        return await context.TransactionTypes.FirstOrDefaultAsync(t => t.Name == name);
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await context.SaveChangesAsync() > 0;
    }
}
