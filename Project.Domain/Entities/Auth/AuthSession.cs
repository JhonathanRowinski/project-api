namespace Project.Domain.Entities.Auth;

/// <summary>Represents an account authentication session, including its refresh token, expiration limits, and client information.</summary>
/// <param name="Id">The session's unique identifier.</param>
/// <param name="AccountId">The identifier of the account associated with the session.</param>
/// <param name="RefreshTokenHash">The hash of the current refresh token.</param>
/// <param name="CreatedAtUtc">The date and time when the session was created, in UTC.</param>
/// <param name="RefreshTokenExpiresAtUtc">The date and time when the current refresh token expires, in UTC.</param>
/// <param name="AbsoluteExpiresAtUtc">The maximum date and time the session can remain active, in UTC.</param>
/// <param name="RevokedAtUtc">The date and time when the session was revoked, or <see langword="null"/> if it has not been revoked.</param>
/// <param name="Country">The approximate country associated with the client's IP address, if available.</param>
/// <param name="CountryCode">The ISO country code associated with the client's IP address, if available.</param>
/// <param name="State">The approximate state or region associated with the client's IP address, if available.</param>
/// <param name="StateCode">The state or region code associated with the client's IP address, if available.</param>
/// <param name="City">The approximate city associated with the client's IP address, if available.</param>
/// <param name="Browser">The browser identified from the client's User-Agent string, if available.</param>
/// <param name="BrowserVersion">The browser version identified from the client's User-Agent string, if available.</param>
/// <param name="OperatingSystem">The operating system identified from the client's User-Agent string, if available.</param>
/// <param name="OperatingSystemVersion">The operating system version identified from the client's User-Agent string, if available.</param>
/// <param name="DeviceType">The device type identified from the client's User-Agent string, such as desktop, mobile, or tablet.</param>
public sealed record AuthSession(
    Guid Id,
    Guid AccountId,
    string RefreshTokenHash,
    DateTime CreatedAtUtc,
    DateTime RefreshTokenExpiresAtUtc,
    DateTime AbsoluteExpiresAtUtc,
    DateTime? RevokedAtUtc = null,
    string? Country = null,
    string? CountryCode = null,
    string? State = null,
    string? StateCode = null,
    string? City = null,
    string? Browser = null,
    string? BrowserVersion = null,
    string? OperatingSystem = null,
    string? OperatingSystemVersion = null,
    string? DeviceType = null)
{
    /// <summary>Checks whether the session has not been revoked and both expiration limits are still valid.</summary>
    public bool IsActive(DateTime nowUtc) =>
        RevokedAtUtc is null && RefreshTokenExpiresAtUtc > nowUtc && AbsoluteExpiresAtUtc > nowUtc;
}
