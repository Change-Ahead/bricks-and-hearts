using System.ComponentModel.DataAnnotations.Schema;

namespace BricksAndHearts.Database;

[Table("Tenant")]
public class TenantDbModel
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }

    public PostcodeDbModel? Postcode { get; set; }
    public string? PostcodeId { get; set; }

    public string? Type { get; set; }
    public bool? HasPet { get; set; }
    public bool? InEET { get; set; }
    public bool? UniversalCredit { get; set; }
    public bool? HousingBenefits { get; set; }
    public bool? Under35 { get; set; }
    public bool? HasGuarantor { get; set; }
}