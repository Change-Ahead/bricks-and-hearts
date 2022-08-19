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
        if (targetLocation?.Lat == null || targetLocation.Lon == null)
        {
            return (new List<TenantDbModel>(), 0);
        }

        var tenants = _dbContext.Tenants
            .FromSqlInterpolated(
                @$"SELECT *, (
                  6371 * acos (
                  cos ( radians({targetLocation.Lat}) )
                  * cos( radians( Lat ) )
                  * cos( radians( Lon ) - radians({targetLocation.Lon}) )
                  + sin ( radians({targetLocation.Lat}) )
                  * sin( radians( Lat ) )
                    )
                ) AS distance 
                FROM dbo.Tenant
                WHERE Lon is not NULL and Lat is not NULL
                ORDER BY distance
                OFFSET 0 ROWS
                ");
        return (await tenants.Skip((page - 1) * tenantsPerPage).Take(tenantsPerPage).ToListAsync(), tenants.Count());
    }
}