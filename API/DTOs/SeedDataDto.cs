using System;

namespace API.DTOs;

public class SeedDataDto
{
    public List<CategorySeedDto> Categories { get; set; } = [];
    public List<TransactionSeedDto> Transactions { get; set; } = [];
}

public class CategorySeedDto
{
    /// Identifiant local au fichier de seed, remappé vers un Guid à l'insertion.
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool Enabled { get; set; }

    /// 1 = Expense, 2 = Income — voir le bloc transactionTypes du fichier.
    public int TransactionTypeId { get; set; }
    public string Icon { get; set; } = null!;
    public string Color { get; set; } = null!;
    public string TranslationKey { get; set; } = null!;

    /// True uniquement pour les deux lignes "Other" (une par TransactionTypeId).
    public bool IsLocked { get; set; }
}

public class TransactionSeedDto
{
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string? Description { get; set; }

    /// Référence l'Id local d'une CategorySeedDto.
    public int CategoryId { get; set; }
    public string UserId { get; set; } = null!;
}