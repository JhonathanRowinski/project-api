namespace Project.Application.Auth.Models;

/// <summary>Groups the access and refresh tokens issued for an authentication session.</summary>
/// <param name="AccessToken">The signed access token and its expiration time.</param>
/// <param name="RefreshToken">The refresh token returned to the client.</param>
/// <param name="RefreshTokenExpiresAtUtc">The date and time when the refresh token expires, in UTC.</param>
public sealed record AuthTokens(
    AccessToken AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);
