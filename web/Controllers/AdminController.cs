#region

using BricksAndHearts.Auth;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

#endregion

namespace BricksAndHearts.Controllers;

[Authorize]
[Route("admin")]
public class AdminController : AbstractController
{
    private readonly IAdminService _adminService;
    private readonly ILandlordService _landlordService;
    private readonly IPropertyService _propertyService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ILogger<AdminController> logger,
        IAdminService adminService,
        ILandlordService landlordService,
        IPropertyService propertyService,
        ITenantService tenantService)
    {
        _logger = logger;
        _adminService = adminService;
        _landlordService = landlordService;
        _propertyService = propertyService;
        _tenantService = tenantService;
    }

    [HttpGet("dashboard")]
    public IActionResult AdminDashboard()
    {
        var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
        if (!isAuthenticated)
        {
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        var viewModel =
            new AdminDashboardViewModel(_landlordService.CountLandlords(), _propertyService.CountProperties(),
                _tenantService.CountTenants());
        viewModel.CurrentUser = CurrentUser;

        return View(viewModel);
    }

    [HttpPost("request-access")]
    public IActionResult RequestAdminAccess()
    {
        var user = CurrentUser;
        if (user.IsAdmin)
        {
            LoggerAlreadyAdminWarning(_logger, user);
            return RedirectToAction(nameof(AdminDashboard));
        }

        _adminService.RequestAdminAccess(user);
        _logger.LogInformation($"Successfully requested admin access for user {user.Id}");
        AddFlashMessage("success", "Successfully requested admin access.");
        return RedirectToAction(nameof(AdminDashboard));
    }

    [HttpPost("cancel-request-access")]
    public IActionResult CancelAdminAccessRequest()
    {
        var user = CurrentUser;
        if (user.IsAdmin)
        {
            LoggerAlreadyAdminWarning(_logger, user);
            return RedirectToAction(nameof(AdminDashboard));
        }

        _adminService.CancelAdminAccessRequest(user);
        _logger.LogInformation($"Successfully cancelled admin access request for user {user.Id}");
        AddFlashMessage("success", "Successfully cancelled admin access request.");
        return RedirectToAction(nameof(AdminDashboard));
    }

    private void LoggerAlreadyAdminWarning(ILogger logger, BricksAndHeartsUser user)
    {
        logger.LogInformation($"User {user.Id} already an admin");
        AddFlashMessage("danger", "Already an admin");
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("lists/admins/accept")]
    public ActionResult AcceptAdminRequest(int userToAcceptId)
    {
        _adminService.ApproveAdminAccessRequest(userToAcceptId);
        _logger.LogInformation($"Admin request of user {userToAcceptId} approved by user {CurrentUser.Id}");
        return RedirectToAction("GetAdminList");
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("lists/admins/reject")]
    public ActionResult RejectAdminRequest(int userToRejectId)
    {
        _adminService.RejectAdminAccessRequest(userToRejectId);
        _logger.LogInformation($"Admin request of user {userToRejectId} rejected by user {CurrentUser.Id}");
        return RedirectToAction("GetAdminList");
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("lists/admins/revoke")]
    public ActionResult RemoveAdmin(int userToRemoveId)
    {
        if (userToRemoveId != CurrentUser.Id)
        {
            _adminService.RemoveAdmin(userToRemoveId);
            _logger.LogInformation($"Admin status of user {userToRemoveId} revoked by user {CurrentUser.Id}");
        }
        else
        {
            _logger.LogWarning($"User {userToRemoveId} may not remove their own admin status");
            AddFlashMessage("warning", "You may not remove your own admin status");
        }

        return RedirectToAction("GetAdminList");
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("lists/admins")]
    public async Task<ActionResult> GetAdminList()
    {
        var adminLists = await _adminService.GetAdminLists();
        var adminListModel = new AdminListModel(adminLists.CurrentAdmins, adminLists.PendingAdmins);
        return View("AdminList", adminListModel);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("lists/landlords")]
    public async Task<IActionResult> LandlordList(bool? isApproved = null, bool? isAssigned = null, int page = 1,
        int landlordsPerPage = 10)
    {
        var landlords = await _adminService.GetLandlordList(isApproved, isAssigned, page, landlordsPerPage);
        return View(new LandlordListModel(landlords.LandlordList, landlords.Count, isApproved, isAssigned, page,
            landlordsPerPage));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("/landlord/invite")]
    public ActionResult GetInviteLink(int landlordId)
    {
        var user = _adminService.FindUserByLandlordId(landlordId);
        if (user != null)
        {
            var flashMessageBody = $"Landlord already linked to user {user.GoogleUserName}";
            _logger.LogInformation(flashMessageBody);
            AddFlashMessage("warning", flashMessageBody);
        }
        else
        {
            var inviteLink = _adminService.FindExistingInviteLink(landlordId);
            string logMessage;
            if (string.IsNullOrEmpty(inviteLink))
            {
                logMessage = "Successfully created a new invite link";
                inviteLink = _adminService.CreateNewInviteLink(landlordId);
            }
            else
            {
                logMessage = "Landlord already has an invite link";
            }

            _logger.LogInformation(logMessage);
            var flashMessage = logMessage
                               + $": {(HttpContext.Request.IsHttps ? "https" : "http")}://{HttpContext.Request.Host}/landlord/invite/{inviteLink}";
            AddFlashMessage("success", flashMessage);
        }

        return RedirectToAction("Profile", "Landlord", new { id = landlordId });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("/landlord/invite/renew")]
    public ActionResult RenewInviteLink(int landlordId)
    {
        var user = _adminService.FindUserByLandlordId(landlordId);
        if (user != null)
        {
            _logger.LogInformation($"Landlord {landlordId} already linked to user {user.GoogleUserName}");
            AddFlashMessage("warning", $"Landlord already linked to user {user.GoogleUserName}");
        }
        else
        {
            var inviteLink = _adminService.FindExistingInviteLink(landlordId);
            if (string.IsNullOrEmpty(inviteLink))
            {
                _logger.LogInformation($"Successfully created an invite link for landlord {landlordId}");
                AddFlashMessage("success", "No existing invite link. Successfully created a new invite link");
                inviteLink = _adminService.CreateNewInviteLink(landlordId);
            }
            else
            {
                _adminService.DeleteExistingInviteLink(landlordId);
                inviteLink = _adminService.CreateNewInviteLink(landlordId);
                _logger.LogInformation($"Successfully created a new invite link for landlord {landlordId}");
                AddFlashMessage("success", "Successfully created a new invite link");
            }

            TempData["InviteLink"] = $"{inviteLink}";
        }

        return RedirectToAction("Profile", "Landlord", new { id = landlordId });
    }
}