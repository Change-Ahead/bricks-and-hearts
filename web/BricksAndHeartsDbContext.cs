using BricksAndHearts.Models;
using Microsoft.EntityFrameworkCore;

namespace BricksAndHearts
{
    public class BricksAndHeartsDbContext : DbContext
    {
        private readonly IConfiguration _config;
		public BricksAndHeartsDbContext(IConfiguration config)
		{
			_config = config;
		}

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_config.GetValue<string>("DBConnectionString"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }

        public DbSet<LandlordDbModel> Landlords { get; set; } = null!;
    } 
}
