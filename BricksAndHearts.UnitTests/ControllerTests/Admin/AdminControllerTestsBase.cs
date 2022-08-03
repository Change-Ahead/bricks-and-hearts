using System.Collections.Generic;
using BricksAndHearts.Controllers;
using BricksAndHearts.Database;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
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
