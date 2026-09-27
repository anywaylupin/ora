using Ora.Api.Tests.Infrastructure;

namespace Ora.Api.Tests.Workspaces;

/// <summary>
/// Proves that one workspace's data never reaches a user outside it.
/// </summary>
public sealed class WorkspaceScopingTests(OraApiFactory factory)
{
    [Fact]
    public async Task Viewer_lists_only_workspaces_the_user_belongs_to()
    {
        using var owner = await factory.SignedInClientAsync();
        using var outsider = await factory.SignedInClientAsync();
        await owner.CreateWorkspaceAsync("Owner workspace");

        var response = await outsider.GraphQLAsync("{ viewer { workspaces { totalCount nodes { id } } } }");

        Assert.Equal(0, response.Data["viewer"]!["workspaces"]!["totalCount"]!.GetValue<int>());
    }

    [Fact]
    public async Task Another_users_workspace_cannot_be_fetched_by_id()
    {
        using var owner = await factory.SignedInClientAsync();
        using var outsider = await factory.SignedInClientAsync();
        var workspaceId = await owner.CreateWorkspaceAsync();

        var response = await outsider.GraphQLAsync(
            "query($id: ID!) { node(id: $id) { id ... on Workspace { name } } }", new { id = workspaceId });

        Assert.Null(response.Data["node"]);
    }

    [Fact]
    public async Task Anonymous_requests_cannot_fetch_a_workspace_by_id()
    {
        using var owner = await factory.SignedInClientAsync();
        var workspaceId = await owner.CreateWorkspaceAsync();
        using var anonymous = factory.CreateOraClient();

        var response = await anonymous.GraphQLAsync(
            "query($id: ID!) { node(id: $id) { id } }", new { id = workspaceId });

        Assert.Null(response.Data["node"]);
    }

    [Fact]
    public async Task Members_and_users_of_another_workspace_cannot_be_fetched_by_id()
    {
        using var owner = await factory.SignedInClientAsync();
        using var outsider = await factory.SignedInClientAsync();
        var workspaceId = await owner.CreateWorkspaceAsync();
        var members = await owner.GraphQLAsync(
            """
            query($id: ID!) {
              node(id: $id) { ... on Workspace { members { nodes { id user { id } } } } }
            }
            """,
            new { id = workspaceId });
        var membership = members.Data["node"]!["members"]!["nodes"]![0]!;

        var response = await outsider.GraphQLAsync(
            "query($ids: [ID!]!) { nodes(ids: $ids) { id } }",
            new { ids = new[] { membership["id"]!.GetValue<string>(), membership["user"]!["id"]!.GetValue<string>() } });

        Assert.All(response.Data["nodes"]!.AsArray(), Assert.Null);
    }
}
