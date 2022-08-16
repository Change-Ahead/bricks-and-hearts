using System.Collections.Generic;
using BricksAndHearts.Database;
using BricksAndHearts.ViewModels;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BricksAndHearts.UnitTests.ServiceTests.Tenant;

public class TenantControllerTests : TenantControllerTestsBase
{
    [Fact]
    public async void SortTenantsByLocation_CallsSortTenantsByLocation_AndReturnsViewWithTenantList()
    {
        // Arrange
        var postcode = "CB3 9AJ";
        var tenant = CreateTenant();
        List<TenantDbModel> tenantList = new()
        {
            tenant
        };

        A.CallTo(() => TenantService.SortTenantsByLocation(postcode, 1, 10)).Returns(tenantList);

        // Act
        var result = await UnderTest.SortTenantsByLocation(postcode) as ViewResult;

        // Assert
        A.CallTo(() => TenantService.SortTenantsByLocation(postcode, 1, 10)).MustHaveHappened();
        result!.ViewData.Model.Should().BeOfType<TenantListModel>();
    }
}