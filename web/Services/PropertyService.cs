using System.Data;
using BricksAndHearts.Database;
using BricksAndHearts.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BricksAndHearts.Services;

public interface IPropertyService
{
    public List<PropertyDbModel> GetPropertiesByLandlord(int landlordId);
    public Task AddNewProperty(PropertyViewModel createModel, int landlordId);
}

public class PropertyService : IPropertyService
{
    private readonly BricksAndHeartsDbContext _dbContext;

    public PropertyService(BricksAndHeartsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public List<PropertyDbModel> GetPropertiesByLandlord(int landlordId)
    {
        return _dbContext.Properties
            .Where(p => p.LandlordId == landlordId).ToList();
    }

    // Create a new property record and associate it with a landlord
    public async Task AddNewProperty(PropertyViewModel createModel, int landlordId)
    {
        var dbModel = new PropertyDbModel
        {
            LandlordId = landlordId,
            CreationTime = DateTime.Now,

            AddressLine1 = createModel.Address.AddressLine1,
            AddressLine2 = createModel.Address.AddressLine2,
            AddressLine3 = createModel.Address.AddressLine3,
            TownOrCity = createModel.Address.TownOrCity,
            County = createModel.Address.County,
            Postcode = createModel.Address.Postcode,

            PropertyType = createModel.PropertyType,
            NumOfBedrooms = createModel.NumOfBedrooms,
            Rent = createModel.Rent,
            Description = createModel.Description
        };

        // We want to atomically update multiple records (insert a property, then add it to its landlord's list of properties), so first start a transaction
        await using (var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            // Insert the property and call SaveChanges
            // Entity Framework will insert the record and populate dbModel.Id with the new record's id
            _dbContext.Properties.Add(dbModel);
            await _dbContext.SaveChangesAsync();

            // Now we can update the landlord record too
            var landlordRecord = _dbContext.Landlords.Single(l => l.Id == landlordId);
            landlordRecord.Properties.Add(dbModel);
            await _dbContext.SaveChangesAsync();

            // and finally commit
            await transaction.CommitAsync();
        }
    }
}