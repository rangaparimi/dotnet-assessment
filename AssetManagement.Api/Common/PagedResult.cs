namespace AssetManagement.Api.Common;

// One page of a longer list, plus the numbers a page bar needs
public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int Page { get; set; }          // current page, starting at 1
    public int PageSize { get; set; }      // rows per page
    public int TotalCount { get; set; }    // rows in the whole list
    public int TotalPages { get; set; }    // at least 1, even for an empty list

    // Convert every item (entity -> DTO) and keep the paging numbers
    public PagedResult<TOut> Map<TOut>(Func<T, TOut> convert) => new()
    {
        Items = Items.Select(convert).ToList(),
        Page = Page,
        PageSize = PageSize,
        TotalCount = TotalCount,
        TotalPages = TotalPages
    };
}
