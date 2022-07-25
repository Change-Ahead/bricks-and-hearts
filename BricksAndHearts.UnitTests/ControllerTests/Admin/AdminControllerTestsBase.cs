using BricksAndHearts.Controllers;
using BricksAndHearts.ViewModels;

namespace BricksAndHearts.UnitTests.ControllerTests.Admin;

public class AdminControllerTestsBase: ControllerTestsBase
{
    protected readonly AdminController _underTest = new(null!, null!, null!);

    protected AdminListModel CreateTestAdminListModel()
    {
        return new AdminListModel(null!, null!);
    }
}