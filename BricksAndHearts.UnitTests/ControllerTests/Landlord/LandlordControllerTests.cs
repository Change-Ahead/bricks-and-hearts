using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BricksAndHearts.Controllers;
using BricksAndHearts.Database;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BricksAndHearts.UnitTests.ControllerTests.Landlord;

public class LandlordControllerTests : LandlordControllerTestsBase
{
    [Fact]
    public void RegisterGet_CalledByUnregisteredUser_ReturnsRegisterViewWithEmail()
    {
        // Arrange
        var unregisteredUser = CreateUnregisteredUser();
        MakeUserPrincipalInController(unregisteredUser, UnderTest);

        // Act
        var result = UnderTest.RegisterGet() as ViewResult;

        // Assert
        result!.Model.Should().BeOfType<LandlordProfileModel>()
            .Which.Email.Should().Be(unregisteredUser.GoogleEmail);
    }

    [Fact]
    public async void RegisterPost_CalledByUnregisteredUser_ReturnsProfile()
    {
        // Arrange
        var unregisteredUser = CreateUnregisteredUser();
        MakeUserPrincipalInController(unregisteredUser, UnderTest);
        var formResultModel = new LandlordProfileModel();
        var returnedLandlord = new LandlordDbModel();

        A.CallTo(() => LandlordService.RegisterLandlord(formResultModel, unregisteredUser))
            .Returns((ILandlordService.LandlordRegistrationResult.Success, returnedLandlord));

        // Act
        var result = await UnderTest.RegisterPost(formResultModel) as RedirectToActionResult;

        // Assert
        A.CallTo(() => LandlordService.RegisterLandlord(formResultModel, unregisteredUser)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>();
        result.Should().NotBeNull();
        result!.ActionName.Should().BeEquivalentTo("Profile");
    }

    [Fact]
    public async void ApproveCharter_CallsApproveLandlord_AndDisplaysSuccessMessage()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);
        var landlordUser = CreateLandlordUser();
        var landlordProfile = new LandlordProfileModel { LandlordId = landlordUser.Id, MembershipId = "abc" };
        A.CallTo(() => LandlordService.ApproveLandlord(landlordUser.Id, adminUser, "abc"))
            .Returns(ILandlordService.ApproveLandlordResult.Success);

        // Act
        await UnderTest.ApproveCharter(landlordProfile);

        // Assert
        A.CallTo(() => LandlordService.ApproveLandlord(landlordUser.Id, adminUser, "abc")).MustHaveHappened();
        FlashMessages.Should().Contain("Successfully approved landlord charter.");
    }

    [Fact]
    public async void ApproveCharter_WithoutMembershipId_DisplaysErrorMessage()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);
        var landlordUser = CreateLandlordUser();
        var landlordProfile = new LandlordProfileModel { LandlordId = landlordUser.Id };

        // Act
        await UnderTest.ApproveCharter(landlordProfile);

        // Assert
        A.CallTo(() => LandlordService.ApproveLandlord(landlordUser.Id, adminUser, null!)).MustNotHaveHappened();
        FlashMessages.Should().Contain("Membership ID is required.");
    }

    [Fact]
    public void EditProfilePage_CalledUsingUserId_ReturnsEditProfileViewWithLandlordProfile()
    {
        // Arrange 
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);
        var landlordProfileModel = CreateTestLandlordProfileModel();

        // Act
        var result = UnderTest.EditProfilePage(landlordProfileModel.LandlordId).Result as ViewResult;

        // Assert   
        result!.Model.Should().BeOfType<LandlordProfileModel>();
    }

    [Fact]
    public void EditProfilePage_CalledUsingInvalidId_Returns404Error()
    {
        // Arrange 
        var adminUser = CreateAdminUser();
        LandlordDbModel? fakeNullLandlord = null;
        MakeUserPrincipalInController(adminUser, UnderTest);
        A.CallTo(() => LandlordService.GetLandlordIfExistsFromId(A<int>._)).Returns(fakeNullLandlord);

        // Act
        var result = UnderTest.EditProfilePage(1000).Result;

        // Assert   
        result.Should().BeOfType<StatusCodeResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public void EditProfilePage_CalledUsingNonAdmin_Returns403Error()
    {
        // Arrange 
        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        // Act
        var result = UnderTest.EditProfilePage(1000).Result;

        // Assert   
        result.Should().BeOfType<StatusCodeResult>().Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public void EditProfileUpdate_CalledUsingLandlordDatabaseModel_ReturnsProfileViewWithLandlordProfile()
    {
        // Arrange 
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);
        var landlordProfileModel = CreateTestLandlordProfileModel();
        A.CallTo(() => LandlordService.CheckForDuplicateEmail(landlordProfileModel)).Returns(false);
        A.CallTo(() => LandlordService.CheckForDuplicateMembershipId(landlordProfileModel)).Returns(false);
        A.CallTo(() => LandlordService.EditLandlordDetails(landlordProfileModel))
            .Returns(ILandlordService.LandlordRegistrationResult.Success);

        // Act
        var result = UnderTest.EditProfileUpdate(landlordProfileModel).Result;

        // Assert   
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().BeEquivalentTo("Profile");
    }

    [Fact]
    public void
        EditProfileUpdate_CalledUsingLandlordDatabaseModelWithDuplicateEmail_ReturnsEditProfileViewWithLandlordProfile()
    {
        // Arrange 
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);
        var landlordProfileModel = CreateTestLandlordProfileModel();
        A.CallTo(() => LandlordService.CheckForDuplicateEmail(landlordProfileModel)).Returns(true);
        A.CallTo(() => LandlordService.CheckForDuplicateMembershipId(landlordProfileModel)).Returns(false);

        // Act
        var result = UnderTest.EditProfileUpdate(landlordProfileModel).Result as ViewResult;

        // Assert   
        result!.Model.Should().BeOfType<LandlordProfileModel>();
        result.Should().BeOfType<ViewResult>().Which.ViewName!.Should().BeEquivalentTo("EditProfilePage");
    }

    [Fact]
    public void
        EditProfileUpdate_CalledUsingLandlordDatabaseModelWithDuplicateMembershipId_ReturnsEditProfileViewWithLandlordProfile()
    {
        // Arrange 
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);
        var landlordProfileModel = CreateTestLandlordProfileModel();
        A.CallTo(() => LandlordService.CheckForDuplicateEmail(landlordProfileModel)).Returns(false);
        A.CallTo(() => LandlordService.CheckForDuplicateMembershipId(landlordProfileModel)).Returns(true);

        // Act
        var result = UnderTest.EditProfileUpdate(landlordProfileModel).Result as ViewResult;

        // Assert   
        result!.Model.Should().BeOfType<LandlordProfileModel>();
        result.Should().BeOfType<ViewResult>().Which.ViewName!.Should().BeEquivalentTo("EditProfilePage");
    }

    [Fact]
    public void EditProfileUpdate_CalledUsingNonAdmin_Returns403Error()
    {
        // Arrange
        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);
        var invalidLandlordModel = CreateInvalidLandlordProfileModel();

        // Act
        var result = UnderTest.EditProfileUpdate(invalidLandlordModel).Result;

        // Assert   
        result.Should().BeOfType<StatusCodeResult>().Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public void EditProfileUpdate_CalledUsingInvalidModel_ReturnsEditPageView()
    {
        // Arrange
        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);
        UnderTest.ModelState.AddModelError("Invalid", "this is a pretend error");
        var invalidLandlordModel = CreateInvalidLandlordProfileModel();

        // Act
        var result = UnderTest.EditProfileUpdate(invalidLandlordModel).Result as ViewResult;

        // Assert
        result!.Model.Should().BeOfType<LandlordProfileModel>();
        result.Should().BeOfType<ViewResult>().Which.ViewName.Should().BeEquivalentTo("EditProfilePage");
        UnderTest.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ViewProperties_CalledByAdmin_CanReturnOtherUsersPropertyList()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        adminUser.Id = 1;
        MakeUserPrincipalInController(adminUser, UnderTest);
        A.CallTo(() => PropertyService.GetPropertiesByLandlord(2))
            .Returns(A.CollectionOfFake<PropertyDbModel>(10).ToList());

        // Act
        var result = await UnderTest.ViewProperties(2) as ViewResult;

        // Assert
        result.Should().BeOfType<ViewResult>();
        result.Should().NotBeNull();
        result!.Model.Should().BeOfType<PropertiesDashboardViewModel>();
        result.Model.As<PropertiesDashboardViewModel>().Properties.Count.Should()
            .Be(10);
    }

    [Fact]
    public async Task ViewProperties_CalledByNonAdmin_CannotReturnOtherUsersPropertyList()
    {
        // Arrange
        var unregisteredUser = CreateUnregisteredUser();
        unregisteredUser.Id = 1;
        var fakeLandlordService = A.Fake<ILandlordService>();
        var fakePropertyService = A.Fake<IPropertyService>();
        var underTest =
            new LandlordController(A.Fake<ILogger<LandlordController>>(), fakeLandlordService, null!, null!);
        MakeUserPrincipalInController(unregisteredUser, underTest);
        A.CallTo(() => fakePropertyService.GetPropertiesByLandlord(2))
            .Returns(A.CollectionOfFake<PropertyDbModel>(10).ToList());

        // Act
        var result = await underTest.ViewProperties(2) as StatusCodeResult;
        result.Should().BeOfType<StatusCodeResult>();
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(403);
    }

    [Fact]
    public async void TieUserWithLandlord_WithNonExistentLink_RedirectToInvite()
    {
        // Arrange 
        var landlordUser = CreateLandlordUser();
        const string inviteLink = "11111";
        A.CallTo(() => LandlordService.LinkExistingLandlordWithUser(inviteLink, landlordUser))
            .Returns(ILandlordService.LinkUserWithLandlordResult.ErrorLinkDoesNotExist);
        MakeUserPrincipalInController(landlordUser, UnderTest);

        // Act
        var result = await UnderTest.TieUserWithLandlord(inviteLink);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Invite");
        result.Should().BeOfType<RedirectToActionResult>().Which.RouteValues.Should()
            .Contain("inviteLink", inviteLink);
        UnderTest.TempData["FlashMessages"].Should().BeNull();
    }

    [Fact]
    public async void TieUserWithLandlord_WithLandlord_RedirectToProfile()
    {
        // Arrange 
        var landlordUser = CreateLandlordUser();
        const string inviteLink = "11111";

        A.CallTo(() => LandlordService.LinkExistingLandlordWithUser(inviteLink, landlordUser))
            .Returns(ILandlordService.LinkUserWithLandlordResult.ErrorUserAlreadyHasLandlordRecord);
        MakeUserPrincipalInController(landlordUser, UnderTest);

        // Act
        var result = await UnderTest.TieUserWithLandlord(inviteLink);

        // Assert   
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("MyProfile");
        FlashMessages.Should()
            .Contain($"User already registered with landlord (landlordId = {landlordUser.LandlordId})");
    }

    [Fact]
    public async void TieUserWithLandlord_WithNonLandlordUser_RedirectToProfile()
    {
        // Arrange 
        var nonLandlordUser = CreateUnregisteredUser();
        const string inviteLink = "11111";

        A.CallTo(() => LandlordService.LinkExistingLandlordWithUser(inviteLink, nonLandlordUser))
            .Returns(ILandlordService.LinkUserWithLandlordResult.Success);
        MakeUserPrincipalInController(nonLandlordUser, UnderTest);

        // Act
        var result = await UnderTest.TieUserWithLandlord(inviteLink);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("MyProfile");
        FlashMessages.Should()
            .Contain($"User {nonLandlordUser.Id} successfully linked with landlord (landlordId = {nonLandlordUser.LandlordId})");
    }
}