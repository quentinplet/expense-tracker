using API.DTOs.Responses;
using API.Entities;

namespace API.Extensions;

public static class RecurringExpenseExtension
{
    public static RecurringExpenseResponseDto ToRecurringExpenseResponseDto(this RecurringExpense expense)
    {
        return new RecurringExpenseResponseDto
        {
            Id = expense.Id,
            Label = expense.Label,
            Amount = expense.Amount,
            Type = expense.Type.ToString(),
            Frequency = expense.Frequency.ToString(),
            NextDueDate = expense.NextDueDate,
            Active = expense.Active,
            CategoryId = expense.CategoryId,
            CategoryName = expense.Category?.Name ?? string.Empty,
            CategoryTranslationKey = expense.Category?.TranslationKey,
            CategoryIcon = expense.Category?.Icon,
            CategoryColor = expense.Category?.Color,
        };
    }
}
