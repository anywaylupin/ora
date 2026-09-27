using Ora.Api.Domain;
using Ora.Api.Tests.Infrastructure;

namespace Ora.Api.Tests.Catalog;

public sealed class ClientTests(OraApiFactory factory)
{
    [Fact]
    public async Task Admins_create_rename_archive_and_restore_clients()
    {
        using var admin = await factory.SignedInClientAsync();
        var workspaceId = await admin.CreateWorkspaceAsync();
        var clientId = await admin.CreateClientAsync(workspaceId, "Globex");

        await admin.GraphQLAsync(
            "mutation($id: ID!) { updateClient(input: { id: $id, name: \"Globex Corporation\" }) { client { id } } }",
            new { id = clientId });
        var archived = await admin.GraphQLAsync(
            "mutation($id: ID!) { archiveClient(input: { id: $id }) { client { name isArchived } } }",
            new { id = clientId });
        var restored = await admin.GraphQLAsync(
            "mutation($id: ID!) { restoreClient(input: { id: $id }) { client { isArchived } } }",
            new { id = clientId });

        var archivedClient = archived.Data["archiveClient"]!["client"]!;
        Assert.Equal("Globex Corporation", archivedClient["name"]!.GetValue<string>());
        Assert.True(archivedClient["isArchived"]!.GetValue<bool>());
        Assert.False(restored.Data["restoreClient"]!["client"]!["isArchived"]!.GetValue<bool>());
    }

    [Fact]
    public async Task Client_lists_page_with_cursors_and_filter_by_archived_state()
    {
        using var admin = await factory.SignedInClientAsync();
        var workspaceId = await admin.CreateWorkspaceAsync();
        foreach (var name in new[] { "Delta", "Alpha", "Charlie", "Bravo" })
        {
            await admin.CreateClientAsync(workspaceId, name);
        }

        var archivedId = await admin.CreateClientAsync(workspaceId, "Echo");
        await admin.GraphQLAsync(
            "mutation($id: ID!) { archiveClient(input: { id: $id }) { client { id } } }", new { id = archivedId });

        const string query =
            """
            query($id: ID!, $after: String) {
              node(id: $id) {
                ... on Workspace {
                  clients(first: 2, after: $after, where: { isArchived: { eq: false } }) {
                    totalCount
                    pageInfo { hasNextPage endCursor }
                    nodes { name }
                  }
                }
              }
            }
            """;
        var first = (await admin.GraphQLAsync(query, new { id = workspaceId })).Data["node"]!["clients"]!;
        var second = (await admin.GraphQLAsync(
            query, new { id = workspaceId, after = first["pageInfo"]!["endCursor"]!.GetValue<string>() }))
            .Data["node"]!["clients"]!;

        Assert.Equal(4, first["totalCount"]!.GetValue<int>());
        Assert.Equal(["Alpha", "Bravo"], first["nodes"]!.AsArray().Select(n => n!["name"]!.GetValue<string>()));
        Assert.True(first["pageInfo"]!["hasNextPage"]!.GetValue<bool>());
        Assert.Equal(["Charlie", "Delta"], second["nodes"]!.AsArray().Select(n => n!["name"]!.GetValue<string>()));
        Assert.False(second["pageInfo"]!["hasNextPage"]!.GetValue<bool>());
    }

    [Fact]
    public async Task Members_cannot_manage_clients()
    {
        using var admin = await factory.SignedInClientAsync();
        using var member = await factory.SignedInClientAsync();
        var workspaceId = await admin.CreateWorkspaceAsync();
        var clientId = await admin.CreateClientAsync(workspaceId);
        await factory.AddMemberAsync(workspaceId, member, MembershipRole.Manager);

        var create = await member.GraphQLAsync(
            "mutation($id: ID!) { createClient(input: { workspaceId: $id, name: \"Initech\" }) { errors { __typename } } }",
            new { id = workspaceId });
        var archive = await member.GraphQLAsync(
            "mutation($id: ID!) { archiveClient(input: { id: $id }) { errors { __typename } } }",
            new { id = clientId });

        Assert.Equal(["AccessDeniedError"], create.PayloadErrors("createClient"));
        Assert.Equal(["AccessDeniedError"], archive.PayloadErrors("archiveClient"));
    }

    [Fact]
    public async Task Members_can_read_clients()
    {
        using var admin = await factory.SignedInClientAsync();
        using var member = await factory.SignedInClientAsync();
        var workspaceId = await admin.CreateWorkspaceAsync();
        var clientId = await admin.CreateClientAsync(workspaceId, "Globex");
        await factory.AddMemberAsync(workspaceId, member, MembershipRole.Member);

        var response = await member.GraphQLAsync(
            "query($id: ID!) { node(id: $id) { ... on Client { name } } }", new { id = clientId });

        Assert.Equal("Globex", response.Data["node"]!["name"]!.GetValue<string>());
    }
}
