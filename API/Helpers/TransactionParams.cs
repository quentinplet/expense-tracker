using System;
using API.Entities;

namespace API.Helpers;

public class TransactionParams : PagingParams
{
    public string? CurrentUserId { get; set; }

    public int? CategoryId { get; set; }
    public TransactionTypeName? TransactionType { get; set; }
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; } = "desc";


}
