using System.Text.RegularExpressions;
using BricksAndHearts.Database;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

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
    public async Task<IActionResult> AddNewProperty_Continue([FromRoute] int step, [FromForm] PropertyViewModel newPropertyModel)
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
    public ActionResult AddNewProperty_Cancel()
    {
        var landlordId = GetCurrentUser().LandlordId!.Value;
        var property = _propertyService.GetIncompleteProperty(landlordId);
        if (property == null)
        {
            return RedirectToAction("ViewProperties", "Landlord");
        }
        _propertyService.DeleteProperty(property);
        
        return RedirectToAction("ViewProperties", "Landlord");
    }
    
    [HttpGet("addImages/{propertyId:int}")]
    public IActionResult AddPropertyImages(int propertyId)
    {
        return View(propertyId);
    }

    [HttpPost("addImages/{propertyId:int}")]
    public async Task<IActionResult> AddPropertyImages([FromForm] List<IFormFile> images, [FromRoute] int propertyId)
    {
        foreach (var image in images)
        {
            if (image.Length > 0)
            {
                await _azureStorage.UploadFileAsync(image, "property", propertyId);
            }
        }
        return RedirectToAction("ViewProperties", "Landlord");
    }

    [HttpGet]
    public async Task<IActionResult> ListPropertyImages(int propertyId)
    {
        List<string> fileNames = await _azureStorage.ListFilesAsync("property", propertyId);
        return View(fileNames);
    }
    
    [HttpPost]
    public async Task<IActionResult> DisplayPropertyImage(string containerName, string fileName)
    {
        var image = await _azureStorage.DownloadFileAsync(containerName, fileName);
        return File(image, "image/jpeg");
    }
    
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
        var propertyViewModel = PropertyViewModel.FromDbModel(model);
        return View(propertyViewModel);
    }
}