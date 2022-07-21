﻿using System;
using BricksAndHearts.Controllers;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;

namespace BricksAndHearts.UnitTests.ControllerTests.Landlord;

public class LandlordControllerTestsBase : ControllerTestsBase
{
    protected readonly LandlordController _underTest = new(null!, null!, new LandlordService(null!),
        new PropertyService(null!));

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