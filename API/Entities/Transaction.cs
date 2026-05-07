using System;

namespace API.Entities;

public class Transaction
{
    public int Id { get; set; }
    public required decimal Amount { get; set; }
    public required DateTime Date { get; set; }
    public string? Description { get; set; }

    //navigation properties for category
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    //navigation properties for user
    public string UserId { get; set; } = null!;
    public AppUser User { get; set; } = null!;

}
