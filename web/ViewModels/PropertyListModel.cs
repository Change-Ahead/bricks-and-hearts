namespace BricksAndHearts.ViewModels;

public class PropertyListModel
{
    public List<PropertyDetailsViewModel> Properties { get; set; }

    public LandlordProfileModel? Owner { get; set; }

    public int Page { get; set; }
    public int Total { get; set; }

    public string? SortBy { get; set; }
    public string? Target { get; set; }

    public int PropPerPage { get; set; } = 10;

    public PropertyListModel(List<PropertyDetailsViewModel> properties, int total, LandlordProfileModel owner = null!,
        int page = 1, string? sortBy = null, string? target = null)
    {
        Properties = properties;
        Owner = owner;
        Total = total;
        Page = page;
        SortBy = sortBy;
        Target = target;
    }
}