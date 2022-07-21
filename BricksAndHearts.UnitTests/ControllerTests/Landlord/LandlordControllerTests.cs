using BricksAndHearts.Controllers;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BricksAndHearts.UnitTests.ControllerTests.Landlord;

public class LandlordControllerTests : LandlordControllerTestsBase
{
    [Fact]
    public void RegisterGet_CalledByUnregisteredUser_ReturnsRegisterViewWithEmail()
    {
        // Arrange 
        var unregisteredUser = CreateUnregisteredUser();
        MakeUserPrincipalInController(unregisteredUser, _underTest);

        // Act
        var result = _underTest.RegisterGet() as ViewResult;

        // Assert
        result!.Model.Should().BeOfType<LandlordProfileModel>()
            .Which.Email.Should().Be(unregisteredUser.GoogleEmail);
    }

    /*[Fact]
    public void AddNewPropertyPost_CalledByUnregisteredUser_Returns403()
    {
        // Arrange 
        var unregisteredUser = CreateUnregisteredUser();
        MakeUserPrincipalInController(unregisteredUser, _underTest);
        var formResultModel = A.Fake<PropertyViewModel>();

        // Act
        var result = _underTest.AddNewProperty(formResultModel) as StatusCodeResult;

        // Assert
        result!.StatusCode.Should().Be(403);
    }

    [Fact]
    public void AddNewPropertyPost_WithInvalidModel_ReturnsViewWithModel()
    {
        // Arrange 
        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, _underTest);
        var formResultModel = CreateExamplePropertyViewModel();
        _underTest.ViewData.ModelState.AddModelError("Key", "ErrorMessage");

        // Act
        var result = _underTest.AddNewProperty(formResultModel) as ViewResult;

        // Assert
        result!.ViewData.Model.Should().BeOfType<PropertyViewModel>().And.Be(formResultModel);
    }

    [Fact]
    public void AddNewPropertyPost_WithValidModel_CallsPropertyServiceMethod_AndRedirects()
    {
        // Arrange
        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, _underTest);

        var formResultModel = A.Fake<PropertyViewModel>();

        // Act
        var result = _underTest.AddNewProperty(formResultModel);

        // Assert
        A.CallTo(() => propertyService.AddNewProperty(formResultModel, 1)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("ViewProperties");
    }*/
}