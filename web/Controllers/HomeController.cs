using System.Diagnostics;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BricksAndHearts.Controllers;

public class HomeController : AbstractController
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
        var model = new HomeViewModel
        {
            IsLoggedIn = isAuthenticated,
            UserName = User.Identity?.Name,
            IsRegisteredAsLandlord = isAuthenticated && GetCurrentUser().LandlordId != null
        };
        return View(model);
    }

    public IActionResult Privacy()
    {
        return StatusCode(418);
        return View();
    }

    [Route("/Error/{status:int}")]
    public IActionResult Error(int status)
    {
        var errorInfo = ErrorService.GetStatusMessage(status);
        return View(new ErrorViewModel
        {
            RequestId = status.ToString(),
            StatusName = errorInfo.Item1,
            StatusMessage = errorInfo.Item2
        });
    }
}