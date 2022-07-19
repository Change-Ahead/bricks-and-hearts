using System.ComponentModel.DataAnnotations;
using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels;

public class PropertyViewModel
{
    [Required] public PropertyAddress Address { get; set; }

    [Required] 
    public string PropertyType { get; set; } = string.Empty;
    public int NumOfBedrooms { get; set; } = 0;
    public DateTime CreationTime { get; set; }
    public int Rent { get; set; } = 0;
    public string Description { get; set; } = String.Empty;
    
    public static PropertyViewModel FromDbModel(PropertyDbModel property)
    {
        return new PropertyViewModel
        {
            PropertyType = property.PropertyType,
            NumOfBedrooms = property.NumOfBedrooms,
            CreationTime = property.CreationTime,
            Rent = property.Rent,
            Description = property.Description,
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