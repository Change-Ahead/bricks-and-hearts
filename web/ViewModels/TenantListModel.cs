using BricksAndHearts.Database;
namespace BricksAndHearts.ViewModels;

public class TenantListModel
{
    public List<TenantDbModel> TenantList { get; set; }

    public HousingRequirementModel Filter { get; set; } = new ();

    public int Page { get; set; }
    public int Total { get; set; } 

    public string? TargetPostcode { get; set; }

    public int TenantsPerPage { get; set; } = 10;

    public TenantListModel(List<TenantDbModel> tenantList, HousingRequirementModel filter, int total, int page = 1, string? targetPostcode = null)
    {
        TenantList = tenantList;
        Filter = filter;
        Total = total;
        Page = page;
        TargetPostcode = targetPostcode;
    }
}