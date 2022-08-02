namespace BricksAndHearts.ViewModels;

public class PropertiesDashboardViewModel
{
    public List<PropertyViewModel> Properties { get; set; }
    public int? LandlordId { get; set; } = null;

    public PropertiesDashboardViewModel(List<PropertyViewModel> properties, int? landlordId = null)
    {
        Properties = properties;
        LandlordId = landlordId;
    }
}