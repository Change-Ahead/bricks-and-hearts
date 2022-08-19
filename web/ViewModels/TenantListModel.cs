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

    public TenantListModel()
    {
        TenantList = new List<TenantDbModel>();
        Filter = new HousingRequirementModel();
        Total = 0;
        Page = 1;
        TargetPostcode = null;
    }

    public TenantListModel(List<TenantDbModel> tenantList, HousingRequirementModel filter, int total, int page = 1, string? targetPostcode = null)
    {
        TenantList = tenantList;
        Filter = filter;
        Total = total;
        Page = page;
        TargetPostcode = targetPostcode;
    }
    /*public RouteValueDictionary ToRouteValueDictionary(int pageChange)
    {
        return new RouteValueDictionary
        {
            { "AcceptsSingleTenant", Filter.AcceptsSingleTenant },
            { "AcceptsCouple", Filter.AcceptsCouple },
            { "AcceptsFamily", Filter.AcceptsFamily },
            { "AcceptsPets", Filter.AcceptsPets },
            { "AcceptsNotEET", Filter.AcceptsNotEET },
            { "AcceptsCredit", Filter.AcceptsCredit },
            { "AcceptsBenefits", Filter.AcceptsBenefits },
            { "AcceptsOver35", Filter.AcceptsOver35 },
            { "Page", Page + pageChange },
            { "TargetPostcode", TargetPostcode }
        };
    }

    public TenantListModel FromRouteValueDictionary(RouteValueDictionary routeValueDictionary)
    {
        return new TenantListModel
        {
            Filter = new HousingRequirementModel
            {
                AcceptsSingleTenant = (bool?)routeValueDictionary["AcceptsSingleTenant"],
                AcceptsCouple = (bool?)routeValueDictionary["AcceptsCouple"],
                AcceptsFamily = (bool?)routeValueDictionary["AcceptsFamily"],
                AcceptsPets = (bool?)routeValueDictionary["AcceptsPets"],
                AcceptsNotEET = (bool?)routeValueDictionary["AcceptsNotEET"],
                AcceptsCredit = (bool?)routeValueDictionary["AcceptsCredit"],
                AcceptsBenefits = (bool?)routeValueDictionary["AcceptsBenefits"],
                AcceptsOver35 = (bool?)routeValueDictionary["AcceptsOver35"]   
            },
            Page = (int)(routeValueDictionary["Page"] ?? 1),
            TargetPostcode = (string?)routeValueDictionary["TargetPostcode"]
        };
    }*/
    
}