using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ora.Api.Data;

/// <summary>
/// Lets dotnet-ef build the model without starting the web host, which would also build the GraphQL schema.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<OraDbContext>
{
    public OraDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OraDbContext>()
            .UseSqlServer("Server=design-time-only")
            .Options;

        return new OraDbContext(options, UnrestrictedDataScope.Instance);
    }
}
