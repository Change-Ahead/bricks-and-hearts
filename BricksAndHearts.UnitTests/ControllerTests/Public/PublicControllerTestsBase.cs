using BricksAndHearts.Controllers;
using BricksAndHearts.Database;
using BricksAndHearts.Services;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;

namespace BricksAndHearts.UnitTests.ControllerTests.Public;

public class PublicControllerTestsBase : ControllerTestsBase
{
    protected readonly IPropertyService PropertyService;
    protected PublicController UnderTest { get; }

    protected PublicControllerTestsBase()
    {
        PropertyService = A.Fake<IPropertyService>();
        var logger = A.Fake<ILogger<PublicController>>();
        var httpContext = new DefaultHttpContext();
        var tempData = new TempDataDictionary(httpContext, A.Fake<ITempDataProvider>());
        UnderTest = new PublicController(logger,PropertyService){TempData = tempData};
    }
    
    protected static PropertyDbModel CreateExamplePropertyDbModel()
    {
        return new PropertyDbModel
        {
            LandlordId = 1,
            Id = 1,
            AddressLine1 = "10 Downing Street",
            Postcode = new PostcodeDbModel
            {
                Postcode = "SW1A 2AA"
            },
            NumOfBedrooms = 2,
            Rent = 750,
            Description = "Property description"
        };
    }
}