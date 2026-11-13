using System.Security.Cryptography;
using System.Text;
using Project.Application.Auth.Interfaces.Tokens;

namespace Project.Infrastructure.Auth.Tokens;

/// <summary>Generates secure refresh tokens and creates hashes for their storage.</summary>
public sealed class RefreshTokenGenerator : IRefreshTokenGenerator
{
    /// <summary>
    /// Generates a cryptographically secure random refresh token and encodes it
    /// using URL-safe Base64.
    /// </summary>
    /// <returns>The generated refresh token.</returns>
    public string GenerateToken()
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(64);

        return Convert.ToBase64String(tokenBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <summary>
    /// Creates a SHA-256 hash of a refresh token so the original value does not
    /// need to be stored in the database.
    /// </summary>
    /// <param name="refreshToken">The original refresh token.</param>
    /// <returns>The token hash represented as an uppercase hexadecimal string.</returns>
    public string HashToken(string refreshToken)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(refreshToken);
        var hashBytes = SHA256.HashData(tokenBytes);

        return Convert.ToHexString(hashBytes);
    }
}
