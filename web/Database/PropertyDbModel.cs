using System.ComponentModel.DataAnnotations.Schema;

namespace BricksAndHearts.Database;

[Table("Property")]
public class PropertyDbModel
{
    public int Id { get; set; }
    public int LandlordId { get; set; }
    public virtual LandlordDbModel Landlord { get; set; } = null!;
    
    public string AddressLine1 { get; set; } // Will be fixed properly by BNH-34
    public string AddressLine2 { get; set; }
    public string AddressLine3 { get; set; }
    public string TownOrCity { get; set; }
    public string County { get; set; }
    public string Postcode { get; set; }
}