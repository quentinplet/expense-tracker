using System;
using Microsoft.EntityFrameworkCore;

namespace API.Helpers;

public class PaginatedResult<T>
{
    public PaginationMetadata Metadata { get; set; } = default!;
    public List<T> Items { get; set; } = default!;
};

public class PaginationMetadata
{
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
};

public class PaginationHelper
{
    public static async Task<PaginatedResult<T>> CreateAsync<T>(IQueryable<T> query, int pageNumber, int pageSize)
    {
        var count = await query.CountAsync();
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        var totalPages = (int)Math.Ceiling(count / (double)pageSize);
        var metadata = new PaginationMetadata
        {
            TotalCount = count,
            PageSize = pageSize,
            CurrentPage = pageNumber,
            TotalPages = totalPages
        };

        return new PaginatedResult<T>
        {
            Metadata = metadata,
            Items = items
        };
    }
}


