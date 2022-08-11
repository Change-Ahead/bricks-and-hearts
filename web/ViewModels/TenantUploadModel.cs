namespace BricksAndHearts.ViewModels;

public class TenantUploadModel
{
    public string? Name { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Postcode { get; set; }
    
    public string? Type { get; set; }
    public string? HasPet { get; set; }
    public string? ETT { get; set; }
    public string? UniversalCredit { get; set; }
    public string? HousingBenefits { get; set; }
    public string? Over35 { get; set; }
}