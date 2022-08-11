using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels.PropertyInput;

public class PropertyInputModelStep3 : PropertyInputModelBase
{
    public override void PropertyInputModelStepInitialiser(PropertyDbModel property)
    {
        PropertyType = property.PropertyType;
        NumOfBedrooms = property.NumOfBedrooms;
    }

    public int? NumOfBedrooms { get; set; }

    public string? PropertyType { get; set; }
}