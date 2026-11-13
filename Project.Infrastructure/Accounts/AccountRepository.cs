using Microsoft.EntityFrameworkCore;
using Project.Application.Auth.Interfaces.Repositories;
using Project.Domain.Entities.Accounts;

namespace Project.Infrastructure.Accounts;

/// <summary>Retrieves account information from the application database.</summary>
public sealed class AccountRepository(ProjectDbContext dbContext) : IAccountRepository
{
    /// <summary>
    /// Finds an account by its normalized email address without tracking it in the current DbContext.
    /// </summary>
    /// <param name="normalizedEmail">The normalized email address to search for.</param>
    /// <returns>The matching account, or <see langword="null"/> if no account is found.</returns>
    public Task<Account?> FindByEmailAsync(string normalizedEmail) =>
        dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(account => account.Email == normalizedEmail);

    /// <summary>
    /// Finds an account by its unique identifier without tracking it in the current DbContext.
    /// </summary>
    /// <param name="accountId">The identifier of the account to find.</param>
    /// <returns>The matching account, or <see langword="null"/> if no account is found.</returns>
    public Task<Account?> FindByIdAsync(Guid accountId) =>
        dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(account => account.Id == accountId);
}
