using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Project.Application.Auth.Options;
using Project.Domain.Entities.Accounts;
using Project.Infrastructure.Auth;
using Project.Infrastructure.Auth.Tokens;
using Project.Infrastructure.Accounts;
using Project.Infrastructure.Database;
using Project.Infrastructure.Auth.Origin;
using Project.Application.Auth.Interfaces.Repositories;
using Project.Application.Auth.Interfaces.Origin;
using Project.Application.Auth.Interfaces.Tokens;

namespace Project.Infrastructure;

/// <summary>Registers infrastructure services and persistence dependencies.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the infrastructure dependencies, including database access, configuration options,
    /// repositories, token services, client information services, and background services.
    /// </summary>
    /// <param name="services">The service collection to register dependencies with.</param>
    /// <param name="configuration">The application configuration used to load connection strings and options.</param>
    /// <returns>The service collection with the infrastructure dependencies registered.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ProjectDb");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Configure the ConnectionStrings:ProjectDb connection string.");

        services.AddDbContext<ProjectDbContext>(options => options.UseSqlServer(connectionString));
        services.AddSingleton<DatabaseScriptRunner>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "JWT issuer is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "JWT audience is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.SigningKey) && Encoding.UTF8.GetByteCount(options.SigningKey) >= 32, "JWT signing key must contain at least 32 bytes.")
            .Validate(options => options.AccessTokenLifetimeMinutes is > 0 and <= 60, "JWT lifetime must be between 1 and 60 minutes.")
            .ValidateOnStart();

        services.AddOptions<AuthTokenOptions>()
            .Bind(configuration.GetSection(AuthTokenOptions.SectionName))
            .Validate(options => options.RefreshTokenLifetimeDays is > 0 and <= 3, "Refresh token lifetime must be between 1 and 3 days.")
            .Validate(options => options.SessionLifetimeDays >= options.RefreshTokenLifetimeDays && options.SessionLifetimeDays <= 7, "Session lifetime must be at least the refresh token lifetime and no more than 7 days.")
            .ValidateOnStart();

        services.AddOptions<ClientIpOptions>()
            .Bind(configuration.GetSection(ClientIpOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<IpGeolocationOptions>()
            .Bind(configuration.GetSection(IpGeolocationOptions.SectionName))
            .ValidateOnStart();

        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IAuthSessionRepository, AuthSessionRepository>();
        services.AddHostedService<InactiveAuthSessionCleanupService>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddScoped<IPasswordHasher<Account>, PasswordHasher<Account>>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddHttpContextAccessor();
        services.AddScoped<IClientIpService, ClientIpService>();
        services.AddScoped<IHttpRequestInformationService, HttpRequestInformationService>();
        services.AddSingleton<IIpGeolocationService, IpGeolocationService>();
        services.AddSingleton<IUserAgentParser, UserAgentParser>();

        return services;
    }
}
