using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    public string Postcode { get; set; } = string.Empty;
    public decimal? Lat { get; set; } // latitude
    public decimal? Lon { get; set; } // longitude

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
    public bool? AcceptsBenefits { get; set; }
    public bool? AcceptsNotEET { get; set; }
    public bool? AcceptsWithoutGuarantor { get; set; }

    // Rent, deposits, availability and duration
    public int? Rent { get; set; }

    public string Availability { get; set; } = Avail_Draft;
    public const string Avail_Draft = "Draft";
    public const string Avail_Available = "Available";
    public const string Avail_AvailableSoon = "Available Soon";
    public const string Avail_Occupied = "Occupied";
    public const string Avail_Unavailable = "Unavailable";
    
    [DataType(DataType.Date)]
    public DateTime? AvailableFrom { get; set; } = null;
    public int? RenterUserId { get; set; } = null;
}