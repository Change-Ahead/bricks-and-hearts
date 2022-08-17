using BricksAndHearts.Controllers;
using BricksAndHearts.Services;
using BricksAndHearts.UnitTests.ControllerTests;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;

namespace BricksAndHearts.UnitTests.ServiceTests.Tenant;

public class TenantControllerTestsBase : ControllerTestsBase
{
    protected readonly ILogger<TenantController> Logger;
    protected readonly ITenantService TenantService;
    protected readonly TenantController UnderTest;

    protected TenantControllerTestsBase()
    {
        Logger = A.Fake<ILogger<TenantController>>();
        TenantService = A.Fake<ITenantService>();
        var httpContext = new DefaultHttpContext();
        var tempData = new TempDataDictionary(httpContext, A.Fake<ITempDataProvider>());
        UnderTest = new TenantController(Logger, TenantService){TempData = tempData};
    }
}