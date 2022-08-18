using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels.PropertyInput;

public class PropertyInputModelDetails : PropertyInputModelBase
{
    public int? NumOfBedrooms { get; set; }

    public string? PropertyType { get; set; }

    public override void InitialiseViewModel(PropertyDbModel property)
    {
        Title = "Property Details";
        base.InitialiseViewModel(property);
        PropertyType = property.PropertyType;
        NumOfBedrooms = property.NumOfBedrooms;
        LandlordId = property.LandlordId;
    }

    public override PropertyViewModel FormToViewModel()
    {
        return new PropertyViewModel
        {
            PropertyType = PropertyType,
            NumOfBedrooms = NumOfBedrooms
        };
    }
}