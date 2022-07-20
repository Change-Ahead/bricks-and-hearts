#region

using BricksAndHearts.Auth;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

#endregion

namespace BricksAndHearts.Controllers;

public class AdminController : AbstractController
{
    private readonly IAdminService _adminService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(ILogger<AdminController> logger, IAdminService adminService)
    {
        _logger = logger;
        _adminService = adminService;
    }

    public IActionResult Index()
    {
        var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
        var viewModel = new AdminViewModel();
        if (isAuthenticated) viewModel.CurrentUser = GetCurrentUser();

        return View(viewModel);
    }

    [Authorize]
    [HttpPost]
    public IActionResult RequestAdminAccess()
    {
        var user = GetCurrentUser();
        if (user.IsAdmin)
        {
            LoggerAlreadyAdminWarning(_logger, user);
            return RedirectToAction(nameof(Index));
        }

        _adminService.RequestAdminAccess(user);
        FlashRequestSuccess(_logger, user, "requested admin access");
        return RedirectToAction(nameof(Index));
    }

    public IActionResult CancelAdminAccessRequest()
    {
        var user = GetCurrentUser();
        if (user.IsAdmin)
        {
            LoggerAlreadyAdminWarning(_logger, user);

            return RedirectToAction(nameof(Index));
        }

        _adminService.CancelAdminAccessRequest(user);

        FlashRequestSuccess(_logger, user, "cancelled admin access request");

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<AdminListModel> GetAdminList()
    {
        AdminListModel adminListModel = new AdminListModel();
        var adminLists = await _adminService.GetAdminLists();
        adminListModel.CurrentAdmins = adminLists.CurrentAdmins;
        adminListModel.PendingAdmins = adminLists.PendingAdmins;
        return adminListModel;
    }

    [Authorize(Roles="Admin")]
    [HttpGet]
    public async Task<IActionResult> AdminList()
    {
        AdminListModel adminListModel = GetAdminList().Result;
        return View(adminListModel);
    }

    [HttpGet]
    public UserDbModel findUserFromId(int userId)
    {
        return _dbContext.Users.SingleOrDefault(u => u.Id == userId);
    }
    
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> LandlordList(string? approvalStatus = "")
    {
        LandlordListModel landlordListModel = new LandlordListModel();
        landlordListModel.LandlordDisplayList = await _adminService.GetLandlordDisplayList(approvalStatus!);
        return View(landlordListModel);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public ActionResult GetInviteLink(int landlordId)
    {
        var user = _adminService.FindUserByLandlordId(landlordId);
        if (user != null) // If landlord already linked to user
        {
            TempData["InviteLinkMessage"] = $"Landlord already linked to user {user.GoogleUserName}";
        }
        else
        {
            var inviteLink = _adminService.FindExistingInviteLink(landlordId);
            if (string.IsNullOrEmpty(inviteLink))
            {
                TempData["InviteLinkMessage"] = "Successfully created a new invite link :)";
                inviteLink = _adminService.CreateNewInviteLink(landlordId);
            }
            else
            {
                TempData["InviteLinkMessage"] = "Landlord already has an invite link!";
            }
            TempData["InviteLink"] = $"{inviteLink}";
        }
        return RedirectToAction("Profile","Landlord", new {id=landlordId});
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public ActionResult RenewInviteLink(int landlordId)
    {
        var user = _adminService.FindUserByLandlordId(landlordId);
        if (user != null) // If landlord already linked to user
        {
            TempData["InviteLinkMessage"] = $"Landlord already linked to user {user.GoogleUserName}";
        }
        else
        {
            var inviteLink = _adminService.FindExistingInviteLink(landlordId);
            if (string.IsNullOrEmpty(inviteLink))
            {
                TempData["InviteLinkMessage"] = "No existing invite link. Successfully created a new invite link :)";
                inviteLink = _adminService.CreateNewInviteLink(landlordId);
            }
            else
            {
                _adminService.DeleteExistingInviteLink(landlordId);
                inviteLink = _adminService.CreateNewInviteLink(landlordId);
                TempData["InviteLinkMessage"] = "Successfully created a new invite link :)";
            }
            TempData["InviteLink"] = $"{inviteLink}";
        }
        return RedirectToAction("Profile","Landlord", new {id=landlordId});
    }

    private void LoggerAlreadyAdminWarning(ILogger logger, BricksAndHeartsUser user)
    {
        FlashMessage(logger,
            ($"User {user.Id} already an admin",
                "danger",
                "Already an admin"));
    }

    [HttpPost]
    public async Task<ActionResult> AcceptAdminRequest(AdminListModel userToAcceptList, int userToAcceptId)
    {
        var userToAccept = findUserFromId(userToAcceptId);
        
        userToAccept.IsAdmin = true;
        userToAccept.HasRequestedAdmin = false;
        _dbContext.SaveChanges();

        AdminListModel adminListModel = GetAdminList().Result;
        
        return View("AdminList", adminListModel);
    }
}