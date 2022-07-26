using System;
using BricksAndHearts.Controllers;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using FakeItEasy;
using Microsoft.Extensions.Logging;

namespace BricksAndHearts.UnitTests.ControllerTests.Landlord;

public class LandlordControllerTestsBase : ControllerTestsBase
{
    protected readonly IPropertyService propertyService;
    protected readonly ILandlordService landlordService;
    protected readonly Logger<LandlordController> logger;
    protected readonly MailService mailService;
    protected readonly LandlordController _underTest;

    protected LandlordControllerTestsBase()
    {
        propertyService = A.Fake<IPropertyService>();
        landlordService = A.Fake<ILandlordService>();
        logger = A.Fake<Logger<LandlordController>>();
        mailService = A.Fake<MailService>();
        _underTest = new LandlordController(logger, landlordService, propertyService, mailService);
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

    protected LandlordProfileModel CreateTestLandlordProfileModel()
    {
        return new LandlordProfileModel()
        {
            Id = 1,
            Title = "Mr",
            FirstName = "John",
            LastName = "Doe",
            CompanyName = "John Doe",
            Email = "test.email@gmail.com",
            CharterApproved = false,
            LandlordStatus = "some data",
            LandlordProvidedCharterStatus = false
        };
    }
}