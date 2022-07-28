using BricksAndHearts.Controllers;
using BricksAndHearts.Services;
using FakeItEasy;

namespace BricksAndHearts.UnitTests.ControllerTests.Landlord;

public class LandlordControllerTestsBase : ControllerTestsBase
{
    protected readonly IPropertyService PropertyService;
    protected readonly ILandlordService LandlordService;
    protected readonly LandlordController UnderTest;

    protected LandlordControllerTestsBase()
    {
        LandlordService = A.Fake<ILandlordService>();
        PropertyService = A.Fake<IPropertyService>();
        UnderTest = new LandlordController(null!, LandlordService, PropertyService, null!);
    }
}
