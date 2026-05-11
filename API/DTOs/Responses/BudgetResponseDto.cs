using System;

namespace API.DTOs.Responses;

public class BudgetResponseDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

}
