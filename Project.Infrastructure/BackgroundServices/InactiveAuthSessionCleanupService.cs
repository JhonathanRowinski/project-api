using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Project.Application.Auth.Interfaces.Repositories;

/// <summary>Periodically removes revoked and expired authentication sessions.</summary>
public sealed class InactiveAuthSessionCleanupService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<InactiveAuthSessionCleanupService> logger) : BackgroundService
{
    /// <summary>The interval between consecutive cleanup runs.</summary>
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromDays(7);

    /// <summary>
    /// Runs the cleanup process at regular intervals until the application is stopping.
    /// </summary>
    /// <param name="stoppingToken">A token that signals when the host is shutting down.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CleanupInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
            await CleanupInactiveSessionsAsync();
    }

    /// <summary>
    /// Creates a dependency injection scope, removes inactive sessions, and logs the result.
    /// Any errors are logged without interrupting the background service.
    /// </summary>
    private async Task CleanupInactiveSessionsAsync()
    {
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var authSessionRepository = scope.ServiceProvider.GetRequiredService<IAuthSessionRepository>();
            var removedSessionsCount = await authSessionRepository.RemoveInactiveSessionsAsync(DateTime.UtcNow);

            logger.LogInformation(
                "Removed {RemovedSessionsCount} inactive authentication sessions.",
                removedSessionsCount);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to remove inactive authentication sessions.");
        }
    }
}
