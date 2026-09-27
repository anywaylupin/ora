namespace Ora.Api.Domain;

/// <summary>
/// A piece of client work that members log time against.
/// </summary>
public sealed class Project
{
    public const int NameMaxLength = 100;

    public Guid Id { get; init; }

    public Guid WorkspaceId { get; init; }

    public Guid ClientId { get; set; }

    public required string Name { get; set; }

    /// <summary>
    /// A CSS hex color such as #3b82f6, used to tell projects apart in the timesheet.
    /// </summary>
    public required string Color { get; set; }

    public bool IsArchived { get; set; }
}
