using System.Data;
using System.Diagnostics;
using BricksAndHearts.Database;
using Microsoft.EntityFrameworkCore;

namespace BricksAndHearts.Services;

public interface ICsvImportService
{
    public (int[] columnOrder, (List<string> FlashTypes, List<string> FlashMessages)) CheckIfImportWorks(IFormFile csvFile);

    public Task<(List<string> FlashTypes, List<string> FlashMessages)> ImportTenants(IFormFile csvFile,
        int[] columnOrder, (List<string> flashTypes, List<string> flashMessages) flashResponse);
}

public class CsvImportService : ICsvImportService
{
    private readonly BricksAndHeartsDbContext _dbContext;
    private readonly ILogger<AdminService> _logger;

    public CsvImportService(BricksAndHeartsDbContext dbContext, ILogger<AdminService> logger)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public (int[], (List<string>, List<string>)) CheckIfImportWorks(IFormFile csvFile)
    {
        var headerLine = "";
        using (var streamReader = new StreamReader(csvFile.OpenReadStream()))
        {
            headerLine = streamReader.ReadLine();
        }
        var headers = headerLine!.Split(",");

        var columnOrder = Enumerable.Repeat(-1, typeof(TenantDbModel).GetProperties().Count()).ToArray();
        var checkUse = new bool[headers.Count()];
        List<string> flashTypes = new(), flashMessages = new();

        //align database properties with csv file columns
        for (var i = 1; i < typeof(TenantDbModel).GetProperties().Count(); i++)
        {
            var dataProp = typeof(TenantDbModel).GetProperties()[i];
            for (var j = 0; j < headers.Length; j++)
                if (dataProp.Name == headers[j])
                {
                    columnOrder[i] = j;
                    checkUse[j] = true;
                }

            if (columnOrder[i] == -1)
            {
                _logger.LogWarning($"Column {dataProp.Name} is missing.");
                flashTypes.Add("danger");
                flashMessages.Add(
                    $"Import has failed because column {dataProp.Name} is missing. Please add this column to your records before attempting to import them.");
            }
        }

        //check for extra columns in csv file
        for (var k = 0; k < checkUse.Length; k++)
            if (!checkUse[k] && headers[k] != "")
            {
                _logger.LogWarning($"Extra column: {headers[k]}");
                flashTypes.Add("warning");
                flashMessages.Add(
                    $"The column {headers[k]} does not exist in the database. All data in this column has been ignored.");
            }

        return (columnOrder, (flashTypes, flashMessages));
    }

    public async Task<(List<string> FlashTypes, List<string> FlashMessages)> ImportTenants(IFormFile csvFile,
        int[] columnOrder, (List<string> flashTypes, List<string> flashMessages) flashResponse)
    {
        List<string> flashTypes = flashResponse.flashTypes,
            flashMessages = flashResponse.flashMessages;
        await using (var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            using (var streamReader = new StreamReader(csvFile.OpenReadStream()))
            {
                var currentLine = streamReader.ReadLine();
                currentLine = streamReader.ReadLine();
                while (currentLine != null)
                {
                    var entries = currentLine.Split(",");
                    var dbModel = new TenantDbModel();
                    for (var i = 1; i < typeof(TenantDbModel).GetProperties().Count(); i++)
                    {
                        var prop = typeof(TenantDbModel).GetProperties()[i];
                        var key = columnOrder[i];
                        if (entries[key] != "")
                        {
                            //Boolean fields
                            if (prop.PropertyType == typeof(bool?))
                            {
                                var boolInput = entries[key].ToUpper();
                                if (boolInput == "TRUE" || boolInput == "YES" || boolInput == "1")
                                {
                                    prop.SetValue(dbModel, true);
                                }
                                else if (boolInput == "FALSE" || boolInput == "NO" || boolInput == "0")
                                {
                                    prop.SetValue(dbModel, false);
                                }
                                else
                                {
                                    _logger.LogWarning($"Invalid input for {prop.Name} in record for {dbModel.Name}.");
                                    flashTypes.Add("danger");
                                    flashMessages.Add(
                                        $"Invalid input in record for tenant {dbModel.Name}: '{prop.Name}' cannot be '{entries[key]}', as it must be a Boolean value (true/false).");
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
                        flashMessages.Add(
                            $"Name is missing from record {currentLine}. This record has not been added to the database. Please add a name to this tenant in order to import their information.");
                    }
                    else
                    {
                        _dbContext.Tenants.Add(dbModel);
                    }

                    await _dbContext.SaveChangesAsync();
                    currentLine = streamReader.ReadLine();
                }
            }

            await transaction.CommitAsync();
        }

        return (flashTypes, flashMessages);
    }
}