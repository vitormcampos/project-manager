using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Api.Infrastructure;
using ProjectHub.Api.Security;
using ProjectHub.Application;
using ProjectHub.Data;
using ProjectHub.Domain.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
}

builder.Services.AddData(connectionString);
builder.Services.AddApplication();
builder.Services.AddProjectHubSecurity(builder.Configuration);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(
        "login",
        context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }
            )
    );
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (args.Contains("--bootstrap-platform-admin", StringComparer.Ordinal))
{
    var accountName = builder.Configuration["BootstrapPlatformAdmin:AccountName"];
    var firstName = builder.Configuration["BootstrapPlatformAdmin:FirstName"];
    var lastName = builder.Configuration["BootstrapPlatformAdmin:LastName"];
    var email = builder.Configuration["BootstrapPlatformAdmin:Email"];
    var password = builder.Configuration["BootstrapPlatformAdmin:Password"];
    if (
        string.IsNullOrWhiteSpace(accountName)
        || string.IsNullOrWhiteSpace(firstName)
        || string.IsNullOrWhiteSpace(lastName)
        || string.IsNullOrWhiteSpace(email)
        || string.IsNullOrWhiteSpace(password)
    )
    {
        throw new InvalidOperationException(
            "All BootstrapPlatformAdmin settings must be configured through a secret provider."
        );
    }

    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
    if (await dbContext.Set<Account>().AnyAsync(account => account.Role == AccountRole.Admin))
    {
        throw new InvalidOperationException(
            "A platform-administration account is already provisioned."
        );
    }

    if (password.Length is < 12 or > 128)
    {
        throw new InvalidOperationException(
            "The bootstrap password must contain between 12 and 128 characters."
        );
    }

    var platformAccount = Account.CreatePlatformAccount(accountName);
    var passwordHasher =
        scope.ServiceProvider.GetRequiredService<ProjectHub.Application.Contracts.IPasswordHasher>();
    var platformUser = User.CreateOwner(
        platformAccount.Id,
        firstName,
        lastName,
        email,
        passwordHasher.Hash(password)
    );
    dbContext.Set<Account>().Add(platformAccount);
    dbContext.Set<User>().Add(platformUser);
    await dbContext.SaveChangesAsync();
    return;
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
