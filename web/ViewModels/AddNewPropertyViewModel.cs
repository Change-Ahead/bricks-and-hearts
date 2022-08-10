namespace BricksAndHearts.ViewModels;

public class AddNewPropertyViewModel
{
    public bool Edit { get; set; } = false;
    public int Step { get; set; }
    public PropertyViewModel? Property { get; set; }

    public static readonly int MaximumStep = 6;
}