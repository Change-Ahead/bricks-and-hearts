﻿using System.Data;
using BricksAndHearts.Auth;
using BricksAndHearts.Database;
using BricksAndHearts.Enums;
using BricksAndHearts.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BricksAndHearts.Services;

public interface ILandlordService
{
    public Task<(LandlordRegistrationResult result, LandlordDbModel? dbModel)> RegisterLandlord(
        LandlordProfileModel createModel,
        BricksAndHeartsUser user);

    public Task<(LandlordRegistrationResult result, LandlordDbModel? landlord)> RegisterLandlord(
        LandlordProfileModel createModel);

    public Task<LandlordDbModel?> GetLandlordIfExistsFromId(int? id);
    public Task<LandlordDbModel?> GetLandlordIfExistsWithProperties(int? id);

    public Task<LandlordDbModel> GetLandlordFromId(int id);
    public Task<LandlordRegistrationResult> EditLandlordDetails(LandlordProfileModel editModel);
    public bool CheckForDuplicateEmail(LandlordProfileModel editModel);
    public bool CheckForDuplicateMembershipId(LandlordProfileModel editModel);
    public Task<ApproveLandlordResult> ApproveLandlord(int landlordId, BricksAndHeartsUser user, string membershipId);
    public Task UnapproveLandlord(int landlordId);
    public Task<DisableOrEnableLandlordResult> DisableOrEnableLandlord(int landlordId, string action);
    public LandlordDbModel? FindLandlordWithInviteLink(string inviteLink);
    public string? GetLandlordProfilePicture(int landlordId);

    public Task<LinkUserWithLandlordResult> LinkExistingLandlordWithUser(
        string inviteLink,
        BricksAndHeartsUser user);

    public LandlordCountModel CountLandlords();
}

public class LandlordService : ILandlordService
{
    private readonly BricksAndHeartsDbContext _dbContext;

    public LandlordService(BricksAndHeartsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(LandlordRegistrationResult result, LandlordDbModel? dbModel)> RegisterLandlord(
        LandlordProfileModel createModel,
        BricksAndHeartsUser user)
    {
        var dbModel = new LandlordDbModel
        {
            Title = createModel.Title,
            FirstName = createModel.FirstName,
            LastName = createModel.LastName,
            CompanyName = createModel.CompanyName,
            Email = createModel.Email,
            Phone = createModel.Phone,
            LandlordType = createModel.LandlordType,
            IsLandlordForProfit = createModel.IsLandlordForProfit,
            MembershipId = createModel.MembershipId,
            AddressLine1 = createModel.Address.AddressLine1!,
            AddressLine2 = createModel.Address.AddressLine2,
            AddressLine3 = createModel.Address.AddressLine3,
            TownOrCity = createModel.Address.TownOrCity!,
            County = createModel.Address.County!,
            Postcode = createModel.Address.Postcode!
        };

        await using (var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            // Check there isn't already a Landlord with that email. Nothing depends on this currently, but it would probably mean the landlord is a duplicate
            // This requires Serializable isolation, otherwise it will not lock any rows, and two racing registrations could create duplicate records
            if (await _dbContext.Landlords.AnyAsync(l => l.Email == createModel.Email))
            {
                return (LandlordRegistrationResult.ErrorLandlordEmailAlreadyRegistered, null);
            }

            // If the landlord has a membershipId, check there isn't already a Landlord with that membershipId.
            if (dbModel.MembershipId != null)
            {
                if (await _dbContext.Landlords.AnyAsync(l => l.MembershipId == dbModel.MembershipId))
                {
                    return (LandlordRegistrationResult.ErrorLandlordMembershipIdAlreadyRegistered,
                        null);
                }
            }

            // Check the user doesn't already have a landlord associated
            var userRecord = _dbContext.Users.Single(u => u.Id == user.Id);
            if (userRecord.LandlordId != null)
            {
                return (LandlordRegistrationResult.ErrorUserAlreadyHasLandlordRecord, null);
            }

            // Insert the landlord and call SaveChanges
            // Entity Framework will insert the record, update the user LandlordId and populate dbModel.Id with the new record's id
            _dbContext.Landlords.Add(dbModel);
            userRecord.Landlord = dbModel;
            await _dbContext.SaveChangesAsync();

            // and finally commit
            await transaction.CommitAsync();
        }

        user.LandlordId = dbModel.Id;

        return (LandlordRegistrationResult.Success, dbModel);
    }

    public Task<LandlordDbModel?> GetLandlordIfExistsFromId(int? id)
    {
        return _dbContext.Landlords.SingleOrDefaultAsync(l => l.Id == id);
    }

    public Task<LandlordDbModel?> GetLandlordIfExistsWithProperties(int? id)
    {
        return _dbContext.Landlords.Include(l => l.Properties).SingleOrDefaultAsync(l => l.Id == id);
    }

    public async Task<(LandlordRegistrationResult result, LandlordDbModel? landlord)> RegisterLandlord(
        LandlordProfileModel createModel)
    {
        var dbModel = new LandlordDbModel
        {
            Title = createModel.Title == "Other" ? createModel.TitleInput! : createModel.Title,
            FirstName = createModel.FirstName,
            LastName = createModel.LastName,
            CompanyName = createModel.CompanyName,
            Email = createModel.Email,
            Phone = createModel.Phone,
            LandlordType = createModel.LandlordType,
            IsLandlordForProfit = createModel.IsLandlordForProfit,
            MembershipId = createModel.MembershipId,
            AddressLine1 = createModel.Address.AddressLine1!,
            AddressLine2 = createModel.Address.AddressLine2,
            AddressLine3 = createModel.Address.AddressLine3,
            TownOrCity = createModel.Address.TownOrCity!,
            County = createModel.Address.County!,
            Postcode = createModel.Address.Postcode!
        };

        await using (var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            // Check there isn't already a Landlord with that email. Nothing depends on this currently, but it would probably mean the landlord is a duplicate
            // This requires Serializable isolation, otherwise it will not lock any rows, and two racing registrations could create duplicate records
            if (await _dbContext.Landlords.AnyAsync(l => l.Email == createModel.Email))
            {
                return (LandlordRegistrationResult.ErrorLandlordEmailAlreadyRegistered, null);
            }

            // If the landlord has a membershipId, check there isn't already a Landlord with that membershipId.
            if (dbModel.MembershipId != null)
            {
                if (await _dbContext.Landlords.AnyAsync(l => l.MembershipId == dbModel.MembershipId))
                {
                    return (LandlordRegistrationResult.ErrorLandlordMembershipIdAlreadyRegistered,
                        null);
                }
            }

            // Insert the landlord and call SaveChanges
            // Entity Framework will insert the record and populate dbModel.Id with the new record's id
            _dbContext.Landlords.Add(dbModel);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        return (LandlordRegistrationResult.Success, dbModel);
    }

    public async Task<ApproveLandlordResult> ApproveLandlord(int landlordId, BricksAndHeartsUser user,
        string membershipId)
    {
        var landlord = await GetLandlordIfExistsFromId(landlordId);
        if (landlord is null)
        {
            return ApproveLandlordResult.ErrorLandlordNotFound;
        }

        if (landlord.CharterApproved)
        {
            return ApproveLandlordResult.ErrorAlreadyApproved;
        }

        if (CheckForDuplicateMembershipId(new LandlordProfileModel
                { LandlordId = landlordId, MembershipId = membershipId }))
        {
            return ApproveLandlordResult.ErrorDuplicateMembershipId;
        }

        landlord.CharterApproved = true;
        landlord.ApprovalTime = DateTime.Now;
        landlord.ApprovalAdminId = user.Id;
        landlord.MembershipId = membershipId;

        await _dbContext.SaveChangesAsync();
        return ApproveLandlordResult.Success;
    }

    public async Task UnapproveLandlord(int landlordId)
    {
        var landlord = _dbContext.Landlords.Single(l => l.Id == landlordId);

        landlord.CharterApproved = false;
        landlord.ApprovalTime = null;
        landlord.ApprovalAdminId = null;

        await _dbContext.SaveChangesAsync();
    }

    public async Task<DisableOrEnableLandlordResult> DisableOrEnableLandlord(int landlordId, string action)
    {
        var landlord = await GetLandlordIfExistsFromId(landlordId);
        if (landlord is null)
        {
            return DisableOrEnableLandlordResult.ErrorLandlordNotFound;
        }
        
        if ((action == "disable" && landlord.Disabled)
            || (action == "enable" && !landlord.Disabled))
        {
            return DisableOrEnableLandlordResult.ErrorAlreadyInState;
        }
        
        landlord.Disabled = action switch
        {
            "disable" => true,
            "enable" => false,
            _ => landlord.Disabled
        };

        await _dbContext.SaveChangesAsync();
        return DisableOrEnableLandlordResult.Success;
    }

    public LandlordDbModel? FindLandlordWithInviteLink(string inviteLink)
    {
        return _dbContext.Landlords.SingleOrDefault(l => l.InviteLink == inviteLink);
    }


    public async Task<LandlordRegistrationResult> EditLandlordDetails(LandlordProfileModel editModel)
    {
        var landlordToEdit = await _dbContext.Landlords.SingleAsync(l => l.Id == editModel.LandlordId);
        landlordToEdit.Title = editModel.Title == "Other" ? editModel.TitleInput! : editModel.Title;
        landlordToEdit.FirstName = editModel.FirstName;
        landlordToEdit.LastName = editModel.LastName;
        landlordToEdit.CompanyName = editModel.CompanyName;
        landlordToEdit.Email = editModel.Email;
        landlordToEdit.Phone = editModel.Phone;
        landlordToEdit.LandlordType = editModel.LandlordType;
        landlordToEdit.IsLandlordForProfit = editModel.IsLandlordForProfit;
        landlordToEdit.MembershipId = editModel.MembershipId;
        landlordToEdit.AddressLine1 = editModel.Address.AddressLine1!;
        landlordToEdit.AddressLine2 = editModel.Address.AddressLine2;
        landlordToEdit.AddressLine3 = editModel.Address.AddressLine3;
        landlordToEdit.TownOrCity = editModel.Address.TownOrCity!;
        landlordToEdit.County = editModel.Address.County!;
        landlordToEdit.Postcode = editModel.Address.Postcode!;

        await _dbContext.SaveChangesAsync();
        return LandlordRegistrationResult.Success;
    }

    public bool CheckForDuplicateEmail(LandlordProfileModel editModel)
    {
        var editedLandlord = _dbContext.Landlords.Single(l => l.Id == editModel.LandlordId);
        return _dbContext.Landlords.SingleOrDefault(l => l.Email == editModel.Email) != null
               && editedLandlord.Email != editModel.Email;
    }

    public bool CheckForDuplicateMembershipId(LandlordProfileModel editModel)
    {
        var editedLandlord = _dbContext.Landlords.Single(l => l.Id == editModel.LandlordId);
        if (editModel.MembershipId == null)
        {
            return false;
        }

        return _dbContext.Landlords.SingleOrDefault(l => l.MembershipId == editModel.MembershipId) != null
               && editedLandlord.MembershipId != editModel.MembershipId;
    }

    // Link existing landlord with user
    public async Task<LinkUserWithLandlordResult> LinkExistingLandlordWithUser(
        string inviteLink,
        BricksAndHeartsUser user)
    {
        // Use a REPEATABLE READ transaction so that the invite link and user records are locked
        // while we check that they have not already been used/associated with a landlord respectively.
        var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
        await using (transaction)
        {
            // Check that the link exists
            var landlord = _dbContext.Landlords.SingleOrDefault(l => l.InviteLink == inviteLink);
            if (landlord == null)
            {
                return LinkUserWithLandlordResult.ErrorLinkDoesNotExist;
            }

            // Check the user doesn't already have a landlord associated
            var userRecord = _dbContext.Users.Single(u => u.Id == user.Id);
            if (userRecord.LandlordId != null)
            {
                return LinkUserWithLandlordResult.ErrorUserAlreadyHasLandlordRecord;
            }

            // Disable invite link
            landlord.InviteLink = null;

            // Now we can update the user record too
            userRecord.LandlordId = landlord.Id; // EF should automatically update the rest of it for us
            await _dbContext.SaveChangesAsync();

            // and finally commit
            await transaction.CommitAsync();

            user.LandlordId = landlord.Id; // Update the in memory user object
        }

        return LinkUserWithLandlordResult.Success;
    }

    public Task<LandlordDbModel> GetLandlordFromId(int id)
    {
        return _dbContext.Landlords.SingleOrDefaultAsync(l => l.Id == id)!;
    }

    public LandlordCountModel CountLandlords()
    {
        return new LandlordCountModel
        {
            RegisteredLandlords = _dbContext.Landlords.Count(),
            ApprovedLandlords = _dbContext.Landlords.Count(l => l.CharterApproved == true)
        };
    }

    public string? GetLandlordProfilePicture(int landlordId)
    {
        return _dbContext.Users.FirstOrDefault(u => u.LandlordId == landlordId)?.GoogleProfileImageUrl;
    }
}