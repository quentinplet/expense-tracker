using System;
using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Requests;

public class BudgetRequestDto
{
    // Pas de [Required] : absent ou null == budget global. Ignoré au PUT (CategoryId
    // est immuable après création), comme Month ci-dessous.
    public Guid? CategoryId { get; set; }

    [Required(ErrorMessage = "The Budget Month is required")]
    [RegularExpression(@"^\d{4}-(0[1-9]|1[0-2])$", ErrorMessage = "The Budget Month must be in YYYY-MM format")]
    public string? Month { get; set; }

    [Required(ErrorMessage = "The Budget Amount Limit is required")]
    [Range(0, (double)decimal.MaxValue, ErrorMessage = "The Budget Amount Limit cannot be negative")]
    public decimal? AmountLimit { get; set; }

    [Required(ErrorMessage = "The Budget AutoRenew flag is required")]
    public bool? AutoRenew { get; set; }
}