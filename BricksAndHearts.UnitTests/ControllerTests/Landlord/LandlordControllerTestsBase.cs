using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BricksAndHearts.Controllers;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;

namespace BricksAndHearts.UnitTests.ControllerTests.Landlord;

public class LandlordControllerTestsBase : ControllerTestsBase
{
    protected readonly ILandlordService LandlordService;
    protected readonly ILogger<LandlordController> Logger;
    protected readonly IMailService MailService;
    protected readonly IPropertyService PropertyService;
    protected IEnumerable<string>? FlashMessages => UnderTest.TempData["FlashMessages"] as List<string>;
    protected readonly LandlordController UnderTest;

    protected LandlordControllerTestsBase()
    {
        Logger = A.Fake<ILogger<LandlordController>>();
        LandlordService = A.Fake<ILandlordService>();
        MailService = A.Fake<IMailService>();
        PropertyService = A.Fake<IPropertyService>();
        MailService = A.Fake<IMailService>();
        var httpContext = new DefaultHttpContext();
        var tempData = new TempDataDictionary(httpContext, A.Fake<ITempDataProvider>());
        UnderTest = new LandlordController(Logger, LandlordService, PropertyService, MailService)
            { TempData = tempData };
        // Fixes NullReferenceException when calling TryValidateModel()
        UnderTest.ObjectValidator = new CustomObjectValidator();
    }
    
    protected PropertyViewModel CreateExamplePropertyViewModel()
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
                Postcode = "CB21LA"
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
        return new LandlordProfileModel
        {
            LandlordId = 1,
            Title = "Mr",
            FirstName = "John",
            LastName = "Doe",
            CompanyName = "John Doe",
            Email = "test.email@gmail.com",
            CharterApproved = false,
            LandlordType = "some data",
            Address = new AddressModel
            {
                AddressLine1 = "Adr1",
                AddressLine2 = "Adr2",
                AddressLine3 = "Adr3",
                TownOrCity = "City",
                County = "County",
                Postcode = "CB21LA"
            }
        };
    }

    protected LandlordProfileModel CreateInvalidLandlordProfileModel()
    {
        return new LandlordProfileModel()
        {
            LandlordId = 1000,
            Title = "MRMRMRMRMRMRMRMRMRMRMRMRMRMRMRMRMRMRMRMRMRMRMRMRMRfiftyMRMRMRMRMRMRMRMRMRMRseventy",
            LastName = "Doe",
            CompanyName = "John Doe",
            CharterApproved = false,
            LandlordType = "some data",
            Address = new AddressModel
            {
                AddressLine1 = "Adr1",
                AddressLine2 = "Adr2",
                AddressLine3 = "Adr3",
                TownOrCity = "City",
                County = "County",
                Postcode = "CB21LA"
            }
        };
    }

    public class CustomObjectValidator : IObjectModelValidator
    {
        public void Validate(ActionContext actionContext, ValidationStateDictionary? validationState, string prefix,
            object? model)
        {
            var context = new ValidationContext(model!, serviceProvider: null, items: null);
            var results = new List<ValidationResult>();

            bool isValid = Validator.TryValidateObject(
                model!, context, results,
                validateAllProperties: true
            );

            if (!isValid)
                results.ForEach(r =>
                {
                    // Add validation errors to the ModelState
                    actionContext.ModelState.AddModelError("", r.ErrorMessage!);
                });
        }
    }
}