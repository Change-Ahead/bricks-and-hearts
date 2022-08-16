namespace BricksAndHearts.ViewModels;

public class LandlordDashboardViewModel
{
    public List<PropertyDetailsViewModel>? Properties { get; set; }
    public LandlordProfileModel CurrentLandlord { get; set; } = new();
}