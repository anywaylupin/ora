using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;
using Ora.Api.Domain;

namespace Ora.Api.GraphQL.Projects;

/// <summary>
/// Lets the projects page search by name and switch between active and archived projects.
/// </summary>
public sealed class ProjectFilterInputType : FilterInputType<Project>
{
    protected override void Configure(IFilterInputTypeDescriptor<Project> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.AllowAnd(false).AllowOr(false);
        descriptor.Field(p => p.Name).Type<NameFilterInputType>();
        descriptor.Field(p => p.IsArchived).Type<ArchivedFilterInputType>();
    }
}

/// <summary>
/// Projects sort by name only; the ID tiebreaker is added on the server.
/// </summary>
public sealed class ProjectSortInputType : SortInputType<Project>
{
    protected override void Configure(ISortInputTypeDescriptor<Project> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(p => p.Name);
    }
}
