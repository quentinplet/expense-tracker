using System;
using API.Entities;

namespace API.Interfaces;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync();
    Task<List<Category>> GetByTypeAsync(TransactionType type);
    Task<Category?> GetByIdAsync(Guid id);

    void Add(Category category);
    void Update(Category category);
    void Delete(Category category);

}
