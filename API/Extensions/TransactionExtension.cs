using System;
using API.DTOs.Requests;
using API.DTOs.Responses;
using API.Entities;

namespace API.Extensions;

public static class TransactionExtension
{
    public static TransactionResponseDto ToTransactionResponseDto(this Transaction transaction)
    {
        return new TransactionResponseDto
        {
            Id = transaction.Id,
            Amount = transaction.Amount,
            Date = transaction.Date,
            Label = transaction.Label,
            Note = transaction.Note,
            CategoryName = transaction.Category?.Name ?? string.Empty,
            CategoryTranslationKey = transaction.Category?.TranslationKey,
            CategoryColor = transaction.Category?.Color,
            CategoryIcon = transaction.Category?.Icon,
            CategoryId = transaction.CategoryId,
            Type = transaction.Type.ToString()
        };
    }

    public static Transaction ToEntity(this TransactionRequestDto dto, Guid userId) => new()
    {
        Amount = dto.Amount!.Value,
        Type = dto.Type!.Value,
        Date = dto.Date!.Value,
        Label = dto.Label!,
        Note = dto.Note,
        CategoryId = dto.CategoryId!.Value,
        UserId = userId
    };

    public static void Apply(this TransactionRequestDto dto, Transaction transaction)
    {
        transaction.Amount = dto.Amount!.Value;
        transaction.Type = dto.Type!.Value;
        transaction.Date = dto.Date!.Value;
        transaction.Label = dto.Label!;
        transaction.Note = dto.Note;
        transaction.CategoryId = dto.CategoryId!.Value;
    }
}