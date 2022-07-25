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
        _underTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _anonUser }
        };

        // Act
        var result = _underTest.Index() as ViewResult;

        // Assert
        result!.ViewData.Model.Should().BeOfType<AdminViewModel>()
            .Which.CurrentUser.Should().BeNull();
    }

    [Fact]
    public async void LandlordList_WhenCalled_CallsGetUnapprovedLandlordsAndReturnsLandlordListView()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, _underTest);

        // Act
        var result = await _underTest.LandlordList() as ViewResult;

        // Assert
        A.CallTo(() => adminService.GetUnapprovedLandlords()).MustHaveHappened();
        result!.ViewData.Model.Should().BeOfType<LandlordListModel?>();
    }
}