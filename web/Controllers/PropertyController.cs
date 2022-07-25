using BricksAndHearts.Database;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BricksAndHearts.Controllers;

public class PropertyController : Controller
{
    private readonly BricksAndHeartsDbContext _dbContext;
    private readonly ILandlordService _landlordService;
    private readonly ILogger<LandlordController> _logger;
    private readonly IAzureStorage _azureStorage;
    
    public PropertyController(ILogger<PropertyController> logger, BricksAndHeartsDbContext dbContext,
        ILandlordService landlordService, IAzureStorage azureStorage)
    {
        _dbContext = dbContext;
        //_logger = logger;
        _landlordService = landlordService;
        _azureStorage = azureStorage;
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