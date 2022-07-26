using System.Threading.Tasks;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
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
        var landlordProfileModel = CreateTestLandlordProfileModel();
        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, _underTest);

        // Act
        A.CallTo(() => landlordService.CheckForDuplicateEmail(landlordProfileModel, 1)).Returns(false);
        A.CallTo(() => landlordService.EditLandlordDetails(landlordProfileModel, 1)).Returns(ILandlordService.LandlordRegistrationResult.Success);
        var result = _underTest.EditProfileUpdate(landlordProfileModel).Result;

        // Assert   
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().BeEquivalentTo("MyProfile");
    }
    
    [Fact]
    public void EditProfileUpdate_CalledUsingLandlordDatabaseModelWithDuplicateEmail_ReturnsEditProfileViewWithLandlordProfile()
    {
        // Arrange 
        var landlordProfileModel = CreateTestLandlordProfileModel();
        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, _underTest);

        // Act
        A.CallTo(() => landlordService.CheckForDuplicateEmail(landlordProfileModel, 1)).Returns(true);
        var result = _underTest.EditProfileUpdate(landlordProfileModel).Result as ViewResult;

        // Assert   
        result!.Model.Should().BeOfType<LandlordProfileModel>();
        result.Should().BeOfType<ViewResult>().Which.ViewName!.Should().BeEquivalentTo("EditProfilePage");
    }
    
    [Fact]
    public void EditProfilePage_CalledUsingUserEmail_ReturnsEditProfileViewWithLandlordProfile()
    {
        // Arrange 
        var landlordProfileModel = CreateTestLandlordProfileModel();

        // Act
        var result = _underTest.EditProfilePage(landlordProfileModel) as ViewResult;

        // Assert   
        result!.Model.Should().BeOfType<LandlordProfileModel>();
    }
}