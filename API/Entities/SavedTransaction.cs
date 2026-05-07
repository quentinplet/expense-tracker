namespace API.Entities;

public enum Frequency
{
    Daily,
    Weekly,
    Monthly,
    Yearly
}

public class SavedTransaction
{
    public int Id { get; set; }
    public required decimal Amount { get; set; }
    public string? Description { get; set; }
    public Frequency Frequency { get; set; }
    public DateOnly UpcomingDate { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public int TransactionTypeId { get; set; }
    public TransactionType TransactionType { get; set; } = null!;

    public string UserId { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}