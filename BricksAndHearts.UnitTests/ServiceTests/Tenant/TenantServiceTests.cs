using System.Collections.Generic;
using System.Linq;
using BricksAndHearts.Database;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using FakeItEasy;
using FluentAssertions;
using Xunit;

namespace BricksAndHearts.UnitTests.ServiceTests.Tenant;

public class TenantServiceTests : IClassFixture<TestDatabaseFixture>
{
    public TenantServiceTests(TestDatabaseFixture fixture)
    {
        Fixture = fixture;
    }
    private TestDatabaseFixture Fixture { get; }

    #region TenantListTests

    [Fact]
    public async void FilterNearestTenantsToProperty_WithInvalidPostcodeAndMatching_ReturnsFilteredUnorderedTenants()
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new TenantService(context, A.Fake<IPostcodeService>());
        var filters = new HousingRequirementModel
        {
            AcceptsSingleTenant = true,
            AcceptsCouple = true,
            AcceptsFamily = true,
            AcceptsPets = true,
            AcceptsNotInEET = true,
            AcceptsCredit = true,
            AcceptsBenefits = true,
            AcceptsUnder35 = true
        };
        //Act
        var result = await service.FilterNearestTenantsToProperty(filters, true, "", 1, 10);

        //Assert
        result.TenantList.Should().BeOfType<List<TenantDbModel>>().Which.Count.Should().Be(context.Tenants.Count());
    }

    [Fact]
    public async void FilterNearestTenantsToProperty_WithInvalidPostcodeAndNoMatchingAndAllFilters_ReturnsNoTenants()
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new TenantService(context, A.Fake<IPostcodeService>());
        var filters = new HousingRequirementModel
        {
            AcceptsSingleTenant = true,
            AcceptsCouple = true,
            AcceptsFamily = true,
            AcceptsPets = true,
            AcceptsNotInEET = true,
            AcceptsCredit = true,
            AcceptsBenefits = true,
            AcceptsUnder35 = true
        };
        //Act
        var result = await service.FilterNearestTenantsToProperty(filters, false, "", 1, 10);

        //Assert
        result.TenantList.Should().BeOfType<List<TenantDbModel>>().Which.Count.Should().Be(0);
    }

    [Fact]
    public async void FilterNearestTenantsToProperty_WithValidPostcodeAndNoMatching_ReturnsClosestTenantFirst()
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new TenantService(context, A.Fake<IPostcodeService>());
        var filters = new HousingRequirementModel
        {
            AcceptsSingleTenant = true,
            AcceptsCouple = true,
            AcceptsFamily = true,
            AcceptsPets = false,
            AcceptsNotInEET = true,
            AcceptsCredit = false,
            AcceptsBenefits = false,
            AcceptsUnder35 = false
        };
        //Act
        var result = await service.FilterNearestTenantsToProperty(filters, false, "PE11BF", 1, 10);

        //Assert
        result.TenantList.Should().BeOfType<List<TenantDbModel>>().Which.First().Postcode.Should()
            .BeOfType<PostcodeDbModel>().Which.Postcode.Should().Be("PE1 1BF");
    }

    [Fact]
    public async void GetNearestTenantsToProperty_CalledWithFilters_ReturnsCorrectlyFilteredTenantListMaxLength5()
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new TenantService(context, A.Fake<IPostcodeService>());
        var property = new PropertyViewModel
        {
            Address = new AddressModel
            {
                Postcode = "CO105HU"
            },
            LandlordRequirements = new HousingRequirementModel
            {
                AcceptsSingleTenant = true,
                AcceptsCouple = false,
                AcceptsFamily = false,
                AcceptsPets = true,
                AcceptsNotInEET = true,
                AcceptsCredit = false,
                AcceptsBenefits = true,
                AcceptsUnder35 = true
            }
        };

        // Act
        var result = await service.GetNearestTenantsToProperty(property);

        // Assert
        result.TenantList.Should().BeOfType<List<TenantDbModel>>();
        result.TenantList.Count.Should().Be(context.Tenants.Where(t => t.UniversalCredit != true)
            .Count(t => t.Type == "Single"));
        result.Count.Should().BeLessThan(6);
    }

    [Fact]
    public async void GetNearestTenantsToProperty_CalledWithOnlyAcceptsSingle_ReturnsSingleTenants()
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new TenantService(context, A.Fake<IPostcodeService>());
        var property = new PropertyViewModel
        {
            Address = new AddressModel
            {
                Postcode = "CO105HU"
            },
            LandlordRequirements = new HousingRequirementModel
            {
                AcceptsSingleTenant = true,
                AcceptsCouple = false,
                AcceptsFamily = false,
                AcceptsPets = true,
                AcceptsNotInEET = true,
                AcceptsCredit = true,
                AcceptsBenefits = true,
                AcceptsUnder35 = true
            }
        };

        // Act
        var result = await service.GetNearestTenantsToProperty(property);

        // Assert
        result.TenantList.Should().BeOfType<List<TenantDbModel>>();
        result.TenantList.Count.Should().Be(context.Tenants.Count(t => t.Type == "Single"));
    }

    #endregion
}