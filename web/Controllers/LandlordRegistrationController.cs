using BricksAndHearts.Database;
using BricksAndHearts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BricksAndHearts.Controllers;

[Authorize]
[Route("/landlord/register")]
public class LandlordRegistrationController : AbstractController
{
    private readonly BricksAndHeartsDbContext _dbContext;

    public LandlordRegistrationController(BricksAndHeartsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    [Route("")]
    public ActionResult Index()
    {
        return View("Create", new LandlordCreateModel
        {
            Email = GetCurrentUser().GoogleEmail
        });
    }

    [HttpPost]
    [Route("")]
    public async Task<ActionResult> Create([FromForm] LandlordCreateModel createModel)
    {
        if (!ModelState.IsValid)
        {
            return View("Create");
        }

        var dbModel = new LandlordDbModel
        {
            FirstName = createModel.FirstName,
            LastName = createModel.LastName,
            CompanyName = createModel.CompanyName,
            Email = createModel.Email,
            Phone = createModel.Phone,
        };
        _dbContext.Landlords.Add(dbModel);
        await _dbContext.SaveChangesAsync();

        return Redirect("/landlord/me/profile"); // qq
    }
}