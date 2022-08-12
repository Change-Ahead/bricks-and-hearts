using BricksAndHearts.Database;
namespace BricksAndHearts.ViewModels;

public class TenantListModel
{
    public List<TenantDbModel>? TenantList { get; set; }

    public HousingRequirementModel Filter { get; set; } = new ();
}