using System.Threading.Tasks;
using BricksAndHearts.Controllers;
using BricksAndHearts.Database;
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
    public void AddNewPropertyContinueGet_CalledByUnregisteredUser_Returns403()
    {
        // Arrange 
        CreateUnregisteredUserInController(_underTest);

        // Act
        var result = _underTest.AddNewProperty_Continue(1) as StatusCodeResult;

        // Assert
        result!.StatusCode.Should().Be(403);
    }

    [Fact]
    public void AddNewPropertyBeginGet_ReturnsView_AtStep1()
    {
        // Arrange
        CreateRegisteredUserInController(_underTest);

        // Act
        var result = _underTest.AddNewProperty_Begin() as ViewResult;

        // Assert
        result!.ViewName.Should().Be("AddNewProperty");
        result.Model.Should().BeOfType<AddNewPropertyViewModel>().Which.Step.Should().Be(1);
    }

    [Fact]
    public void AddNewPropertyContinueGet_ReturnsViewAtStep()
    {
        // Arrange
        CreateRegisteredUserInController(_underTest);

        // Act
        var result = _underTest.AddNewProperty_Continue(2) as ViewResult;

        // Assert
        result!.ViewName.Should().Be("AddNewProperty");
        result.Model.Should().BeOfType<AddNewPropertyViewModel>().Which.Step.Should().Be(2);
    }

    [Fact]
    public void AddNewPropertyContinueGet_WithAddInProgress_ReturnsPartiallyCompleteModel()
    {
        // Arrange
        var fakePropertyDbModel = A.Fake<PropertyDbModel>();
        fakePropertyDbModel.Id = 1;
        fakePropertyDbModel.Description = "hello";

        var propertyService = A.Fake<IPropertyService>();
        var apiService = A.Fake<IApiService>();
        A.CallTo(() => propertyService.GetIncompleteProperty(1)).Returns(fakePropertyDbModel);

        var controller = new PropertyController(propertyService, apiService,null!);
        CreateRegisteredUserInController(controller);

        // Act
        var result = controller.AddNewProperty_Continue(2) as ViewResult;

        // Assert
        result!.ViewName.Should().Be("AddNewProperty");
        A.CallTo(() => propertyService.GetIncompleteProperty(1)).MustHaveHappened();
        result.Model.Should().BeOfType<AddNewPropertyViewModel>().Which.Step.Should().Be(2);
        result.Model.Should().BeOfType<AddNewPropertyViewModel>().Which.Property!.Description.Should().Be("hello");
    }

    [Fact]
    public void AddNewPropertyContinueGet_WithNoAddInProgress_ReturnsBlankModel()
    {
        // Arrange
        var propertyService = A.Fake<IPropertyService>();
        A.CallTo(() => propertyService.GetIncompleteProperty(1)).Returns(null);
        var apiService = A.Fake<IApiService>();

        var controller = new PropertyController(propertyService, apiService, null!);
        CreateRegisteredUserInController(controller);

        // Act
        var result = controller.AddNewProperty_Continue(2) as ViewResult;

        // Assert
        result!.ViewName.Should().Be("AddNewProperty");
        A.CallTo(() => propertyService.GetIncompleteProperty(1)).MustHaveHappened();
        result.Model.Should().BeOfType<AddNewPropertyViewModel>().Which.Step.Should().Be(2);
        result.Model.Should().BeOfType<AddNewPropertyViewModel>().Which.Property!.Description.Should().BeNull();
    }

    [Fact]
    public async void AddNewPropertyContinuePost_WithInvalidModel_ReturnsViewWithModel()
    {
        // Arrange 
        var propertyService = A.Fake<IPropertyService>();

        CreateRegisteredUserInController(_underTest);

        var formResultModel = CreateExamplePropertyViewModel();
        _underTest.ViewData.ModelState.AddModelError("Key", "ErrorMessage");

        // Act
        var result = await _underTest.AddNewProperty_Continue(2, formResultModel) as ViewResult;

        // Assert
        A.CallTo(() => propertyService.UpdateProperty(1, formResultModel, true)).MustNotHaveHappened();
        A.CallTo(() => propertyService.AddNewProperty(1, formResultModel, true)).MustNotHaveHappened();
        result!.ViewData.Model.Should().BeOfType<AddNewPropertyViewModel>().Which.Property.Should()
            .BeOfType<PropertyViewModel>().And.Be(formResultModel);
    }

    [Fact]
    public void AddNewPropertyContinuePost_AtStep1_WithoutAddress1AndPostcode_ReturnsViewWithModel()
    {
        // Arrange
        var propertyService = A.Fake<IPropertyService>();
        A.CallTo(() => propertyService.GetIncompleteProperty(1)).Returns(null);

        var controller = new PropertyController(propertyService, null!);
        CreateRegisteredUserInController(controller);

        var formResultModel = new PropertyViewModel
        {
            Address = new PropertyAddress()
        };

        // Act
        var result = controller.AddNewProperty_Continue(1, formResultModel) as ViewResult;

        // Assert
        A.CallTo(() => propertyService.AddNewProperty(1, formResultModel, true)).MustNotHaveHappened();
        A.CallTo(() => propertyService.UpdateProperty(1, formResultModel, true)).MustNotHaveHappened();
        result!.ViewData.Model.Should().BeOfType<AddNewPropertyViewModel>().Which.Property.Should()
            .BeOfType<PropertyViewModel>().And.Be(formResultModel);
    }

    [Fact]
    public async void AddNewPropertyContinuePost_AtStep1_CreatesNewRecord_AndRedirectsToNextStep()
    {
        // Arrange
        var propertyService = A.Fake<IPropertyService>();
        A.CallTo(() => propertyService.GetIncompleteProperty(1)).Returns(null);
        var apiService = A.Fake<IApiService>();

        var controller = new PropertyController(propertyService, apiService,null!);
        CreateRegisteredUserInController(controller);

        var formResultModel = new PropertyViewModel
        {
            Address = new PropertyAddress
            {
                AddressLine1 = "Line 1",
                Postcode = "Postcode"
            }
        };
        A.CallTo(() => apiService.AutofillAddress(formResultModel))
            .Returns(Task.Run(() => formResultModel));

        // Act
        var result = await controller.AddNewProperty_Continue(1, formResultModel);

        // Assert
        A.CallTo(() => propertyService.AddNewProperty(1, formResultModel, true)).MustHaveHappened();
        A.CallTo(() => propertyService.UpdateProperty(1, formResultModel, true)).MustNotHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("AddNewProperty_Continue");
        result.Should().BeOfType<RedirectToActionResult>().Which.RouteValues.Should().ContainKey("step").WhoseValue
            .Should().Be(2);
    }

    [Fact]
    public async void AddNewPropertyContinuePost_AtMiddleSteps_WithNoAddInProgress_RedirectsToViewProperties()
    {
        // Arrange
        var propertyService = A.Fake<IPropertyService>();
        var apiService = A.Fake<IApiService>();
        A.CallTo(() => propertyService.GetIncompleteProperty(1)).Returns(null);

        var controller = new PropertyController(propertyService, apiService,null!);
        CreateRegisteredUserInController(controller);

        var formResultModel = A.Fake<PropertyViewModel>();

        // Act
        var result = await controller.AddNewProperty_Continue(2, formResultModel);

        // Assert
        A.CallTo(() => propertyService.UpdateProperty(1, formResultModel, true)).MustNotHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("ViewProperties");
    }

    [Fact]
    public async void AddNewPropertyContinuePost_AtMiddleSteps_UpdatesRecord_AndRedirectsToNextStep()
    {
        // Arrange
        var fakePropertyDbModel = A.Fake<PropertyDbModel>();
        fakePropertyDbModel.Id = 1;

        var propertyService = A.Fake<IPropertyService>();
        var apiService = A.Fake<IApiService>();
        A.CallTo(() => propertyService.GetIncompleteProperty(1)).Returns(fakePropertyDbModel);

        var controller = new PropertyController(propertyService, apiService,null!);
        CreateRegisteredUserInController(controller);

        var formResultModel = A.Fake<PropertyViewModel>();

        // Act
        var result = await controller.AddNewProperty_Continue(2, formResultModel);

        // Assert
        A.CallTo(() => propertyService.GetIncompleteProperty(1)).MustHaveHappened();
        A.CallTo(() => propertyService.UpdateProperty(1, formResultModel, true)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("AddNewProperty_Continue");
        result.Should().BeOfType<RedirectToActionResult>().Which.RouteValues.Should().ContainKey("step").WhoseValue
            .Should().Be(3);
    }

    [Fact]
    public async void AddNewPropertyContinuePost_AtFinalStep_UpdatesRecord_AndRedirectsToViewProperties()
    {
        // Arrange
        var propertyService = A.Fake<IPropertyService>();
        var apiService = A.Fake<IApiService>();

        var controller = new PropertyController(propertyService, apiService,null!);
        CreateRegisteredUserInController(controller);

        var formResultModel = A.Fake<PropertyViewModel>();

        // Act
        var result = await controller.AddNewProperty_Continue(AddNewPropertyViewModel.MaximumStep, formResultModel);

        // Assert
        A.CallTo(() => propertyService.UpdateProperty(0, formResultModel, false)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("ViewProperties");
    }

    [Fact]
    public void AddNewPropertyCancelPost_DeletesRecord_AndRedirectsToViewProperties()
    {
        // Arrange
        var fakePropertyDbModel = A.Fake<PropertyDbModel>();
        fakePropertyDbModel.Id = 1;

        var propertyService = A.Fake<IPropertyService>();
        var apiService = A.Fake<IApiService>();

        A.CallTo(() => propertyService.GetIncompleteProperty(1)).Returns(fakePropertyDbModel);

        var controller = new PropertyController(propertyService, apiService,null!);
        CreateRegisteredUserInController(controller);

        // Act
        var result = controller.AddNewProperty_Cancel();

        // Assert
        A.CallTo(() => propertyService.GetIncompleteProperty(1)).MustHaveHappened();
        A.CallTo(() => propertyService.DeleteProperty(fakePropertyDbModel)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("ViewProperties");
    }

    [Fact]
    public void AddNewPropertyCancelPost_WithNoAddInProgress_RedirectsToViewProperties()
    {
        // Arrange
        var fakePropertyDbModel = A.Fake<PropertyDbModel>();
        fakePropertyDbModel.Id = 1;

        var propertyService = A.Fake<IPropertyService>();
        var apiService = A.Fake<IApiService>();

        A.CallTo(() => propertyService.GetIncompleteProperty(1)).Returns(null);

        var controller = new PropertyController(propertyService, apiService,null!);
        CreateRegisteredUserInController(controller);

        // Act
        var result = controller.AddNewProperty_Cancel();

        // Assert
        A.CallTo(() => propertyService.GetIncompleteProperty(1)).MustHaveHappened();
        A.CallTo(() => propertyService.DeleteProperty(fakePropertyDbModel)).MustNotHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("ViewProperties");
    }
}