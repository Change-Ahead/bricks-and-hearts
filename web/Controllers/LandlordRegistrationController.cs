using BricksAndHearts.Database;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BricksAndHearts.Controllers;

[Authorize]
[Route("/landlord/register")]
public class LandlordRegistrationController : AbstractController
{
    private readonly BricksAndHeartsDbContext _dbContext;
    private readonly ILandlordRegistrationService _landlordRegistrationService;
    private readonly ILogger<LandlordRegistrationController> _logger;

    public LandlordRegistrationController(ILogger<LandlordRegistrationController> logger, BricksAndHeartsDbContext dbContext,
        ILandlordRegistrationService landlordRegistrationService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _landlordRegistrationService = landlordRegistrationService;
    }

    [HttpGet]
    [Route("")]
    public ActionResult Index()
    {
        var currentUser = GetCurrentUser();
        if (currentUser.LandlordId != null)
        {
            _logger.LogWarning("User {UserId} is already registered, will redirect to profile", currentUser.Id);
            return Redirect("/landlord/me/profile");
        }

        return View("Create", new LandlordCreateModel
        {
            Email = GetCurrentUser().GoogleEmail
        });
    }

    [HttpPost]
    [Route("")]
    public async Task<ActionResult> Create([FromForm] LandlordCreateModel createModel)
    {
        // This does checks based on the annotations (e.g. [Required]) on LandlordCreateModel
        if (!ModelState.IsValid) return View("Create");

        var user = GetCurrentUser();

        var result = await _landlordRegistrationService.RegisterLandlordWithUser(createModel, user);

        switch (result)
        {
            case ILandlordRegistrationService.LandlordRegistrationResult.Success:
                _logger.LogInformation("Successfully created landlord for user {UserId}", user.Id);
                return Redirect("/landlord/me/profile");

            case ILandlordRegistrationService.LandlordRegistrationResult.ErrorLandlordEmailAlreadyRegistered:
                _logger.LogWarning("Email already registered {Email}", createModel.Email);
                ModelState.AddModelError("Email", "Email already registered");
                return View("Create");

            case ILandlordRegistrationService.LandlordRegistrationResult.ErrorUserAlreadyHasLandlordRecord:
                _logger.LogWarning("User {UserId} already associated with landlord", user.Id);
                TempData["FlashMessage"] = "Already registered!"; // TODO: Landlord profile page should display this message
                return Redirect("/landlord/me/profile");

            default:
                throw new Exception($"Unknown landlord registration error ${result}");
        }
    }
}