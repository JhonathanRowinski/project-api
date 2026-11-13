namespace Project.Api.DTOs.Auth;

/// <summary>Defines the token payload returned after login or refresh.</summary>
    /// <param name="AccessToken">The signed JWT used to authenticate API requests.</param>
    /// <param name="RefreshToken">The token used to obtain a new token pair.</param>
    /// <param name="TokenType">The authorization scheme for the access token.</param>
    /// <param name="AccessTokenExpiresAtUtc">The UTC expiration time of the access token.</param>
    /// <param name="RefreshTokenExpiresAtUtc">The UTC expiration time of the refresh token.</param>
public sealed record LoginResponseDTO(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    DateTime AccessTokenExpiresAtUtc,
    DateTime RefreshTokenExpiresAtUtc);
