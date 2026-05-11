using System;
using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Requests;

public class BudgetRequestDto
{
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    [Required]
    [Range(1, 12, ErrorMessage = "Month must be between 1 and 12.")]
    public int Month { get; set; }
    [Required]
    [Range(2000, 2100, ErrorMessage = "Year must be between 2000 and 2100.")]
    public int Year { get; set; }

    [Required]
    public int CategoryId { get; set; }

}
