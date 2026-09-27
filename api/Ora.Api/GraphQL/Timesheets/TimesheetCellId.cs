using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Relay;

namespace Ora.Api.GraphQL.Timesheets;

/// <summary>
/// Identifies a timesheet cell by what it means rather than by a database row.
/// </summary>
/// <remarks>
/// An empty cell and the entry saved into it share this ID, so the client can apply optimistic updates without an updater.
/// </remarks>
public readonly record struct TimesheetCellId(Guid UserId, Guid ProjectId, DateOnly Date);

/// <summary>
/// Writes the cell ID as three parts inside the opaque global ID, with the date as its day number.
/// </summary>
/// <remarks>
/// Guids are written as hex rather than compressed bytes, because a raw byte of 0x5C before a separator breaks part splitting in Hot Chocolate 16.6.
/// </remarks>
internal sealed class TimesheetCellIdSerializer : CompositeNodeIdValueSerializer<TimesheetCellId>
{
    protected override NodeIdFormatterResult Format(Span<byte> buffer, TimesheetCellId value, out int written)
    {
        if (TryFormatIdPart(buffer, value.UserId, out var userBytes, compress: false)
            && TryFormatIdPart(buffer[userBytes..], value.ProjectId, out var projectBytes, compress: false)
            && TryFormatIdPart(buffer[(userBytes + projectBytes)..], value.Date.DayNumber, out var dateBytes))
        {
            written = userBytes + projectBytes + dateBytes;
            return NodeIdFormatterResult.Success;
        }

        written = 0;
        return NodeIdFormatterResult.BufferTooSmall;
    }

    protected override bool TryParse(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out TimesheetCellId value)
    {
        if (TryParseIdPart(buffer, out Guid userId, out var userBytes, compress: false)
            && TryParseIdPart(buffer[userBytes..], out Guid projectId, out var projectBytes, compress: false)
            && TryParseIdPart(buffer[(userBytes + projectBytes)..], out int dayNumber, out _)
            && dayNumber >= DateOnly.MinValue.DayNumber
            && dayNumber <= DateOnly.MaxValue.DayNumber)
        {
            value = new TimesheetCellId(userId, projectId, DateOnly.FromDayNumber(dayNumber));
            return true;
        }

        value = default;
        return false;
    }
}
