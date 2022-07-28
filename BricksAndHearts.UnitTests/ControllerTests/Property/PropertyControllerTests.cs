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
    public void AddNewPropertyBeginGet_ReturnsView_AtStep1()
    {
        // Arrange
        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        // Act
        var result = UnderTest.AddNewProperty_Begin() as ViewResult;

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
        MakeUserPrincipalInController(landlordUser, UnderTest);

        // Act
        var result = UnderTest.AddNewProperty_Continue(step) as ViewResult;

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
        var fakePropertyDbModel = CreateExamplePropertyDbModel();

        A.CallTo(() => PropertyService.GetIncompleteProperty(1)).Returns(fakePropertyDbModel);

        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        // Act
        var result = UnderTest.AddNewProperty_Continue(step) as ViewResult;

        // Assert
        result!.ViewName.Should().Be("AddNewProperty");
        A.CallTo(() => PropertyService.GetIncompleteProperty(1)).MustHaveHappened();
        result.Model.Should().BeOfType<AddNewPropertyViewModel>().Which.Step.Should().Be(step);
        result.Model.Should().BeOfType<AddNewPropertyViewModel>().Which.Property!.Description.Should().Be(fakePropertyDbModel.Description);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void AddNewPropertyContinueGet_WithNoAddInProgress_ReturnsBlankModel(int step)
    {
        // Arrange
        A.CallTo(() => PropertyService.GetIncompleteProperty(1)).Returns(null);

        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        // Act
        var result = UnderTest.AddNewProperty_Continue(step) as ViewResult;

        // Assert
        result!.ViewName.Should().Be("AddNewProperty");
        A.CallTo(() => PropertyService.GetIncompleteProperty(1)).MustHaveHappened();
        result.Model.Should().BeOfType<AddNewPropertyViewModel>().Which.Step.Should().Be(step);
        result.Model.Should().BeOfType<AddNewPropertyViewModel>().Which.Property!.Description.Should().BeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async void AddNewPropertyContinuePost_WithInvalidModel_ReturnsViewWithModel(int step)
    {
        // Arrange
        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        var formResultModel = CreateExamplePropertyViewModel();
        UnderTest.ViewData.ModelState.AddModelError("Key", "ErrorMessage");

        // Act
        var result = await UnderTest.AddNewProperty_Continue(step, formResultModel) as ViewResult;

        // Assert
        A.CallTo(() => PropertyService.UpdateProperty(1, formResultModel, true)).MustNotHaveHappened();
        A.CallTo(() => PropertyService.AddNewProperty(1, formResultModel, true)).MustNotHaveHappened();
        result!.ViewData.Model.Should().BeOfType<AddNewPropertyViewModel>().Which.Property.Should()
            .BeOfType<PropertyViewModel>().And.Be(formResultModel);
    }

    [Fact]
    public async void AddNewPropertyContinuePost_AtStep1_WithoutAddress1AndPostcode_ReturnsViewWithModel()
    {
        // Arrange
        A.CallTo(() => PropertyService.GetIncompleteProperty(1)).Returns(null);

        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        var formResultModel = new PropertyViewModel
        {
            Address = new PropertyAddress()
        };

        // Act
        var result = await UnderTest.AddNewProperty_Continue(1, formResultModel) as ViewResult;

        // Assert
        A.CallTo(() => PropertyService.AddNewProperty(1, formResultModel, true)).MustNotHaveHappened();
        A.CallTo(() => PropertyService.UpdateProperty(1, formResultModel, true)).MustNotHaveHappened();
        result!.ViewData.Model.Should().BeOfType<AddNewPropertyViewModel>().Which.Property.Should()
            .BeOfType<PropertyViewModel>().And.Be(formResultModel);
    }

    [Fact]
    public async void AddNewPropertyContinuePost_AtStep1_CreatesNewRecord_AndRedirectsToNextStep()
    {
        // Arrange
        A.CallTo(() => PropertyService.GetIncompleteProperty(1)).Returns(null);

        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        var formResultModel = new PropertyViewModel
        {
            Address = new PropertyAddress
            {
                AddressLine1 = "Line 1",
                Postcode = "Postcode"
            }
        };
        A.CallTo(() => AzureMapsApiService.AutofillAddress(formResultModel))
            .Returns(Task.Run(() => formResultModel));

        // Act
        var result = await UnderTest.AddNewProperty_Continue(1, formResultModel);

        // Assert
        A.CallTo(() => PropertyService.AddNewProperty(1, formResultModel, true)).MustHaveHappened();
        A.CallTo(() => PropertyService.UpdateProperty(1, formResultModel, true)).MustNotHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("AddNewProperty_Continue");
        result.Should().BeOfType<RedirectToActionResult>().Which.RouteValues.Should().ContainKey("step").WhoseValue
            .Should().Be(2);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async void AddNewPropertyContinuePost_AtMiddleSteps_WithNoAddInProgress_RedirectsToViewProperties(int step)
    {
        // Arrange
        A.CallTo(() => PropertyService.GetIncompleteProperty(1)).Returns(null);

        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        var formResultModel = A.Fake<PropertyViewModel>();

        // Act
        var result = await UnderTest.AddNewProperty_Continue(step, formResultModel);

        // Assert
        A.CallTo(() => PropertyService.UpdateProperty(1, formResultModel, true)).MustNotHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("ViewProperties");
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public async void AddNewPropertyContinuePost_AtMiddleSteps_UpdatesRecord_AndRedirectsToNextStep(int step)
    {
        // Arrange
        var fakePropertyDbModel = CreateExamplePropertyDbModel();

        A.CallTo(() => PropertyService.GetIncompleteProperty(1)).Returns(fakePropertyDbModel);

        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        var formResultModel = A.Fake<PropertyViewModel>();

        // Act
        var result = await UnderTest.AddNewProperty_Continue(step, formResultModel);

        // Assert
        A.CallTo(() => PropertyService.GetIncompleteProperty(1)).MustHaveHappened();
        A.CallTo(() => PropertyService.UpdateProperty(1, formResultModel, true)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("AddNewProperty_Continue");
        result.Should().BeOfType<RedirectToActionResult>().Which.RouteValues.Should().ContainKey("step").WhoseValue
            .Should().Be(step + 1);
    }

    [Fact]
    public async void AddNewPropertyContinuePost_AtFinalStep_UpdatesRecord_AndRedirectsToViewProperties()
    {
        // Arrange
        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        var formResultModel = A.Fake<PropertyViewModel>();

        // Act
        var result = await UnderTest.AddNewProperty_Continue(AddNewPropertyViewModel.MaximumStep, formResultModel);

        // Assert
        A.CallTo(() => PropertyService.UpdateProperty(0, formResultModel, false)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("ViewProperties");
    }

    [Fact]
    public void AddNewPropertyCancelPost_DeletesRecord_AndRedirectsToViewProperties()
    {
        // Arrange
        var fakePropertyDbModel = CreateExamplePropertyDbModel();
        A.CallTo(() => PropertyService.GetIncompleteProperty(1)).Returns(fakePropertyDbModel);

        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        // Act
        var result = UnderTest.AddNewProperty_Cancel();

        // Assert
        A.CallTo(() => PropertyService.GetIncompleteProperty(1)).MustHaveHappened();
        A.CallTo(() => PropertyService.DeleteProperty(fakePropertyDbModel)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("ViewProperties");
    }

    [Fact]
    public void AddNewPropertyCancelPost_WithNoAddInProgress_RedirectsToViewProperties()
    {
        // Arrange
        var fakePropertyDbModel = CreateExamplePropertyDbModel();
        A.CallTo(() => PropertyService.GetIncompleteProperty(1)).Returns(null);

        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        // Act
        var result = UnderTest.AddNewProperty_Cancel();

        // Assert
        A.CallTo(() => PropertyService.GetIncompleteProperty(1)).MustHaveHappened();
        A.CallTo(() => PropertyService.DeleteProperty(fakePropertyDbModel)).MustNotHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("ViewProperties");
    }
}
