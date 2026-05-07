using System;
using System.ComponentModel;

namespace API.Entities;

public enum TransactionTypeName
{
    Expense,
    Income
}

public class TransactionType
{
    public int Id { get; set; }
    public TransactionTypeName Name { get; set; }
    //navigation property
    public ICollection<Category> Categories { get; set; } = [];
}
