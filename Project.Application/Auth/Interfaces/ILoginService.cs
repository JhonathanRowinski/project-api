using Project.Application.Auth.Models;

namespace Project.Application.Auth.Interfaces;

/// <summary>
/// Defines the operations available for authenticating users
/// and managing their authentication sessions.
/// </summary>
public interface ILoginService
{
    /// <summary>Authenticates a user and creates a new session.</summary>
    /// <param name="email">The email address associated with the account.</param>
    /// <param name="password">The password provided by the user.</param>
    /// <returns>The access and refresh tokens issued for the new session.</returns>
    Task<AuthTokens> LoginAsync(string email, string password);

    /// <summary>Renews an active session by replacing its refresh token and issuing a new access token.</summary>
    /// <param name="refreshToken">The refresh token currently held by the client.</param>
    /// <returns>The newly issued access and refresh tokens.</returns>
    Task<AuthTokens> RefreshAsync(string refreshToken);

    /// <summary>Ends an authentication session using its identifier.</summary>
    /// <param name="sessionId">The identifier of the session to revoke.</param>
    /// <returns>A task that represents the session revocation operation.</returns>
    Task RevokeSessionAsync(string? sessionId);
}