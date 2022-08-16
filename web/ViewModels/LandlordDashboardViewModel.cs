namespace BricksAndHearts.ViewModels;

public class LandlordDashboardViewModel
{
    public List<PropertyViewModel>? Properties { get; set; }
    public LandlordProfileModel CurrentLandlord { get; set; } = new();
}