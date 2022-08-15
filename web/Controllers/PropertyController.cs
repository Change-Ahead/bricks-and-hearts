using System.Text.RegularExpressions;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BricksAndHearts.Controllers;

[Authorize]
[Route("/property")]
public class PropertyController : AbstractController
{
    private readonly IPropertyService _propertyService;
    private readonly IAzureMapsApiService _azureMapsApiService;
    private readonly ILogger<PropertyController> _logger;
    private readonly IAzureStorage _azureStorage;

    public PropertyController(IPropertyService propertyService, IAzureMapsApiService azureMapsApiService,
        ILogger<PropertyController> logger, IAzureStorage azureStorage, IPostcodeApiService postcodeApiService)
    {
        _propertyService = propertyService;
        _azureMapsApiService = azureMapsApiService;
        _logger = logger;
        _azureStorage = azureStorage;
    }

    [HttpPost]
    [Authorize(Roles = "Landlord, Admin")]
    public ActionResult DeleteProperty(int propertyId)
    {
        var propDB = _propertyService.GetPropertyByPropertyId(propertyId);
        if (propDB == null)
        {
            _logger.LogWarning("Property with ID {PropertyId} does not exist", propertyId);
            return StatusCode(404);
        }

        if (GetCurrentUser().IsAdmin == false && GetCurrentUser().LandlordId != propDB.LandlordId)
        {
            _logger.LogWarning("You do not have access to any property with ID {PropertyId}.", propertyId);
            return StatusCode(404);
        }

        var landlordId = propDB.Landlord.Id;

        // Delete property
        _propertyService.DeleteProperty(propDB);

        // Go to View Properties page
        return RedirectToAction("ViewProperties", "Landlord", new { id = landlordId });
    }

    [HttpGet]
    [Route("/property/{propertyId:int}/view")]
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

        var propertyViewModel = PropertyViewModel.FromDbModel(model);

        var fileNames = await _azureStorage.ListFileNames("property", propertyId);
        var imageFiles = GetFilesFromFileNames(fileNames, propertyId);
        var propertyDetailsModel = new PropertyDetailsViewModel { Property = propertyViewModel, Images = imageFiles };

        return View(propertyDetailsModel);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("SortProperties")]
    public IActionResult SortProperties(string sortBy, int page = 1, int propPerPage = 10)
    {
        var properties = _propertyService.SortProperties(sortBy);

        var listOfProperties = properties.Select(PropertyViewModel.FromDbModel).ToList();

        return View("~/Views/Admin/PropertyList.cshtml",
            new PropertiesDashboardViewModel(listOfProperties.Skip((page - 1) * propPerPage).Take(propPerPage).ToList(),
                listOfProperties.Count, null!, page, sortBy));
    }

    [Authorize(Roles = "Admin")]
    [Route("/admin/get-public-view-link/{propertyId:int}")]
    [HttpGet]
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

    #region AddProperty

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet("add")]
    public ActionResult AddNewProperty_Begin(int landlordId)
    {
        return AddNewProperty_Continue(1, 0, landlordId);
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet("add/step/{step:int}")]
    public ActionResult AddNewProperty_Continue([FromRoute] int step, [FromQuery] int propertyId,
        int? landlordId = null)
    {
        var newPropertyModel = new PropertyViewModel { Address = new AddressModel() };
        landlordId ??= GetCurrentUser().LandlordId!.Value;

        if (propertyId != 0)
        {
            var property = _propertyService.GetPropertyByPropertyId(propertyId);
            if (property == null)
            {
                return RedirectToAction("ViewProperties", "Landlord", new { id = landlordId });
            }

            newPropertyModel = PropertyViewModel.FromDbModel(property);
        }
        else
        {
            newPropertyModel.PropertyId = propertyId;
            newPropertyModel.LandlordId = (int)landlordId;
        }

        if (!(GetCurrentUser().IsAdmin || GetCurrentUser().LandlordId == newPropertyModel.LandlordId))
        {
            return StatusCode(403);
        }

        // Show the form for this step
        return View("AddNewProperty", new AddNewPropertyViewModel { Step = step, Property = newPropertyModel });
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpPost("add/step/{step:int}")]
    public async Task<ActionResult> AddNewProperty_Continue([FromRoute] int step, int propertyId,
        [FromForm] PropertyViewModel newPropertyModel)
    {
        newPropertyModel.PropertyId = propertyId;

        if (!ModelState.IsValid)
        {
            return View("AddNewProperty", new AddNewPropertyViewModel { Step = step, Property = newPropertyModel });
        }

        if (newPropertyModel.Address.Postcode != null)
        {
            newPropertyModel.Address.Postcode =
                Regex.Replace(newPropertyModel.Address.Postcode, @"^(\S+?)\s*?(\d\w\w)$", "$1 $2");
            newPropertyModel.Address.Postcode = newPropertyModel.Address.Postcode.ToUpper();
        }

        if (step == 1)
        {
            if (!(GetCurrentUser().IsAdmin || GetCurrentUser().LandlordId == newPropertyModel.LandlordId))
            {
                return StatusCode(403);
            }

            if (newPropertyModel.Address.AddressLine1 == null || newPropertyModel.Address.Postcode == null)
            {
                // Address line 1 and postcode are the minimum information we need to create a new record
                return View("AddNewProperty",
                    new AddNewPropertyViewModel { Step = step, Property = newPropertyModel });
            }

            await _azureMapsApiService.AutofillAddress(newPropertyModel);

            // Create new record in the database for this property
            newPropertyModel.PropertyId =
                _propertyService.AddNewProperty(newPropertyModel.LandlordId, newPropertyModel);

            // Go to step 2
            return View("AddNewProperty", new AddNewPropertyViewModel { Step = step + 1, Property = newPropertyModel });
        }

        var property = _propertyService.GetPropertyByPropertyId(propertyId);
        if (property == null)
        {
            return StatusCode(404);
        }

        if (!(GetCurrentUser().IsAdmin || GetCurrentUser().LandlordId == property.LandlordId))
        {
            return StatusCode(403);
        }

        if (step < AddNewPropertyViewModel.MaximumStep)
        {
            // Update the property's record with the values entered at this step
            _propertyService.UpdateProperty(property.Id, newPropertyModel);
            return View("AddNewProperty", new AddNewPropertyViewModel { Step = step + 1, Property = newPropertyModel });
        }

        // Update the property's record with the final set of values
        _propertyService.UpdateProperty(property.Id, newPropertyModel, false);
        return RedirectToAction("ViewProperties", "Landlord", new { id = property.LandlordId });
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpPost("add/cancel")]
    public async Task<ActionResult> AddNewProperty_Cancel(int propertyId, int? landlordId = null)
    {
        // Get the property we're currently adding
        var property = _propertyService.GetPropertyByPropertyId(propertyId);
        if (property == null)
        {
            return RedirectToAction("ViewProperties", "Landlord", new { id = landlordId });
        }

        if (!(GetCurrentUser().LandlordId == property.LandlordId || GetCurrentUser().IsAdmin))
        {
            return StatusCode(403);
        }

        landlordId ??= property.Landlord.Id;

        // Delete partially complete property
        _propertyService.DeleteProperty(property);
        await _azureStorage.DeleteContainer("property", property.Id);

        return RedirectToAction("ViewProperties", "Landlord", new { id = landlordId });
    }

    #endregion

    #region EditProperties

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet("edit")]
    public ActionResult EditProperty(int propertyId)
    {
        var property = _propertyService.GetPropertyByPropertyId(propertyId);

        var propertyViewModel = PropertyViewModel.FromDbModel(property!);

        if (!(GetCurrentUser().IsAdmin || GetCurrentUser().LandlordId == propertyViewModel.LandlordId))
        {
            return StatusCode(403);
        }

        // Start at step 1
        return View("EditProperty", new AddNewPropertyViewModel { Step = 1, Property = propertyViewModel });
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet("edit/step/{step:int}")]
    public ActionResult EditProperty_Continue([FromRoute] int step, int propertyId)
    {
        var property = _propertyService.GetPropertyByPropertyId(propertyId);
        if (property == null)
        {
            return StatusCode(404);
        }

        var newPropertyModel = PropertyViewModel.FromDbModel(property);
        newPropertyModel.PropertyId = propertyId;

        if (!(GetCurrentUser().IsAdmin || GetCurrentUser().LandlordId == newPropertyModel.LandlordId))
        {
            return StatusCode(403);
        }

        return View("EditProperty", new AddNewPropertyViewModel { Step = step, Property = newPropertyModel });
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpPost("edit/step/{step:int}")]
    public async Task<IActionResult> EditProperty_Continue([FromRoute] int step, int propertyId,
        [FromForm] PropertyViewModel newPropertyModel, int? landlordId = null)
    {
        newPropertyModel.PropertyId = propertyId;

        if (!ModelState.IsValid)
        {
            return View("EditProperty", new AddNewPropertyViewModel { Step = step, Property = newPropertyModel });
        }

        var property = _propertyService.GetPropertyByPropertyId(propertyId);
        if (property == null)
        {
            return StatusCode(404);
        }

        if (!(GetCurrentUser().LandlordId == property.LandlordId || GetCurrentUser().IsAdmin))
        {
            return StatusCode(403);
        }

        if (newPropertyModel.Address.Postcode != null)
        {
            newPropertyModel.Address.Postcode =
                Regex.Replace(newPropertyModel.Address.Postcode, @"^(\S+?)\s*?(\d\w\w)$", "$1 $2");
            newPropertyModel.Address.Postcode = newPropertyModel.Address.Postcode.ToUpper();
        }

        if (step == 1)
        {
            if (newPropertyModel.Address.AddressLine1 == null || newPropertyModel.Address.Postcode == null)
            {
                // Address line 1 and postcode are the minimum information we need to create a new record

                return View("EditProperty",
                    new AddNewPropertyViewModel { Step = step, Property = newPropertyModel });
            }

            await _azureMapsApiService.AutofillAddress(newPropertyModel);

            _propertyService.UpdateProperty(propertyId, newPropertyModel, newPropertyModel.IsIncomplete);
            // Go to step 2
            newPropertyModel = PropertyViewModel.FromDbModel(_propertyService.GetPropertyByPropertyId(propertyId)!);
            return View("EditProperty", new AddNewPropertyViewModel { Step = step + 1, Property = newPropertyModel });
        }

        // Update the property's record with the final set of values
        _propertyService.UpdateProperty(propertyId, newPropertyModel, newPropertyModel.IsIncomplete);
        newPropertyModel = PropertyViewModel.FromDbModel(_propertyService.GetPropertyByPropertyId(propertyId)!);

        if (step < AddNewPropertyViewModel.MaximumStep)
        {
            // Go to next step
            return View("EditProperty", new AddNewPropertyViewModel { Step = step + 1, Property = newPropertyModel });
        }

        _propertyService.UpdateProperty(propertyId, newPropertyModel, false);
        // Finished adding property, so go to View Properties page
        return RedirectToAction("ViewProperties", "Landlord", new { id = landlordId });
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpPost("edit/cancel")]
    public ActionResult EditProperty_Cancel(int landlordId)
    {
        // Go to View Properties page
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
    [HttpGet]
    [Route("{propertyId:int}/{fileName}")]
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
        var listOfProperties = properties.Select(PropertyViewModel.FromDbModel).ToList();

        return View("~/Views/Admin/PropertyList.cshtml",
            new PropertiesDashboardViewModel(listOfProperties.Skip((page - 1) * propPerPage).Take(propPerPage).ToList(),
                listOfProperties.Count, null!, page, "Location"));
    }
}