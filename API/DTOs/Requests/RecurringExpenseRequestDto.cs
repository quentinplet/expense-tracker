using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using API.Entities;

namespace API.DTOs.Requests;

public class RecurringExpenseRequestDto
{
    [Required(ErrorMessage = "The Recurring Expense Label is required")]
    [StringLength(200, ErrorMessage = "The Recurring Expense Label must be 200 characters or fewer")]
    public string? Label { get; set; }

    [Required(ErrorMessage = "The Recurring Expense Amount is required")]
    [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "The Recurring Expense Amount must be greater than 0")]
    public decimal? Amount { get; set; }

    // Lu à la création, ignoré à l'update — même gotcha que Category.Type.
    [Required(ErrorMessage = "The Recurring Expense Type is required")]
    [EnumDataType(typeof(TransactionType))]
    [JsonConverter(typeof(JsonStringEnumConverter<TransactionType>))]
    public TransactionType? Type { get; set; }

    [Required(ErrorMessage = "The Recurring Expense Frequency is required")]
    [EnumDataType(typeof(Frequency))]
    [JsonConverter(typeof(JsonStringEnumConverter<Frequency>))]
    public Frequency? Frequency { get; set; }

    [Required(ErrorMessage = "The Recurring Expense Next Due Date is required")]
    public DateOnly? NextDueDate { get; set; }

    [Required(ErrorMessage = "The Recurring Expense Active flag is required")]
    public bool? Active { get; set; }

    [Required(ErrorMessage = "The Recurring Expense Category is required")]
    public Guid? CategoryId { get; set; }
}
