using Microsoft.EntityFrameworkCore;
using ProjectHub.Domain.Models;

namespace ProjectHub.Data;

/// <summary>
/// Entity Framework Core database context for ProjectHub persistence.
/// </summary>
public sealed class ProjectHubDbContext(DbContextOptions<ProjectHubDbContext> options)
    : DbContext(options)
{
    /// <summary>Gets the tenant accounts.</summary>
    public DbSet<Account> Accounts => Set<Account>();

    /// <summary>Gets the account members and authentication identities.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Gets the one-time user invitations.</summary>
    public DbSet<UserInvitation> UserInvitations => Set<UserInvitation>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProjectHubDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
