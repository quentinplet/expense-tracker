using System;
using API.Entities;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data.Repositories;

public class TransactionRepository(AppDbContext context) : ITransactionRepository
{
    public async Task<IReadOnlyList<Transaction>> GetAllTransactionsByUserIdAsync(string userId)
    {
        return await context.Transactions
            .Include(t => t.Category)
            .ThenInclude(c => c.TransactionType)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
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

}
