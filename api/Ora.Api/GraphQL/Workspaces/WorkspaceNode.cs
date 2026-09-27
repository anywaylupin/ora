using GreenDonut.Data;
using HotChocolate.Types.Pagination;
using Ora.Api.Auth;
using Ora.Api.Domain;
using Ora.Api.GraphQL.Clients;
using Ora.Api.GraphQL.Projects;
using Ora.Api.GraphQL.Timesheets;

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
        [Parent(requires: nameof(Workspace.Id))] Workspace workspace,
        WorkspaceAccess access,
        CancellationToken cancellationToken) =>
        await access.GetRoleAsync(workspace.Id, cancellationToken)
        ?? throw new InvalidOperationException("The viewer is not a member of a visible workspace.");

    [UsePaging]
    public static async Task<Connection<Membership>> GetMembersAsync(
        [Parent(requires: nameof(Workspace.Id))] Workspace workspace,
        PagingArguments pagingArguments,
        IMembershipsByWorkspaceIdDataLoader membershipsByWorkspaceId,
        CancellationToken cancellationToken) =>
        await membershipsByWorkspaceId
            .With(pagingArguments)
            .LoadAsync(workspace.Id, cancellationToken)
            .ToConnectionAsync();

    [UsePaging]
    [UseFiltering<ClientFilterInputType>]
    [UseSorting<ClientSortInputType>]
    public static async Task<Connection<Client>> GetClientsAsync(
        [Parent(requires: nameof(Workspace.Id))] Workspace workspace,
        PagingArguments pagingArguments,
        QueryContext<Client> query,
        IClientsByWorkspaceIdDataLoader clientsByWorkspaceId,
        CancellationToken cancellationToken) =>
        await clientsByWorkspaceId
            .With(pagingArguments, query)
            .LoadAsync(workspace.Id, cancellationToken)
            .ToConnectionAsync();

    [UsePaging]
    [UseFiltering<ProjectFilterInputType>]
    [UseSorting<ProjectSortInputType>]
    public static async Task<Connection<Project>> GetProjectsAsync(
        [Parent(requires: nameof(Workspace.Id))] Workspace workspace,
        PagingArguments pagingArguments,
        QueryContext<Project> query,
        IProjectsByWorkspaceIdDataLoader projectsByWorkspaceId,
        CancellationToken cancellationToken) =>
        await projectsByWorkspaceId
            .With(pagingArguments, query)
            .LoadAsync(workspace.Id, cancellationToken)
            .ToConnectionAsync();

    /// <summary>
    /// The viewer's own week in this workspace.
    /// </summary>
    /// <param name="weekStart">The Monday that starts the week, in the user's own calendar.</param>
    public static async Task<Timesheet> GetTimesheetAsync(
        [Parent(requires: nameof(Workspace.Id))] Workspace workspace,
        DateOnly weekStart,
        WorkspaceAccess access,
        TimesheetService timesheets,
        CancellationToken cancellationToken)
    {
        if (weekStart.DayOfWeek != DayOfWeek.Monday)
        {
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage("The week must start on a Monday.")
                .SetCode("INVALID_WEEK_START")
                .Build());
        }

        return await timesheets.GetWeekAsync(workspace.Id, access.ViewerId, weekStart, cancellationToken);
    }
}
