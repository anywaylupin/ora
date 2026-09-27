using Microsoft.EntityFrameworkCore;
using Ora.Api.Data;

namespace Ora.Api.Commands;

/// <summary>
/// Maintenance commands that run in place of the web server, so Docker and IIS deployments use the same binary.
/// </summary>
public static class DatabaseCommands
{
    /// <summary>
    /// Applies pending EF Core migrations and returns a process exit code.
    /// </summary>
    public static async Task<int> MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await using var db = CreateUnrestrictedContext(scope.ServiceProvider);
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseCommands));

        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        logger.LogInformation("Applying {Count} pending migrations", pending.Count);
        await db.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Database is up to date");
        return 0;
    }

    /// <summary>
    /// Commands act as the system rather than a user, so they need the workspace filters lifted.
    /// </summary>
    /// <param name="scopedServices">A scoped provider, because EF Core registers context options as scoped.</param>
    internal static OraDbContext CreateUnrestrictedContext(IServiceProvider scopedServices) =>
        new(scopedServices.GetRequiredService<DbContextOptions<OraDbContext>>(), UnrestrictedDataScope.Instance);
}
