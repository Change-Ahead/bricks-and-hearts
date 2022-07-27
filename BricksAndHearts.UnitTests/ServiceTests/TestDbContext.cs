using BricksAndHearts.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BricksAndHearts.UnitTests.ServiceTests;

public class TestDbContext : BricksAndHeartsDbContext
{
    private readonly IConfiguration _config;

    public TestDbContext(IConfiguration config) : base(config)
    {
        _config = config;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(_config["TestDBConnectionString"]);
    }
}
