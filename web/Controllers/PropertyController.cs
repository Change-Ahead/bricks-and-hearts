using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using BricksAndHearts.Database;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
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
            FlashMessage(_logger,
                ($"Failed to find postcode {postcode}", "warning",
                    $"Failed to sort property using postcode {postcode}: invalid postcode"));
            return RedirectToAction("SortProperties", "Property", new { sortBy = "Availability" });
        }

        _logger.LogInformation("Successfully sorted by location");
        var listOfProperties = properties.Select(PropertyViewModel.FromDbModel).ToList();

        return View("~/Views/Admin/PropertyList.cshtml",
            new PropertiesDashboardViewModel(listOfProperties.Skip((page - 1) * propPerPage).Take(propPerPage).ToList(),
                listOfProperties.Count, null!, page, "Location"));
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
            if (GetCurrentUser().LandlordId != null)
            {
                return RedirectToAction("ViewProperties", "Landlord", new { id = GetCurrentUser().LandlordId });
            }

            return RedirectToAction("Index", "Home");
        }

        if (GetCurrentUser().IsAdmin == false && GetCurrentUser().LandlordId != propDb.LandlordId)
        {
            _logger.LogWarning(
                $"User {GetCurrentUser().Id} does not have access to any property with ID {propertyId}.");
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

        if (!(GetCurrentUser().IsAdmin || GetCurrentUser().LandlordId == model.LandlordId))
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

    private string IsEditToOperationType(bool input) //QQQ
    {
        if (input)
        {
            return "edit";
        }

        return "add";
    }

    private bool OperationTypeToIsEdit(string input)
    {
        if (input.ToUpper() == "ADD")
        {
            return false;
        }

        return true;
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet]
    [Route("/landlord/{landlordId:int}/property/{operationType:regex(^(add|edit)$)}/{propertyId:int}/step/1")]
    public ActionResult PropertyInputStepOne([FromRoute] int propertyId, [FromRoute] int landlordId,
        [FromRoute] string operationType)
    {
        var property = _propertyService.GetPropertyByPropertyId(propertyId);
        var model = new PropertyInputFormViewModel();

        if (property == null)
        {
            property = new PropertyDbModel
            {
                Id = propertyId,
                LandlordId = landlordId
            };

            if (!OwnerOrAdminCheck(PropertyViewModel.FromDbModel(property), false))
            {
                return StatusCode(403);
            }
        }
        else
        {
            if (!OwnerOrAdminCheck(PropertyViewModel.FromDbModel(property)))
            {
                return StatusCode(403);
            }
        }

        model.IsEdit = OperationTypeToIsEdit(operationType);
        model.InitialiseViewModel(1, property);
        return View("PropertyInput", model);
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpPost]
    [Route("/landlord/{landlordId:int}/property/{operationType:regex(^(add|edit)$)}/{propertyId:int}/step/1")]
    public async Task<ActionResult> PropertyInputStepOne([FromForm] PropertyInputFormViewModel model,
        [FromRoute] int landlordId, [FromRoute] int propertyId, [FromRoute] string operationType)
    {
        var isValid =
            Validator.TryValidateObject(model.Step1.Address, new ValidationContext(model.Step1?.Address), null);
        // Check model validity
        if (!ModelState.IsValid && isValid)
        {
            return RedirectToAction("PropertyInputStepOne", "Property",
                new
                {
                    propertyId = model.PropertyId, landlordId = model.LandlordId,
                    operationType = IsEditToOperationType(model.IsEdit)
                });
        }

        var propertyView = model.FormToViewModel();

        //Check Auth
        if (!OwnerOrAdminCheck(propertyView, false))
        {
            return StatusCode(403);
        }

        if (propertyView.Address.AddressLine1 == null || propertyView.Address.Postcode == null)
        {
            return RedirectToAction("PropertyInputStepOne", "Property",
                new
                {
                    propertyId = model.PropertyId, landlordId = model.LandlordId,
                    operationType = IsEditToOperationType(model.IsEdit)
                });
        }

        if (propertyView.Address.Postcode != null)
        {
            propertyView.Address.Postcode =
                Regex.Replace(propertyView.Address.Postcode, @"^(\S+?)\s*?(\d\w\w)$", "$1 $2");
            propertyView.Address.Postcode = propertyView.Address.Postcode.ToUpper();
        }

        await _azureMapsApiService.AutofillAddress(propertyView);

        //Does the property exist already?
        var property = _propertyService.GetPropertyByPropertyId(propertyView.PropertyId);
        if (property != null)
        {
            if (!OwnerOrAdminCheck(propertyView))
            {
                return StatusCode(403);
            }

            _propertyService.UpdateProperty(model.PropertyId, propertyView);
            return RedirectToAction("PropertyInputStepTwo", "Property",
                new { propertyId = propertyView.PropertyId, operationType = IsEditToOperationType(model.IsEdit) });
        }

        //Check Auth
        if (!OwnerOrAdminCheck(propertyView, false))
        {
            return StatusCode(403);
        }

        // Create new record in the database for this property
        propertyView.PropertyId =
            await _propertyService.AddNewProperty(propertyView.LandlordId, propertyView);
        return RedirectToAction("PropertyInputStepTwo", "Property",
            new { propertyId = propertyView.PropertyId, operationType = IsEditToOperationType(model.IsEdit) });
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet("{operationType:regex(^(add|edit)$)}/{propertyId:int}/step/2")]
    public ActionResult PropertyInputStepTwo(int propertyId, [FromRoute] string operationType)
    {
        //Get Database record (if there is one)
        var property = _propertyService.GetPropertyByPropertyId(propertyId);
        if (property == null)
        {
            return StatusCode(404);
        }

        if (!OwnerOrAdminCheck(PropertyViewModel.FromDbModel(property)))
        {
            return StatusCode(403);
        }

        //Make new model for this step using database record
        var model = new PropertyInputFormViewModel();
        model.InitialiseViewModel(2, property);
        model.IsEdit = OperationTypeToIsEdit(operationType);
        //Return step two view
        return View("PropertyInput", model);
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpPost("{operationType:regex(^(add|edit)$)}/{propertyId:int}/step/2")]
    public ActionResult PropertyInputStepTwo([FromForm] PropertyInputFormViewModel model,
        [FromRoute] string operationType)
    {
        // Check model validity
        if (!ModelState.IsValid)
        {
            return RedirectToAction("PropertyInputStepTwo", "Property",
                new
                {
                    propertyId = model.PropertyId, landlordId = model.LandlordId,
                    operationType = IsEditToOperationType(model.IsEdit)
                });
        }

        var propertyView = model.FormToViewModel();

        //Check Auth
        if (!OwnerOrAdminCheck(propertyView))
        {
            return StatusCode(403);
        }

        // Update the property's record with the values entered at this step
        await _propertyService.UpdateProperty(model.PropertyId, propertyView);

        //Redirect to next step (could be subverted with a bool for the edit function?)
        return RedirectToAction("PropertyInputStepThree", "Property",
            new { propertyId = model.PropertyId, operationType = IsEditToOperationType(model.IsEdit) });
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet("{operationType:regex(^(add|edit)$)}/{propertyId:int}/step/3")]
    public ActionResult PropertyInputStepThree([FromRoute] int propertyId, [FromRoute] string operationType)
    {
        //Get Database record (if there is one)
        var property = _propertyService.GetPropertyByPropertyId(propertyId);
        if (property == null)
        {
            return StatusCode(404);
        }

        if (!OwnerOrAdminCheck(PropertyViewModel.FromDbModel(property)))
        {
            return StatusCode(403);
        }

        //Make new model for this step using database record
        var model = new PropertyInputFormViewModel();
        model.InitialiseViewModel(3, property);
        model.IsEdit = OperationTypeToIsEdit(operationType);
        return View("PropertyInput", model);
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpPost("{operationType:regex(^(add|edit)$)}/{propertyId:int}/step/3")]
    public ActionResult PropertyInputStepThree([FromForm] PropertyInputFormViewModel model,
        [FromRoute] string operationType)
    {
        // Check model validity
        if (!ModelState.IsValid)
        {
            return RedirectToAction("PropertyInputStepThree", "Property",
                new
                {
                    propertyId = model.PropertyId, landlordId = model.LandlordId,
                    operationType = IsEditToOperationType(model.IsEdit)
                });
        }

        var propertyView = model.FormToViewModel();

        //Check Auth
        if (!OwnerOrAdminCheck(propertyView))
        {
            return StatusCode(403);
        }

        // Update the property's record with the values entered at this step
        _propertyService.UpdateProperty(model.PropertyId, propertyView);

        //Redirect to next step
        return RedirectToAction("PropertyInputStepFour", "Property",
            new { propertyId = model.PropertyId, operationType = IsEditToOperationType(model.IsEdit) });
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet("{operationType:regex(^(add|edit)$)}/{propertyId:int}/step/4")]
    public ActionResult PropertyInputStepFour([FromRoute] int propertyId, [FromRoute] string operationType)
    {
        //Get Database record (if there is one)
        var property = _propertyService.GetPropertyByPropertyId(propertyId);
        if (property == null)
        {
            return StatusCode(404);
        }

        if (!OwnerOrAdminCheck(PropertyViewModel.FromDbModel(property)))
        {
            return StatusCode(403);
        }

        //Make new model for this step using database record
        var model = new PropertyInputFormViewModel();
        model.InitialiseViewModel(4, property);
        model.IsEdit = OperationTypeToIsEdit(operationType);
        return View("PropertyInput", model);
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpPost("{operationType:regex(^(add|edit)$)}/{propertyId:int}/step/4")]
    public ActionResult PropertyInputStepFour([FromForm] PropertyInputFormViewModel model,
        [FromRoute] string operationType)
    {
        // Check model validity
        if (!ModelState.IsValid)
        {
            return RedirectToAction("PropertyInputStepFour", "Property",
                new
                {
                    propertyId = model.PropertyId, landlordId = model.LandlordId,
                    operationType = IsEditToOperationType(model.IsEdit)
                });
        }

        var propertyView = model.FormToViewModel();

        //Check Auth
        if (!OwnerOrAdminCheck(propertyView))
        {
            return StatusCode(403);
        }

        // Update the property's record with the values entered at this step
        _propertyService.UpdateProperty(model.PropertyId, propertyView);

        //Redirect to next step (could be subverted with a bool for the edit function?)
        return RedirectToAction("PropertyInputStepFive", "Property",
            new { propertyId = model.PropertyId, operationType = IsEditToOperationType(model.IsEdit) });
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet("{operationType:regex(^(add|edit)$)}/{propertyId:int}/step/5")]
    public ActionResult PropertyInputStepFive([FromRoute] int propertyId, [FromRoute] string operationType)
    {
        //Get Database record (if there is one)
        var property = _propertyService.GetPropertyByPropertyId(propertyId);
        if (property == null)
        {
            return StatusCode(404);
        }

        if (!OwnerOrAdminCheck(PropertyViewModel.FromDbModel(property)))
        {
            return StatusCode(403);
        }

        //Make new model for this step using database record
        var model = new PropertyInputFormViewModel();
        model.InitialiseViewModel(5, property);
        model.IsEdit = OperationTypeToIsEdit(operationType);
        return View("PropertyInput", model);
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpPost("{operationType:regex(^(add|edit)$)}/{propertyId:int}/step/5")]
    public ActionResult PropertyInputStepFive([FromForm] PropertyInputFormViewModel model,
        [FromRoute] string operationType)
    {
        // Check model validity
        if (!ModelState.IsValid)
        {
            return RedirectToAction("PropertyInputStepFive", "Property",
                new
                {
                    propertyId = model.PropertyId, landlordId = model.LandlordId,
                    operationType = IsEditToOperationType(model.IsEdit)
                });
        }

        var propertyView = model.FormToViewModel();
        //Check Auth
        if (!OwnerOrAdminCheck(propertyView))
        {
            return StatusCode(403);
        }

        // Update the property's record with the values entered at this step
        _propertyService.UpdateProperty(model.PropertyId, propertyView);

        //Redirect to next step (could be subverted with a bool for the edit function?)
        return RedirectToAction("PropertyInputStepSix", "Property",
            new { propertyId = model.PropertyId, operationType = IsEditToOperationType(model.IsEdit) });
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet("{operationType:regex(^(add|edit)$)}/{propertyId:int}/step/6")]
    public ActionResult PropertyInputStepSix([FromRoute] int propertyId, [FromRoute] string operationType)
    {
        //Get Database record (if there is one)
        var property = _propertyService.GetPropertyByPropertyId(propertyId);
        if (property == null)
        {
            return StatusCode(404);
        }

        if (!OwnerOrAdminCheck(PropertyViewModel.FromDbModel(property)))
        {
            return StatusCode(403);
        }

        //Make new model for this step using database record
        var model = new PropertyInputFormViewModel();
        model.InitialiseViewModel(6, property);
        model.IsEdit = OperationTypeToIsEdit(operationType);
        return View("PropertyInput", model);
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpPost("{operationType:regex(^(add|edit)$)}/{propertyId:int}/step/6")]
    public ActionResult PropertyInputStepSix([FromForm] PropertyInputFormViewModel model)
    {
        // Check model validity
        if (!ModelState.IsValid)
        {
            return RedirectToAction("PropertyInputStepSix", "Property",
                new
                {
                    propertyId = model.PropertyId, landlordId = model.LandlordId,
                    operationType = IsEditToOperationType(model.IsEdit)
                });
        }

        var propertyView = model.FormToViewModel();
        //Check Auth
        if (!OwnerOrAdminCheck(propertyView))
        {
            return StatusCode(403);
        }

        // Update the property's record with the values entered at this step
        _propertyService.UpdateProperty(model.PropertyId, propertyView);

            _propertyService.UpdateProperty(propertyId, newPropertyModel, newPropertyModel.IsIncomplete);
            // Go to step 2
            newPropertyModel = PropertyViewModel.FromDbModel(_propertyService.GetPropertyByPropertyId(propertyId)!);
            return View("EditProperty", new AddNewPropertyViewModel { Step = step + 1, Property = newPropertyModel });
    }

        // Update the property's record with the final set of values
        _propertyService.UpdateProperty(propertyId, newPropertyModel, newPropertyModel.IsIncomplete);
        newPropertyModel = PropertyViewModel.FromDbModel(_propertyService.GetPropertyByPropertyId(propertyId)!);

        return GetCurrentUser().IsAdmin || GetCurrentUser().LandlordId == newPropertyModel.LandlordId;
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet(
        "/landlord/{landlordId:int}/property/{operationType:regex(^(add|edit)$)})/{propertyId:int}/step/{step:int}/back")]
    public ActionResult PropertyInputBack([FromRoute] int step, [FromRoute] int propertyId, [FromRoute] int landlordId)
    {
        return step switch
        {
            1 => RedirectToAction("PropertyInputStepOne", new { propertyId, landlordId }),
            2 => RedirectToAction("PropertyInputStepTwo", new { propertyId }),
            3 => RedirectToAction("PropertyInputStepThree", new { propertyId }),
            4 => RedirectToAction("PropertyInputStepFour", new { propertyId }),
            5 => RedirectToAction("PropertyInputStepFive", new { propertyId }),
            6 => RedirectToAction("PropertyInputStepSix", new { propertyId }),
            _ => StatusCode(404)
        };
    }

        _propertyService.UpdateProperty(propertyId, newPropertyModel, false);
        // Finished adding property, so go to View Properties page
        return RedirectToAction("ViewProperties", "Landlord", new { id = landlordId });
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpPost(
        @"/landlord/{landlordId:int}/property/{operationType:regex(^(add|edit)$)})/{propertyId:int}/step/{step:int}/cancel")]
    public async Task<ActionResult> PropertyInputCancel([FromRoute] int propertyId, [FromRoute] int landlordId,
        [FromRoute] string operationType)
    {
        if (operationType == "edit")
        {
            return RedirectToAction("ViewProperties", "Landlord", new { id = landlordId });
        }

        // Get the property we're currently adding
        var property = _propertyService.GetPropertyByPropertyId(propertyId);

        if (property == null)
        {
            return RedirectToAction("ViewProperties", "Landlord", new { id = landlordId });
        }

        if (!OwnerOrAdminCheck(PropertyViewModel.FromDbModel(property)))
        {
            return StatusCode(403);
        }

        _propertyService.DeleteProperty(property);
        await _azureStorage.DeleteContainer("property", property.Id);

        return RedirectToAction("ViewProperties", "Landlord", new { id = landlordId });
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
        if (!_propertyService.IsUserAdminOrCorrectLandlord(GetCurrentUser(), propertyId))
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
        if (!_propertyService.IsUserAdminOrCorrectLandlord(GetCurrentUser(), propertyId))
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
        if (!_propertyService.IsUserAdminOrCorrectLandlord(GetCurrentUser(), propertyId))
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