using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels.PropertyInput;

public class PropertyInputModelStep4 : PropertyInputModelBase
{
    public PropertyInputModelStep4(PropertyDbModel property)
    {
        Description = property.Description;
    }

    public string? Description { get; set; }
}