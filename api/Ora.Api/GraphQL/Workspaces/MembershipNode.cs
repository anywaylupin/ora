using Ora.Api.Auth;
using Ora.Api.Domain;
using Ora.Api.GraphQL.Users;

namespace Ora.Api.GraphQL.Workspaces;

/// <summary>
/// A person's place in a workspace, with their role.
/// </summary>
[ObjectType<Membership>]
public static partial class MembershipNode
{
    static partial void Configure(IObjectTypeDescriptor<Membership> descriptor)
    {
        descriptor.Ignore(m => m.UserId);
        descriptor.Ignore(m => m.WorkspaceId);
    }

    [NodeResolver]
    public static async Task<Membership?> GetMembershipByIdAsync(
        Guid id,
        IMembershipByIdDataLoader membershipById,
        WorkspaceAccess access,
        CancellationToken cancellationToken) =>
        await access.VisibleOrNullAsync(
            await membershipById.LoadAsync(id, cancellationToken), m => m.WorkspaceId, cancellationToken);

    public static async Task<User> GetUserAsync(
        [Parent(requires: nameof(Membership.UserId))] Membership membership,
        IVisibleUserByIdDataLoader userById,
        CancellationToken cancellationToken) =>
        await userById.LoadRequiredAsync(membership.UserId, cancellationToken);
}
