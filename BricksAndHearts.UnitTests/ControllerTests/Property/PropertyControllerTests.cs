using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BricksAndHearts.Database;
using BricksAndHearts.ViewModels;
using BricksAndHearts.ViewModels.PropertyInput;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BricksAndHearts.UnitTests.ControllerTests.Property;

public class PropertyControllerTests : PropertyControllerTestsBase
{
    #region SortProperties

    [Fact]
    public async void PropertyList_ReturnsViewWith_PropertiesListModel()
    {
        // Arrange
        var dummyString = "Rent";

        // Act
        var result = await UnderTest.PropertyList(dummyString, "") as ViewResult;

        // Assert
        result!.ViewData.Model.Should().BeOfType<PropertyListModel>();
    }

    #endregion

    #region AvailableUnits

    [Fact]
    public void AvailableUnits_CalculatesCorrectly()
    {
        // Arrange
        var property = CreateExamplePropertyViewModel();

        // Act
        var availableUnits = property.AvailableUnits;

        // Assert
        availableUnits.Should().Be(3);
    }

    #endregion

    #region TestDataClasses

    //Classes to allow ranges for tests

    /// <summary>
    ///     Counts from 1, to 1 below final step
    /// </summary>
    private class NoLastStepTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var i in Enumerable.Range(1, PropertyInputFormViewModel.MaximumStep - 1))
                yield return new object[] { i };
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    ///     Counts from 2, to 1 below final step
    /// </summary>
    private class NoFirstNoLastStepTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var i in Enumerable.Range(2, PropertyInputFormViewModel.MaximumStep - 2))
                yield return new object[] { i };
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    ///     Counts from 1, to final step inclusively
    /// </summary>
    private class StepTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var i in Enumerable.Range(1, PropertyInputFormViewModel.MaximumStep))
                yield return new object[] { i };
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    #endregion

    #region ViewProperties

    [Fact]
    public async Task ViewProperty_WithNonExistingProperty_Returns404ErrorPage()
    {
        // Arrange
        A.CallTo(() => PropertyService.GetPropertyByPropertyId(1)).Returns(null);

        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        // Act
        var result = await UnderTest.ViewProperty(1);

        // Assert
        A.CallTo(() => PropertyService.GetPropertyByPropertyId(1)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("Error");
        result.As<RedirectToActionResult>().RouteValues.Should().Contain("status", 404);
    }

    [Fact]
    public async Task ViewProperty_WithExistingProperty_ReturnViewPropertyView()
    {
        // Arrange
        var model = CreateExamplePropertyDbModel();
        A.CallTo(() => PropertyService.GetPropertyByPropertyId(1)).Returns(model);

        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        // Act
        var result = await UnderTest.ViewProperty(1) as ViewResult;

        // Assert
        A.CallTo(() => PropertyService.GetPropertyByPropertyId(1)).MustHaveHappened();
        result!.ViewData.Model.Should().BeOfType<PropertyDetailsViewModel>();
    }

    #endregion

    #region PropertyImages

    // This is not working
    /*[Fact]
    public async void ListPropertyImages_CallsListFilesAsync_AndReturnsViewListPropertyImages()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);
        A.CallTo(() => PropertyService.IsUserAdminOrCorrectLandlord(adminUser, 1)).Returns(true);
        
        var fakeImage = CreateExampleImage();
        var image = (fakeImage.OpenReadStream(), "jpeg");
        A.CallTo(() => AzureStorage.DownloadFile("property", 1, fakeImage.FileName)).Returns(image);
        
        var urlHelper = A.Fake<IUrlHelper>();
        // ReSharper disable once Mvc.ActionNotResolved
        A.CallTo(() => urlHelper.Action("GetImage", new { propertyId = 1, fileName = "TestFileName" })!).Returns("/fake/url");
        UnderTest.Url = urlHelper;
        
        var fileNames = new List<string> { fakeImage.FileName };
        A.CallTo(() => AzureStorage.ListFileNames("property", 1)).Returns(fileNames);

        // Act
        var result = await UnderTest.ListPropertyImages(1) as ViewResult;

        // Assert
        A.CallTo(() => AzureStorage.ListFileNames("property", 1)).MustHaveHappened();
        result!.ViewData.Model.Should().BeOfType<ImageListViewModel>().And.Be(fileNames);
    }*/

    [Fact]
    public async void
        AddPropertyImages_CallsUploadImageForEachImage_AndRedirectsToViewProperty_WithFlashMultipleMessages()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);
        A.CallTo(() => PropertyService.IsUserAdminOrCorrectLandlord(adminUser, 1)).Returns(true);

        var fakeImage = CreateExampleImage();
        var fakeImage2 = CreateExampleImage();
        A.CallTo(() => AzureStorage.IsImage(fakeImage.FileName)).Returns((true, null));
        A.CallTo(() => AzureStorage.IsImage(fakeImage2.FileName)).Returns((true, null));
        var fakeImageList = new List<IFormFile> { fakeImage, fakeImage2 };

        // Act
        var result = await UnderTest.AddPropertyImages(fakeImageList, 1);

        // Assert
        A.CallTo(() => AzureStorage.UploadFile(fakeImage, "property", 1)).MustHaveHappened();
        A.CallTo(() => AzureStorage.UploadFile(fakeImage2, "property", 1)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("ViewProperty");
    }

    [Fact]
    public async void GetImage_CallsDownloadFileAsync_AndReturnsFile()
    {
        // Arrange
        var adminUser = CreateAdminUser();
        MakeUserPrincipalInController(adminUser, UnderTest);
        A.CallTo(() => PropertyService.IsUserAdminOrCorrectLandlord(adminUser, 1)).Returns(true);

        var fakeImage = CreateExampleImage();
        var image = (fakeImage.OpenReadStream(), "jpeg");
        A.CallTo(() => AzureStorage.DownloadFile("property", 1, fakeImage.FileName)).Returns(image);

        // Act
        var result = await UnderTest.GetImage(1, fakeImage.FileName) as FileStreamResult;

        // Assert
        A.CallTo(() => AzureStorage.DownloadFile("property", 1, fakeImage.FileName)).MustHaveHappened();
        result!.ContentType.Should().Be("image/jpeg");
    }

    [Fact]
    public async void DeleteImage_CallsDeleteFileAsync_AndRedirectsToViewProperty()
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
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("ViewProperty");
    }

    #endregion

    #region DeleteProperty

    [Fact]
    public void DeleteProperty_WithExistingProperty_DeletesPropertyAndImagesAndRedirectsToViewProperties()
    {
        // Arrange
        var fakePropertyDbModel = CreateExamplePropertyDbModel();
        fakePropertyDbModel.Landlord = A.Fake<LandlordDbModel>();
        fakePropertyDbModel.Landlord.Id = 1;
        A.CallTo(() => PropertyService.GetPropertyByPropertyId(1)).Returns(fakePropertyDbModel);

        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        // Act
        var result = UnderTest.DeleteProperty(1);

        // Assert
        A.CallTo(() => PropertyService.DeleteProperty(fakePropertyDbModel)).MustHaveHappened();
        A.CallTo(() => AzureStorage.DeleteContainer("property", fakePropertyDbModel.Id)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("ViewProperties");
    }

    [Fact]
    public void DeleteProperty_WithNonExistingProperty_RedirectsToViewProperties()
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
        A.CallTo(() => AzureStorage.DeleteContainer("property", fakePropertyDbModel.Id)).MustNotHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("ViewProperties");
    }

    #endregion

    #region Add/Edit Property

    [Fact]
    public void PropertyInputStepOneGet_ReturnsViewAtStep()
    {
        // Arrange
        var landlordUser = CreateLandlordUser();
        landlordUser.Id = 1;
        MakeUserPrincipalInController(landlordUser, UnderTest);
        var prop = A.Fake<PropertyDbModel>();
        prop.LandlordId = 1;
        prop.Id = 1;
        A.CallTo(() => PropertyService.GetPropertyByPropertyId(1)).Returns(prop);

        // Act
        var result = UnderTest.PropertyInputStepOne(prop.LandlordId, prop.Id, "add") as ViewResult;

        // Assert
        result!.ViewName.Should().Be("PropertyInput");
        result.Model.Should().BeOfType<PropertyInputFormViewModel>().Which.Step.Should().Be(1);
        result.Model.Should().BeOfType<PropertyInputFormViewModel>().Which.Step1?.Address.AddressLine1.Should()
            .NotBeNull();
    }

    [Theory]
    [InlineData("add")]
    [InlineData("edit")]
    public void PropertyInputStepTwoGet_ReturnsViewAtStep(string operationType)
    {
        // Arrange
        var landlordUser = CreateLandlordUser();
        landlordUser.Id = 1;
        MakeUserPrincipalInController(landlordUser, UnderTest);
        var prop = CreateExamplePropertyDbModel();
        prop.LandlordId = 1;
        prop.Id = 1;
        A.CallTo(() => PropertyService.GetPropertyByPropertyId(1)).Returns(prop);

        // Act
        var result = UnderTest.PropertyInputStepTwo(prop.Id, operationType) as ViewResult;

        // Assert
        result!.ViewName.Should().Be("PropertyInput");
        result.Model.Should().BeOfType<PropertyInputFormViewModel>().Which.Step.Should().Be(2);
        result.Model.Should().BeOfType<PropertyInputFormViewModel>().Which.Step1?.Address.AddressLine2.Should()
            .NotBeNull();
    }

    [Theory]
    [InlineData("add", false)]
    [InlineData("edit", true)]
    public async void PropertyInputPost_WithInvalidModel_ReturnsViewWithModel(string operationType,
        bool isEdit)
    {
        // Arrange
        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        var formResultModel = CreateExamplePropertyInputFormViewModel();
        formResultModel.LandlordId = 1;
        formResultModel.IsEdit = isEdit;
        UnderTest.ViewData.ModelState.AddModelError("Key", "ErrorMessage");

        // Act
        var result =
            await UnderTest.PropertyInputStepOne(formResultModel, formResultModel.LandlordId, 0, operationType) as
                RedirectToActionResult;

        // Assert
        A.CallTo(() => PropertyService.UpdateProperty(1, formResultModel.FormToViewModel(), true))
            .MustNotHaveHappened();
        A.CallTo(() => PropertyService.AddNewProperty(1, formResultModel.FormToViewModel(), true))
            .MustNotHaveHappened();
        result!.ActionName.Should().Be("PropertyInputStepOne");
        result.RouteValues.Should().ContainKey("propertyId").WhoseValue.Should().Be(0);
        result.RouteValues.Should().ContainKey("operationType").WhoseValue.Should().Be(operationType);
    }

    [Theory]
    [InlineData("add", false)]
    [InlineData("edit", true)]
    public async void PropertyInputPost_AtStep1_WithoutAddress1AndPostcode_ReturnsViewWithModel(
        string operationType, bool isEdit)
    {
        // Arrange
        A.CallTo(() => PropertyService.GetPropertyByPropertyId(1)).Returns(null);

        var landlordUser = CreateLandlordUser();
        landlordUser.LandlordId = 1;
        MakeUserPrincipalInController(landlordUser, UnderTest);

        var formResultModel = new PropertyInputFormViewModel
        {
            Step1 = new PropertyInputModelStep1
            {
                Address = new AddressModel()
            },
            LandlordId = 1,
            IsEdit = isEdit,
            PropertyId = 1
        };

        // Act
        var result =
            await UnderTest.PropertyInputStepOne(formResultModel, 1, 1, operationType) as RedirectToActionResult;

        // Assert
        A.CallTo(() => PropertyService.AddNewProperty(1, formResultModel.FormToViewModel(), true))
            .MustNotHaveHappened();
        A.CallTo(() => PropertyService.UpdateProperty(1, formResultModel.FormToViewModel(), true))
            .MustNotHaveHappened();
        result!.ActionName.Should().Be("PropertyInputStepOne");
        result.RouteValues.Should().ContainKey("propertyId").WhoseValue.Should().Be(1);
        result.RouteValues.Should().ContainKey("operationType").WhoseValue.Should().Be(operationType);
    }

    [Theory]
    [InlineData("add", false)]
    [InlineData("edit", true)]
    public async void PropertyInputPost_AtStep1_CreatesNewRecord_AndRedirectsToNextStep(string operationType,
        bool isEdit)
    {
        // Arrange
        if (!isEdit)
        {
            A.CallTo(() => PropertyService.GetPropertyByPropertyId(1)).Returns(null);
        }
        else
        {
            A.CallTo(() => PropertyService.GetPropertyByPropertyId(1)).Returns(new PropertyDbModel
            {
                LandlordId = 1,
                Id = 1
            });
        }

        var landlordUser = CreateLandlordUser();
        landlordUser.LandlordId = 1;
        MakeUserPrincipalInController(landlordUser, UnderTest);

        var formResultModel = new PropertyInputFormViewModel
        {
            Step1 = new PropertyInputModelStep1
            {
                Address = new AddressModel
                {
                    AddressLine1 = "Line 1",
                    Postcode = "Postcode"
                }
            },
            LandlordId = 1,
            IsEdit = isEdit,
            Step = 1,
            PropertyId = 1
        };

        A.CallTo(() => AzureMapsApiService.AutofillAddress(formResultModel.FormToViewModel()))
            .Returns(Task.Run(() => formResultModel.FormToViewModel()));

        // Act
        var result =
            await UnderTest.PropertyInputStepOne(formResultModel, 1, 1, operationType) as RedirectToActionResult;

        // Assert
        if (!isEdit)
        {
            A.CallTo(() => PropertyService.AddNewProperty(1, formResultModel.FormToViewModel(), A<bool>._))
                .WithAnyArguments().MustHaveHappened();
            A.CallTo(() => PropertyService.UpdateProperty(1, formResultModel.FormToViewModel(), A<bool>._))
                .WithAnyArguments().MustNotHaveHappened();
        }
        else
        {
            A.CallTo(() => PropertyService.AddNewProperty(1, formResultModel.FormToViewModel(), A<bool>._))
                .WithAnyArguments().MustNotHaveHappened();
            A.CallTo(() => PropertyService.UpdateProperty(1, formResultModel.FormToViewModel(), A<bool>._))
                .WithAnyArguments().MustHaveHappened();
        }

        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("PropertyInputStepTwo");
    }

    [Theory]
    [InlineData("add", false)]
    [InlineData("edit", true)]
    public void PropertyInputPost_AtMiddleSteps_UpdatesRecord_AndRedirectsToNextStep(string operationType,
        bool isEdit)
    {
        // Arrange
        var fakePropertyDbModel = CreateExamplePropertyDbModel();
        fakePropertyDbModel.Landlord.Id = 1;

        A.CallTo(() => PropertyService.GetPropertyByPropertyId(1)).Returns(fakePropertyDbModel);

        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        var formResultModel = CreateExamplePropertyInputFormViewModel();
        formResultModel.PropertyId = 1;
        formResultModel.LandlordId = 1;

        // Act
        var result = UnderTest.PropertyInputStepFive(formResultModel, operationType) as RedirectToActionResult;

        // Assert
        A.CallTo(() => PropertyService.GetPropertyByPropertyId(1)).MustHaveHappened();
        A.CallTo(() => PropertyService.UpdateProperty(1, A<PropertyViewModel>._, A<bool>._)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("PropertyInputStepSix");
    }

    [Theory]
    [InlineData("add", false)]
    [InlineData("edit", true)]
    public void PropertyInputStepSixPost_UpdatesRecord_AndRedirectsToViewProperties(string operationType,
        bool isEdit)
    {
        // Arrange
        var landlordUser = CreateLandlordUser();
        landlordUser.LandlordId = 1;
        MakeUserPrincipalInController(landlordUser, UnderTest);
        var fakePropertyDbModel = CreateExamplePropertyDbModel();
        fakePropertyDbModel.Landlord.Id = 1;

        A.CallTo(() => PropertyService.GetPropertyByPropertyId(1)).Returns(fakePropertyDbModel);
        var formResultModel = CreateExamplePropertyInputFormViewModel();
        formResultModel.PropertyId = 1;
        formResultModel.LandlordId = 1;

        // Act
        var result = UnderTest.PropertyInputStepSix(formResultModel) as RedirectToActionResult;

        // Assert
        A.CallTo(() => PropertyService.UpdateProperty(1, A<PropertyViewModel>._, true)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("ViewProperty");
    }

    [Theory]
    [InlineData("add", false, 1)]
    [InlineData("edit", true, 1)]
    [InlineData("add", false, 2)]
    [InlineData("edit", true, 2)]
    [InlineData("add", false, 3)]
    [InlineData("edit", true, 3)]
    [InlineData("add", false, 4)]
    [InlineData("edit", true, 4)]
    [InlineData("add", false, 5)]
    [InlineData("edit", true, 5)]
    [InlineData("add", false, 6)]
    [InlineData("edit", true, 6)]
    public async void PropertyInputStep_CalledByNonOwnerNonAdmin_Returns403(string operationType,
        bool isEdit, int step)
    {
        // Arrange
        var landlordUser = CreateLandlordUser();
        landlordUser.LandlordId = 2;
        MakeUserPrincipalInController(landlordUser, UnderTest);
        var fakePropertyDbModel = CreateExamplePropertyDbModel();
        fakePropertyDbModel.Landlord.Id = 1;

        A.CallTo(() => PropertyService.GetPropertyByPropertyId(1)).Returns(fakePropertyDbModel);
        var formResultModel = CreateExamplePropertyInputFormViewModel();
        formResultModel.PropertyId = 1;
        formResultModel.LandlordId = 1;
        formResultModel.IsEdit = isEdit;

        StatusCodeResult? result = null;
        // Act
        switch (step)
        {
            case 1:
                result = await UnderTest.PropertyInputStepOne(formResultModel, 1, 1, operationType) as StatusCodeResult;
                break;
            case 2:
                result = UnderTest.PropertyInputStepTwo(formResultModel, operationType) as StatusCodeResult;
                break;
            case 3:
                result = UnderTest.PropertyInputStepThree(formResultModel, operationType) as StatusCodeResult;
                break;
            case 4:
                result = UnderTest.PropertyInputStepFour(formResultModel, operationType) as StatusCodeResult;
                break;
            case 5:
                result = UnderTest.PropertyInputStepFive(formResultModel, operationType) as StatusCodeResult;
                break;
            case 6:
                result = UnderTest.PropertyInputStepSix(formResultModel) as StatusCodeResult;
                break;
        }

        // Assert
        A.CallTo(() => PropertyService.UpdateProperty(1, A<PropertyViewModel>._, true)).MustNotHaveHappened();
        result!.StatusCode.Should().Be(403);
    }


    [Fact]
    public async void PropertyInputCancelPost_DeletesRecordAndContainer_AndRedirectsToViewProperties()
    {
        // Arrange
        var fakePropertyDbModel = CreateExamplePropertyDbModel();
        A.CallTo(() => PropertyService.GetPropertyByPropertyId(1)).Returns(fakePropertyDbModel);

        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        // Act
        var result = await UnderTest.PropertyInputCancel(1, landlordUser.Id, "add");

        // Assert
        A.CallTo(() => PropertyService.DeleteProperty(fakePropertyDbModel)).MustHaveHappened();
        A.CallTo(() => AzureStorage.DeleteContainer("property", fakePropertyDbModel.Id)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("ViewProperties");
    }

    [Fact]
    public async void PropertyInputCancelPost_WithNoAddInProgress_RedirectsToViewProperties()
    {
        // Arrange
        var fakePropertyDbModel = CreateExamplePropertyDbModel();
        A.CallTo(() => PropertyService.GetPropertyByPropertyId(1)).Returns(null);

        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        // Act
        var result = await UnderTest.PropertyInputCancel(1, landlordUser.Id, "add");

        // Assert
        A.CallTo(() => PropertyService.DeleteProperty(fakePropertyDbModel)).MustNotHaveHappened();
        A.CallTo(() => AzureStorage.DeleteContainer("property", fakePropertyDbModel.Id)).MustNotHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("ViewProperties");
    }


    [Theory]
    [ClassData(typeof(StepTestData))]
    public async void EditProperty_AtEachStep_WhenPerformedByNonOwnerNonAdmin_ReturnsForbidden(int step)
    {
        // Arrange
        var landlordUser = CreateLandlordUser();
        landlordUser.LandlordId = 2;
        MakeUserPrincipalInController(landlordUser, UnderTest);

        var formResultModel = CreateExamplePropertyViewModel();
        formResultModel.LandlordId = 1;

        // Act
        var result =
            await UnderTest.EditProperty_Continue(step, 1, formResultModel,
                landlordUser.Id);

        // Assert
        A.CallTo(() => PropertyService.UpdateProperty(formResultModel.PropertyId, formResultModel, A<bool>._))
            .MustNotHaveHappened();
        result.Should().BeOfType<StatusCodeResult>().Which.StatusCode.Should().Be(403);
    }

    #endregion

    #region AvailableUnits

    [Fact]
    public void AvailableUnits_CalculatesCorrectly()
    {
        // Arrange
        var property = CreateExamplePropertyViewModel();

        // Act
        var availableUnits = property.AvailableUnits;

        // Assert
        availableUnits.Should().Be(3);
    }

    #endregion

    #region PublicLinks

    [Fact]
    public async Task PublicViewProperty_CalledWithValidLink_ReturnCorrectModel()
    {
        // Arrange
        var property = CreateExamplePropertyDbModel();
        const string token = "token";
        property.PublicViewLink = token;
        property.Landlord.Id = 1;
        A.CallTo(() => PropertyService.GetPropertyByPublicViewLink(token)).Returns(property);
        A.CallTo(() => AzureStorage.ListFileNames("property", property.Id)).Returns(new List<string>());
        A.CallTo(() => PropertyService.GetPropertyOwner(property.Id)).Returns(property.Landlord);
        A.CallTo(() => LandlordService.GetLandlordProfilePicture(property.LandlordId)).Returns("image url");
        var propertyViewModel = PropertyViewModel.FromDbModel(property);
        var landlordProfileModel = LandlordProfileModel.FromDbModel(property.Landlord);
        landlordProfileModel.GoogleProfileImageUrl = "image url";

        // Act
        var result = await UnderTest.PublicViewProperty(token) as ViewResult;

        // Assert
        result!.Model.Should().BeOfType<PropertyDetailsViewModel>().Which.Property.Should()
            .BeEquivalentTo(propertyViewModel);
        result.Model.As<PropertyDetailsViewModel>().Images.Should().BeEquivalentTo(new List<ImageFileUrlModel>());
        result.Model.As<PropertyDetailsViewModel>().Owner.Should().BeEquivalentTo(landlordProfileModel);
    }

    [Fact]
    public async Task PublicViewProperty_CalledWithInvalidLink_ReturnNullModel()
    {
        // Arrange
        const string token = "token";
        A.CallTo(() => PropertyService.GetPropertyByPublicViewLink(token)).Returns(null);

        // Act
        var result = await UnderTest.PublicViewProperty(token) as ViewResult;

        // Assert
        result!.Model.Should().BeNull();
    }

    #endregion
}