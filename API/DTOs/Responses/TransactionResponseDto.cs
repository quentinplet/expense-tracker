using System;
using API.Entities;

namespace API.DTOs.Responses;

public class TransactionResponseDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string Label { get; set; } = null!;
    public string? Note { get; set; }
    public string CategoryName { get; set; } = null!;

    /// Non nulle pour une catégorie système : le client affiche la traduction
    /// et retombe sur CategoryName pour une catégorie créée par l'utilisateur.
    public string? CategoryTranslationKey { get; set; }
    public string? CategoryColor { get; set; }
    public string? CategoryIcon { get; set; }
    public Guid CategoryId { get; set; }
    public string Type { get; set; } = null!;

}