using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels.PropertyInput;

public class PropertyInputModelDescription : PropertyInputModelBase
{
    public string? Description { get; set; }

    public override void InitialiseViewModel(PropertyDbModel property)
    {
        Title = "Property Description";
        base.InitialiseViewModel(property);
        Description = property.Description;
    }

    public override PropertyViewModel FormToViewModel()
    {
        return new PropertyViewModel
        {
            Description = Description
        };
    }
}