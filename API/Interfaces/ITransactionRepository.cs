using System;
using System.Transactions;
using API.Entities;
using API.Helpers;

namespace API.Interfaces;

public interface ITransactionRepository
{
    Task<PaginatedResult<Entities.Transaction>> GetAllTransactionsByUserIdAsync(TransactionParams transactionParams);
    Task<IReadOnlyList<Entities.Transaction>> GetTransactionsByTypeAsync(string userId, TransactionTypeName type);
    Task<List<Entities.Transaction>> GetMonthlyTransactionsAsync(string userId, int month, int year);
    Task<List<Entities.Transaction>> GetTransactionsByIdsAsync(List<int> ids, string userId);
    Task<Entities.Transaction?> GetTransactionByIdAsync(int id);

    void AddTransaction(Entities.Transaction transaction);
    void UpdateTransaction(Entities.Transaction transaction);
    void DeleteTransaction(Entities.Transaction transaction);
    void DeleteTransactions(List<Entities.Transaction> transactions);

}
