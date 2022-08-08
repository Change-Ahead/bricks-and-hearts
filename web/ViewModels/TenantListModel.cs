using System.Runtime.CompilerServices;
using BricksAndHearts.Database;
namespace BricksAndHearts.ViewModels;

public class TenantListModel
{
    public List<TenantDbModel>? TenantList { get; set; }

    public bool[] Filters { get; set; } = new bool[7];
}