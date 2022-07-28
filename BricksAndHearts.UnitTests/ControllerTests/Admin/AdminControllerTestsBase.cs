using BricksAndHearts.Controllers;
using BricksAndHearts.Services;
using FakeItEasy;

namespace BricksAndHearts.UnitTests.ControllerTests.Admin;

public class AdminControllerTestsBase: ControllerTestsBase
{
    protected readonly IAdminService adminService;
    protected readonly AdminController _underTest;

    protected AdminControllerTestsBase()
    {
        adminService = A.Fake<IAdminService>();
        _underTest = new(null!, adminService);
    }
}