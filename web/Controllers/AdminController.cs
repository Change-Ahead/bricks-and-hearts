using BricksAndHearts.Database;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BricksAndHearts.Controllers;

public class AdminController : AbstractController
{
    private readonly BricksAndHeartsDbContext _dbContext;
    private readonly IAdminService _adminService;
    private readonly ILogger<HomeController> _logger;

    public AdminController(ILogger<HomeController> logger, BricksAndHeartsDbContext dbContext, IAdminService adminService)
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
            _logger.LogWarning("User {UserId} already an admin",user.Id);
            TempData["FlashType"] = "danger";
            TempData["FlashMessage"] = "Already an admin";

            return RedirectToAction(nameof(Index));
        }

        _adminService.RequestAdminAccess(user);

        _logger.LogInformation("Successfully requested admin access for user {UserId}",user.Id);
        TempData["FlashType"] = "success";
        TempData["FlashMessage"] = "Successfully requested admin access";

        return RedirectToAction(nameof(Index));
    }

    public IActionResult CancelAdminAccessRequest()
    {
        var user = GetCurrentUser();
        if (user.IsAdmin)
        {
            _logger.LogWarning("User {UserId} already an admin",user.Id);
            TempData["FlashType"] = "danger";
            TempData["FlashMessage"] = "Already an admin";

            return RedirectToAction(nameof(Index));
        }

        _adminService.CancelAdminAccessRequest(user);

        _logger.LogInformation("Successfully cancelled admin access request for user {UserId}",user.Id);
        TempData["FlashType"] = "success";
        TempData["FlashMessage"] = "Successfully cancelled admin access request";

        return RedirectToAction(nameof(Index));
    }

    public IActionResult AdminList()
    {
        AdminListModel adminListModel = new AdminListModel();
        var adminLists = _adminService.GetAdminLists();
        adminListModel.CurrentAdmins = adminLists.CurrentAdmins;
        adminListModel.PendingAdmins = adminLists.PendingAdmins;
        return View(adminListModel);
    }
}