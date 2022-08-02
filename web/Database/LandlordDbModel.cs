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

    //Type of landlord
    public string LandlordType { get; set; } = null!;

    //Whether the landlord claims to have signed the charter
    public bool LandlordProvidedCharterStatus { get; set; } = false;
    
    public string? MembershipId { get; set; }

    //Whether the charter has been approved by an admin
    public bool CharterApproved { get; set; } = false;
    public DateTime? ApprovalTime { get; set; }
    public int? ApprovalAdminId { get; set; }

    public virtual UserDbModel? User { get; set; }
    public virtual List<PropertyDbModel> Properties { get; set; } = new();

    public string? InviteLink { get; set; } = null; //Nullable & initialised as null as it should be the default value
    
    public bool IsLandlordForProfit { get; set; }
}