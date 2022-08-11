using System.Collections.Generic;
using BricksAndHearts.Database;
using BricksAndHearts.ViewModels;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BricksAndHearts.UnitTests.ControllerTests.Admin;

public class AdminControllerTests : AdminControllerTestsBase
{
    [Fact]
    public void Index_WhenCalledByAnonymousUser_ReturnsViewWithLoginLink()
    {
        // Arrange
        UnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _anonUser }
        };

        // Act
        var result = UnderTest.AdminDashboard() as ViewResult;

        // Assert
        result!.ViewData.Model.Should().BeOfType<AdminDashboardViewModel>()
            .Which.CurrentUser.Should().BeNull();
    }

    [Fact]
    public async void LandlordList_WhenCalled_CallsGetLandlordListAndReturnsLandlordListView()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);

        // Act
        var result = await UnderTest.LandlordList() as ViewResult;

        // Assert
        A.CallTo(() => AdminService.GetLandlordDisplayList("")).MustHaveHappened();
        result!.ViewData.Model.Should().BeOfType<LandlordListModel?>();
    }
    
    [Fact]
    public async void TenantList_WhenCalled_CallsGetTenantListAndReturnsTenantListView()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);

        // Act
        var result = await UnderTest.TenantList() as ViewResult;

        // Assert
        A.CallTo(() => AdminService.GetTenantList()).MustHaveHappened();
        result!.ViewData.Model.Should().BeOfType<TenantListModel?>();
    }

    [Fact]
    public void GetAdminList_WhenCalled_CallsGetAdminListsAndReturnsViewWithAdminListModel()
    {
        // Arrange
        MakeUserPrincipalInController(CreateAdminUser(), UnderTest);

        // Act
        var result = UnderTest.GetAdminList().Result as ViewResult;

        // Assert
        A.CallTo(() => AdminService.GetAdminLists()).MustHaveHappened();
        result!.ViewData.Model.Should().BeOfType<AdminListModel>();
    }

    [Fact]
    public void GetFilteredTenants_WithCorrectInput_CallsGetTenantDbModelsFromFilter()
    {
        // Arrange
        MakeUserPrincipalInController(CreateAdminUser(), UnderTest);
        var fakeTenantListModel = A.Fake<TenantListModel>();

        // Act
        A.CallTo(() => AdminService.GetTenantDbModelsFromFilter(fakeTenantListModel.Filters)).Returns(A.Fake<List<TenantDbModel>>());
        var result = UnderTest.GetFilteredTenants(fakeTenantListModel).Result;
        
        // Assert
        A.CallTo(() => AdminService.GetTenantDbModelsFromFilter(fakeTenantListModel.Filters)).MustHaveHappened();
        result.Should().BeOfType<ViewResult>().Which.Model.Should().Be(fakeTenantListModel);
    }
    
    [Fact]
    public async void ImportTenants_WhenCalledWithEmptyFile_RedirectsToTenantListWithErrorFlashMessage()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);
        var csvFile = CreateExampleFile("fakeFile.csv", 0);

        // Act
        var result = await UnderTest.ImportTenants(csvFile) as RedirectToActionResult;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        result.Should().NotBeNull();
        result!.ActionName.Should().BeEquivalentTo("TenantList");
        UnderTest.TempData["FlashMessage"].Should().Be(
            $"{csvFile.FileName} contains no data. Please upload a file containing the tenant data you would like to import.");
       
    }
    
    [Fact]
    public async void ImportTenants_WhenCalledWithNonCsvFile_RedirectsToTenantListWithErrorFlashMessage()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);
        var csvFile = CreateExampleFile("fakeFile.doc", 1);

        // Act
        var result = await UnderTest.ImportTenants(csvFile) as RedirectToActionResult;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        result.Should().NotBeNull();
        result!.ActionName.Should().BeEquivalentTo("TenantList");
        UnderTest.TempData["FlashMessage"].Should().Be(
            $"{csvFile.FileName} is not a CSV file. Please upload your data as a CSV file.");
    }
    
    [Fact]
    public async void ImportTenants_WhenCalledWithCsvFileWithMissingColumns_CallsCheckIfImportWorksAndDoesNotCallImportTenantsAndRedirectsToTenantList()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);
        var csvFile = CreateExampleFile("fakeFile.csv", 1);

        List<string> flashTypes = new List<string> { "danger" };
        List<string> flashMessages = new List<string> { "Import has failed because column MissingColumn is missing. Please add this column to your records before attempting to import them."};
        var flashResponse = (flashTypes, flashMessages);
        A.CallTo(() => CsvImportService.CheckIfImportWorks(csvFile))
            .Returns(flashResponse);
        
        // Act
        var result = await UnderTest.ImportTenants(csvFile) as RedirectToActionResult;

        // Assert
        A.CallTo(() => CsvImportService.CheckIfImportWorks(csvFile)).MustHaveHappened();
        A.CallTo(() => CsvImportService.ImportTenants(csvFile, flashResponse)).MustNotHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>();
        result.Should().NotBeNull();
        result!.ActionName.Should().BeEquivalentTo("TenantList");
    }
    
    [Fact]
    public async void ImportTenants_WhenCalledWithCsvFileWithMissingColumns_CallsCheckIfImportWorksAndThenCallsImportTenantsAndRedirectsToTenantList()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);
        var csvFile = CreateExampleFile("fakeFile.csv", 1);

        List<string> flashTypes = new List<string>();
        List<string> flashMessages = new List<string>();
        var flashResponse = (flashTypes, flashMessages);
        A.CallTo(() => CsvImportService.CheckIfImportWorks(csvFile))
            .Returns(flashResponse);
        
        // Act
        var result = await UnderTest.ImportTenants(csvFile) as RedirectToActionResult;

        // Assert
        A.CallTo(() => CsvImportService.CheckIfImportWorks(csvFile)).MustHaveHappened();
        A.CallTo(() => CsvImportService.ImportTenants(csvFile, flashResponse)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>();
        result.Should().NotBeNull();
        result!.ActionName.Should().BeEquivalentTo("TenantList");
    }
}
