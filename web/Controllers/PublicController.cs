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
    [Route("/public/propertyId/{propertyId:int}")]
    public IActionResult ViewPublicProperty(int propertyId)
    {
        var property = _propertyService.GetPropertyByPropertyId(propertyId);
        var publicProperty = PublicPropertyViewModel.FromDbModel(property);
        return View(publicProperty);
    }
}