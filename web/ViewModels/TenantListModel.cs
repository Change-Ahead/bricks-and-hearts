using BricksAndHearts.Database;
namespace BricksAndHearts.ViewModels;

public class TenantListModel
{
    public List<TenantDbModel>? TenantList { get; set; }

    public HousingRequirementModel Filter { get; set; } = new ();

    public int Page { get; set; }
    public int Total { get; set; } 

    public string? SortBy { get; set; }

    public int TenantsPerPage { get; set; } = 10;

    public TenantListModel(List<TenantDbModel> tenantList, HousingRequirementModel filter, int total, int page = 1, string? sortBy = null)
    {
        TenantList = tenantList;
        Filter = filter;
        Total = total;
        Page = page;
        SortBy = sortBy;
    }
}