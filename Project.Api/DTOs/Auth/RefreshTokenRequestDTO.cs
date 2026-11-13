using System.ComponentModel.DataAnnotations;

namespace Project.Api.DTOs.Auth;

/// <summary>Contains a refresh token submitted to renew an authentication session.</summary>
public sealed record RefreshTokenRequestDTO
{
    [Required]
    /// <summary>Gets the refresh token to rotate.</summary>
    public required string RefreshToken { get; init; }
}
