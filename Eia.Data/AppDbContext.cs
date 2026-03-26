using Eia.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Eia.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<NuclearOutage> NuclearOutages => Set<NuclearOutage>();
        public DbSet<ExtractionRun> ExtractionRuns => Set<ExtractionRun>();
        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ExtractionRun>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.ExtractedAt).IsRequired();
                e.Property(x => x.Status).IsRequired().HasMaxLength(20);
                e.Property(x => x.ErrorMessage).HasMaxLength(500);
            });

            modelBuilder.Entity<NuclearOutage>(e =>
            {
                e.HasKey(x => x.Id);

                e.HasIndex(x => x.Period).IsUnique();
                e.Property(x => x.Period).IsRequired().HasMaxLength(10);

                e.HasOne(x => x.ExtractionRun)
                 .WithMany(r => r.NuclearOutages)
                 .HasForeignKey(x => x.ExtractionRunId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<User>(e =>
            {
                e.HasKey(x => x.Id);

                e.HasIndex(x => x.Email).IsUnique();
                e.Property(x => x.Email).IsRequired().HasMaxLength(200);
                e.Property(x => x.PasswordHash).IsRequired();
                e.Property(x => x.Role).IsRequired().HasMaxLength(20);
            });
        }
    }
}