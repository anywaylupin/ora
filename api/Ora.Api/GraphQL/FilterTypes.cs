using System.Linq.Expressions;
using GreenDonut.Data;
using HotChocolate.Data.Filters;

namespace Ora.Api.GraphQL;

/// <summary>
/// Name filters offer only the operations the list pages use, which keeps the schema small and the SQL predictable.
/// </summary>
public sealed class NameFilterInputType : StringOperationFilterInputType
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Name("NameFilterInput");
        descriptor.AllowAnd(false).AllowOr(false);
        descriptor.Operation(DefaultFilterOperations.Equals).Type<StringType>();
        descriptor.Operation(DefaultFilterOperations.Contains).Type<StringType>();
    }
}

/// <summary>
/// Archived filters need only equality.
/// </summary>
public sealed class ArchivedFilterInputType : BooleanOperationFilterInputType
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Name("ArchivedFilterInput");
        descriptor.AllowAnd(false).AllowOr(false);
        descriptor.Operation(DefaultFilterOperations.Equals).Type<BooleanType>();
    }
}

/// <summary>
/// Sort helpers shared by list DataLoaders.
/// </summary>
public static class DefaultOrder
{
    /// <summary>
    /// Sorts by name when the client asks for no order, and always by ID last so keyset cursors stay unique.
    /// </summary>
    public static SortDefinition<T> ByName<T>(
        SortDefinition<T> sort,
        Expression<Func<T, string>> name,
        Expression<Func<T, Guid>> id) =>
        sort.IfEmpty(s => s.AddAscending(name)).AddAscending(id);
}
