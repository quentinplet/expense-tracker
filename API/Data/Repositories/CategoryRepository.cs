using System;
using API.Entities;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data.Repositories;

public class CategoryRepository(AppDbContext context) : ICategoryRepository
{
    public async Task<List<Category>> GetAllAsync()
    {
        return await context.Categories.ToListAsync();
    }

    public async Task<List<Category>> GetByTypeAsync(TransactionType type)
    {
        return await context.Categories
            .Where(c => c.Type == type)
            .ToListAsync();
    }

    public async Task<Category?> GetByIdAsync(Guid id)
    {
        return await context.Categories.FirstOrDefaultAsync(c => c.Id == id);
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