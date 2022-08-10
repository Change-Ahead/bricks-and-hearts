using System.Data;
using BricksAndHearts.Database;
using BricksAndHearts.ViewModels;
using LINQtoCSV;
using Microsoft.EntityFrameworkCore;

namespace BricksAndHearts.Services;

public interface ICsvImportService
{
    public (List<string> FlashTypes, List<string> FlashMessages) CheckIfImportWorks(IFormFile csvFile);
    public Task<(List<string> FlashTypes, List<string> FlashMessages)> ImportTenants(IFormFile csvFile, (List<string> flashTypes, List<string> flashMessages) flashResponse);
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

    public async Task<(List<string> FlashTypes, List<string> FlashMessages)> ImportTenants(IFormFile csvFile, (List<string> flashTypes, List<string> flashMessages) flashResponse)
    {
        CsvFileDescription csvFileDescription = new CsvFileDescription
        {
            SeparatorChar = ',',
            FirstLineHasColumnNames = true,
            IgnoreUnknownColumns = true
        };
        CsvContext csvContext = new CsvContext();
        StreamReader streamReader = new StreamReader(csvFile.OpenReadStream());
        IEnumerable<TenantUploadModel> list = csvContext.Read<TenantUploadModel>(streamReader, csvFileDescription);
        
        List<string> flashTypes = flashResponse.flashTypes, flashMessages = flashResponse.flashMessages;
        await using (var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            foreach (var tenant in _dbContext.Tenants)
            {
                _dbContext.Tenants.Remove(tenant);
            }

            int lineNo = 0;
            foreach (TenantUploadModel tenant in list)
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
                    foreach (var uploadProp in typeof(TenantUploadModel).GetProperties())
                    {
                        if (uploadProp.GetValue(tenant) is not null)
                        {
                            var dataProp = typeof(TenantDbModel).GetProperty(uploadProp.Name);
                            if (dataProp!.PropertyType == typeof(bool?))
                            {
                                bool isBool = bool.TryParse(uploadProp.GetValue(tenant)!.ToString(), out bool boolData);
                                if (isBool)
                                {
                                    dataProp.SetValue(dbTenant, boolData);
                                }
                                else
                                {
                                    _logger.LogWarning(
                                        $"Invalid input for {uploadProp.Name} in record for {tenant.Name}.");
                                    flashTypes.Add("danger");
                                    flashMessages.Add(
                                        $"Invalid input in record for tenant {tenant.Name}: '{uploadProp.Name}' cannot be '{uploadProp.GetValue(tenant)!.ToString()}' as this is the wrong data type.");
                                }
                            }
                            else
                            {
                                dataProp.SetValue(dbTenant, uploadProp.GetValue(tenant));
                            }
                        }
                    }
                    _dbContext.Tenants.Add(dbTenant);
                }
                _dbContext.SaveChanges();
            }
            await transaction.CommitAsync();
        }
        return (flashTypes, flashMessages);
    }
}