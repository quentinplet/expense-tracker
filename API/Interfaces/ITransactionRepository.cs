using System;
using System.Transactions;
using API.DTOs.Responses;
using API.Entities;
using API.Helpers;

namespace API.Interfaces;

public interface ITransactionRepository
{
    Task<PaginatedResult<Entities.Transaction>> GetAllTransactionsByUserIdAsync(TransactionParams transactionParams);
    Task<IReadOnlyList<Entities.Transaction>> GetTransactionsByTypeAsync(Guid userId, TransactionType type);
    Task<List<Entities.Transaction>> GetMonthlyTransactionsAsync(Guid userId, int month, int year);
    Task<List<Entities.Transaction>> GetTransactionsByIdsAsync(List<Guid> ids, Guid userId);
    Task<List<Entities.Transaction>> GetByCategoryIdAsync(Guid userId, Guid categoryId);
    Task<Entities.Transaction?> GetTransactionByIdAsync(Guid id);

    void AddTransaction(Entities.Transaction transaction);
    void UpdateTransaction(Entities.Transaction transaction);
    void DeleteTransaction(Entities.Transaction transaction);
    void DeleteTransactions(List<Entities.Transaction> transactions);

}
