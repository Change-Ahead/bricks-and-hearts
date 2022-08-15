using BricksAndHearts.Database;
using Microsoft.EntityFrameworkCore;

namespace BricksAndHearts.Services;

public interface ITenantService
{
    public Task<List<TenantDbModel>?> SortTenantsByLocation(string postalCode, int page, int perPage);
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

    
    public async Task<List<TenantDbModel>?> SortTenantsByLocation(string postalCode, int page, int perPage)
    {
        var postcode = _postcodeService.FormatPostcode(postalCode);
        await _postcodeService.AddSinglePostcodeToDatabaseIfAbsent(postcode);
        var model = _dbContext.Postcodes.SingleOrDefault(p => p.Postcode == postcode);
        if (model == null || model.Lat == null || model.Lon == null)
        {
            return null;
        }

        var tenants = _dbContext.Tenants
            .FromSqlInterpolated(
                @$"SELECT *, (
                  6371 * acos (
                  cos ( radians({model.Lat}) )
                  * cos( radians( Lat ) )
                  * cos( radians( Lon ) - radians({model.Lon}) )
                  + sin ( radians({model.Lat}) )
                  * sin( radians( Lat ) )
                    )
                ) AS distance 
                FROM dbo.Tenant
                WHERE Lon is not NULL and Lat is not NULL
                ORDER BY distance
                OFFSET {perPage * (page - 1)} ROWS
                FETCH NEXT {perPage} ROWS ONLY
                "
            );
        return await tenants.ToListAsync();
    }
}