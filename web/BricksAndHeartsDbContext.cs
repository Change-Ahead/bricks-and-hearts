using BricksAndHearts.Models;
using Microsoft.EntityFrameworkCore;

namespace BricksAndHearts;

public class BricksAndHeartsDbContext : DbContext
{
    private readonly IConfiguration _config;

    public BricksAndHeartsDbContext(IConfiguration config)
    {
        _config = config;
    }

    public DbSet<LandlordDbModel> Landlords { get; set; } = null!;
    public DbSet<UserDbModel> Users { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(_config.GetValue<string>("DBConnectionString"));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserDbModel>()
            .HasIndex(u => u.GoogleAccountId)
            .IsUnique();
    }
}