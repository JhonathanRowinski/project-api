using Project.Application.Auth.Models;
using Project.Domain.Entities.Accounts;

namespace Project.Application.Auth.Interfaces.Tokens;

/// <summary>Defines how access tokens are created for authenticated users.</summary>
public interface IJwtTokenGenerator
{
    /// <summary>Creates a signed access token linked to an account and its active session.</summary>
    /// <param name="account">The account associated with the token.</param>
    /// <param name="sessionId">The identifier of the session the token belongs to.</param>
    /// <param name="sessionExpiresAtUtc">The time when the session expires, which limits the token's validity.</param>
    /// <returns>The generated access token and its expiration time.</returns>
    AccessToken GenerateAccessToken(Account account, Guid sessionId, DateTime sessionExpiresAtUtc);
}
