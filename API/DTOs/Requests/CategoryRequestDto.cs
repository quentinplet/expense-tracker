using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using API.Entities;

namespace API.DTOs.Requests;

public class CategoryRequestDto
{
    [Required(ErrorMessage = "The Category Name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "The Category Name must be between 2 and 100 characters")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "The Category Icon is required")]
    public string? Icon { get; set; }

    [Required(ErrorMessage = "The Category Color is required")]
    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "The Category Color must be a hex value like #3b82f6")]
    public string? Color { get; set; }

    // Lu à la création, ignoré à l'update — voir CategoriesController.Update.
    [Required(ErrorMessage = "The Category Type is required")]
    [EnumDataType(typeof(TransactionType))]
    [JsonConverter(typeof(JsonStringEnumConverter<TransactionType>))]
    public TransactionType? Type { get; set; }
}
