namespace BricksAndHearts.ViewModels;

public class PropertyDetailsViewModel
{
    public PropertyViewModel Property { get; set; } = null!;
    public List<ImageFileUrlModel> Images { get; set; } = null!;
}