using BricksAndHearts.Database;
namespace BricksAndHearts.ViewModels;

public class TenantListModel
{
    public List<TenantDbModel>? TenantList { get; set; }

    public TenantListFilter Filter { get; set; } = new TenantListFilter();
}