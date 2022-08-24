using BricksAndHearts.Database;
using BricksAndHearts.Enums;
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
        ILandlordService landlordService, IPropertyService propertyService, IMailService mailService,
        IAzureStorage azureStorage)
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
        var currentUser = CurrentUser;
        if (currentUser.LandlordId != null && !currentUser.IsAdmin)
        {
            _logger.LogWarning("User {UserId} is already registered, will redirect to profile", currentUser.Id);
            return RedirectToAction(nameof(MyProfile));
        }

        if (currentUser.IsAdmin && createUnassigned)
        {
            return View("Register", new LandlordProfileModel { Unassigned = true });
        }

        if (currentUser.IsAdmin)
        {
            AddFlashMessage("warning",
                "You are currently registering yourself as a landlord. If your intention is to create an unassigned landlord account on behalf of someone else, please use the 'Create Unassigned Landlord' link on the Landlord List page.");
        }

        return View("Register", new LandlordProfileModel
        {
            Email = CurrentUser.GoogleEmail
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

        var user = CurrentUser;
        LandlordRegistrationResult result;
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
            case LandlordRegistrationResult.Success:
                _logger.LogInformation("Successfully created landlord for user {UserId}", user.Id);
                var msgBody = "A Landlord has just registered\n"
                              + "\n"
                              + $"Title: {createModel.Title}\n"
                              + $"First Name: {createModel.FirstName}"
                              + "\n"
                              + $"Last Name: {createModel.LastName}"
                              + "\n"
                              + $"Company Name: {createModel.CompanyName}"
                              + "\n"
                              + $"Email: {createModel.Email}"
                              + "\n"
                              + $"Phone: {createModel.Phone}"
                              + "\n";
                var subject = "Bricks&Hearts - landlord registration notification";
                _mailService.TrySendMsgInBackground(msgBody, subject);
                return RedirectToAction(nameof(Profile), "Landlord", new { landlord!.Id });

            case LandlordRegistrationResult.ErrorLandlordEmailAlreadyRegistered:
                _logger.LogWarning("Email already registered {Email}", createModel.Email);
                ModelState.AddModelError("Email", "Email already registered");
                return View("Register", createModel);

            case LandlordRegistrationResult.ErrorLandlordMembershipIdAlreadyRegistered:
                _logger.LogWarning("Membership Id already registered {MembershipId}", createModel.MembershipId);
                ModelState.AddModelError("MembershipId", "Membership Id already registered");
                return View("Register", createModel);

            case LandlordRegistrationResult.ErrorUserAlreadyHasLandlordRecord:
                _logger.LogWarning("User {UserId} already associated with landlord", user.Id);
                AddFlashMessage("warning", "Already registered");
                return RedirectToAction(nameof(MyProfile));

            default:
                throw new Exception($"Unknown landlord registration error ${result}");
        }
    }

    [Authorize(Roles = "Admin, Landlord")]
    [HttpGet("{id:int}/profile")]
    public async Task<ActionResult> Profile([FromRoute] int id)
    {
        var user = CurrentUser;
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

    [Authorize(Roles = "Admin, Landlord")]
    [HttpGet("me/profile")]
    public async Task<ActionResult> MyProfile()
    {
        var landlordId = CurrentUser.LandlordId;
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
            return RedirectToAction(nameof(Profile), "Landlord", new { Id = landlord.LandlordId!.Value });
        }

        var user = CurrentUser;
        var result = await _landlordService.ApproveLandlord(landlord.LandlordId!.Value, user, landlord.MembershipId);

        string flashMessageBody,
            flashMessageType;
        switch (result)
        {
            case ApproveLandlordResult.ErrorLandlordNotFound:
                flashMessageBody = "Sorry, it appears that no landlord with this ID exists.";
                flashMessageType = "warning";
                break;
            case ApproveLandlordResult.ErrorAlreadyApproved:
                flashMessageBody = "The charter for this landlord has already been approved.";
                flashMessageType = "warning";
                break;
            case ApproveLandlordResult.ErrorDuplicateMembershipId:
                flashMessageBody = "This membership ID already exists for another user.";
                flashMessageType = "warning";
                break;
            case ApproveLandlordResult.Success:
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
        return RedirectToAction(nameof(Profile), "Landlord", new { Id = landlord.LandlordId.Value });
    }
    
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/{action}")]
    public async Task<ActionResult> DisableOrEnableLandlord(LandlordProfileModel landlord, string action)
    {
        var result = await _landlordService.DisableOrEnableLandlord(landlord.LandlordId!.Value, action);

        string flashMessageBody, flashMessageType;
        switch (result)
        {
            case DisableOrEnableLandlordResult.ErrorLandlordNotFound:
                flashMessageBody = "Sorry, it appears that no landlord with this ID exists.";
                flashMessageType = "warning";
                break;
            case DisableOrEnableLandlordResult.ErrorAlreadyInState:
                flashMessageBody = $"This landlord has already been {action}d.";
                flashMessageType = "warning";
                break;
            case DisableOrEnableLandlordResult.Success:
                flashMessageBody = $"Successfully {action}d landlord.";
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

    [Authorize(Roles = "Admin, Landlord")]
    [HttpGet]
    [Route("me/properties")]
    [Route("{id:int}/properties")]
    public async Task<IActionResult> ViewProperties(int? id = null, int page = 1, int propPerPage = 10)
    {
        if (id == null)
        {
            id = CurrentUser.LandlordId;
            if (id == null)
            {
                return StatusCode(404);
            }
        }
        else if (!CurrentUser.IsAdmin && id != CurrentUser.LandlordId)
        {
            return StatusCode(403);
        }

        var properties = await _propertyService.GetPropertiesByLandlord(id.Value, page, propPerPage);
        var listOfProperties = properties.PropertyList.Select(PropertyViewModel.FromDbModel).ToList();
        var landlordProfile = LandlordProfileModel.FromDbModel(await _landlordService.GetLandlordFromId((int)id));

        TempData["FullWidthPage"] = true;
        return View("Properties",
            new PropertyListModel(listOfProperties, properties.Count, landlordProfile, page));
    }

    [Authorize(Roles = "Admin, Landlord")]
    [HttpGet("{landlordId:int}/profile/edit")]
    public async Task<ActionResult> EditProfilePage(int? landlordId)
    {
        var user = CurrentUser;
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

    [Authorize(Roles = "Admin, Landlord")]
    [HttpPost("profile/edit")]
    public async Task<ActionResult> EditProfileUpdate([FromForm] LandlordProfileModel editModel)
    {
        var user = CurrentUser;
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
        return RedirectToAction(nameof(Profile), new { id = editModel.LandlordId });
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

    [HttpPost("/invite/{inviteLink}/accept")]
    public async Task<IActionResult> TieUserWithLandlord(string inviteLink)
    {
        var user = CurrentUser;
        var result = await _landlordService.LinkExistingLandlordWithUser(inviteLink, user);
        switch (result)
        {
            case LinkUserWithLandlordResult.ErrorLinkDoesNotExist:
                _logger.LogWarning("Invite Link {Link} does not work", inviteLink);
                return RedirectToAction(nameof(Invite), new { inviteLink });
            case LinkUserWithLandlordResult.ErrorUserAlreadyHasLandlordRecord:
                _logger.LogWarning("User {UserId} already associated with landlord", user.Id);
                AddFlashMessage("warning", $"User already registered with landlord (landlordId = {user.LandlordId})");
                return RedirectToAction(nameof(MyProfile));
            case LinkUserWithLandlordResult.Success:
                _logger.LogInformation("Successfully registered landlord with user {UserId}", user.Id);
                AddFlashMessage("success",
                    $"User {user.Id} successfully linked with landlord (landlordId = {user.LandlordId})");
                return RedirectToAction(nameof(MyProfile));
            default:
                throw new Exception($"Unknown landlord registration error ${result}");
        }
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult> Dashboard()
    {
        var user = CurrentUser;
        if (!user.LandlordId.HasValue)
        {
            return StatusCode(403);
        }

        var landlord = await _landlordService.GetLandlordIfExistsWithProperties(user.LandlordId);
        if (landlord == null)
        {
            return StatusCode(404);
        }

        var landlordViewProperties = landlord.Properties.Select(PropertyViewModel.FromDbModel)
            .OrderByDescending(p => p.PropertyId).Take(2);
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
            PropertyTypeCount = _propertyService.CountProperties(user.LandlordId)
        };
        viewModel.CurrentLandlord.GoogleProfileImageUrl =
            _landlordService.GetLandlordProfilePicture(user.LandlordId.Value);
        return View("Dashboard", viewModel);
    }

    private List<ImageFileUrlModel> GetFilesFromFileNames(IEnumerable<string> fileNames, int propertyId)
    {
        return fileNames.Select(fileName =>
            {
                var url = Url.Action(nameof(GetImage), new { propertyId, fileName })!;
                return new ImageFileUrlModel(fileName, url);
            })
            .ToList();
    }

    [HttpGet("property/{propertyId:int}/{fileName}")]
    public async Task<IActionResult> GetImage(int propertyId, string fileName)
    {
        var (data, fileType) = await _azureStorage.DownloadFile("property", propertyId, fileName);

        return File(data!, $"image/{fileType}");
    }
}