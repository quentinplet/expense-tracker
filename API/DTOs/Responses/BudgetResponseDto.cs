using System;

namespace API.DTOs.Responses;

public class BudgetResponseDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

}
