using Ora.Api.Tests.Infrastructure;

namespace Ora.Api.Tests.Workspaces;

/// <summary>
/// Proves clients and projects stay inside their workspace for reads and writes.
/// </summary>
public sealed class CatalogScopingTests(OraApiFactory factory)
{
    [Fact]
    public async Task Clients_and_projects_of_another_workspace_cannot_be_read()
    {
        using var owner = await factory.SignedInClientAsync();
        using var outsider = await factory.SignedInClientAsync();
        var workspaceId = await owner.CreateWorkspaceAsync();
        var clientId = await owner.CreateClientAsync(workspaceId);
        var projectId = await owner.CreateProjectAsync(clientId);

        var response = await outsider.GraphQLAsync(
            "query($ids: [ID!]!) { nodes(ids: $ids) { id } }", new { ids = new[] { clientId, projectId } });

        Assert.All(response.Data["nodes"]!.AsArray(), Assert.Null);
    }

    [Fact]
    public async Task Another_workspace_cannot_be_written_to()
    {
        using var owner = await factory.SignedInClientAsync();
        using var outsider = await factory.SignedInClientAsync();
        var workspaceId = await owner.CreateWorkspaceAsync();
        var clientId = await owner.CreateClientAsync(workspaceId);
        var projectId = await owner.CreateProjectAsync(clientId);
        await outsider.CreateWorkspaceAsync();

        var createClient = await outsider.GraphQLAsync(
            "mutation($id: ID!) { createClient(input: { workspaceId: $id, name: \"Hijack\" }) { errors { __typename } } }",
            new { id = workspaceId });
        var updateClient = await outsider.GraphQLAsync(
            "mutation($id: ID!) { updateClient(input: { id: $id, name: \"Hijack\" }) { errors { __typename } } }",
            new { id = clientId });
        var createProject = await outsider.GraphQLAsync(
            """
            mutation($id: ID!) {
              createProject(input: { clientId: $id, name: "Hijack", color: "#000000" }) { errors { __typename } }
            }
            """,
            new { id = clientId });
        var archiveProject = await outsider.GraphQLAsync(
            "mutation($id: ID!) { archiveProject(input: { id: $id }) { errors { __typename } } }",
            new { id = projectId });

        Assert.Equal(["NotFoundError"], createClient.PayloadErrors("createClient"));
        Assert.Equal(["NotFoundError"], updateClient.PayloadErrors("updateClient"));
        Assert.Equal(["NotFoundError"], createProject.PayloadErrors("createProject"));
        Assert.Equal(["NotFoundError"], archiveProject.PayloadErrors("archiveProject"));
    }

    [Fact]
    public async Task A_project_cannot_be_moved_to_a_client_in_another_workspace()
    {
        using var owner = await factory.SignedInClientAsync();
        var first = await owner.CreateWorkspaceAsync("First");
        var second = await owner.CreateWorkspaceAsync("Second");
        var projectId = await owner.CreateProjectAsync(await owner.CreateClientAsync(first));
        var foreignClientId = await owner.CreateClientAsync(second);

        var response = await owner.GraphQLAsync(
            """
            mutation($id: ID!, $clientId: ID!) {
              updateProject(input: { id: $id, clientId: $clientId, name: "Moved", color: "#000000" }) {
                errors { __typename }
              }
            }
            """,
            new { id = projectId, clientId = foreignClientId });

        Assert.Equal(["NotFoundError"], response.PayloadErrors("updateProject"));
    }
}
