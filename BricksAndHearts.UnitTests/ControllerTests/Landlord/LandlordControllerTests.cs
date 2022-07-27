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
        MakeUserPrincipalInController(unregisteredUser, _underTest);

        // Act
        var result = _underTest.RegisterGet() as ViewResult;

        // Assert
        result!.Model.Should().BeOfType<LandlordProfileModel>()
            .Which.Email.Should().Be(unregisteredUser.GoogleEmail);
    }

    [Fact]
    public async void ApproveCharter_CallsApproveLandlord()
    {
        // Arrange 
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, _underTest);
        var landlord = CreateLandlordUser();

        // Act
        var result = await _underTest.ApproveCharter(landlord.Id) as ViewResult;

        // Assert
        A.CallTo(() => landlordService.ApproveLandlord(landlord.Id, adminUser)).MustHaveHappened();
    }
}