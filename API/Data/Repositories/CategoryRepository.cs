using System;
using API.Entities;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data.Repositories;

public class CategoryRepository(AppDbContext context) : ICategoryRepository
{
    public async Task<List<Category>> GetAllAsync(Guid userId, TransactionType? type = null)
    {
        var query = context.Categories.Where(c => c.UserId == userId);
        if (type != null) query = query.Where(c => c.Type == type);

        return await query
            .OrderBy(c => c.Type)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Category?> GetByIdAsync(Guid id)
    {
        return await context.Categories.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Category?> GetLockedByTypeAsync(Guid userId, TransactionType type)
    {
        return await context.Categories
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Type == type && c.IsLocked);
    }

    public async Task<bool> ExistsAsync(Guid userId, string name, TransactionType type, Guid? excludeId = null)
    {
        return await context.Categories
            .AnyAsync(c => c.UserId == userId && c.Name == name && c.Type == type && c.Id != excludeId);
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

}
