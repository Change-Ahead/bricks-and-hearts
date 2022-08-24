using BricksAndHearts.ViewModels;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BricksAndHearts.UnitTests.ControllerTests.HomeControllerTests;

public class HomeControllerTests : HomeControllerTestsBase
{
    [Fact]
    public void Index_WhenCalledByAnonymousUser_ReturnsIndexViewWithLoginLink()
    {
        // Arrange
        UnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = AnonUser }
        };

        // Act
        var result = UnderTest.Index() as ViewResult;

        // Assert
        result!.ViewData.Model.Should().BeOfType<HomeViewModel>()
            .Which.IsLoggedIn.Should().Be(false);
    }

    [Fact]
    public void Index_WhenCalledByNonAdminNonLandlordUser_ReturnsIndexViewWithRegisterButtons()
    {
        // Arrange
        var nonAdminNonLandlordUser = CreateNonAdminNonLandlordUser();
        MakeUserPrincipalInController(nonAdminNonLandlordUser, UnderTest);

        // Act
        var result = UnderTest.Index() as ViewResult;

        // Assert
        result!.ViewData.Model.Should().BeOfType<HomeViewModel>()
            .Which.IsLoggedIn.Should().Be(true);
    }

    [Fact]
    public void Index_WhenCalledByLandlordUser_RedirectsToLandlordDashboard()
    {
        // Arrange
        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        // Act
        var result = UnderTest.Index() as RedirectToActionResult;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        result.Should().NotBeNull();
        result!.ActionName.Should().BeEquivalentTo("Dashboard");
    }

    [Fact]
    public void Index_WhenCalledByAdminUser_RedirectsToAmindDashboard()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);

        // Act
        var result = UnderTest.Index() as RedirectToActionResult;

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
        result.Should().NotBeNull();
        result!.ActionName.Should().BeEquivalentTo("AdminDashboard");
    }
}