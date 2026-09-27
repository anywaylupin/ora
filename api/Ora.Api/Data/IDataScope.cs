namespace Ora.Api.Data;

/// <summary>
/// Tells the database layer whose data it may return.
/// </summary>
/// <remarks>
/// The workspace query filters read this on every query, so a resolver that forgets a membership check still cannot see another workspace's rows.
/// </remarks>
public interface IDataScope
{
    /// <summary>
    /// The signed-in user, or null for anonymous requests, which then see no workspace data at all.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// True only for trusted maintenance work such as migrations and seeding.
    /// </summary>
    bool IsUnrestricted { get; }
}

/// <summary>
/// Scopes data to the user on the current HTTP request.
/// </summary>
public sealed class HttpContextDataScope(IHttpContextAccessor httpContextAccessor) : IDataScope
{
    public Guid? UserId => httpContextAccessor.HttpContext?.User.GetUserId();

    public bool IsUnrestricted => false;
}

/// <summary>
/// Lifts the workspace filters for commands that run outside any user's request.
/// </summary>
public sealed class UnrestrictedDataScope : IDataScope
{
    public static readonly UnrestrictedDataScope Instance = new();

    private UnrestrictedDataScope()
    {
    }

    public Guid? UserId => null;

    public bool IsUnrestricted => true;
}
