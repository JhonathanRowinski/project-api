namespace Project.Application.Auth.Interfaces.Tokens;

/// <summary>Defines how refresh tokens are generated and prepared for secure storage.</summary>
public interface IRefreshTokenGenerator
{
    /// <summary>Creates a new cryptographically secure refresh token.</summary>
    /// <returns>A randomly generated refresh token.</returns>
    string GenerateToken();

    /// <summary>Creates a hash of a refresh token so its original value does not need to be stored.</summary>
    /// <param name="refreshToken">The refresh token to hash.</param>
    /// <returns>The token's hash, used for storage and subsequent lookup.</returns>
    string HashToken(string refreshToken);
}
