using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels.PropertyInput;

public class PropertyInputModelStep4 : PropertyInputModelBase
{
    public string? Description { get; set; }

    public override void InitialiseViewModel(PropertyDbModel property)
    {
        base.InitialiseViewModel(property);
        Description = property.Description;
    }

    public override PropertyViewModel FormToViewModel(int propertyId, int landlordId)
    {
        var property = base.FormToViewModel(propertyId, landlordId);
        property.Description = Description;
        return property;
    }
}