using System.ComponentModel.DataAnnotations.Schema;

namespace BricksAndHearts.Database;

[Table("Tenant")]
public class TenantDbModel
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    
    public string? Postcode { get; set; }
    
    
    public string? Type { get; set; }
    public bool? HasPet { get; set; }
    public bool? ETT { get; set; }
    public bool? UniversalCredit { get; set; }
    public bool? HousingBenefits { get; set; }
    public bool? Over35 { get; set; }
}