namespace Ora.Api.Tests.Infrastructure;

/// <summary>
/// GraphQL operations that many tests use to arrange state.
/// </summary>
public static class Operations
{
    /// <summary>
    /// Creates a workspace as the client's user and returns its global ID.
    /// </summary>
    public static async Task<string> CreateWorkspaceAsync(this OraClient client, string name = "Acme consulting")
    {
        var response = await client.GraphQLAsync(
            """
            mutation($name: String!) {
              createWorkspace(input: { name: $name }) { workspace { id } errors { __typename } }
            }
            """,
            new { name });

        return response.Data["createWorkspace"]!["workspace"]!["id"]!.GetValue<string>();
    }

    /// <summary>
    /// Signs up a user in a fresh client, which stands in for a second person using Ora.
    /// </summary>
    public static async Task<OraClient> SignedInClientAsync(this OraApiFactory factory)
    {
        var client = factory.CreateOraClient();
        await client.SignUpAndSignInAsync();
        return client;
    }

    public static async Task<string> CreateClientAsync(this OraClient client, string workspaceId, string name = "Globex")
    {
        var response = await client.GraphQLAsync(
            """
            mutation($workspaceId: ID!, $name: String!) {
              createClient(input: { workspaceId: $workspaceId, name: $name }) { client { id } errors { __typename } }
            }
            """,
            new { workspaceId, name });

        return response.Data["createClient"]!["client"]!["id"]!.GetValue<string>();
    }

    public static async Task<string> CreateProjectAsync(
        this OraClient client, string clientId, string name = "Website", string color = "#3b82f6")
    {
        var response = await client.GraphQLAsync(
            """
            mutation($clientId: ID!, $name: String!, $color: String!) {
              createProject(input: { clientId: $clientId, name: $name, color: $color }) {
                project { id }
                errors { __typename }
              }
            }
            """,
            new { clientId, name, color });

        return response.Data["createProject"]!["project"]!["id"]!.GetValue<string>();
    }

    public static async Task<string> ViewerIdAsync(this OraClient client)
    {
        var response = await client.GraphQLAsync("{ viewer { user { id } } }");
        return response.Data["viewer"]!["user"]!["id"]!.GetValue<string>();
    }

    public static async Task AssignAsync(this OraClient admin, string projectId, string userId)
    {
        var response = await admin.GraphQLAsync(
            """
            mutation($projectId: ID!, $userId: ID!) {
              assignProjectMember(input: { projectId: $projectId, userId: $userId }) { errors { __typename } }
            }
            """,
            new { projectId, userId });

        Assert.Empty(response.Data["assignProjectMember"]!["errors"]?.AsArray() ?? []);
    }

    /// <summary>
    /// Returns the __typename of each payload error, or an empty list when the mutation succeeded.
    /// </summary>
    public static IReadOnlyList<string> PayloadErrors(this GraphQLResponse response, string mutation) =>
        [.. (response.Data[mutation]!["errors"]?.AsArray() ?? []).Select(e => e!["__typename"]!.GetValue<string>())];
}
