using System.ComponentModel.DataAnnotations.Schema;

namespace BricksAndHearts.Database;

[Table("Property")]
public class PropertyDbModel
{
    // Backend
    public int Id { get; set; }
    public int LandlordId { get; set; }
    public int? RenterUserId { get; set; } = null;
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

    // Rent, deposits, and duration
    public int? Rent { get; set; }
}