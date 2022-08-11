using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels.PropertyInput;

public class PropertyInputModelStep2 : PropertyInputModelBase
{
    public PropertyInputModelStep2(PropertyDbModel property)
    {
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

    public AddressModel Address { get; set; }
}