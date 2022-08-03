using System.Security.Claims;
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
        var result = UnderTest.Index() as ViewResult;

        // Assert
        result!.ViewData.Model.Should().BeOfType<AdminViewModel>()
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


    /*public void PropertyList_ReturnsViewWith_PropertiesDashboardViewModel()
    {
        // Act
        var result = UnderTest.PropertyList().Result as ViewResult;

        // Assert
        result!.Model.Should().BeOfType<PropertiesDashboardViewModel>();
    }*/

    [Fact]
    public void ViewLandlord_ReturnsViewWith_LandlordProfileModel()
    {
        // Arrange
        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);
        var dummyId = 1;

        // Act
        var result = UnderTest.ViewLandlord(dummyId).Result as ViewResult;

        // Assert
        result!.Model.Should().BeOfType<LandlordProfileModel>();
    }

    /*
    [Fact]
    public void SortProperties_ReturnsViewWith_PropertiesDashboardViewModel()
    {
        // Arrange
        var dummyString = A.Dummy<string>();

        // Act
        var result = UnderTest.SortProperties(dummyString) as ViewResult;
    }*/
}
