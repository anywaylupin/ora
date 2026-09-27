using Ora.Api.Auth;
using Ora.Api.Domain;
using Ora.Api.GraphQL.Clients;
using Ora.Api.GraphQL.Users;

namespace Ora.Api.GraphQL.Projects;

[ObjectType<Project>]
public static partial class ProjectNode
{
    static partial void Configure(IObjectTypeDescriptor<Project> descriptor)
    {
        descriptor.Ignore(p => p.WorkspaceId);
        descriptor.Ignore(p => p.ClientId);
    }

    [NodeResolver]
    public static async Task<Project?> GetProjectByIdAsync(
        Guid id,
        IProjectByIdDataLoader projectById,
        WorkspaceAccess access,
        CancellationToken cancellationToken) =>
        await access.VisibleOrNullAsync(
            await projectById.LoadAsync(id, cancellationToken), p => p.WorkspaceId, cancellationToken);

    public static async Task<Client> GetClientAsync(
        [Parent(requires: nameof(Project.ClientId))] Project project,
        IClientByIdDataLoader clientById,
        CancellationToken cancellationToken) =>
        await clientById.LoadRequiredAsync(project.ClientId, cancellationToken);

    /// <summary>
    /// A plain list rather than a connection, because a project's team is small and edited as a whole.
    /// </summary>
    public static async Task<IReadOnlyList<User>> GetAssigneesAsync(
        [Parent(requires: nameof(Project.Id))] Project project,
        IAssigneeIdsByProjectIdDataLoader assigneeIdsByProjectId,
        IVisibleUserByIdDataLoader userById,
        CancellationToken cancellationToken)
    {
        var userIds = await assigneeIdsByProjectId.LoadRequiredAsync(project.Id, cancellationToken);
        var users = await userById.LoadAsync(userIds, cancellationToken);
        return [.. users.OfType<User>().OrderBy(u => u.Email)];
    }
}
