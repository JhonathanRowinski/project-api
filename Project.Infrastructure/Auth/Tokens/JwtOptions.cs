namespace Project.Infrastructure.Auth.Tokens;

/// <summary>Defines the settings used to create and sign JWT access tokens.</summary>
public sealed class JwtOptions
{
    /// <summary>The configuration section used to load these settings.</summary>
    public const string SectionName = "Authentication:Jwt";

    /// <summary>The issuer identifier included in generated tokens.</summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>The intended audience for generated tokens.</summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>The secret key used to sign access tokens.</summary>
    public string SigningKey { get; init; } = string.Empty;

    /// <summary>The number of minutes an access token remains valid.</summary>
    public int AccessTokenLifetimeMinutes { get; init; } = 15;
}
