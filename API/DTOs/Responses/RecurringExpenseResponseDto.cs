namespace API.DTOs.Responses;

public class RecurringExpenseResponseDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Type { get; set; } = null!;
    public string Frequency { get; set; } = null!;
    public DateOnly NextDueDate { get; set; }
    public bool Active { get; set; }

    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public string? CategoryTranslationKey { get; set; }
    public string? CategoryIcon { get; set; }
    public string? CategoryColor { get; set; }
}
