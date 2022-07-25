using System;
using BricksAndHearts.Controllers;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using FakeItEasy;

namespace BricksAndHearts.UnitTests.ControllerTests.Property;

public class PropertyControllerTestsBase : ControllerTestsBase
{
    public static readonly IPropertyService fakePropertyService = A.Fake<IPropertyService>();
    public static IApiService fakeApiService = A.Fake<IApiService>();
    protected readonly PropertyController _underTest = new(fakePropertyService, fakeApiService,null!);

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
}
