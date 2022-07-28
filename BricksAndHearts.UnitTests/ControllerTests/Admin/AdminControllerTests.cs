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
        MakeUserPrincipalInController(CreateAdminUser(), _underTest);
        
        // Act
        var result = _underTest.GetAdminList().Result as ViewResult;

        // Assert
        result!.ViewData.Model.Should().BeOfType<AdminListModel>();
    }

    [Fact]
    public void AcceptAdminRequest_WhenCalled_ApprovesAdminAccessRequest()
    {
        // Arrange
        MakeUserPrincipalInController(CreateAdminUser(), _underTest);
        var dummyId = 1;

        // Act
        var result = _underTest.AcceptAdminRequest(dummyId);
        
        // Assert
        A.CallTo(() => adminService.ApproveAdminAccessRequest(dummyId)).MustHaveHappened();
    }
}