using Project.Domain.Entities.Accounts;

namespace Project.Application.Auth.Interfaces.Repositories;

/// <summary>Defines how account information is retrieved during authentication.</summary>
public interface IAccountRepository
{
    /// <summary>Finds an account using its normalized email address.</summary>
    /// <param name="normalizedEmail">The email address normalized before the lookup.</param>
    /// <returns>The matching account, or <see langword="null"/> if no account is found.</returns>
    Task<Account?> FindByEmailAsync(string normalizedEmail);

    /// <summary>Finds an account using its unique identifier.</summary>
    /// <param name="accountId">The identifier of the account to retrieve.</param>
    /// <returns>The matching account, or <see langword="null"/> if no account is found.</returns>
    Task<Account?> FindByIdAsync(Guid accountId);
}
