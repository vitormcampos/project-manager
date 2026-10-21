using Microsoft.Extensions.DependencyInjection;
using ProjectHub.Application.Services;

namespace ProjectHub.Application;

/// <summary>Registers application use-case services.</summary>
public static class DependencyInjection
{
    /// <summary>Adds application services using scoped lifetimes.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<AuthenticationService>();
        services.AddScoped<AccountUserService>();
        return services;
    }
}
