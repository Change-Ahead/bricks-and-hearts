using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels.PropertyInput;

public class PropertyInputModelStep4 : PropertyInputModelBase
{
    public override void PropertyInputModelStepInitialiser(PropertyDbModel property)
    {
        Description = property.Description;
    }

    public string? Description { get; set; }
}