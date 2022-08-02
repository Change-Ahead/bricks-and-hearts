namespace BricksAndHearts.ViewModels;

public class AddNewPropertyViewModel
{
    public int Step { get; set; }
    public int? LandlordId { get; set; } = null;
    public PropertyViewModel? Property { get; set; }

    public static readonly int MaximumStep = 5;
}