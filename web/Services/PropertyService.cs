using BricksAndHearts.Database;
using BricksAndHearts.ViewModels;

namespace BricksAndHearts.Services;

public interface IPropertyService
{
    public List<PropertyDbModel> GetPropertiesByLandlord(int landlordId);
    public int AddNewProperty(PropertyViewModel createModel, int landlordId, bool isIncomplete = true);
    public void UpdateProperty(int propertyId, PropertyViewModel updateModel, bool isIncomplete = true);
    public void DeleteProperty(PropertyDbModel property);
    public PropertyDbModel? GetIncompleteProperty(int landlordId);
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
    public int AddNewProperty(PropertyViewModel createModel, int landlordId, bool isIncomplete = true)
    {
        var dbModel = new PropertyDbModel
        {
            LandlordId = landlordId,
            CreationTime = DateTime.Now,
            IsIncomplete = isIncomplete,

            AddressLine1 = createModel.Address!.AddressLine1,
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

        // Add the new property to the database
        _dbContext.Properties.Add(dbModel);
        _dbContext.SaveChanges();

        return dbModel.Id;
    }

    // Update any fields that are not null in updateModel
    public void UpdateProperty(int propertyId, PropertyViewModel updateModel, bool isIncomplete = true)
    {
        var dbModel = _dbContext.Properties.Single(p => p.Id == propertyId);

        if (updateModel.Address != null)
        {
            dbModel.AddressLine1 = updateModel.Address.AddressLine1;
            dbModel.AddressLine2 = updateModel.Address.AddressLine2;
            dbModel.AddressLine3 = updateModel.Address.AddressLine3;
            dbModel.TownOrCity = updateModel.Address.TownOrCity;
            dbModel.County = updateModel.Address.County;
            dbModel.Postcode = updateModel.Address.Postcode;
        }

        dbModel.PropertyType = updateModel.PropertyType ?? dbModel.PropertyType;
        dbModel.NumOfBedrooms = updateModel.NumOfBedrooms ?? dbModel.NumOfBedrooms;
        dbModel.Rent = updateModel.Rent ?? dbModel.Rent;
        dbModel.Description = updateModel.Description ?? dbModel.Description;

        dbModel.IsIncomplete = isIncomplete;
        _dbContext.SaveChanges();
    }

    public void DeleteProperty(PropertyDbModel property)
    {
        _dbContext.Properties.Remove(property);
        _dbContext.SaveChanges();
    }

    public PropertyDbModel? GetIncompleteProperty(int landlordId)
    {
        return _dbContext.Properties.SingleOrDefault(p => p.LandlordId == landlordId && p.IsIncomplete == true);
    }
}