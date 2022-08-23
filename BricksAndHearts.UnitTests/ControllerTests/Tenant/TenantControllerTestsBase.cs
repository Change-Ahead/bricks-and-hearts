using System.Collections.Generic;
using System.IO;
using BricksAndHearts.Controllers;
using BricksAndHearts.Database;
using BricksAndHearts.Services;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;

namespace BricksAndHearts.UnitTests.ControllerTests.Tenant;

public class TenantControllerTestsBase : ControllerTestsBase
{
    protected readonly ILogger<TenantController> Logger;
    protected readonly ITenantService TenantService;
    protected readonly IPropertyService PropertyService;
    protected readonly ILandlordService LandlordService;
    protected readonly IMailService MailService;
    protected readonly ICsvImportService CsvImportService;
    protected readonly IAzureStorage AzureStorage;
    protected IEnumerable<string>? FlashMessages => UnderTest.TempData["FlashMessages"] as string[];
    protected readonly TenantController UnderTest;

    protected TenantControllerTestsBase()
    {
        Logger = A.Fake<ILogger<TenantController>>();
        TenantService = A.Fake<ITenantService>();
        PropertyService = A.Fake<IPropertyService>();
        LandlordService = A.Fake<ILandlordService>();
        MailService = A.Fake<IMailService>();
        CsvImportService = A.Fake<ICsvImportService>();
        AzureStorage = A.Fake<AzureStorage>();

        var httpContext = new DefaultHttpContext();
        var tempData = new TempDataDictionary(httpContext, A.Fake<ITempDataProvider>());
        UnderTest = new TenantController(Logger, TenantService, PropertyService, LandlordService, MailService, CsvImportService,AzureStorage){TempData = tempData};
    }
    
    protected IFormFile CreateExampleFile(string fileName, int length)
    {
        var stream = new MemoryStream();
        IFormFile fakeFile = new FormFile(stream, 0, length, null!, fileName);
        return fakeFile;
    }
}