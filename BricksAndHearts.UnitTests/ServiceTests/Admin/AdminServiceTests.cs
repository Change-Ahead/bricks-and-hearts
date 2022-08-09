using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BricksAndHearts.Auth;
using BricksAndHearts.Database;
using BricksAndHearts.Services;
using FluentAssertions;
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
    public async Task GetAdminLists_OnlyGetsAdmins()
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new AdminService(context, null!);

        var adminUser = context.Users.Single(u => u.GoogleUserName == "AdminUser");

        // Act
        var adminLists = await service.GetAdminLists();

        // Assert
        adminLists.CurrentAdmins.Should().OnlyContain(u => u.Id == adminUser.Id);
    }

    [Fact]
    public void RequestAdminAccess_SetsHasRequestedAdminToTrue_ForCorrectUser()
    {
        // Arrange
        using var context = Fixture.CreateWriteContext();
        var service = new AdminService(context, null!);

        var nonAdminUser = context.Users.Single(u => u.GoogleUserName == "NonAdminUser");
        var adminUser = context.Users.Single(u => u.GoogleUserName == "AdminUser");

        // Act
        service.RequestAdminAccess(new BricksAndHeartsUser(nonAdminUser, null!, null!));

        // Before assert we need to clear the context's change tracker so that the following database queries actually
        // query the database, as if this were a new context. This should be done for all write tests.
        context.ChangeTracker.Clear();

        // Assert
        context.Users.Single(u => u.Id == nonAdminUser.Id).HasRequestedAdmin.Should().BeTrue();
        context.Users.Single(u => u.Id == adminUser.Id).HasRequestedAdmin.Should().BeFalse();
    }

    [Fact]
    public void CancelAdminAccessRequest_SetsHasRequestedAdminToFalse_ForCorrectUser()
    {
        // Arrange
        using var context = Fixture.CreateWriteContext();
        var service = new AdminService(context, null!);

        var requestedAdminUser = context.Users.Single(u => u.GoogleUserName == "NonAdminUser");

        // Act
        service.CancelAdminAccessRequest(new BricksAndHeartsUser(requestedAdminUser, null!, null!));

        // Before assert we need to clear the context's change tracker so that the following database queries actually
        // query the database, as if this were a new context. This should be done for all write tests.
        context.ChangeTracker.Clear();

        // Assert
        context.Users.Single(u => u.Id == requestedAdminUser.Id).HasRequestedAdmin.Should().BeFalse();
    }

    [Fact]
    public void GetAdminLists_GetsListOfCurrentAndPendingAdmins_ForAdminUser()
    {
        // Arrange
        using var context = Fixture.CreateReadContext();
        var service = new AdminService(context,null!);

        // Act
        var result = service.GetAdminLists().Result;

        // Assert

        result.Item1.Should().BeOfType<List<UserDbModel>>().And.Subject.Where(l => l.IsAdmin).Should().HaveCount(result.Item1.Count);
        result.Item2.Should().BeOfType<List<UserDbModel>>().And.Subject.Where(l => l.HasRequestedAdmin).Should().HaveCount(result.Item2.Count);
    }
    
    [Fact]
    public void GetTenantList_GetsListOfTenants()
    {
        // Arrange
        using var context = Fixture.CreateReadContext();
        var service = new AdminService(context,null!);

        // Act
        var result = service.GetTenantList().Result;

        // Assert

        result.Should().BeOfType<List<TenantDbModel>>().And.Subject.Should().HaveCount(context.Tenants.Count());
    }

    [Fact]
    public void GetLandlordDisplayList_CalledWithApproved_ReturnsApprovedLandlords()
    {
        // Arrange
        using var context = Fixture.CreateReadContext();
        var service = new AdminService(context,null!);

        // Act
        var result = service.GetLandlordDisplayList("Approved").Result;

        // Assert
        result.Should().BeOfType<List<LandlordDbModel>>().And.Subject.Where(u => u.CharterApproved).Should()
            .HaveCount(result.Count);
    }
    
    [Fact]
    public void GetLandlordDisplayList_CalledWithUnapproved_ReturnsUnapprovedLandlords()
    {
        // Arrange
        using var context = Fixture.CreateReadContext();
        var service = new AdminService(context,null!);

        // Act
        var result = service.GetLandlordDisplayList("Unapproved").Result;

        // Assert
        result.Should().BeOfType<List<LandlordDbModel>>().And.Subject.Where(u => u.CharterApproved == false).Should()
            .HaveCount(result.Count);
    }
    
    [Fact]
    public void GetLandlordDisplayList_CalledWithNothing_ReturnsAllLandlords()
    {
        // Arrange
        using var context = Fixture.CreateReadContext();
        var service = new AdminService(context,null!);

        // Act
        var result = service.GetLandlordDisplayList("").Result;
        
        // Assert
        result.Should().BeOfType<List<LandlordDbModel>>().And.Subject.Should().HaveCount(context.Landlords.Count());
    }

    [Fact]
    public void FindUserById_GivenCorrectId_FindsUser()
    {
        // Arrange
        using var context = Fixture.CreateReadContext();
        var service = new AdminService(context,null!);
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
        var service = new AdminService(context,null!);

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
        var service = new AdminService(context,null!);

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
        var service = new AdminService(context,null!);

        // Assert
        service.Invoking(y => y.CreateNewInviteLink(3)).Should().Throw<Exception>();
    }
    
    [Fact]
    public void CreateNewInviteLink_GivenUserWithoutLink_ReturnsLinkAndUpdatesDb()
    {
        // Arrange
        using var context = Fixture.CreateWriteContext();
        var service = new AdminService(context,null!);

        // Act
        var result = service.CreateNewInviteLink(1);

        // Before assert we need to clear the context's change tracker so that the following database queries actually
        // query the database, as if this were a new context. This should be done for all write tests.
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
        var service = new AdminService(context,null!);

        // Act
        service.DeleteExistingInviteLink(3);

        // Before assert we need to clear the context's change tracker so that the following database queries actually
        // query the database, as if this were a new context. This should be done for all write tests.
        context.ChangeTracker.Clear();

        // Assert
        context.Landlords.Single(l => l.Id == 3).InviteLink.Should().BeNull();
    }
    
    [Fact]
    public void DeleteExistingInviteLink_GivenUserWithoutLink_ThrowsException()
    {
        // Arrange
        using var context = Fixture.CreateReadContext();
        var service = new AdminService(context,null!);

        // Assert
        service.Invoking(y => y.DeleteExistingInviteLink(1)).Should().Throw<Exception>();
    }
}
