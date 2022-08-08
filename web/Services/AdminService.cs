using System.Data;
using BricksAndHearts.Auth;
using BricksAndHearts.Database;
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
    public Task<List<TenantDbModel>> GetTenantDbModelsFromFilter(string[] filterArray);
    public (int[] columnOrder, (List<string> FlashTypes, List<string> FlashMessages)) CheckIfImportWorks(IFormFile csvFile);
    public Task<(List<string> FlashTypes, List<string> FlashMessages)> ImportTenants(IFormFile csvFile, int[] columnOrder, (List<string> flashTypes, List<string> flashMessages) flashResponse);
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

    public async Task<List<TenantDbModel>> GetTenantDbModelsFromFilter(string[] filterArray)
    {
        var tenantQuery = from tenants in _dbContext.Tenants select tenants;
        for (var currentFilterNo = 0; currentFilterNo < filterArray.Length; currentFilterNo++)
        {
            var filterToCheck = filterArray[currentFilterNo];
            if (filterToCheck != "all")
            {
                tenantQuery = currentFilterNo switch
                {
                    0 => filterToCheck switch
                    {
                        "Single" => tenantQuery.Where(t => t.Type == "Single"),
                        "Couple" => tenantQuery.Where(t => t.Type == "Couple"),
                        "Family" => tenantQuery.Where(t => t.Type == "Family"),
                        _ => tenantQuery
                    },
                    1 => tenantQuery.Where(t => t.HasPet == Convert.ToBoolean(filterToCheck)),
                    2 => tenantQuery.Where(t => t.ETT == Convert.ToBoolean(filterToCheck)),
                    3 => tenantQuery.Where(t => t.UniversalCredit == Convert.ToBoolean(filterToCheck)),
                    4 => tenantQuery.Where(t => t.HousingBenefits == Convert.ToBoolean(filterToCheck)),
                    5 => tenantQuery.Where(t => t.Over35 == Convert.ToBoolean(filterToCheck)),
                    _ => tenantQuery
                };
            }
        }
        return await tenantQuery.ToListAsync();
    }
    
    public (int[], (List<string>, List<string>)) CheckIfImportWorks(IFormFile csvFile)
    {
        string headerLine = "";
        using (var streamReader = new StreamReader(csvFile.OpenReadStream()))
        {
            headerLine = streamReader.ReadLine();
        }
        string[] headers = headerLine.Split(",");
        
        int[] columnOrder = new int[typeof(TenantDbModel).GetProperties().Count()];
        bool[] checkUse = new bool[headers.Count()];
        List<string> flashTypes = new List<string>(), flashMessages = new List<string>();
        
        //align database properties with csv file columns
        for (int i = 1; i < typeof(TenantDbModel).GetProperties().Count(); i++)
        {
            var dataProp = typeof(TenantDbModel).GetProperties()[i];
            for (int j = 0; j < headers.Length; j++)
            {
                if (dataProp.Name == headers[j])
                {
                    columnOrder[i] = j;
                    checkUse[j] = true;
                }
            }

            if (columnOrder[i] == null)
            {
                _logger.LogWarning($"Column {dataProp.Name} is missing.");
                flashTypes.Add("danger");
                flashMessages.Add($"Import has failed because column {dataProp.Name} is missing. Please add this column to your records before attempting to import them.");
            }
        }
        
        //check for extra columns in csv file
        for (int k = 0; k < checkUse.Length; k++)
        {
            if (!checkUse[k] && headers[k] != "")
            {
                _logger.LogWarning($"Extra column: {headers[k]}");
                flashTypes.Add("warning");
                flashMessages.Add(
                    $"The column {headers[k]} does not exist in the database. All data in this column has been ignored.");
            }
        }
        
        return (columnOrder, (flashTypes, flashMessages));
    }

    public async Task<(List<string> FlashTypes, List<string> FlashMessages)> ImportTenants(IFormFile csvFile, int[] columnOrder, (List<string> flashTypes, List<string> flashMessages) flashResponse)
    {
        List<string> flashTypes = flashResponse.flashTypes,
            flashMessages = flashResponse.flashMessages;
        var target = typeof(TenantDbModel).GetProperties()[7].PropertyType;
        await using (var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            using (var streamReader = new StreamReader(csvFile.OpenReadStream()))
            {
                string currentLine = streamReader.ReadLine();
                currentLine = streamReader.ReadLine();
                while (currentLine != null)
                {
                    string[] entries = currentLine.Split(",");
                    if (entries != null)
                    {
                        var dbModel = new TenantDbModel();
                        for(int i = 1; i < typeof(TenantDbModel).GetProperties().Count(); i++)
                        {
                            var prop = typeof(TenantDbModel).GetProperties()[i];
                            int key = columnOrder[i];
                            if (entries[key] != "")
                            {
                                //Boolean fields
                                if (prop.PropertyType == target)
                                {
                                    string boolInput = entries[key].ToUpper();
                                    if (boolInput == "TRUE"|| boolInput == "YES"|| boolInput == "1")
                                    {
                                        prop.SetValue(dbModel, true);
                                    }
                                    else if (boolInput == "FALSE"|| boolInput == "NO"|| boolInput == "0")
                                    {
                                        prop.SetValue(dbModel, false);
                                    }
                                    else
                                    {
                                        _logger.LogWarning($"Invalid input for {prop.Name} in record for {dbModel.Name}.");
                                        flashTypes.Add("danger");
                                        flashMessages.Add($"Invalid input in record for tenant {dbModel.Name}: '{prop.Name}' cannot be '{entries[key]}', as it must be a Boolean value (true/false).");
                                    }
                                }
                                
                                //String fields
                                else
                                {
                                    prop.SetValue(dbModel, entries[key]);
                                }
                            }
                        }
                        if (dbModel.Name == null)
                        {
                            _logger.LogWarning($"Name is missing from record {currentLine}.");
                            flashTypes.Add("danger");
                            flashMessages.Add($"Name is missing from record {currentLine}. This record has not been added to the database. Please add a name to this tenant in order to import their information.");
                        }
                        else
                        {
                            _dbContext.Tenants.Add(dbModel);
                        }
                        await _dbContext.SaveChangesAsync();
                    }
                    currentLine = streamReader.ReadLine();
                }
            }
            await transaction.CommitAsync();
        }
        return (flashTypes, flashMessages);
    }
}