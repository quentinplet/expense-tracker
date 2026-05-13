using System;

namespace API.Helpers;

public class TransactionParams : PagingParams
{
    public string? CurrentUserId { get; set; }

}
