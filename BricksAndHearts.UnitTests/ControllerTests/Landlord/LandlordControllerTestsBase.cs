using BricksAndHearts.Controllers;
using BricksAndHearts.Services;
using FakeItEasy;

namespace BricksAndHearts.UnitTests.ControllerTests.Landlord;

public class LandlordControllerTestsBase : ControllerTestsBase
{
    public static readonly IPropertyService propertyService = A.Fake<IPropertyService>();
    protected readonly LandlordController _underTest = new(null!, null!, new LandlordService(null!), propertyService,null!);
}