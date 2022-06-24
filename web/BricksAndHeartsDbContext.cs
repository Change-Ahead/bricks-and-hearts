using BricksAndHearts.Models;
using Microsoft.EntityFrameworkCore;

namespace BricksAndHearts
{
    public class BricksAndHeartsDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=(local);Database=BricksAndHearts;Trusted_Connection=True;Integrated Security=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }

        public DbSet<LandlordDbModel> Landlords { get; set; } = null!;
    } 
}
