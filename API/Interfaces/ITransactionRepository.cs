using System;
using System.Transactions;
using API.Entities;

namespace API.Interfaces;

public interface ITransactionRepository
{
    Task<IReadOnlyList<Entities.Transaction>> GetAllTransactionsByUserIdAsync(string userId);
    Task<IReadOnlyList<Entities.Transaction>> GetTransactionsByTypeAsync(string userId, TransactionTypeName type);
    Task<List<Entities.Transaction>> GetMonthlyTransactionsAsync(string userId, int month, int year);
    Task<Entities.Transaction?> GetTransactionByIdAsync(int id);

    void AddTransaction(Entities.Transaction transaction);
    void UpdateTransaction(Entities.Transaction transaction);
    void DeleteTransaction(Entities.Transaction transaction);

}
