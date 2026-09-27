using Ora.Api.Auth;
using Ora.Api.Domain;
using Ora.Api.GraphQL.Projects;
using Ora.Api.GraphQL.Users;

namespace Ora.Api.GraphQL.Timesheets;

/// <summary>
/// Time entries are private to their author in v0.1; approvals will open them to managers.
/// </summary>
[ObjectType<TimeEntry>]
public static partial class TimeEntryNode
{
    static partial void Configure(IObjectTypeDescriptor<TimeEntry> descriptor)
    {
        descriptor.Ignore(e => e.WorkspaceId);
        descriptor.Ignore(e => e.UserId);
        descriptor.Ignore(e => e.ProjectId);
    }

    [NodeResolver]
    public static async Task<TimeEntry?> GetTimeEntryByIdAsync(
        Guid id,
        ITimeEntryByIdDataLoader timeEntryById,
        WorkspaceAccess access,
        CancellationToken cancellationToken)
    {
        var entry = await timeEntryById.LoadAsync(id, cancellationToken);
        return entry?.UserId == access.ViewerId
            ? await access.VisibleOrNullAsync(entry, e => e.WorkspaceId, cancellationToken)
            : null;
    }

    public static async Task<Project> GetProjectAsync(
        [Parent(requires: nameof(TimeEntry.ProjectId))] TimeEntry entry,
        IProjectByIdDataLoader projectById,
        CancellationToken cancellationToken) =>
        await projectById.LoadRequiredAsync(entry.ProjectId, cancellationToken);

    public static async Task<User> GetUserAsync(
        [Parent(requires: nameof(TimeEntry.UserId))] TimeEntry entry,
        IVisibleUserByIdDataLoader userById,
        CancellationToken cancellationToken) =>
        await userById.LoadRequiredAsync(entry.UserId, cancellationToken);
}

[ObjectType<TimesheetCell>]
public static partial class TimesheetCellNode
{
    /// <summary>
    /// Refetching a cell works for the viewer's own cells in projects they can see.
    /// </summary>
    [NodeResolver]
    public static async Task<TimesheetCell?> GetTimesheetCellByIdAsync(
        TimesheetCellId id,
        ITimeEntryByCellIdDataLoader timeEntryByCellId,
        IProjectByIdDataLoader projectById,
        WorkspaceAccess access,
        CancellationToken cancellationToken)
    {
        if (id.UserId != access.ViewerId)
        {
            return null;
        }

        var project = await access.VisibleOrNullAsync(
            await projectById.LoadAsync(id.ProjectId, cancellationToken), p => p.WorkspaceId, cancellationToken);

        return project is null
            ? null
            : new TimesheetCell(id, await timeEntryByCellId.LoadAsync(id, cancellationToken));
    }
}

[ObjectType<TimesheetRow>]
public static partial class TimesheetRowNode;

[ObjectType<Timesheet>]
public static partial class TimesheetNode;
