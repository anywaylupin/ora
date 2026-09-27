using Microsoft.EntityFrameworkCore;
using Ora.Api.Data;
using Ora.Api.Domain;

namespace Ora.Api.GraphQL.Users;

/// <summary>
/// Batches user lookups for every relationship that points at a user.
/// </summary>
internal static class UserDataLoaders
{
    /// <summary>
    /// Returns only the viewer and people who share a workspace with them.
    /// </summary>
    /// <remarks>
    /// Identity owns the users table, so it has no workspace query filter and the visibility rule lives here instead.
    /// </remarks>
    [DataLoader]
    public static async Task<Dictionary<Guid, User>> GetVisibleUserByIdAsync(
        IReadOnlyList<Guid> ids,
        IDataScope scope,
        OraDbContext db,
        CancellationToken cancellationToken)
    {
        var viewerId = scope.UserId;

        return await db.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Where(u => scope.IsUnrestricted
                || u.Id == viewerId
                || db.Memberships.Any(m => m.UserId == u.Id
                    && db.Memberships.Any(v => v.UserId == viewerId && v.WorkspaceId == m.WorkspaceId)))
            .ToDictionaryAsync(u => u.Id, cancellationToken);
    }
}
