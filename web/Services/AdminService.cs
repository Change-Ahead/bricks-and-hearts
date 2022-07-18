using BricksAndHearts.Auth;
using BricksAndHearts.Database;

namespace BricksAndHearts.Services;

public interface IAdminService
{
    public enum RequestAdminAccessResult
    {
        ErrorUserNotFound,
        Success
    }
    
    public RequestAdminAccessResult RequestAdminAccess(BricksAndHeartsUser user);
}

public class AdminService : IAdminService
{
    private readonly BricksAndHeartsDbContext _dbContext;

    public AdminService(BricksAndHeartsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IAdminService.RequestAdminAccessResult RequestAdminAccess(BricksAndHeartsUser user)
    {
        var userRecord = _dbContext.Users.SingleOrDefault(u => u.Id == user.Id);
        if (userRecord == null) return IAdminService.RequestAdminAccessResult.ErrorUserNotFound;
        userRecord.HasRequestedAdmin = true;
        _dbContext.SaveChanges();
        return IAdminService.RequestAdminAccessResult.Success;
    }
}