using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ProjectHub.Data;

/// <summary>
/// Registers Data-layer persistence services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the ProjectHub database context and PostgreSQL pgvector support.
    /// </summary>
    /// <param name="services">The dependency injection service collection.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddData(
        this IServiceCollection services,
        string connectionString
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<ProjectHubDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseVector())
        );
        services.AddScoped<DbContext>(provider =>
            provider.GetRequiredService<ProjectHubDbContext>()
        );

        return services;
    }
}
