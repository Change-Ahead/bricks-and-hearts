using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BricksAndHearts.Controllers;

public class TenantController : AbstractController
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<AdminController> _logger;

    public TenantController(ILogger<AdminController> logger, ITenantService tenantService)
    {
        _logger = logger;
        _tenantService = tenantService;
    }
    
    [Authorize(Roles = "Admin")]
    [HttpGet("SortPropertiesByLocation")]
    public async Task<IActionResult> SortTenantsByLocation(string postcode, int page = 1, int tenantsPerPage = 10)
    {
        var tenants = await _tenantService.SortTenantsByLocation(postcode, page, tenantsPerPage);

        if (tenants == null)
        {
            _logger.LogWarning($"Failed to find postcode {postcode}");
            AddFlashMessage("warning",$"Failed to sort tenants using postcode {postcode}: invalid postcode");
            return RedirectToAction("TenantList", "Admin");
        }

        _logger.LogInformation("Successfully sorted by location");
        var tenantListModel = new TenantListModel();
        tenantListModel.TenantList = tenants.ToList();
        
        return View("~/Views/Admin/TenantList.cshtml", tenantListModel);
    }
}