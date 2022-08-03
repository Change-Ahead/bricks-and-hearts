using System.Diagnostics.Eventing.Reader;
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
    private readonly IAzureMapsApiService _azureMapsApiService;
    private readonly ILogger<PropertyController> _logger;
    private readonly IAzureStorage _azureStorage;

    public PropertyController(IPropertyService propertyService, IAzureMapsApiService azureMapsApiService, ILogger<PropertyController> logger, IAzureStorage azureStorage)
    {
        _propertyService = propertyService;
        _azureMapsApiService = azureMapsApiService;
        _logger = logger;
        _azureStorage = azureStorage;
    }

    [Authorize(Roles = "Landlord")]
    [HttpGet("add")]
    public ActionResult AddNewProperty_Begin()
    {
        return AddNewProperty_Continue(1);
    }

    [Authorize(Roles = "Landlord")]
    [HttpGet("add/step/{step:int}")]
    public ActionResult AddNewProperty_Continue([FromRoute] int step)
    {
        var landlordId = GetCurrentUser().LandlordId!.Value;
        
        var dbModel = _propertyService.GetIncompleteProperty(landlordId);
        var property = dbModel == null
            ? new PropertyViewModel { Address = new PropertyAddress() }
            : PropertyViewModel.FromDbModel(dbModel);
        
        return View("AddNewProperty", new AddNewPropertyViewModel { Step = step, Property = property });
    }

    [Authorize(Roles = "Landlord")]
    [HttpPost("add/step/{step:int}")]
    public async Task<ActionResult> AddNewProperty_Continue([FromRoute] int step, [FromForm] PropertyViewModel newPropertyModel)
    {
        var landlordId = GetCurrentUser().LandlordId!.Value;

        if (!ModelState.IsValid)
        {
            return View("AddNewProperty", new AddNewPropertyViewModel { Step = step, Property = newPropertyModel });
        }
        
        var property = _propertyService.GetIncompleteProperty(landlordId);
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

            if (property == null)
            {
                // Create new record in the database for this property
                _propertyService.AddNewProperty(landlordId, newPropertyModel, isIncomplete: true);
            }
            else
            {
                _propertyService.UpdateProperty(property.Id, newPropertyModel, isIncomplete: true);
            }

            // Go to step 2
            return RedirectToAction("AddNewProperty_Continue", new { step = 2 });
        }
        else if (step < AddNewPropertyViewModel.MaximumStep)
        {
            if (property == null)
            {
                return RedirectToAction("ViewProperties", "Landlord");
            }
            
            // Update the property's record with the values entered at this step
            _propertyService.UpdateProperty(property.Id, newPropertyModel, isIncomplete: true);
            return RedirectToAction("AddNewProperty_Continue", new { step = step + 1 });
        }
        else
        {
            if (property == null)
            {
                // No property in progress
                return RedirectToAction("ViewProperties", "Landlord");
            }
            
            // Update the property's record with the final set of values
            _propertyService.UpdateProperty(property.Id, newPropertyModel, isIncomplete: false);
            return RedirectToAction("ViewProperties", "Landlord");
        }
    }

    [Authorize(Roles = "Landlord")]
    [HttpPost("add/cancel")]
    public async Task<ActionResult> AddNewProperty_Cancel()
    {
        var landlordId = GetCurrentUser().LandlordId!.Value;
        var property = _propertyService.GetIncompleteProperty(landlordId);
        if (property == null)
        {
            return RedirectToAction("ViewProperties", "Landlord");
        }

        // Delete partially complete property
        _propertyService.DeleteProperty(property);
        await _azureStorage.DeleteContainer("property", property.Id);
        
        return RedirectToAction("ViewProperties", "Landlord");
    }
    
    [HttpPost]
    [Authorize(Roles = "Landlord, Admin")]
    public ActionResult DeleteProperty(int propertyId)
    {
        PropertyDbModel? propDB = _propertyService.GetPropertyByPropertyId(propertyId);
        if (propDB == null)
        {
            _logger.LogWarning("Property with ID {PropertyId} does not exist", propertyId);
            return StatusCode(404);
        }

        if (GetCurrentUser().IsAdmin == false & GetCurrentUser().LandlordId != propDB.LandlordId)
        {
            _logger.LogWarning("You do not have access to any property with ID {PropertyId}.", propertyId);
            return StatusCode(404);
        }

        // Delete property
        _propertyService.DeleteProperty(propDB);

        // Go to View Properties page
        return RedirectToAction("ViewProperties", "Landlord");
    }


    [HttpGet]
    [Route("/property/{propertyId:int}/view")]
    public ActionResult ViewProperty(int propertyId)
    {
        PropertyDbModel? model = _propertyService.GetPropertyByPropertyId(propertyId);
        if (model == null)
        {
            _logger.LogWarning("Property with ID {PropertyId} does not exist", propertyId);
            return RedirectToAction("Error", "Home", new { status = 404 });
        }
        PropertyViewModel propertyViewModel = PropertyViewModel.FromDbModel(model);
        return View(propertyViewModel);
    }

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet]
    public async Task<IActionResult> ListPropertyImages(int propertyId)
    {
        if (!_propertyService.IsUserAdminOrCorrectLandlord(GetCurrentUser(), propertyId))
        {
            return StatusCode(403);
        }
        ImageListViewModel imageList = await _azureStorage.ListFiles("property", propertyId);
        return View(imageList);
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
            if (!_azureStorage.IsImage(image.FileName))
            {
                FlashMessage(_logger,
                    ($"Failed to upload {image.FileName}: not in a recognised image format", "danger",
                        "{image.FileName} is not in a recognised image format. Please submit your images in one of the following formats: JPG, JPEG, PNG, BMP, GIF"));
            }
            else
            {
                if (image.Length > 0)
                {
                    string message = await _azureStorage.UploadFile(image, "property", propertyId);
                    FlashMessage(_logger,
                        ($"Successfully uploaded {image.FileName}", "success", message));
                }
                else
                {
                    FlashMessage(_logger,
                        ($"Failed to upload {image.FileName}: has length zero.", "danger", $"{image.FileName} contains no data, and so has not been uploaded"));
                }
            }
        }
        return RedirectToAction("ListPropertyImages", "Property", new{propertyId});
    }
    
    [Authorize(Roles = "Landlord, Admin")]
    [HttpPost("displayImage")]
    public async Task<IActionResult> DisplayPropertyImage(int propertyId, string fileName)
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
        return RedirectToAction("ListPropertyImages", "Property", new{propertyId});
    }
}