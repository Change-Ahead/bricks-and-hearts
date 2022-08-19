using System.ComponentModel.DataAnnotations;
using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels.PropertyInput;

public class PropertyInputModelDetails : PropertyInputModelBase
{
    [Required]
    public int? NumOfBedrooms { get; set; }

    [Required]
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