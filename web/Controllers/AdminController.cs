using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BricksAndHearts.Controllers;

[Authorize]
public class AdminController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly BricksAndHeartsDbContext _dbContext;

    public AdminController(ILogger<HomeController> logger, BricksAndHeartsDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public IActionResult Index()
    {
        return View();
    }
}