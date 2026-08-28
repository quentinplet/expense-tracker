using System;
using API.Entities;

namespace API.Interfaces;

public interface ICategoryRepository
{
    /// Catégories système (UserId null) + celles créées par cet utilisateur.
    Task<List<Category>> GetAllAsync(Guid userId);
    Task<List<Category>> GetByTypeAsync(Guid userId, TransactionType type);
    Task<Category?> GetByIdAsync(Guid id);

    void Add(Category category);
    void Update(Category category);
    void Delete(Category category);

}
