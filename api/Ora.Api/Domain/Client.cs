namespace Ora.Api.Domain;

/// <summary>
/// A customer of the consultancy that projects are billed to.
/// </summary>
public sealed class Client
{
    public const int NameMaxLength = 100;

    public Guid Id { get; init; }

    public Guid WorkspaceId { get; init; }

    public required string Name { get; set; }

    public bool IsArchived { get; set; }
}
