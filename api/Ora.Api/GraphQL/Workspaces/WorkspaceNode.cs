using GreenDonut.Data;
using HotChocolate.Types.Pagination;
using Ora.Api.Auth;
using Ora.Api.Domain;

namespace Ora.Api.GraphQL.Workspaces;

/// <summary>
/// The entry point to everything inside a workspace; every nested field is reachable only through a visible workspace.
/// </summary>
[ObjectType<Workspace>]
public static partial class WorkspaceNode
{
    [NodeResolver]
    public static async Task<Workspace?> GetWorkspaceByIdAsync(
        Guid id,
        IWorkspaceByIdDataLoader workspaceById,
        WorkspaceAccess access,
        CancellationToken cancellationToken) =>
        await access.VisibleOrNullAsync(
            await workspaceById.LoadAsync(id, cancellationToken), w => w.Id, cancellationToken);

    /// <summary>
    /// Lets the client hide admin controls without a second query.
    /// </summary>
    public static async Task<MembershipRole> GetViewerRoleAsync(
        [Parent] Workspace workspace,
        WorkspaceAccess access,
        CancellationToken cancellationToken) =>
        await access.GetRoleAsync(workspace.Id, cancellationToken)
        ?? throw new InvalidOperationException("The viewer is not a member of a visible workspace.");

    [UsePaging]
    public static async Task<Connection<Membership>> GetMembersAsync(
        [Parent] Workspace workspace,
        PagingArguments pagingArguments,
        IMembershipsByWorkspaceIdDataLoader membershipsByWorkspaceId,
        CancellationToken cancellationToken) =>
        await membershipsByWorkspaceId
            .With(pagingArguments)
            .LoadAsync(workspace.Id, cancellationToken)
            .ToConnectionAsync();
}
