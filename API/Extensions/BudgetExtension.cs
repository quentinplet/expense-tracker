using System;
using API.DTOs.Responses;
using API.Entities;

namespace API.Extensions;

public static class BudgetExtension
{
    /// `spent` est calculé par le service (agrégation sur les transactions), jamais
    /// stocké sur l'entité — voir BudgetService.
    public static BudgetResponseDto ToBudgetResponseDto(this Budget budget, decimal spent)
    {
        return new BudgetResponseDto
        {
            Id = budget.Id,
            Month = budget.Month,
            CategoryId = budget.CategoryId,
            CategoryName = budget.Category?.Name,
            CategoryTranslationKey = budget.Category?.TranslationKey,
            CategoryIcon = budget.Category?.Icon,
            CategoryColor = budget.Category?.Color,
            AmountLimit = budget.AmountLimit,
            AutoRenew = budget.AutoRenew,
            Spent = spent,
            Remaining = budget.AmountLimit - spent
        };
    }
}