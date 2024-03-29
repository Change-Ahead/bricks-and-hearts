﻿using BricksAndHearts.Services;
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
            var user = CurrentUser;
            if (user.IsAdmin)
            {
                return RedirectToAction(nameof(AdminController.AdminDashboard), "Admin");
            }

            if (user.LandlordId != null)
            {
                return RedirectToAction(nameof(LandlordController.Dashboard), "Landlord");
            }
        }

        var model = new HomeViewModel
        {
            IsLoggedIn = isAuthenticated,
            UserName = User.Identity?.Name
        };
        return View(model);
    }

    [HttpGet("privacy")]
    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet("contact")]
    public IActionResult ContactUs()
    {
        return View(new ContactUsViewModel());
    }

    [HttpGet]
    [Route("/error/{status:int}")]
    public IActionResult Error(int status)
    {
        var errorService = new ErrorService();
        var errorInfo = errorService.GetStatusMessage(status);
        return View(new ErrorViewModel
        {
            RequestId = status.ToString(),
            StatusName = errorInfo.StatusName,
            StatusMessage = errorInfo.StatusMessage
        });
    }
}