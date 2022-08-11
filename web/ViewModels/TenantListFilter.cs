namespace BricksAndHearts.ViewModels;

public class TenantListFilter
{
    public string Type { get; set; } = "";
    public bool? HasPet { get; set; }
    public bool? ETT { get; set; }
    public bool? UniversalCredit { get; set; }
    public bool? HousingBenefits { get; set; }
    public bool? Over35 { get; set; }
}