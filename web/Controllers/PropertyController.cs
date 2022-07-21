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
    private readonly BricksAndHeartsDbContext _dbContext;
    private readonly IPropertyService _propertyService;
    private readonly ILogger<PropertyController> _logger;

    public PropertyController(BricksAndHeartsDbContext dbContext, IPropertyService propertyService,
        ILogger<PropertyController> logger)
    {
        _dbContext = dbContext;
        _propertyService = propertyService;
        _logger = logger;
    }

    [HttpGet("add")]
    public ActionResult AddNewProperty_Begin()
    {
        // Start at the form for step 1
        return View("AddNewPropertyFormStep1");
    }

    [HttpPost("add")]
    public ActionResult AddNewProperty_Begin([FromForm] PropertyViewModel newPropertyModel)
    {
        var landlordId = GetCurrentUser().LandlordId;
        if (!landlordId.HasValue)
        {
            return StatusCode(403);
        }

        // This does checks based on the annotations (e.g. [Required]) on PropertyViewModel
        if (!ModelState.IsValid)
        {
            return RedirectToAction("AddNewProperty_Begin");
        }

        // Create new record in the database for this property
        _propertyService.AddNewProperty(newPropertyModel, landlordId.Value, isIncomplete: true);

        // Go to step 2
        return RedirectToAction("AddNewProperty_Continue", new { step = 2 });
    }

    [HttpGet("add/step/{step:int}")]
    public ActionResult AddNewProperty_Continue([FromRoute] int step)
    {
        // Show the form for this step
        return View("AddNewPropertyFormStep" + step);
    }

    [HttpPost("add/step/{step:int}")]
    public ActionResult AddNewProperty_Continue([FromRoute] int step, [FromForm] PropertyViewModel updateModel)
    {
        var landlordId = GetCurrentUser().LandlordId;
        if (!landlordId.HasValue)
        {
            return StatusCode(403);
        }
        
        // Get Id of the property we're currently adding
        var propertyId = _propertyService.GetIncompletePropertyId(landlordId.Value);
        if (!propertyId.HasValue)
        {
            // Start over
            return RedirectToAction("AddNewProperty_Begin");
        }

        // Update the property's record with the values entered at this step
        _propertyService.UpdateProperty(propertyId.Value, updateModel, isIncomplete: true);

        // Go to next step
        return RedirectToAction("AddNewProperty_Continue", new { step = step + 1 });
    }

    [HttpPost("add/submit")]
    public ActionResult AddNewProperty_Submit([FromForm] PropertyViewModel updateModel)
    {
        var landlordId = GetCurrentUser().LandlordId;
        if (!landlordId.HasValue)
        {
            return StatusCode(403);
        }
        
        // Get Id of the property we're currently adding
        var propertyId = _propertyService.GetIncompletePropertyId(landlordId.Value);
        if (!propertyId.HasValue)
        {
            // Start over
            return RedirectToAction("AddNewProperty_Begin");
        }

        // Update the property's record with the final set of values
        _propertyService.UpdateProperty(propertyId.Value, updateModel, isIncomplete: false);

        // Finished adding property, so go to View Properties page
        return RedirectToAction("ViewProperties", "Landlord");
    }
}