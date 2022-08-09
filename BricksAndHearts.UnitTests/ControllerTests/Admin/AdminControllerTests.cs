using System.Collections.Generic;
using BricksAndHearts.Database;
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
    public async void TenantList_WhenCalled_CallsGetTenantListAndReturnsTenantListView()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);

        // Act
        var result = await UnderTest.TenantList() as ViewResult;

        // Assert
        result!.ViewData.Model.Should().BeOfType<TenantListModel?>();
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
    
        
    [Fact]
    public void ReceiveTenantFilterList_Always_RedirectsToGetFilteredTenants()
    {
        // Arrange
        MakeUserPrincipalInController(CreateAdminUser(), UnderTest);
        var fakeTenantListModel = A.Fake<TenantListModel>();
        
        // Act
        var result = UnderTest.ReceiveTenantFilterList(fakeTenantListModel);
            
        // Assert
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("GetFilteredTenants");
    }
    
    [Fact]
    public void GetFilteredTenants_WithCorrectInput_CallsGetTenantDbModelsFromFilter()
    {
        // Arrange
        MakeUserPrincipalInController(CreateAdminUser(), UnderTest);
        var fakeTenantListModel = A.Fake<TenantListModel>();

        // Act
        A.CallTo(() => AdminService.GetTenantDbModelsFromFilter(fakeTenantListModel.Filters)).Returns(A.Fake<List<TenantDbModel>>());
        var result = UnderTest.GetFilteredTenants(fakeTenantListModel).Result;
        
        // Assert
        A.CallTo(() => AdminService.GetTenantDbModelsFromFilter(fakeTenantListModel.Filters)).MustHaveHappened();
        result.Should().BeOfType<ViewResult>().Which.Model.Should().Be(fakeTenantListModel);
    }
}
