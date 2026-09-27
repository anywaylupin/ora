using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using Ora.Api.Auth;
using Ora.Api.Data;
using Ora.Api.Domain;
using Ora.Api.GraphQL.Errors;

namespace Ora.Api.GraphQL.Clients;

/// <summary>
/// Client changes are for workspace admins only.
/// </summary>
[MutationType]
public static partial class ClientMutations
{
    [Authorize]
    [Error<ValidationError>]
    [Error<NotFoundError>]
    [Error<AccessDeniedError>]
    public static async Task<Client> CreateClientAsync(
        [ID<Workspace>] Guid workspaceId,
        string name,
        WorkspaceAccess access,
        OraDbContext db,
        CancellationToken cancellationToken)
    {
        await access.RequireRoleAsync(workspaceId, MembershipRole.Admin, cancellationToken);
        var client = new Client
        {
            WorkspaceId = workspaceId,
            Name = Validate.Name(name, nameof(name), Client.NameMaxLength),
        };
        db.Clients.Add(client);
        await db.SaveChangesAsync(cancellationToken);
        return client;
    }

    [Authorize]
    [Error<ValidationError>]
    [Error<NotFoundError>]
    [Error<AccessDeniedError>]
    public static async Task<Client> UpdateClientAsync(
        [ID<Client>] Guid id,
        string name,
        WorkspaceAccess access,
        OraDbContext db,
        CancellationToken cancellationToken)
    {
        var client = await FindForAdminAsync(id, access, db, cancellationToken);
        client.Name = Validate.Name(name, nameof(name), Client.NameMaxLength);
        await db.SaveChangesAsync(cancellationToken);
        return client;
    }

    /// <summary>
    /// Archiving hides the client from new work but keeps its history intact.
    /// </summary>
    [Authorize]
    [Error<NotFoundError>]
    [Error<AccessDeniedError>]
    public static async Task<Client> ArchiveClientAsync(
        [ID<Client>] Guid id,
        WorkspaceAccess access,
        OraDbContext db,
        CancellationToken cancellationToken) =>
        await SetArchivedAsync(id, true, access, db, cancellationToken);

    [Authorize]
    [Error<NotFoundError>]
    [Error<AccessDeniedError>]
    public static async Task<Client> RestoreClientAsync(
        [ID<Client>] Guid id,
        WorkspaceAccess access,
        OraDbContext db,
        CancellationToken cancellationToken) =>
        await SetArchivedAsync(id, false, access, db, cancellationToken);

    private static async Task<Client> SetArchivedAsync(
        Guid id,
        bool isArchived,
        WorkspaceAccess access,
        OraDbContext db,
        CancellationToken cancellationToken)
    {
        var client = await FindForAdminAsync(id, access, db, cancellationToken);
        client.IsArchived = isArchived;
        await db.SaveChangesAsync(cancellationToken);
        return client;
    }

    /// <summary>
    /// The query filter already hides clients in other workspaces, so a missing row covers both cases.
    /// </summary>
    private static async Task<Client> FindForAdminAsync(
        Guid id,
        WorkspaceAccess access,
        OraDbContext db,
        CancellationToken cancellationToken)
    {
        var client = await db.Clients.SingleOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("The client was not found.");
        await access.RequireRoleAsync(client.WorkspaceId, MembershipRole.Admin, cancellationToken);
        return client;
    }
}
