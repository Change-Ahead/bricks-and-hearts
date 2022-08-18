using System;
using System.Collections.Generic;
using System.Linq;
using BricksAndHearts.Auth;
using BricksAndHearts.Database;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BricksAndHearts.UnitTests.ServiceTests.Admin;

public class AdminServiceTests : IClassFixture<TestDatabaseFixture>
{
    private TestDatabaseFixture Fixture { get; }

    public AdminServiceTests(TestDatabaseFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public void RequestAdminAccess_SetsHasRequestedAdminToTrue_ForCorrectUser()
    {
        // Arrange
        using var context = Fixture.CreateWriteContext();
        var service = new AdminService(context, A.Fake<ILogger<AdminService>>(), A.Fake<ITenantService>());

        var nonAdminUser = context.Users.Single(u => u.GoogleUserName == "NonAdminUser");
        var adminUser = context.Users.Single(u => u.GoogleUserName == "AdminUser");

        // Act
        service.RequestAdminAccess(new BricksAndHeartsUser(nonAdminUser, null!, null!));

        // Before assert we need to clear the context's change tracker so that the following database queries actually
        // query the database, as if this were a new context. This should be done for all write tests.
        context.ChangeTracker.Clear();

        // Assert
        context.Users.Single(u => u.Id == nonAdminUser.Id).HasRequestedAdmin.Should().BeTrue();
        context.Users.Single(u => u.Id == nonAdminUser.Id).IsAdmin.Should().BeFalse();
        context.Users.Single(u => u.Id == adminUser.Id).HasRequestedAdmin.Should().BeFalse();
    }

    [Fact]
    public void RequestAdminAccess_FirstUserToRequestAdminAccess_Granted()
    {
        // Arrange
        using var context = Fixture.CreateWriteContext();
        var service = new AdminService(context, A.Fake<ILogger<AdminService>>(), A.Fake<ITenantService>());

        var nonAdminUser = context.Users.Single(u => u.GoogleUserName == "NonAdminUser");
        context.Users.Where(u => u.IsAdmin).ToList().ForEach(u => u.IsAdmin = false);
        context.SaveChanges();
        context.ChangeTracker.Clear();

        // Act
        service.RequestAdminAccess(new BricksAndHeartsUser(nonAdminUser, null!, null!));

        // Before assert we need to clear the context's change tracker so that the following database queries actually
        // query the database, as if this were a new context. This should be done for all write tests.
        context.ChangeTracker.Clear();

        // Assert
        nonAdminUser = context.Users.Single(u => u.Id == nonAdminUser.Id);
        nonAdminUser.HasRequestedAdmin.Should().BeFalse();
        nonAdminUser.IsAdmin.Should().BeTrue();
    }

    [Fact]
    public void CancelAdminAccessRequest_SetsHasRequestedAdminToFalse_ForCorrectUser()
    {
        // Arrange
        using var context = Fixture.CreateWriteContext();
        var service = new AdminService(context, A.Fake<ILogger<AdminService>>(), A.Fake<ITenantService>());

        var requestedAdminUser = context.Users.Single(u => u.GoogleUserName == "NonAdminUser");

        // Act
        service.CancelAdminAccessRequest(new BricksAndHeartsUser(requestedAdminUser, null!, null!));

        context.ChangeTracker.Clear();

        // Assert
        context.Users.Single(u => u.Id == requestedAdminUser.Id).HasRequestedAdmin.Should().BeFalse();
    }

    [Fact]
    public void ApproveAdminAccessRequest_SetsIsAdminToTrueAndHasRequestedAdminAccessToFalse_ForCorrectUser()
    {
        // Arrange
        using var context = Fixture.CreateWriteContext();
        var service = new AdminService(context, A.Fake<ILogger<AdminService>>(), A.Fake<ITenantService>());

        var nonAdminUser = context.Users.Single(u => u.GoogleUserName == "NonAdminUser");

        // Act
        service.ApproveAdminAccessRequest(nonAdminUser.Id);

        context.ChangeTracker.Clear();

        // Assert
        context.Users.Single(u => u.Id == nonAdminUser.Id).IsAdmin.Should().BeTrue();
        context.Users.Single(u => u.Id == nonAdminUser.Id).HasRequestedAdmin.Should().BeFalse();
    }

    [Fact]
    public void RejectAdminAccessRequest_SetsHasRequestedAdminAccessToFalse_ForCorrectUser()
    {
        // Arrange
        using var context = Fixture.CreateWriteContext();
        var service = new AdminService(context, A.Fake<ILogger<AdminService>>(), A.Fake<ITenantService>());

        var nonAdminUser = context.Users.Single(u => u.GoogleUserName == "NonAdminUser");

        // Act
        service.ApproveAdminAccessRequest(nonAdminUser.Id);

        context.ChangeTracker.Clear();

        // Assert
        context.Users.Single(u => u.Id == nonAdminUser.Id).HasRequestedAdmin.Should().BeFalse();
    }
    
    [Fact]
    public void RemoveAdmin_SetsIsAdminToFalse_ForCorrectUser()
    {
        // Arrange
        using var context = Fixture.CreateWriteContext();
        var service = new AdminService(context, A.Fake<ILogger<AdminService>>(), A.Fake<ITenantService>());

        var adminUser = context.Users.Single(u => u.GoogleUserName == "AdminUser");

        // Act
        service.RemoveAdmin(adminUser.Id);

        context.ChangeTracker.Clear();

        // Assert
        context.Users.Single(u => u.Id == adminUser.Id).IsAdmin.Should().BeFalse();
    }

    [Fact]
    public async void GetAdminLists_GetsListOfCurrentAndPendingAdmins_ForAdminUser()
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new AdminService(context, A.Fake<ILogger<AdminService>>(), A.Fake<ITenantService>());

        // Act
        var result = await service.GetAdminLists();

        // Assert

        result.Item1.Should().BeOfType<List<UserDbModel>>().And.Subject.Where(l => l.IsAdmin).Should().HaveCount(result.Item1.Count);
        result.Item2.Should().BeOfType<List<UserDbModel>>().And.Subject.Where(l => l.HasRequestedAdmin).Should().HaveCount(result.Item2.Count);
    }

    [Fact]
    public async void GetAdminLists_OnlyGetsAdmins()
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new AdminService(context, A.Fake<ILogger<AdminService>>(), A.Fake<ITenantService>());

        var adminUser = context.Users.Single(u => u.GoogleUserName == "AdminUser");

        // Act
        var adminLists = await service.GetAdminLists();

        // Assert
        adminLists.CurrentAdmins.Should().OnlyContain(u => u.Id == adminUser.Id);
    }

    [Fact]
    public async void LandlordList_CalledWithNoFilter_ReturnsAllLandlords()
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new AdminService(context, A.Fake<ILogger<AdminService>>(), A.Fake<ITenantService>());

        // Act
        var result = await service.GetLandlordList(null, null, 1, 10);

        // Assert
        result.LandlordList.Should().BeOfType<List<LandlordDbModel>>();
        result.Count.Should().Be(context.Landlords.Count());
    }

    [Fact]
    public async void LandlordList_CalledWithFilters_ReturnsCorrectlyFilteredLandlordList()
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new AdminService(context, A.Fake<ILogger<AdminService>>(), A.Fake<ITenantService>());

        // Act
        var result = await service.GetLandlordList(true, false, 1, 10);

        // Assert
        result.LandlordList.Should().BeOfType<List<LandlordDbModel>>();
        result.Count.Should().Be(context.Landlords.Where(l => l.CharterApproved == true)
            .Count(l => context.Users.SingleOrDefault(u => u.LandlordId == l.Id) == null));
    }

    #region TenantListTests

    [Fact]
    public async void GetTenantList_CalledWithNoFilter_ReturnsAllTenants()
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new AdminService(context, A.Fake<ILogger<AdminService>>(), A.Fake<ITenantService>());
        var filter = new HousingRequirementModel();

        // Act
        var result = await service.GetTenantList(filter, 1, 10);

        // Assert
        result.TenantList.Should().BeOfType<List<TenantDbModel>>();
        result.Count.Should().Be(context.Tenants.Count());
    }

    [Fact]
    public async void GetTenantList_CalledWithFilters_ReturnsCorrectlyFilteredTenantList()
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new AdminService(context, A.Fake<ILogger<AdminService>>(), A.Fake<ITenantService>());
        var filter = new HousingRequirementModel
        {
            AcceptsSingleTenant = true,
            AcceptsNotEET = true,
            AcceptsCredit = true
        };

        // Act
        var result = await service.GetTenantList(filter, 1, 10);

        // Assert
        result.TenantList.Should().BeOfType<List<TenantDbModel>>();
        result.Count.Should().Be(context.Tenants.Where(t => t.ETT == true)
            .Where(t => t.UniversalCredit == true)
            .Count(t => t.Type == "Single"));
    }

    [Fact]
    public async void GetNearestTenantsToProperty_CalledWithFilters_ReturnsCorrectlyFilteredTenantListMaxLength5()
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new AdminService(context, A.Fake<ILogger<AdminService>>(), A.Fake<ITenantService>());
        var filter = new HousingRequirementModel
        {
            AcceptsSingleTenant = true,
            AcceptsNotEET = true,
            AcceptsCredit = false
        };

        // Act
        var result = await service.GetTenantList(filter, 1, 10);

        // Assert
        result.TenantList.Should().BeOfType<List<TenantDbModel>>();
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
        var service = new AdminService(context, A.Fake<ILogger<AdminService>>(), A.Fake<ITenantService>());
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
        var result = await service.GetTenantList(filter, 1, 10);

        // Assert
        result.TenantList.Should().BeOfType<List<TenantDbModel>>();
        result.Count.Should().Be(0);
    }
    
    #endregion
    
    [Fact]
    public void FindUserById_GivenCorrectId_FindsUser()
    {
        // Arrange
        using var context = Fixture.CreateReadContext();
        var service = new AdminService(context, A.Fake<ILogger<AdminService>>(), A.Fake<ITenantService>());
        var approvedLandlord = Fixture.CreateApprovedLandlordUser();

        // Act
        var result = service.FindUserByLandlordId(1);

        // Assert
        // This landlord is created first by the fixture so should have id 1
        result.Should().BeOfType<UserDbModel>();
        result!.GoogleAccountId.Should().Be(approvedLandlord.GoogleAccountId);
    }

    [Fact]
    public void FindUserById_GivenNonexistentId_ReturnsNull()
    {
        // Arrange
        using var context = Fixture.CreateReadContext();
        var service = new AdminService(context, A.Fake<ILogger<AdminService>>(), A.Fake<ITenantService>());

        // Act
        var result = service.FindUserByLandlordId(1000);

        // Assert

        result.Should().BeNull();
    }

    [Fact]
    public void FindExistingInviteLink_GivenUserWithLink_ReturnsLink()
    {
        // Arrange
        using var context = Fixture.CreateReadContext();
        var service = new AdminService(context, A.Fake<ILogger<AdminService>>(), A.Fake<ITenantService>());

        // Act
        var result = service.FindExistingInviteLink(3);

        // Assert
        result.Should().Be("InvitimusLinkimus");
    }

    [Fact]
    public void CreateNewInviteLink_GivenUserWithLink_ThrowsException()
    {
        // Arrange
        using var context = Fixture.CreateReadContext();
        var service = new AdminService(context, A.Fake<ILogger<AdminService>>(), A.Fake<ITenantService>());

        // Assert
        service.Invoking(y => y.CreateNewInviteLink(3)).Should().Throw<Exception>();
    }

    [Fact]
    public void CreateNewInviteLink_GivenUserWithoutLink_ReturnsLinkAndUpdatesDb()
    {
        // Arrange
        using var context = Fixture.CreateWriteContext();
        var service = new AdminService(context, A.Fake<ILogger<AdminService>>(), A.Fake<ITenantService>());

        // Act
        var result = service.CreateNewInviteLink(1);

        context.ChangeTracker.Clear();

        // Assert
        context.Landlords.Single(l => l.Id == 1).InviteLink.Should().NotBeNull();
        result.Should().BeOfType<string>();
    }

    [Fact]
    public void DeleteExistingInviteLink_GivenUserWithLink_MakesLinkNullInDb()
    {
        // Arrange
        using var context = Fixture.CreateWriteContext();
        var service = new AdminService(context, A.Fake<ILogger<AdminService>>(), A.Fake<ITenantService>());

        // Act
        service.DeleteExistingInviteLink(3);

        context.ChangeTracker.Clear();

        // Assert
        context.Landlords.Single(l => l.Id == 3).InviteLink.Should().BeNull();
    }

    [Fact]
    public void DeleteExistingInviteLink_GivenUserWithoutLink_ThrowsException()
    {
        // Arrange
        using var context = Fixture.CreateReadContext();
        var service = new AdminService(context, A.Fake<ILogger<AdminService>>(), A.Fake<ITenantService>());

        // Assert
        service.Invoking(y => y.DeleteExistingInviteLink(1)).Should().Throw<Exception>();
    }
}