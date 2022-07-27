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
        result!.ActionName.Should().BeEquivalentTo("MyProfile");
    }

    [Fact]
    public async void ApproveCharter_CallsApproveLandlord_AndDisplaysSuccessMessage()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);
        var landlord = CreateLandlordUser();

        // Act
        var result = await UnderTest.ApproveCharter(landlord.Id) as ViewResult;

        // Assert
        A.CallTo(() => LandlordService.ApproveLandlord(landlord.Id, adminUser)).MustHaveHappened();
        UnderTest.TempData["ApprovalSuccessMessage"].Should().Be("");
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

        // Act
        A.CallTo(() => LandlordService.GetLandlordIfExistsFromId(A<int>._)).Returns(fakeNullLandlord);
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

        // Act
        A.CallTo(() => LandlordService.CheckForDuplicateEmail(landlordProfileModel)).Returns(false);
        A.CallTo(() => LandlordService.EditLandlordDetails(landlordProfileModel))
            .Returns(ILandlordService.LandlordRegistrationResult.Success);
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

        // Act
        A.CallTo(() => LandlordService.CheckForDuplicateEmail(landlordProfileModel)).Returns(true);
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
    public void EditProfileUpdate_CalledUsingInvalidModel_Returns404Error()
    {
        // Arrange
        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);
        UnderTest.ModelState.AddModelError("Invalid", "this is a pretend error");
        var invalidLandlordModel = CreateInvalidLandlordProfileModel();

        // Act
        var result = UnderTest.EditProfileUpdate(invalidLandlordModel).Result;

        // Assert   
        result.Should().BeOfType<StatusCodeResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public void ViewProperties_CalledByAdmin_CanReturnOtherUsersPropertyList()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        var fakeLandlordService = A.Fake<ILandlordService>();
        var fakePropertyService = A.Fake<IPropertyService>();
        var _underTest =
            new LandlordController(A.Fake<ILogger<LandlordController>>(), null!, fakeLandlordService,
                fakePropertyService, null!);
        MakeUserPrincipalInController(adminUser, _underTest);

        // Act
        var results = new List<ViewResult?>();
        A.CallTo(() => fakePropertyService.GetPropertiesByLandlord(1))
            .Returns(A.CollectionOfFake<PropertyDbModel>(10).ToList());
        results.Add(_underTest.ViewProperties(1) as ViewResult);
        A.CallTo(() => fakePropertyService.GetPropertiesByLandlord(2))
            .Returns(A.CollectionOfFake<PropertyDbModel>(10).ToList());
        results.Add(_underTest.ViewProperties(2) as ViewResult);

        // Assert
        foreach (var result in results)
        {
            result.Should().BeOfType<ViewResult>();
            result.Should().NotBeNull();
            result!.Model.Should().BeOfType<PropertiesDashboardViewModel>();
            result.Model.As<PropertiesDashboardViewModel>().Properties.Count.Should()
                .Be(10); //Properties.Count.Should().Be(10);
        }
    }

    [Fact]
    public void ViewProperties_CalledByNonAdmin_CannotReturnOtherUsersPropertyList()
    {
        // Arrange
        var unregisteredUser = CreateUnregisteredUser();
        var fakeLandlordService = A.Fake<ILandlordService>();
        var fakePropertyService = A.Fake<IPropertyService>();
        var underTest =
            new LandlordController(A.Fake<ILogger<LandlordController>>(), null!, fakeLandlordService, null!, null!);
        MakeUserPrincipalInController(unregisteredUser, underTest);

        // Act
        var results = new List<StatusCodeResult?>();
        A.CallTo(() => fakePropertyService.GetPropertiesByLandlord(1))
            .Returns(A.CollectionOfFake<PropertyDbModel>(10).ToList());
        results.Add(underTest.ViewProperties(1) as StatusCodeResult);
        A.CallTo(() => fakePropertyService.GetPropertiesByLandlord(2))
            .Returns(A.CollectionOfFake<PropertyDbModel>(10).ToList());
        results.Add(underTest.ViewProperties(2) as StatusCodeResult);

        // Assert
        foreach (var result in results)
        {
            result.Should().BeOfType<StatusCodeResult>();
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(403);
        }
    }
}