using GreenDonut.Data;
using Microsoft.EntityFrameworkCore;
using Ora.Api.Data;
using Ora.Api.Domain;

namespace Ora.Api.GraphQL.Projects;

/// <summary>
/// Batches project lookups, pages, and assignments.
/// </summary>
internal static class ProjectDataLoaders
{
    [DataLoader]
    public static async Task<Dictionary<Guid, Project>> GetProjectByIdAsync(
        IReadOnlyList<Guid> ids,
        OraDbContext db,
        CancellationToken cancellationToken) =>
        await db.Projects
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

    [DataLoader]
    public static async Task<Dictionary<Guid, Page<Project>>> GetProjectsByWorkspaceIdAsync(
        IReadOnlyList<Guid> workspaceIds,
        PagingArguments pagingArguments,
        QueryContext<Project> query,
        OraDbContext db,
        CancellationToken cancellationToken) =>
        await db.Projects
            .AsNoTracking()
            .Where(p => workspaceIds.Contains(p.WorkspaceId))
            .With(query, sort => DefaultOrder.ByName(sort, p => p.Name, p => p.Id))
            .ToBatchPageAsync(p => p.WorkspaceId, pagingArguments, cancellationToken);

    [DataLoader]
    public static async Task<Dictionary<Guid, Page<Project>>> GetProjectsByClientIdAsync(
        IReadOnlyList<Guid> clientIds,
        PagingArguments pagingArguments,
        QueryContext<Project> query,
        OraDbContext db,
        CancellationToken cancellationToken) =>
        await db.Projects
            .AsNoTracking()
            .Where(p => clientIds.Contains(p.ClientId))
            .With(query, sort => DefaultOrder.ByName(sort, p => p.Name, p => p.Id))
            .ToBatchPageAsync(p => p.ClientId, pagingArguments, cancellationToken);

    /// <summary>
    /// A lookup returns an empty list for projects with no assignees instead of null.
    /// </summary>
    [DataLoader]
    public static async Task<ILookup<Guid, Guid>> GetAssigneeIdsByProjectIdAsync(
        IReadOnlyList<Guid> projectIds,
        OraDbContext db,
        CancellationToken cancellationToken)
    {
        var assignments = await db.ProjectAssignments
            .AsNoTracking()
            .Where(a => projectIds.Contains(a.ProjectId))
            .ToListAsync(cancellationToken);

        return assignments.ToLookup(a => a.ProjectId, a => a.UserId);
    }
}
