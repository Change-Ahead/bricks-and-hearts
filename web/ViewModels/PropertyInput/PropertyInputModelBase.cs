using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels.PropertyInput;

public abstract class PropertyInputModelBase
{
    public static readonly int MaximumStep = 6;
    public int LandlordId { get; set; }
    public int PropertyId { get; set; }
    public bool IsEdit { get; set; }
    public int Step { get; set; }

    public virtual void InitialiseViewModel(PropertyDbModel property)
    {
        PropertyId = property.Id;
        LandlordId = property.LandlordId;
    }

    public virtual PropertyViewModel FormToViewModel(int propertyId, int landlordId)
    {
        var property = new PropertyViewModel();
        property.PropertyId = propertyId;
        property.LandlordId = landlordId;
        return property;
    }
}