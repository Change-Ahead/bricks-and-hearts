using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels;

public class TenantListModel
{
    public List<TenantDbModel> TenantList { get; set; }
    public HousingRequirementModel Filter { get; set; }
    public int Page { get; set; }
    public int Total { get; set; }
    public string? TargetPostcode { get; set; }
    public int TenantsPerPage { get; set; } = 10;
    public RouteValueDictionary PreviousPageRouteValues { get; set; } = new();
    public RouteValueDictionary NextPageRouteValues { get; set; } = new();

    private RouteValueDictionary GetRouteValueDictionary(int pageChange)
    {
        var possibleRouteValues = new RouteValueDictionary
        {
            { nameof(Filter.AcceptsSingleTenant), Filter.AcceptsSingleTenant },
            { nameof(Filter.AcceptsCouple), Filter.AcceptsCouple },
            { nameof(Filter.AcceptsFamily), Filter.AcceptsFamily },
            { nameof(Filter.AcceptsPets), Filter.AcceptsPets },
            { nameof(Filter.AcceptsNotInEET), Filter.AcceptsNotInEET },
            { nameof(Filter.AcceptsCredit), Filter.AcceptsCredit },
            { nameof(Filter.AcceptsBenefits), Filter.AcceptsBenefits },
            { nameof(Filter.AcceptsUnder35), Filter.AcceptsUnder35 },
            { nameof(Page), Page + pageChange },
            { nameof(TargetPostcode), TargetPostcode }
        };
        return new RouteValueDictionary(possibleRouteValues.Where(entry => entry.Value != null));
    }
    
    public TenantListModel()
    {
        TenantList = new List<TenantDbModel>();
        Filter = new HousingRequirementModel();
        Total = 0;
        Page = 1;
        TargetPostcode = null;
    }

    public TenantListModel(List<TenantDbModel> tenantList, HousingRequirementModel filter, int total, int page = 1,
        string? targetPostcode = null)
    {
        TenantList = tenantList;
        Filter = filter;
        Total = total;
        Page = page;
        TargetPostcode = targetPostcode;

        PreviousPageRouteValues = GetRouteValueDictionary(-1);
        NextPageRouteValues = GetRouteValueDictionary(1);
    }
}