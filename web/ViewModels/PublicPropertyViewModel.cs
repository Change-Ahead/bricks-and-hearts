using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels;

public class PublicPropertyViewModel
{
    // Front end messages
    public PublicPropertySearchResult SearchResult;
    
    // Backend info
    public int? PropertyId;
    public DateTime? CreationTime { get; set; }


    // Address
    public int? LandlordId { get; set; }
    public PublicPropertyAddress Address { get; set; } = new();


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
    public HousingRequirementModel LandlordRequirements { get; set; } = new();
    
    // Rent, deposits, and duration
    [Range(0, 100000, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int? Rent { get; set; }

    public static PublicPropertyViewModel FromDbModel(PropertyDbModel property)
    {
        return new PublicPropertyViewModel
        {
            SearchResult = PublicPropertySearchResult.Success,
            PropertyId = property.Id,
            LandlordId = property.LandlordId,
            PropertyType = property.PropertyType,
            NumOfBedrooms = property.NumOfBedrooms,
            CreationTime = property.CreationTime,
            Rent = property.Rent,
            Description = property.Description,
            Address = new PublicPropertyAddress
            {
                AddressLine1 = property.AddressLine1,
                AddressLine2 = property.AddressLine2,
                AddressLine3 = property.AddressLine3,
                TownOrCity = property.TownOrCity,
                County = property.County,
                Postcode = property.Postcode,
            },
            LandlordRequirements = new HousingRequirementModel
            {
                AcceptsSingleTenant = property.AcceptsSingleTenant,
                AcceptsCouple = property.AcceptsCouple,
                AcceptsFamily = property.AcceptsFamily,
                AcceptsPets = property.AcceptsPets,
                AcceptsBenefits = property.AcceptsBenefits,
                AcceptsNotEET = property.AcceptsNotEET,
                AcceptsWithoutGuarantor = property.AcceptsWithoutGuarantor
            }
        };
    }
}

public class PublicPropertyAddress
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

public enum PublicPropertySearchResult
{
    IncorrectPublicViewLink,
    Success
}