using System;
using API.Data;
using API.Data.Repositories;
using API.DTOs.Responses;
using API.Entities;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public class DashboardService(IUnitOfWork uow) : IDashboardService
{
    public async Task<DashboardResponseDto> GetDashboardDataAsync(string userId, int month, int year)
    {
        var transactions = await uow.TransactionRepository.GetMonthlyTransactionsAsync(userId, month, year);
        var budgets = await uow.BudgetRepository.GetAllByUserIdAndMonthAsync(userId, month, year);

        var totalIncome = CalculateTotalIncome(transactions);
        var totalExpenses = CalculateTotalExpenses(transactions);
        var expensesByCategory = CalculateExpensesByCategory(transactions);
        var budgetSummaries = CalculateBudgetSummaries(budgets, transactions);

        return new DashboardResponseDto
        {
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            Balance = totalIncome - totalExpenses,
            ExpensesByCategory = expensesByCategory,
            BudgetSummaries = budgetSummaries
        };
    }
    private static decimal CalculateTotalIncome(List<Transaction> transactions)
    {
        return transactions
            .Where(t => t.Category.TransactionType.Name == TransactionTypeName.Income)
            .Sum(t => t.Amount);
    }

    private static decimal CalculateTotalExpenses(List<Transaction> transactions)
    {
        return transactions
            .Where(t => t.Category.TransactionType.Name == TransactionTypeName.Expense)
            .Sum(t => t.Amount);
    }

    private static List<CategorySummaryDto> CalculateExpensesByCategory(List<Transaction> transactions)
    {
        return transactions
            .Where(t => t.Category.TransactionType.Name == TransactionTypeName.Expense)
            .GroupBy(t => t.Category.Name)
            .Select(g => new CategorySummaryDto
            {
                CategoryName = g.Key,
                TotalAmount = g.Sum(t => t.Amount)
            })
            .OrderByDescending(c => c.TotalAmount)
            .ToList();
    }

    private static List<BudgetSummaryDto> CalculateBudgetSummaries(List<Budget> budgets, List<Transaction> transactions)
    {
        return budgets.Select(b =>
        {
            var spent = transactions
                .Where(t => t.CategoryId == b.CategoryId && t.Category.TransactionType.Name == TransactionTypeName.Expense)
                .Sum(t => t.Amount);
            return new BudgetSummaryDto
            {
                TotalBudget = b.Amount,
                TotalSpent = spent,
                RemainingBudget = b.Amount - spent,
                IsExceeded = spent > b.Amount
            };
        }).ToList();
    }
}
