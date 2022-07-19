using System.ComponentModel.DataAnnotations.Schema;

namespace BricksAndHearts.Database;

[Table("Property")]
public class PropertyDbModel
{
    public int Id { get; set; }
    public int LandlordId { get; set; }
    public virtual LandlordDbModel Landlord { get; set; }
    public string Address { get; set; }
}