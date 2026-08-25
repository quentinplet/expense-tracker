using System;

namespace API.Entities;

public class Budget
{
    public Guid Id { get; set; }
    public required decimal Amount { get; set; }

    public required int Month { get; set; }
    public required int Year { get; set; }

    //navigation properties for category
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    //navigation properties for user
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

}