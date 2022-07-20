using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels;

public class AdminListModel
{
    public List<UserDbModel> CurrentAdmins { get; set; }
    public List<UserDbModel> PendingAdmins { get; set; }
}