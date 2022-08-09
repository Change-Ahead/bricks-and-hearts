using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using BricksAndHearts.Auth;
using BricksAndHearts.Services;
using BricksAndHearts.ViewModels;
using FluentAssertions;
using Xunit;

namespace BricksAndHearts.UnitTests.ServiceTests.Landlord;

public class LandlordServiceTests : IClassFixture<TestDatabaseFixture>
{
    private TestDatabaseFixture Fixture { get; }

    public LandlordServiceTests(TestDatabaseFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async void CountLandlords_ReturnsLandlordCountModel_WithCorrectData()
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new LandlordService(context);

        // Act
        var result = service.CountLandlords();

        // Assert
        result.Should().BeOfType<LandlordCountModel>();
        result.RegisteredLandlords.Should().Be(4);
        result.ApprovedLandlords.Should().Be(3);
    }

    [Fact]
    public async void LinkExistingLandlordWithUser_WithInvalidLink_ReturnsErrorLinkDoesNotExist()
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new LandlordService(context);
        const string inviteLink = "000";
        var nonAdminUserModel = context.Users.Single(u => u.GoogleUserName == "NonAdminUser");
        var nonAdminUser = new BricksAndHeartsUser(nonAdminUserModel, new List<Claim>(), "google");

        // Act
        var result = await service.LinkExistingLandlordWithUser(inviteLink, nonAdminUser);

        // Assert
        result.Should().Be(ILandlordService.LinkUserWithLandlordResult.ErrorLinkDoesNotExist);
    }

    [Fact]
    public async void LinkExistingLandlordWithUser_WithValidLink_ReturnsErrorUserAlreadyHasLandlordRecord()
    {
        // Arrange
        await using var context = Fixture.CreateReadContext();
        var service = new LandlordService(context);
        var landlord = context.Landlords.Single(l => l.FirstName == "Unlinked");
        var inviteLink = landlord.InviteLink;
        if (inviteLink == null)
        {
            throw new Exception("Expect invite link to exist!");
        }

        var landlordUserModel = context.Users.Single(u => u.GoogleUserName == "LinkedLandlordUser");
        var user = new BricksAndHeartsUser(landlordUserModel, new List<Claim>(), "google");

        if (landlordUserModel.LandlordId == null)
        {
            throw new Exception($"Expect landlord ID to not be null!");
        }

        if (landlordUserModel.LandlordId == landlord.Id)
        {
            throw new Exception(
                $"Expect landlord ID of the linked user to be different from landlord ID of unlinked landlord!");
        }

        // Act
        var result = await service.LinkExistingLandlordWithUser(inviteLink, user);

        // Assert
        result.Should().Be(ILandlordService.LinkUserWithLandlordResult.ErrorUserAlreadyHasLandlordRecord);
    }

    [Fact]
    public async void ApproveLandlord_SetsMembershipId()
    {
        // Arrange
        await using var context = Fixture.CreateWriteContext();
        var service = new LandlordService(context);

        var landlord = context.Landlords.Single(l => l.Email == "test.landlord2@gmail.com");
        var admin = new BricksAndHeartsUser(Fixture.CreateAdminUser(), new List<Claim>(), "google");

        // Act
        var result = await service.ApproveLandlord(landlord.Id, admin, "abc");

        // Assert
        context.ChangeTracker.Clear();

        result.Should().Be($"Successfully approved landlord charter for {landlord.FirstName} {landlord.LastName}.");

        landlord = context.Landlords.Single(l => l.Email == "test.landlord2@gmail.com");
        landlord.CharterApproved.Should().BeTrue();
        landlord.ApprovalAdminId.Should().Be(admin.Id);
        landlord.MembershipId.Should().Be("abc");
    }

    // Cannot test for the case LinkExistingLandlordWithError returning Success
    // Since it involves writing to the database and the method includes a transaction
}