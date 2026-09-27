namespace Ora.Api.Domain;

/// <summary>
/// Grants a member a row for the project in their timesheet.
/// </summary>
public sealed class ProjectAssignment
{
    public Guid ProjectId { get; init; }

    public Guid UserId { get; init; }
}
