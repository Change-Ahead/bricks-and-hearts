using BricksAndHearts.Database;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using BricksAndHearts.ViewModels.PropertyInput;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BricksAndHearts.Controllers;

[Authorize]
[Route("/property")]
public class PropertyController : AbstractController
{
    private readonly IPropertyService _propertyService;
    private readonly ILandlordService _landlordService;
    private readonly IAzureMapsApiService _azureMapsApiService;
    private readonly IAzureStorage _azureStorage;
    private readonly ILogger<PropertyController> _logger;
    private readonly IPropertyService _propertyService;

    public PropertyController(IPropertyService propertyService,ILandlordService landlordService, IAzureMapsApiService azureMapsApiService,
        ILogger<PropertyController> logger, IAzureStorage azureStorage)
    {
        _propertyService = propertyService;
        _landlordService = landlordService;
        _azureMapsApiService = azureMapsApiService;
        _logger = logger;
        _azureStorage = azureStorage;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("SortPropertiesByLocation")]
    public async Task<IActionResult> SortPropertiesByLocation(string postcode, int page = 1, int propPerPage = 10)
    {
        var properties = await _propertyService.SortPropertiesByLocation(postcode, page, propPerPage);

        if (properties == null)
        {
            _logger.LogWarning($"Failed to find postcode {postcode}");
            AddFlashMessage("warning", $"Failed to sort property using postcode {postcode}: invalid postcode");
            return RedirectToAction("SortProperties", "Property", new { sortBy = "Availability" });
        }

        _logger.LogInformation("Successfully sorted by location");
        var listOfProperties = properties.Select(PropertyViewModel.FromDbModel).Skip((page - 1) * propPerPage)
            .Take(propPerPage).ToList();

        TempData["FullWidthPage"] = true;

        return View("~/Views/Admin/PropertyList.cshtml",
            new PropertiesDashboardViewModel(listOfProperties, listOfProperties.Count, null!, page, "Location"));
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
                return RedirectToAction("ViewProperties", "Landlord", new { id = CurrentUser.LandlordId });
            }

            return RedirectToAction("Index", "Home");
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

        var fileNames = await _azureStorage.ListFileNames("property", propertyId);
        var imageFiles = GetFilesFromFileNames(fileNames, propertyId);
        var propertyDetailsModel = new PropertyDetailsViewModel { Property = propertyViewModel, Images = imageFiles };

        return View(propertyDetailsModel);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("PropertyList")]
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

        var listOfProperties = properties.PropertyList.Select(PropertyViewModel.FromDbModel).ToList();
        TempData["FullWidthPage"] = true;
        return View("~/Views/Admin/PropertyList.cshtml",
            new PropertyListModel(listOfProperties, properties.Count, null!, page, sortBy, target));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("/admin/get-public-view-link/{propertyId:int}")]
    public ActionResult GetPublicViewLink(int propertyId)
    {
        var property = _propertyService.GetPropertyByPropertyId(propertyId);
        if (property == null) // If property does not exist
        {
            var flashMessageBody = $"Property with ID: {propertyId} does not exist";
            _logger.LogInformation(flashMessageBody);
            AddFlashMessage("warning", flashMessageBody);
        }
        else
        {
            var publicViewLink = property.PublicViewLink;
            string flashMessageBody;
            if (string.IsNullOrEmpty(publicViewLink))
            {
                flashMessageBody = "Successfully created a new public view link";
                publicViewLink = _propertyService.CreateNewPublicViewLink(propertyId);
                _logger.LogInformation("Created public view link for property {PropertyId}: {PublicViewLink}",
                    propertyId, publicViewLink);
            }
            else
            {
                flashMessageBody = "Property already has a public view link";
            }

            var baseUrl = HttpContext.Request.GetUri().Authority;
            _logger.LogInformation(flashMessageBody);
            AddFlashMessage("success",
                flashMessageBody + ": " + baseUrl + $"/public/propertyid/{propertyId}/{publicViewLink}");
        }

        return RedirectToAction("ViewProperty", "Property", new { propertyId });
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

        var model = new PropertyInputModelAddress
        {
            IsEdit = OperationTypeToIsEdit(operationType),
            Step = 1
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
            return RedirectToAction("PropertyInputStepOnePostcode", "Property",
                new
                {
                    propertyId, operationType, landlordId
                });
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
            _propertyService.UpdateProperty(propertyId, propertyView);
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
    public ActionResult PropertyInputStepTwoAddress([FromRoute] string operationType,
        [FromRoute] int propertyId)
    {
        const string viewName = "PropertyInputForm/FullAddress";
        var model = new PropertyInputModelAddress
        {
            Step = 2,
            IsEdit = OperationTypeToIsEdit(operationType)
        };

        return StandardPropertyInputGetMethod(model, propertyId, viewName);
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpPost("{propertyId:int}/{operationType:regex(^(add|edit)$)}/step/2")]
    public ActionResult PropertyInputStepTwoAddress([FromForm] PropertyInputModelAddress model,
        [FromRoute] int propertyId, [FromRoute] string operationType)
    {
        const string nextActionName = "PropertyInputStepThreeDetails";

        return StandardPropertyInputPostMethod(model, propertyId, operationType, nextActionName);
    }

        // Update the property's record with the values entered at this step
        await _propertyService.UpdateProperty(propertyId, propertyView);

        //Redirect to next step (could be subverted with a bool for the edit function?)
        return RedirectToAction("PropertyInputStepThreeDetails", "Property",
            new { landlordId, operationType, propertyId });
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet("{propertyId:int}/{operationType:regex(^(add|edit)$)}/step/3")]
    public ActionResult PropertyInputStepThreeDetails([FromRoute] string operationType, [FromRoute] int propertyId)
    {
        const string viewName = "PropertyInputForm/Details";
        var model = new PropertyInputModelDetails
        {
            Step = 3,
            IsEdit = OperationTypeToIsEdit(operationType)
        };

        return StandardPropertyInputGetMethod(model, propertyId, viewName);
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpPost("{propertyId:int}/{operationType:regex(^(add|edit)$)}/step/3")]
    public ActionResult PropertyInputStepThreeDetails([FromForm] PropertyInputModelDetails model,
        [FromRoute] int propertyId,
        [FromRoute] string operationType)
    {
        const string nextActionName = "PropertyInputStepFourDescription";

        return StandardPropertyInputPostMethod(model, propertyId, operationType, nextActionName);
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet("{propertyId:int}/{operationType:regex(^(add|edit)$)}/step/4")]
    public ActionResult PropertyInputStepFourDescription([FromRoute] int propertyId, [FromRoute] string operationType)
    {
        const string viewName = "PropertyInputForm/Description";
        var model = new PropertyInputModelDescription
        {
            Step = 4,
            IsEdit = OperationTypeToIsEdit(operationType)
        };

        return StandardPropertyInputGetMethod(model, propertyId, viewName);
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpPost("{propertyId:int}/{operationType:regex(^(add|edit)$)}/step/4")]
    public ActionResult PropertyInputStepFourDescription([FromForm] PropertyInputModelDescription model,
        [FromRoute] int propertyId, [FromRoute] string operationType)
    {
        const string nextActionName = "PropertyInputStepFiveTenantPreferences";

        return StandardPropertyInputPostMethod(model, propertyId, operationType, nextActionName);
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet("{propertyId:int}/{operationType:regex(^(add|edit)$)}/step/5")]
    public ActionResult PropertyInputStepFiveTenantPreferences([FromRoute] int propertyId,
        [FromRoute] string operationType)
    {
        const string viewName = "PropertyInputForm/TenantPreferences";
        var model = new PropertyInputModelTenantPreferences
        {
            Step = 5,
            IsEdit = OperationTypeToIsEdit(operationType)
        };

        return StandardPropertyInputGetMethod(model, propertyId, viewName);
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpPost("{propertyId:int}/{operationType:regex(^(add|edit)$)}/step/5")]
    public ActionResult PropertyInputStepFiveTenantPreferences([FromForm] PropertyInputModelTenantPreferences model,
        [FromRoute] int propertyId, [FromRoute] string operationType)
    {
        const string nextActionName = "PropertyInputStepSixAvailability";

        return StandardPropertyInputPostMethod(model, propertyId, operationType, nextActionName);
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet("{propertyId:int}/{operationType:regex(^(add|edit)$)}/step/6")]
    public ActionResult PropertyInputStepSixAvailability([FromRoute] int propertyId, [FromRoute] string operationType)
    {
        const string viewName = "PropertyInputForm/Availability";
        var model = new PropertyInputModelAvailability
        {
            Step = 6,
            IsEdit = OperationTypeToIsEdit(operationType)
        };

        return StandardPropertyInputGetMethod(model, propertyId, viewName);
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpPost("{propertyId:int}/{operationType:regex(^(add|edit)$)}/step/6")]
    public ActionResult PropertyInputStepSixAvailability([FromForm] PropertyInputModelAvailability model,
        [FromRoute] int propertyId, [FromRoute] string operationType)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction("PropertyInputStepSixAvailability", "Property",
                new
                {
                    propertyId,
                    operationType
                });
        }

        var propertyView = model.FormToViewModel();

        if (!_propertyService.IsUserAdminOrCorrectLandlord(CurrentUser, propertyId))
        {
            return StatusCode(403);
        }

        _propertyService.UpdateProperty(propertyId, propertyView);

        return RedirectToAction("ViewProperty", "Property", new { propertyId });
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet("{operationType:regex(^(add|edit)$)})/{propertyId:int}/step/{step:int}/back")]
    public ActionResult PropertyInputBack([FromRoute] int step,
        [FromRoute] string operationType, [FromRoute] int propertyId, [FromQuery] int landlordId)
    {
        return step switch
        {
            1 => RedirectToAction("PropertyInputStepOnePostcode", new { propertyId, operationType, landlordId }),
            2 => RedirectToAction("PropertyInputStepTwoAddress", new { propertyId, operationType }),
            3 => RedirectToAction("PropertyInputStepThreeDetails", new { propertyId, operationType }),
            4 => RedirectToAction("PropertyInputStepFourDescription", new { propertyId, operationType }),
            5 => RedirectToAction("PropertyInputStepFiveTenantPreferences",
                new { propertyId, operationType }),
            6 => RedirectToAction("PropertyInputStepSixAvailability", new { propertyId, operationType }),
            _ => StatusCode(404)
        };
    }

        _propertyService.UpdateProperty(propertyId, newPropertyModel, false);
        // Finished adding property, so go to View Properties page
        return RedirectToAction("ViewProperties", "Landlord", new { id = landlordId });
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet(
        @"/landlord/{landlordId:int}/property/{operationType:regex(^(add|edit)$)})/{propertyId:int}/cancel")]
    public async Task<ActionResult> PropertyInputCancel([FromRoute] int propertyId, [FromRoute] int landlordId,
        [FromRoute] string operationType)
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

    private bool OperationTypeToIsEdit(string input)
    {
        return input.ToUpper() != "ADD";
    }

    private ActionResult StandardPropertyInputPostMethod(PropertyInputModelBase model, int propertyId,
        string operationType, string nextActionName)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction("PropertyInputStepTwoAddress", "Property",
                new
                {
                    propertyId,
                    operationType
                });
        }

        if (!_propertyService.IsUserAdminOrCorrectLandlord(CurrentUser, propertyId))
        {
            return StatusCode(403);
        }

        var propertyView = model.FormToViewModel();


        _propertyService.UpdateProperty(propertyId, propertyView);

        if (operationType == "add")
        {
            return RedirectToAction(nextActionName, "Property", new { propertyId, operationType });
        }

        return RedirectToAction("ViewProperty", "Property", new { propertyId });
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
                var url = Url.Action("GetImage", new { propertyId, fileName })!;
                return new ImageFileUrlModel(fileName, url);
            })
            .ToList();
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet("{propertyId:int}/{fileName}")]
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
    [HttpPost("addImage")]
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
    [HttpPost("deleteImage")]
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
}