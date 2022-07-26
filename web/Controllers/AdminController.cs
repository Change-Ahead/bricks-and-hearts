#region

using BricksAndHearts.Auth;
using BricksAndHearts.Database;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

#endregion

namespace BricksAndHearts.Controllers;

public class AdminController : AbstractController
{
    private readonly IAdminService _adminService;
    private readonly BricksAndHeartsDbContext _dbContext;
    private readonly ILogger<AdminController> _logger;

    public AdminController(ILogger<AdminController> logger, BricksAndHeartsDbContext dbContext,
        IAdminService adminService)
    {
        _logger = logger;
        _adminService = adminService;
        _dbContext = dbContext;
    }

    public IActionResult Index()
    {
        var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
        var viewModel = new AdminViewModel();
        if (isAuthenticated) viewModel.CurrentUser = GetCurrentUser();

        return View(viewModel);
    }

    [Authorize]
    [HttpPost]
    public IActionResult RequestAdminAccess()
    {
        var user = GetCurrentUser();
        if (user.IsAdmin)
        {
            LoggerAlreadyAdminWarning(_logger, user);

            return RedirectToAction(nameof(Index));
        }

        _adminService.RequestAdminAccess(user);

        FlashRequestSuccess(_logger, user, "requested admin access");

        return RedirectToAction(nameof(Index));
    }

    public IActionResult CancelAdminAccessRequest()
    {
        var user = GetCurrentUser();
        if (user.IsAdmin)
        {
            LoggerAlreadyAdminWarning(_logger, user);

            return RedirectToAction(nameof(Index));
        }

        _adminService.CancelAdminAccessRequest(user);

        FlashRequestSuccess(_logger, user, "cancelled admin access request");

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> AdminList()
    {
        var adminLists = await _adminService.GetAdminLists();
        var adminListModel = new AdminListModel(adminLists.CurrentAdmins, adminLists.PendingAdmins);
        return View(adminListModel);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> LandlordList()
    {
        var landlordListModel = new LandlordListModel();
        landlordListModel.UnapprovedLandlords = await _adminService.GetUnapprovedLandlords();
        return View(landlordListModel);
    }

    private void LoggerAlreadyAdminWarning(ILogger logger, BricksAndHeartsUser user)
    {
        FlashMessage(logger,
            ($"User {user.Id} already an admin",
                "danger",
                "Already an admin"));
    }
}