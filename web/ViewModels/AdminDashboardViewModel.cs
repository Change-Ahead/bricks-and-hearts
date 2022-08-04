using BricksAndHearts.Auth;

namespace BricksAndHearts.ViewModels;

public class AdminDashboardViewModel
{
    public BricksAndHeartsUser? CurrentUser { get; set; }
    public LandlordCountModel LandlordCounts { get; set; }
    public PropertyCountModel PropertyCounts { get; set; }
    public AdminDashboardViewModel(LandlordCountModel landlordCounts, PropertyCountModel propertyCounts)
    {
        LandlordCounts = landlordCounts;
        PropertyCounts = propertyCounts;
    }
}