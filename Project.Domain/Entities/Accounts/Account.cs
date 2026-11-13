namespace Project.Domain.Entities.Accounts;

/// <summary>Represents a platform account and the credentials used to authenticate it.</summary>
/// <param name="id">The account's unique identifier.</param>
/// <param name="email">The email address associated with the account.</param>
/// <param name="passwordHash">The hashed password stored for authentication.</param>
public sealed class Account(Guid id, string email, string passwordHash)
{
    /// <summary>The account's unique identifier.</summary>
    public Guid Id { get; private set; } = id;

    /// <summary>The email address associated with the account.</summary>
    public string Email { get; private set; } = email;

    /// <summary>The hashed password used to verify login attempts.</summary>
    public string PasswordHash { get; private set; } = passwordHash;
}
