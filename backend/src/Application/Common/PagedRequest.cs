namespace InventoryApp.Application.Common;

public class PagedRequest
{
    private int _page = 1;
    private int _pageSize = 10;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value is < 1 or > 100 ? 10 : value;
    }

    public string? Search { get; set; }
    public string? SortBy { get; set; } = "Nombre";
    public string? SortDirection { get; set; } = "asc"; // asc | desc
}
