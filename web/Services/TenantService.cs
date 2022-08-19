using BricksAndHearts.Database;
using BricksAndHearts.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BricksAndHearts.Services;

public interface ITenantService
{
    public TenantCountModel CountTenants();
    public Task<List<TenantDbModel>?> FilterNearestTenantsToProperty(HousingRequirementModel filters, bool isMatching, string postcode, int currentPage, int tenantsPerPage);
    public Task<List<TenantDbModel>?> GetNearestTenantsToProperty(PropertyViewModel currentProperty);
}

public class TenantService : ITenantService
{
    private readonly BricksAndHeartsDbContext _dbContext;
    private readonly IPostcodeService _postcodeService;

    public TenantService(BricksAndHeartsDbContext dbContext, IPostcodeService postcodeService)
    {
        _dbContext = dbContext;
        _postcodeService = postcodeService;
    }
    
    public TenantCountModel CountTenants()
    {
        return new TenantCountModel
        {
            RegisteredTenants = _dbContext.Tenants.Count(),
            LocatedTenants = _dbContext.Tenants.Count(t => t.Postcode != null)
        };
    }

    public async Task<(IQueryable<TenantDbModel>?, int Count)> SortTenantsByLocationAndFilter(HousingRequirementModel filters, bool isMatching, string postalCode, int page, int tenantsPerPage)
    {
        var postcode = _postcodeService.FormatPostcode(postalCode);
        var postcodeList = new List<string> { postcode };
        await _postcodeService.AddPostcodesToDatabaseIfAbsent(postcodeList);
        var targetLocation = _dbContext.Postcodes.SingleOrDefault(p => p.Postcode == postcode);
        var tenantQuery = GetFilteredTenantQuery(filters, isMatching);
        if (targetLocation?.Location == null)
        {
            return (tenantQuery, 0);
        }
        
        var tenants = _dbContext.Tenants
            .Include(t => t.Postcode)
            .Where(t => t.Postcode != null)
            .OrderBy(p =>   p.Postcode!.Location!.Distance(targetLocation.Location))
            .Skip(tenantsPerPage * (page - 1))
            .Take(tenantsPerPage);
        return (await tenants.ToListAsync(), _dbContext.Tenants.Count());
    }
    
    public async Task<List<TenantDbModel>?> GetNearestTenantsToProperty(PropertyViewModel currentProperty)
    {
        var sortedFilteredTenantQuery = (await SortTenantsByLocationAndFilter(currentProperty.LandlordRequirements, true, currentProperty.Address.Postcode!, 1, 1000000000))!.Take(5);
        return await sortedFilteredTenantQuery.ToListAsync();
    }

    private IQueryable<TenantDbModel> GetFilteredTenantQuery(HousingRequirementModel filters, bool isMatching)
    {
        var tenantQuery = _dbContext.Tenants
            .Include(p => p.Postcode)
            .AsQueryable();
        tenantQuery = tenantQuery.Where(t => (t.Type == "Single" && filters.AcceptsSingleTenant == true) 
                                             || (t.Type == "Couple" && filters.AcceptsCouple == true) 
                                             || (t.Type == "Family" && filters.AcceptsFamily == true) 
                                             || (filters.AcceptsSingleTenant != true && filters.AcceptsCouple != true && filters.AcceptsFamily != true));
        /*the above are INCLUSIVE filters*/
        if (!isMatching)
        {
            return tenantQuery.Where(t => (filters.AcceptsPets == null || t.HasPet == filters.AcceptsPets) 
                                          && (filters.AcceptsNotEET == null || t.ETT == filters.AcceptsNotEET) 
                                          && (filters.AcceptsCredit == null || t.UniversalCredit == filters.AcceptsCredit) 
                                          && (filters.AcceptsBenefits == null || t.HousingBenefits == filters.AcceptsBenefits) 
                                          && (filters.AcceptsOver35 == null || t.Over35 == filters.AcceptsOver35));
            /*Above are EXCLUSIVE filters for the filters page*/
            
        }
        if (filters.AcceptsPets == false)
        {
            tenantQuery = tenantQuery.Where(t => t.HasPet == false);
        }

        if (filters.AcceptsNotEET == false)
        {
            tenantQuery = tenantQuery.Where(t => t.ETT == false);
        }

        if (filters.AcceptsCredit == false)
        {
            tenantQuery = tenantQuery.Where(t => t.UniversalCredit == false);
        }

        if (filters.AcceptsBenefits == false)
        {
            tenantQuery = tenantQuery.Where(t => t.HousingBenefits == false);
        }

        if (filters.AcceptsOver35 == false)
        {
            tenantQuery = tenantQuery.Where(t => t.Over35 == false);
        }
        return tenantQuery;
        /*Above are INCLUSIVE filters for the matching page*/
    }
    
    private async Task<IQueryable<TenantDbModel>?> SortTenantsByLocationAndFilter(HousingRequirementModel filters, bool isMatching, string postalCode, int page, int perPage)
    {
        var postcode = _postcodeService.FormatPostcode(postalCode);
        var postcodeList = new List<string> { postcode };
        await _postcodeService.AddPostcodesToDatabaseIfAbsent(postcodeList);
        var targetLocation = _dbContext.Postcodes.SingleOrDefault(p => p.Postcode == postcode);
        var tenantQuery = GetFilteredTenantQuery(filters, isMatching);
        if (postcode == "")
        {
            return tenantQuery;
        }
        if (targetLocation?.Location == null)
        {
            return (tenantQuery, 0);
        }
        
        var tenants = _dbContext.Tenants
            .Include(t => t.Postcode)
            .Where(t => t.Postcode != null)
            .OrderBy(p =>   p.Postcode!.Location!.Distance(targetLocation.Location))
            .Skip(tenantsPerPage * (page - 1))
            .Take(tenantsPerPage);
        return (await tenants.ToListAsync(), _dbContext.Tenants.Count());
    }
        /*tenants = tenants.Intersect(tenantQuery);
        return tenants;
    }*/
}