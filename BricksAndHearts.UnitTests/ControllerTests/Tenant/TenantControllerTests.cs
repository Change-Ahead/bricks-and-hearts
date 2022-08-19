using System.Collections.Generic;
using BricksAndHearts.Database;
using BricksAndHearts.ViewModels;
using FakeItEasy;
using FluentAssertions; 
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BricksAndHearts.UnitTests.ControllerTests.Tenant;

public class TenantControllerTests : TenantControllerTestsBase
{

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
        FlashMessages.Should().Contain(
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
        FlashMessages.Should().Contain(
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
        A.CallTo(() => CsvImportService.ImportTenants(csvFile)).MustNotHaveHappened();
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
        A.CallTo(() => CsvImportService.ImportTenants(csvFile)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>();
        result.Should().NotBeNull();
        result!.ActionName.Should().BeEquivalentTo("TenantList");
    }

    [Fact]
    public async void TenantMatchList_CalledWithAnyInput_ReturnsTenantMatchListAndGetsNearestTenants()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);
        
        
        // Act
        A.CallTo(() => PropertyService.GetPropertyByPropertyId(1)).Returns(A.Fake<PropertyDbModel>());
        A.CallTo(() => TenantService.GetNearestTenantsToProperty(A.Fake<PropertyViewModel>())).Returns(A.Fake<List<TenantDbModel>>());
        var result = await UnderTest.TenantMatchList(1);

        // Assert
        A.CallTo(() => TenantService.GetNearestTenantsToProperty(A<PropertyViewModel>.Ignored)).MustHaveHappened();
        A.CallTo(() => PropertyService.GetPropertyByPropertyId(1)).MustHaveHappened();
        result.Should().BeOfType<ViewResult>().Which.ViewName.Should().Be("~/Views/Admin/TenantMatchList.cshtml");
    }

    [Fact]
    public void SendMatchLinkEmail_CalledWithAnyInput_ReturnsTenantMatchListAndSendsEmail()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);
        
        // Act
        A.CallTo(() => MailService.TrySendMsgInBackground(A<string>.Ignored, A<string>.Ignored, A<List<string>>.Ignored)).DoesNothing();
        var result = UnderTest.SendMatchLinkEmail("test", "a@b.com", 1);

        // Assert
        A.CallTo(() => MailService.TrySendMsgInBackground(A<string>.Ignored,
                                                                        A<string>.Ignored,
                                                                            A<List<string>>.Ignored)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("TenantMatchList");
    }
}