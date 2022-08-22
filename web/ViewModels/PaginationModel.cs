namespace BricksAndHearts.ViewModels;

public class PaginationModel
{
    public int Page { get; set; }
    public int Total { get; set; }
    public int ItemsPerPage { get; set; }
    public int ItemsOnPage { get; set; }
    public string? HrefPrevious { get; set; }
    public string? HrefNext { get; set; }
    public string TypeOfData { get; set; } = string.Empty;
}