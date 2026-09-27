using Ora.Api.Tests.Infrastructure;

namespace Ora.Api.Tests.Workspaces;

public sealed class WorkspaceTests(OraApiFactory factory)
{
    [Fact]
    public async Task Viewer_requires_a_signed_in_user()
    {
        using var client = factory.CreateOraClient();

        var response = await client.GraphQLAsync("{ viewer { user { email } } }");

        Assert.Contains("AUTH_NOT_AUTHENTICATED", response.ErrorCodes);
    }

    [Fact]
    public async Task Viewer_returns_the_signed_in_user()
    {
        using var client = await factory.SignedInClientAsync();

        var response = await client.GraphQLAsync("{ viewer { user { email } } }");

        Assert.Equal(client.Email, response.Data["viewer"]!["user"]!["email"]!.GetValue<string>());
    }

    [Fact]
    public async Task Creating_a_workspace_makes_the_creator_its_admin()
    {
        using var client = await factory.SignedInClientAsync();

        var id = await client.CreateWorkspaceAsync("Northwind studio");
        var response = await client.GraphQLAsync(
            "{ viewer { workspaces { nodes { id name viewerRole } } } }");

        var workspace = Assert.Single(response.Data["viewer"]!["workspaces"]!["nodes"]!.AsArray());
        Assert.Equal(id, workspace!["id"]!.GetValue<string>());
        Assert.Equal("Northwind studio", workspace["name"]!.GetValue<string>());
        Assert.Equal("ADMIN", workspace["viewerRole"]!.GetValue<string>());
    }

    [Fact]
    public async Task Creating_a_workspace_with_a_blank_name_returns_a_validation_error()
    {
        using var client = await factory.SignedInClientAsync();

        var response = await client.GraphQLAsync(
            """
            mutation {
              createWorkspace(input: { name: "   " }) {
                workspace { id }
                errors { __typename ... on ValidationError { field message } }
              }
            }
            """);

        var payload = response.Data["createWorkspace"]!;
        Assert.Null(payload["workspace"]);
        var error = Assert.Single(payload["errors"]!.AsArray());
        Assert.Equal("ValidationError", error!["__typename"]!.GetValue<string>());
        Assert.Equal("name", error["field"]!.GetValue<string>());
    }
}
