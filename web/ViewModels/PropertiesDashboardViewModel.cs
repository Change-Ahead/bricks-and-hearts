namespace BricksAndHearts.ViewModels;

public class PropertiesDashboardViewModel
{
    public List<PropertyViewModel> Properties { get; set; }
    
    public int Page { get; set; }
    public int Total { get; set; } 
    
    public string? SortBy { get; set; }

    public int PropPerPage { get; set; } = 10;

    public PropertiesDashboardViewModel(List<PropertyViewModel> properties, int total, int page = 1, string? sortBy = null)
    {
        Properties = properties;
        Total = total;
        Page = page;
        SortBy = sortBy;
    }
}