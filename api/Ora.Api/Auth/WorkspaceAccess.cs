using Ora.Api.Data;
using Ora.Api.Domain;
using Ora.Api.GraphQL.Errors;
using Ora.Api.GraphQL.Workspaces;

namespace Ora.Api.Auth;

/// <summary>
/// The one place that decides what the viewer may see and do in a workspace.
/// </summary>
/// <remarks>
/// Membership lookups go through a DataLoader, so checking many objects in one request costs one query.
/// </remarks>
public sealed class WorkspaceAccess(IDataScope scope, IViewerMembershipByWorkspaceIdDataLoader viewerMemberships)
{
    /// <summary>
    /// Resolvers only run for authenticated viewers, so a missing user here is a programming error.
    /// </summary>
    public Guid ViewerId => scope.UserId ?? throw new InvalidOperationException("No signed-in user.");

    public async Task<MembershipRole?> GetRoleAsync(Guid workspaceId, CancellationToken cancellationToken) =>
        scope.UserId is null
            ? null
            : (await viewerMemberships.LoadAsync(workspaceId, cancellationToken))?.Role;

    public async Task<bool> IsMemberAsync(Guid workspaceId, CancellationToken cancellationToken) =>
        await GetRoleAsync(workspaceId, cancellationToken) is not null;

    /// <summary>
    /// Returns the object only if the viewer belongs to its workspace, so node lookups never cross workspaces.
    /// </summary>
    public async Task<T?> VisibleOrNullAsync<T>(T? value, Func<T, Guid> workspaceId, CancellationToken cancellationToken)
        where T : class =>
        value is not null && await IsMemberAsync(workspaceId(value), cancellationToken) ? value : null;

    /// <summary>
    /// Throws unless the viewer holds at least the given role.
    /// </summary>
    /// <remarks>
    /// Non-members get NotFound rather than AccessDenied so they cannot confirm the workspace exists.
    /// </remarks>
    public async Task RequireRoleAsync(Guid workspaceId, MembershipRole minimum, CancellationToken cancellationToken)
    {
        var role = await GetRoleAsync(workspaceId, cancellationToken)
            ?? throw new NotFoundException("The workspace was not found.");

        if (role < minimum)
        {
            throw new AccessDeniedException($"Only a workspace {minimum.ToString().ToLowerInvariant()} can do this.");
        }
    }
}
