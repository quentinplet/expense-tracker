using System;
using System.Transactions;
using API.Entities;

namespace API.DTOs.Responses;

public class TransactionResponseDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Description { get; set; }
    public string CategoryName { get; set; } = null!;
    public int CategoryId { get; set; }
    public string Type { get; set; } = null!;

}
