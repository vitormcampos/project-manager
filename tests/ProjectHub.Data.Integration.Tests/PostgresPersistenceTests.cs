using Microsoft.EntityFrameworkCore;
using ProjectHub.Data;
using ProjectHub.Domain.Models;
using Testcontainers.PostgreSql;

namespace ProjectHub.Data.Integration.Tests;

public sealed class PostgresPersistenceTests
{
    [Fact]
    public async Task MigrationsCreateSchemaAndEnforceUniqueNormalizedEmail()
    {
        await using var postgres = new PostgreSqlBuilder("pgvector/pgvector:pg16")
            .WithDatabase("projecthub_tests")
            .WithUsername("projecthub")
            .WithPassword("projecthub")
            .Build();
        await postgres.StartAsync();

        var options = new DbContextOptionsBuilder<ProjectHubDbContext>()
            .UseNpgsql(postgres.GetConnectionString())
            .Options;
        await using var dbContext = new ProjectHubDbContext(options);
        await dbContext.Database.MigrateAsync();

        var account = Account.CreateTenant($"Integration-{Guid.NewGuid():N}");
        var email = $"integration-{Guid.NewGuid():N}@example.com";
        var owner = User.CreateOwner(
            account.Id,
            "Integration",
            "Owner",
            email,
            "test-password-hash"
        );
        dbContext.Accounts.Add(account);
        dbContext.Users.Add(owner);
        await dbContext.SaveChangesAsync();

        try
        {
            dbContext.Users.Add(
                User.CreateOwner(
                    Guid.NewGuid(),
                    "Orphan",
                    "User",
                    $"orphan-{Guid.NewGuid():N}@example.com",
                    "test-password-hash"
                )
            );
            await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
            dbContext.ChangeTracker.Clear();

            dbContext.Users.Add(
                User.CreateOwner(account.Id, "Duplicate", "User", email, "test-password-hash")
            );
            await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
        }
        finally
        {
            dbContext.ChangeTracker.Clear();
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM \"Users\" WHERE \"AccountId\" = {account.Id}"
            );
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM \"Accounts\" WHERE \"Id\" = {account.Id}"
            );
        }
    }
}
