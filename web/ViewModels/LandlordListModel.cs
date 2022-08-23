using BricksAndHearts.Database;
namespace BricksAndHearts.ViewModels;

public class LandlordListModel
{
    public List<LandlordDbModel>? LandlordList { get; set; }
    public bool? IsApproved { get; set; }
    public bool? IsAssigned { get; set; }
    
    public int Page { get; set; }
    public int Total { get; set; }
    public int LandlordsPerPage { get; set; } = 10;
    
    public LandlordListModel(List<LandlordDbModel> landlordList, int total, bool? isApproved, bool? isAssigned, int page = 1, int landlordsPerPage = 10)
    {
        LandlordList = landlordList;
        Total = total;
        Page = page;
        LandlordsPerPage = landlordsPerPage;
        IsApproved = isApproved;
        IsAssigned = isAssigned;
    }
}