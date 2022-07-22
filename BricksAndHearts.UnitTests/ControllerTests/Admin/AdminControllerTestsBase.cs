using BricksAndHearts.Controllers;
using BricksAndHearts.Services;
using FakeItEasy;

namespace BricksAndHearts.UnitTests.ControllerTests.Admin;

public class AdminControllerTestsBase: ControllerTestsBase
{
    public static readonly IAdminService adminService = A.Fake<IAdminService>();
    protected readonly AdminController _underTest = new(null!, null!, adminService);
}