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
    public void AdminDashboard_WhenCalledByAnonymousUser_RedirectsToIndex()
    {
        // Arrange
        UnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = AnonUser }
        };

        // Act
        var result = UnderTest.AdminDashboard() as RedirectToActionResult;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        result.Should().NotBeNull();
        result!.ActionName.Should().BeEquivalentTo("Index");
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
        FlashMessages.Should().Contain("You may not remove your own admin status");
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
    public async void GetAdminList_CallsGetAdminListsAndReturnsViewWithAdminListModel()
    {
        // Arrange
        MakeUserPrincipalInController(CreateAdminUser(), UnderTest);

        // Act
        var result = await UnderTest.GetAdminList() as ViewResult;

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

        // Act
        var result = await UnderTest.LandlordList() as ViewResult;

        // Assert
        A.CallTo(() => AdminService.GetLandlordList(null, null, 1, 10)).MustHaveHappened();
        result!.ViewData.Model.Should().BeOfType<LandlordListModel?>();
    }

    [Fact]
    public void
        GetInviteLink_CalledOnLinkedLandlord_CallsFindUserByLandlordIdAndRedirectsToLandlordProfileWithWarningFLash()
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
        FlashMessages.Should().Contain($"Landlord already linked to user {landlordUser.GoogleUserName}");
    }

    [Fact]
    public void
        GetInviteLink_CalledOnUnlinkedLandlordWithoutExistingLink_CallsFindExistingInviteLinkThenCallsCreateNewInviteLinkAndRedirectsToLandlordProfileWithSuccessFlash()
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
        FlashMessages.Should().Contain("Successfully created a new invite link: http:///landlord/invite/new link");
    }

    [Fact]
    public void
        GetInviteLink_CalledOnUnlinkedLandlordWithExistingLink_CallsFindExistingInviteLinkAndRedirectsToLandlordProfileWithSuccessFlash()
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
        FlashMessages.Should().Contain("Landlord already has an invite link: http:///landlord/invite/existing link");
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
        FlashMessages.Should().Contain($"Landlord already linked to user {landlordUser.GoogleUserName}");
    }

    [Fact]
    public void
        RenewInviteLink_CalledOnUnlinkedLandlordWithoutExistingLink_CallsFindExistingInviteLinkThenCallsCreateNewInviteLinkAndRedirectsToLandlordProfileWithSuccessFlash()
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
        FlashMessages.Should().Contain("No existing invite link. Successfully created a new invite link");
    }

    [Fact]
    public void
        RenewInviteLink_CalledOnUnlinkedLandlordWithExistingLink_CallsFindExistingInviteLinkThenCallsDeleteExistingLinkThenCallsCreateNewInviteLinkAndRedirectsToLandlordProfileWithSuccessFlash()
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
        FlashMessages.Should().Contain("Successfully created a new invite link");
    }
}