using System.Collections.Generic;
using System.Linq;
using BricksAndHearts.Database;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BricksAndHearts.UnitTests.ServiceTests.Tenant;

public class TenantServiceTests : IClassFixture<TestDatabaseFixture>
{
    private TestDatabaseFixture Fixture { get; }

    public TenantServiceTests(TestDatabaseFixture fixture)
    {
        Fixture = fixture;
    }
    #region TenantListTests

    [Fact]
    public async void SortTenantsByLocationAndFilter_WithInvalidPostcodeAndMatching_ReturnsFilteredUnorderedTenants()
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
            AcceptsNotEET = true,
            AcceptsCredit = true,
            AcceptsBenefits = true,
            AcceptsOver35 = true
        };
        //Act
        var result = await (await service.SortTenantsByLocationAndFilter(filters, true,"",1,10))!.ToListAsync();
        
        //Assert
        result.Should().BeOfType<List<TenantDbModel>>().Which.Count.Should().Be(context.Tenants.Count());
    }
    
    [Fact]
    public async void SortTenantsByLocationAndFilter_WithInvalidPostcodeAndNoMatchingAndAllFilters_ReturnsNoTenants()
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
            AcceptsNotEET = true,
            AcceptsCredit = true,
            AcceptsBenefits = true,
            AcceptsOver35 = true
        };
        //Act
        var result = await (await service.SortTenantsByLocationAndFilter(filters, false,"",1,10))!.ToListAsync();
        
        //Assert
        result.Should().BeOfType<List<TenantDbModel>>().Which.Count.Should().Be(0);
    }
    
    [Fact]
    public async void SortTenantsByLocationAndFilter_WithValidPostcodeAndNoMatching_ReturnsClosestTenantFirst()
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
            AcceptsNotEET = false,
            AcceptsCredit = false,
            AcceptsBenefits = false,
            AcceptsOver35 = false
        };
        //Act
        var result = await (await service.SortTenantsByLocationAndFilter(filters, false,"PE11BF",1,10))!.ToListAsync();
        
        //Assert
        result.Should().BeOfType<List<TenantDbModel>>().Which.First().Postcode.Should().Be("PE1 1BF");
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
                AcceptsNotEET = true,
                AcceptsCredit = false,
                AcceptsBenefits = true,
                AcceptsOver35 = true
            }
        };

        // Act
        var result = await service.GetNearestTenantsToProperty(property);

        // Assert
        result.Should().BeOfType<List<TenantDbModel>>();
        result.Count.Should().Be(context.Tenants.Where(t => t.UniversalCredit != true)
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
                AcceptsNotEET = true,
                AcceptsCredit = true,
                AcceptsBenefits = true,
                AcceptsOver35 = true
            }
        };

        // Act
        var result = await service.GetNearestTenantsToProperty(property);

        // Assert
        result.Should().BeOfType<List<TenantDbModel>>();
        result.Count.Should().Be(context.Tenants.Count(t => t.Type =="Single"));
    }
    
    #endregion
}