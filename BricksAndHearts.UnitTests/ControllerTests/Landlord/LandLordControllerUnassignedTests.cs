using BricksAndHearts.Database;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using Castle.Core.Internal;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BricksAndHearts.UnitTests.ControllerTests.Landlord;

public class LandLordControllerUnassignedTests : LandlordControllerTestsBase
{
    [Fact]
    public async void
        RegisterLandlordWithNoAssignedUser_WhenAttemptedByAdmin_CreatesNewUnassignedLandlord_AndRedirectsToTheirProfile()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);

        var formResultModel = A.Fake<LandlordProfileModel>();
        formResultModel.Unassigned = true;
        // Act
        A.CallTo(() => MailService.SendMsg(
            A<string>._, A<string>._, A<string>._, A<string>._
        )).DoesNothing();
        A.CallTo(() => LandlordService.RegisterLandlord(formResultModel))
            .Returns((ILandlordService.LandlordRegistrationResult.Success, A.Fake<LandlordDbModel>()));
        var result = await UnderTest.RegisterPost(formResultModel) as RedirectToActionResult;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        result.Should().NotBeNull();
        if (result != null)
        {
            result.ActionName.Should().BeEquivalentTo("Profile");
            result.ControllerName.Should().BeEquivalentTo("Landlord");
            Assert.False(result.RouteValues.IsNullOrEmpty());
            result.RouteValues?["id"].Should().Be(0);
        }
    }

    [Fact]
    public async void RegisterLandlordWithNoAssignedUser_WhenAttemptedByLandlord_Fails()
    {
        // Arrange
        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        var formResultModel = A.Fake<LandlordProfileModel>();
        formResultModel.Unassigned = true;
        // Act
        A.CallTo(() => LandlordService.RegisterLandlord(formResultModel))
            .Returns((ILandlordService.LandlordRegistrationResult.Success, A.Fake<LandlordDbModel>()));
        var result = await UnderTest.RegisterPost(formResultModel) as RedirectToActionResult;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        result.Should().NotBeNull();
        if (result != null)
        {
            result.ActionName.Should().BeEquivalentTo("Profile");
            result.ControllerName.Should().BeEquivalentTo("Landlord");
            Assert.False(result.RouteValues.IsNullOrEmpty());
            result.RouteValues?["id"].Should().Be(0);
        }
    }
}