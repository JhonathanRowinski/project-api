using Microsoft.EntityFrameworkCore;
using Project.Domain.Entities.Accounts;
using Project.Domain.Entities.Auth;

namespace Project.Infrastructure;

/// <summary>Provides Entity Framework access to accounts and authentication sessions.</summary>
public sealed class ProjectDbContext(DbContextOptions<ProjectDbContext> options) : DbContext(options)
{
    /// <summary>Provides access to account records in the database.</summary>
    public DbSet<Account> Accounts => Set<Account>();

    /// <summary>Provides access to authentication session records in the database.</summary>
    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();
}
