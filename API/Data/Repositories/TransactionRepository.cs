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

        var query = context.Transactions
            .Where(t => t.UserId == transactionParams.CurrentUserId)
            .Include(t => t.Category)
            .ThenInclude(c => c.TransactionType)
            .AsQueryable();


        // Filter by category
        if (transactionParams.CategoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == transactionParams.CategoryId.Value);
        }

        // Filter by transaction type
        if (transactionParams.TransactionType.HasValue)
        {
            query = query.Where(t => t.Category.TransactionType.Name == transactionParams.TransactionType.Value);
        }

        // Search by description, category name
        if (!string.IsNullOrEmpty(transactionParams.Search))
        {
            var search = transactionParams.Search.ToLower();
            query = query.Where(t => EF.Functions.Like(t.Description.ToLower(), $"%{search}%") ||
                                     EF.Functions.Like(t.Category.Name.ToLower(), $"%{search}%"));

        }

        // Sort
        query = transactionParams.SortBy?.ToLower() switch
        {
            "date" => transactionParams.SortDirection == "asc"
            ? query.OrderBy(t => t.Date)
            : query.OrderByDescending(t => t.Date),
            "amount" => transactionParams.SortDirection == "asc"
            ? query.OrderBy(t => t.Amount)
            : query.OrderByDescending(t => t.Amount),
            "category" => transactionParams.SortDirection == "asc"
            ? query.OrderBy(t => t.Category.Name)
            : query.OrderByDescending(t => t.Category.Name),
            _ => query.OrderByDescending(t => t.Date)
        };


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
