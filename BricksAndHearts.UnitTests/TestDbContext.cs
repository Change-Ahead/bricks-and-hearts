using BricksAndHearts.Database;
using Microsoft.EntityFrameworkCore;

namespace BricksAndHearts.UnitTests;

public class TestDbContext : BricksAndHeartsDbContext
{
    private const string ConnectionString =
        "Server=(local);Database=BricksAndHeartsTest;Trusted_Connection=True;Integrated Security=True;";

    public TestDbContext() : base(null!)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(ConnectionString);
    }
}
