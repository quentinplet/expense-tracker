using System;

namespace API.DTOs.Responses;

public class BudgetResponseDto
{
    public Guid Id { get; set; }
    public string Month { get; set; } = null!;

    // Tous null pour un budget global (CategoryId == null).
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? CategoryTranslationKey { get; set; }
    public string? CategoryIcon { get; set; }
    public string? CategoryColor { get; set; }

    public decimal AmountLimit { get; set; }
    public bool AutoRenew { get; set; }
    public decimal Spent { get; set; }

    /// AmountLimit - Spent, peut être négatif.
    public decimal Remaining { get; set; }
}