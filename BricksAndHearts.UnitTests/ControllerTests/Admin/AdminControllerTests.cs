using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BricksAndHearts.Controllers;
using BricksAndHearts.Database;
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
    public void GetAdminList_ReturnsViewWithAdminListModel()
    {
        // Arrange
        CreateAdminUserInController(_underTest);
        
        // Act
        var result = _underTest.GetAdminList().Result as ViewResult;

        // Assert
        result!.ViewData.Model.Should().BeOfType<AdminListModel>();
    }

    [Fact]
    public void AcceptAdminRequest_WhenCalledWithAdminPermissions_ApproveAdminAccessRequest()
    {
        // Arrange
        CreateAdminUserInController(_underTest);
        var dummyId = 1;

        // Act
        var result = _underTest.AcceptAdminRequest(dummyId);
        
        // Assert
        A.CallTo(() => _underTestService.ApproveAdminAccessRequest(dummyId)).MustHaveHappened();
    }
    
    [Fact]
    public void RejectAdminRequest_WhenCalledWithAdminPermissions_RejectAdminAccessRequest()
    {
        // Arrange
        CreateAdminUserInController(_underTest);
        var dummyId = 1;

        // Act
        var result = _underTest.RejectAdminRequest(dummyId);
        
        // Assert
        A.CallTo(() => _underTestService.RejectAdminAccessRequest(dummyId)).MustHaveHappened();
    }
    
    
}