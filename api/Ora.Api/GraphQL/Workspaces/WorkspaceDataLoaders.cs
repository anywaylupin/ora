using GreenDonut.Data;
using Microsoft.EntityFrameworkCore;
using Ora.Api.Data;
using Ora.Api.Domain;

namespace Ora.Api.GraphQL.Workspaces;

/// <summary>
/// Batches workspace and membership lookups.
/// </summary>
internal static class WorkspaceDataLoaders
{
    [DataLoader]
    public static async Task<Dictionary<Guid, Workspace>> GetWorkspaceByIdAsync(
        IReadOnlyList<Guid> ids,
        OraDbContext db,
        CancellationToken cancellationToken) =>
        await db.Workspaces
            .AsNoTracking()
            .Where(w => ids.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, cancellationToken);

    /// <summary>
    /// The viewer's own membership per workspace, which every authorization check reads.
    /// </summary>
    [DataLoader]
    public static async Task<Dictionary<Guid, Membership>> GetViewerMembershipByWorkspaceIdAsync(
        IReadOnlyList<Guid> workspaceIds,
        IDataScope scope,
        OraDbContext db,
        CancellationToken cancellationToken)
    {
        var viewerId = scope.UserId;

        return await db.Memberships
            .AsNoTracking()
            .Where(m => m.UserId == viewerId && workspaceIds.Contains(m.WorkspaceId))
            .ToDictionaryAsync(m => m.WorkspaceId, cancellationToken);
    }

    [DataLoader]
    public static async Task<Dictionary<Guid, Membership>> GetMembershipByIdAsync(
        IReadOnlyList<Guid> ids,
        OraDbContext db,
        CancellationToken cancellationToken) =>
        await db.Memberships
            .AsNoTracking()
            .Where(m => ids.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, cancellationToken);

    /// <summary>
    /// Pages members in the order they joined, which keeps the workspace creator first.
    /// </summary>
    [DataLoader]
    public static async Task<Dictionary<Guid, Page<Membership>>> GetMembershipsByWorkspaceIdAsync(
        IReadOnlyList<Guid> workspaceIds,
        PagingArguments pagingArguments,
        OraDbContext db,
        CancellationToken cancellationToken) =>
        await db.Memberships
            .AsNoTracking()
            .Where(m => workspaceIds.Contains(m.WorkspaceId))
            .OrderBy(m => m.Id)
            .ToBatchPageAsync(m => m.WorkspaceId, pagingArguments, cancellationToken);
}
