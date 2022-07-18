using BricksAndHearts.Database;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BricksAndHearts.Controllers;

[Authorize(Roles = "Admin")]
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

    [AllowAnonymous]
    public IActionResult Index()
    {
        var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
        var viewModel = new AdminViewModel();
        if (isAuthenticated) viewModel.CurrentUser = GetCurrentUser();

        return View(viewModel);
    }

    [AllowAnonymous]
    [HttpPost]
    public IActionResult RequestAdminAccess()
    {
        var isAuthenticated = User.Identity?.IsAuthenticated ?? false;

        if (!isAuthenticated)
        {
            _logger.LogWarning("Not authenticated user");
            TempData["FlashType"] = "danger";
            TempData["FlashMessage"] = "Not logged in";

            return RedirectToAction(nameof(Index));
        }

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


}