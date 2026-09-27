using Microsoft.EntityFrameworkCore;
using Ora.Api.Data;
using Ora.Api.Domain;

namespace Ora.Api.GraphQL.Timesheets;

/// <summary>
/// Builds a week and decides which projects a user may log time on.
/// </summary>
/// <remarks>
/// The query and the mutation share these rules so the grid never offers a cell the server would reject.
/// </remarks>
public sealed class TimesheetService(OraDbContext db)
{
    public async Task<Timesheet> GetWeekAsync(
        Guid workspaceId,
        Guid userId,
        DateOnly weekStart,
        CancellationToken cancellationToken)
    {
        var weekEnd = weekStart.AddDays(Timesheet.DaysPerWeek - 1);

        var entries = await db.TimeEntries
            .AsNoTracking()
            .Where(e => e.WorkspaceId == workspaceId && e.UserId == userId && e.Date >= weekStart && e.Date <= weekEnd)
            .ToListAsync(cancellationToken);
        var loggedProjectIds = entries.Select(e => e.ProjectId).Distinct().ToList();
        var assignedProjectIds = db.ProjectAssignments.Where(a => a.UserId == userId).Select(a => a.ProjectId);

        var projects = await db.Projects
            .AsNoTracking()
            .Where(p => p.WorkspaceId == workspaceId)
            .Join(db.Clients, p => p.ClientId, c => c.Id, (p, c) => new
            {
                Project = p,
                ClientName = c.Name,
                IsEditable = assignedProjectIds.Contains(p.Id) && !p.IsArchived && !c.IsArchived,
            })
            .Where(x => x.IsEditable || loggedProjectIds.Contains(x.Project.Id))
            .OrderBy(x => x.ClientName)
            .ThenBy(x => x.Project.Name)
            .ToListAsync(cancellationToken);

        var entriesByCell = entries.ToDictionary(e => new TimesheetCellId(e.UserId, e.ProjectId, e.Date));
        var rows = projects
            .Select(x => new TimesheetRow(
                x.Project,
                x.IsEditable,
                [.. Enumerable.Range(0, Timesheet.DaysPerWeek).Select(offset =>
                {
                    var id = new TimesheetCellId(userId, x.Project.Id, weekStart.AddDays(offset));
                    return new TimesheetCell(id, entriesByCell.GetValueOrDefault(id));
                })]))
            .ToList();

        return new Timesheet(weekStart, rows);
    }

    /// <summary>
    /// Time goes only to active projects of active clients that the user is assigned to.
    /// </summary>
    public async Task<bool> CanLogTimeAsync(Project project, Guid userId, CancellationToken cancellationToken) =>
        !project.IsArchived
        && await db.ProjectAssignments.AnyAsync(a => a.ProjectId == project.Id && a.UserId == userId, cancellationToken)
        && await db.Clients.AnyAsync(c => c.Id == project.ClientId && !c.IsArchived, cancellationToken);
}
