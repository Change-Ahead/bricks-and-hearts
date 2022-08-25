using BricksAndHearts.Database;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using BricksAndHearts.ViewModels.PropertyInput;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BricksAndHearts.Controllers;

[Authorize]
[Route("/property")]
public class PropertyController : AbstractController
{
    private readonly IAzureMapsApiService _azureMapsApiService;
    private readonly IAzureStorage _azureStorage;
    private readonly ILandlordService _landlordService;
    private readonly ILogger<PropertyController> _logger;
    private readonly IPropertyService _propertyService;

    public PropertyController(IPropertyService propertyService, ILandlordService landlordService,
        IAzureMapsApiService azureMapsApiService,
        ILogger<PropertyController> logger, IAzureStorage azureStorage)
    {
        _propertyService = propertyService;
        _landlordService = landlordService;
        _azureMapsApiService = azureMapsApiService;
        _logger = logger;
        _azureStorage = azureStorage;
    }

    [HttpGet("public/{token}")]
    [AllowAnonymous]
    public async Task<ActionResult> PublicViewProperty(string token)
    {
        var dbModel = _propertyService.GetPropertyByPublicViewLink(token);
        if (dbModel == null)
        {
            return View();
        }

        var propertyViewModel = PropertyViewModel.FromDbModel(dbModel);

        var fileNames = await _azureStorage.ListFileNames("property", propertyViewModel.PropertyId);
        var imageFiles = GetFilesFromFileNames(fileNames, propertyViewModel.PropertyId);

        var landlord =
            LandlordProfileModel.FromDbModel(_propertyService.GetPropertyOwner(propertyViewModel.PropertyId));
        landlord.GoogleProfileImageUrl = _landlordService.GetLandlordProfilePicture(landlord.LandlordId!.Value);

        var propertyDetailsModel = new PropertyDetailsViewModel
        {
            Property = propertyViewModel,
            Images = imageFiles,
            Owner = landlord
        };
        return View(propertyDetailsModel);
    }

    #region Misc

    [HttpPost]
    [Authorize(Roles = "Landlord, Admin")]
    public ActionResult DeleteProperty(int propertyId)
    {
        var propDb = _propertyService.GetPropertyByPropertyId(propertyId);
        if (propDb == null)
        {
            _logger.LogWarning($"Property with Id {propertyId} does not exist");
            AddFlashMessage("danger", $"Property with Id {propertyId} does not exist");
            if (CurrentUser.LandlordId != null)
            {
                return RedirectToAction(nameof(LandlordController.ViewProperties), "Landlord",
                    new { id = CurrentUser.LandlordId });
            }

            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        if (CurrentUser.IsAdmin == false && CurrentUser.LandlordId != propDb.LandlordId)
        {
            _logger.LogWarning(
                $"User {CurrentUser.Id} does not have access to any property with ID {propertyId}.");
            return StatusCode(403);
        }

        var landlordId = propDb.LandlordId;
        _propertyService.DeleteProperty(propDb);
        _azureStorage.DeleteContainer("property", propertyId);

        AddFlashMessage("danger", $"Successfully deleted property with Id {propertyId}");
        return RedirectToAction("ViewProperties", "Landlord", new { id = landlordId });
    }

    [HttpGet("/property/{propertyId:int}/view")]
    public async Task<ActionResult> ViewProperty(int propertyId)
    {
        var model = _propertyService.GetPropertyByPropertyId(propertyId);
        if (model == null)
        {
            _logger.LogWarning("Property with ID {PropertyId} does not exist", propertyId);
            return RedirectToAction("Error", "Home", new { status = 404 });
        }

        if (!(CurrentUser.IsAdmin || CurrentUser.LandlordId == model.LandlordId))
        {
            return StatusCode(403);
        }

        model.PublicViewLink ??= _propertyService.CreatePublicViewLink(model.Id);

        var propertyViewModel = PropertyViewModel.FromDbModel(model);

        var owner = LandlordProfileModel.FromDbModel(_propertyService.GetPropertyOwner(propertyId));
        var fileNames = await _azureStorage.ListFileNames("property", propertyId);
        var imageFiles = GetFilesFromFileNames(fileNames, propertyId);
        var propertyDetailsModel = new PropertyDetailsViewModel { Property = propertyViewModel, Owner = owner, Images = imageFiles };

        return View(propertyDetailsModel);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("/admin/lists/properties")]
    public async Task<IActionResult> PropertyList(string sortBy, string? target, int page = 1, int propPerPage = 10)
    {
        var properties = await _propertyService.GetPropertyList(sortBy, target, page, propPerPage);

        if (properties.Count == 0 && sortBy == "Location")
        {
            _logger.LogWarning($"Failed to find postcode {target}");
            AddFlashMessage("warning", $"Failed to sort property using postcode {target}: invalid postcode");
            sortBy = "";
            properties = await _propertyService.GetPropertyList(sortBy, target, page, propPerPage);
        }

        var propertyViewModels = properties.PropertyList.Select(PropertyViewModel.FromDbModel);
        var propertyDetailsModels = new List<PropertyDetailsViewModel>();

        foreach (var property in propertyViewModels)
        {
            var owner = LandlordProfileModel.FromDbModel(_propertyService.GetPropertyOwner(property.PropertyId));
            var fileNames = await _azureStorage.ListFileNames("property", property.PropertyId);
            var imageFiles = GetFilesFromFileNames(fileNames, property.PropertyId);
            propertyDetailsModels.Add(new PropertyDetailsViewModel
                { Property = property, Images = imageFiles, Owner = owner });
        }

        TempData["FullWidthPage"] = true;
        return View("~/Views/Admin/PropertyList.cshtml",
            new PropertyListModel(propertyDetailsModels, properties.Count, null!, page, sortBy, target));
    }

    #endregion

    #region Add/Edit Property

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet("{propertyId:int}/{operationType:regex(^(add|edit)$)}/step/1")]
    public ActionResult PropertyInputStepOnePostcode([FromRoute] string operationType,
        [FromRoute] int propertyId, [FromQuery] int landlordId)
    {
        var property = _propertyService.GetPropertyByPropertyId(propertyId);

        if (!(CurrentUser.IsAdmin
              || (property != null && property.LandlordId == CurrentUser.LandlordId)
              || (property == null && landlordId == CurrentUser.LandlordId)))
        {
            return StatusCode(403);
        }

        property ??= new PropertyDbModel
        {
            Id = propertyId,
            LandlordId = landlordId
        };

        var model = new PropertyInputModelInitialAddress
        {
            IsEdit = OperationTypeToIsEdit(operationType)
        };
        model.InitialiseViewModel(property);
        return View("PropertyInputForm/InitialAddress", model);
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpPost("{propertyId:int}/{operationType:regex(^(add|edit)$)}/step/1")]
    public async Task<ActionResult> PropertyInputStepOnePostcode([FromForm] PropertyInputModelInitialAddress model,
        [FromRoute] int propertyId, [FromRoute] string operationType, [FromQuery] int landlordId)
    {
        if (!ModelState.IsValid)
        {
            return View("PropertyInputForm/InitialAddress", model);
        }

        var property = _propertyService.GetPropertyByPropertyId(propertyId);

        if (!(CurrentUser.IsAdmin
              || (property != null && property.LandlordId == CurrentUser.LandlordId)
              || (property == null && landlordId == CurrentUser.LandlordId)))
        {
            return StatusCode(403);
        }

        var propertyView = model.FormToViewModel();

        await _azureMapsApiService.AutofillAddress(propertyView);

        if (property != null)
        {
            await _propertyService.UpdateProperty(propertyId, propertyView);
        }
        else
        {
            propertyId = await _propertyService.AddNewProperty(landlordId, propertyView);
        }

        return RedirectToAction("PropertyInputStepTwoAddress", "Property",
            new { operationType, propertyId });
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet("{propertyId:int}/{operationType:regex(^(add|edit)$)}/step/2")]
    public ActionResult PropertyInputStepTwoAddress([FromRoute] int propertyId, [FromRoute] string operationType)
    {
        const string viewName = "PropertyInputForm/FullAddress";
        var model = new PropertyInputModelAddress
        {
            IsEdit = OperationTypeToIsEdit(operationType)
        };

        return StandardPropertyInputGetMethod(model, propertyId, viewName);
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpPost("{propertyId:int}/{operationType:regex(^(add|edit)$)}/step/2")]
    public async Task<ActionResult> PropertyInputStepTwoAddress([FromForm] PropertyInputModelAddress model,
        [FromRoute] int propertyId, [FromRoute] string operationType)
    {
        const string nextActionName = "PropertyInputStepThreeDetails";
        const string currentViewName = "PropertyInputForm/FullAddress";

        return await StandardPropertyInputPostMethod(model, propertyId, operationType, nextActionName,
            currentViewName);
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet("{propertyId:int}/{operationType:regex(^(add|edit)$)}/step/3")]
    public ActionResult PropertyInputStepThreeDetails([FromRoute] int propertyId, [FromRoute] string operationType)
    {
        const string viewName = "PropertyInputForm/Details";
        var model = new PropertyInputModelDetails
        {
            IsEdit = OperationTypeToIsEdit(operationType)
        };

        return StandardPropertyInputGetMethod(model, propertyId, viewName);
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpPost("{propertyId:int}/{operationType:regex(^(add|edit)$)}/step/3")]
    public async Task<ActionResult> PropertyInputStepThreeDetails([FromForm] PropertyInputModelDetails model,
        [FromRoute] int propertyId,
        [FromRoute] string operationType)
    {
        const string nextActionName = "PropertyInputStepFourDescription";
        const string currentViewName = "PropertyInputForm/Details";

        return await StandardPropertyInputPostMethod(model, propertyId, operationType, nextActionName,
            currentViewName);
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet("{propertyId:int}/{operationType:regex(^(add|edit)$)}/step/4")]
    public ActionResult PropertyInputStepFourDescription([FromRoute] int propertyId, [FromRoute] string operationType)
    {
        const string viewName = "PropertyInputForm/Description";
        var model = new PropertyInputModelDescription
        {
            IsEdit = OperationTypeToIsEdit(operationType)
        };

        return StandardPropertyInputGetMethod(model, propertyId, viewName);
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpPost("{propertyId:int}/{operationType:regex(^(add|edit)$)}/step/4")]
    public async Task<ActionResult> PropertyInputStepFourDescription([FromForm] PropertyInputModelDescription model,
        [FromRoute] int propertyId, [FromRoute] string operationType)
    {
        const string nextActionName = "PropertyInputStepFiveTenantPreferences";
        const string currentViewName = "PropertyInputForm/Description";

        return await StandardPropertyInputPostMethod(model, propertyId, operationType, nextActionName,
            currentViewName);
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet("{propertyId:int}/{operationType:regex(^(add|edit)$)}/step/5")]
    public ActionResult PropertyInputStepFiveTenantPreferences([FromRoute] int propertyId,
        [FromRoute] string operationType)
    {
        const string viewName = "PropertyInputForm/TenantPreferences";
        var model = new PropertyInputModelTenantPreferences
        {
            IsEdit = OperationTypeToIsEdit(operationType)
        };

        return StandardPropertyInputGetMethod(model, propertyId, viewName);
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpPost("{propertyId:int}/{operationType:regex(^(add|edit)$)}/step/5")]
    public async Task<ActionResult> PropertyInputStepFiveTenantPreferences(
        [FromForm] PropertyInputModelTenantPreferences model,
        [FromRoute] int propertyId, [FromRoute] string operationType)
    {
        const string nextActionName = "PropertyInputStepSixAvailability";
        const string currentViewName = "PropertyInputForm/TenantPreferences";

        return await StandardPropertyInputPostMethod(model, propertyId, operationType, nextActionName,
            currentViewName);
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet("{propertyId:int}/{operationType:regex(^(add|edit)$)}/step/6")]
    public ActionResult PropertyInputStepSixAvailability([FromRoute] int propertyId, [FromRoute] string operationType)
    {
        const string viewName = "PropertyInputForm/Availability";
        var model = new PropertyInputModelAvailability
        {
            IsEdit = OperationTypeToIsEdit(operationType)
        };

        return StandardPropertyInputGetMethod(model, propertyId, viewName);
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpPost("{propertyId:int}/{operationType:regex(^(add|edit)$)}/step/6")]
    public async Task<ActionResult> PropertyInputStepSixAvailability([FromForm] PropertyInputModelAvailability model,
        [FromRoute] int propertyId, [FromRoute] string operationType)
    {
        if (!ModelState.IsValid)
        {
            return View("PropertyInputForm/Availability", model);
        }

        if (!_propertyService.IsUserAdminOrCorrectLandlord(CurrentUser, propertyId))
        {
            return StatusCode(403);
        }

        var propertyView = model.FormToViewModel();

        await _propertyService.UpdateProperty(propertyId, propertyView);

        return RedirectToAction("ViewProperty", "Property", new { propertyId });
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet("{propertyId:int}/cancel")]
    public async Task<ActionResult> PropertyInputCancel([FromRoute] int propertyId, int landlordId)
    {
        var property = _propertyService.GetPropertyByPropertyId(propertyId);

        if (property == null)
        {
            return RedirectToAction("ViewProperties", "Landlord", new { id = landlordId });
        }

        if (!_propertyService.IsUserAdminOrCorrectLandlord(CurrentUser, propertyId))
        {
            return StatusCode(403);
        }

        _propertyService.DeleteProperty(property);
        await _azureStorage.DeleteContainer("property", property.Id);

        return RedirectToAction("ViewProperties", "Landlord", new { id = landlordId });
    }

    private static bool OperationTypeToIsEdit(string input)
    {
        return input.ToUpper() != "ADD";
    }

    private async Task<ActionResult> StandardPropertyInputPostMethod(PropertyInputModelBase model, int propertyId,
        string operationType, string nextActionName, string currentViewName)
    {
        if (!ModelState.IsValid)
        {
            return View(currentViewName, model);
        }

        if (!_propertyService.IsUserAdminOrCorrectLandlord(CurrentUser, propertyId))
        {
            return StatusCode(403);
        }

        var propertyView = model.FormToViewModel();

        // Update the property's record with the values entered at this step
        await _propertyService.UpdateProperty(propertyId, propertyView);

        return operationType == "add"
            ? RedirectToAction(nextActionName, "Property", new { propertyId, operationType })
            : RedirectToAction("ViewProperty", "Property", new { propertyId });
    }

    private ActionResult StandardPropertyInputGetMethod(PropertyInputModelBase model, int propertyId, string viewName)
    {
        var property = _propertyService.GetPropertyByPropertyId(propertyId);

        if (property == null)
        {
            return StatusCode(404);
        }

        if (!_propertyService.IsUserAdminOrCorrectLandlord(CurrentUser, propertyId))
        {
            return StatusCode(403);
        }

        model.InitialiseViewModel(property);
        return View(viewName, model);
    }

    #endregion

    #region Images

    public List<ImageFileUrlModel> GetFilesFromFileNames(List<string> fileNames, int propertyId)
    {
        return fileNames.Select(fileName =>
            {
                var url = Url.Action(nameof(GetImage), new { propertyId, fileName })!;
                return new ImageFileUrlModel(fileName, url);
            })
            .ToList();
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet("{propertyId:int}/images/{fileName}")]
    public async Task<IActionResult> GetImage(int propertyId, string fileName)
    {
        if (!_propertyService.IsUserAdminOrCorrectLandlord(CurrentUser, propertyId))
        {
            return StatusCode(403);
        }

        var image = await _azureStorage.DownloadFile("property", propertyId, fileName);
        if (image == (null, null))
        {
            return StatusCode(404);
        }

        var data = image.data;
        var fileType = image.fileType;
        return File(data!, $"image/{fileType}");
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpPost("images/upload")]
    public async Task<IActionResult> AddPropertyImages(List<IFormFile> images, int propertyId)
    {
        if (!_propertyService.IsUserAdminOrCorrectLandlord(CurrentUser, propertyId))
        {
            return StatusCode(403);
        }

        foreach (var image in images)
        {
            var isImageResult = _azureStorage.IsImage(image.FileName);
            if (!isImageResult.isImage)
            {
                _logger.LogInformation($"Failed to upload {image.FileName}: not in a recognised image format");
                AddFlashMessage("danger",
                    $"{image.FileName} is not in a recognised image format. Please submit your images in one of the following formats: {isImageResult.imageExtString}");
            }
            else
            {
                if (image.Length > 0)
                {
                    var message = await _azureStorage.UploadFile(image, "property", propertyId);
                    _logger.LogInformation($"Successfully uploaded {image.FileName}");
                    AddFlashMessage("success", message);
                }
                else
                {
                    _logger.LogInformation($"Failed to upload {image.FileName}: has length zero.");
                    AddFlashMessage("danger", $"{image.FileName} contains no data, and so has not been uploaded");
                }
            }
        }

        return RedirectToAction("ViewProperty", "Property", new { propertyId });
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpPost("images/delete")]
    public async Task<IActionResult> DeletePropertyImage(int propertyId, string fileName)
    {
        if (!_propertyService.IsUserAdminOrCorrectLandlord(CurrentUser, propertyId))
        {
            return StatusCode(403);
        }

        await _azureStorage.DeleteFile("property", propertyId, fileName);
        return RedirectToAction("ViewProperty", "Property", new { propertyId });
    }

    #endregion
}