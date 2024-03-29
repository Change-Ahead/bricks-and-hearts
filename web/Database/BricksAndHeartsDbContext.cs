using Microsoft.EntityFrameworkCore;

namespace BricksAndHearts.Database;

public class BricksAndHeartsDbContext : DbContext
{
    private readonly IConfiguration _config;

    public BricksAndHeartsDbContext(IConfiguration config)
    {
        _config = config;
    }

    public DbSet<LandlordDbModel> Landlords { get; set; } = null!;
    public DbSet<UserDbModel> Users { get; set; } = null!;
    public DbSet<PropertyDbModel> Properties { get; set; } = null!;
    public DbSet<TenantDbModel> Tenants { get; set; } = null!;
    public DbSet<PostcodeDbModel> Postcodes { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(_config.GetValue<string>("DBConnectionString"), x => x.UseNetTopologySuite());
        optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserDbModel>()
            .HasIndex(u => u.GoogleAccountId)
            .IsUnique();

        modelBuilder.Entity<UserDbModel>()
            .HasOne(u => u.Landlord)
            .WithOne(l => l.User)
            .HasForeignKey<UserDbModel>(u => u.LandlordId);

        modelBuilder.Entity<PropertyDbModel>()
            .HasOne(p => p.Landlord)
            .WithMany(l => l.Properties)
            .HasForeignKey(p => p.LandlordId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PropertyDbModel>()
            .HasCheckConstraint("OccupiedUnits", "[OccupiedUnits] <= [TotalUnits]");
    }

    public static void MigrateDatabase(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dataContext = scope.ServiceProvider.GetRequiredService<BricksAndHeartsDbContext>();
        if (dataContext.Database.GetPendingMigrations().Any())
        {
            app.Logger.LogWarning("Running migrations on startup");
            dataContext.Database.Migrate();
        }
    }
}