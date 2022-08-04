namespace BricksAndHearts.ViewModels;

public class PropertiesDashboardViewModel
{
    public PropertiesDashboardViewModel(List<PropertyViewModel> properties, LandlordProfileModel owner)
    {
        Properties = properties;
        Owner = owner;
    }

    public LandlordProfileModel Owner { get; set; }
    public List<PropertyViewModel> Properties { get; set; }
}