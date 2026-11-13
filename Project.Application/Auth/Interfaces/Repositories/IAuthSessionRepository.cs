using Project.Domain.Entities.Auth;

namespace Project.Application.Auth.Interfaces.Repositories;

/// <summary>Defines how authentication sessions are stored, retrieved, and managed.</summary>
public interface IAuthSessionRepository
{
    /// <summary>Saves a newly created authentication session.</summary>
    /// <param name="session">The session to store.</param>
    /// <returns>A task that represents the save operation.</returns>
    Task AddAsync(AuthSession session);

    /// <summary>Finds the session associated with a refresh token hash.</summary>
    /// <param name="refreshTokenHash">The hash of the refresh token used to identify the session.</param>
    /// <returns>The matching session, or <see langword="null"/> if none is found.</returns>
    Task<AuthSession?> FindByRefreshTokenHashAsync(string refreshTokenHash);

    /// <summary>Replaces the current refresh token hash and updates its expiration time.</summary>
    /// <param name="currentRefreshTokenHash">The hash currently stored for the session.</param>
    /// <param name="newRefreshTokenHash">The hash of the replacement refresh token.</param>
    /// <param name="nowUtc">The current UTC time, used to check whether the session is still valid.</param>
    /// <param name="newExpiresAtUtc">The expiration time to assign to the new refresh token.</param>
    /// <returns><see langword="true"/> if the token was successfully replaced; otherwise, <see langword="false"/>.</returns>
    Task<bool> RotateRefreshTokenAsync(
        string currentRefreshTokenHash,
        string newRefreshTokenHash,
        DateTime nowUtc,
        DateTime newExpiresAtUtc);

    /// <summary>Revokes an active session using its identifier.</summary>
    /// <param name="sessionId">The identifier of the session to revoke.</param>
    /// <param name="nowUtc">The current UTC time, used to check whether the session is still valid.</param>
    /// <returns><see langword="true"/> if the session was successfully revoked; otherwise, <see langword="false"/>.</returns>
    Task<bool> RevokeBySessionIdAsync(Guid sessionId, DateTime nowUtc);

    /// <summary>Checks whether a session is still active.</summary>
    /// <param name="sessionId">The identifier of the session to check.</param>
    /// <param name="nowUtc">The current UTC time used for the expiration check.</param>
    /// <returns><see langword="true"/> if an active session exists; otherwise, <see langword="false"/>.</returns>
    Task<bool> IsActiveAsync(Guid sessionId, DateTime nowUtc);

    /// <summary>Removes revoked or expired sessions from storage.</summary>
    /// <param name="nowUtc">The current UTC time used to identify expired sessions.</param>
    /// <returns>The number of sessions removed.</returns>
    Task<int> RemoveInactiveSessionsAsync(DateTime nowUtc);
}
