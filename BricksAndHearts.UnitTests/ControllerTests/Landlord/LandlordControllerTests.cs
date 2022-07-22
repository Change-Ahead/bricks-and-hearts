using BricksAndHearts.Controllers;
using BricksAndHearts.Services;
using BricksAndHearts.Database;
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
    public async void ApproveCharter_CallsApproveLandlord()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);
        var landlord = CreateLandlordUser();

        // Act
        var result = await UnderTest.ApproveCharter(landlord.Id) as ViewResult;

        // Assert
        A.CallTo(() => LandlordService.ApproveLandlord(landlord.Id, adminUser)).MustHaveHappened();
    }
    
    [Fact]
    public void EditProfileUpdate_CalledUsingLandlordDatabaseModel_ReturnsProfileViewWithLandlordProfile()
    {
        // Arrange 
        var landlordDbModel = CreateTestLandlordDbModel();
        var fakeLandlordService = A.Fake<ILandlordService>();
        var fakeLandlordController = new LandlordController(null!, null!, fakeLandlordService, null!);

        // Act
        A.CallTo(() => fakeLandlordService.UpdateEditedLandlord(landlordDbModel)).Returns(landlordDbModel);
        var result = fakeLandlordController.EditProfileUpdate(landlordDbModel) as ViewResult;

        // Assert   
        A.CallTo(() => fakeLandlordService.UpdateEditedLandlord(landlordDbModel)).MustHaveHappened();
        result!.Model.Should().BeOfType<LandlordProfileModel>();
    }
    
    [Fact]
    public void EditProfileUpdate_CalledUsingLandlordDatabaseModelWithDuplicateEmail_ReturnsEditProfileViewWithLandlordProfile()
    {
        // Arrange 
        var landlordDbModel = CreateTestLandlordDbModel();
        var fakeLandlordService = A.Fake<ILandlordService>();
        var fakeLogger = A.Fake<Logger<LandlordController>>();
        var fakeLandlordController = new LandlordController(fakeLogger, null!, fakeLandlordService, null!);

        // Act
        A.CallTo(() => fakeLandlordService.CheckForDuplicateEmail(landlordDbModel)).Returns(true);
        var result = fakeLandlordController.EditProfileUpdate(landlordDbModel) as ViewResult;

        // Assert   
        result!.Model.Should().BeOfType<LandlordDbModel>();
    }
    
    [Fact]
    public void EditProfilePage_CalledUsingUserEmail_ReturnsEditProfileViewWithLandlordProfile()
    {
        // Arrange 
        var landlordDbModel = CreateTestLandlordDbModel();
        var fakeLandlordService = A.Fake<ILandlordService>();
        var dummyEmail = A.Dummy<string>();
        var fakeLandlordController = new LandlordController(null!, null!, fakeLandlordService, null!);

        // Act
        A.CallTo(() => fakeLandlordService.GetLandlordFromEmail(dummyEmail)).Returns(landlordDbModel);
        var result = fakeLandlordController.EditProfilePage(dummyEmail) as ViewResult;

        // Assert   
        result!.Model.Should().BeOfType<LandlordDbModel>();
    }
}