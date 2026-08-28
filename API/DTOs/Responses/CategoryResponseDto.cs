using System;
using API.Entities;

namespace API.DTOs.Responses;

public class CategoryResponseDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public bool Enabled { get; set; }
    public string Type { get; set; } = null!;
    public string? TranslationKey { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public bool IsLocked { get; set; }

}
