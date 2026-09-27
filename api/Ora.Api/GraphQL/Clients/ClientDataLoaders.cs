using GreenDonut.Data;
using Microsoft.EntityFrameworkCore;
using Ora.Api.Data;
using Ora.Api.Domain;

namespace Ora.Api.GraphQL.Clients;

/// <summary>
/// Batches client lookups and pages.
/// </summary>
internal static class ClientDataLoaders
{
    [DataLoader]
    public static async Task<Dictionary<Guid, Client>> GetClientByIdAsync(
        IReadOnlyList<Guid> ids,
        OraDbContext db,
        CancellationToken cancellationToken) =>
        await db.Clients
            .AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

    [DataLoader]
    public static async Task<Dictionary<Guid, Page<Client>>> GetClientsByWorkspaceIdAsync(
        IReadOnlyList<Guid> workspaceIds,
        PagingArguments pagingArguments,
        QueryContext<Client> query,
        OraDbContext db,
        CancellationToken cancellationToken) =>
        await db.Clients
            .AsNoTracking()
            .Where(c => workspaceIds.Contains(c.WorkspaceId))
            .With(query, sort => DefaultOrder.ByName(sort, c => c.Name, c => c.Id))
            .ToBatchPageAsync(c => c.WorkspaceId, pagingArguments, cancellationToken);
}
