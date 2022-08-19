using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels;

public class TenantMatchListModel
{
    public List<TenantDbModel> TenantList { get; set; } = new();

    public LandlordAndPropertyViewModel LandlordAndProperty { get; set; } = new();
}