using Microsoft.EntityFrameworkCore;

namespace ProjectHub.Data;

/// <summary>
/// Entity Framework Core database context for ProjectHub persistence.
/// </summary>
public sealed class ProjectHubDbContext(DbContextOptions<ProjectHubDbContext> options)
    : DbContext(options)
{
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        base.OnModelCreating(modelBuilder);
    }
}
