using System.ComponentModel.DataAnnotations.Schema;

namespace BricksAndHearts.Database;

[Table("User")]
public class UserDbModel
{
    public int Id { get; set; }

    public string GoogleAccountId { get; set; } = null!;

    public string GoogleUserName { get; set; } = null!;

    public string GoogleEmail { get; set; } = null!;

    public bool IsAdmin { get; set; }

    public int? LandlordId { get; set; }
    public virtual LandlordDbModel? Landlord { get; set; }
    
    public bool HasRequestedAdmin { get; set; }
}