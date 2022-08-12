using System.Collections.Generic;
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
    public void AdminDashboard_WhenCalledByAnonymousUser_ReturnsViewWithLoginLink()
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
    public void AdminDashboard_WhenCalledByNonAdminUser_ReturnsViewWithLoginLink()
    {
        // Arrange
        var nonAdminNonLandlordUser = CreateNonAdminNonLandlordUser();
        MakeUserPrincipalInController(nonAdminNonLandlordUser, UnderTest);

        // Act
        var result = UnderTest.AdminDashboard() as ViewResult;

        // Assert
        result!.ViewData.Model.Should().BeOfType<AdminDashboardViewModel>()
            .Which.CurrentUser!.IsAdmin.Should().Be(false);
    }
    
    [Fact]
    public void AdminDashboard_WhenCalledByAdminUser_ReturnsViewWithLoginLink()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);

        // Act
        var result = UnderTest.AdminDashboard() as ViewResult;

        // Assert
        result!.ViewData.Model.Should().BeOfType<AdminDashboardViewModel>()
            .Which.CurrentUser!.IsAdmin.Should().Be(true);
    }

    [Fact]
    public void RequestAdminAccess_WhenCalledByNonAdmin_CallsRequestAdminAccessAndRedirectsToAdminDashboard()
    {
        // Arrange
        var nonAdminNonLandlordUser = CreateNonAdminNonLandlordUser();
        MakeUserPrincipalInController(nonAdminNonLandlordUser, UnderTest);

        // Act
        var result = UnderTest.RequestAdminAccess() as RedirectToActionResult;

        // Assert
        A.CallTo(() => AdminService.RequestAdminAccess(nonAdminNonLandlordUser)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>();
        result.Should().NotBeNull();
        result!.ActionName.Should().BeEquivalentTo("AdminDashboard");
    }
    
    [Fact]
    public void RequestAdminAccess_WhenCalledByAdmin_RedirectsToAdminDashboardView()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);

        // Act
        var result = UnderTest.RequestAdminAccess() as RedirectToActionResult;

        // Assert
        A.CallTo(() => AdminService.RequestAdminAccess(adminUser)).MustNotHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>();
        result.Should().NotBeNull();
        result!.ActionName.Should().BeEquivalentTo("AdminDashboard");
    }
    
    [Fact]
    public void CancelAdminAccess_WhenCalledByNonAdmin_CallsCancelAdminAccessRequestAndRedirectsToAdminDashboard()
    {
        // Arrange
        var nonAdminNonLandlordUser = CreateNonAdminNonLandlordUser();
        MakeUserPrincipalInController(nonAdminNonLandlordUser, UnderTest);

        // Act
        var result = UnderTest.CancelAdminAccessRequest() as RedirectToActionResult;

        // Assert
        A.CallTo(() => AdminService.CancelAdminAccessRequest(nonAdminNonLandlordUser)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>();
        result.Should().NotBeNull();
        result!.ActionName.Should().BeEquivalentTo("AdminDashboard");
    }
    
    [Fact]
    public void CancelAdminAccess_WhenCalledByAdmin_RedirectsToAdminDashboardView()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);

        // Act
        var result = UnderTest.CancelAdminAccessRequest() as RedirectToActionResult;

        // Assert
        A.CallTo(() => AdminService.CancelAdminAccessRequest(adminUser)).MustNotHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>();
        result.Should().NotBeNull();
        result!.ActionName.Should().BeEquivalentTo("AdminDashboard");
    }

    [Fact]
    public void AcceptAdminRequest_CallsApproveAdminAccessRequestAndRedirectsToGetAdminList()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);

        // Act
        var result = UnderTest.AcceptAdminRequest(1) as RedirectToActionResult;

        // Assert
        A.CallTo(() => AdminService.ApproveAdminAccessRequest(1)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>();
        result.Should().NotBeNull();
        result!.ActionName.Should().BeEquivalentTo("GetAdminList");
    }

    [Fact]
    public void RejectAdminRequest_CallsRejectAdminAccessRequestAndRedirectsToGetAdminList()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);

        // Act
        var result = UnderTest.RejectAdminRequest(1) as RedirectToActionResult;

        // Assert
        A.CallTo(() => AdminService.RejectAdminAccessRequest(1)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>();
        result.Should().NotBeNull();
        result!.ActionName.Should().BeEquivalentTo("GetAdminList");
    }
    
    [Fact]
    public void RemoveAdmin_OnOwnUserId_RedirectsToGetAdminListWithFlash()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);

        // Act
        var result = UnderTest.RemoveAdmin(1) as RedirectToActionResult;

        // Assert
        A.CallTo(() => AdminService.RemoveAdmin(1)).MustNotHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>();
        result.Should().NotBeNull();
        result!.ActionName.Should().BeEquivalentTo("GetAdminList");
        UnderTest.TempData["FlashMessage"].Should().Be("You may not remove your own admin status");
    }
    
    [Fact]
    public void RemoveAdmin_OnNonSelfUserId_CallsRemoveAdminAndRedirectsToGetAdminList()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);

        // Act
        var result = UnderTest.RemoveAdmin(2) as RedirectToActionResult;

        // Assert
        A.CallTo(() => AdminService.RemoveAdmin(2)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>();
        result.Should().NotBeNull();
        result!.ActionName.Should().BeEquivalentTo("GetAdminList");
    }

    [Fact]
    public void GetAdminList_CallsGetAdminListsAndReturnsViewWithAdminListModel()
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
    public async void LandlordList_CallsGetLandlordListAndReturnsLandlordListView()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);
        var landlordListModel = new LandlordListModel();
        
        // Act
        var result = await UnderTest.LandlordList(landlordListModel) as ViewResult;

        // Assert
        A.CallTo(() => AdminService.GetLandlordList(landlordListModel)).MustHaveHappened();
        result!.ViewData.Model.Should().BeOfType<LandlordListModel?>();
    }
    
    [Fact]
    public async void TenantList_CallsGetTenantListAndReturnsTenantListView()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);
        var tenantListModel = new TenantListModel();

        // Act
        var result = await UnderTest.TenantList(tenantListModel) as ViewResult;

        // Assert
        A.CallTo(() => AdminService.GetTenantList(tenantListModel.Filter)).MustHaveHappened();
        result!.ViewData.Model.Should().BeOfType<TenantListModel?>();
    }
    
    [Fact]
    public void GetInviteLink_CalledOnLinkedLandlord_CallsFindUserByLandlordIdAndRedirectsToLandlordProfileWithWarningFLash()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);
        var landlordUser = CreateUserDbModel(false, true);
        A.CallTo(() => AdminService.FindUserByLandlordId(1)).Returns(landlordUser);

        // Act
        var result = UnderTest.GetInviteLink(1) as RedirectToActionResult;

        // Assert
        A.CallTo(() => AdminService.FindUserByLandlordId(1)).MustHaveHappened();
        A.CallTo(() => AdminService.FindExistingInviteLink(1)).MustNotHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>();
        result.Should().NotBeNull();
        result!.ActionName.Should().BeEquivalentTo("Profile");
        UnderTest.TempData["FlashMessage"].Should().Be($"Landlord already linked to user {landlordUser.GoogleUserName}");
    }
    
    [Fact]
    public void GetInviteLink_CalledOnUnlinkedLandlordWithoutExistingLink_CallsFindExistingInviteLinkThenCallsCreateNewInviteLinkAndRedirectsToLandlordProfileWithSuccessFlash()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);
        A.CallTo(() => AdminService.FindUserByLandlordId(1)).Returns(null);
        A.CallTo(() => AdminService.FindExistingInviteLink(1)).Returns(null);
        A.CallTo(() => AdminService.CreateNewInviteLink(1)).Returns("new link");

        // Act
        var result = UnderTest.GetInviteLink(1) as RedirectToActionResult;

        // Assert
        A.CallTo(() => AdminService.FindUserByLandlordId(1)).MustHaveHappened();
        A.CallTo(() => AdminService.FindExistingInviteLink(1)).MustHaveHappened();
        A.CallTo(() => AdminService.CreateNewInviteLink(1)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>();
        result.Should().NotBeNull();
        result!.ActionName.Should().BeEquivalentTo("Profile");
        UnderTest.TempData["FlashMessage"].Should().Be("Successfully created a new invite link: http:///invite/new link");
    }
    
    [Fact]
    public void GetInviteLink_CalledOnUnlinkedLandlordWithExistingLink_CallsFindExistingInviteLinkAndRedirectsToLandlordProfileWithSuccessFlash()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);
        A.CallTo(() => AdminService.FindUserByLandlordId(1)).Returns(null);
        A.CallTo(() => AdminService.FindExistingInviteLink(1)).Returns("existing link");

        // Act
        var result = UnderTest.GetInviteLink(1) as RedirectToActionResult;

        // Assert
        A.CallTo(() => AdminService.FindUserByLandlordId(1)).MustHaveHappened();
        A.CallTo(() => AdminService.FindExistingInviteLink(1)).MustHaveHappened();
        A.CallTo(() => AdminService.CreateNewInviteLink(1)).MustNotHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>();
        result.Should().NotBeNull();
        result!.ActionName.Should().BeEquivalentTo("Profile");
        UnderTest.TempData["FlashMessage"].Should().Be("Landlord already has an invite link: http:///invite/existing link");
    }
    
    [Fact]
    public void RenewInviteLink_CalledOnLinkedLandlord_CallsFindUserByLandlordIdAndRedirectsToLandlordProfileWithFLash()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);
        var landlordUser = CreateUserDbModel(false, true);
        A.CallTo(() => AdminService.FindUserByLandlordId(1)).Returns(landlordUser);

        // Act
        var result = UnderTest.RenewInviteLink(1) as RedirectToActionResult;

        // Assert
        A.CallTo(() => AdminService.FindUserByLandlordId(1)).MustHaveHappened();
        A.CallTo(() => AdminService.FindExistingInviteLink(1)).MustNotHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>();
        result.Should().NotBeNull();
        result!.ActionName.Should().BeEquivalentTo("Profile");
        UnderTest.TempData["InviteLinkMessage"].Should().Be($"Landlord already linked to user {landlordUser.GoogleUserName}");
    }
    
    [Fact]
    public void RenewInviteLink_CalledOnUnlinkedLandlordWithoutExistingLink_CallsFindExistingInviteLinkThenCallsCreateNewInviteLinkAndRedirectsToLandlordProfileWithSuccessFlash()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);
        A.CallTo(() => AdminService.FindUserByLandlordId(1)).Returns(null);
        A.CallTo(() => AdminService.FindExistingInviteLink(1)).Returns(null);
        A.CallTo(() => AdminService.CreateNewInviteLink(1)).Returns("new link");

        // Act
        var result = UnderTest.RenewInviteLink(1) as RedirectToActionResult;

        // Assert
        A.CallTo(() => AdminService.FindUserByLandlordId(1)).MustHaveHappened();
        A.CallTo(() => AdminService.FindExistingInviteLink(1)).MustHaveHappened();
        A.CallTo(() => AdminService.DeleteExistingInviteLink(1)).MustNotHaveHappened();
        A.CallTo(() => AdminService.CreateNewInviteLink(1)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>();
        result.Should().NotBeNull();
        result!.ActionName.Should().BeEquivalentTo("Profile");
        UnderTest.TempData["InviteLinkMessage"].Should().Be("No existing invite link. Successfully created a new invite link :)");
    }
    
    [Fact]
    public void RenewInviteLink_CalledOnUnlinkedLandlordWithExistingLink_CallsFindExistingInviteLinkThenCallsDeleteExistingLinkThenCallsCreateNewInviteLinkAndRedirectsToLandlordProfileWithSuccessFlash()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);
        A.CallTo(() => AdminService.FindUserByLandlordId(1)).Returns(null);
        A.CallTo(() => AdminService.FindExistingInviteLink(1)).Returns("existing link");

        // Act
        var result = UnderTest.RenewInviteLink(1) as RedirectToActionResult;

        // Assert
        A.CallTo(() => AdminService.FindUserByLandlordId(1)).MustHaveHappened();
        A.CallTo(() => AdminService.FindExistingInviteLink(1)).MustHaveHappened();
        A.CallTo(() => AdminService.DeleteExistingInviteLink(1)).MustHaveHappened();
        A.CallTo(() => AdminService.CreateNewInviteLink(1)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>();
        result.Should().NotBeNull();
        result!.ActionName.Should().BeEquivalentTo("Profile");
        UnderTest.TempData["InviteLinkMessage"].Should().Be("Successfully created a new invite link :)");
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
