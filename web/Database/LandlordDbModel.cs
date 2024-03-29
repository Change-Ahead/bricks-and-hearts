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

    // Address
    public string AddressLine1 { get; set; } = null!;
    public string? AddressLine2 { get; set; }
    public string? AddressLine3 { get; set; }
    public string TownOrCity { get; set; } = null!;
    public string County { get; set; } = null!;
    public string Postcode { get; set; } = null!;

    //Type of landlord
    public string LandlordType { get; set; } = null!;

    // Charter approval
    public string? MembershipId { get; set; }
    public bool CharterApproved { get; set; }
    public DateTime? ApprovalTime { get; set; }
    public int? ApprovalAdminId { get; set; }

    public virtual UserDbModel? User { get; set; }
    public virtual List<PropertyDbModel> Properties { get; set; } = new();

    public string? InviteLink { get; set; } = null; //Nullable & initialised as null as it should be the default value
    public bool Disabled { get; set; }
    public bool IsLandlordForProfit { get; set; }
}