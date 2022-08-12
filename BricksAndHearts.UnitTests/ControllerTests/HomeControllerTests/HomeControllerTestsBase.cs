using BricksAndHearts.Controllers;
using FakeItEasy;
using Microsoft.Extensions.Logging;

namespace BricksAndHearts.UnitTests.ControllerTests.HomeControllerTests;

public class HomeControllerTestsBase : ControllerTestsBase
{
    protected readonly ILogger<HomeController> Logger;
    protected readonly HomeController UnderTest;

    protected HomeControllerTestsBase()
    {
        Logger = A.Fake<ILogger<HomeController>>();
        UnderTest = new HomeController(Logger);
    }
}