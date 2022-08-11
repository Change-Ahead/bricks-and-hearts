using BricksAndHearts.Database;
namespace BricksAndHearts.ViewModels;

public class LandlordListModel
{
    public List<LandlordDbModel>? LandlordList { get; set; }
    public bool? Approved { get; set; }
    public bool? Assigned { get; set; }
}