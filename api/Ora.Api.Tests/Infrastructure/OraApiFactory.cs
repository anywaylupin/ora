using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Ora.Api.Commands;
using Testcontainers.MsSql;

[assembly: AssemblyFixture(typeof(Ora.Api.Tests.Infrastructure.OraApiFactory))]

namespace Ora.Api.Tests.Infrastructure;

/// <summary>
/// Hosts the real API against a real SQL Server, shared by every test in the run.
/// </summary>
/// <remarks>
/// Tests isolate themselves by signing up fresh users and workspaces, so the database never needs resetting between them.
/// </remarks>
public sealed class OraApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-latest").Build();

    public async ValueTask InitializeAsync()
    {
        await _sql.StartAsync();
        await DatabaseCommands.MigrateAsync(Services);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _sql.DisposeAsync();
    }

    /// <summary>
    /// The Testing environment keeps developer user secrets out of the run.
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", $"{_sql.GetConnectionString()};Database=OraTests");
        builder.UseSetting("Cors:AllowedOrigins", "http://localhost:5173");
    }

    /// <summary>
    /// Opens a client with no credentials, for anonymous calls and for signing in.
    /// </summary>
    public OraClient CreateOraClient() => new(CreateClient());

    /// <summary>
    /// Runs code against the database with the workspace filters lifted, for arranging and asserting state.
    /// </summary>
    public async Task<T> WithDatabaseAsync<T>(Func<Ora.Api.Data.OraDbContext, Task<T>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        await using var db = DatabaseCommands.CreateUnrestrictedContext(scope.ServiceProvider);
        return await action(db);
    }
}
