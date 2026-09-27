using GreenDonut.Data;
using HotChocolate.Authorization;
using HotChocolate.Types.Pagination;
using Microsoft.EntityFrameworkCore;
using Ora.Api.Auth;
using Ora.Api.Data;
using Ora.Api.Domain;
using Ora.Api.GraphQL.Users;

namespace Ora.Api.GraphQL.Viewer;

/// <summary>
/// The signed-in person and the workspaces they can open.
/// </summary>
/// <remarks>
/// Kept separate from User so fields that only make sense for yourself never appear on other people.
/// </remarks>
public sealed record Viewer(Guid UserId);

[QueryType]
public static partial class ViewerQueries
{
    [Authorize]
    public static Viewer GetViewer(WorkspaceAccess access) => new(access.ViewerId);
}

[ObjectType<Viewer>]
public static partial class ViewerNode
{
    static partial void Configure(IObjectTypeDescriptor<Viewer> descriptor) =>
        descriptor.Ignore(v => v.UserId);

    public static async Task<User> GetUserAsync(
        [Parent] Viewer viewer,
        IVisibleUserByIdDataLoader userById,
        CancellationToken cancellationToken) =>
        await userById.LoadRequiredAsync(viewer.UserId, cancellationToken);

    /// <summary>
    /// The workspace query filter already limits rows to the viewer's memberships.
    /// </summary>
    [UsePaging]
    public static async Task<Connection<Workspace>> GetWorkspacesAsync(
        PagingArguments pagingArguments,
        OraDbContext db,
        CancellationToken cancellationToken) =>
        await db.Workspaces
            .AsNoTracking()
            .OrderBy(w => w.Name)
            .ThenBy(w => w.Id)
            .ToPageAsync(pagingArguments, cancellationToken)
            .ToConnectionAsync();
}
