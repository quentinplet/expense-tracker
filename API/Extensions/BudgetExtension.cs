using System;
using API.DTOs.Responses;
using API.Entities;

namespace API.Extensions;

public static class BudgetExtension
{
    /// `spent` est calculé par le service (agrégation sur les transactions), jamais
    /// stocké sur l'entité — voir BudgetService. `autoRenewConflict` par défaut à
    /// false : seul GetBudgetsForMonthAsync le calcule (§ BudgetService), les autres
    /// appelants renvoient un budget isolé pour lequel la question ne se pose pas de
    /// la même façon.
    public static BudgetResponseDto ToBudgetResponseDto(
        this Budget budget, decimal spent, bool autoRenewConflict = false)
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
            AutoRenewConflict = autoRenewConflict,
            Spent = spent,
            Remaining = budget.AmountLimit - spent
        };
    }
}