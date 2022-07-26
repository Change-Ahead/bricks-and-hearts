using System.Linq;
using BricksAndHearts.Auth;
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
    public async void GetAdminLists_OnlyGetsAdmins()
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new AdminService(context);

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
        var service = new AdminService(context);

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
        var service = new AdminService(context);

        var requestedAdminUser = context.Users.Single(u => u.GoogleUserName == "NonAdminUser");

        // Act
        service.CancelAdminAccessRequest(new BricksAndHeartsUser(requestedAdminUser, null!, null!));

        // Before assert we need to clear the context's change tracker so that the following database queries actually
        // query the database, as if this were a new context. This should be done for all write tests.
        context.ChangeTracker.Clear();

        // Assert
        context.Users.Single(u => u.Id == requestedAdminUser.Id).HasRequestedAdmin.Should().BeFalse();
    }
}
