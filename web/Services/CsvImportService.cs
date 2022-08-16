﻿using System.Data;
using System.Text.RegularExpressions;
using BricksAndHearts.Database;
using BricksAndHearts.ViewModels;
using LINQtoCSV;
using Microsoft.EntityFrameworkCore;

namespace BricksAndHearts.Services;

public interface ICsvImportService
{
    public (List<string> FlashTypes, List<string> FlashMessages) CheckIfImportWorks(IFormFile csvFile);
    public Task<(List<string> FlashTypes, List<string> FlashMessages)> ImportTenants(IFormFile csvFile);
    public (List<string> FlashTypes, List<string> FlashMessages) AddLatLonToTenantDb();
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
        var tenantUploadList =
            csvContext.Read<TenantUploadModel>(streamReader, csvFileDescription);

        List<string> flashTypes = new(),
            flashMessages = new(),
            postcodes = new();
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
                var addTenantResponse = AddTenantToDb(tenant, lineNo);
                flashTypes.AddRange(addTenantResponse.flashTypes);
                flashMessages.AddRange(addTenantResponse.flashMessages);
                if (addTenantResponse.postcode != "")
                {
                    postcodes.Add(addTenantResponse.postcode);
                }
            }

            await transaction.CommitAsync();
        }

        await _postcodeService.AddPostcodesToDatabaseIfAbsent(postcodes);
        return (flashTypes, flashMessages);
    }
    
    private (List<string> flashTypes, List<string> flashMessages, string postcode) AddTenantToDb(TenantUploadModel tenant, int lineNo)
    {
        List<string> flashTypes = new(), flashMessages = new();
        var postcode = "";
        if (tenant.Name == null)
        {
            _logger.LogWarning($"Name is missing from record {tenant.Email}.");
            flashTypes.Add("danger");
            flashMessages.Add($"Name is missing from record on line {lineNo} (email address provided is {tenant.Email}). This record has not been added to the database. Please add a name to this tenant in order to import their information.");
        }
        else
        {
            var dbTenant = new TenantDbModel()
            {
                Name = tenant.Name,
                Email = tenant.Email,
                Phone = tenant.Phone
            };

            if (tenant.Postcode is not null)
            {
                postcode = _postcodeService.FormatPostcode(tenant.Postcode);
                if (postcode != "")
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

            if (bool.TryParse(tenant.HasPet, out var boolHasPet) || tenant.HasPet is null)
            {
                dbTenant.HasPet = boolHasPet;
            }
            else
            {
                flashTypes.Add("danger");
                flashMessages.Add(InvalidInputMessage(tenant.Name, "HasPet", tenant.HasPet));
            }

            if (bool.TryParse(tenant.ETT, out var boolETT) || tenant.ETT is null)
            {
                dbTenant.ETT = boolETT;
            }
            else
            {
                flashTypes.Add("danger");
                flashMessages.Add(InvalidInputMessage(tenant.Name, "ETT", tenant.ETT));
            }

            if (bool.TryParse(tenant.UniversalCredit, out var boolUniversalCredit) ||
                tenant.UniversalCredit is null)
            {
                dbTenant.UniversalCredit = boolUniversalCredit;
            }
            else
            {
                flashTypes.Add("danger");
                flashMessages.Add(InvalidInputMessage(tenant.Name, "UniversalCredit", tenant.UniversalCredit));
            }

            if (bool.TryParse(tenant.HousingBenefits, out var boolHousingBenefits) ||
                tenant.HousingBenefits is null)
            {
                dbTenant.HousingBenefits = boolHousingBenefits;
            }
            else
            {
                flashTypes.Add("danger");
                flashMessages.Add(InvalidInputMessage(tenant.Name, "HousingBenefits", tenant.HousingBenefits));
            }

            if (bool.TryParse(tenant.Over35, out var boolOver35) || tenant.Over35 is null)
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
        return (flashTypes, flashMessages, postcode);
    }

    private string InvalidInputMessage(string tenantName, string propName, string propValue)
    {
        _logger.LogWarning($"Invalid input for {propName} in record for {tenantName}.");
        return $"Invalid input in record for tenant {tenantName}: '{propName}' cannot be '{propValue}' as this is the wrong data type. This input has been ignored.";

    }
    
    public (List<string> FlashTypes, List<string> FlashMessages) AddLatLonToTenantDb()
    {
        List<string> flashTypes = new(), flashMessages = new();
        var tenantsWithPostcodesToConvert =
            _dbContext.Tenants.Where(t => t.Postcode != null && t.Lat == null && t.Lon == null).ToList();
        
        foreach (var tenant in tenantsWithPostcodesToConvert)
        {
            var postcodeDbModel = _dbContext.Postcodes.FirstOrDefault(p => p.Postcode == tenant.Postcode);
            if (postcodeDbModel?.Lat == null || postcodeDbModel.Lon == null)
            {
                _logger.LogWarning($"Postcode {tenant.Postcode} is absent from Postcode table");
                flashTypes.Add("danger");
                flashMessages.Add($"Postcode {tenant.Postcode} cannot be converted to coordinates. This will affect the inclusion of tenant {tenant.Name} in the matching process.");
            }
            else
            {
                tenant.Lat = postcodeDbModel.Lat;
                tenant.Lon = postcodeDbModel.Lon;
                _dbContext.SaveChanges();
            }
        }
        return (flashTypes, flashMessages);
    }
}