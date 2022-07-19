using BricksAndHearts.Auth;
using BricksAndHearts.Database;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;

namespace BricksAndHearts.Services;

public interface IAdminService
{
    public void RequestAdminAccess(BricksAndHeartsUser user);
    public void CancelAdminAccessRequest(BricksAndHeartsUser user);
    public (List<UserDbModel> CurrentAdmins, List<UserDbModel> PendingAdmins) GetAdminLists();
}

public class AdminService : IAdminService
{
    private readonly BricksAndHeartsDbContext _dbContext;

    public AdminService(BricksAndHeartsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void RequestAdminAccess(BricksAndHeartsUser user)
    {
        var userRecord = _dbContext.Users.Single(u => u.Id == user.Id);
        userRecord.HasRequestedAdmin = true;
        _dbContext.SaveChanges();
    }

    public void CancelAdminAccessRequest(BricksAndHeartsUser user)
    {
        var userRecord = _dbContext.Users.Single(u => u.Id == user.Id);
        userRecord.HasRequestedAdmin = false;
        _dbContext.SaveChanges();
    }

    private async Task<List<UserDbModel>> GetCurrentAdmins()
    {
        List<UserDbModel> CurrentAdmins = await _dbContext.Users.Where(u => u.IsAdmin == true).ToListAsync();
        return CurrentAdmins;
    }

    private async Task<List<UserDbModel>> GetPendingAdmins()
    {
        List<UserDbModel> PendingAdmins = await _dbContext.Users.Where(u => u.IsAdmin == false && u.HasRequestedAdmin).ToListAsync();
        return PendingAdmins;
    }

    public async Task<(List<UserDbModel> CurrentAdmins, List<UserDbModel> PendingAdmins)> GetAdminLists()
    {
        return (await GetCurrentAdmins(), await GetPendingAdmins());
    }
}