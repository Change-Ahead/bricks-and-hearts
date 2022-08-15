using BricksAndHearts.Auth;
using BricksAndHearts.Database;
using BricksAndHearts.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BricksAndHearts.Services;

public interface IAdminService
{
    public UserDbModel? GetUserFromId(int userId);

    //Admin Access
    public void RequestAdminAccess(BricksAndHeartsUser user);
    public void CancelAdminAccessRequest(BricksAndHeartsUser user);
    public void ApproveAdminAccessRequest(int userId);
    public void RejectAdminAccessRequest(int userId);
    public void RemoveAdmin(int userId);

    //Information Lists
    public Task<(List<UserDbModel> CurrentAdmins, List<UserDbModel> PendingAdmins)> GetAdminLists();
    public Task<List<LandlordDbModel>> GetLandlordList(LandlordListModel landlordListModel);

    public Task<List<TenantDbModel>> GetTenantList(HousingRequirementModel filter);

    //Invite Links
    public UserDbModel? FindUserByLandlordId(int landlordId);
    public string? FindExistingInviteLink(int landlordId);
    public string CreateNewInviteLink(int landlordId);
    public void DeleteExistingInviteLink(int landlordId);
}

public class AdminService : IAdminService
{
    private readonly BricksAndHeartsDbContext _dbContext;
    private readonly ILogger<AdminService> _logger;

    public AdminService(BricksAndHeartsDbContext dbContext, ILogger<AdminService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public UserDbModel? GetUserFromId(int userId)
    {
        UserDbModel userFromId = _dbContext.Users.SingleOrDefault(u => u.Id == userId)!;
        return userFromId;
    }

    public void RequestAdminAccess(BricksAndHeartsUser user)
    {
        var userRecord = _dbContext.Users.Single(u => u.Id == user.Id);
        userRecord.HasRequestedAdmin = true;
        _dbContext.SaveChanges();

        if (!_dbContext.Users.Any(u => u.IsAdmin))
        {
            _logger.LogInformation("Automatically approved admin request for user {userName} ({userId})", user.Name, user.Id);
            ApproveAdminAccessRequest(user.Id);
        }
    }

    public void CancelAdminAccessRequest(BricksAndHeartsUser user)
    {
        var userRecord = _dbContext.Users.Single(u => u.Id == user.Id);
        userRecord.HasRequestedAdmin = false;
        _dbContext.SaveChanges();
    }

    public void ApproveAdminAccessRequest(int userId)
    {
        var userToAdmin = _dbContext.Users.SingleOrDefault(u => u.Id == userId);
        if (userToAdmin == null)
        {
            throw new Exception($"No user found with id {userId}");
        }

        userToAdmin.IsAdmin = true;
        userToAdmin.HasRequestedAdmin = false;
        _dbContext.SaveChanges();
    }

    public void RejectAdminAccessRequest(int userId)
    {
        var userToAdmin = _dbContext.Users.SingleOrDefault(u => u.Id == userId);
        if (userToAdmin == null)
        {
            throw new Exception($"No user found with id {userId}");
        }

        userToAdmin.HasRequestedAdmin = false;
        _dbContext.SaveChanges();
    }
    
    public void RemoveAdmin(int userId)
    {
        var userToUnAdmin = _dbContext.Users.SingleOrDefault(u => u.Id == userId);
        if (userToUnAdmin == null)
        {
            throw new Exception($"No user found with id {userId}");
        }

        userToUnAdmin.IsAdmin = false;
        _dbContext.SaveChanges();
    }

    public async Task<(List<UserDbModel> CurrentAdmins, List<UserDbModel> PendingAdmins)> GetAdminLists()
    {
        return (await GetCurrentAdmins(), await GetPendingAdmins());
    }

    private async Task<List<UserDbModel>> GetCurrentAdmins()
    {
        return await _dbContext.Users.Where(u => u.IsAdmin == true).ToListAsync();
    }

    private async Task<List<UserDbModel>> GetPendingAdmins()
    {
        var pendingAdmins =
            await _dbContext.Users.Where(u => u.IsAdmin == false && u.HasRequestedAdmin).ToListAsync();
        return pendingAdmins;
    }

    public async Task<List<LandlordDbModel>> GetLandlordList(LandlordListModel landlordListModel)
    {
        var landlordQuery = _dbContext.Landlords.AsQueryable();
        if (landlordListModel.IsApproved != null)
        {
            landlordQuery = landlordQuery.Where(l => l.CharterApproved == landlordListModel.IsApproved);
        }

        if (landlordListModel.IsAssigned != null)
        {
            landlordQuery = landlordQuery.Where(l => _dbContext.Users.Any(u => u.LandlordId == l.Id) == landlordListModel.IsAssigned);
        }

        return await landlordQuery.ToListAsync();
    }

    public async Task<List<TenantDbModel>> GetTenantList(HousingRequirementModel filters)
    {
        var tenantQuery = from tenants in _dbContext.Tenants select tenants;
        var currentFilterNo = -1;
        foreach (var filter in filters)
        {
            currentFilterNo++;
            if (filter != null)
            {
                tenantQuery = currentFilterNo switch
                {
                    0 => filter switch
                    {
                        true => tenantQuery.Where(t => t.Type == "Single"),
                        false => tenantQuery.Where(t => t.Type != "Single")
                    },
                    1 => filter switch
                    {
                        true => tenantQuery.Where(t => t.Type == "Couple"),
                        false => tenantQuery.Where(t => t.Type != "Couple")
                    },
                    2 => filter switch
                    {
                        true => tenantQuery.Where(t => t.Type == "Family"),
                        false => tenantQuery.Where(t => t.Type != "Family")
                    },
                    3 => tenantQuery.Where(t => t.HasPet == filter),
                    4 => tenantQuery.Where(t => t.ETT == filter),
                    5 => tenantQuery.Where(t => t.UniversalCredit == filter),
                    6 => tenantQuery.Where(t => t.HousingBenefits == filter),
                    7 => tenantQuery.Where(t => t.Over35 == filter),
                    _ => tenantQuery
                };
            }
        }

        return await tenantQuery.ToListAsync();
    }

    public UserDbModel? FindUserByLandlordId(int landlordId)
    {
        return _dbContext.Users.SingleOrDefault(u => u.LandlordId == landlordId);
    }

    public string? FindExistingInviteLink(int landlordId)
    {
        var landlord = _dbContext.Landlords.Single(u => u.Id == landlordId);
        return !string.IsNullOrEmpty(landlord.InviteLink) ? landlord.InviteLink : null;
    }

    public string CreateNewInviteLink(int landlordId)
    {
        var landlord = _dbContext.Landlords.Single(u => u.Id == landlordId);
        if (!string.IsNullOrEmpty(landlord.InviteLink))
        {
            throw new Exception("Landlord should not have existing invite link!");
        }

        var g = Guid.NewGuid();
        var inviteLink = g.ToString();
        landlord.InviteLink = inviteLink;
        _dbContext.SaveChanges();
        return inviteLink;
    }

    public void DeleteExistingInviteLink(int landlordId)
    {
        var landlord = _dbContext.Landlords.Single(u => u.Id == landlordId);
        if (string.IsNullOrEmpty(landlord.InviteLink))
        {
            throw new Exception("Landlord should have existing invite link!");
        }

        landlord.InviteLink = null;
        _dbContext.SaveChanges();
    }
}