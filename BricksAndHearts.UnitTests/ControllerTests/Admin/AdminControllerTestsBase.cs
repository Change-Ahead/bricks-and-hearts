using BricksAndHearts.Controllers;

namespace BricksAndHearts.UnitTests.ControllerTests.Admin;

public class AdminControllerTestsBase: ControllerTestsBase
{
    protected readonly AdminController _underTest = new(null, null, null);
}