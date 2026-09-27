namespace Ora.Api.Domain;

/// <summary>
/// Time one user spent on one project on one day.
/// </summary>
/// <remarks>
/// Durations are whole minutes so totals never drift the way floating point hours do.
/// </remarks>
public sealed class TimeEntry
{
    public const int MaxMinutesPerDay = 24 * 60;

    public const int NoteMaxLength = 500;

    public Guid Id { get; init; }

    public Guid WorkspaceId { get; init; }

    public Guid UserId { get; init; }

    public Guid ProjectId { get; init; }

    public DateOnly Date { get; init; }

    public int DurationMinutes { get; set; }

    public string? Note { get; set; }
}
