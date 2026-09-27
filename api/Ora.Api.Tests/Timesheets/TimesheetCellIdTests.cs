using HotChocolate.Execution;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;
using Ora.Api.GraphQL.Timesheets;
using Ora.Api.Tests.Infrastructure;

namespace Ora.Api.Tests.Timesheets;

public sealed class TimesheetCellIdTests(OraApiFactory factory)
{
    /// <summary>
    /// Random Guids cover byte values such as the separator and the escape character.
    /// </summary>
    [Fact]
    public async Task Cell_ids_survive_a_round_trip_through_the_global_id()
    {
        var executor = await factory.Services.GetRequiredService<IRequestExecutorProvider>()
            .GetExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
        var serializer = executor.Schema.Services.GetRequiredService<INodeIdSerializer>();

        for (var i = 0; i < 500; i++)
        {
            var id = new TimesheetCellId(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 9, 21).AddDays(i));

            var parsed = serializer.Parse(serializer.Format("TimesheetCell", id), typeof(TimesheetCellId));

            Assert.Equal(id, parsed.InternalId);
        }
    }
}
