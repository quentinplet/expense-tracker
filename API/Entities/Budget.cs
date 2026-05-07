using System;

namespace API.Entities;

public class Budget
{
    public int Id { get; set; }
    public required decimal Amount { get; set; }

    public required int Month { get; set; }
    public required int Year { get; set; }

    //navigation properties for category
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    //navigation properties for user
    public string UserId { get; set; } = null!;
    public AppUser User { get; set; } = null!;

}
