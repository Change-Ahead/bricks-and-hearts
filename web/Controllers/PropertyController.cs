using BricksAndHearts.Database;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BricksAndHearts.Controllers;

public class PropertyController : AbstractController
{
    private readonly BricksAndHeartsDbContext _dbContext;
    private readonly IPropertyService _propertyService;
    private readonly ILogger<LandlordController> _logger;
    private readonly IAzureStorage _azureStorage;
    
    public PropertyController(ILogger<PropertyController> logger, BricksAndHeartsDbContext dbContext,
        IPropertyService propertyService, IAzureStorage azureStorage)
    {
        _dbContext = dbContext;
        //_logger = logger;
        _propertyService = propertyService;
        _azureStorage = azureStorage;
    }
    
    [HttpPost]
    [Route("/add-property")]
    public ActionResult AddNewProperty([FromForm] PropertyViewModel newPropertyModel)
    {
        var landlordId = GetCurrentUser().LandlordId;
        if (!landlordId.HasValue)
        {
            return StatusCode(403);
        }
        if (!ModelState.IsValid)
        {
            return View(newPropertyModel);
        }
        _propertyService.AddNewProperty(newPropertyModel, landlordId.Value);
        _azureStorage.CreateContainerAsync(newPropertyModel.Id);

        return RedirectToAction("ViewProperties", "Landlord");
    }
    
    [HttpPost("UploadFiles")]
    public async Task<IActionResult> UploadImage(List<IFormFile> images)
    {
        foreach (var image in images)
        {
            if (image.Length > 0)
            {
                await _azureStorage.UploadAsync(image);
            }
        }
        return RedirectToAction("ViewProperties","Landlord");
    }
    
    public async Task<IActionResult> DownloadImage(List<IFormFile> images)
    {
        foreach (var image in images)
        {
            if (image.Length > 0)
            {
                await _azureStorage.UploadAsync(image);
            }
        }
        return RedirectToAction("ViewProperties","Landlord");
    }
    
    public async Task<IActionResult> DeleteImage(List<IFormFile> images)
    {
        foreach (var image in images)
        {
            if (image.Length > 0)
            {
                await _azureStorage.UploadAsync(image);
            }
        }
        return RedirectToAction("ViewProperties","Landlord");
    }
}