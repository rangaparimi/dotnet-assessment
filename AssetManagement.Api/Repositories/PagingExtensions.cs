using AssetManagement.Api.Common;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Api.Repositories;

public static class PagingExtensions
{
    public const int MaxPageSize = 100;

    // Runs two queries: COUNT(*) for the totals, then one page of rows with
    // OFFSET/FETCH. The query must already be ordered, or pages can overlap.
    // A page number that is too high is moved back to the last page.
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> query, int page, int pageSize)
    {
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var totalCount = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }
}
