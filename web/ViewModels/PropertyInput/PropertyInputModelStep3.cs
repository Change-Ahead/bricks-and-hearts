using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels.PropertyInput;

public class PropertyInputModelStep3 : PropertyInputModelBase
{
    public int? NumOfBedrooms { get; set; }

    public string? PropertyType { get; set; }

    public override void InitialiseViewModel(PropertyDbModel property)
    {
        base.InitialiseViewModel(property);
        PropertyType = property.PropertyType;
        NumOfBedrooms = property.NumOfBedrooms;
        LandlordId = property.LandlordId;
    }

    public override PropertyViewModel FormToViewModel(int propertyId, int landlordId)
    {
        var property = base.FormToViewModel(propertyId, landlordId);
        property.PropertyType = PropertyType;
        property.NumOfBedrooms = NumOfBedrooms;
        return property;
    }
}