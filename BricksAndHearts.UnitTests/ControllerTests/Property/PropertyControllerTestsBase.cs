using System;
using System.IO;
using BricksAndHearts.Controllers;
using BricksAndHearts.Database;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;

namespace BricksAndHearts.UnitTests.ControllerTests.Property;

public class PropertyControllerTestsBase : ControllerTestsBase
{
    protected readonly IAzureMapsApiService AzureMapsApiService;
    protected readonly IAzureStorage AzureStorage;
    protected readonly ILogger<PropertyController>? Logger;
    protected readonly IPropertyService PropertyService;
    protected readonly ILandlordService LandlordService;
    protected readonly PropertyController UnderTest;

    protected PropertyControllerTestsBase()
    {
        PropertyService = A.Fake<IPropertyService>();
        LandlordService = A.Fake<ILandlordService>();
        AzureMapsApiService = A.Fake<IAzureMapsApiService>();
        AzureStorage = A.Fake<IAzureStorage>();
        var logger = A.Fake<ILogger<PropertyController>>();
        var httpContext = new DefaultHttpContext();
        var tempData = new TempDataDictionary(httpContext, A.Fake<ITempDataProvider>());
        UnderTest = new PropertyController(PropertyService, LandlordService, AzureMapsApiService, logger, AzureStorage)
            { TempData = tempData };
    }

    protected static PropertyViewModel CreateExamplePropertyViewModel()
    {
        return new PropertyViewModel
        {
            Address = new AddressModel
            {
                AddressLine1 = "Adr1",
                AddressLine2 = "Adr2",
                AddressLine3 = "Adr3",
                TownOrCity = "City",
                County = "County",
                Postcode = "Postcode"
            },
            PropertyType = "Type",
            NumOfBedrooms = 2,
            Rent = 1200,
            Description = "Description",
            CreationTime = DateTime.Now,
            TotalUnits = 5,
            OccupiedUnits = 2
        };
    }

    protected PropertyDbModel CreateExamplePropertyDbModel()
    {
        return new PropertyDbModel
        {
            LandlordId = 1,
            Id = 1,
            AddressLine1 = "10 Downing Street",
            AddressLine2 = "London",
            TownOrCity = "City",
            County = "County",
            Postcode = new PostcodeDbModel
            {
                Postcode = "SW1A 2AA"
            },
            NumOfBedrooms = 2,
            Rent = 750,
            Description = "Property description",
            Landlord = A.Fake<LandlordDbModel>(),
            AcceptsSingleTenant = false,
            AcceptsCouple = false,
            AcceptsFamily = true,
            AcceptsPets = true,
            AcceptsNotInEET = false,
            AcceptsCredit = true,
            AcceptsBenefits = false,
            AcceptsUnder35 = true,
            AcceptsWithoutGuarantor = true,
            OccupiedUnits = 2,
            TotalUnits = 5,
            AvailableFrom = null,
            Availability = "draft",
            PropertyType = "Detached"
        };
    }

    protected static IFormFile CreateExampleImage()
    {
        var stream = new MemoryStream();
        IFormFile fakeImage = new FormFile(stream, 0, 1, null!, "fakeImage.jpeg");
        return fakeImage;
    }
}