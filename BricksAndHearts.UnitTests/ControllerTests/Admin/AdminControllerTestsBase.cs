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
    protected readonly ILandlordService LandlordService;
    protected readonly IPropertyService PropertyService;
    protected readonly AdminController UnderTest;

    protected AdminControllerTestsBase()
    {
        AdminService = A.Fake<IAdminService>();
        LandlordService = A.Fake<ILandlordService>();
        PropertyService = A.Fake<IPropertyService>();
        UnderTest = new AdminController(null!, AdminService, LandlordService, PropertyService);
    }
}
