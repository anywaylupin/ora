namespace Ora.Api.Domain;

/// <summary>
/// Links a user to a workspace and decides what they may do there.
/// </summary>
public sealed class Membership
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public Guid WorkspaceId { get; init; }

    public MembershipRole Role { get; set; }
}

/// <summary>
/// Roles are ordered so that a higher value always includes the rights of the lower ones.
/// </summary>
public enum MembershipRole
{
    Member = 0,
    Manager = 1,
    Admin = 2,
}
