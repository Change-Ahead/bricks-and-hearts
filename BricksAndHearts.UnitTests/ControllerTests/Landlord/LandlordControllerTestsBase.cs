using System;
using BricksAndHearts.Controllers;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;

namespace BricksAndHearts.UnitTests.ControllerTests.Landlord;

public class LandlordControllerTestsBase : ControllerTestsBase
{
    protected readonly IPropertyService PropertyService;
    protected readonly ILandlordService LandlordService;
    protected readonly Logger<LandlordController> Logger;
    protected readonly MailService MailService;
    protected readonly LandlordController UnderTest;

    protected LandlordControllerTestsBase()
    {
        PropertyService = A.Fake<IPropertyService>();
        LandlordService = A.Fake<ILandlordService>();
        Logger = A.Fake<Logger<LandlordController>>();
        MailService = A.Fake<MailService>();
        var httpContext = new DefaultHttpContext();
        var tempData = new TempDataDictionary(httpContext, A.Fake<ITempDataProvider>());
        UnderTest = new LandlordController(Logger, LandlordService, PropertyService, MailService){TempData = tempData};
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
            LandlordId = 1,
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