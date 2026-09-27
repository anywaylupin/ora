using HotChocolate.Execution;
using HotChocolate.Types.Relay;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ora.Api.Commands;
using Ora.Api.Data;
using Ora.Api.Domain;
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

    private string ConnectionString => $"{_sql.GetConnectionString()};Database=OraTests";

    /// <summary>
    /// Migrates before the host starts, so nothing at startup touches a database that does not exist yet.
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        await _sql.StartAsync();
        await using var db = new OraDbContext(
            new DbContextOptionsBuilder<OraDbContext>().UseSqlServer(ConnectionString).Options,
            UnrestrictedDataScope.Instance);
        await db.Database.MigrateAsync();
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
        builder.UseSetting("ConnectionStrings:Default", ConnectionString);
        builder.UseSetting("Cors:AllowedOrigins", "http://localhost:5173");
    }

    /// <summary>
    /// Opens a client with no credentials, for anonymous calls and for signing in.
    /// </summary>
    public OraClient CreateOraClient() => new(CreateClient());

    /// <summary>
    /// Runs code against the database with the workspace filters lifted, for arranging and asserting state.
    /// </summary>
    public async Task<T> WithDatabaseAsync<T>(Func<OraDbContext, Task<T>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        await using var db = DatabaseCommands.CreateUnrestrictedContext(scope.ServiceProvider);
        return await action(db);
    }

    /// <summary>
    /// Turns an opaque global ID back into the database key, using the schema's own serializer.
    /// </summary>
    public async Task<Guid> DecodeIdAsync(string globalId)
    {
        var executor = await Services.GetRequiredService<IRequestExecutorProvider>().GetExecutorAsync();
        var serializer = executor.Schema.Services.GetRequiredService<INodeIdSerializer>();
        return (Guid)serializer.Parse(globalId, typeof(Guid)).InternalId;
    }

    /// <summary>
    /// Adds a signed-in user to a workspace directly, because v0.1 has no invitations.
    /// </summary>
    public async Task AddMemberAsync(string workspaceId, OraClient member, MembershipRole role)
    {
        var workspaceKey = await DecodeIdAsync(workspaceId);
        await WithDatabaseAsync(async db =>
        {
            var userId = await db.Users.Where(u => u.Email == member.Email).Select(u => u.Id).SingleAsync();
            db.Memberships.Add(new Membership { UserId = userId, WorkspaceId = workspaceKey, Role = role });
            return await db.SaveChangesAsync();
        });
    }
}
