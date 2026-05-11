using System;
using API.DTOs.Responses;
using API.Entities;

namespace API.Extensions;

public static class BudgetExtension
{
    public static BudgetResponseDto ToBudgetResponseDto(this Budget budget)
    {
        return new BudgetResponseDto
        {
            Id = budget.Id,
            Amount = budget.Amount,
            Month = budget.Month,
            Year = budget.Year,
            CategoryId = budget.CategoryId,
            CategoryName = budget.Category.Name
        };
    }
}
