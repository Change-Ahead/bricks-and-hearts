using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BricksAndHearts.Controllers;

public class TenantController : AbstractController
{
    private readonly ITenantService _tenantService;
    private readonly IAdminService _adminService;
    private readonly ILogger<TenantController> _logger;

    public TenantController(ILogger<TenantController> logger, ITenantService tenantService, IAdminService adminService)
    {
        _logger = logger;
        _tenantService = tenantService;
        _adminService = adminService;
    }
    
}