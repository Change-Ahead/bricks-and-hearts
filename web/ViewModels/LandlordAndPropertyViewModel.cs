namespace BricksAndHearts.ViewModels;

public class LandlordAndPropertyViewModel
{
    public PropertyViewModel? Property { get; set; }
    public List<ImageFileUrlModel> Images { get; set; } = new();
    public PropertyCountModel? PropertyTypeCount { get; set; }
    public LandlordProfileModel CurrentLandlord { get; set; } = new();
}