namespace BricksAndHearts.ViewModels;

public class AddNewPropertyViewModel
{
    public int Step { get; set; }
    public PropertyViewModel? Property { get; set; }

    public static readonly int MaximumStep = 6;
}