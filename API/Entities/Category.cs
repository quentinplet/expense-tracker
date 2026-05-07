using System;

namespace API.Entities;

public class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public bool Enabled { get; set; } = true;

    //navigation property
    public int TransactionTypeId { get; set; }
    public TransactionType TransactionType { get; set; } = null!;

    public ICollection<Transaction> Transactions { get; set; } = [];

}
