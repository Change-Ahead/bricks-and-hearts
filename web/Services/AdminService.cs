using BricksAndHearts.Auth;
using BricksAndHearts.Database;

namespace BricksAndHearts.Services;

public interface IAdminService
{
    public void RequestAdminAccess(BricksAndHeartsUser user);
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
        var userRecord = _dbContext.Users.SingleOrDefault(u => u.Id == user.Id);
        userRecord.HasRequestedAdmin = true;
        _dbContext.SaveChanges();
    }
}