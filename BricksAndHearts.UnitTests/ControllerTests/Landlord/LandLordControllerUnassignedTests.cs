using BricksAndHearts.Database;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
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
        var returnedLandlord = new LandlordDbModel
        {
            Id = 5
        };
        var formResultModel = new LandlordProfileModel
        {
            Unassigned = true
        };
        A.CallTo(() => LandlordService.RegisterLandlord(formResultModel))
            .Returns((ILandlordService.LandlordRegistrationResult.Success, returnedLandlord));
        A.CallTo(() => MailService.SendMsg(
            A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored
        )).WithAnyArguments().DoesNothing();
        // Act
        var result = await UnderTest.RegisterPost(formResultModel) as RedirectToActionResult;
        // Assert
        A.CallTo(() => MailService.SendMsg(
            A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored
        )).WithAnyArguments().MustHaveHappened();
        A.CallTo(() => LandlordService.RegisterLandlord(formResultModel)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>();
        result.Should().NotBeNull();
        result!.ActionName.Should().BeEquivalentTo("Profile");
        result.ControllerName.Should().BeEquivalentTo("Landlord");
        result.RouteValues!["id"].Should().Be(returnedLandlord.Id);
    }

    [Fact]
    public async void RegisterLandlordWithNoAssignedUser_WhenAttemptedByNonAdmin_ReturnsForbidden()
    {
        // Arrange
        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);
        var returnedLandlord = A.Fake<LandlordDbModel>();
        var formResultModel = A.Fake<LandlordProfileModel>();
        formResultModel.Unassigned = true;

        // Act
        A.CallTo(() => LandlordService.RegisterLandlord(formResultModel))
            .Returns((ILandlordService.LandlordRegistrationResult.Success, returnedLandlord));
        A.CallTo(() => MailService.SendMsg(
            A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored
        )).WithAnyArguments().DoesNothing();
        var result = await UnderTest.RegisterPost(formResultModel) as StatusCodeResult;

        // Assert
        A.CallTo(() => LandlordService.RegisterLandlord(formResultModel)).WithAnyArguments().MustNotHaveHappened();
        A.CallTo(() => MailService.SendMsg(
            A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored
        )).WithAnyArguments().MustNotHaveHappened();
        result.Should().NotBeNull();
        if (result != null)
        {
            result.StatusCode.Should().Be(403);
        }
    }
}