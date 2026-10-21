namespace AssetManagement.UI.Models;

// What the pager needs, whatever the list holds
public interface IPaged
{
    int Page { get; }
    int PageSize { get; }
    int TotalCount { get; }
    int TotalPages { get; }
}

// One page of a list as the API sends it (the /paged endpoints)
public class PagedResult<T> : IPaged
{
    public List<T> Items { get; set; } = [];
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; } = 1;
}
