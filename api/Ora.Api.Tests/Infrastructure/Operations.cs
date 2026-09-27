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
}
