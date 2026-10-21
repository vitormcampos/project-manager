using Microsoft.EntityFrameworkCore;
using ProjectHub.Data;
using Testcontainers.PostgreSql;

namespace ProjectHub.Application.Tests;

public sealed class PostgresTestDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("pgvector/pgvector:pg16")
        .WithDatabase("projecthub_application_tests")
        .WithUsername("projecthub")
        .WithPassword("projecthub")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE \"UserInvitations\", \"Users\", \"Accounts\" CASCADE;"
        );
    }

    public ProjectHubDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ProjectHubDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        return new ProjectHubDbContext(options);
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();
}
