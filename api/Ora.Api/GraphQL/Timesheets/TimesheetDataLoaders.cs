using Microsoft.EntityFrameworkCore;
using Ora.Api.Data;
using Ora.Api.Domain;

namespace Ora.Api.GraphQL.Timesheets;

/// <summary>
/// Batches time entry lookups by row ID and by cell.
/// </summary>
internal static class TimesheetDataLoaders
{
    [DataLoader]
    public static async Task<Dictionary<Guid, TimeEntry>> GetTimeEntryByIdAsync(
        IReadOnlyList<Guid> ids,
        OraDbContext db,
        CancellationToken cancellationToken) =>
        await db.TimeEntries
            .AsNoTracking()
            .Where(e => ids.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, cancellationToken);

    /// <summary>
    /// Narrows by each key part in SQL, then matches exact cells in memory, which stays one query per batch.
    /// </summary>
    [DataLoader]
    public static async Task<Dictionary<TimesheetCellId, TimeEntry>> GetTimeEntryByCellIdAsync(
        IReadOnlyList<TimesheetCellId> cellIds,
        OraDbContext db,
        CancellationToken cancellationToken)
    {
        var userIds = cellIds.Select(c => c.UserId).Distinct().ToList();
        var projectIds = cellIds.Select(c => c.ProjectId).Distinct().ToList();
        var dates = cellIds.Select(c => c.Date).Distinct().ToList();

        var entries = await db.TimeEntries
            .AsNoTracking()
            .Where(e => userIds.Contains(e.UserId) && projectIds.Contains(e.ProjectId) && dates.Contains(e.Date))
            .ToListAsync(cancellationToken);

        var wanted = cellIds.ToHashSet();
        return entries
            .Select(e => (Id: new TimesheetCellId(e.UserId, e.ProjectId, e.Date), Entry: e))
            .Where(x => wanted.Contains(x.Id))
            .ToDictionary(x => x.Id, x => x.Entry);
    }
}
