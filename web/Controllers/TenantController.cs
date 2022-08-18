using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BricksAndHearts.Controllers;

public class TenantController : AbstractController
{
    private readonly ILogger<TenantController> _logger;
    private readonly ITenantService _tenantService;
    private readonly IPropertyService _propertyService;
    private readonly IMailService _mailService;
    private readonly ICsvImportService _csvImportService;

    public TenantController(ILogger<TenantController> logger, ITenantService tenantService, IPropertyService propertyService, IMailService mailService, ICsvImportService csvImportService)
    {
        _logger = logger;
        _tenantService = tenantService;
        _propertyService = propertyService;
        _mailService = mailService;
        _csvImportService = csvImportService;
    }
    
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> TenantList(HousingRequirementModel filter, string? targetPostcode, int page = 1, int tenantsPerPage = 10)
    {
        if (targetPostcode != null)
        {
            var tenantsByLocation = await _tenantService.SortTenantsByLocation(targetPostcode, page, tenantsPerPage);

            if (tenantsByLocation.Count > 0)
            {
                _logger.LogInformation("Successfully sorted by location");
                return View(new TenantListModel(tenantsByLocation.TenantList, new HousingRequirementModel(), tenantsByLocation.Count, page, targetPostcode));
            }

            _logger.LogWarning($"Failed to find postcode {targetPostcode}");
            AddFlashMessage("warning",$"Failed to sort tenants using postcode {targetPostcode}: invalid postcode");//TODO make this message actually display
        }
        
        var tenantsByFilter = await _adminService.GetTenantList(filter, page, tenantsPerPage);
        return View(new TenantListModel(tenantsByFilter.TenantList, filter, tenantsByFilter.Count, page, targetPostcode));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{currentPropertyId:int}/Matches")]
    public async Task<IActionResult> TenantMatchList(int currentPropertyId)
    {
        var currentProperty = PropertyViewModel.FromDbModel(_propertyService.GetPropertyByPropertyId(currentPropertyId)!);
        
        var tenantMatchListModel = new TenantMatchListModel{
            TenantList = await _adminService.GetNearestTenantsToProperty(currentProperty),
            CurrentProperty = currentProperty
        };
        return View("TenantMatchList", tenantMatchListModel);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{currentPropertyId:int}/Matches")]
    public ActionResult SendMatchLinkEmail(string propertyLink, string tenantEmail, int currentPropertyId)
    {
        var addressToSendTo = new List<string> { tenantEmail };
        _mailService.TrySendMsgInBackground(propertyLink, tenantEmail, addressToSendTo);
        _logger.LogInformation($"Successfully emailed tenant {tenantEmail}");
        AddFlashMessage("success", $"successfully emailed {tenantEmail}");
        return RedirectToAction("TenantMatchList", new {currentPropertyId});
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult> ImportTenants(IFormFile? csvFile)
    {
        if (csvFile == null)
        {
            return RedirectToAction(nameof(TenantList));
        }
        var fileName = csvFile.FileName;
        if (csvFile.Length == 0)
        {
            _logger.LogWarning($"{fileName} has length zero.");
            AddFlashMessage("danger", $"{fileName} contains no data. Please upload a file containing the tenant data you would like to import.");
            return RedirectToAction(nameof(TenantList));
        }
        if (fileName.Substring(fileName.Length - 3) != "csv")
        {
            _logger.LogWarning($"{fileName} not a CSV file.");
            AddFlashMessage("danger", $"{fileName} is not a CSV file. Please upload your data as a CSV file.");
            return RedirectToAction(nameof(TenantList));
        }
        
        var flashResponse = _csvImportService.CheckIfImportWorks(csvFile);
        for (var i = 0; i < flashResponse.FlashMessages.Count; i++)
        {
            AddFlashMessage(flashResponse.FlashTypes[i], flashResponse.FlashMessages[i]);
        }
        
        if (flashResponse.FlashTypes.Contains("danger"))
        {
            return RedirectToAction(nameof(TenantList));
        }
        
        flashResponse = await _csvImportService.ImportTenants(csvFile);
        for (var i = 0; i < flashResponse.FlashMessages.Count; i++)
        {
            AddFlashMessage(flashResponse.FlashTypes[i],  flashResponse.FlashMessages[i]);
        }
        
        if (flashResponse.FlashMessages.Count == 0)
        {
            _logger.LogInformation("Successfuly imported all tenant data.");
            AddFlashMessage("success", "Successfully imported all tenant data.");
        }
        
        return RedirectToAction(nameof(TenantList));
    }
}