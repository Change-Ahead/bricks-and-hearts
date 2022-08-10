using BricksAndHearts.Auth;
using BricksAndHearts.Database;
using BricksAndHearts.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BricksAndHearts.Services;

public interface IAdminService
{
    public void RequestAdminAccess(BricksAndHeartsUser user);
    public void CancelAdminAccessRequest(BricksAndHeartsUser user);
    public Task<(List<UserDbModel> CurrentAdmins, List<UserDbModel> PendingAdmins)> GetAdminLists();
    public Task<List<LandlordDbModel>> GetLandlordDisplayList(string approvalStatus);
    public Task<List<TenantDbModel>> GetTenantList();
    public UserDbModel? FindUserByLandlordId(int landlordId);
    public string? FindExistingInviteLink(int landlordId);
    public string CreateNewInviteLink(int landlordId);
    public void DeleteExistingInviteLink(int landlordId);
    public void ApproveAdminAccessRequest(int userId);
    public void RejectAdminAccessRequest(int userId);
    public UserDbModel GetUserFromId(int userId);
    public Task<List<TenantDbModel>> GetTenantDbModelsFromFilter(bool[] filterArray);
}

public class AdminService : IAdminService
{
    private readonly BricksAndHeartsDbContext _dbContext;
    private readonly ILogger<AdminService> _logger;

    public AdminService(BricksAndHeartsDbContext dbContext, ILogger<AdminService> logger)
    {
        _logger = logger;
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

    public async Task<(List<UserDbModel> CurrentAdmins, List<UserDbModel> PendingAdmins)> GetAdminLists()
    {
        return (await GetCurrentAdmins(), await GetPendingAdmins());
    }

    public async Task<List<LandlordDbModel>> GetLandlordDisplayList(string approvalStatus)
    {
        return approvalStatus switch
        {
            "Unapproved" => await _dbContext.Landlords.Where(u => u.CharterApproved == false).ToListAsync(),
            "Approved" => await _dbContext.Landlords.Where(u => u.CharterApproved == true).ToListAsync(),
            _ => await _dbContext.Landlords.ToListAsync(),
        };
    }
    
    public async Task<List<TenantDbModel>> GetTenantList()
    {
        return await _dbContext.Tenants.ToListAsync();
    }

    public UserDbModel? FindUserByLandlordId(int landlordId)
    {
        return _dbContext.Users.SingleOrDefault(u => u.LandlordId == landlordId);
    }

    public string? FindExistingInviteLink(int landlordId)
    {
        // Find landlord in db (assumes existence)
        var landlord = _dbContext.Landlords.Single(u => u.Id == landlordId);
        // Get existing invite link (if exists)
        return !string.IsNullOrEmpty(landlord.InviteLink) ? landlord.InviteLink : null;
    }

    public string CreateNewInviteLink(int landlordId)
    {
        // Find landlord in db (assumes existence)
        var landlord = _dbContext.Landlords.Single(u => u.Id == landlordId);
        // Should not have existing link
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
        // Find landlord in db (assumes existence)
        var landlord = _dbContext.Landlords.Single(u => u.Id == landlordId);
        // Should have existing link
        if (string.IsNullOrEmpty(landlord.InviteLink))
        {
            throw new Exception("Landlord should have existing invite link!");
        }

        // Delete
        landlord.InviteLink = null;
        _dbContext.SaveChanges();
    }

    private async Task<List<UserDbModel>> GetCurrentAdmins()
    {
        var currentAdmins = await _dbContext.Users.Where(u => u.IsAdmin == true).ToListAsync();
        return currentAdmins;
    }

    private async Task<List<UserDbModel>> GetPendingAdmins()
    {
        var pendingAdmins =
            await _dbContext.Users.Where(u => u.IsAdmin == false && u.HasRequestedAdmin).ToListAsync();
        return pendingAdmins;
    }
    
    public void ApproveAdminAccessRequest(int userId)
    {
        var userToAdmin = _dbContext.Users.Single(u => u.Id == userId);
        
        userToAdmin.IsAdmin = true;
        userToAdmin.HasRequestedAdmin = false;
        _dbContext.SaveChanges();
        _logger.LogInformation("Admin request approved");
    }
    
    public void RejectAdminAccessRequest(int userId)
    {
        var userToAdmin = _dbContext.Users.Single(u => u.Id == userId);
        
        userToAdmin.HasRequestedAdmin = false;
        _dbContext.SaveChanges();
        _logger.LogInformation("Admin request rejected");
    }

    public UserDbModel GetUserFromId(int userId)
    {
        UserDbModel userFromId = _dbContext.Users.SingleOrDefault(u => u.Id == userId)!;
        return userFromId;
    }

    public async Task<List<TenantDbModel>> GetTenantDbModelsFromFilter(bool[] filterArray)
    {
        var tenantQuery = from tenants in _dbContext.Tenants select tenants;
        for (var currentFilter = 0; currentFilter < filterArray.Length; currentFilter++)
        {
            if (filterArray[currentFilter])
            {
                tenantQuery = currentFilter switch
                {
                    0 => tenantQuery.Where(t => t.Type == "Single"),
                    1 => tenantQuery.Where(t => t.Type == "Couple"),
                    2 => tenantQuery.Where(t => t.Type == "Family"),
                    3 => tenantQuery.Where(t => t.HasPet == true),
                    4 => tenantQuery.Where(t => t.ETT == true),
                    5 => tenantQuery.Where(t => t.UniversalCredit == true),
                    6 => tenantQuery.Where(t => t.HousingBenefits == true),
                    7 => tenantQuery.Where(t => t.Over35 == true),
                    _ => tenantQuery
                };
            }
        }
        return await tenantQuery.ToListAsync();
    }
}