using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BricksAndHearts.ViewModels;

namespace BricksAndHearts.Database;

[Table("Property")]
public class PropertyDbModel
{
    // Backend
    public int Id { get; set; }
    public int LandlordId { get; set; }
    public virtual LandlordDbModel Landlord { get; set; } = null!;
    public bool IsIncomplete { get; set; }
    public DateTime? CreationTime { get; set; }

    // Address line 1 and postcode are always required
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string? AddressLine3 { get; set; }
    public string? TownOrCity { get; set; }
    public string? County { get; set; }
    public string? PostcodeId { get; set; }
    public PostcodeDbModel? Postcode { get; set; }

    // Property details
    public string? PropertyType { get; set; }
    public int? NumOfBedrooms { get; set; }

    // Descriptions
    public string? Description { get; set; }

    // Tenant profile
    public bool? AcceptsSingleTenant { get; set; }
    public bool? AcceptsCouple { get; set; }
    public bool? AcceptsFamily { get; set; }
    public bool? AcceptsPets { get; set; }
    public bool? AcceptsCredit { get; set; }
    public bool? AcceptsBenefits { get; set; }
    public bool? AcceptsNotInEET { get; set; }
    public bool? AcceptsUnder35 { get; set; }
    public bool? AcceptsWithoutGuarantor { get; set; }

    // Rent, deposits, availability and duration
    public int? Rent { get; set; }

    // Availability and units
    public string Availability { get; set; } = AvailabilityState.Draft;
    public int TotalUnits { get; set; } = 1;
    public int OccupiedUnits { get; set; }

    [DataType(DataType.Date)]
    public DateTime? AvailableFrom { get; set; }

    // Tenant
    public int? RenterUserId { get; set; } = null;

    // Suffix to append to url for public view
    public string? PublicViewLink { get; set; }
}