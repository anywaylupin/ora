using HotChocolate.Authorization;
using Ora.Api.Auth;
using Ora.Api.Data;
using Ora.Api.Domain;
using Ora.Api.GraphQL.Errors;

namespace Ora.Api.GraphQL.Workspaces;

[MutationType]
public static partial class WorkspaceMutations
{
    /// <summary>
    /// Creates a workspace and makes the viewer its first admin.
    /// </summary>
    [Authorize]
    [Error<ValidationError>]
    public static async Task<Workspace> CreateWorkspaceAsync(
        string name,
        WorkspaceAccess access,
        OraDbContext db,
        CancellationToken cancellationToken)
    {
        var workspace = new Workspace { Name = Validate.Name(name, nameof(name), Workspace.NameMaxLength) };
        db.Workspaces.Add(workspace);
        db.Memberships.Add(new Membership
        {
            UserId = access.ViewerId,
            WorkspaceId = workspace.Id,
            Role = MembershipRole.Admin,
        });
        await db.SaveChangesAsync(cancellationToken);
        return workspace;
    }
}
