using System.Security.Claims;
using System.Threading.Tasks;
using BricksAndHearts.ViewModels;
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
    public async Task GetAdminList_WhenCalledByAdmin_ReturnsViewWithAdminList()
    {
        // Arrange 
        var adminUser = CreateAdminUserInController(_underTest);
        _underTest.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(adminUser) }
        };

        // Act
        var result = await _underTest.GetAdminList() as ViewResult;

        // Assert
        result!.ViewData.Model.Should().BeOfType<AdminListModel>() ;
    }
}