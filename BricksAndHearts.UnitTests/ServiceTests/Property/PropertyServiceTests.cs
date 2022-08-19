using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BricksAndHearts.Database;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BricksAndHearts.UnitTests.ServiceTests.Property;

public class PropertyServiceTests : PropertyServiceTestsBase
{
    private IPostcodeService _postcodeService = A.Fake<IPostcodeService>();

    public PropertyServiceTests(TestDatabaseFixture fixture)
    {
        Fixture = fixture;
    }

    private TestDatabaseFixture Fixture { get; }

    #region GetPropertiesByLandlord

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async void GetPropertiesByLandlord_OnlyGetsPropertiesFromInputLandlord(int landlordId)
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new PropertyService(context, null!);

        // Act
        var propertiesByLandlord = await service.GetPropertiesByLandlord(landlordId, 1, 10);

        // Assert
        propertiesByLandlord.PropertyList.Should().OnlyContain(u => u.LandlordId == landlordId);
    }

    #endregion

    #region AddNewProperty

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async void AddNewProperty_AddsNewProperty(int landlordId)
    {
        // Arrange
        await using var context = Fixture.CreateWriteContext();
        var service = new PropertyService(context, _postcodeService);
        var propertiesBeforeCount = context.Properties.Count();
        var createModel = new PropertyViewModel
        {
            Address = new AddressModel
            {
                AddressLine1 = "AddNewProperty_AddsNewProperty",
                County = "Cambridgeshire",
                TownOrCity = "Cambridge",
                Postcode = "CB1 1DX"
            }
        };
        A.CallTo(() => _postcodeService.GetPostcodeAndAddIfAbsent(A<string>.Ignored))!
            .Returns(Task.FromResult(TestDatabaseFixture.Postcodes["CB1 1DX"]));

        // Act
        await service.AddNewProperty(landlordId, createModel);
        context.ChangeTracker.Clear();

        // Assert
        var property = context.Properties.OrderBy(u => u.CreationTime).Last();
        property.AddressLine1.Should().Be("AddNewProperty_AddsNewProperty");
        property.IsIncomplete.Should().BeTrue();
        property.LandlordId.Should().Be(landlordId);
        context.Properties.Count().Should().Be(propertiesBeforeCount + 1);
    }

    #endregion

    #region DeleteProperty

    [Fact]
    public async void DeleteProperty_DeletesProperty()
    {
        // Arrange
        await using var context = Fixture.CreateWriteContext();
        var service = new PropertyService(context, null!);
        var propertiesBeforeCount = context.Properties.Count();
        var deleteModel = context.Properties.Single(u => u.Id == 1);

        // Act
        service.DeleteProperty(deleteModel);
        context.ChangeTracker.Clear();

        // Assert
        context.Properties.Count(u => u.Id == 1).Should().Be(0);
        context.Properties.Count().Should().Be(propertiesBeforeCount - 1);
    }

    #endregion


    #region UpdateProperty

    [Fact]
    public async void UpdateProperty_UpdatesProperty()
    {
        // Arrange
        await using var context = Fixture.CreateWriteContext();
        A.CallTo(() => _postcodeService.GetPostcodeAndAddIfAbsent(A<string>.Ignored))!
            .Returns(Task.FromResult(TestDatabaseFixture.Postcodes["CB1 1DX"]));
        var service = new PropertyService(context, _postcodeService);
        context.Properties.Single(u => u.Id == 1).AcceptsBenefits = true;
        var propertiesBeforeCount = context.Properties.Count();
        var updateModel = new PropertyViewModel
        {
            Address = new AddressModel
            {
                AddressLine1 = "UpdateProperty_UpdatesProperty1",
                County = "Cambridgeshire",
                TownOrCity = "Cambridge",
                Postcode = "CB1 1DX"
            },
            LandlordRequirements = new HousingRequirementModel
            {
                AcceptsBenefits = false
            }
        };

        // Act
        await service.UpdateProperty(1, updateModel, false);
        context.ChangeTracker.Clear();

        // Assert
        var property = context.Properties.Single(u => u.Id == 1);
        property.AddressLine1.Should().Be("UpdateProperty_UpdatesProperty1");
        property.IsIncomplete.Should().BeFalse();
        property.AcceptsBenefits.Should().BeFalse();
        context.Properties.Count().Should().Be(propertiesBeforeCount);
    }

    [Fact]
    public async Task UpdateProperty_SetsStateToOccupied_IfAllUnitsOccupied()
    {
        // Arrange
        await using var context = Fixture.CreateWriteContext();
        A.CallTo(() => _postcodeService.GetPostcodeAndAddIfAbsent(A<string>.Ignored))!
            .Returns(Task.FromResult<PostcodeDbModel?>(null));
        var service = new PropertyService(context, _postcodeService);

        var propertyDb = context.Properties.Single(p => p.AddressLine1 == "MultiUnit Property");
        var propertyUpdate = new PropertyViewModel { OccupiedUnits = propertyDb.TotalUnits };

        // Act
        await service.UpdateProperty(propertyDb.Id, propertyUpdate, isIncomplete: false);
        context.ChangeTracker.Clear();

        // Assert
        propertyDb = context.Properties.Single(p => p.AddressLine1 == "MultiUnit Property");
        propertyDb.Availability.Should().Be(AvailabilityState.Occupied);
        propertyDb.OccupiedUnits.Should().Be(propertyDb.TotalUnits);
    }

    [Fact]
    public async Task UpdateProperty_Fails_OccupiedGreaterThanTotal()
    {
        // Arrange
        await using var context = Fixture.CreateWriteContext();
        A.CallTo(() => _postcodeService.GetPostcodeAndAddIfAbsent(A<string>.Ignored))!
            .Returns(Task.FromResult<PostcodeDbModel?>(null));
        var service = new PropertyService(context, _postcodeService);

        var propertyDb = context.Properties.Single(p => p.AddressLine1 == "MultiUnit Property");
        var propertyUpdate = new PropertyViewModel { OccupiedUnits = propertyDb.TotalUnits + 1 };

        // Act
        var act = async () => await service.UpdateProperty(propertyDb.Id, propertyUpdate, isIncomplete: false);
        context.ChangeTracker.Clear();

        // Assert
        (await act.Should().ThrowAsync<DbUpdateException>()).WithInnerException<SqlException>()
            .WithMessage("The UPDATE statement conflicted with the CHECK constraint \"OccupiedUnits\".*");
    }

    [Fact]
    public async Task UpdateProperty_UpdatesAvailableFromDate_WhenAvailableSoon()
    {
        // Arrange
        await using var context = Fixture.CreateWriteContext();
        A.CallTo(() => _postcodeService.GetPostcodeAndAddIfAbsent(A<string>.Ignored))!
            .Returns(Task.FromResult<PostcodeDbModel?>(null));
        var service = new PropertyService(context, _postcodeService);

        var date = DateTime.Now;
        var propertyDb = context.Properties.Single(p => p.AddressLine1 == "MultiUnit Property");
        var propertyUpdate = new PropertyViewModel
        {
            Availability = AvailabilityState.AvailableSoon,
            AvailableFrom = date
        };

        // Act
        await service.UpdateProperty(propertyDb.Id, propertyUpdate, isIncomplete: false);
        context.ChangeTracker.Clear();

        // Assert
        propertyDb = context.Properties.Single(p => p.AddressLine1 == "MultiUnit Property");
        propertyDb.Availability.Should().Be(AvailabilityState.AvailableSoon);
        propertyDb.AvailableFrom.Should().Be(date);
    }

    [Fact]
    public async Task UpdateProperty_DoesntChangeAvailableFromDate_WhenOccupiedStateOverrides()
    {
        // Arrange
        await using var context = Fixture.CreateWriteContext();
        A.CallTo(() => _postcodeService.GetPostcodeAndAddIfAbsent(A<string>.Ignored))!
            .Returns(Task.FromResult<PostcodeDbModel?>(null));
        var service = new PropertyService(context, _postcodeService);

        var date = DateTime.Now;
        var propertyDb = context.Properties.Single(p => p.AddressLine1 == "AvailableSoon Property");
        var propertyUpdate = new PropertyViewModel
        {
            AvailableFrom = date
        };

        // Act
        await service.UpdateProperty(propertyDb.Id, propertyUpdate, isIncomplete: false);
        context.ChangeTracker.Clear();

        // Assert
        propertyDb = context.Properties.Single(p => p.AddressLine1 == "AvailableSoon Property");
        propertyDb.Availability.Should().Be(AvailabilityState.AvailableSoon);
        propertyDb.AvailableFrom.Should().NotBe(date).And.Be(DateTime.MinValue);
    }

    [Fact]
    public async Task UpdateProperty_SetsAvailableFromDateToNull_WhenOccupiedStateOverrides()
    {
        // Arrange
        await using var context = Fixture.CreateWriteContext();
        A.CallTo(() => _postcodeService.GetPostcodeAndAddIfAbsent(A<string>.Ignored))!
            .Returns(Task.FromResult<PostcodeDbModel?>(null));
        var service = new PropertyService(context, _postcodeService);

        var date = DateTime.Now;
        var propertyDb = context.Properties.Single(p => p.AddressLine1 == "MultiUnit Property");
        var propertyUpdate = new PropertyViewModel
        {
            OccupiedUnits = propertyDb.TotalUnits,
            Availability = AvailabilityState.AvailableSoon,
            AvailableFrom = date
        };

        // Act
        await service.UpdateProperty(propertyDb.Id, propertyUpdate, isIncomplete: false);
        context.ChangeTracker.Clear();

        // Assert
        propertyDb = context.Properties.Single(p => p.AddressLine1 == "MultiUnit Property");
        propertyDb.Availability.Should().Be(AvailabilityState.Occupied);
        propertyDb.AvailableFrom.Should().BeNull();
    }

    #endregion

    #region CountProperties()

    [Fact]
    public async void CountProperties_ReturnsPropertyCountModel_WithCorrectData()
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new PropertyService(context, null!);

        // Act
        var result = service.CountProperties();

        // Assert
        result.Should().BeOfType<PropertyCountModel>();
        var registeredCount = context.Properties.Count();
        result.RegisteredProperties.Should().Be(registeredCount);
        var liveCount =
            context.Properties.Count(p => p.Availability != AvailabilityState.Draft && p.Landlord.CharterApproved);
        result.LiveProperties.Should().Be(liveCount);
        var availableCount = context.Properties.Count(p => p.Availability == AvailabilityState.Available);
        result.AvailableProperties.Should().Be(availableCount);
    }

    #endregion

    #region IsUserAdminOrCorrectLandlord

    [Fact]
    public async void IsUserAdminOrCorrectLandlord_UsedByAdmin_ReturnsTrueIfPropertyIsntTheirs()
    {
        // Arrange
        await using var context = Fixture.CreateWriteContext();
        var service = new PropertyService(context, null!);
        var adminUser = CreateAdminUser();
        adminUser.LandlordId = 50;

        context.Properties.First(u => u.Id == 1).LandlordId = 49;

        // Act
        var allowed = service.IsUserAdminOrCorrectLandlord(adminUser, 1);

        // Assert
        allowed.Should().BeTrue();
    }

    [Fact]
    public async void IsUserAdminOrCorrectLandlord_UsedByAdmin_ReturnsTrueIfPropertyIsTheirs()
    {
        // Arrange
        await using var context = Fixture.CreateWriteContext();
        var service = new PropertyService(context, null!);
        var adminUser = CreateAdminUser();
        adminUser.LandlordId = 50;

        context.Properties.First(u => u.Id == 1).LandlordId = 50;

        // Act
        var allowed = service.IsUserAdminOrCorrectLandlord(adminUser, 1);
        context.ChangeTracker.Clear();

        // Assert
        allowed.Should().BeTrue();
    }

    [Fact]
    public async void IsUserAdminOrCorrectLandlord_UsedByLandLord_ReturnsTrueIfPropertyIsTheirs()
    {
        // Arrange
        await using var context = Fixture.CreateWriteContext();
        var service = new PropertyService(context, null!);
        var landlordUser = CreateLandlordUser();
        landlordUser.LandlordId = 50;

        context.Properties.First(u => u.Id == 1).LandlordId = 50;

        // Act
        var allowed = service.IsUserAdminOrCorrectLandlord(landlordUser, 1);
        context.ChangeTracker.Clear();

        // Assert
        allowed.Should().BeTrue();
    }

    [Fact]
    public async void IsUserAdminOrCorrectLandlord_UsedByLandLord_ReturnsFalseIfPropertyIsNotTheirs()
    {
        // Arrange
        await using var context = Fixture.CreateWriteContext();
        var service = new PropertyService(context, null!);
        var landlordUser = CreateLandlordUser();
        landlordUser.LandlordId = 50;

        context.Properties.First(u => u.Id == 1).LandlordId = 49;

        // Act
        var allowed = service.IsUserAdminOrCorrectLandlord(landlordUser, 1);
        context.ChangeTracker.Clear();

        // Assert
        allowed.Should().BeFalse();
    }

    #endregion

    #region GetPropertyByPropertyID

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async void GetPropertyByPropertyId_GetsPropertyWithId(int propertyId)
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new PropertyService(context, null!);

        // Act
        var property = service.GetPropertyByPropertyId(propertyId)!;

        // Assert
        property.Should().NotBeNull();
        property.Id.Should().Be(propertyId);
    }

    [Fact]
    public async void GetPropertyByPropertyIdReturnsNullIfPropertyWithIdDoesntExist()
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new PropertyService(context, null!);
        var propertyId = context.Properties.Count() + 1; //One more than highest propertyID

        // Act
        var property = service.GetPropertyByPropertyId(propertyId);

        // Assert
        property.Should().BeNull();
    }

    #endregion

    [Fact]
    public void CreateNewPublicViewLink_UpdateDatabase()
    {
        // Arrange
        using var context = Fixture.CreateWriteContext();
        var service = new PropertyService(context, null!);

        // Act
        var result = service.CreateNewPublicViewLink(1);

        // Assert
        result.Should().NotBeNullOrEmpty();
        context.Properties.Single(p => p.Id == 1).PublicViewLink.Should().Be(result);
    }
    
    [Fact]
    public async void SortPropertiesByLocation_WhenCalledWithInvalidPostcode_ReturnsEmptyList()
    {
        // Arrange
        var logger = A.Fake<ILogger<PostcodeService>>();
        var messageHandler = A.Fake<HttpMessageHandler>();
        const string postcode = "eeeeee";
        var responseBody = string.Empty;
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.NotFound,
            Content = new StringContent(responseBody)
        };
        // Slightly icky because "SendAsync" is protected
        A.CallTo(messageHandler).Where(c => c.Method.Name == "SendAsync").WithReturnType<Task<HttpResponseMessage>>().Returns(response);
        var httpClient = new HttpClient(messageHandler);
        var postcodeApiService = new PostcodeService(logger, null!, httpClient);
        var service = new PropertyService(null!, postcodeApiService);

        // Act
        var result = await service.GetPropertyList("Location", postcode, 1, 10);
        
        // Assert
        result.PropertyList.Count.Should().Be(0);
        result.Count.Should().Be(0);
    }

    [Fact]
    public async void SortPropertiesByLocation_WhenCalledWithValidPostcode_ReturnPropertiesSorted()
    {
        // Arrange
        await using var context = Fixture.CreateWriteContext();
        var logger = A.Fake<ILogger<PostcodeService>>();
        var messageHandler = A.Fake<HttpMessageHandler>();
        const string postcode = "eh11ad";
        var responseBody = await File.ReadAllTextAsync($"{AppDomain.CurrentDomain.BaseDirectory}/../../../ServiceTests/Api/PostcodeioApiBulkResponse.json");
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(responseBody)
        };
        A.CallTo(messageHandler).Where(c => c.Method.Name == "SendAsync").WithReturnType<Task<HttpResponseMessage>>().Returns(response);
        var httpClient = new HttpClient(messageHandler);
        var postcodeApiService = new PostcodeService(logger, context, httpClient);
        var service = new PropertyService(context, postcodeApiService);

        // Act
        var result = await service.GetPropertyList("Location", postcode, 1, 10);

        // Assert
        var correctList = new List<PropertyDbModel>
        {
            context.Properties.Single(p => p.TownOrCity == "Leeds"),
            context.Properties.Single(p => p.TownOrCity == "Peterborough"),
            context.Properties.Single(p => p.TownOrCity == "London"),
            context.Properties.Single(p => p.TownOrCity == "Brighton"),
        };
        var propertyDbModelList = result.PropertyList.FindAll(p => p.LandlordId == 3);
        propertyDbModelList.Should().BeEquivalentTo(correctList, options => options.WithStrictOrdering());
    }
}