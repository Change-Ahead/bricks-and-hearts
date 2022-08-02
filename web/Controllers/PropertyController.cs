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
        // Start at step 1
        return AddNewProperty_Continue(1);
    }

    [Authorize(Roles = "Landlord")]
    [HttpGet("add/step/{step:int}")]
    public ActionResult AddNewProperty_Continue([FromRoute] int step)
    {
        var landlordId = GetCurrentUser().LandlordId!.Value;

        // See if we're already adding a property
        var dbModel = _propertyService.GetIncompleteProperty(landlordId);
        var property = dbModel == null
            ? new PropertyViewModel { Address = new PropertyAddress() }
            : PropertyViewModel.FromDbModel(dbModel);

        // Show the form for this step
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

        // Get the property we're currently adding
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
                // No property in progress
                return RedirectToAction("ViewProperties", "Landlord");
            }
            
            // Update the property's record with the values entered at this step
            _propertyService.UpdateProperty(property.Id, newPropertyModel, isIncomplete: true);

            // Go to next step
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

            // Finished adding property, so go to View Properties page
            return RedirectToAction("ViewProperties", "Landlord");
        }
    }

    [Authorize(Roles = "Landlord")]
    [HttpPost("add/cancel")]
    public async Task<ActionResult> AddNewProperty_Cancel()
    {
        var landlordId = GetCurrentUser().LandlordId!.Value;

        // Get the property we're currently adding
        var property = _propertyService.GetIncompleteProperty(landlordId);
        if (property == null)
        {
            // No property in progress
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
    
    [Authorize(Roles = "Admin")]
    [HttpGet]
    [Route("/admin/view-properties/landlordId/{landlordId:int}")]
    public IActionResult AdminViewProperties(int landlordId)
    {
        var databaseResult = _propertyService.GetPropertiesByLandlord(landlordId);
        var listOfProperties = databaseResult.Select(PropertyViewModel.FromDbModel).ToList();
        return View("../Admin/ViewLandlordProperties", new PropertiesDashboardViewModel(listOfProperties, landlordId));
    }
    
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/add-property/landlordId/{landlordId:int?}")]
    public ActionResult AdminAddNewProperty_Begin(int? landlordId)
    {
        if (!landlordId.HasValue)
        {
            throw new ArgumentException("Landlord Id should not be null");
        }

        // Start at step 1
        return AdminAddNewProperty_Continue(1, landlordId.Value);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin/add-property/landlordId/{landlordId:int}/step/{step:int}")]
    public ActionResult AdminAddNewProperty_Continue(int step, int landlordId)
    {
        // See if we're already adding a property
        var dbModel = _propertyService.GetIncompleteProperty(landlordId);
        var property = dbModel == null
            ? new PropertyViewModel { Address = new PropertyAddress() }
            : PropertyViewModel.FromDbModel(dbModel);
        // Show the form for this step
        return View("AdminManageProperty/AddNewProperty", new AddNewPropertyViewModel { Step = step, Property = property, LandlordId = landlordId});
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("admin/add-property/landlordId/{landlordId:int}/step/{step:int}")]
    public async Task<IActionResult> AdminAddNewProperty_Continue([FromRoute] int step, [FromRoute] int? landlordId,
        [FromForm] PropertyViewModel newPropertyModel)
    {
        if (!landlordId.HasValue)
        {
            throw new ArgumentException("LandlordId should not be null");
        }
        if (!ModelState.IsValid)
        {
            return View("AdminManageProperty/AddNewProperty", new AddNewPropertyViewModel { Step = step, Property = newPropertyModel, LandlordId = landlordId});
        }

        // Get the property we're currently adding
        var property = _propertyService.GetIncompleteProperty(landlordId.Value);
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
                return View("AdminManageProperty/AddNewProperty",
                    new AddNewPropertyViewModel { Step = step, Property = newPropertyModel, LandlordId = landlordId.Value});
            }

            await _azureMapsApiService.AutofillAddress(newPropertyModel);

            if (property == null)
            {
                // Create new record in the database for this property
                _propertyService.AddNewProperty(landlordId.Value, newPropertyModel, isIncomplete: true);
            }
            else
            {
                _propertyService.UpdateProperty(property.Id, newPropertyModel, isIncomplete: true);
            }

            // Go to step 2
            return RedirectToAction("AdminAddNewProperty_Continue", new { step = 2, landlordId = landlordId.Value } );
        }
        else if (step < AddNewPropertyViewModel.MaximumStep)
        {
            if (property == null)
            {
                // No property in progress
                return RedirectToAction("AdminViewProperties",new {landlordId = landlordId.Value });
            }

            // Update the property's record with the values entered at this step
            _propertyService.UpdateProperty(property.Id, newPropertyModel, isIncomplete: true);

            // Go to next step
            return RedirectToAction("AdminAddNewProperty_Continue", new { step = step + 1, landlordId = landlordId.Value });
        }
        else
        {
            if (property == null)
            {
                // No property in progress
                return RedirectToAction("AdminViewProperties",new { landlordId = landlordId.Value });
            }

            // Update the property's record with the final set of values
            _propertyService.UpdateProperty(property.Id, newPropertyModel, isIncomplete: false);

            // Finished adding property, so go to View Properties page
            return RedirectToAction("AdminViewProperties",new{ landlordId = landlordId.Value });
        }
    }
    
    [Authorize(Roles = "Admin")]
    [HttpPost("admin/add-property/cancel/landlordId/{landlordId:int?}")]
    public ActionResult AdminAddNewProperty_Cancel([FromRoute] int? landlordId)
    {
        if (!landlordId.HasValue)
        {
            throw new ArgumentException("LandlordId should not be null");
        }
        
        // Get the property we're currently adding
        var property = _propertyService.GetIncompleteProperty(landlordId.Value);
        if (property == null)
        {
            // No property in progress
            return RedirectToAction("AdminViewProperties", new { landlordId = landlordId.Value});
        }

        // Delete partially complete property
        _propertyService.DeleteProperty(property);

        // Go to View Properties page
        return RedirectToAction("AdminViewProperties", new { landlordId = landlordId.Value});
    }
}