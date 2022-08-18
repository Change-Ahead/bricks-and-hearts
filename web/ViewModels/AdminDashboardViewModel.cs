using BricksAndHearts.Auth;

namespace BricksAndHearts.ViewModels;

public class AdminDashboardViewModel
{
    public BricksAndHeartsUser? CurrentUser { get; set; }
    public LandlordCountModel LandlordCounts { get; set; }
    public PropertyCountModel PropertyCounts { get; set; }
    public TenantCountModel TenantCounts { get; set; }
    
    public AdminDashboardViewModel(LandlordCountModel landlordCounts, PropertyCountModel propertyCounts, TenantCountModel tenantCounts)
    {
        LandlordCounts = landlordCounts;
        PropertyCounts = propertyCounts;
        TenantCounts = tenantCounts;
    }
}