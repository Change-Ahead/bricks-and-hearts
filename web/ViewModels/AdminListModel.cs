using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels;

public class AdminListModel
{
    public AdminListModel(List<UserDbModel> currentAdmins, List<UserDbModel> pendingAdmins)
    {
        CurrentAdmins = currentAdmins;
        PendingAdmins = pendingAdmins;
    }

    public List<UserDbModel> CurrentAdmins { get; set; }
    public List<UserDbModel> PendingAdmins { get; set; }
}