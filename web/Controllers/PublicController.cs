using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BricksAndHearts.Controllers;


[Route("/public")]
public class PublicController : AbstractController
{
    private readonly ILogger<PublicController> _logger;
    private readonly IPropertyService _propertyService;

    public PublicController(ILogger<PublicController> logger, IPropertyService propertyService)
    {
        _logger = logger;
        _propertyService = propertyService;
    }

    [HttpGet]
    [Route("/public/propertyId/{propertyId:int}/{publicViewLink}")]
    public IActionResult ViewPublicProperty(int propertyId,string publicViewLink)
    {
        var property = _propertyService.GetPropertyByPropertyId(propertyId);
        if (property == null)
        {
            var publicProperty = new PublicPropertyViewModel
            {
                SearchResult = PublicPropertySearchResult.IncorrectPublicViewLink
            };
            return View(publicProperty);
        }
        if (property.PublicViewLink == null || property.PublicViewLink != publicViewLink)
        {
            var publicProperty = new PublicPropertyViewModel
            {
                SearchResult = PublicPropertySearchResult.IncorrectPublicViewLink
            };
            return View(publicProperty);
        }
        else
        {
            var publicProperty = PublicPropertyViewModel.FromDbModel(property); // FromDbModel sets SearchResult as Success automatically
            return View(publicProperty);
        }
    }
}