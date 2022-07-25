using System.Security.Claims;
using System.Threading.Tasks;
using BricksAndHearts.Controllers;
using BricksAndHearts.Services;
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
    public void GetAdminList_ReturnsViewWith_AdminListModel()
    {
        // Arrange
        var fakeAdminService = A.Fake<IAdminService>();
        var fakeAdminController = new AdminController(null!, null!, fakeAdminService);
        
        // Act
        var result = fakeAdminController.GetAdminList().Result as ViewResult;

        // Assert
        result!.Model.Should().BeOfType<AdminListModel>();
    }

    [Fact]
    public void AcceptAdminRequest_Calls_ApproveAdminAccessRequest()
    {
        // Arrange
        var fakeAdminService = A.Fake<IAdminService>();
        var fakeAdminController = new AdminController(null!, null!, fakeAdminService);
        var dummyId = A.Dummy<int>();

        // Act
        var result = fakeAdminController.AcceptAdminRequest(dummyId);
        
        // Assert
        A.CallTo(() => fakeAdminService.ApproveAdminAccessRequest(dummyId)).MustHaveHappened();
    }
}