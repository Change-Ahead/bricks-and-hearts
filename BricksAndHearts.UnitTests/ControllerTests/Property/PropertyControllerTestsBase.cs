using System;
using System.IO;
using BricksAndHearts.Controllers;
using BricksAndHearts.Database;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BricksAndHearts.UnitTests.ControllerTests.Property;

public class PropertyControllerTestsBase : ControllerTestsBase
{
    protected readonly IPropertyService PropertyService;
    protected readonly IAzureMapsApiService AzureMapsApiService;
    protected readonly IAzureStorage AzureStorage;
    protected readonly PropertyController UnderTest;
    protected readonly ILogger<PropertyController> Logger;

    protected PropertyControllerTestsBase()
    {
        PropertyService = A.Fake<IPropertyService>();
        AzureMapsApiService = A.Fake<IAzureMapsApiService>();
        AzureStorage = A.Fake<IAzureStorage>();
        Logger = A.Fake<ILogger<PropertyController>>();
        UnderTest = new PropertyController(PropertyService, AzureMapsApiService, Logger, AzureStorage);
    }

    protected PropertyViewModel CreateExamplePropertyViewModel()
    {
        return new PropertyViewModel
        {
            Address = new PropertyAddress
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
            CreationTime = DateTime.Now
        };
    }

    protected PropertyDbModel CreateExamplePropertyDbModel()
    {
        return new PropertyDbModel
        {
            Id = 1,
            AddressLine1 = "10 Downing Street",
            Postcode = "SW1A 2AA",
            NumOfBedrooms = 2,
            Rent = 750,
            Description = "Property description"
        };
    }

    protected IFormFile CreateExampleImage()
    {
        var stream = new MemoryStream();
        IFormFile fakeImage = new FormFile(stream, 0, 1, null!, "fakeImage.jpeg");
        return fakeImage;
    }
}
