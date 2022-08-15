#region

using BricksAndHearts.Auth;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;

#endregion

namespace BricksAndHearts.Controllers;

public class AdminController : AbstractController
{
    private readonly IAdminService _adminService;
    private readonly ILandlordService _landlordService;
    private readonly IPropertyService _propertyService;
    private readonly ICsvImportService _csvImportService;
    private readonly ILogger<AdminController> _logger;
    private readonly IMailService _mailService;

    public AdminController(ILogger<AdminController> logger, IAdminService adminService,
        ILandlordService landlordService, IPropertyService propertyService, ICsvImportService csvImportService, IMailService mailService)
    {
        _logger = logger;
        _adminService = adminService;
        _landlordService = landlordService;
        _propertyService = propertyService;
        _csvImportService = csvImportService;
        _mailService = mailService;
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
        _logger.LogInformation($"Successfully requested admin access for user {user.Id}");
        AddFlashMessage("success", "Successfully requested admin access.");
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
    [HttpPost]
    public ActionResult AcceptAdminRequest(int userToAcceptId)
    {
        _adminService.ApproveAdminAccessRequest(userToAcceptId);
        _logger.LogInformation($"Admin request of user {userToAcceptId} approved by user {GetCurrentUser().Id}");
        return RedirectToAction("GetAdminList");
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public ActionResult RejectAdminRequest(int userToRejectId)
    {
        _adminService.RejectAdminAccessRequest(userToRejectId);
        _logger.LogInformation($"Admin request of user {userToRejectId} rejected by user {GetCurrentUser().Id}");
        return RedirectToAction("GetAdminList");
    }
    
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public ActionResult RemoveAdmin(int userToRemoveId)
    {
        if (userToRemoveId != GetCurrentUser().Id)
        {
            _adminService.RemoveAdmin(userToRemoveId);
            _logger.LogInformation($"Admin status of user {userToRemoveId} revoked by user {GetCurrentUser().Id}");
        }
        else
        {
            _logger.LogWarning($"User {userToRemoveId} may not remove their own admin status");
            AddFlashMessage("warning", "You may not remove your own admin status");
        }
        return RedirectToAction("GetAdminList");
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult> GetAdminList()
    {
        var adminLists = await _adminService.GetAdminLists();
        AdminListModel adminListModel = new AdminListModel(adminLists.CurrentAdmins, adminLists.PendingAdmins);
        return View("AdminList", adminListModel);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> LandlordList(LandlordListModel landlordListModel)
    {
        landlordListModel.LandlordList = await _adminService.GetLandlordList(landlordListModel);
        return View(landlordListModel);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> TenantList(TenantListModel tenantListModel)
    {
        tenantListModel.TenantList = await _adminService.GetTenantList(tenantListModel.Filter);
        return View("TenantList", tenantListModel);
    }
    
    [Authorize(Roles = "Admin")]
    [HttpGet]
    [Route("{currentPropertyId:int}/Matches")]
    public async Task<IActionResult> TenantMatchList(int currentPropertyId)
    {
        var currentProperty = PropertyViewModel.FromDbModel(_propertyService.GetPropertyByPropertyId(currentPropertyId)!);
        
        var tenantMatchListModel = new TenantMatchListModel{
            TenantList = await _adminService.GetNearestTenantsToProperty(currentProperty),
            CurrentProperty = currentProperty
        };
        return View("TenantMatchList", tenantMatchListModel);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
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
            var flashMessage = logMessage + $": {(HttpContext.Request.IsHttps ? "https" : "http")}://{HttpContext.Request.Host}/invite/{inviteLink}";
            AddFlashMessage("success", flashMessage);
        }

        return RedirectToAction("Profile", "Landlord", new { id = landlordId });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
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

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public ActionResult GetSampleTenantCSV()
    {
        return File("~/TenantImportCSVTemplate.csv", "text/csv");
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
            _logger.LogWarning($"{fileName} has length zero.");
            AddFlashMessage("danger", $"{fileName} contains no data. Please upload a file containing the tenant data you would like to import.");
            return RedirectToAction(nameof(TenantList));
        }
        if (fileName.Substring(fileName.Length - 3) != "csv")
        {
            _logger.LogWarning($"{fileName} not a CSV file.");
            AddFlashMessage("danger", $"{fileName} is not a CSV file. Please upload your data as a CSV file.");
            return RedirectToAction(nameof(TenantList));
        }
        
        var flashResponse = _csvImportService.CheckIfImportWorks(csvFile);
        for (int i = 0; i < flashResponse.FlashMessages.Count(); i++)
        {
            AddFlashMessage(flashResponse.FlashTypes[i], flashResponse.FlashMessages[i]);
        }
        
        if (flashResponse.FlashTypes.Contains("danger"))
        {
            return RedirectToAction(nameof(TenantList));
        }
        
        var furtherFlashResponse = await _csvImportService.ImportTenants(csvFile);
        for (int i = 0; i < furtherFlashResponse.FlashMessages.Count(); i++)
        {
            AddFlashMessage(furtherFlashResponse.FlashTypes[i],  furtherFlashResponse.FlashMessages[i]);
        }
        
        if (furtherFlashResponse.FlashMessages.Count() == 0)
        {
            _logger.LogInformation("Successfuly imported all tenant data.");
            AddFlashMessage("success", "Successfully imported all tenant data.");
        }
        return RedirectToAction(nameof(TenantList));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [Route("{currentPropertyId:int}/Matches")]
    public ActionResult SendMatchLinkEmail(string propertyLink, string tenantEmail, int currentPropertyId)
    {
        var addressToSendTo = new List<string> { tenantEmail };
        _mailService.TrySendMsgInBackground(propertyLink, tenantEmail, addressToSendTo);
        FlashMessage(_logger, ("Successfully emailed tenant", "success", $"successfully emailed {tenantEmail}"));
        return RedirectToAction(nameof(TenantMatchList), new {currentPropertyId});
    }
}