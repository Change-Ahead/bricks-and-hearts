using BricksAndHearts.Auth;
using BricksAndHearts.Database;
using BricksAndHearts.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BricksAndHearts.Services;

public interface IPropertyService
{
    public Task<int> AddNewProperty(int landlordId, PropertyViewModel createModel, bool isIncomplete = true);
    public Task UpdateProperty(int propertyId, PropertyViewModel updateModel, bool isIncomplete = true);
    public void DeleteProperty(PropertyDbModel property);
    public PropertyDbModel? GetPropertyByPropertyId(int propertyId);
    public bool IsUserAdminOrCorrectLandlord(BricksAndHeartsUser currentUser, int propertyId);
    public LandlordDbModel GetPropertyOwner(int propertyId);
    public Task<(List<PropertyDbModel> PropertyList, int Count)> GetPropertyList(string sortBy, string? target, int page, int propPerPage);
    public Task<(List<PropertyDbModel> PropertyList, int Count)> GetPropertiesByLandlord(int landlordId, int page, int propPerPage);
    public PropertyCountModel CountProperties(int? landlordId = null);
    public void CreatePublicViewLink(int propertyId);
    public PropertyDbModel? GetPropertyByPublicViewLink(string token);
}

public class PropertyService : IPropertyService
{
    private readonly BricksAndHeartsDbContext _dbContext;
    private readonly IPostcodeService _postcodeService;

    public PropertyService(BricksAndHeartsDbContext dbContext, IPostcodeService postcodeService)
    {
        _dbContext = dbContext;
        _postcodeService = postcodeService;
    }

    // Create a new property record and associate it with a landlord
    public async Task<int> AddNewProperty(int landlordId, PropertyViewModel createModel, bool isIncomplete = true)
    {
        var postcode = await _postcodeService.GetPostcodeAndAddIfAbsent(createModel.Address.Postcode);
        var dbModel = new PropertyDbModel
        {
            LandlordId = landlordId,
            CreationTime = DateTime.Now,
            IsIncomplete = isIncomplete,
            PublicViewLink = Guid.NewGuid().ToString(),

            // Address line 1 and postcode should not be null by this point
            AddressLine1 = createModel.Address.AddressLine1!,
            AddressLine2 = createModel.Address.AddressLine2,
            AddressLine3 = createModel.Address.AddressLine3,
            TownOrCity = createModel.Address.TownOrCity,
            County = createModel.Address.County,
            PostcodeId = postcode?.Postcode,

            PropertyType = createModel.PropertyType,
            NumOfBedrooms = createModel.NumOfBedrooms,

            Description = createModel.Description,

            AcceptsSingleTenant = createModel.LandlordRequirements.AcceptsSingleTenant,
            AcceptsCouple = createModel.LandlordRequirements.AcceptsCouple,
            AcceptsFamily = createModel.LandlordRequirements.AcceptsFamily,
            AcceptsPets = createModel.LandlordRequirements.AcceptsPets,
            AcceptsCredit = createModel.LandlordRequirements.AcceptsCredit,
            AcceptsBenefits = createModel.LandlordRequirements.AcceptsBenefits,
            AcceptsNotEET = createModel.LandlordRequirements.AcceptsNotEET,
            AcceptsOver35 = createModel.LandlordRequirements.AcceptsOver35,
            AcceptsWithoutGuarantor = createModel.LandlordRequirements.AcceptsWithoutGuarantor,

            Rent = createModel.Rent,

            Availability = createModel.Availability ?? AvailabilityState.Draft,
            TotalUnits = createModel.TotalUnits ?? 1,
            OccupiedUnits = createModel.OccupiedUnits ?? 0,
            AvailableFrom = createModel.Availability == AvailabilityState.AvailableSoon
                ? createModel.AvailableFrom
                : null
        };

        // Add the new property to the database
        _dbContext.Properties.Add(dbModel);
        await _dbContext.SaveChangesAsync();

        return dbModel.Id;
    }

    // Update any fields that are not null in updateModel
    public async Task UpdateProperty(int propertyId, PropertyViewModel updateModel, bool isIncomplete = true)
    {
        var dbModel = _dbContext.Properties
            .Include(p => p.Postcode)
            .Single(p => p.Id == propertyId);
        var newPostcode = await _postcodeService.GetPostcodeAndAddIfAbsent(updateModel.Address.Postcode);

        // Update fields if they have been set (i.e. not null) in updateModel
        // Otherwise use the value we currently have in the database
        dbModel.AddressLine1 = updateModel.Address.AddressLine1 ?? dbModel.AddressLine1;
        dbModel.AddressLine2 = updateModel.Address.AddressLine2 ?? dbModel.AddressLine2;
        dbModel.AddressLine3 = updateModel.Address.AddressLine3 ?? dbModel.AddressLine3;
        dbModel.TownOrCity = updateModel.Address.TownOrCity ?? dbModel.TownOrCity;
        dbModel.County = updateModel.Address.County ?? dbModel.County;
        dbModel.PostcodeId = newPostcode?.Postcode ?? dbModel.Postcode?.Postcode;

        dbModel.PropertyType = updateModel.PropertyType ?? dbModel.PropertyType;
        dbModel.NumOfBedrooms = updateModel.NumOfBedrooms ?? dbModel.NumOfBedrooms;

        dbModel.Description = updateModel.Description ?? dbModel.Description;

        dbModel.AcceptsSingleTenant =
            updateModel.LandlordRequirements.AcceptsSingleTenant ?? dbModel.AcceptsSingleTenant;
        dbModel.AcceptsCouple = updateModel.LandlordRequirements.AcceptsCouple ?? dbModel.AcceptsCouple;
        dbModel.AcceptsFamily = updateModel.LandlordRequirements.AcceptsFamily ?? dbModel.AcceptsFamily;
        dbModel.AcceptsPets = updateModel.LandlordRequirements.AcceptsPets ?? dbModel.AcceptsPets;
        dbModel.AcceptsCredit = updateModel.LandlordRequirements.AcceptsCredit ?? dbModel.AcceptsCredit;
        dbModel.AcceptsBenefits = updateModel.LandlordRequirements.AcceptsBenefits ?? dbModel.AcceptsBenefits;
        dbModel.AcceptsNotEET = updateModel.LandlordRequirements.AcceptsNotEET ?? dbModel.AcceptsNotEET;
        dbModel.AcceptsOver35 = updateModel.LandlordRequirements.AcceptsOver35 ?? dbModel.AcceptsOver35;
        dbModel.AcceptsWithoutGuarantor = updateModel.LandlordRequirements.AcceptsWithoutGuarantor ??
                                          dbModel.AcceptsWithoutGuarantor;

        dbModel.Rent = updateModel.Rent ?? dbModel.Rent;

        dbModel.TotalUnits = updateModel.TotalUnits ?? dbModel.TotalUnits;
        dbModel.OccupiedUnits = updateModel.OccupiedUnits ?? dbModel.OccupiedUnits;

        // Occupied state takes precedence over attempted update
        // If no update, fallback to current value as usual
        dbModel.Availability = dbModel.OccupiedUnits == dbModel.TotalUnits
            ? AvailabilityState.Occupied
            : updateModel.Availability ?? dbModel.Availability;

        if (dbModel.Availability == AvailabilityState.AvailableSoon)
        {
            if (updateModel.Availability == AvailabilityState.AvailableSoon)
            {
                // If update succeeds in making the property "available soon", then use its from date
                dbModel.AvailableFrom = updateModel.AvailableFrom;
            }
        }
        else
        {
            // If we're not "available soon" then don't have a from date
            dbModel.AvailableFrom = null;
        }

        dbModel.IsIncomplete = isIncomplete;
        await _dbContext.SaveChangesAsync();
    }

    public void DeleteProperty(PropertyDbModel property)
    {
        _dbContext.Properties.Remove(property);
        _dbContext.SaveChanges();
    }

    public PropertyDbModel? GetPropertyByPropertyId(int propertyId)
    {
        return _dbContext.Properties
            .Include(p => p.Postcode)
            .SingleOrDefault(p => p.Id == propertyId);
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

    public LandlordDbModel GetPropertyOwner(int propertyId)
    {
        return _dbContext.Properties.Include(p => p.Landlord).Single(p => p.Id == propertyId).Landlord;
    }
    
    public async Task<(List<PropertyDbModel> PropertyList, int Count)> GetPropertyList(string? sortBy, string? target, int page, int propPerPage)
    {
        IQueryable<PropertyDbModel> properties;
        switch (sortBy)
        {
            case "Location":
                var postcode = _postcodeService.FormatPostcode(target!);
                if (postcode == "")
                {
                    return (new List<PropertyDbModel>(), 0);
                }

                var postcodeList = new List<string> { postcode };
                await _postcodeService.AddPostcodesToDatabaseIfAbsent(postcodeList);
                var model = _dbContext.Postcodes.SingleOrDefault(p => p.Postcode == postcode);
                if (model?.Location == null)
                {
                    return (new List<PropertyDbModel>(), 0);
                }

                properties = _dbContext.Properties
                    .Include(p => p.Postcode)
                    .Where(p => p.Postcode != null && p.Postcode.Location != null)
                    .OrderBy(p => p.Postcode!.Location!.Distance(model.Location));
                break;
            case "Rent":
                properties = _dbContext.Properties
                    .Include(p => p.Postcode)
                    .OrderBy(m => m.Rent);
                break;
            case "Availability": //TODO once availability state logic is improved (BNH-122), make this a useful sort
                properties = _dbContext.Properties
                    .Include(p => p.Postcode)
                    .OrderBy(m => m.AvailableFrom);
                break;
            default:
                properties = _dbContext.Properties.Include(p => p.Postcode);
                break;
        }

        return (
            await properties
                .Include(t => t.Postcode)
                .Skip((page - 1) * propPerPage)
                .Take(propPerPage)
                .ToListAsync(),
            properties.Count()
        );
    }

    public async Task<(List<PropertyDbModel> PropertyList, int Count)> GetPropertiesByLandlord(int landlordId, int page, int propPerPage)
    {
        var properties = _dbContext.Properties
            .Include(p => p.Postcode)
            .Where(p => p.LandlordId == landlordId)
            .Skip((page - 1) * propPerPage)
            .Take(propPerPage)
            .ToList();
        var propCount = await _dbContext.Properties.Where(p => p.LandlordId == landlordId).CountAsync();
        return (properties, propCount);
    }

    public PropertyCountModel CountProperties(int? landlordId = null)
    {
        if (landlordId == null)
        {
            return new PropertyCountModel
            {
                RegisteredProperties = _dbContext.Properties.Count(),
                LiveProperties = _dbContext.Properties.Count(p =>
                    p.Availability != AvailabilityState.Draft
                    && p.Landlord.CharterApproved),
                AvailableProperties = _dbContext.Properties.Count(p => p.Availability == AvailabilityState.Available)
            };
        }

        var landlord = _dbContext.Landlords.Include(l => l.Properties).SingleOrDefault(l => l.Id == landlordId);
        var properties = landlord?.Properties ?? new List<PropertyDbModel>();
        return new PropertyCountModel
        {
            RegisteredProperties = properties.Count,
            LiveProperties = properties.Count(p =>
                landlord?.CharterApproved == true && p.Availability != AvailabilityState.Draft),
            AvailableProperties = properties.Count(p =>
                p.Availability == AvailabilityState.Available)
        };
    }

    public void CreatePublicViewLink(int propertyId)
    {
        var property = _dbContext.Properties.Single(p => p.Id == propertyId);
        property.PublicViewLink = Guid.NewGuid().ToString();
        _dbContext.SaveChanges();
    }

    public PropertyDbModel? GetPropertyByPublicViewLink(string token)
    {
        return _dbContext.Properties.SingleOrDefault(p => p.PublicViewLink == token);
    }
}