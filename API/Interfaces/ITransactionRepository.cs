using System;
using System.Transactions;
using API.Entities;

namespace API.Interfaces;

public interface ITransactionRepository
{
    Task<List<Entities.Transaction>> GetAllTransactionsByUserIdAsync(string userId);
    Task<List<Entities.Transaction>> GetTransactionsByTypeAsync(string userId, TransactionTypeName type);
    Task<Entities.Transaction?> GetTransactionByIdAsync(int id);

    void AddTransaction(Entities.Transaction transaction);
    void UpdateTransaction(Entities.Transaction transaction);
    void DeleteTransaction(Entities.Transaction transaction);
    Task<bool> SaveAllAsync();

}
