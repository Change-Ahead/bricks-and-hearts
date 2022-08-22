using BricksAndHearts.Database;
using Microsoft.EntityFrameworkCore;

namespace BricksAndHearts.Services;

public interface ITenantService
{
    public Task<(List<TenantDbModel> TenantList, int Count)> SortTenantsByLocation(string postalCode, int page, int tenantsPerPage);
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

    public async Task<(List<TenantDbModel> TenantList, int Count)> SortTenantsByLocation(string postalCode, int page, int tenantsPerPage)
    {
        var postcode = _postcodeService.FormatPostcode(postalCode);
        var postcodeList = new List<string> { postcode };
        await _postcodeService.AddPostcodesToDatabaseIfAbsent(postcodeList);
        var targetLocation = _dbContext.Postcodes.SingleOrDefault(p => p.Postcode == postcode);
        if (targetLocation?.Location == null)
        {
            return (new List<TenantDbModel>(), 0);
        }

        var tenants = _dbContext.Tenants
            .Include(t => t.Postcode)
            .Where(t => t.Postcode != null)
            .OrderBy(p =>   p.Postcode!.Location!.Distance(targetLocation.Location))
            .Skip(tenantsPerPage * (page - 1))
            .Take(tenantsPerPage);
        return (await tenants.ToListAsync(), _dbContext.Tenants.Count());
    }
}