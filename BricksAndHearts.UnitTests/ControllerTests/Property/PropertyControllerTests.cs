using System.Collections.Generic;
using System.Threading.Tasks;
using BricksAndHearts.ViewModels;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
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

        var formResultModel = CreateExamplePropertyViewModel();

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

        var formResultModel = CreateExamplePropertyViewModel();

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

        var formResultModel = CreateExamplePropertyViewModel();

        // Act
        var result = await UnderTest.AddNewProperty_Continue(AddNewPropertyViewModel.MaximumStep, formResultModel);

        // Assert
        A.CallTo(() => PropertyService.UpdateProperty(0, formResultModel, false)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("ViewProperties");
    }

    [Fact]
    public async void AddNewPropertyCancelPost_DeletesRecordAndContainer_AndRedirectsToViewProperties()
    {
        // Arrange
        var fakePropertyDbModel = CreateExamplePropertyDbModel();
        A.CallTo(() => PropertyService.GetIncompleteProperty(1)).Returns(fakePropertyDbModel);

        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        // Act
        var result = await UnderTest.AddNewProperty_Cancel();

        // Assert
        A.CallTo(() => PropertyService.GetIncompleteProperty(1)).MustHaveHappened();
        A.CallTo(() => PropertyService.DeleteProperty(fakePropertyDbModel)).MustHaveHappened();
        A.CallTo(() => AzureStorage.DeleteContainer("property", fakePropertyDbModel.Id)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("ViewProperties");
    }

    [Fact]
    public async void AddNewPropertyCancelPost_WithNoAddInProgress_RedirectsToViewProperties()
    {
        // Arrange
        var fakePropertyDbModel = CreateExamplePropertyDbModel();
        A.CallTo(() => PropertyService.GetIncompleteProperty(1)).Returns(null);

        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        // Act
        var result = await UnderTest.AddNewProperty_Cancel();

        // Assert
        A.CallTo(() => PropertyService.GetIncompleteProperty(1)).MustHaveHappened();
        A.CallTo(() => PropertyService.DeleteProperty(fakePropertyDbModel)).MustNotHaveHappened();
        A.CallTo(() => AzureStorage.DeleteContainer("property", fakePropertyDbModel.Id)).MustNotHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("ViewProperties");
    }

    [Fact]
    public void ViewProperty_WithNonExistingProperty_Returns404ErrorPage()
    {
        // Arrange
        A.CallTo(() => PropertyService.GetPropertyByPropertyId(1)).Returns(null);
        
        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);
        
        // Act
        var result = UnderTest.ViewProperty(1);
        
        // Assert
        A.CallTo(() => PropertyService.GetPropertyByPropertyId(1)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Error");
        result.As<RedirectToActionResult>().RouteValues.Should().Contain("status",404);
    }
    
    [Fact]
    public void ViewProperty_WithExistingProperty_ReturnViewPropertyView()
    {
        // Arrange
        var model = CreateExamplePropertyDbModel();
        A.CallTo(() => PropertyService.GetPropertyByPropertyId(1)).Returns(model);
        
        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);
        
        // Act
        var result = UnderTest.ViewProperty(1) as ViewResult;
        
        // Assert
        A.CallTo(() => PropertyService.GetPropertyByPropertyId(1)).MustHaveHappened();
        result!.ViewData.Model.Should().BeOfType<PropertyViewModel>();
    }
    
    [Fact]
    public async void ListPropertyImages_CallsListFilesAsync_AndReturnsViewListPropertyImages()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);
        A.CallTo(() => PropertyService.IsUserAdminOrCorrectLandlord(adminUser, 1)).Returns(true);

        var model = new ImageListViewModel
        {
            PropertyId = 1,
            FileList = new List<string> { "image1", "image2" }
        };
        A.CallTo(() => AzureStorage.ListFiles("property", 1)).Returns(model);

        // Act
        var result = await UnderTest.ListPropertyImages(1) as ViewResult;

        // Assert
        A.CallTo(() => AzureStorage.ListFiles("property", 1)).MustHaveHappened();
        result!.ViewData.Model.Should().BeOfType<ImageListViewModel>().And.Be(model);
    }
    
    [Fact]
    public async void AddPropertyImages_CallsUploadImageForEachImage_AndRedirectsToListImagesWithFlashMessage()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);
        A.CallTo(() => PropertyService.IsUserAdminOrCorrectLandlord(adminUser, 1)).Returns(true);

        var fakeImage = CreateExampleImage();
        var fakeImage2 = CreateExampleImage();
        A.CallTo(() => AzureStorage.IsImage(fakeImage.FileName)).Returns(true);
        A.CallTo(() => AzureStorage.IsImage(fakeImage2.FileName)).Returns(true);
        var fakeImageList = new List<IFormFile>{fakeImage, fakeImage2};
        
        // Act
        var result = await UnderTest.AddPropertyImages(fakeImageList, 1);

        // Assert
        A.CallTo(() => AzureStorage.UploadFile(fakeImage, "property", 1)).MustHaveHappened();
        A.CallTo(() => AzureStorage.UploadFile(fakeImage2, "property", 1)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("ListPropertyImages");
        UnderTest.TempData["FlashMessage"].Should().Be("");
    }

    [Fact]
    public async void DisplayImage_CallsDownloadFileAsync_AndReturnsFile()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);
        A.CallTo(() => PropertyService.IsUserAdminOrCorrectLandlord(adminUser, 1)).Returns(true);
        
        var fakeImage = CreateExampleImage();
        var image = (fakeImage.OpenReadStream(), "jpeg");
        A.CallTo(() => AzureStorage.DownloadFile("property", 1, fakeImage.FileName)).Returns(image);

        // Act
        var result = await UnderTest.DisplayPropertyImage(1, fakeImage.FileName) as FileStreamResult;

        // Assert
        A.CallTo(() => AzureStorage.DownloadFile("property", 1, fakeImage.FileName)).MustHaveHappened();
        result!.ContentType.Should().Be("image/jpeg");
    }
    
    [Fact]
    public async void DeleteImage_CallsDeleteFileAsync_AndRedirectsToListImages()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);
        A.CallTo(() => PropertyService.IsUserAdminOrCorrectLandlord(adminUser, 1)).Returns(true);
        
        var fakeImage = CreateExampleImage();
        
        // Act
        var result = await UnderTest.DeletePropertyImage(1, fakeImage.FileName);

        // Assert
        A.CallTo(() => AzureStorage.DeleteFile("property", 1, fakeImage.FileName)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("ListPropertyImages");
    }
    
    [Fact]
    public void DeleteProperty_WithExistingProperty_DeletesAndRedirectsToViewProperties()
    {
        // Arrange
        var fakePropertyDbModel = CreateExamplePropertyDbModel();
        A.CallTo(() => PropertyService.GetPropertyByPropertyId(1)).Returns(fakePropertyDbModel);

        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        // Act
        var result = UnderTest.DeleteProperty(1);

        // Assert
        A.CallTo(() => PropertyService.DeleteProperty(fakePropertyDbModel)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("ViewProperties");
    }
    
    [Fact]
    public void DeleteProperty_WithNonExistingProperty_Returns404ErrorPage()
    {
        // Arrange
        var fakePropertyDbModel = CreateExamplePropertyDbModel();
        A.CallTo(() => PropertyService.GetPropertyByPropertyId(1)).Returns(null);

        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        // Act
        var result = UnderTest.DeleteProperty(1);

        // Assert
        A.CallTo(() => PropertyService.GetPropertyByPropertyId(1)).MustHaveHappened();
        A.CallTo(() => PropertyService.DeleteProperty(fakePropertyDbModel)).MustNotHaveHappened();
        result.Should().BeOfType<StatusCodeResult>().Which.StatusCode.Should().Be(404);
    }
    
    [Fact]
    public void SortProperties_ReturnsViewWith_PropertiesDashboardViewModel()
    {
        // Arrange
        var dummyString = "Rent";

        // Act
        var result = UnderTest.SortProperties(dummyString) as ViewResult;

        // Assert
        result!.ViewData.Model.Should().BeOfType<PropertiesDashboardViewModel>();
    }
}