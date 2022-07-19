using BricksAndHearts.Controllers;
using BricksAndHearts.Services;

namespace BricksAndHearts.UnitTests.ControllerTests.Landlord;

public class LandlordControllerTestsBase : ControllerTestsBase
{
    protected readonly LandlordController _underTest = new(null, null, new LandlordService(null));
}