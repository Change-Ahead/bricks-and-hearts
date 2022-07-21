using System.ComponentModel.DataAnnotations.Schema;

namespace BricksAndHearts.Database;

[Table("Landlord")]
public class LandlordDbModel
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? CompanyName { get; set; }
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;

    public string LandlordStatus { get; set; } = null!;
    public bool LandlordProvidedCharterStatus { get; set; } = false;
    public bool CharterApproved { get; set; } = false;

    public virtual UserDbModel? User { get; set; }
    public virtual List<PropertyDbModel>? Properties { get; set; }
}