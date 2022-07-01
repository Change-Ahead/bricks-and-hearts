using BricksAndHearts.Auth;
using BricksAndHearts.Database;
using BricksAndHearts.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BricksAndHearts.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly BricksAndHeartsDbContext _dbContext;
    private readonly ILogger<HomeController> _logger;

    public AdminController(ILogger<HomeController> logger, BricksAndHeartsDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    private BricksAndHeartsUser GetCurrentUser()
    {
        if (User.Identity?.IsAuthenticated != true) throw new Exception("GetCurrentUser called when not authenticated");

        return (BricksAndHeartsUser)User.Identity;
    }

    [AllowAnonymous]
    public IActionResult Index()
    {
        var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
        var viewModel = new AdminViewModel();
        if (isAuthenticated) viewModel.CurrentUser = GetCurrentUser();

        return View(viewModel);
    }
}