using BricksAndHearts.Controllers;
using BricksAndHearts.Services;
using FakeItEasy;

namespace BricksAndHearts.UnitTests.ControllerTests.Admin;

public class AdminControllerTestsBase : ControllerTestsBase
{
    protected readonly IAdminService AdminService;
    protected readonly ICsvImportService CsvImportService;
    protected readonly ILandlordService LandlordService;
    protected readonly IPropertyService PropertyService;
    protected readonly AdminController UnderTest;

    protected AdminControllerTestsBase()
    {
        AdminService = A.Fake<IAdminService>();
        LandlordService = A.Fake<ILandlordService>();
        PropertyService = A.Fake<IPropertyService>();
        CsvImportService = A.Fake<ICsvImportService>();
        UnderTest = new AdminController(null!, AdminService, LandlordService, PropertyService, CsvImportService);
    }
}