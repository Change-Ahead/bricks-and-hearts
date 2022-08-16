namespace BricksAndHearts.ViewModels;

public class PropertyDetailsViewModel
{
    public PropertyViewModel? Property { get; set; }
    public List<ImageFileUrlModel> Images { get; set; } = new();
}