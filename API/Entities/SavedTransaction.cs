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
    public Guid Id { get; set; }
    public required decimal Amount { get; set; }
    public string? Description { get; set; }
    public Frequency Frequency { get; set; }
    public DateOnly UpcomingDate { get; set; }

    public TransactionType Type { get; set; }

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
}