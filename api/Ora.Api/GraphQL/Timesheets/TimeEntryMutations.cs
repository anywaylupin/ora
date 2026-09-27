using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using Ora.Api.Auth;
using Ora.Api.Data;
using Ora.Api.Domain;
using Ora.Api.GraphQL.Errors;

namespace Ora.Api.GraphQL.Timesheets;

[MutationType]
public static partial class TimeEntryMutations
{
    /// <summary>
    /// Saves the viewer's time for one project on one day; zero minutes clears the cell.
    /// </summary>
    /// <remarks>
    /// The grid saves on blur, so this is idempotent: sending the same values twice leaves one entry.
    /// </remarks>
    [Authorize]
    [Error<ValidationError>]
    [Error<NotFoundError>]
    [Error<ProjectNotAssignedError>]
    public static async Task<TimesheetCell> UpsertTimeEntryAsync(
        [ID<Project>] Guid projectId,
        DateOnly date,
        int durationMinutes,
        string? note,
        WorkspaceAccess access,
        TimesheetService timesheets,
        OraDbContext db,
        CancellationToken cancellationToken)
    {
        if (durationMinutes is < 0 or > TimeEntry.MaxMinutesPerDay)
        {
            throw new ValidationException(nameof(durationMinutes), "Enter between 0 and 24 hours.");
        }

        var normalizedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (normalizedNote?.Length > TimeEntry.NoteMaxLength)
        {
            throw new ValidationException(nameof(note), $"Use {TimeEntry.NoteMaxLength} characters or fewer.");
        }

        var project = await db.Projects.AsNoTracking().SingleOrDefaultAsync(p => p.Id == projectId, cancellationToken)
            ?? throw new NotFoundException("The project was not found.");
        var userId = access.ViewerId;
        if (!await timesheets.CanLogTimeAsync(project, userId, cancellationToken))
        {
            throw new ProjectNotAssignedException();
        }

        var otherMinutesThatDay = await db.TimeEntries
            .Where(e => e.UserId == userId && e.Date == date && e.ProjectId != projectId)
            .SumAsync(e => e.DurationMinutes, cancellationToken);
        if (otherMinutesThatDay + durationMinutes > TimeEntry.MaxMinutesPerDay)
        {
            throw new ValidationException(nameof(durationMinutes), "A day cannot hold more than 24 hours.");
        }

        var entry = await SaveAsync(db, project, userId, date, durationMinutes, normalizedNote, cancellationToken);
        return new TimesheetCell(new TimesheetCellId(userId, projectId, date), entry);
    }

    /// <summary>
    /// Two quick saves of a new cell can race to insert; the loser retries as an update against the winner's row.
    /// </summary>
    private static async Task<TimeEntry?> SaveAsync(
        OraDbContext db,
        Project project,
        Guid userId,
        DateOnly date,
        int durationMinutes,
        string? note,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var entry = await db.TimeEntries.SingleOrDefaultAsync(
                e => e.UserId == userId && e.ProjectId == project.Id && e.Date == date, cancellationToken);

            if (durationMinutes == 0)
            {
                if (entry is not null)
                {
                    db.TimeEntries.Remove(entry);
                    await db.SaveChangesAsync(cancellationToken);
                }

                return null;
            }

            if (entry is null)
            {
                entry = new TimeEntry { WorkspaceId = project.WorkspaceId, UserId = userId, ProjectId = project.Id, Date = date };
                db.TimeEntries.Add(entry);
            }

            entry.DurationMinutes = durationMinutes;
            entry.Note = note;

            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return entry;
            }
            catch (DbUpdateException) when (attempt == 1)
            {
                db.ChangeTracker.Clear();
            }
        }
    }
}

/// <summary>
/// Thrown when the viewer logs time on a project that is not open to them.
/// </summary>
public sealed class ProjectNotAssignedException()
    : Exception("You can only log time on active projects you are assigned to.");

/// <summary>
/// The payload error for time logged outside the viewer's assigned, active projects.
/// </summary>
public sealed class ProjectNotAssignedError(ProjectNotAssignedException exception)
{
    public string Message { get; } = exception.Message;
}
