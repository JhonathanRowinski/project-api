using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Project.Application.Auth.Models;
using Project.Domain.Entities.Accounts;
using Project.Application.Auth.Interfaces.Tokens;

namespace Project.Infrastructure.Auth.Tokens;

/// <summary>Generates signed JWT access tokens for authenticated accounts and their sessions.</summary>
public sealed class JwtTokenGenerator(IOptions<JwtOptions> options) : IJwtTokenGenerator
{
    private readonly JwtOptions _options = options.Value;

    /// <summary>
    /// Creates a signed JWT containing the account and session identifiers, with an expiration time
    /// that does not exceed the session's absolute expiration.
    /// </summary>
    /// <param name="account">The account associated with the token.</param>
    /// <param name="sessionId">The identifier of the active authentication session.</param>
    /// <param name="sessionExpiresAtUtc">The absolute expiration time of the session, in UTC.</param>
    /// <returns>The generated access token and its expiration time.</returns>
    public AccessToken GenerateAccessToken(Account account, Guid sessionId, DateTime sessionExpiresAtUtc)
    {
        var issuedAtUtc = DateTime.UtcNow;
        var configuredExpiryUtc = issuedAtUtc.AddMinutes(_options.AccessTokenLifetimeMinutes);
        var expiresAtUtc = configuredExpiryUtc < sessionExpiresAtUtc ? configuredExpiryUtc : sessionExpiresAtUtc;
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, account.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("sid", sessionId.ToString())
        };

        var jwt = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: issuedAtUtc,
            expires: expiresAtUtc,
            signingCredentials: credentials);
        var token = new JwtSecurityTokenHandler().WriteToken(jwt);

        return new AccessToken(token, expiresAtUtc);
    }
}
