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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(_config.GetValue<string>("DBConnectionString"));
        optionsBuilder.LogTo(Console.WriteLine, minimumLevel: LogLevel.Information);
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