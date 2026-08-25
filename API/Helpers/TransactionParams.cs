using System;
using API.Entities;

namespace API.Helpers;

public class TransactionParams : PagingParams
{
    public Guid? CurrentUserId { get; set; }

    public Guid? CategoryId { get; set; }
    public TransactionType? TransactionType { get; set; }
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; } = "desc";


}
