using System.Data;
using BricksAndHearts.Database;
using BricksAndHearts.ViewModels;
using LINQtoCSV;
using Microsoft.EntityFrameworkCore;

namespace BricksAndHearts.Services;

public interface ICsvImportService
{
    public (List<string> FlashTypes, List<string> FlashMessages) CheckIfImportWorks(IFormFile csvFile);
    public Task<(List<string> FlashTypes, List<string> FlashMessages)> ImportTenants(IFormFile csvFile);
}

public class CsvImportService : ICsvImportService
{
    private readonly BricksAndHeartsDbContext _dbContext;
    private readonly ILogger<CsvImportService> _logger;

    public CsvImportService(BricksAndHeartsDbContext dbContext, ILogger<CsvImportService> logger)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public (List<string>, List<string>) CheckIfImportWorks(IFormFile csvFile)
    {
        List<string> flashTypes = new(),
            flashMessages = new();
        var headerLine = "";
        using (var streamReader = new StreamReader(csvFile.OpenReadStream()))
        {
            headerLine = streamReader.ReadLine();
        }
        
        var headers = headerLine!.Split(",");
        var databaseHeaders = new List<string>();
        
        //check for missing columns in csv file
        foreach (var dataProp in typeof(TenantUploadModel).GetProperties())
        {
            if (!headers.Contains(dataProp.Name) && dataProp.Name != "Id")
            {
                _logger.LogWarning($"Missing column: {dataProp.Name}");
                flashTypes.Add("danger");
                flashMessages.Add(
                    $"Import has failed because column {dataProp.Name} is missing. Please add this column to your records before attempting to import them.");
            }
            databaseHeaders.Add(dataProp.Name);
        }

        //check for extra columns in csv file
        foreach (var header in headers)
        {
            if (!databaseHeaders.Contains(header))
            {
                _logger.LogWarning($"Extra column: {header}");
                flashTypes.Add("warning");
                flashMessages.Add(
                    $"The column \"{header}\" does not exist in the database. All data in this column has been ignored.");
            }
        }
        return (flashTypes, flashMessages);
    }

    public async Task<(List<string> FlashTypes, List<string> FlashMessages)> ImportTenants(IFormFile csvFile)
    {
        var csvFileDescription = new CsvFileDescription
        {
            SeparatorChar = ',',
            FirstLineHasColumnNames = true,
            IgnoreUnknownColumns = true
        };
        var csvContext = new CsvContext();
        var streamReader = new StreamReader(csvFile.OpenReadStream());
        IEnumerable<TenantUploadModel> tenantUploadList = csvContext.Read<TenantUploadModel>(streamReader, csvFileDescription);
        
        List<string> flashTypes = new(),
            flashMessages = new();
        await using (var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            foreach (var tenant in _dbContext.Tenants)
            {
                _dbContext.Tenants.Remove(tenant);
            }

            int lineNo = 0;
            foreach (TenantUploadModel tenant in tenantUploadList)
            {
                lineNo += 1;
                if (tenant.Name == null)
                {
                    _logger.LogWarning($"Name is missing from record {tenant.Email}.");
                    flashTypes.Add("danger");
                    flashMessages.Add($"Name is missing from record on line {lineNo} (email address provided is {tenant.Email}). This record has not been added to the database. Please add a name to this tenant in order to import their information.");
                }
                else
                {
                    TenantDbModel dbTenant = new TenantDbModel();
                    dbTenant.Name = tenant.Name;
                    dbTenant.Email = tenant.Email;
                    dbTenant.Phone = tenant.Phone;
                    dbTenant.Postcode = tenant.Postcode;
                    dbTenant.Type = tenant.Type;

                    if (Boolean.TryParse(tenant.HasPet, out bool boolHasPet)|| tenant.HasPet is null)
                    {
                        dbTenant.HasPet = boolHasPet;
                    }
                    else
                    {
                        flashTypes.Add("danger");
                        flashMessages.Add(InvalidInputMessage(tenant.Name, "HasPet", tenant.HasPet));
                    }
                    
                    if (Boolean.TryParse(tenant.ETT, out bool boolETT)|| tenant.ETT is null)
                    {
                        dbTenant.ETT = boolETT;
                    }
                    else
                    {
                        flashTypes.Add("danger");
                        flashMessages.Add(InvalidInputMessage(tenant.Name, "ETT", tenant.ETT));
                    }
                    
                    if (Boolean.TryParse(tenant.UniversalCredit, out bool boolUniversalCredit)|| tenant.UniversalCredit is null)
                    {
                        dbTenant.UniversalCredit = boolUniversalCredit;
                    }
                    else
                    {
                        flashTypes.Add("danger");
                        flashMessages.Add(InvalidInputMessage(tenant.Name, "UniversalCredit", tenant.UniversalCredit));
                    }
                    
                    if (Boolean.TryParse(tenant.HousingBenefits, out bool boolHousingBenefits)|| tenant.HousingBenefits is null)
                    {
                        dbTenant.HousingBenefits = boolHousingBenefits;
                    }
                    else
                    {
                        flashTypes.Add("danger");
                        flashMessages.Add(InvalidInputMessage(tenant.Name, "HousingBenefits", tenant.HousingBenefits));
                    }
                    
                    if (Boolean.TryParse(tenant.Over35, out bool boolOver35)|| tenant.Over35 is null)
                    {
                        dbTenant.Over35 = boolOver35;
                    }
                    else
                    {
                        flashTypes.Add("danger");
                        flashMessages.Add(InvalidInputMessage(tenant.Name, "Over35", tenant.Over35));
                    }

                    _dbContext.Tenants.Add(dbTenant);
                }
                _dbContext.SaveChanges();
            }
            await transaction.CommitAsync();
        }
        return (flashTypes, flashMessages);
    }

    private string InvalidInputMessage(string tenantName, string propName, string propValue)
    {
        _logger.LogWarning($"Invalid input for {propName} in record for {tenantName}.");
        return $"Invalid input in record for tenant {tenantName}: '{propName}' cannot be '{propValue}' as this is the wrong data type.";

    }
}