using System.Threading.Tasks;
using BricksAndHearts.Controllers;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using Castle.Core.Logging;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BricksAndHearts.UnitTests.ControllerTests.Landlord;

public class LandLordControllerUnassignedTests : LandlordControllerTestsBase
{
    private static AdminController _adminController = A.Fake<AdminController>();

    [Fact]
    public async void AddNewUnassignedLandlord()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        var fakeLandlordService = A.Fake<ILandlordService>();
        var _underTest =
            new LandlordController(A.Fake<ILogger<LandlordController>>(), null!, fakeLandlordService, null!);
        MakeUserPrincipalInController(adminUser, _underTest);

        var formResultModel = A.Fake<LandlordProfileModel>();
        formResultModel.Unassigned = true;
        // Act
        A.CallTo(() => fakeLandlordService.RegisterLandlord(formResultModel))
            .Returns(ILandlordService.LandlordRegistrationResult.Success);
        RedirectToActionResult? result = await _underTest.RegisterPost(formResultModel) as RedirectToActionResult;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        result.Should().NotBeNull();
        result.ActionName.Should().BeEquivalentTo("LandlordList");
        result.ControllerName.Should().BeEquivalentTo("Admin");
    }
}