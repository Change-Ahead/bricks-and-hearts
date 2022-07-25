using BricksAndHearts.Controllers;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BricksAndHearts.UnitTests.ControllerTests.Property;

public class PropertyControllerTests : PropertyControllerTestsBase
{
    [Fact]
    public void AddNewPropertyPost_CalledByUnregisteredUser_Returns403()
    {
        // Arrange 
        CreateUnregisteredUserInController(_underTest);
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
        CreateRegisteredUserInController(_underTest);
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
        var propertyService = A.Fake<IPropertyService>();

        var controller = new PropertyController(null!, null!, new PropertyService(null!), null!);
        CreateRegisteredUserInController(controller);

        var formResultModel = A.Fake<PropertyViewModel>();

        // Act
        var result = controller.AddNewProperty(formResultModel);

        // Assert
        A.CallTo(() => propertyService.AddNewProperty(formResultModel, 1)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("ViewProperties");
    }
}