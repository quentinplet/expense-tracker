using System;
using API.DTOs.Responses;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data.Repositories;

public class TransactionRepository(AppDbContext context) : ITransactionRepository
{
    public async Task<PaginatedResult<Transaction>> GetAllTransactionsByUserIdAsync(TransactionParams transactionParams)
    {
        var query = context.Transactions.AsQueryable();

        query = query.Where(t => t.UserId == transactionParams.CurrentUserId);

        query = context.Transactions
            .Include(t => t.Category)
            .ThenInclude(c => c.TransactionType)
            .OrderByDescending(t => t.Date);

        return await PaginationHelper.CreateAsync(query, transactionParams.PageNumber, transactionParams.PageSize);
    }

    public async Task<IReadOnlyList<Transaction>> GetTransactionsByTypeAsync(string userId, TransactionTypeName type)
    {
        return await context.Transactions
            .Include(t => t.Category)
            .ThenInclude(c => c.TransactionType)
            .Where(t => t.UserId == userId && t.Category.TransactionType.Name == type)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }

    public async Task<List<Transaction>> GetMonthlyTransactionsAsync(string userId, int month, int year)
    {
        return await context.Transactions
            .Include(t => t.Category)
            .ThenInclude(c => c.TransactionType)
            .Where(t => t.UserId == userId && t.Date.Month == month && t.Date.Year == year)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
    }

    public async Task<List<Transaction>> GetTransactionsByIdsAsync(List<int> ids, string userId)
    {
        return await context.Transactions
            .Include(t => t.Category)
            .ThenInclude(c => c.TransactionType)
            .Where(t => ids.Contains(t.Id) && t.UserId == userId)
            .ToListAsync();
    }

    public async Task<Transaction?> GetTransactionByIdAsync(int id)
    {
        return await context.Transactions
            .Include(t => t.Category)
            .ThenInclude(c => c.TransactionType)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public void AddTransaction(Transaction transaction)
    {
        context.Transactions.Add(transaction);
    }

    public void UpdateTransaction(Transaction transaction)
    {
        context.Transactions.Update(transaction);
    }

    public void DeleteTransaction(Transaction transaction)
    {
        context.Transactions.Remove(transaction);
    }

    public void DeleteTransactions(List<Transaction> transactions)
    {
        context.Transactions.RemoveRange(transactions);
    }

}
