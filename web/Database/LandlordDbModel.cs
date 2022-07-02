using System.ComponentModel.DataAnnotations.Schema;

namespace BricksAndHearts.Database;

[Table("Landlord")]
public class LandlordDbModel
{
    public int Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
}