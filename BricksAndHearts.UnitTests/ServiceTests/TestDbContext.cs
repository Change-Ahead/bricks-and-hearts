using System;
using System.Threading;
using System.Threading.Tasks;
using BricksAndHearts.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BricksAndHearts.UnitTests.ServiceTests;

public class TestDbContext : BricksAndHeartsDbContext
{
    private readonly IConfiguration _config;
    private readonly bool _isReadOnly;

    public TestDbContext(IConfiguration config, bool isReadOnly) : base(config)
    {
        _config = config;
        _isReadOnly = isReadOnly;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(_config["TestDBConnectionString"]);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        if (_isReadOnly)
        {
            throw new InvalidOperationException("This TestDbContext is read only");
        }

        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        if (_isReadOnly)
        {
            throw new InvalidOperationException("This TestDbContext is read only");
        }

        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}