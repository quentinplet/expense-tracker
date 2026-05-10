using System;

namespace API.DTOs;

public class SeedDataDto
{
    public List<CategorySeedDto> Categories { get; set; } = [];
    public List<TransactionSeedDto> Transactions { get; set; } = [];
}

public class CategorySeedDto
{
    public string Name { get; set; } = null!;
    public bool Enabled { get; set; }
    public int TransactionTypeId { get; set; }
}

public class TransactionSeedDto
{
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public string UserId { get; set; } = null!;
}
