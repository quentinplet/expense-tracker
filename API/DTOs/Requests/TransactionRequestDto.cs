using System;
using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Requests;

public class TransactionRequestDto
{
    [Required(ErrorMessage = "The Transaction Amount is required")]
    [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "The Transaction Amount must be greater than 0")]
    public decimal? Amount { get; set; }

    [Required(ErrorMessage = "The Transaction Date is required")]
    public DateTime? Date { get; set; }

    public string? Description { get; set; }

    [Required(ErrorMessage = "The Transaction Category is required")]
    public int? CategoryId { get; set; }
}
