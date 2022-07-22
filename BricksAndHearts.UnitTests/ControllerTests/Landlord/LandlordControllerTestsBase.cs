using System;
using BricksAndHearts.Controllers;
using BricksAndHearts.Database;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using FakeItEasy;

namespace BricksAndHearts.UnitTests.ControllerTests.Landlord;

public class LandlordControllerTestsBase : ControllerTestsBase
{
    protected readonly IPropertyService PropertyService;
    protected readonly ILandlordService LandlordService;
    protected readonly LandlordController UnderTest;

    protected LandlordControllerTestsBase()
    {
        LandlordService = A.Fake<ILandlordService>();
        PropertyService = A.Fake<IPropertyService>();
        UnderTest = new LandlordController(null!, LandlordService, PropertyService, null!);
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

    protected LandlordDbModel CreateTestLandlordDbModel()
    {
        return new LandlordDbModel()
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