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
    public void Index_WhenCalledByAnonymousUser_ReturnsViewWithLoginLink()
    {
        // Arrange
        UnderTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _anonUser }
        };

        // Act
        var result = UnderTest.AdminDashboard() as ViewResult;

        // Assert
        result!.ViewData.Model.Should().BeOfType<AdminDashboardViewModel>()
            .Which.CurrentUser.Should().BeNull();
    }

    [Fact]
    public async void LandlordList_WhenCalled_CallsGetLandlordListAndReturnsLandlordListView()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);

        // Act
        var result = await UnderTest.LandlordList() as ViewResult;

        // Assert
        A.CallTo(() => AdminService.GetLandlordDisplayList("")).MustHaveHappened();
        result!.ViewData.Model.Should().BeOfType<LandlordListModel?>();
    }

    [Fact]
    public void GetAdminList_ReturnsViewWithAdminListModel()
    {
        // Arrange
        MakeUserPrincipalInController(CreateAdminUser(), UnderTest);

        // Act
        var result = UnderTest.GetAdminList().Result as ViewResult;

        // Assert
        result!.ViewData.Model.Should().BeOfType<AdminListModel>();
    }
}
