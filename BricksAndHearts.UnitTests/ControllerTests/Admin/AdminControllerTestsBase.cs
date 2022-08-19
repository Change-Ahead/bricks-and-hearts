using System.Collections.Generic;
using BricksAndHearts.Controllers;
using BricksAndHearts.Services;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;

namespace BricksAndHearts.UnitTests.ControllerTests.Admin;

public class AdminControllerTestsBase : ControllerTestsBase
{
    protected readonly ILogger<AdminController> Logger;
    protected readonly IAdminService AdminService;
    protected readonly ILandlordService LandlordService;
    protected readonly IPropertyService PropertyService;
    protected IEnumerable<string>? FlashMessages => UnderTest.TempData["FlashMessages"] as List<string>;
    protected readonly AdminController UnderTest;

    protected AdminControllerTestsBase()
    {
        Logger = A.Fake<ILogger<AdminController>>();
        AdminService = A.Fake<IAdminService>();
        LandlordService = A.Fake<ILandlordService>();
        PropertyService = A.Fake<IPropertyService>();
        var httpContext = new DefaultHttpContext();
        var tempData = new TempDataDictionary(httpContext, A.Fake<ITempDataProvider>());
        UnderTest = new AdminController(Logger, AdminService, LandlordService, PropertyService){TempData = tempData};
    }
}