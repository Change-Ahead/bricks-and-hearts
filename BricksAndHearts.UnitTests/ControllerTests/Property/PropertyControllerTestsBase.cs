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
    protected readonly IPropertyService PropertyService;
    protected readonly PropertyController UnderTest;

    protected PropertyControllerTestsBase()
    {
        PropertyService = A.Fake<IPropertyService>();
        AzureMapsApiService = A.Fake<IAzureMapsApiService>();
        AzureStorage = A.Fake<IAzureStorage>();
        var logger = A.Fake<ILogger<PropertyController>>();
        var httpContext = new DefaultHttpContext();
        var tempData = new TempDataDictionary(httpContext, A.Fake<ITempDataProvider>());
        UnderTest = new PropertyController(PropertyService, AzureMapsApiService, logger, AzureStorage){TempData = tempData};
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

    protected static PropertyDbModel CreateExamplePropertyDbModel()
    {
        return new PropertyDbModel
        {
            LandlordId = 1,
            Id = 1,
            AddressLine1 = "10 Downing Street",
            Postcode = new PostcodeDbModel
            {
                Postcode = "SW1A 2AA"
            },
            NumOfBedrooms = 2,
            Rent = 750,
            Description = "Property description",
            Landlord = A.Fake<LandlordDbModel>()
        };
    }

    protected static IFormFile CreateExampleImage()
    {
        var stream = new MemoryStream();
        IFormFile fakeImage = new FormFile(stream, 0, 1, null!, "fakeImage.jpeg");
        return fakeImage;
    }
}