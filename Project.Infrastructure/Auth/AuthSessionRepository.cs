using Microsoft.EntityFrameworkCore;
using Project.Application.Auth.Interfaces.Repositories;
using Project.Domain.Entities.Auth;

namespace Project.Infrastructure.Auth;

/// <summary>Stores and manages authentication sessions in the application database.</summary>
public sealed class AuthSessionRepository(ProjectDbContext dbContext) : IAuthSessionRepository
{
    /// <summary>Adds a new authentication session to the database.</summary>
    /// <param name="session">The session to persist.</param>
    public async Task AddAsync(AuthSession session)
    {
        await dbContext.AuthSessions.AddAsync(session);
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Finds a session using its refresh token hash without tracking it in the current DbContext.
    /// </summary>
    /// <param name="refreshTokenHash">The hash of the refresh token associated with the session.</param>
    /// <returns>The matching session, or <see langword="null"/> if no session is found.</returns>
    public Task<AuthSession?> FindByRefreshTokenHashAsync(string refreshTokenHash) =>
        dbContext.AuthSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(session => session.RefreshTokenHash == refreshTokenHash);

    /// <summary>
    /// Replaces the refresh token hash and expiration time only if the current token still belongs
    /// to an active session.
    /// </summary>
    /// <param name="currentRefreshTokenHash">The hash of the refresh token currently associated with the session.</param>
    /// <param name="newRefreshTokenHash">The hash of the replacement refresh token.</param>
    /// <param name="nowUtc">The current date and time, in UTC, used to check whether the session is still valid.</param>
    /// <param name="newExpiresAtUtc">The expiration time assigned to the replacement refresh token, in UTC.</param>
    /// <returns><see langword="true"/> if exactly one session was updated; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> RotateRefreshTokenAsync(
        string currentRefreshTokenHash,
        string newRefreshTokenHash,
        DateTime nowUtc,
        DateTime newExpiresAtUtc)
    {
        var updatedRows = await dbContext.AuthSessions
            .Where(session =>
                session.RefreshTokenHash == currentRefreshTokenHash &&
                session.RevokedAtUtc == null &&
                session.RefreshTokenExpiresAtUtc > nowUtc &&
                session.AbsoluteExpiresAtUtc > nowUtc)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(session => session.RefreshTokenHash, newRefreshTokenHash)
                .SetProperty(session => session.RefreshTokenExpiresAtUtc, newExpiresAtUtc));

        return updatedRows == 1;
    }

    /// <summary>
    /// Revokes a session only if it has not already been revoked and both expiration limits are still valid.
    /// </summary>
    /// <param name="sessionId">The identifier of the session to revoke.</param>
    /// <param name="nowUtc">The current date and time, in UTC, used to check whether the session is still valid.</param>
    /// <returns><see langword="true"/> if exactly one session was revoked; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> RevokeBySessionIdAsync(Guid sessionId, DateTime nowUtc)
    {
        var updatedRows = await dbContext.AuthSessions
            .Where(session =>
                session.Id == sessionId &&
                session.RevokedAtUtc == null &&
                session.RefreshTokenExpiresAtUtc > nowUtc &&
                session.AbsoluteExpiresAtUtc > nowUtc)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(session => session.RevokedAtUtc, nowUtc));

        return updatedRows == 1;
    }

    /// <summary>Checks whether a session exists and is still active.</summary>
    /// <param name="sessionId">The identifier of the session to check.</param>
    /// <param name="nowUtc">The current date and time, in UTC, used to evaluate the session's validity.</param>
    /// <returns><see langword="true"/> if the session is active; otherwise, <see langword="false"/>.</returns>
    public Task<bool> IsActiveAsync(Guid sessionId, DateTime nowUtc) =>
        dbContext.AuthSessions.AnyAsync(session =>
            session.Id == sessionId &&
            session.RevokedAtUtc == null &&
            session.RefreshTokenExpiresAtUtc > nowUtc &&
            session.AbsoluteExpiresAtUtc > nowUtc);

    /// <summary>Deletes all sessions that have been revoked or have reached either expiration limit.</summary>
    /// <param name="nowUtc">The current date and time, in UTC, used to identify inactive sessions.</param>
    /// <returns>The number of sessions removed from the database.</returns>
    public Task<int> RemoveInactiveSessionsAsync(DateTime nowUtc) =>
        dbContext.AuthSessions
            .Where(session =>
                session.RevokedAtUtc != null ||
                session.RefreshTokenExpiresAtUtc <= nowUtc ||
                session.AbsoluteExpiresAtUtc <= nowUtc)
            .ExecuteDeleteAsync();
}
