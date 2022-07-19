using System.ComponentModel.DataAnnotations;
using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels;

public class PropertyViewModel
{
    [Required] public PropertyAddress Address { get; set; }

    public static PropertyViewModel FromDbModel(PropertyDbModel property)
    {
        return new PropertyViewModel
        {
            Address = new PropertyAddress
            {
                AddressLine1 = property.AddressLine1,
                AddressLine2 = property.AddressLine2,
                AddressLine3 = property.AddressLine3,
                TownOrCity = property.TownOrCity,
                County = property.County,
                Postcode = property.Postcode
            }
        };
    }
}

public class PropertyAddress
{
    public string AddressLine1 { get; set; }
    public string AddressLine2 { get; set; }
    public string AddressLine3 { get; set; }
    public string TownOrCity { get; set; }
    public string County { get; set; }
    public string Postcode { get; set; }
}