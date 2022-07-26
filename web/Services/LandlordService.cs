﻿using System.Data;
using BricksAndHearts.Auth;
using BricksAndHearts.Database;
using BricksAndHearts.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BricksAndHearts.Services;

public interface ILandlordService
{
    public enum LandlordRegistrationResult
    {
        ErrorLandlordEmailAlreadyRegistered,
        ErrorUserAlreadyHasLandlordRecord,
        Success
    }

    public Task<LandlordRegistrationResult> RegisterLandlordWithUser(LandlordProfileModel createModel, BricksAndHeartsUser user);
    public Task<LandlordDbModel?> GetLandlordIfExistsFromId(int id);
    public Task<LandlordRegistrationResult> EditLandlordDetails(LandlordProfileModel createModel, int? selectedUserId);
    public bool CheckForDuplicateEmail(LandlordProfileModel createModel, int? selectedUserId);
    public Task ApproveLandlord(int landlordId, BricksAndHeartsUser user);

}

public class LandlordService : ILandlordService
{
    private readonly BricksAndHeartsDbContext _dbContext;

    public LandlordService(BricksAndHeartsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Create a new landlord record and associate it with a user
    public async Task<ILandlordService.LandlordRegistrationResult> RegisterLandlordWithUser(
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
            LandlordStatus = createModel.LandlordStatus,
            LandlordProvidedCharterStatus = createModel.LandlordProvidedCharterStatus
        };

        await using (var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            // Check there isn't already a Landlord with that email. Nothing depends on this currently, but it would probably mean the landlord is a duplicate
            // This requires Serializable isolation, otherwise it will not lock any rows, and two racing registrations could create duplicate records
            if (await _dbContext.Landlords.AnyAsync(l => l.Email == createModel.Email))
                return ILandlordService.LandlordRegistrationResult.ErrorLandlordEmailAlreadyRegistered;

            // Check the user doesn't already have a landlord associated
            var userRecord = _dbContext.Users.Single(u => u.Id == user.Id);
            if (userRecord.LandlordId != null)
            {
                return ILandlordService.LandlordRegistrationResult.ErrorUserAlreadyHasLandlordRecord;
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

        return ILandlordService.LandlordRegistrationResult.Success;
    }
    
    public Task<LandlordDbModel?> GetLandlordIfExistsFromId(int id)
    {
        return _dbContext.Landlords.SingleOrDefaultAsync(l => l.Id == id);
    }

    public List<PropertyDbModel> GetListOfProperties(int landlordId)
    {
        return _dbContext.Properties
            .Where(p => p.LandlordId == landlordId).ToList();
    }
    
    public async Task ApproveLandlord(int landlordId,  BricksAndHeartsUser user)
    {
        var landlord = await GetLandlordIfExistsFromId(landlordId);
        if (landlord is null)
        {
            throw new Exception("Landlord does not exist");
        }
        if (landlord!.CharterApproved)
        {
            throw new Exception("Landlord already approved");
        }
        landlord.CharterApproved = true;
        landlord.ApprovalTime = DateTime.Now;
        landlord.ApprovalAdminId = user.Id;
        await _dbContext.SaveChangesAsync();
    }
    
    public async Task<ILandlordService.LandlordRegistrationResult> EditLandlordDetails(LandlordProfileModel createModel, int? selectedUserId)
    {
        var landlordToEdit = await _dbContext.Landlords.SingleAsync(l => l.Id == selectedUserId);
        landlordToEdit.Title = createModel.Title;
        landlordToEdit.FirstName = createModel.FirstName;
        landlordToEdit.LastName = createModel.LastName;
        landlordToEdit.CompanyName = createModel.CompanyName;
        landlordToEdit.Email = createModel.Email;
        landlordToEdit.Phone = createModel.Phone;

        _dbContext.Update(landlordToEdit);
        await _dbContext.SaveChangesAsync();
        return ILandlordService.LandlordRegistrationResult.Success;
    }

    public bool CheckForDuplicateEmail(LandlordProfileModel createModel, int? selectedUserId)
    {
        var editedLandlord = _dbContext.Landlords.Single(l => l.Id == selectedUserId);
        return _dbContext.Landlords.SingleOrDefault(l => l.Email == createModel.Email) != null 
               && editedLandlord.Email != createModel.Email;
    }
}