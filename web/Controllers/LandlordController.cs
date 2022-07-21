using BricksAndHearts.Database;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BricksAndHearts.Controllers;

[Authorize]
[Route("/landlord")]
public class LandlordController : AbstractController
{
    private readonly ILandlordService _landlordService;
    private readonly IPropertyService _propertyService;
    private readonly ILogger<LandlordController> _logger;

    public LandlordController(ILogger<LandlordController> logger, BricksAndHeartsDbContext dbContext,
        ILandlordService landlordService, IPropertyService propertyService)
    {
        _logger = logger;
        _landlordService = landlordService;
        _propertyService = propertyService;
    }

    [HttpGet]
    [Route("register")]
    public ActionResult RegisterGet()
    {
        var currentUser = GetCurrentUser();
        if (currentUser.LandlordId != null)
        {
            _logger.LogWarning("User {UserId} is already registered, will redirect to profile", currentUser.Id);
            return Redirect(Url.Action("MyProfile")!);
        }

        return View("Register", new LandlordProfileModel
        {
            Email = GetCurrentUser().GoogleEmail
        });
    }

    [HttpPost]
    [Route("register")]
    public async Task<ActionResult> RegisterPost([FromForm] LandlordProfileModel createModel)
    {
        // This does checks based on the annotations (e.g. [Required]) on LandlordProfileModel
        if (!ModelState.IsValid)
        {
            return View("Register");
        }

        var user = GetCurrentUser();

        var result = await _landlordService.RegisterLandlordWithUser(createModel, user);

        switch (result)
        {
            case ILandlordService.LandlordRegistrationResult.Success:
                _logger.LogInformation("Successfully created landlord for user {UserId}", user.Id);
                return Redirect(Url.Action("MyProfile")!);

            case ILandlordService.LandlordRegistrationResult.ErrorLandlordEmailAlreadyRegistered:
                _logger.LogWarning("Email already registered {Email}", createModel.Email);
                ModelState.AddModelError("Email", "Email already registered");
                return View("Register");

            case ILandlordService.LandlordRegistrationResult.ErrorUserAlreadyHasLandlordRecord:
                _logger.LogWarning("User {UserId} already associated with landlord", user.Id);
                TempData["FlashMessage"] = "Already registered!"; // This will be displayed on the Profile page
                return Redirect(Url.Action("MyProfile")!);

            default:
                throw new Exception($"Unknown landlord registration error ${result}");
        }
    }

    [HttpGet]
    [Route("{id:int}/profile")]
    public async Task<ActionResult> Profile([FromRoute] int id)
    {
        var user = GetCurrentUser();
        if (user.LandlordId != id && !user.IsAdmin)
        {
            return StatusCode(403);
        }

        var landlord = _landlordService.GetLandlordFromId(id);
        if (landlord == null)
        {
            return StatusCode(404);
        }

        var viewModel = LandlordProfileModel.FromDbModel(await landlord);
        return View("Profile", viewModel);
    }

    [HttpGet]
    [Route("me/profile")]
    public async Task<ActionResult> MyProfile()
    {
        var landlordId = GetCurrentUser().LandlordId;
        if (landlordId == null)
        {
            return StatusCode(404);
        }

        return await Profile(landlordId.Value);
    }

    [HttpGet]
    [Route("/properties")]
    public IActionResult ViewProperties()
    {
        var landlordId = GetCurrentUser().LandlordId;
        if (!landlordId.HasValue)
        {
            return StatusCode(403);
        }

        var databaseResult = _propertyService.GetPropertiesByLandlord(landlordId.Value);
        var listOfProperties = databaseResult.Select(PropertyViewModel.FromDbModel).ToList();
        return View("Properties", new PropertiesDashboardViewModel(listOfProperties));
    }

    [HttpGet]
    [Route("/add-property")]
    public IActionResult AddNewProperty()
    {
        return View();
    }

    [HttpPost]
    [Route("/add-property")]
    public ActionResult AddNewProperty([FromForm] PropertyViewModel newPropertyModel)
    {
        var landlordId = GetCurrentUser().LandlordId;
        if (!landlordId.HasValue)
        {
            return StatusCode(403);
        }

        // This does checks based on the annotations (e.g. [Required]) on PropertyViewModel
        if (!ModelState.IsValid)
        {
            return View(newPropertyModel);
        }

        // add property to database
        _propertyService.AddNewProperty(newPropertyModel, landlordId.Value);

        return RedirectToAction("ViewProperties");
    }
}