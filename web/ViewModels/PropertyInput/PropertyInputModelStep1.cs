using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels.PropertyInput;

public class PropertyInputModelStep1 : PropertyInputModelBase
{
    public AddressModel? Address { get; set; }

    public override void InitialiseViewModel(PropertyDbModel property)
    {
        base.InitialiseViewModel(property);
        Address = new AddressModel
        {
            AddressLine1 = property.AddressLine1,
            AddressLine2 = property.AddressLine2,
            AddressLine3 = property.AddressLine3,
            TownOrCity = property.TownOrCity,
            County = property.County,
            Postcode = property.Postcode
        };
    }

    public override PropertyViewModel FormToViewModel(int propertyId, int landlordId)
    {
        var property = base.FormToViewModel(propertyId, landlordId);
        property.Address = Address!;
        return property;
    }
}