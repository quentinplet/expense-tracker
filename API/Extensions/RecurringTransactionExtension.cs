using API.DTOs.Responses;
using API.Entities;

namespace API.Extensions;

public static class RecurringTransactionExtension
{
    public static RecurringTransactionResponseDto ToRecurringTransactionResponseDto(this RecurringTransaction recurringTransaction)
    {
        return new RecurringTransactionResponseDto
        {
            Id = recurringTransaction.Id,
            Label = recurringTransaction.Label,
            Amount = recurringTransaction.Amount,
            Type = recurringTransaction.Type.ToString(),
            Frequency = recurringTransaction.Frequency.ToString(),
            NextDueDate = recurringTransaction.NextDueDate,
            Active = recurringTransaction.Active,
            CategoryId = recurringTransaction.CategoryId,
            CategoryName = recurringTransaction.Category?.Name ?? string.Empty,
            CategoryTranslationKey = recurringTransaction.Category?.TranslationKey,
            CategoryIcon = recurringTransaction.Category?.Icon,
            CategoryColor = recurringTransaction.Category?.Color,
        };
    }
}
