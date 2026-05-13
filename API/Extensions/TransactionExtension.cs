using System;
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
            Description = transaction.Description,
            CategoryName = transaction.Category?.Name ?? "Non spécifiée",
            Type = transaction.Category?.TransactionType?.Name.ToString() ?? "Unknown"
        };
    }
}
