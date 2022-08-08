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
    private readonly IAzureMapsApiService _azureMapsApiService;
    private readonly IAzureStorage _azureStorage;
    private readonly ILogger<PropertyController> _logger;
    private readonly IPropertyService _propertyService;

    public PropertyController(IPropertyService propertyService, IAzureMapsApiService azureMapsApiService,
        ILogger<PropertyController> logger, IAzureStorage azureStorage)
    {
        _propertyService = propertyService;
        _azureMapsApiService = azureMapsApiService;
        _logger = logger;
        _azureStorage = azureStorage;
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet("add")]
    public ActionResult AddNewProperty_Begin(int landlordId)
    {
        return AddNewProperty_Continue(1, 0, landlordId);
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet("edit")]
    public ActionResult EditProperty(int propertyId)
    {
        var property = _propertyService.GetPropertyByPropertyId(propertyId);
        /*_propertyService.ChangePropertyToIncomplete(property);*/ //TODU ?
        var propertyViewModel = PropertyViewModel.FromDbModel(property!);
        // Start at step 1
        return View("EditProperty", new AddNewPropertyViewModel { Step = 1, Property = propertyViewModel });
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet("add/step/{step:int}")]
    public ActionResult AddNewProperty_Continue([FromRoute] int step, int propertyId, int? landlordId = null)
    {
        var newPropertyModel = new PropertyViewModel { Address = new PropertyAddress() };
        landlordId ??= GetCurrentUser().LandlordId!.Value;

        if (propertyId != 0)
        {
            newPropertyModel = PropertyViewModel.FromDbModel(_propertyService.GetPropertyByPropertyId(propertyId));
        }
        else
        {
            newPropertyModel.PropertyId = propertyId;
            newPropertyModel.LandlordId = (int)landlordId;
        }

        // Show the form for this step
        return View("AddNewProperty", new AddNewPropertyViewModel { Step = step, Property = newPropertyModel });
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet("edit/step/{step:int}")]
    public ActionResult EditProperty_Continue([FromRoute] int step, int propertyId)
    {
        var newPropertyModel = PropertyViewModel.FromDbModel(_propertyService.GetPropertyByPropertyId(propertyId));
        newPropertyModel.PropertyId = propertyId;

        // Show the form for this step
        return View("EditProperty", new AddNewPropertyViewModel { Step = step, Property = newPropertyModel });
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpPost("add/step/{step:int}")]
    public async Task<ActionResult> AddNewProperty_Continue([FromRoute] int step, int propertyId,
        [FromForm] PropertyViewModel newPropertyModel, int? landlordId = null)
    {
        landlordId ??= GetCurrentUser().LandlordId!.Value;
        newPropertyModel.PropertyId = propertyId;
        newPropertyModel.LandlordId = (int)landlordId;

        if (!ModelState.IsValid)
        {
            return View("AddNewProperty", new AddNewPropertyViewModel { Step = step, Property = newPropertyModel });
        }

        var property = _propertyService.GetPropertyByPropertyId(propertyId);
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
                return View("AddNewProperty",
                    new AddNewPropertyViewModel { Step = step, Property = newPropertyModel });
            }

            await _azureMapsApiService.AutofillAddress(newPropertyModel);

            // Create new record in the database for this property
            var newPropId = _propertyService.AddNewProperty((int)landlordId, newPropertyModel);

            // Go to step 2
            return RedirectToAction("AddNewProperty_Continue", new { step = 2, propertyId = newPropId });
        }

        if (step < AddNewPropertyViewModel.MaximumStep)
        {
            // Update the property's record with the values entered at this step
            _propertyService.UpdateProperty(property.Id, newPropertyModel);
            return RedirectToAction("AddNewProperty_Continue", new { step = step + 1, propertyId = property.Id });
        }

        // Update the property's record with the final set of values
        _propertyService.UpdateProperty(property.Id, newPropertyModel, false);
        return RedirectToAction("ViewProperties", "Landlord", new { id = property.LandlordId });
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

        // Get the property we're currently adding
        var property = _propertyService.GetPropertyByPropertyId(propertyId);
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

            _propertyService.UpdateProperty(propertyId, newPropertyModel, false);
            // Go to step 2
            return View("EditProperty", new AddNewPropertyViewModel { Step = 2, Property = newPropertyModel });
        }

        // Update the property's record with the final set of values
        _propertyService.UpdateProperty(propertyId, newPropertyModel, false);

        if (step < AddNewPropertyViewModel.MaximumStep)
        {
            // Update the property's record with the values entered at this step
            var newProperty = PropertyViewModel.FromDbModel(_propertyService.GetPropertyByPropertyId(propertyId));

            // Go to next step
            return View("EditProperty", new AddNewPropertyViewModel { Step = step + 1, Property = newProperty });
        }

        // Finished adding property, so go to View Properties page
        return RedirectToAction("ViewProperties", "Landlord", new { id = landlordId });
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

        landlordId ??= property.Landlord.Id;

        // Delete partially complete property
        _propertyService.DeleteProperty(property);
        await _azureStorage.DeleteContainer("property", property.Id);

        return RedirectToAction("ViewProperties", "Landlord", new { id = landlordId });
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

        if ((GetCurrentUser().IsAdmin == false) & (GetCurrentUser().LandlordId != propDB.LandlordId))
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

    [Authorize(Roles = "Landlord, Admin")]
    [HttpPost("edit/cancel")]
    public ActionResult EditProperty_Cancel(int landlordId) //TODU Fix this to go to landlord's property page
    {
        // Go to View Properties page
        return RedirectToAction("ViewProperties", "Landlord", new { id = landlordId });
    }


    [HttpGet]
    [Authorize(Roles = "Landlord, Admin")]
    [Route("/property/{propertyId:int}/view")]
    public ActionResult ViewProperty(int propertyId)
    {
        var model = _propertyService.GetPropertyByPropertyId(propertyId);
        if (model == null)
        {
            _logger.LogWarning("Property with ID {PropertyId} does not exist", propertyId);
            return RedirectToAction("Error", "Home", new { status = 404 });
        }

        var propertyViewModel = PropertyViewModel.FromDbModel(model);
        return View(propertyViewModel);
    }

    public PropertyViewModel UpdatePropertyViewModel(PropertyViewModel oldModel, PropertyViewModel updateModel)
    {
        // Update fields if they have been set (i.e. not null) in updateModel
        // Otherwise use the value we currently have in the database
        oldModel.Address.AddressLine1 = updateModel.Address.AddressLine1 ?? oldModel.Address.AddressLine1;
        oldModel.Address.AddressLine2 = updateModel.Address.AddressLine2 ?? oldModel.Address.AddressLine2;
        oldModel.Address.AddressLine3 = updateModel.Address.AddressLine3 ?? oldModel.Address.AddressLine3;
        oldModel.Address.TownOrCity = updateModel.Address.TownOrCity ?? oldModel.Address.TownOrCity;
        oldModel.Address.County = updateModel.Address.County ?? oldModel.Address.County;
        oldModel.Address.Postcode = updateModel.Address.Postcode ?? oldModel.Address.Postcode;
        oldModel.PropertyType = updateModel.PropertyType ?? oldModel.PropertyType;
        oldModel.NumOfBedrooms = updateModel.NumOfBedrooms ?? oldModel.NumOfBedrooms;
        oldModel.Rent = updateModel.Rent ?? oldModel.Rent;
        oldModel.Description = updateModel.Description ?? oldModel.Description;

        return oldModel;
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet]
    public async Task<IActionResult> ListPropertyImages(int propertyId)
    {
        if (!_propertyService.IsUserAdminOrCorrectLandlord(GetCurrentUser(), propertyId))
        {
            return StatusCode(403);
        }

        var fileNames = await _azureStorage.ListFileNames("property", propertyId);
        var imageFiles = GetFilesFromFileNames(fileNames, propertyId);

        var imageList = new ImageListViewModel
        {
            PropertyId = propertyId,
            FileList = imageFiles
        };

        return View(imageList);
    }

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

        List<string> flashTypes = new(),
            flashMessages = new();
        foreach (var image in images)
        {
            var isImageResult = _azureStorage.IsImage(image.FileName);
            if (!isImageResult.isImage)
            {
                _logger.LogInformation($"Failed to upload {image.FileName}: not in a recognised image format");
                flashTypes.Add("danger");
                flashMessages.Add(
                    $"{image.FileName} is not in a recognised image format. Please submit your images in one of the following formats: {isImageResult.imageExtString}");
            }
            else
            {
                if (image.Length > 0)
                {
                    var message = await _azureStorage.UploadFile(image, "property", propertyId);
                    _logger.LogInformation($"Successfully uploaded {image.FileName}");
                    flashTypes.Add("success");
                    flashMessages.Add(message);
                }
                else
                {
                    _logger.LogInformation($"Failed to upload {image.FileName}: has length zero.");
                    flashTypes.Add("danger");
                    flashMessages.Add($"{image.FileName} contains no data, and so has not been uploaded");
                }
            }
        }

        FlashMultipleMessages(flashTypes, flashMessages);
        return RedirectToAction("ListPropertyImages", "Property", new { propertyId });
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
        return RedirectToAction("ListPropertyImages", "Property", new { propertyId });
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
            FlashMessage(_logger, (flashMessageBody, "warning", flashMessageBody));
        }
        else
        {
            var publicViewLink = property.PublicViewLink;
            string flashMessageBody;
            if (string.IsNullOrEmpty(publicViewLink))
            {
                flashMessageBody = "Successfully created a new public view link";
                publicViewLink = _propertyService.CreateNewPublicViewLink(propertyId);
                _logger.LogInformation("Created public view link for property {PropertyId}: {PublicViewLink}", propertyId, publicViewLink);
            }
            else
            {
                flashMessageBody = "Property already has a public view link";
            }

            var baseUrl = HttpContext.Request.GetUri().Authority;
            FlashMessage(_logger, (flashMessageBody, "success", flashMessageBody + ": "+ baseUrl + $"/public/propertyid/{propertyId}/{publicViewLink}"));
        }

        return RedirectToAction("ViewProperty", "Property", new { propertyId });
    }
}