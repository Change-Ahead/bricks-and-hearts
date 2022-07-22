using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels;

public class PropertyViewModel
{
    [Required] public PropertyAddress? Address { get; set; }

    [Required] [StringLength(10000)] public string? PropertyType { get; set; } = string.Empty;

    [Required]
    [Range(0, 1000, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    [DisplayName("Number of Bedrooms")]
    public int? NumOfBedrooms { get; set; }

    public DateTime? CreationTime { get; set; } = DateTime.Now;

    [Required]
    [Range(0, 100000, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int? Rent { get; set; }

    [Required] [StringLength(20000)] public string? Description { get; set; } = string.Empty;

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
                AddressLine1 = property.AddressLine1!,
                AddressLine2 = property.AddressLine2,
                AddressLine3 = property.AddressLine3,
                TownOrCity = property.TownOrCity!,
                County = property.County!,
                Postcode = property.Postcode!
            }
        };
    }
}

public class PropertyAddress
{
    [Required] [StringLength(10000)] public string AddressLine1 { get; set; } = string.Empty;

    [StringLength(10000)] public string? AddressLine2 { get; set; }

    [StringLength(10000)] public string? AddressLine3 { get; set; }

    [Required] [StringLength(10000)] public string TownOrCity { get; set; } = string.Empty;

    [Required] [StringLength(10000)] public string County { get; set; } = string.Empty;

    [Required] [StringLength(100)] public string Postcode { get; set; } = string.Empty;
}