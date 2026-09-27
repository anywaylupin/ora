using GreenDonut.Data;
using HotChocolate.Types.Pagination;
using Ora.Api.Auth;
using Ora.Api.Domain;
using Ora.Api.GraphQL.Projects;

namespace Ora.Api.GraphQL.Clients;

[ObjectType<Client>]
public static partial class ClientNode
{
    static partial void Configure(IObjectTypeDescriptor<Client> descriptor) =>
        descriptor.Ignore(c => c.WorkspaceId);

    [NodeResolver]
    public static async Task<Client?> GetClientByIdAsync(
        Guid id,
        IClientByIdDataLoader clientById,
        WorkspaceAccess access,
        CancellationToken cancellationToken) =>
        await access.VisibleOrNullAsync(
            await clientById.LoadAsync(id, cancellationToken), c => c.WorkspaceId, cancellationToken);

    [UsePaging]
    [UseFiltering<ProjectFilterInputType>]
    [UseSorting<ProjectSortInputType>]
    public static async Task<Connection<Project>> GetProjectsAsync(
        [Parent(requires: nameof(Client.Id))] Client client,
        PagingArguments pagingArguments,
        QueryContext<Project> query,
        IProjectsByClientIdDataLoader projectsByClientId,
        CancellationToken cancellationToken) =>
        await projectsByClientId
            .With(pagingArguments, query)
            .LoadAsync(client.Id, cancellationToken)
            .ToConnectionAsync();
}
