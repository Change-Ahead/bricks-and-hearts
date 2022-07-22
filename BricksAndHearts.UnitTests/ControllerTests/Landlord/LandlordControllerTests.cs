using BricksAndHearts.ViewModels;
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
        var unregisteredUser = CreateUnregisteredUserInController(_underTest);

        // Act
        var result = _underTest.RegisterGet() as ViewResult;

        // Assert
        result!.Model.Should().BeOfType<LandlordProfileModel>()
            .Which.Email.Should().Be(unregisteredUser.GoogleEmail);
    }

    /*


    [Fact]
    public void AddNewPropertyPost_WithValidModel_CallsPropertyServiceMethod_AndRedirects()
    {
        // Arrange
        var propertyService = A.Fake<IPropertyService>();

        var controller = new LandlordController(null!, null!, new LandlordService(null!), propertyService);
        CreateRegisteredUserInController(controller);

        var formResultModel = A.Fake<PropertyViewModel>();

        // Act
        var result = controller.AddNewProperty(formResultModel);

        // Assert
        A.CallTo(() => propertyService.AddNewProperty(formResultModel, 1)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("ViewProperties");
    }*/
}