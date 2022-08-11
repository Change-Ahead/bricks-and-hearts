using BricksAndHearts.Database;
namespace BricksAndHearts.ViewModels;

public class LandlordListModel
{
    public List<LandlordDbModel>? LandlordList { get; set; }
    public string[] Filters { get; set; } = new string[2];
}