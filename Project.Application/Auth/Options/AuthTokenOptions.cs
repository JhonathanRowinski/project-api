namespace Project.Application.Auth.Options;

/// <summary>Defines how long refresh tokens and authentication sessions remain valid.</summary>
public sealed class AuthTokenOptions
{
    /// <summary>The configuration section used to load these settings.</summary>
    public const string SectionName = "Authentication:Jwt";

    /// <summary>The number of days a refresh token remains valid.</summary>
    public int RefreshTokenLifetimeDays { get; init; } = 3;

    /// <summary>The maximum number of days an authentication session can remain active.</summary>
    public int SessionLifetimeDays { get; init; } = 7;
}
