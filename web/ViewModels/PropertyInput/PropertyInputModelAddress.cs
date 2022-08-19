using System.ComponentModel.DataAnnotations;
using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels.PropertyInput;

public class PropertyInputModelAddress : PropertyInputModelBase
{
    [Required]
    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }
    public string? AddressLine3 { get; set; }

    [Required]
    public string? TownOrCity { get; set; }

    [Required]
    public string? County { get; set; }

    [Required]
    public string? Postcode { get; set; }

    public override string PreviousAction { get; set; } = "PropertyInputStepOnePostcode";

    public override void InitialiseViewModel(PropertyDbModel property)
    {
        Title = "Address";
        base.InitialiseViewModel(property);
        AddressLine1 = property.AddressLine1;
        AddressLine2 = property.AddressLine2;
        AddressLine3 = property.AddressLine3;
        TownOrCity = property.TownOrCity;
        County = property.County;
        Postcode = property.Postcode;
    }

    public override PropertyViewModel FormToViewModel()
    {
        return new PropertyViewModel
        {
            Address = new AddressModel
            {
                AddressLine1 = AddressLine1,
                AddressLine2 = AddressLine2,
                AddressLine3 = AddressLine3,
                TownOrCity = TownOrCity,
                County = County,
                Postcode = Postcode
            }
        };
    }
}