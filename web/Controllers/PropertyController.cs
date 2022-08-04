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
        return AddNewProperty_Continue(1,0);
    }

    [Authorize(Roles = "Landlord")]
    [HttpGet("edit")]
    public ActionResult EditProperty(int propertyId)
    {
        var property = _propertyService.GetPropertyByPropertyId(propertyId);
        /*_propertyService.ChangePropertyToIncomplete(property);*/
        var propertyViewModel = PropertyViewModel.FromDbModel(property);
        // Start at step 1
        return View("EditProperty", new AddNewPropertyViewModel { Step = 1, Property = propertyViewModel });
    }

    [Authorize(Roles = "Landlord")]
    [HttpGet("add/step/{step:int}")]
    public ActionResult AddNewProperty_Continue([FromRoute] int step, int propertyId)
    {
        var newPropertyModel = new PropertyViewModel { Address = new PropertyAddress() };
        
        if (propertyId != 0)
        { 
            newPropertyModel = PropertyViewModel.FromDbModel(_propertyService.GetPropertyByPropertyId(propertyId));
        }

        newPropertyModel.PropertyId = propertyId;

        // Show the form for this step
        return View("AddNewProperty", new AddNewPropertyViewModel { Step = step, Property = newPropertyModel });
    }
    
    [Authorize(Roles = "Landlord")]
    [HttpGet("edit/step/{step:int}")]
    public ActionResult EditProperty_Continue([FromRoute] int step, int propertyId)
    {
        var newPropertyModel = PropertyViewModel.FromDbModel(_propertyService.GetPropertyByPropertyId(propertyId));
        newPropertyModel.PropertyId = propertyId;

        // Show the form for this step
        return View("EditProperty", new AddNewPropertyViewModel { Step = step, Property = newPropertyModel });
    }

    [Authorize(Roles = "Landlord")]
    [HttpPost("add/step/{step:int}")]
    public async Task<IActionResult> AddNewProperty_Continue([FromRoute] int step, int propertyId, [FromForm] PropertyViewModel newPropertyModel)
    {
        var landlordId = GetCurrentUser().LandlordId!.Value;
        newPropertyModel.PropertyId = propertyId;

        if (!ModelState.IsValid)
        {
            return View("AddNewProperty", new AddNewPropertyViewModel { Step = step, Property = newPropertyModel });
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
                return View("AddNewProperty",
                    new AddNewPropertyViewModel { Step = step, Property = newPropertyModel });
            }

            await _azureMapsApiService.AutofillAddress(newPropertyModel);

            // Create new record in the database for this property
            var newPropId = _propertyService.AddNewProperty(landlordId, newPropertyModel, isIncomplete: true);
            
            // Go to step 2
            return RedirectToAction("AddNewProperty_Continue", new { step = 2 , propertyId = newPropId});
        }
        
        if (step < AddNewPropertyViewModel.MaximumStep)
        {
            // Update the property's record with the values entered at this step
            _propertyService.UpdateProperty(property.Id, newPropertyModel, isIncomplete: true);
            return RedirectToAction("AddNewProperty_Continue", new { step = step + 1 , propertyId = property.Id});
        }
        
        // Update the property's record with the final set of values
        _propertyService.UpdateProperty(property.Id, newPropertyModel, isIncomplete: false);
        return RedirectToAction("ViewProperties", "Landlord");

    }
    
    
    [Authorize(Roles = "Landlord")]
    [HttpPost("edit/step/{step:int}")]
    public async Task<IActionResult> EditProperty_Continue([FromRoute] int step, int propertyId, [FromForm] PropertyViewModel newPropertyModel)
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

            _propertyService.UpdateProperty(propertyId, newPropertyModel, isIncomplete: false);
                // Go to step 2
             return View("EditProperty", new AddNewPropertyViewModel { Step = 2, Property = newPropertyModel });
        }

        // Update the property's record with the final set of values
        _propertyService.UpdateProperty(propertyId, newPropertyModel, isIncomplete: false);
        
        if (step < AddNewPropertyViewModel.MaximumStep)
        {
            // Update the property's record with the values entered at this step
            var newProperty = PropertyViewModel.FromDbModel(_propertyService.GetPropertyByPropertyId(propertyId));

            // Go to next step
            return View("EditProperty", new AddNewPropertyViewModel { Step = step + 1, Property = newProperty });
        }

        // Finished adding property, so go to View Properties page
        return RedirectToAction("ViewProperties", "Landlord");
    }

    [Authorize(Roles = "Landlord")]
    [HttpPost("add/cancel")]
    public ActionResult AddNewProperty_Cancel(int id)
    {
        // Get the property we're currently adding
        var property = _propertyService.GetPropertyByPropertyId(id);

        // Delete partially complete property
        _propertyService.DeleteProperty(property);
        _azureStorage.DeleteContainer("property", property.Id);

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
    
    [Authorize(Roles = "Landlord")]
    [HttpPost("edit/cancel")]
    public ActionResult EditProperty_Cancel()
    {
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
        var fileNames = await _azureStorage.ListFileNames("property", propertyId);
        var imageFiles = GetFilesFromFileNames(fileNames, propertyId);

        var imageList = new ImageListViewModel()
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

        List<string> flashTypes = new List<string>(),
            flashMessages = new List<string>();
        foreach (var image in images)
        {
            var isImageResult = _azureStorage.IsImage(image.FileName);
            if (!isImageResult.isImage)
            {
                _logger.LogInformation($"Failed to upload {image.FileName}: not in a recognised image format");
                flashTypes.Add("danger");
                flashMessages.Add($"{image.FileName} is not in a recognised image format. Please submit your images in one of the following formats: {isImageResult.imageExtString}");
            }
            else
            {
                if (image.Length > 0)
                {
                    string message = await _azureStorage.UploadFile(image, "property", propertyId);
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
        return RedirectToAction("ListPropertyImages", "Property", new{propertyId});
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
        return RedirectToAction("ListPropertyImages", "Property", new{propertyId});
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("SortProperties")]
    public IActionResult SortProperties(string sortBy, int page = 1, int propPerPage = 10)
    {
        List<PropertyDbModel> properties = _propertyService.SortProperties(sortBy);

        var listOfProperties = properties.Select(PropertyViewModel.FromDbModel).ToList();
        
        return View("~/Views/Admin/PropertyList.cshtml", new PropertiesDashboardViewModel(listOfProperties.Skip((page-1)*propPerPage).Take(propPerPage).ToList(),  listOfProperties.Count, null! , page, sortBy));
    }
}











































