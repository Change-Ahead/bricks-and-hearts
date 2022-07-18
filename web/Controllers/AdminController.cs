using BricksAndHearts.Database;
using BricksAndHearts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BricksAndHearts.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : AbstractController
{
    private readonly BricksAndHeartsDbContext _dbContext;
    private readonly ILogger<HomeController> _logger;

    public AdminController(ILogger<HomeController> logger, BricksAndHeartsDbContext dbContext)
    {
        _logger = logger;
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
    
    [HttpPost]
    public IActionResult RequestAdminAccess()
    {
        return View("Index");
    }


}