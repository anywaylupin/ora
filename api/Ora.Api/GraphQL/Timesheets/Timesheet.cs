using Ora.Api.Domain;

namespace Ora.Api.GraphQL.Timesheets;

/// <summary>
/// One person's week in one workspace, shaped for the grid: a row per project and a cell per day.
/// </summary>
public sealed record Timesheet(DateOnly WeekStart, IReadOnlyList<TimesheetRow> Rows)
{
    public const int DaysPerWeek = 7;

    /// <summary>
    /// Monday to Sunday, so the client never does week arithmetic to label columns.
    /// </summary>
    public IReadOnlyList<DateOnly> Days { get; } =
        [.. Enumerable.Range(0, DaysPerWeek).Select(WeekStart.AddDays)];
}

/// <summary>
/// A project's line in the timesheet.
/// </summary>
/// <param name="IsEditable">False for projects the user is no longer assigned to, or that were archived, which stay visible only because they hold time this week.</param>
public sealed record TimesheetRow(Project Project, bool IsEditable, IReadOnlyList<TimesheetCell> Cells);

/// <summary>
/// The time for one project on one day, whether or not an entry exists yet.
/// </summary>
public sealed class TimesheetCell(TimesheetCellId id, TimeEntry? entry)
{
    public TimesheetCellId Id { get; } = id;

    public DateOnly Date => Id.Date;

    public int DurationMinutes => Entry?.DurationMinutes ?? 0;

    public string? Note => Entry?.Note;

    public TimeEntry? Entry { get; } = entry;
}
