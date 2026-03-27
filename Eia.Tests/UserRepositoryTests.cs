using Eia.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Eia.Tests;

[TestClass]
public class UserRepositoryTests : DbTestBase
{
    private UserRepository Repo => new(Context);

    [TestMethod]
    public async Task CreatesAdmin_WithHashedPassword()
    {
        await Repo.SeedDefaultAdminAsync("admin@test.com", "Secret123!");

        var user = await Context.Users.FirstOrDefaultAsync();
        Assert.IsNotNull(user);
        Assert.AreEqual("Admin", user.Role);
        Assert.IsTrue(user.IsActive);
        Assert.AreNotEqual("Secret123!", user.PasswordHash);
        Assert.IsTrue(BCrypt.Net.BCrypt.Verify("Secret123!", user.PasswordHash));
    }
}