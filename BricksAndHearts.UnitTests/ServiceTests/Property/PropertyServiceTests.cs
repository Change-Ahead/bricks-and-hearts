using System.Linq;
using BricksAndHearts.Database;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using FluentAssertions;
using Xunit;

namespace BricksAndHearts.UnitTests.ServiceTests.Property;

public class PropertyServiceTests : PropertyServiceTestsBase
{
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
        var service = new PropertyService(context);

        // Act
        var propertiesByLandlord = service.GetPropertiesByLandlord(landlordId);

        // Assert
        propertiesByLandlord.Should().OnlyContain(u => u.LandlordId == landlordId);
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
        var service = new PropertyService(context);
        var propertiesBeforeCount = context.Properties.Count();
        var createModel = new PropertyViewModel
        {
            Address = new PropertyAddress
            {
                AddressLine1 = "AddNewProperty_AddsNewProperty",
                County = "Cambridgeshire",
                TownOrCity = "Cambridge",
                Postcode = "CB1 1DX"
            }
        };

        // Act
        service.AddNewProperty(landlordId, createModel);
        context.ChangeTracker.Clear();

        // Assert
        var property = context.Properties.OrderBy(u => u.CreationTime).Last();
        property.AddressLine1.Should().Be("AddNewProperty_AddsNewProperty");
        property.IsIncomplete.Should().BeTrue();
        property.LandlordId.Should().Be(landlordId);
        context.Properties.Count().Should().Be(propertiesBeforeCount + 1);
    }

    #endregion

    #region UpdateProperty

    [Fact]
    public async void UpdateProperty_UpdatesProperty()
    {
        // Arrange
        await using var context = Fixture.CreateWriteContext();
        var service = new PropertyService(context);
        context.Properties.Single(u => u.Id == 1).AcceptsBenefits = true;
        var propertiesBeforeCount = context.Properties.Count();
        var updateModel = new PropertyViewModel
        {
            Address = new PropertyAddress
            {
                AddressLine1 = "UpdateProperty_UpdatesProperty1",
                County = "Cambridgeshire",
                TownOrCity = "Cambridge",
                Postcode = "CB1 1DX"
            },
            AcceptsBenefits = false
        };

        // Act
        service.UpdateProperty(1, updateModel, false);
        context.ChangeTracker.Clear();

        // Assert
        var property = context.Properties.Single(u => u.Id == 1);
        property.AddressLine1.Should().Be("UpdateProperty_UpdatesProperty1");
        property.IsIncomplete.Should().BeFalse();
        property.AcceptsBenefits.Should().BeFalse();
        context.Properties.Count().Should().Be(propertiesBeforeCount);
    }

    #endregion

    #region DeleteProperty

    [Fact]
    public async void DeleteProperty_DeletesProperty()
    {
        // Arrange
        await using var context = Fixture.CreateWriteContext();
        var service = new PropertyService(context);
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

    #region CountProperties()

    [Fact]
    public async void CountProperties_ReturnsPropertyCountModel_WithCorrectData()
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new PropertyService(context);

        // Act
        var result = service.CountProperties();

        // Assert
        result.Should().BeOfType<PropertyCountModel>();
        var registeredCount = context.Properties.Count();
        result.RegisteredProperties.Should().Be(registeredCount);
        var liveCount =
            context.Properties.Count(p => p.Availability != PropertyDbModel.Avail_Draft && p.Landlord.CharterApproved);
        result.LiveProperties.Should().Be(liveCount);
        var availableCount = context.Properties.Count(p => p.Availability == PropertyDbModel.Avail_Available);
        result.AvailableProperties.Should().Be(availableCount);
    }

    #endregion

    #region IsUserAdminOrCorrectLandlord

    [Fact]
    public async void IsUserAdminOrCorrectLandlord_UsedByAdmin_ReturnsTrueIfPropertyIsntTheirs()
    {
        // Arrange
        await using var context = Fixture.CreateWriteContext();
        var service = new PropertyService(context);
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
        var service = new PropertyService(context);
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
        var service = new PropertyService(context);
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
        var service = new PropertyService(context);
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
        var service = new PropertyService(context);

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
        var service = new PropertyService(context);
        var propertyId = context.Properties.Count() + 1; //One more than highest propertyID

        // Act
        var property = service.GetPropertyByPropertyId(propertyId);

        // Assert
        property.Should().BeNull();
    }

    #endregion
}