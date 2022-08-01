using BricksAndHearts.Database;
using BricksAndHearts.ViewModels;

namespace BricksAndHearts.Services;

public interface IPropertyService
{
    public List<PropertyDbModel> GetPropertiesByLandlord(int landlordId);
    public int AddNewProperty(int landlordId, PropertyViewModel createModel, bool isIncomplete = true);
    public void UpdateProperty(int propertyId, PropertyViewModel updateModel, bool isIncomplete = true);
    public void DeleteProperty(PropertyDbModel property);
    public PropertyDbModel? GetIncompleteProperty(int landlordId);
    public PropertyDbModel? GetPropertyByPropertyId(int propertyId);
    public bool IsUserAdminOrCorrectLandlord(BricksAndHeartsUser currentUser, int propertyId);
    public List<PropertyDbModel> SortProperties(string by);
    public PropertyCountModel CountProperties();
    public void ChangePropertyToIncomplete(PropertyDbModel property);

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
    public int AddNewProperty(int landlordId, PropertyViewModel createModel, bool isIncomplete = true)
    {
        var dbModel = new PropertyDbModel
        {
            LandlordId = landlordId,
            CreationTime = DateTime.Now,
            IsIncomplete = isIncomplete,

            // Address line 1 and postcode should not be null by this point
            AddressLine1 = createModel.Address.AddressLine1!,
            AddressLine2 = createModel.Address.AddressLine2,
            AddressLine3 = createModel.Address.AddressLine3,
            TownOrCity = createModel.Address.TownOrCity,
            County = createModel.Address.County,
            Postcode = createModel.Address.Postcode!,
            Lat = createModel.Lat,
            Lon = createModel.Lon,

            PropertyType = createModel.PropertyType,
            NumOfBedrooms = createModel.NumOfBedrooms,

            Description = createModel.Description,

            AcceptsSingleTenant = createModel.AcceptsSingleTenant,
            AcceptsCouple = createModel.AcceptsCouple,
            AcceptsFamily = createModel.AcceptsFamily,
            AcceptsPets = createModel.AcceptsPets,
            AcceptsBenefits = createModel.AcceptsBenefits,
            AcceptsNotEET = createModel.AcceptsNotEET,
            AcceptsWithoutGuarantor = createModel.AcceptsWithoutGuarantor,

            Rent = createModel.Rent
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

        // Update fields if they have been set (i.e. not null) in updateModel
        // Otherwise use the value we currently have in the database
        dbModel.AddressLine1 = updateModel.Address.AddressLine1 ?? dbModel.AddressLine1;
        dbModel.AddressLine2 = updateModel.Address.AddressLine2 ?? dbModel.AddressLine2;
        dbModel.AddressLine3 = updateModel.Address.AddressLine3 ?? dbModel.AddressLine3;
        dbModel.TownOrCity = updateModel.Address.TownOrCity ?? dbModel.TownOrCity;
        dbModel.County = updateModel.Address.County ?? dbModel.County;
        dbModel.Postcode = updateModel.Address.Postcode ?? dbModel.Postcode;
        dbModel.Lat = updateModel.Lat ?? dbModel.Lat;
        dbModel.Lon = updateModel.Lat ?? dbModel.Lon;

        dbModel.PropertyType = updateModel.PropertyType ?? dbModel.PropertyType;
        dbModel.NumOfBedrooms = updateModel.NumOfBedrooms ?? dbModel.NumOfBedrooms;

        dbModel.Description = updateModel.Description ?? dbModel.Description;

        dbModel.AcceptsSingleTenant = updateModel.AcceptsSingleTenant ?? dbModel.AcceptsSingleTenant;
        dbModel.AcceptsCouple = updateModel.AcceptsCouple ?? dbModel.AcceptsCouple;
        dbModel.AcceptsFamily = updateModel.AcceptsFamily ?? dbModel.AcceptsFamily;
        dbModel.AcceptsPets = updateModel.AcceptsPets ?? dbModel.AcceptsPets;
        dbModel.AcceptsBenefits = updateModel.AcceptsBenefits ?? dbModel.AcceptsBenefits;
        dbModel.AcceptsNotEET = updateModel.AcceptsNotEET ?? dbModel.AcceptsNotEET;
        dbModel.AcceptsWithoutGuarantor = updateModel.AcceptsWithoutGuarantor ?? dbModel.AcceptsWithoutGuarantor;

        dbModel.Rent = updateModel.Rent ?? dbModel.Rent;

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
        // Only one property for each landlord is allowed to be incomplete at a time
        return _dbContext.Properties.SingleOrDefault(p => p.LandlordId == landlordId && p.IsIncomplete == true);
    }

    public PropertyDbModel? GetPropertyByPropertyId(int propertyId)
    {
        return _dbContext.Properties.SingleOrDefault(p => p.Id == propertyId);
    }
    
    public void ChangePropertyToIncomplete(PropertyDbModel property)
    {
        property.IsIncomplete = true;
        _dbContext.SaveChanges();
    }

    public bool IsUserAdminOrCorrectLandlord(BricksAndHeartsUser currentUser, int propertyId)
    {
        if (currentUser.IsAdmin)
        {
            return true;
        }

        var propertyLandlordId = GetPropertyByPropertyId(propertyId)!.LandlordId;
        var userLandlordId = currentUser.LandlordId;
        return propertyLandlordId == userLandlordId;
    }
    
    public List<PropertyDbModel> SortProperties(string? by)
    {
        List<PropertyDbModel> properties;
        if (by == "Availability")
        {
            properties = _dbContext.Properties.OrderBy(m => m.RenterUserId).ToList();
        }
        else if (by == "Rent")
            properties = _dbContext.Properties.OrderBy(m => m.Rent).ToList();
        else {
            properties = _dbContext.Properties.ToList();
        }
        return properties;
    }
    
    public PropertyCountModel CountProperties()
    {
        PropertyCountModel propertyCounts = new PropertyCountModel();
        propertyCounts.RegisteredProperties = _dbContext.Properties.Count();
        //TODO add counts for properties which are Live, Available (Ticket BNH-40)
        return propertyCounts;
    }
}