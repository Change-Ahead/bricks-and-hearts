using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels;

public class PropertyViewModel : IValidatableObject
{
    // Backend info
    public int PropertyId;
    public int LandlordId { get; set; }
    public DateTime? CreationTime { get; set; }


    // Location
    public PropertyAddress Address { get; set; } = new();
    public decimal? Lat { get; set; }
    public decimal? Lon { get; set; }


    // Property details
    [StringLength(10000)]
    public string? PropertyType { get; set; }

    [Range(0, 1000, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    [DisplayName("Number of bedrooms")]
    public int? NumOfBedrooms { get; set; }

    public bool IsIncomplete { get; set; }

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

// Availability and units
    public string? Availability { get; set; }

    [DataType(DataType.Date)]
    public DateTime? AvailableFrom { get; set; }

    [Range(1, 10000, ErrorMessage = "Total units for a property must be between {1} and {2}.")]
    public int? TotalUnits { get; set; }

    public int? OccupiedUnits { get; set; }


    // Tenant
    public int? UserWhoRented { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AcceptsSingleTenant == false && AcceptsCouple == false && AcceptsFamily == false)
        {
            yield return new ValidationResult("At least one type of tenant must be selected");
        }

        if (Availability == AvailabilityState.AvailableSoon && AvailableFrom == null)
        {
            yield return new ValidationResult("Available From must be provided if property is Available Soon");
        }

        if (OccupiedUnits > TotalUnits)
        {
            yield return new ValidationResult(
                "The number of occupied units must be less than or equal to the total units at the property.");
        }
    }

    public int? AvailableUnits => TotalUnits - OccupiedUnits;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AcceptsSingleTenant == false && AcceptsCouple == false && AcceptsFamily == false)
        {
            yield return new ValidationResult("At least one type of tenant must be selected");
        }

        if (Availability == PropertyDbModel.Avail_AvailableSoon && AvailableFrom == null)
        {
            yield return new ValidationResult("Available From must be provided if property is Available Soon");
        }
    }

    public static PropertyViewModel FromDbModel(PropertyDbModel property)
    {
        return new PropertyViewModel
        {
            PropertyId = property.Id,
            LandlordId = property.LandlordId,
            PropertyType = property.PropertyType,
            NumOfBedrooms = property.NumOfBedrooms,
            CreationTime = property.CreationTime,
            Rent = property.Rent,
            UserWhoRented = property.RenterUserId,
            IsIncomplete = property.IsIncomplete,
            Description = property.Description,
            Lat = property.Lat,
            Lon = property.Lon,
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
            AcceptsWithoutGuarantor = property.AcceptsWithoutGuarantor,
            Availability = property.Availability,
            AvailableFrom = property.AvailableFrom
            UserWhoRented = property.RenterUserId,
            TotalUnits = property.TotalUnits,
            OccupiedUnits = property.OccupiedUnits
        };
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