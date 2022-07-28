using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels;

public class PropertyViewModel
{
    public int PropertyId;
    public PropertyAddress Address { get; set; } = new();

    [StringLength(10000)] public string? PropertyType { get; set; }

    [Range(0, 1000, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    [DisplayName("Number of Bedrooms")]
    public int? NumOfBedrooms { get; set; }

    public DateTime? CreationTime { get; set; }

    [Range(0, 100000, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int? Rent { get; set; }

    [StringLength(20000)] public string? Description { get; set; }

    public static PropertyViewModel FromDbModel(PropertyDbModel property)
    {
        return new PropertyViewModel
        {
            PropertyId =  property.Id,
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
    [StringLength(10000)] public string? AddressLine1 { get; set; }

    [StringLength(10000)] public string? AddressLine2 { get; set; }

    [StringLength(10000)] public string? AddressLine3 { get; set; }

    [StringLength(10000)] public string? TownOrCity { get; set; }

    [StringLength(10000)] public string? County { get; set; }

    [RegularExpression(@"([Gg][Ii][Rr] 0[Aa]{2})|((([A-Za-z][0-9]{1,2})|(([A-Za-z][A-Ha-hJ-Yj-y][0-9]{1,2})|(([A-Za-z][0-9][A-Za-z])|([A-Za-z][A-Ha-hJ-Yj-y][0-9][A-Za-z]?))))\s?[0-9][A-Za-z]{2})",
    ErrorMessage="Please enter a valid postcode")]
    public string? Postcode { get; set; }
}