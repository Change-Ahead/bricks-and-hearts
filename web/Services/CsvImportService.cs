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
    private readonly IPostcodeService _postcodeService;
    private readonly ILogger<CsvImportService> _logger;

    public CsvImportService(BricksAndHeartsDbContext dbContext, IPostcodeService postcodeService, ILogger<CsvImportService> logger)
    {
        _logger = logger;
        _postcodeService = postcodeService;
        _dbContext = dbContext;
    }

    public (List<string>, List<string>) CheckIfImportWorks(IFormFile csvFile)
    {
        List<string> flashTypes = new(),
            flashMessages = new();
        string headerLine;
        using (var streamReader = new StreamReader(csvFile.OpenReadStream()))
        {
            headerLine = streamReader.ReadLine() ?? "";
        }
        
        var headers = headerLine.Split(",");
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
        var tenantUploadList =
            csvContext.Read<TenantUploadModel>(streamReader, csvFileDescription)?.ToList() 
            ?? new List<TenantUploadModel>();

        List<string> flashTypes = new(),
            flashMessages = new();
        await using (var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            foreach (var tenant in _dbContext.Tenants)
            {
                _dbContext.Tenants.Remove(tenant);
            }

            var lineNo = 0;
            foreach (var tenant in tenantUploadList)
            {
                lineNo += 1;
                var addTenantResponse = await AddTenantToDb(tenant, lineNo);
                flashTypes.AddRange(addTenantResponse.flashTypes);
                flashMessages.AddRange(addTenantResponse.flashMessages);
            }

            await transaction.CommitAsync();
        }

        return (flashTypes, flashMessages);
    }
    
    private async Task<(List<string> flashTypes, List<string> flashMessages)> AddTenantToDb(TenantUploadModel tenant, int lineNo)
    {
        List<string> flashTypes = new(), flashMessages = new();
        if (tenant.Name == null)
        {
            _logger.LogWarning($"Name is missing from record {tenant.Email}.");
            flashTypes.Add("danger");
            flashMessages.Add($"Name is missing from record on line {lineNo} (email address provided is {tenant.Email}). This record has not been added to the database. Please add a name to this tenant in order to import their information.");
            return (flashTypes, flashMessages);
        }
       
        var dbTenant = new TenantDbModel
        {
            Name = tenant.Name,
            Email = tenant.Email,
            Phone = tenant.Phone
        };

        if (tenant.Postcode is not null)
        {
            var postcode = await _postcodeService.GetPostcodeAndAddIfAbsent(tenant.Postcode);
            if (postcode is not null)
            {
                dbTenant.Postcode = postcode;
            }
            else
            {
                flashTypes.Add("danger");
                flashMessages.Add(InvalidInputMessage(tenant.Name, "Postcode", tenant.Postcode!));
            }
        }

        dbTenant.Type = tenant.Type;

        dbTenant.HasPet = CheckBoolInput("HasPet", tenant.HasPet, tenant.Name, flashTypes, flashMessages);
        dbTenant.ETT = CheckBoolInput("ETT", tenant.ETT, tenant.Name, flashTypes, flashMessages);
        dbTenant.UniversalCredit = CheckBoolInput("UniversalCredit", tenant.UniversalCredit, tenant.Name, flashTypes, flashMessages);
        dbTenant.HousingBenefits = CheckBoolInput("HousingBenefits",tenant.HousingBenefits, tenant.Name, flashTypes, flashMessages);
        dbTenant.Over35 = CheckBoolInput("Over35",tenant.Over35, tenant.Name, flashTypes, flashMessages);
        
        _dbContext.Tenants.Add(dbTenant);
        await _dbContext.SaveChangesAsync();
        return (flashTypes, flashMessages);
    }
    
    private bool? CheckBoolInput(string propName, string? propValue, string tenantName, ICollection<string> flashTypes, ICollection<string> flashMessages)
    {
        if (!bool.TryParse(propValue, out var parsedInput) && propValue is not null)
        {
            flashTypes.Add("danger");
            flashMessages.Add(InvalidInputMessage(tenantName, propName, propValue));
        }

        return parsedInput;
    }
    
    private string InvalidInputMessage(string tenantName, string propName, string propValue)
    {
        _logger.LogWarning($"Invalid input for {propName} in record for {tenantName}.");
        return $"Invalid input in record for tenant {tenantName}: '{propName}' cannot be '{propValue}' as this is the wrong data type. This input has been ignored.";
    }
}