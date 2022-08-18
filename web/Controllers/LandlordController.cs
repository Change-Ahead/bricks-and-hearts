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
    private readonly ILogger<LandlordController> _logger;
    private readonly IMailService _mailService;
    private readonly IPropertyService _propertyService;
    private readonly IAzureStorage _azureStorage;

    public LandlordController(ILogger<LandlordController> logger,
        ILandlordService landlordService, IPropertyService propertyService, IMailService mailService, IAzureStorage azureStorage)
    {
        _logger = logger;
        _landlordService = landlordService;
        _propertyService = propertyService;
        _mailService = mailService;
        _azureStorage = azureStorage;
    }

    [HttpGet("register")]
    public ActionResult RegisterGet(bool createUnassigned = false)
    {
        var currentUser = GetCurrentUser();
        if (currentUser.LandlordId != null && !currentUser.IsAdmin)
        {
            _logger.LogWarning("User {UserId} is already registered, will redirect to profile", currentUser.Id);
            return Redirect(Url.Action("MyProfile")!);
        }

        if (currentUser.IsAdmin && createUnassigned)
        {
            return View("Register", new LandlordProfileModel { Unassigned = true });
        }

        if (currentUser.IsAdmin)
        {
            AddFlashMessage("warning", "You are currently registering yourself as a landlord. If your intention is to create an unassigned landlord account on behalf of someone else, please use the 'Create Unassigned Landlord' link on the Landlord List page.");
        }
        return View("Register", new LandlordProfileModel
        {
            Email = GetCurrentUser().GoogleEmail
        });
    }

    [HttpPost("register")]
    public async Task<ActionResult> RegisterPost([FromForm] LandlordProfileModel createModel)
    {
        // This does checks based on the annotations (e.g. [Required]) on LandlordProfileModel
        if (!ModelState.IsValid || !TryValidateModel(createModel.Address, nameof(AddressModel)))
        {
            return View("Register", createModel);
        }

        var user = GetCurrentUser();
        ILandlordService.LandlordRegistrationResult result;
        LandlordDbModel? landlord;

        if (createModel.Unassigned == false)
        {
            (result, landlord) = await _landlordService.RegisterLandlord(createModel, user);
        }
        else if (createModel.Unassigned && user.IsAdmin)
        {
            (result, landlord) = await _landlordService.RegisterLandlord(createModel);
        }
        else
        {
            return StatusCode(403);
        }

        switch (result)
        {
            case ILandlordService.LandlordRegistrationResult.Success:
                _logger.LogInformation("Successfully created landlord for user {UserId}", user.Id);
                var msgBody = "A Landlord has just registered\n"
                              + "\n"
                              + $"Title: {createModel.Title}\n"
                              + $"First Name: {createModel.FirstName}" + "\n"
                              + $"Last Name: {createModel.LastName}" + "\n"
                              + $"Company Name: {createModel.CompanyName}" + "\n"
                              + $"Email: {createModel.Email}" + "\n"
                              + $"Phone: {createModel.Phone}" + "\n";
                var subject = "Bricks&Hearts - landlord registration notification";
                _mailService.TrySendMsgInBackground(msgBody, subject);
                return RedirectToAction("Profile", "Landlord", new { landlord!.Id });

            case ILandlordService.LandlordRegistrationResult.ErrorLandlordEmailAlreadyRegistered:
                _logger.LogWarning("Email already registered {Email}", createModel.Email);
                ModelState.AddModelError("Email", "Email already registered");
                return View("Register", createModel);

            case ILandlordService.LandlordRegistrationResult.ErrorLandlordMembershipIdAlreadyRegistered:
                _logger.LogWarning("Membership Id already registered {MembershipId}", createModel.MembershipId);
                ModelState.AddModelError("MembershipId", "Membership Id already registered");
                return View("Register", createModel);

            case ILandlordService.LandlordRegistrationResult.ErrorUserAlreadyHasLandlordRecord:
                _logger.LogWarning("User {UserId} already associated with landlord", user.Id);
                AddFlashMessage("warning", "Already registered");
                return Redirect(Url.Action("MyProfile")!);

            default:
                throw new Exception($"Unknown landlord registration error ${result}");
        }
    }

    [HttpGet("{id:int}/profile")]
    public async Task<ActionResult> Profile([FromRoute] int id)
    {
        var user = GetCurrentUser();
        if (user.LandlordId != id && !user.IsAdmin)
        {
            return StatusCode(403);
        }

        var landlord = await _landlordService.GetLandlordIfExistsWithProperties(id);
        if (landlord == null)
        {
            return StatusCode(404);
        }

        var viewModel = LandlordProfileModel.FromDbModel(landlord);
        viewModel.GoogleProfileImageUrl = _landlordService.GetLandlordProfilePicture(id);
        return View("Profile", viewModel);
    }

    [HttpGet("me/profile")]
    public async Task<ActionResult> MyProfile()
    {
        var landlordId = GetCurrentUser().LandlordId;
        if (landlordId == null)
        {
            return StatusCode(404);
        }

        return await Profile(landlordId.Value);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult> ApproveCharter(LandlordProfileModel landlord)
    {
        if (landlord.MembershipId == null)
        {
            var message = "Membership ID is required.";
            _logger.LogInformation(message);
            AddFlashMessage("warning", message);
            return RedirectToAction("Profile", "Landlord", new { Id = landlord.LandlordId!.Value });
        }

        var user = GetCurrentUser();
        var result = await _landlordService.ApproveLandlord(landlord.LandlordId!.Value, user, landlord.MembershipId);

        string flashMessageBody,
            flashMessageType;
        switch (result)
        {
            case ILandlordService.ApproveLandlordResult.ErrorLandlordNotFound:
                flashMessageBody = "Sorry, it appears that no landlord with this ID exists.";
                flashMessageType = "warning";
                break;
            case ILandlordService.ApproveLandlordResult.ErrorAlreadyApproved:
                flashMessageBody = "The charter for this landlord has already been approved.";
                flashMessageType = "warning";
                break;
            case ILandlordService.ApproveLandlordResult.ErrorDuplicateMembershipId:
                flashMessageBody = "This membership ID already exists for another user.";
                flashMessageType = "warning";
                break;
            case ILandlordService.ApproveLandlordResult.Success:
                flashMessageBody = "Successfully approved landlord charter.";
                flashMessageType = "success";
                break;
            default:
                flashMessageBody = "Unknown error occurred.";
                flashMessageType = "warning";
                break;
        }

        _logger.LogInformation(flashMessageBody);
        AddFlashMessage(flashMessageType, flashMessageBody);
        return RedirectToAction("Profile", "Landlord", new { Id = landlord.LandlordId.Value });
    }

    [HttpGet]
    [Route("me/properties")]
    [Route("{id:int}/properties")]
    public async Task<IActionResult> ViewProperties(int? id = null)
    {
        if (id == null)
        {
            id = GetCurrentUser().LandlordId;
            if (id == null)
            {
                return StatusCode(404);
            }
        }
        else if (!GetCurrentUser().IsAdmin && id != GetCurrentUser().LandlordId)
        {
            return StatusCode(403);
        }

        var databaseResult = _propertyService.GetPropertiesByLandlord(id.Value);
        var listOfProperties = databaseResult.Select(PropertyViewModel.FromDbModel).ToList();
        var landlordProfile = LandlordProfileModel.FromDbModel(await _landlordService.GetLandlordFromId((int)id));

        TempData["Wide"] = true;
        return View("Properties",
            new PropertiesDashboardViewModel(listOfProperties, listOfProperties.Count, landlordProfile));
    }

    [HttpGet("{landlordId:int}/edit")]
    public async Task<ActionResult> EditProfilePage(int? landlordId)
    {
        var user = GetCurrentUser();
        var landlordFromDb = await _landlordService.GetLandlordIfExistsFromId(landlordId);
        if (landlordFromDb == null)
        {
            return StatusCode(404);
        }

        if (user.LandlordId != landlordFromDb.Id && !user.IsAdmin)
        {
            return StatusCode(403);
        }

        return View("EditProfilePage", LandlordProfileModel.FromDbModel(landlordFromDb));
    }

    [HttpPost("edit")]
    public async Task<ActionResult> EditProfileUpdate([FromForm] LandlordProfileModel editModel)
    {
        var user = GetCurrentUser();
        if (!ModelState.IsValid || !TryValidateModel(editModel.Address, nameof(AddressModel)))
        {
            return View("EditProfilePage", editModel);
        }

        if (user.LandlordId != editModel.LandlordId && !user.IsAdmin)
        {
            return StatusCode(403);
        }

        var isEmailDuplicated = _landlordService.CheckForDuplicateEmail(editModel);
        if (isEmailDuplicated)
        {
            _logger.LogWarning("Email already registered {Email}", editModel.Email);
            ModelState.AddModelError("Email", "Email already registered");
            return View("EditProfilePage", editModel);
        }

        var isMembershipIdDuplicated = _landlordService.CheckForDuplicateMembershipId(editModel);
        if (isMembershipIdDuplicated)
        {
            _logger.LogWarning("Membership Id already registered {MembershipId}", editModel.MembershipId);
            ModelState.AddModelError("MembershipId", "Membership Id already registered");
            return View("EditProfilePage", editModel);
        }

        if (editModel.MembershipId == null)
        {
            // If membership ID is removed, also unapprove charter
            await _landlordService.UnapproveLandlord(editModel.LandlordId!.Value);
        }

        await _landlordService.EditLandlordDetails(editModel);
        _logger.LogInformation("Successfully edited landlord for landlord {LandlordId}", editModel.LandlordId);
        return RedirectToAction("Profile", new { id = editModel.LandlordId });
    }

    [HttpGet("/invite/{inviteLink}")]
    public ActionResult Invite(string inviteLink)
    {
        InviteViewModel model = new();
        var landlord = _landlordService.FindLandlordWithInviteLink(inviteLink);
        if (landlord == null)
        {
            return View(model);
        }

        model.InviteLinkToAccept = inviteLink;
        model.Landlord = LandlordProfileModel.FromDbModel(landlord);
        return View(model);
    }

    [HttpPost("/invite/{inviteLink}/accepted")]
    public async Task<IActionResult> TieUserWithLandlord(string inviteLink)
    {
        var user = GetCurrentUser();
        var result = await _landlordService.LinkExistingLandlordWithUser(inviteLink, user);
        switch (result)
        {
            case ILandlordService.LinkUserWithLandlordResult.ErrorLinkDoesNotExist:
                _logger.LogWarning("Invite Link {Link} does not work", inviteLink);
                return RedirectToAction(nameof(Invite), new { inviteLink });
            case ILandlordService.LinkUserWithLandlordResult.ErrorUserAlreadyHasLandlordRecord:
                _logger.LogWarning("User {UserId} already associated with landlord", user.Id);
                AddFlashMessage("warning", $"User already registered with landlord (landlordId = {user.LandlordId})");
                return RedirectToAction(nameof(MyProfile));
            case ILandlordService.LinkUserWithLandlordResult.Success:
                _logger.LogInformation("Successfully registered landlord with user {UserId}", user.Id);
                AddFlashMessage("success",
                    $"User {user.Id} successfully linked with landlord (landlordId = {user.LandlordId})");
                return RedirectToAction(nameof(MyProfile));
            default:
                throw new Exception($"Unknown landlord registration error ${result}");
        }
    }

    [HttpGet("{id:int}/dashboard")]
    public async Task<ActionResult> Dashboard([FromRoute] int id)
    {
        var user = GetCurrentUser();
        if (user.LandlordId != id && !user.IsAdmin)
        {
            return StatusCode(403);
        }

        var landlord = await _landlordService.GetLandlordIfExistsWithProperties(id);
        if (landlord == null)
        {
            return StatusCode(404);
        }
        var landlordViewProperties = landlord.Properties.Select(PropertyViewModel.FromDbModel).OrderByDescending(p => p.PropertyId).Take(2);
        var allPropertyDetails = new List<PropertyDetailsViewModel>();
        foreach (var property in landlordViewProperties)
        {
            var fileNames = await _azureStorage.ListFileNames("property", property.PropertyId);
            allPropertyDetails.Add(new PropertyDetailsViewModel
            {
                Images = GetFilesFromFileNames(fileNames, property.PropertyId),
                Property = property
            });
        }
        var viewModel = new LandlordDashboardViewModel
        {
            CurrentLandlord = LandlordProfileModel.FromDbModel(landlord),
            Properties = allPropertyDetails,
            PropertyTypeCount = _propertyService.CountProperties(id)
        };
        viewModel.CurrentLandlord.GoogleProfileImageUrl = _landlordService.GetLandlordProfilePicture(id);
        return View("Dashboard", viewModel);
    }

    private List<ImageFileUrlModel> GetFilesFromFileNames(IEnumerable<string> fileNames, int propertyId)
    {
        return fileNames.Select(fileName =>
            {
                var url = Url.Action("GetImage", new { propertyId, fileName })!;
                return new ImageFileUrlModel(fileName, url);
            })
            .ToList();
    }
    
    [HttpGet("{propertyId:int}/{fileName}")]
    public async Task<IActionResult> GetImage(int propertyId, string fileName)
    {
        var (data, fileType) = await _azureStorage.DownloadFile("property", propertyId, fileName);

        return File(data!, $"image/{fileType}");
    }

    [HttpGet("me/dashboard")]
    public async Task<ActionResult> MyDashboard()
    {
        var landlordId = GetCurrentUser().LandlordId;
        if (landlordId == null)
        {
            return StatusCode(404);
        }

        return await Dashboard(landlordId.Value);
    }
}