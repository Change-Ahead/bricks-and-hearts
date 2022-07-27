using System.Collections.Generic;
using BricksAndHearts.Controllers;
using BricksAndHearts.Database;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using FakeItEasy;

namespace BricksAndHearts.UnitTests.ControllerTests.Admin;

public class AdminControllerTestsBase: ControllerTestsBase
{
    protected readonly static IAdminService _underTestService = A.Fake<IAdminService>();
    
    protected readonly AdminController _underTest = new(null!, null!, _underTestService);

    protected AdminListModel CreateTestAdminListModel()
    {
        return new AdminListModel(new List<UserDbModel>(), new List<UserDbModel>());
    }
}