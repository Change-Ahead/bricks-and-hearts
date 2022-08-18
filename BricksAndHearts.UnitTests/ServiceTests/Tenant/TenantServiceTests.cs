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
    private TestDatabaseFixture Fixture { get; }

    public TenantServiceTests(TestDatabaseFixture fixture)
    {
        Fixture = fixture;
    }
    #region TenantListTests

    [Fact]
    public async void GetTenantList_CalledWithNoFilter_ReturnsAllTenants()
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new TenantService(context, A.Fake<IPostcodeService>());
        var tenantListModel = new TenantListModel();

        // Act
        var result = await service.GetTenantList(tenantListModel.Filter);

        // Assert
        result.Should().BeOfType<List<TenantDbModel>>();
        result.Count.Should().Be(context.Tenants.Count());
    }

    [Fact]
    public async void GetTenantList_CalledWithFilters_ReturnsCorrectlyFilteredTenantList()
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new TenantService(context, A.Fake<IPostcodeService>());
        var filter = new HousingRequirementModel
        {
            AcceptsSingleTenant = true,
            AcceptsNotEET = true,
            AcceptsCredit = true
        };

        // Act
        var result = await service.GetTenantList(filter);

        // Assert
        result.Should().BeOfType<List<TenantDbModel>>();
        result.Count.Should().Be(context.Tenants.Where(t => t.ETT == true)
            .Where(t => t.UniversalCredit == true)
            .Count(t => t.Type == "Single"));
    }

    [Fact]
    public async void GetNearestTenantsToProperty_CalledWithFilters_ReturnsCorrectlyFilteredTenantListMaxLength5()
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new TenantService(context, A.Fake<IPostcodeService>());
        var filter = new HousingRequirementModel
        {
            AcceptsSingleTenant = true,
            AcceptsNotEET = true,
            AcceptsCredit = false
        };

        // Act
        var result = await service.GetTenantList(filter);

        // Assert
        result.Should().BeOfType<List<TenantDbModel>>();
        result.Count.Should().Be(context.Tenants.Where(t => t.ETT == true)
            .Where(t => t.UniversalCredit != true)
            .Count(t => t.Type == "Single"));
        result.Count.Should().BeLessThan(6);
    }
    
    [Fact]
    public async void GetNearestTenantsToProperty_CalledWithAllFalseFilters_ReturnsNoTenants()
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new TenantService(context, A.Fake<IPostcodeService>());
        var filter = new HousingRequirementModel
        {
            AcceptsSingleTenant = false,
            AcceptsCouple = false,
            AcceptsFamily = false,
            AcceptsPets = false,
            AcceptsNotEET = false,
            AcceptsCredit = false,
            AcceptsBenefits = false,
            AcceptsOver35 = false
        };

        // Act
        var result = await service.GetTenantList(filter);

        // Assert
        result.Should().BeOfType<List<TenantDbModel>>();
        result.Count.Should().Be(0);
    }
    
    #endregion
}