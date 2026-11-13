using System.ComponentModel.DataAnnotations;

namespace Project.Api.DTOs.Auth;

/// <summary>Contains the credentials and client identity submitted to the login endpoint.</summary>
public sealed record LoginRequestDTO
{
    [Required]
    [EmailAddress]
    /// <summary>Gets the account email address.</summary>
    public required string Email { get; init; }

    [Required]
    /// <summary>Gets the account password.</summary>
    public required string Password { get; init; }
}