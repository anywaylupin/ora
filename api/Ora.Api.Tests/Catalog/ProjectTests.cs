using Ora.Api.Domain;
using Ora.Api.Tests.Infrastructure;

namespace Ora.Api.Tests.Catalog;

public sealed class ProjectTests(OraApiFactory factory)
{
    [Fact]
    public async Task Admins_create_projects_under_a_client_and_assign_members()
    {
        using var admin = await factory.SignedInClientAsync();
        using var member = await factory.SignedInClientAsync();
        var workspaceId = await admin.CreateWorkspaceAsync();
        await factory.AddMemberAsync(workspaceId, member, MembershipRole.Member);
        var clientId = await admin.CreateClientAsync(workspaceId, "Globex");
        var projectId = await admin.CreateProjectAsync(clientId, "Website", "#3B82F6");

        await admin.AssignAsync(projectId, await member.ViewerIdAsync());
        await admin.AssignAsync(projectId, await member.ViewerIdAsync());
        var response = await admin.GraphQLAsync(
            """
            query($id: ID!) {
              node(id: $id) { ... on Project { name color isArchived client { name } assignees { email } } }
            }
            """,
            new { id = projectId });

        var project = response.Data["node"]!;
        Assert.Equal("#3b82f6", project["color"]!.GetValue<string>());
        Assert.Equal("Globex", project["client"]!["name"]!.GetValue<string>());
        var assignee = Assert.Single(project["assignees"]!.AsArray());
        Assert.Equal(member.Email, assignee!["email"]!.GetValue<string>());
    }

    [Fact]
    public async Task Invalid_colors_and_archived_clients_are_validation_errors()
    {
        using var admin = await factory.SignedInClientAsync();
        var workspaceId = await admin.CreateWorkspaceAsync();
        var clientId = await admin.CreateClientAsync(workspaceId);
        await admin.GraphQLAsync(
            "mutation($id: ID!) { archiveClient(input: { id: $id }) { client { id } } }", new { id = clientId });

        var response = await admin.GraphQLAsync(
            """
            mutation($clientId: ID!) {
              createProject(input: { clientId: $clientId, name: "Website", color: "blue" }) {
                errors { __typename ... on ValidationError { field } }
              }
            }
            """,
            new { clientId });

        var error = Assert.Single(response.Data["createProject"]!["errors"]!.AsArray());
        Assert.Equal("clientId", error!["field"]!.GetValue<string>());
    }

    [Fact]
    public async Task People_outside_the_workspace_cannot_be_assigned()
    {
        using var admin = await factory.SignedInClientAsync();
        using var outsider = await factory.SignedInClientAsync();
        var workspaceId = await admin.CreateWorkspaceAsync();
        var projectId = await admin.CreateProjectAsync(await admin.CreateClientAsync(workspaceId));

        var response = await admin.GraphQLAsync(
            """
            mutation($projectId: ID!, $userId: ID!) {
              assignProjectMember(input: { projectId: $projectId, userId: $userId }) { errors { __typename } }
            }
            """,
            new { projectId, userId = await outsider.ViewerIdAsync() });

        Assert.Equal(["NotFoundError"], response.PayloadErrors("assignProjectMember"));
    }

    [Fact]
    public async Task Projects_list_through_the_workspace_and_the_client()
    {
        using var admin = await factory.SignedInClientAsync();
        var workspaceId = await admin.CreateWorkspaceAsync();
        var globex = await admin.CreateClientAsync(workspaceId, "Globex");
        var initech = await admin.CreateClientAsync(workspaceId, "Initech");
        await admin.CreateProjectAsync(globex, "Website");
        await admin.CreateProjectAsync(globex, "App");
        await admin.CreateProjectAsync(initech, "Audit");

        var response = await admin.GraphQLAsync(
            """
            query($id: ID!) {
              node(id: $id) {
                ... on Workspace {
                  projects(order: [{ name: DESC }]) { nodes { name } }
                  clients { nodes { name projects { totalCount } } }
                }
              }
            }
            """,
            new { id = workspaceId });

        var workspace = response.Data["node"]!;
        Assert.Equal(
            ["Website", "Audit", "App"],
            workspace["projects"]!["nodes"]!.AsArray().Select(n => n!["name"]!.GetValue<string>()));
        Assert.Equal(
            [2, 1],
            workspace["clients"]!["nodes"]!.AsArray().Select(n => n!["projects"]!["totalCount"]!.GetValue<int>()));
    }

    /// <summary>
    /// List DataLoaders project only the selected columns, so nested resolvers must declare the parent fields they read.
    /// </summary>
    [Fact]
    public async Task Nested_fields_resolve_through_projected_lists()
    {
        using var admin = await factory.SignedInClientAsync();
        var workspaceId = await admin.CreateWorkspaceAsync();
        var projectId = await admin.CreateProjectAsync(await admin.CreateClientAsync(workspaceId, "Globex"));
        await admin.AssignAsync(projectId, await admin.ViewerIdAsync());

        var response = await admin.GraphQLAsync(
            """
            query($id: ID!) {
              node(id: $id) {
                ... on Workspace {
                  clients(first: 5) { nodes { name projects(first: 5) { nodes { name client { name } assignees { email } } } } }
                  projects(first: 5) { nodes { client { name } assignees { email } } }
                }
              }
            }
            """,
            new { id = workspaceId });

        var workspace = response.Data["node"]!;
        var nested = workspace["clients"]!["nodes"]![0]!["projects"]!["nodes"]![0]!;
        Assert.Equal("Globex", nested["client"]!["name"]!.GetValue<string>());
        Assert.Equal(admin.Email, nested["assignees"]![0]!["email"]!.GetValue<string>());
        Assert.Equal("Globex", workspace["projects"]!["nodes"]![0]!["client"]!["name"]!.GetValue<string>());
    }
}
