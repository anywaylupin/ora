namespace Ora.Api.Domain;

/// <summary>
/// The tenant boundary: every client, project, and time entry belongs to exactly one workspace.
/// </summary>
public sealed class Workspace
{
    public const int NameMaxLength = 100;

    public Guid Id { get; init; }

    public required string Name { get; set; }
}
