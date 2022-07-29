﻿using BricksAndHearts.Services;
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
    private readonly IMailService _mailService;
    private readonly ILogger<LandlordController> _logger;

    public LandlordController(ILogger<LandlordController> logger,
        ILandlordService landlordService, IPropertyService propertyService, IMailService mailService)
    {
        _logger = logger;
        _landlordService = landlordService;
        _propertyService = propertyService;
        _mailService = mailService;
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
                string msgBody = $"A Landlord has just registered\n"
                                 + "\n"
                                 + $"Title: {createModel.Title}\n"
                                 + $"First Name: {createModel.FirstName}" + "\n"
                                 + $"Last Name: {createModel.LastName}" + "\n"
                                 + $"Company Name: {createModel.CompanyName}" + "\n"
                                 + $"Email: {createModel.Email}" + "\n"
                                 + $"Phone: {createModel.Phone}" + "\n";
                _mailService.SendMsg(msgBody);

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

        var landlord = await _landlordService.GetLandlordIfExistsFromId(id);
        if (landlord == null)
        {
            return StatusCode(404);
        }

        var viewModel = LandlordProfileModel.FromDbModel(landlord, user);
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
    
    [Authorize(Roles="Admin")]
    [HttpPost]
    public async Task<ActionResult> ApproveCharter(int landlordId)
    {
        var user = GetCurrentUser();
        await _landlordService.ApproveLandlord(landlordId, user);
        return RedirectToAction("Profile", "Landlord", new { Id = landlordId });
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
    [Route("edit")]
    public async Task<ActionResult> EditProfilePage(int landlordId)
    {
        var user = GetCurrentUser();
        var landlordFromDb =  await _landlordService.GetLandlordIfExistsFromId(landlordId);
        if (landlordFromDb == null) return StatusCode(404);
        if (user.LandlordId != landlordFromDb.Id && !user.IsAdmin) return StatusCode(403);
        return View("EditProfilePage", LandlordProfileModel.FromDbModel(landlordFromDb, user));
    }

    [HttpPost]
    [Route("edit")]
    public async Task<ActionResult> EditProfileUpdate([FromForm] LandlordProfileModel editModel)
    {
        var user = GetCurrentUser();
        if (user.LandlordId != editModel.LandlordId && !user.IsAdmin) return StatusCode(403);
        var isEmailDuplicated = _landlordService.CheckForDuplicateEmail(editModel);
        if (isEmailDuplicated)
        {
            _logger.LogWarning("Email already registered {Email}", editModel.Email);
            ModelState.AddModelError("Email", "Email already registered");
            return View("EditProfilePage", editModel);
        }

        await _landlordService.EditLandlordDetails(editModel);
        _logger.LogInformation("Successfully edited landlord for landlord {LandlordId}", editModel.LandlordId);
        return RedirectToAction("Profile", new{id = editModel.LandlordId});
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

    [HttpGet]
    [Route("/invite/link={inviteLink}")]
    public ActionResult Invite(string inviteLink)
    {
        InviteViewModel model = new();
        var landlord = _landlordService.FindLandlordWithInviteLink(inviteLink);
        if (landlord == null)
        {
            return View(model);
        }

        model.InviteLinkToAccept = inviteLink;
        return View(model);
    }

    [HttpPost]
    [Route("/invite/link={inviteLink}/accepted")]
    public async Task<IActionResult> TieUserWithLandlord(string inviteLink)
    {
        var user = GetCurrentUser();
        var result = await _landlordService.LinkExistingLandlordWithUser(inviteLink, user);
        if (result == ILandlordService.LinkUserWithLandlordResult.ErrorLinkDoesNotExist)
        {
            _logger.LogWarning("Invite Link {Link} does not work", inviteLink);
            return RedirectToAction(nameof(Invite),new InviteViewModel(inviteLink));
        }
        if (result == ILandlordService.LinkUserWithLandlordResult.ErrorUserAlreadyHasLandlordRecord)
        {
            _logger.LogWarning("User {UserId} already associated with landlord", user.Id);
            TempData["FlashMessage"] = $"User already registered with landlord (landlordId = {user.LandlordId})"; // This will be displayed on the Profile page
            return RedirectToAction(nameof(MyProfile));
        }

        if (result == ILandlordService.LinkUserWithLandlordResult.Success)
        {
            _logger.LogInformation("Successfully registered landlord with user {UserId}", user.Id);
            TempData["FlashMessage"] = $"User {user.Id} successfully linked with landlord (landlordId = {user.LandlordId})"; // This will be displayed on the Profile page
            return RedirectToAction(nameof(MyProfile));
        }
        throw new Exception($"Unknown landlord registration error ${result}");
    }
}