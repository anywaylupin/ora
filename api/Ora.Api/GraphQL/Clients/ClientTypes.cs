using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using Ora.Api.Domain;

namespace Ora.Api.GraphQL.Clients;

/// <summary>
/// Lets the clients page search by name and switch between active and archived clients.
/// </summary>
public sealed class ClientFilterInputType : FilterInputType<Client>
{
    protected override void Configure(IFilterInputTypeDescriptor<Client> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.AllowAnd(false).AllowOr(false);
        descriptor.Field(c => c.Name).Type<NameFilterInputType>();
        descriptor.Field(c => c.IsArchived).Type<ArchivedFilterInputType>();
    }
}

/// <summary>
/// Clients sort by name only; the ID tiebreaker is added on the server.
/// </summary>
public sealed class ClientSortInputType : SortInputType<Client>
{
    protected override void Configure(ISortInputTypeDescriptor<Client> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(c => c.Name);
    }
}
