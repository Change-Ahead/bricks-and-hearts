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
    public void AddNewPropertyBeginGet_ReturnsView_AtStep1()
    {
        // Arrange
        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, _underTest);

        // Act
        var result = _underTest.AddNewProperty_Begin() as ViewResult;

        // Assert
        result!.ViewName.Should().Be("AddNewProperty");
        result.Model.Should().BeOfType<AddNewPropertyViewModel>().Which.Step.Should().Be(1);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void AddNewPropertyContinueGet_ReturnsViewAtStep(int step)
    {
        // Arrange
        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, _underTest);

        // Act
        var result = _underTest.AddNewProperty_Continue(step) as ViewResult;

        // Assert
        result!.ViewName.Should().Be("AddNewProperty");
        result.Model.Should().BeOfType<AddNewPropertyViewModel>().Which.Step.Should().Be(step);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void AddNewPropertyContinueGet_WithAddInProgress_ReturnsPartiallyCompleteModel(int step)
    {
        // Arrange
        var fakePropertyDbModel = A.Fake<PropertyDbModel>();
        fakePropertyDbModel.Id = 1;
        fakePropertyDbModel.Description = "hello";

        var propertyService = A.Fake<IPropertyService>();
        A.CallTo(() => propertyService.GetIncompleteProperty(1)).Returns(fakePropertyDbModel);

        var controller = new PropertyController(propertyService, null!);
        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, controller);

        // Act
        var result = controller.AddNewProperty_Continue(step) as ViewResult;

        // Assert
        result!.ViewName.Should().Be("AddNewProperty");
        A.CallTo(() => propertyService.GetIncompleteProperty(1)).MustHaveHappened();
        result.Model.Should().BeOfType<AddNewPropertyViewModel>().Which.Step.Should().Be(step);
        result.Model.Should().BeOfType<AddNewPropertyViewModel>().Which.Property!.Description.Should().Be("hello");
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void AddNewPropertyContinueGet_WithNoAddInProgress_ReturnsBlankModel(int step)
    {
        // Arrange
        var propertyService = A.Fake<IPropertyService>();
        A.CallTo(() => propertyService.GetIncompleteProperty(1)).Returns(null);

        var controller = new PropertyController(propertyService, null!);
        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, controller);

        // Act
        var result = controller.AddNewProperty_Continue(step) as ViewResult;

        // Assert
        result!.ViewName.Should().Be("AddNewProperty");
        A.CallTo(() => propertyService.GetIncompleteProperty(1)).MustHaveHappened();
        result.Model.Should().BeOfType<AddNewPropertyViewModel>().Which.Step.Should().Be(step);
        result.Model.Should().BeOfType<AddNewPropertyViewModel>().Which.Property!.Description.Should().BeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void AddNewPropertyContinuePost_WithInvalidModel_ReturnsViewWithModel(int step)
    {
        // Arrange 
        var propertyService = A.Fake<IPropertyService>();

        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, _underTest);

        var formResultModel = CreateExamplePropertyViewModel();
        _underTest.ViewData.ModelState.AddModelError("Key", "ErrorMessage");

        // Act
        var result = _underTest.AddNewProperty_Continue(step, formResultModel) as ViewResult;

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
        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, controller);

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
    public void AddNewPropertyContinuePost_AtStep1_CreatesNewRecord_AndRedirectsToNextStep()
    {
        // Arrange
        var propertyService = A.Fake<IPropertyService>();
        A.CallTo(() => propertyService.GetIncompleteProperty(1)).Returns(null);

        var controller = new PropertyController(propertyService, null!);
        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, controller);

        var formResultModel = new PropertyViewModel
        {
            Address = new PropertyAddress
            {
                AddressLine1 = "Line 1",
                Postcode = "Postcode"
            }
        };

        // Act
        var result = controller.AddNewProperty_Continue(1, formResultModel);

        // Assert
        A.CallTo(() => propertyService.AddNewProperty(1, formResultModel, true)).MustHaveHappened();
        A.CallTo(() => propertyService.UpdateProperty(1, formResultModel, true)).MustNotHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("AddNewProperty_Continue");
        result.Should().BeOfType<RedirectToActionResult>().Which.RouteValues.Should().ContainKey("step").WhoseValue
            .Should().Be(2);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void AddNewPropertyContinuePost_AtMiddleSteps_WithNoAddInProgress_RedirectsToViewProperties(int step)
    {
        // Arrange
        var propertyService = A.Fake<IPropertyService>();
        A.CallTo(() => propertyService.GetIncompleteProperty(1)).Returns(null);

        var controller = new PropertyController(propertyService, null!);
        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, controller);

        var formResultModel = A.Fake<PropertyViewModel>();

        // Act
        var result = controller.AddNewProperty_Continue(step, formResultModel);

        // Assert
        A.CallTo(() => propertyService.UpdateProperty(1, formResultModel, true)).MustNotHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("ViewProperties");
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void AddNewPropertyContinuePost_AtMiddleSteps_UpdatesRecord_AndRedirectsToNextStep(int step)
    {
        // Arrange
        var fakePropertyDbModel = A.Fake<PropertyDbModel>();
        fakePropertyDbModel.Id = 1;

        var propertyService = A.Fake<IPropertyService>();
        A.CallTo(() => propertyService.GetIncompleteProperty(1)).Returns(fakePropertyDbModel);

        var controller = new PropertyController(propertyService, null!);
        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, controller);

        var formResultModel = A.Fake<PropertyViewModel>();

        // Act
        var result = controller.AddNewProperty_Continue(step, formResultModel);

        // Assert
        A.CallTo(() => propertyService.GetIncompleteProperty(1)).MustHaveHappened();
        A.CallTo(() => propertyService.UpdateProperty(1, formResultModel, true)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("AddNewProperty_Continue");
        result.Should().BeOfType<RedirectToActionResult>().Which.RouteValues.Should().ContainKey("step").WhoseValue
            .Should().Be(step + 1);
    }

    [Fact]
    public void AddNewPropertyContinuePost_AtFinalStep_UpdatesRecord_AndRedirectsToViewProperties()
    {
        // Arrange
        var propertyService = A.Fake<IPropertyService>();

        var controller = new PropertyController(propertyService, null!);
        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, controller);

        var formResultModel = A.Fake<PropertyViewModel>();

        // Act
        var result = controller.AddNewProperty_Continue(AddNewPropertyViewModel.MaximumStep, formResultModel);

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
        A.CallTo(() => propertyService.GetIncompleteProperty(1)).Returns(fakePropertyDbModel);

        var controller = new PropertyController(propertyService, null!);
        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, controller);

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
        A.CallTo(() => propertyService.GetIncompleteProperty(1)).Returns(null);

        var controller = new PropertyController(propertyService, null!);
        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, controller);

        // Act
        var result = controller.AddNewProperty_Cancel();

        // Assert
        A.CallTo(() => propertyService.GetIncompleteProperty(1)).MustHaveHappened();
        A.CallTo(() => propertyService.DeleteProperty(fakePropertyDbModel)).MustNotHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("ViewProperties");
    }
}
