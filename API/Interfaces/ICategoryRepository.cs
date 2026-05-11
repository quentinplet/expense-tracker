using System;
using API.Entities;

namespace API.Interfaces;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync();
    Task<List<Category>> GetByTypeAsync(TransactionTypeName transactionTypeName);
    Task<Category?> GetByIdAsync(int id);

    void Add(Category category);
    void Update(Category category);
    void Delete(Category category);
    Task<TransactionType?> GetTransactionTypeByNameAsync(TransactionTypeName name);

}
