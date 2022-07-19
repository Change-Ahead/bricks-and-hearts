using System.ComponentModel.DataAnnotations;
using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels;

public class PropertyViewModel
{
    [Required] public PropertyAddress Address { get; set; }

    [Required] 
    public string PropertyType { get; set; } = String.Empty;
    [Required]
    public int NumOfBedrooms { get; set; } = 0;
    public DateTime CreationTime { get; set; } = DateTime.Now;
    
    [Required]
    public int Rent { get; set; } = 0;
    [Required]
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
    [Required]
    public string AddressLine1 { get; set; } = String.Empty;
    public string? AddressLine2 { get; set; }
    public string? AddressLine3 { get; set; }
    [Required]
    public string TownOrCity { get; set; } = String.Empty;
    [Required]
    public string County { get; set; } = String.Empty;
    [Required]
    public string Postcode { get; set; } = String.Empty;
}