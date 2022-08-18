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
    public IEnumerable<LandlordDbModel> GetLandlordList(bool? isApproved, bool? isAssigned);

    public IEnumerable<TenantDbModel> GetTenantList(HousingRequirementModel filter);

    public Task<List<TenantDbModel>> GetNearestTenantsToProperty(PropertyViewModel currentProperty);
    
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

    public IEnumerable<LandlordDbModel> GetLandlordList(bool? isApproved, bool? isAssigned)
    {
        var landlordQuery = _dbContext.Landlords.AsQueryable();
        if (isApproved != null)
        {
            landlordQuery = landlordQuery.Where(l => l.CharterApproved == isApproved);
        }

        if (isAssigned != null)
        {
            landlordQuery = landlordQuery.Where(l => _dbContext.Users.Any(u => u.LandlordId == l.Id) == isAssigned);
        }

        return landlordQuery.AsEnumerable();
    }

    public IEnumerable<TenantDbModel> GetTenantList(HousingRequirementModel filters)
    {
        var tenants =  GetFilteredTenantQuery(filters, false);
        return tenants.AsEnumerable();
    }

    public async Task<List<TenantDbModel>> GetNearestTenantsToProperty(PropertyViewModel currentProperty)
    {
        var tenantQuery = GetFilteredTenantQuery(currentProperty.LandlordRequirements, true);
        //TODO: Currently orders by name, needs to order by distance from property. This depends on BNH-142 and others!
        tenantQuery = tenantQuery.OrderBy(t => t.Name).Take(5);
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
    
    private IQueryable<TenantDbModel> GetFilteredTenantQuery(HousingRequirementModel filters, bool isMatching)
    {
        var tenantQuery = from tenants in _dbContext.Tenants select tenants;
        tenantQuery = tenantQuery.Where(t => (t.Type == "Single" && filters.AcceptsSingleTenant == true) 
                                             || (t.Type == "Couple" && filters.AcceptsCouple == true) 
                                             || (t.Type == "Family" && filters.AcceptsFamily == true) 
                                             || (filters.AcceptsSingleTenant != true && filters.AcceptsCouple != true && filters.AcceptsFamily != true));
        /*the above are INCLUSIVE filters*/
        if (!isMatching)
        {
            return tenantQuery.Where(t => (filters.AcceptsPets == null || t.HasPet == filters.AcceptsPets) 
                                          && (filters.AcceptsNotEET == null || t.ETT == filters.AcceptsNotEET) 
                                          && (filters.AcceptsCredit == null || t.UniversalCredit == filters.AcceptsCredit) 
                                          && (filters.AcceptsBenefits == null || t.HousingBenefits == filters.AcceptsBenefits) 
                                          && (filters.AcceptsOver35 == null || t.Over35 == filters.AcceptsOver35));
            /*Above are EXCLUSIVE filters for the filters page*/
            
        }
        if (filters.AcceptsPets == false)
        {
            tenantQuery = tenantQuery.Where(t => t.HasPet == false);
        }

        if (filters.AcceptsNotEET == false)
        {
            tenantQuery = tenantQuery.Where(t => t.ETT == false);
        }

        if (filters.AcceptsCredit == false)
        {
            tenantQuery = tenantQuery.Where(t => t.UniversalCredit == false);
        }

        if (filters.AcceptsBenefits == false)
        {
            tenantQuery = tenantQuery.Where(t => t.HousingBenefits == false);
        }

        if (filters.AcceptsOver35 == false)
        {
            tenantQuery = tenantQuery.Where(t => t.Over35 == false);
        }
        return tenantQuery;
        /*Above are INCLUSIVE filters for the matching page*/
    }
}