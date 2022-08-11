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

    [HttpGet]
    public IActionResult Index()
    {
        var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
        if (isAuthenticated)
        {
            var user = GetCurrentUser();
            if (user.IsAdmin)
            {
                return RedirectToAction("AdminDashboard", "Admin");
            }

            if (user.LandlordId != null)
            {
                return RedirectToAction("MyDashboard", "Landlord");
            }
        }
        var model = new HomeViewModel
        {
            IsLoggedIn = isAuthenticated,
            UserName = User.Identity?.Name,
        };
        return View(model);
    }
    
    [HttpGet]
    public IActionResult Privacy()
    {
        return View();
    }
    
    [HttpGet]
    public IActionResult ContactUs()
    {
        return View();
    }
    
    [HttpGet]
    [Route("/Error/{status:int}")]
    public IActionResult Error(int status)
    {
        ErrorService errorService = new ErrorService();
        var errorInfo = errorService.GetStatusMessage(status);
        return View(new ErrorViewModel
        {
            RequestId = status.ToString(),
            StatusName = errorInfo.StatusName,
            StatusMessage = errorInfo.StatusMessage
        });
    }

    [HttpGet]
    [Route("/ExceptionTest")]
    public IActionResult ExceptionTest()
    {
        throw new Exception("For testing purposes only");
    }
    
}
