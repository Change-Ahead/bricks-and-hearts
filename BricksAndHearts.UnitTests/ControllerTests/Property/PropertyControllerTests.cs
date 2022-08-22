using System.Collections.Generic;
using System.Threading.Tasks;
using BricksAndHearts.Auth;
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
        prop.AddressLine1 = "Test Street";
        prop.LandlordId = 1;
        prop.Id = 1;
        A.CallTo(() => PropertyService.GetPropertyByPropertyId(1)).Returns(prop);
        A.CallTo(() => PropertyService.IsUserAdminOrCorrectLandlord(A<BricksAndHeartsUser>._, A<int>._))
            .WithAnyArguments().Returns(true);

        // Act
        var result = UnderTest.PropertyInputStepOnePostcode("add", prop.Id, prop.LandlordId) as ViewResult;

        // Assert
        result!.ViewName.Should().Be("PropertyInputForm/InitialAddress");
        result.Model.Should().BeOfType<PropertyInputModelInitialAddress>().Which.AddressLine1.Should()
            .Be(prop.AddressLine1);
    }

    [Fact]
    public void PropertyInputStepOneWithNoPropInDBGet_ReturnsViewAtStep()
    {
        // Arrange
        var landlordUser = CreateLandlordUser();
        landlordUser.Id = 1;
        MakeUserPrincipalInController(landlordUser, UnderTest);
        A.CallTo(() => PropertyService.GetPropertyByPropertyId(0)).Returns(null);

        // Act
        var result = UnderTest.PropertyInputStepOnePostcode("add", 0, landlordUser.Id) as ViewResult;

        // Assert
        result!.ViewName.Should().Be("PropertyInputForm/InitialAddress");
        result.Model.Should().BeOfType<PropertyInputModelInitialAddress>().Which.Step.Should().Be(1);
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
        A.CallTo(() => PropertyService.IsUserAdminOrCorrectLandlord(A<BricksAndHeartsUser>._, A<int>._))
            .WithAnyArguments().Returns(true);
        // Act
        var result = UnderTest.PropertyInputStepTwoAddress(prop.Id, operationType) as ViewResult;

        // Assert
        result!.ViewName.Should().Be("PropertyInputForm/FullAddress");
        result.Model.Should().BeOfType<PropertyInputModelAddress>().Which.AddressLine1.Should().Be(prop.AddressLine1);
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

        var formResultModel = new PropertyInputModelInitialAddress
        {
            LandlordId = 1,
            IsEdit = isEdit
        };
        UnderTest.ViewData.ModelState.AddModelError("Key", "ErrorMessage");

        // Act
        var result =
            await UnderTest.PropertyInputStepOnePostcode(formResultModel, 999, operationType,
                    formResultModel.LandlordId)
                as
                RedirectToActionResult;

        // Assert
        A.CallTo(() => PropertyService.UpdateProperty(1, A<PropertyViewModel>._, true)).WithAnyArguments()
            .MustNotHaveHappened();
        A.CallTo(() => PropertyService.AddNewProperty(1, A<PropertyViewModel>._, true)).WithAnyArguments()
            .MustNotHaveHappened();
        result!.ActionName.Should().Be("PropertyInputStepOnePostcode");
        result.RouteValues.Should().ContainKey("propertyId").WhoseValue.Should().Be(999);
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

        var formResultModel = new PropertyInputModelInitialAddress
        {
            AddressLine1 = "Line 1",
            Postcode = "Postcode",
            LandlordId = 1,
            IsEdit = isEdit,
            Step = 1,
            PropertyId = 1
        };

        A.CallTo(() => AzureMapsApiService.AutofillAddress(formResultModel.FormToViewModel()))
            .Returns(Task.Run(() => formResultModel.FormToViewModel()));

        // Act
        var result =
            await UnderTest.PropertyInputStepOnePostcode(formResultModel, 1, operationType,
                1) as RedirectToActionResult;

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

        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("PropertyInputStepTwoAddress");
    }

    [Theory]
    [InlineData("add")]
    [InlineData("edit")]
    public async void PropertyInputPost_AtStepTwo_UpdatesRecord_ThenOnAddAndRedirectsToNextStep_OrOnEditReturnsToView(
        string operationType)
    {
        // Arrange
        A.CallTo(() => PropertyService.IsUserAdminOrCorrectLandlord(A<BricksAndHeartsUser>._, A<int>._))
            .WithAnyArguments().Returns(true);

        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        var formResultModel = new PropertyInputModelAddress();
        formResultModel.InitialiseViewModel(CreateExamplePropertyDbModel());
        formResultModel.PropertyId = 1;
        formResultModel.LandlordId = 1;


        // Act
        var result =
            await UnderTest.PropertyInputStepTwoAddress(formResultModel, formResultModel.PropertyId, operationType) as
                RedirectToActionResult;

        // Assert
        A.CallTo(() => PropertyService.UpdateProperty(1, A<PropertyViewModel>._, A<bool>._)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should()
            .Be(operationType == "add" ? "PropertyInputStepThreeDetails" : "ViewProperty");
    }

    [Theory]
    [InlineData("add")]
    [InlineData("edit")]
    public async void PropertyInputPost_AtStepThree_UpdatesRecord_ThenOnAddAndRedirectsToNextStep_OrOnEditReturnsToView(
        string operationType)
    {
        // Arrange
        A.CallTo(() => PropertyService.IsUserAdminOrCorrectLandlord(A<BricksAndHeartsUser>._, A<int>._))
            .WithAnyArguments().Returns(true);

        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        var formResultModel = new PropertyInputModelDetails();
        formResultModel.InitialiseViewModel(CreateExamplePropertyDbModel());
        formResultModel.PropertyId = 1;
        formResultModel.LandlordId = 1;


        // Act
        var result =
            await UnderTest.PropertyInputStepThreeDetails(formResultModel, formResultModel.PropertyId, operationType) as
                RedirectToActionResult;

        // Assert
        A.CallTo(() => PropertyService.UpdateProperty(1, A<PropertyViewModel>._, A<bool>._)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should()
            .Be(operationType == "add" ? "PropertyInputStepFourDescription" : "ViewProperty");
    }

    [Theory]
    [InlineData("add")]
    [InlineData("edit")]
    public async void PropertyInputPost_AtStepFour_UpdatesRecord_ThenOnAddAndRedirectsToNextStep_OrOnEditReturnsToView(
        string operationType)
    {
        // Arrange
        A.CallTo(() => PropertyService.IsUserAdminOrCorrectLandlord(A<BricksAndHeartsUser>._, A<int>._))
            .WithAnyArguments().Returns(true);

        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        var formResultModel = new PropertyInputModelDescription();
        formResultModel.InitialiseViewModel(CreateExamplePropertyDbModel());
        formResultModel.PropertyId = 1;
        formResultModel.LandlordId = 1;

        // Act
        var result =
            await UnderTest.PropertyInputStepFourDescription(formResultModel, formResultModel.PropertyId, operationType)
                as
                RedirectToActionResult;

        // Assert
        A.CallTo(() => PropertyService.UpdateProperty(1, A<PropertyViewModel>._, A<bool>._)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should()
            .Be(operationType == "add" ? "PropertyInputStepFiveTenantPreferences" : "ViewProperty");
    }

    [Theory]
    [InlineData("add")]
    [InlineData("edit")]
    public async void PropertyInputPost_AtStepFive_UpdatesRecord_ThenOnAddAndRedirectsToNextStep_OrOnEditReturnsToView(
        string operationType)
    {
        // Arrange
        A.CallTo(() => PropertyService.IsUserAdminOrCorrectLandlord(A<BricksAndHeartsUser>._, A<int>._))
            .WithAnyArguments().Returns(true);

        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        var formResultModel = new PropertyInputModelTenantPreferences();
        formResultModel.InitialiseViewModel(CreateExamplePropertyDbModel());
        formResultModel.PropertyId = 1;
        formResultModel.LandlordId = 1;

        // Act
        var result =
            await UnderTest.PropertyInputStepFiveTenantPreferences(formResultModel, formResultModel.PropertyId,
                    operationType)
                as
                RedirectToActionResult;

        // Assert
        A.CallTo(() => PropertyService.UpdateProperty(1, A<PropertyViewModel>._, A<bool>._)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should()
            .Be(operationType == "add" ? "PropertyInputStepSixAvailability" : "ViewProperty");
    }

    [Theory]
    [InlineData("add")]
    [InlineData("edit")]
    public async void PropertyInputPost_AtStepSix_UpdatesRecord_ThenReturnsToView(string operationType)
    {
        // Arrange
        A.CallTo(() => PropertyService.IsUserAdminOrCorrectLandlord(A<BricksAndHeartsUser>._, A<int>._))
            .WithAnyArguments().Returns(true);

        var landlordUser = CreateLandlordUser();
        MakeUserPrincipalInController(landlordUser, UnderTest);

        var formResultModel = new PropertyInputModelAvailability();
        formResultModel.InitialiseViewModel(CreateExamplePropertyDbModel());
        formResultModel.PropertyId = 1;
        formResultModel.LandlordId = 1;

        // Act
        var result =
            await UnderTest.PropertyInputStepSixAvailability(formResultModel, formResultModel.PropertyId, operationType)
                as
                RedirectToActionResult;

        // Assert
        A.CallTo(() => PropertyService.UpdateProperty(1, A<PropertyViewModel>._, A<bool>._)).MustHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should()
            .Be("ViewProperty");
    }

    [Theory]
    [InlineData("add", 1)]
    [InlineData("edit", 1)]
    [InlineData("add", 2)]
    [InlineData("edit", 2)]
    [InlineData("add", 3)]
    [InlineData("edit", 3)]
    [InlineData("add", 4)]
    [InlineData("edit", 4)]
    [InlineData("add", 5)]
    [InlineData("edit", 5)]
    [InlineData("add", 6)]
    [InlineData("edit", 6)]
    public async Task PropertyInputPostStep_CalledByNonOwnerNonAdmin_Returns403(string operationType, int step)
    {
        // Arrange
        MakeUserPrincipalInController(CreateLandlordUser(), UnderTest);

        A.CallTo(() => PropertyService.IsUserAdminOrCorrectLandlord(A<BricksAndHeartsUser>._, A<int>._))
            .WithAnyArguments().Returns(false);
        UnderTest.ModelState.Clear();

        // Act
        var result = step switch
        {
            1 => await UnderTest.PropertyInputStepOnePostcode(A.Fake<PropertyInputModelInitialAddress>(), 1,
                operationType, 1) as StatusCodeResult,
            2 => await UnderTest.PropertyInputStepTwoAddress(A.Fake<PropertyInputModelAddress>(), 1, operationType) as
                StatusCodeResult,
            3 => await UnderTest.PropertyInputStepThreeDetails(A.Fake<PropertyInputModelDetails>(), 1, operationType) as
                StatusCodeResult,
            4 => await UnderTest.PropertyInputStepFourDescription(A.Fake<PropertyInputModelDescription>(), 1,
                    operationType)
                as StatusCodeResult,
            5 => await UnderTest.PropertyInputStepFiveTenantPreferences(A.Fake<PropertyInputModelTenantPreferences>(),
                1,
                operationType) as StatusCodeResult,
            6 => await UnderTest.PropertyInputStepSixAvailability(A.Fake<PropertyInputModelAvailability>(), 1,
                    operationType)
                as StatusCodeResult,
            _ => null
        };

        // Assert
        A.CallTo(() => PropertyService.UpdateProperty(1, A<PropertyViewModel>._, true)).MustNotHaveHappened();
        result!.StatusCode.Should().Be(403);
    }

    [Theory]
    [InlineData("add", 1)]
    [InlineData("edit", 1)]
    [InlineData("add", 2)]
    [InlineData("edit", 2)]
    [InlineData("add", 3)]
    [InlineData("edit", 3)]
    [InlineData("add", 4)]
    [InlineData("edit", 4)]
    [InlineData("add", 5)]
    [InlineData("edit", 5)]
    [InlineData("add", 6)]
    [InlineData("edit", 6)]
    public async void PropertyInputGetStep_CalledByNonOwnerNonAdmin_Returns403(string operationType, int step)
    {
        // Arrange
        MakeUserPrincipalInController(CreateLandlordUser(), UnderTest);

        A.CallTo(() => PropertyService.IsUserAdminOrCorrectLandlord(A<BricksAndHeartsUser>._, A<int>._))
            .WithAnyArguments().Returns(false);
        A.CallTo(() => PropertyService.GetPropertyByPropertyId(1)).Returns(A.Fake<PropertyDbModel>());
        UnderTest.ModelState.Clear();

        var result = step switch
        {
            // Act
            1 => await UnderTest.PropertyInputStepOnePostcode(A.Fake<PropertyInputModelInitialAddress>(), 1,
                operationType, 1) as StatusCodeResult,
            2 => UnderTest.PropertyInputStepTwoAddress(1, operationType) as StatusCodeResult,
            3 => UnderTest.PropertyInputStepThreeDetails(1, operationType) as StatusCodeResult,
            4 => UnderTest.PropertyInputStepFourDescription(1, operationType) as StatusCodeResult,
            5 => UnderTest.PropertyInputStepFiveTenantPreferences(1, operationType) as StatusCodeResult,
            6 => UnderTest.PropertyInputStepSixAvailability(1, operationType) as StatusCodeResult,
            _ => null
        };

        // Assert
        A.CallTo(() => PropertyService.UpdateProperty(1, A<PropertyViewModel>._, true)).MustNotHaveHappened();
        result!.StatusCode.Should().Be(403);
    }

    [Fact]
    public async void PropertyInputCancelPost_DeletesRecordAndContainer_AndRedirectsToViewProperties()
    {
        // Arrange
        var fakePropertyDbModel = CreateExamplePropertyDbModel();
        fakePropertyDbModel.LandlordId = 1;
        A.CallTo(() => PropertyService.GetPropertyByPropertyId(1)).Returns(fakePropertyDbModel);

        var landlordUser = CreateLandlordUser();
        landlordUser.Id = 1;
        MakeUserPrincipalInController(landlordUser, UnderTest);

        A.CallTo(() => PropertyService.IsUserAdminOrCorrectLandlord(A<BricksAndHeartsUser>._, A<int>._))
            .WithAnyArguments().Returns(true);

        // Act
        var result = await UnderTest.PropertyInputCancel(1, landlordUser.Id);

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
        var result = await UnderTest.PropertyInputCancel(1, landlordUser.Id);

        // Assert
        A.CallTo(() => PropertyService.DeleteProperty(fakePropertyDbModel)).MustNotHaveHappened();
        A.CallTo(() => AzureStorage.DeleteContainer("property", fakePropertyDbModel.Id)).MustNotHaveHappened();
        result.Should().BeOfType<RedirectToActionResult>().Which.ActionName.Should().Be("ViewProperties");
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