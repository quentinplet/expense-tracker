using System;

namespace API.DTOs.Responses;

public class DashboardResponseDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal Balance { get; set; }
    public IReadOnlyList<CategorySummaryDto> ExpensesByCategory { get; set; } = [];
    public IReadOnlyList<BudgetSummaryDto> BudgetSummaries { get; set; } = [];
}

public class CategorySummaryDto
{
    public string CategoryName { get; set; } = null!;
    public decimal TotalAmount { get; set; }
}

public class BudgetSummaryDto
{
    public decimal TotalBudget { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal RemainingBudget { get; set; }
    public bool IsExceeded { get; set; }
}