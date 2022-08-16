using System.Collections.Generic;
using System.IO;
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
    protected readonly ICsvImportService CsvImportService;
    protected readonly IMailService MailService;
    protected IEnumerable<string>? FlashMessages => UnderTest.TempData["FlashMessages"] as List<string>;
    protected readonly AdminController UnderTest;

    protected AdminControllerTestsBase()
    {
        Logger = A.Fake<ILogger<AdminController>>();
        AdminService = A.Fake<IAdminService>();
        LandlordService = A.Fake<ILandlordService>();
        PropertyService = A.Fake<IPropertyService>();
        CsvImportService = A.Fake<ICsvImportService>();
        MailService = A.Fake<IMailService>();
        var httpContext = new DefaultHttpContext();
        var tempData = new TempDataDictionary(httpContext, A.Fake<ITempDataProvider>());
        UnderTest = new AdminController(Logger, AdminService, LandlordService, PropertyService, CsvImportService, MailService){TempData = tempData};
    }

    protected IFormFile CreateExampleFile(string fileName, int length)
    {
        var stream = new MemoryStream();
        IFormFile fakeFile = new FormFile(stream, 0, length, null!, fileName);
        return fakeFile;
    }
}