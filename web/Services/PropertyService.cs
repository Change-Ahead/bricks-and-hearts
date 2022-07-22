using BricksAndHearts.Database;
using BricksAndHearts.ViewModels;

namespace BricksAndHearts.Services;

public interface IPropertyService
{
    public List<PropertyDbModel> GetPropertiesByLandlord(int landlordId);
    public void AddNewProperty(PropertyViewModel createModel, int landlordId);
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
    public void AddNewProperty(PropertyViewModel createModel, int landlordId)
    {
        var dbModel = new PropertyDbModel
        {
            LandlordId = landlordId,
            CreationTime = DateTime.Now,

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
    }
}