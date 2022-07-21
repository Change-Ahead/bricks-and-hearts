using System.ComponentModel.DataAnnotations.Schema;

namespace BricksAndHearts.Database;

[Table("Property")]
public class PropertyDbModel
{
    public int Id { get; set; }
    public int LandlordId { get; set; }
    public virtual LandlordDbModel Landlord { get; set; } = null!;
    public bool IsIncomplete { get; set; }

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? AddressLine3 { get; set; }
    public string? TownOrCity { get; set; }
    public string? County { get; set; }
    public string? Postcode { get; set; }

    public string? PropertyType { get; set; }
    public int? NumOfBedrooms { get; set; }
    public DateTime? CreationTime { get; set; }
    public int? Rent { get; set; }
    public string? Description { get; set; }
}