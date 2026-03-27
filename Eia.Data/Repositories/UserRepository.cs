using Eia.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Eia.Data.Repositories
{
    public class UserRepository(AppDbContext db)
    {
        public async Task<User?> FindByEmailAsync(string email, CancellationToken ct = default) =>
            await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    u => u.Email == email.ToLower() && u.IsActive, ct);

        /// <summary>
        /// Creates the default admin user if no users exist in the database.
        /// Intended to run once on application startup.
        /// </summary>
        public async Task SeedDefaultAdminAsync(
            string email,
            string plainPassword,
            CancellationToken ct = default)
        {
            var exists = await db.Users.AnyAsync(ct);
            if (exists) return;

            db.Users.Add(new User
            {
                Email = email.ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword),
                Role = "Admin",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            });

            await db.SaveChangesAsync(ct);
        }
    }
}