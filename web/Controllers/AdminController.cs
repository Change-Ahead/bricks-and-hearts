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
    private readonly ICsvImportService _csvImportService;
    private readonly ILandlordService _landlordService;
    private readonly ILogger<AdminController> _logger;
    private readonly IPropertyService _propertyService;

    public AdminController(ILogger<AdminController> logger, IAdminService adminService,
        ILandlordService landlordService, IPropertyService propertyService, ICsvImportService csvImportService)
    {
        _logger = logger;
        _adminService = adminService;
        _landlordService = landlordService;
        _propertyService = propertyService;
        _csvImportService = csvImportService;
    }

    [HttpGet]
    [Route("/admin")]
    public IActionResult AdminDashboard()
    {
        var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
        var viewModel =
            new AdminDashboardViewModel(_landlordService.CountLandlords(), _propertyService.CountProperties());
        if (isAuthenticated)
        {
            viewModel.CurrentUser = GetCurrentUser();
        }

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
            return RedirectToAction(nameof(AdminDashboard));
        }

        _adminService.RequestAdminAccess(user);
        FlashRequestSuccess(_logger, user, "requested admin access");
        return RedirectToAction(nameof(AdminDashboard));
    }

    public IActionResult CancelAdminAccessRequest()
    {
        var user = GetCurrentUser();
        if (user.IsAdmin)
        {
            LoggerAlreadyAdminWarning(_logger, user);

            return RedirectToAction(nameof(AdminDashboard));
        }

        _adminService.CancelAdminAccessRequest(user);

        FlashRequestSuccess(_logger, user, "cancelled admin access request");

        return RedirectToAction(nameof(AdminDashboard));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult> GetAdminList()
    {
        var adminLists = await _adminService.GetAdminLists();

        var adminListModel = new AdminListModel(adminLists.CurrentAdmins, adminLists.PendingAdmins);

        return View("AdminList", adminListModel);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> LandlordList(string? approvalStatus = "")
    {
        var landlordListModel = new LandlordListModel();
        landlordListModel.LandlordDisplayList = await _adminService.GetLandlordDisplayList(approvalStatus!);
        return View(landlordListModel);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> TenantList()
    {
        var tenantListModel = new TenantListModel();
        tenantListModel.TenantList = await _adminService.GetTenantList();
        return View(tenantListModel);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public ActionResult GetInviteLink(int landlordId)
    {
        var user = _adminService.FindUserByLandlordId(landlordId);
        if (user != null) // If landlord already linked to user
        {
            var flashMessageBody = $"Landlord already linked to user {user.GoogleUserName}";
            FlashMessage(_logger, (flashMessageBody, "warning", flashMessageBody));
        }
        else
        {
            var inviteLink = _adminService.FindExistingInviteLink(landlordId);
            string flashMessageBody;
            if (string.IsNullOrEmpty(inviteLink))
            {
                flashMessageBody = "Successfully created a new invite link";
                inviteLink = _adminService.CreateNewInviteLink(landlordId);
            }
            else
            {
                flashMessageBody = "Landlord already has an invite link";
            }

            FlashMessage(_logger, (flashMessageBody, "success", flashMessageBody + $": {inviteLink}"));
        }

        return RedirectToAction("Profile", "Landlord", new { id = landlordId });
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

        return RedirectToAction("Profile", "Landlord", new { id = landlordId });
    }

    private void LoggerAlreadyAdminWarning(ILogger logger, BricksAndHeartsUser user)
    {
        FlashMessage(logger,
            ($"User {user.Id} already an admin",
                "danger",
                "Already an admin"));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public ActionResult AcceptAdminRequest(int userToAcceptId)
    {
        if (_adminService.GetUserFromId(userToAcceptId) == null)
        {
            return View("Error", new ErrorViewModel());
        }

        _adminService.ApproveAdminAccessRequest(userToAcceptId);

        return RedirectToAction("GetAdminList");
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public ActionResult RejectAdminRequest(int userToAcceptId)
    {
        _adminService.RejectAdminAccessRequest(userToAcceptId);

        return RedirectToAction("GetAdminList");
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetFilteredTenants(TenantListModel tenantListModel)
    {
        tenantListModel.TenantList = await _adminService.GetTenantDbModelsFromFilter(tenantListModel.Filters);
        return View("TenantList", tenantListModel);
    }
    
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult> ImportTenants(IFormFile csvFile)
    {
        if (csvFile == null)
        {
            return RedirectToAction(nameof(TenantList));
        }
        var fileName = csvFile.FileName;
        if (csvFile.Length == 0)
        {
            FlashMessage(_logger,
                ($"{fileName} has length zero.", "danger",
                    $"{fileName} contains no data. Please upload a file containing the tenant data you would like to import."));
            return RedirectToAction(nameof(TenantList));
        }

        if (fileName.Substring(fileName.Length - 3) != "csv")
        {
            FlashMessage(_logger,
                ($"{fileName} not a CSV file.", "danger",
                    $"{fileName} is not a CSV file. Please upload your data as a CSV file."));
            return RedirectToAction(nameof(TenantList));
        }

        var (columnOrder, flashResponse) = _csvImportService.CheckIfImportWorks(csvFile);
        if (flashResponse.FlashTypes.Contains("danger"))
        {
            FlashMultipleMessages(flashResponse.FlashTypes, flashResponse.FlashMessages);
            return RedirectToAction(nameof(TenantList));
        }

        var furtherFlashResponse = await _csvImportService.ImportTenants(csvFile, columnOrder, flashResponse);
        if (furtherFlashResponse.FlashMessages.Count() == 0)
        {
            FlashMessage(_logger,
                ("Successfuly imported all tenant data.", "success", "Successfully imported all tenant data."));
        }

        FlashMultipleMessages(furtherFlashResponse.FlashTypes, furtherFlashResponse.FlashMessages);
        return RedirectToAction(nameof(TenantList));
    }
}