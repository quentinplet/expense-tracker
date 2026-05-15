using System;
using API.Entities;
using API.DTOs.Responses;
namespace API.Extensions;

public static class CategoryExtension
{
    public static CategoryResponseDto ToCategoryResponseDto(this Category category)
    {
        return new CategoryResponseDto
        {
            Id = category.Id,
            Name = category.Name,
            Enabled = category.Enabled,
            Type = category.TransactionType?.Name.ToString() ?? "Unknown"
        };
    }

}
