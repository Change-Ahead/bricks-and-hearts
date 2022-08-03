using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels;

public class PropertyViewModel : IValidatableObject
{
    // Backend info
    public int PropertyId;
    public DateTime? CreationTime { get; set; }


    // Address
    public PropertyAddress Address { get; set; } = new();


    // Property details
    [StringLength(10000)]
    public string? PropertyType { get; set; }

    [Range(0, 1000, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    [DisplayName("Number of Bedrooms")]
    public int? NumOfBedrooms { get; set; }


    // Descriptions
    [StringLength(20000)]
    public string? Description { get; set; }


    // Tenant profile
    public bool? AcceptsSingleTenant { get; set; }
    public bool? AcceptsCouple { get; set; }
    public bool? AcceptsFamily { get; set; }
    public bool? AcceptsPets { get; set; }
    public bool? AcceptsBenefits { get; set; }
    public bool? AcceptsNotEET { get; set; }
    public bool? AcceptsWithoutGuarantor { get; set; }


    // Rent, deposits, and duration
    [Range(0, 100000, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int? Rent { get; set; }


    public static PropertyViewModel FromDbModel(PropertyDbModel property)
    {
        return new PropertyViewModel
        {
            PropertyId = property.Id,
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
            },
            AcceptsSingleTenant = property.AcceptsSingleTenant,
            AcceptsCouple = property.AcceptsCouple,
            AcceptsFamily = property.AcceptsFamily,
            AcceptsPets = property.AcceptsPets,
            AcceptsBenefits = property.AcceptsBenefits,
            AcceptsNotEET = property.AcceptsNotEET,
            AcceptsWithoutGuarantor = property.AcceptsWithoutGuarantor
        };
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AcceptsSingleTenant == false && AcceptsCouple == false && AcceptsFamily == false)
        {
            yield return new ValidationResult("At least one type of tenant must be selected");
        }
    }
}

public class PropertyAddress
{
    [StringLength(10000)]
    public string? AddressLine1 { get; set; }

    [StringLength(10000)]
    public string? AddressLine2 { get; set; }

    [StringLength(10000)]
    public string? AddressLine3 { get; set; }

    [StringLength(10000)]
    public string? TownOrCity { get; set; }

    [StringLength(10000)]
    public string? County { get; set; }

    [RegularExpression(
        @"([Gg][Ii][Rr] 0[Aa]{2})|((([A-Za-z][0-9]{1,2})|(([A-Za-z][A-Ha-hJ-Yj-y][0-9]{1,2})|(([A-Za-z][0-9][A-Za-z])|([A-Za-z][A-Ha-hJ-Yj-y][0-9][A-Za-z]?))))\s?[0-9][A-Za-z]{2})",
        ErrorMessage = "Please enter a valid postcode")]
    public string? Postcode { get; set; }
}