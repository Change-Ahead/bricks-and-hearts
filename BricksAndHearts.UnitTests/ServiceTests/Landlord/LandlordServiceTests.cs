// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Security.Claims;
// using BricksAndHearts.Auth;
// using BricksAndHearts.Database;
// using BricksAndHearts.Services;
// using BricksAndHearts.ViewModels;
// using FakeItEasy;
// using FluentAssertions;
// using FluentAssertions.Extensions;
// using Xunit;
//
// namespace BricksAndHearts.UnitTests.ServiceTests.Landlord;
//
// public class LandlordServiceTests : IClassFixture<TestDatabaseFixture>
// {
//     private TestDatabaseFixture Fixture { get; }
//
//     public LandlordServiceTests(TestDatabaseFixture fixture)
//     {
//         Fixture = fixture;
//     }
//
//     //RegisterLandlord can't be tested as it uses a transaction
//
//     [Fact]
//     public async void GetLandlordIfExistsFromId_ReturnsLandlord_WithUniqueId()
//     {
//         // Arrange
//         await using var context = Fixture.CreateReadContext();
//         var service = new LandlordService(context);
//
//         // Act
//         var result = await service.GetLandlordIfExistsFromId(4);
//
//         // Assert
//         result.Should().BeOfType<LandlordDbModel>().Which.Id.Should().Be(4);
//     }
//
//     [Fact]
//     public async void GetLandlordIfExistsFromId_ReturnsNull_WithInvalidId()
//     {
//         // Arrange
//         await using var context = Fixture.CreateReadContext();
//         var service = new LandlordService(context);
//
//         // Act
//         var result = await service.GetLandlordIfExistsFromId(1000);
//         var result2 = await service.GetLandlordIfExistsFromId(null);
//
//         // Assert
//         result.Should().BeNull();
//         result2.Should().BeNull();
//     }
//
//     [Fact]
//     public async void GetLandlordIfExistsWithProperties_ReturnsLandlord_WithUniqueId()
//     {
//         // Arrange
//         await using var context = Fixture.CreateReadContext();
//         var service = new LandlordService(context);
//
//         // Act
//         var result = await service.GetLandlordIfExistsWithProperties(1);
//
//         // Assert
//         result.Should().BeOfType<LandlordDbModel>().Which.Id.Should().Be(1);
//         result!.Properties.Should().NotBeEmpty().And.NotBeNull();
//     }
//     
//     [Fact]
//     public async void GetLandlordIfExistsWithProperties_ReturnsNull_WithInvalidId()
//     {
//         // Arrange
//         await using var context = Fixture.CreateReadContext();
//         var service = new LandlordService(context);
//
//         // Act
//         var result = await service.GetLandlordIfExistsFromId(1000);
//         var result2 = await service.GetLandlordIfExistsFromId(null);
//
//         // Assert
//         result.Should().BeNull();
//         result2.Should().BeNull();
//     }
//
//     [Fact]
//     public async void ApproveLandlord_ReturnsSorry_ForNonexistentLandlord()
//     {
//         // Arrange
//         await using var context = Fixture.CreateReadContext();
//         var service = new LandlordService(context);
//         var user = A.Fake<BricksAndHeartsUser>();
//         const string membershipId = "abc123";
//
//         // Act
//         var result = await service.ApproveLandlord(1000, user, membershipId);
//
//         // Assert
//         result.Should().Be(ILandlordService.ApproveLandlordResult.ErrorLandlordNotFound);
//     }
//     
//     [Fact]
//     public async void ApproveLandlord_ReturnsCharterAlreadyApproved_ForLandlordWithCharterPreApproved()
//     {
//         // Arrange
//         await using var context = Fixture.CreateReadContext();
//         var service = new LandlordService(context);
//         var user = A.Fake<BricksAndHeartsUser>();
//         const string membershipId = "abc123";
//
//         // Act
//         var result = await service.ApproveLandlord(1, user, membershipId);
//
//         // Assert
//         result.Should().Be(ILandlordService.ApproveLandlordResult.ErrorAlreadyApproved);
//     }
//
//     [Fact]
//     public async void ApproveLandlord_ReturnsCharterApproved_ForLandlordWithUnapprovedCharter()
//     {
//         // Arrange
//         await using var context = Fixture.CreateWriteContext();
//         var service = new LandlordService(context);
//         var user = A.Fake<BricksAndHeartsUser>();
//         const string membershipId = "abc123";
//
//         // Act
//         var result = await service.ApproveLandlord(2, user, membershipId);
//         
//         // Before assert we need to clear the context's change tracker so that the following database queries actually
//         // query the database, as if this were a new context. This should be done for all write tests.
//         context.ChangeTracker.Clear();
//
//         // Assert
//         result.Should().Be(ILandlordService.ApproveLandlordResult.Success);
//         context.Landlords.Single(u => u.Id == 2).CharterApproved.Should().BeTrue();
//         context.Landlords.Single(u => u.Id == 2).ApprovalTime.Should().BeCloseTo(DateTime.Now, 1.Seconds());
//         context.Landlords.Single(u => u.Id == 2).ApprovalAdminId.Should().Be(user.Id);
//         context.Landlords.Single(u => u.Id == 2).MembershipId.Should().Be(membershipId);
//         
//     }
//
//     [Fact]
//     public void FindLandlordWithInviteLink_ReturnsLinkedLandlord_WithValidLink()
//     {
//         // Arrange
//         using var context = Fixture.CreateReadContext();
//         var service = new LandlordService(context);
//
//         // Act
//         var result = service.FindLandlordWithInviteLink("InvitimusLinkimus");
//
//         // Assert
//         result.Should().BeOfType<LandlordDbModel?>().Which!.FirstName.Should().Be("Landlord3Link");
//     }
//     
//     [Fact]
//     public void FindLandlordWithInviteLink_ReturnsNull_WithInvalidLink()
//     {
//         // Arrange
//         using var context = Fixture.CreateReadContext();
//         var service = new LandlordService(context);
//
//         // Act
//         var result = service.FindLandlordWithInviteLink("This link is invalid");
//
//         // Assert
//         result.Should().BeNull();
//     }
//     
//     [Fact]
//     public async void EditLandlordDetails_UpdatesLandlord_WithNewLandlordDetails()
//     {
//         // Arrange
//         await using var context = Fixture.CreateWriteContext();
//         var service = new LandlordService(context);
//         var landlordToEdit = new LandlordProfileModel();
//         // var landlordToEdit = Fixture.CreateLandlordProfileWithEditedEmail("NewEmail@Boring.com");
//
//         // Act
//         var result = await service.EditLandlordDetails(landlordToEdit);
//         
//         // Before assert we need to clear the context's change tracker so that the following database queries actually
//         // query the database, as if this were a new context. This should be done for all write tests.
//         context.ChangeTracker.Clear();
//
//         // Assert
//         context.Landlords.Single(u => u.Id == 3).Email.Should().BeEquivalentTo("NewEmail@Boring.com");
//         result.Should().Be(ILandlordService.LandlordRegistrationResult.Success);
//     }
//
//     #region CheckForDuplicateEmail
//
//         [Fact]
//         public void CheckForDuplicateEmail_ReturnsTrue_WithDuplicateEmail()
//         {
//             // Arrange
//             using var context = Fixture.CreateReadContext();
//             var service = new LandlordService(context);
//             var landlordToEdit = new LandlordProfileModel();
//             // var landlordToEdit = Fixture.CreateLandlordProfileWithEditedEmail("test.landlord1@gmail.com");
//
//             // Act
//             var result = service.CheckForDuplicateEmail(landlordToEdit);
//
//             // Assert
//             result.Should().BeTrue();
//         }
//         
//         [Fact]
//         public void CheckForDuplicateEmail_ReturnsFalse_WithNewEmail()
//         {
//             // Arrange
//             using var context = Fixture.CreateReadContext();
//             var service = new LandlordService(context);
//             var landlordToEdit = new LandlordProfileModel();
//             // var landlordToEdit = Fixture.CreateLandlordProfileWithEditedEmail("NewEmail@Boring.com");
//
//             // Act
//             var result = service.CheckForDuplicateEmail(landlordToEdit);
//
//             // Assert
//             result.Should().BeFalse();
//         }
//
//     #endregion
//
//     #region CheckForDuplicateMembershipId
//
//         [Fact]
//         public void CheckForDuplicateMembershipId_ReturnsTrue_WithDuplicateMembershipId()
//         {
//             // Arrange
//             using var context = Fixture.CreateReadContext();
//             var service = new LandlordService(context);
//             var landlordToEdit = new LandlordProfileModel();
//             // var landlordToEdit = Fixture.CreateLandlordProfileWithEditedMemberId(421);
//
//             // Act
//             var result = service.CheckForDuplicateMembershipId(landlordToEdit);
//
//             // Assert
//             result.Should().BeTrue();
//         }
//         
//         [Fact]
//         public void CheckForDuplicateMembershipId_ReturnsFalse_WithNewMembershipId()
//         {
//             // Arrange
//             using var context = Fixture.CreateReadContext();
//             var service = new LandlordService(context);
//             var landlordToEdit = new LandlordProfileModel();
//             // var landlordToEdit = Fixture.CreateLandlordProfileWithEditedMemberId(1);
//
//             // Act
//             var result = service.CheckForDuplicateMembershipId(landlordToEdit);
//
//             // Assert
//             result.Should().BeFalse();
//         }
//
//         #endregion
//     
//     //this may need to be updated numbers-wise if anyone ever adds any extra landlords to the test area
//     [Fact]
//     public void CountLandlords_ReturnsLandlordCountModel_WithCorrectData()
//     {
//         // Arrange
//         using var context = Fixture.CreateReadContext();
//         var service = new LandlordService(context);
//
//         // Act
//         var result = service.CountLandlords();
//
//         // Assert
//         result.Should().BeOfType<LandlordCountModel>();
//         result.RegisteredLandlords.Should().Be(6);
//         result.ApprovedLandlords.Should().Be(5);
//     }
//
//     [Fact]
//     public async void LinkExistingLandlordWithUser_WithInvalidLink_ReturnsErrorLinkDoesNotExist()
//     {
//         // Arrange
//         await using var context = Fixture.CreateReadContext();
//         var service = new LandlordService(context);
//         const string inviteLink = "000";
//         var nonAdminUserModel = context.Users.Single(u => u.GoogleUserName == "NonAdminUser");
//         var nonAdminUser = new BricksAndHeartsUser(nonAdminUserModel, new List<Claim>(), "google");
//
//         // Act
//         var result = await service.LinkExistingLandlordWithUser(inviteLink, nonAdminUser);
//
//         // Assert
//         result.Should().Be(ILandlordService.LinkUserWithLandlordResult.ErrorLinkDoesNotExist);
//     }
//
//     [Fact]
//     public async void LinkExistingLandlordWithUser_WithValidLink_ReturnsErrorUserAlreadyHasLandlordRecord()
//     {
//         // Arrange
//         await using var context = Fixture.CreateReadContext();
//         var service = new LandlordService(context);
//         var landlord = context.Landlords.Single(l => l.FirstName == "Landlord4Unlinked");
//         var inviteLink = landlord.InviteLink;
//         if (inviteLink == null)
//         {
//             throw new Exception("Expect invite link to exist!");
//         }
//
//         var landlordUserModel = context.Users.Single(u => u.GoogleUserName == "LinkedLandlordUser");
//         var user = new BricksAndHeartsUser(landlordUserModel, new List<Claim>(), "google");
//
//         if (landlordUserModel.LandlordId == null)
//         {
//             throw new Exception("Expect landlord ID to not be null!");
//         }
//
//         if (landlordUserModel.LandlordId == landlord.Id)
//         {
//             throw new Exception(
//                 "Expect landlord ID of the linked user to be different from landlord ID of unlinked landlord!");
//         }
//
//         // Act
//         var result = await service.LinkExistingLandlordWithUser(inviteLink, user);
//
//         // Assert
//         result.Should().Be(ILandlordService.LinkUserWithLandlordResult.ErrorUserAlreadyHasLandlordRecord);
//     }
//
//     [Fact]
//     public async void ApproveLandlord_SetsMembershipId()
//     {
//         // Arrange
//         await using var context = Fixture.CreateWriteContext();
//         var service = new LandlordService(context);
//
//         var landlord = context.Landlords.Single(l => l.Email == "test.landlord2@gmail.com");
//         var admin = new BricksAndHeartsUser(Fixture.CreateAdminUser(), new List<Claim>(), "google");
//
//         // Act
//         var result = await service.ApproveLandlord(landlord.Id, admin, "abc");
//
//         // Assert
//         context.ChangeTracker.Clear();
//
//         result.Should().Be(ILandlordService.ApproveLandlordResult.Success);
//
//         landlord = context.Landlords.Single(l => l.Email == "test.landlord2@gmail.com");
//         landlord.CharterApproved.Should().BeTrue();
//         landlord.ApprovalAdminId.Should().Be(admin.Id);
//         landlord.MembershipId.Should().Be("abc");
//     }
//
//     [Fact]
//     public async void ApproveLandlord_ReturnsError_IfDuplicateMembershipId()
//     {
//         // Arrange
//         await using var context = Fixture.CreateWriteContext();
//         var service = new LandlordService(context);
//
//         var landlord = context.Landlords.Single(l => l.Email == "test.landlord2@gmail.com");
//         var admin = new BricksAndHeartsUser(Fixture.CreateAdminUser(), new List<Claim>(), "google");
//
//         // Act
//         var result = await service.ApproveLandlord(landlord.Id, admin, "member1");
//
//         // Assert
//         context.ChangeTracker.Clear();
//
//         result.Should().Be(ILandlordService.ApproveLandlordResult.ErrorDuplicateMembershipId);
//
//         landlord = context.Landlords.Single(l => l.Email == "test.landlord2@gmail.com");
//         landlord.CharterApproved.Should().BeFalse();
//         landlord.MembershipId.Should().BeNull();
//     }
//
//     // Cannot test for the case LinkExistingLandlordWithError returning Success
//     // Since it involves writing to the database and the method includes a transaction
// }