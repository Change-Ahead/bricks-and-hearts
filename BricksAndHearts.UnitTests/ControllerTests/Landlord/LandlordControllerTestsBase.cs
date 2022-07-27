using BricksAndHearts.Controllers;
using BricksAndHearts.Services;
using FakeItEasy;

namespace BricksAndHearts.UnitTests.ControllerTests.Landlord;

public class LandlordControllerTestsBase : ControllerTestsBase
{
    protected readonly IPropertyService propertyService;
    protected readonly ILandlordService landlordService;
    protected readonly LandlordController _underTest;

    protected LandlordControllerTestsBase()
    {
        landlordService = A.Fake<ILandlordService>();
        propertyService = A.Fake<IPropertyService>();
        _underTest = new(null!, landlordService, propertyService, null!);
    }
}