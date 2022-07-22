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
    private readonly IApiService _apiService;
    private readonly ILogger<PropertyController> _logger;

    public PropertyController(IPropertyService propertyService, ILogger<PropertyController> logger, IApiService apiService)
    {
        _propertyService = propertyService;
        _apiService = apiService;
        _logger = logger;
    }

    [HttpGet("add")]
    public ActionResult AddNewProperty_Begin()
    {
        // Start at step 1
        return AddNewProperty_Continue(1);
    }

    [HttpGet("add/step/{step:int}")]
    public ActionResult AddNewProperty_Continue([FromRoute] int step)
    {
        // Can replace with Role in future
        var landlordId = GetCurrentUser().LandlordId;
        if (!landlordId.HasValue)
        {
            return StatusCode(403);
        }

        // See if we're already adding a property
        var dbModel = _propertyService.GetIncompleteProperty(landlordId.Value);
        var property = dbModel == null
            ? new PropertyViewModel { Address = new PropertyAddress() }
            : PropertyViewModel.FromDbModel(dbModel);

        // Show the form for this step
        return View("AddNewProperty", new AddNewPropertyViewModel { Step = step, Property = property });
    }

    [HttpPost("add/step/{step:int}")]
    public async Task<IActionResult> AddNewProperty_Continue([FromRoute] int step, [FromForm] PropertyViewModel newPropertyModel)
    {
        var landlordId = GetCurrentUser().LandlordId;
        if (!landlordId.HasValue)
        {
            return StatusCode(403);
        }

        if (!ModelState.IsValid)
        {
            return View("AddNewProperty", new AddNewPropertyViewModel { Step = step, Property = newPropertyModel });
        }

        // Get the property we're currently adding
        var property = _propertyService.GetIncompleteProperty(landlordId.Value);
        if (property == null)
        {
            if (step == 1)
            {
                if (newPropertyModel.Address.AddressLine1 == null || newPropertyModel.Address.Postcode == null)
                {
                    // Address line 1 and postcode are the minimum information we need to create a new record
                    return View("AddNewProperty",
                        new AddNewPropertyViewModel { Step = step, Property = newPropertyModel });
                }
                else
                {
                    PropertyViewModel resultModel = await _apiService.AutofillAddress(newPropertyModel);
                    newPropertyModel = resultModel;

                    // Create new record in the database for this property
                    _propertyService.AddNewProperty(landlordId.Value, newPropertyModel, isIncomplete: true);
                }

                // Go to step 2
                return RedirectToAction("AddNewProperty_Continue", new { step = 2 });
            }
            else
            {
                // No property in progress
                return RedirectToAction("ViewProperties", "Landlord");
            }
        }
        else if (step < AddNewPropertyViewModel.MaximumStep)
        {
            // Update the property's record with the values entered at this step
            _propertyService.UpdateProperty(property.Id, newPropertyModel, isIncomplete: true);

            // Go to next step
            return RedirectToAction("AddNewProperty_Continue", new { step = step + 1 });
        }
        else
        {
            // Update the property's record with the final set of values
            _propertyService.UpdateProperty(property.Id, newPropertyModel, isIncomplete: false);

            // Finished adding property, so go to View Properties page
            return RedirectToAction("ViewProperties", "Landlord");
        }
    }

    [HttpPost("add/cancel")]
    public ActionResult AddNewProperty_Cancel()
    {
        var landlordId = GetCurrentUser().LandlordId;
        if (!landlordId.HasValue)
        {
            return StatusCode(403);
        }

        // Get the property we're currently adding
        var property = _propertyService.GetIncompleteProperty(landlordId.Value);
        if (property == null)
        {
            // No property in progress
            return RedirectToAction("ViewProperties", "Landlord");
        }

        // Delete partially complete property
        _propertyService.DeleteProperty(property);

        // Go to View Properties page
        return RedirectToAction("ViewProperties", "Landlord");
    }
}