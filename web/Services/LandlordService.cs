using System.Data;
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
    public List<PropertyViewModel> GetListOfProperties(int landlordId);

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
            Phone = createModel.Phone
        };

        // We want to atomically update multiple records (insert a landlord, then set the user's landlord id), so first start a transaction
        await using (var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            // Check there isn't already a Landlord with that email. Nothing depends on this currently, but it would probably mean the landlord is a duplicate
            // This requires Serializable isolation, otherwise it will not lock any rows, and two racing registrations could create duplicate records
            if (await _dbContext.Landlords.AnyAsync(l => l.Email == createModel.Email))
                return ILandlordService.LandlordRegistrationResult.ErrorLandlordEmailAlreadyRegistered;

            // Check the user doesn't already have a landlord associated
            var userRecord = _dbContext.Users.Single(u => u.Id == user.Id);
            if (userRecord.LandlordId != null) return ILandlordService.LandlordRegistrationResult.ErrorUserAlreadyHasLandlordRecord;

            // Insert the landlord and call SaveChanges
            // Entity Framework will insert the record and populate dbModel.Id with the new record's id
            _dbContext.Landlords.Add(dbModel);
            await _dbContext.SaveChangesAsync();

            // Now we can update the user record too
            userRecord.LandlordId = dbModel.Id;
            await _dbContext.SaveChangesAsync();

            // and finally commit
            await transaction.CommitAsync();
        }

        user.LandlordId = dbModel.Id;

        return ILandlordService.LandlordRegistrationResult.Success;
    }

    public List<PropertyViewModel> GetListOfProperties(int landlordId)
    {

        var landlord = _dbContext.Landlords
            .Include(l => l.Properties)
            .SingleOrDefault(l => l.Id == landlordId);
        // Assumes landlord does exist
        var listOfProperties = new List<PropertyViewModel>(); 
        landlord.Properties.ForEach(p=> listOfProperties.Add(PropertyViewModel.FromDbModel(p)));
        return listOfProperties;
    }
}