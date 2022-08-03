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

    [Authorize(Roles = "Landlord")]
    [HttpGet("add")]
    public ActionResult AddNewProperty_Begin()
    {
        return AddNewProperty_Continue(1, null);
    }

    [Authorize(Roles = "Landlord")]
    [HttpGet("add/step/{step:int}")]
    [HttpGet("edit/{propId:int}/step/{step:int}")]
    public ActionResult AddNewProperty_Continue([FromRoute] int step, [FromRoute] int? propId)
    {
        if (propId == -1)
        {
            propId = null;
        }

        var landlordId = GetCurrentUser().LandlordId!.Value;
        PropertyViewModel property;

        // See if we're already adding a property (URL contained the ID)
        if (propId == null)
        {
            property = new PropertyViewModel { Address = new PropertyAddress() };
        }
        else
        {
            var propertyDb = _propertyService.GetPropertyByPropertyId((int)propId);

            if (propertyDb == null)
            {
                return StatusCode(404);
            }

            if (landlordId != propertyDb.LandlordId)
            {
                return StatusCode(403);
            }

            property = PropertyViewModel.FromDbModel(propertyDb);
        }

        return View("AddNewProperty", new AddNewPropertyViewModel { Step = step, Property = property });
    }

    [Authorize(Roles = "Landlord")]
    [HttpPost("add/{propId:int}/step/{step:int}")]
    public async Task<IActionResult> AddNewProperty_Continue([FromRoute] int step,
        [FromForm] PropertyViewModel newPropertyModel, [FromRoute] int? propId)
    {
        var landlordId = GetCurrentUser().LandlordId!.Value;

        if (!ModelState.IsValid)
        {
            return Vi
            Property", new AddNewPropertyViewModel { Step = step, Property = newPropertyModel });
        }

        PropertyDbModel? property = null;

        if (propId != null)
        {
            property = _propertyService.GetPropertyByPropertyId((int)propId!);
            newPropertyModel.PropertyId = (int)propId;
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
                return View("AddNewProperty",
                    new AddNewPropertyViewModel { Step = step, Property = newPropertyModel });
            }

            await _azureMapsApiService.AutofillAddress(newPropertyModel);

            if (property == null)
            {
                // Create new record in the database for this property
                _propertyService.AddNewProperty(landlordId, newPropertyModel);
            }
            else
            {
                _propertyService.UpdateProperty(property.Id, newPropertyModel);
            }

            // Go to step 2
            return RedirectToAction("AddNewProperty_Continue", new { step = 2 });
        }

        if (step < AddNewPropertyViewModel.MaximumStep)
        {
            if (property == null)
            {
                return RedirectToAction("ViewProperties", "Landlord");
            }

            // Update the property's record with the values entered at this step
            _propertyService.UpdateProperty(property.Id, newPropertyModel);
            return RedirectToAction("AddNewProperty_Continue", new { step = step + 1 });
        }

        if (property == null)
        {
            // No property in progress
            return RedirectToAction("ViewProperties", "Landlord");
        }

        // Update the property's record with the final set of values
        _propertyService.UpdateProperty(property.Id, newPropertyModel, false);


        return RedirectToAction("ViewProperties", "Landlord");
    }

    [Authorize(Roles = "Landlord")]
    [HttpPost("add/{propId:int}/cancel")]
    public async Task<ActionResult> AddNewProperty_Cancel([FromRoute] int? propId = null)
    {
        var landlordId = GetCurrentUser().LandlordId!.Value;
        if (propId == null)
        {
            return RedirectToAction("ViewProperties", "Landlord");
        }

        var property = _propertyService.GetPropertyByPropertyId((int)propId);
        if (property == null)
        {
            return RedirectToAction("ViewProperties", "Landlord");
        }

        if (property.LandlordId != landlordId)
        {
            return StatusCode(403);
        }

        _propertyService.DeleteProperty(property);
        await _azureStorage.DeleteContainer("property", property.Id);

        return RedirectToAction("ViewProperties", "Landlord");
    }

    [HttpGet]
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

    [Authorize(Roles = "Landlord, Admin")]
    [HttpGet]
    public async Task<IActionResult> ListPropertyImages(int propertyId)
    {
        if (!_propertyService.IsUserAdminOrCorrectLandlord(GetCurrentUser(), propertyId))
        {
            return StatusCode(403);
        }

        var imageList = await _azureStorage.ListFiles("property", propertyId);
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
            if (image.Length > 0)
            {
                await _azureStorage.UploadFile(image, "property", propertyId);
            }
            else
            {
                FlashMessage(_logger,
                    ($"File {image.FileName} has length zero",
                        "danger",
                        "File contains no data"));
            }

        return RedirectToAction("ListPropertyImages", "Property", new { propertyId });
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
        return RedirectToAction("ListPropertyImages", "Property", new { propertyId });
    }
}