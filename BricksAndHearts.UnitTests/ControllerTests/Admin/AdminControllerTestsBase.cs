using BricksAndHearts.Controllers;
using BricksAndHearts.Services;
using FakeItEasy;

namespace BricksAndHearts.UnitTests.ControllerTests.Admin;

public class AdminControllerTestsBase: ControllerTestsBase
{
    protected readonly IAdminService AdminService;
    protected readonly AdminController UnderTest;

    protected AdminControllerTestsBase()
    {
        AdminService = A.Fake<IAdminService>();
        UnderTest = new AdminController(null!, AdminService);
    }
}
