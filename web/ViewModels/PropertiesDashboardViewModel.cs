namespace BricksAndHearts.ViewModels;

public class PropertiesDashboardViewModel
{
    public List<PropertyViewModel> Properties { get; set; }

    public PropertiesDashboardViewModel(List<PropertyViewModel> properties)
    {
        Properties = properties;
    }
}