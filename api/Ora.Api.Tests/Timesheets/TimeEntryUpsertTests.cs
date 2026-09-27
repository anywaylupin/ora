using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Ora.Api.Domain;
using Ora.Api.Tests.Infrastructure;

namespace Ora.Api.Tests.Timesheets;

public sealed class TimeEntryUpsertTests(OraApiFactory factory)
{
    private const string Monday = "2026-09-21";

    private const string Upsert =
        """
        mutation($projectId: ID!, $date: LocalDate!, $minutes: Int!, $note: String) {
          upsertTimeEntry(input: { projectId: $projectId, date: $date, durationMinutes: $minutes, note: $note }) {
            timesheetCell { id date durationMinutes note entry { id } }
            errors { __typename ... on ValidationError { field } }
          }
        }
        """;

    [Fact]
    public async Task Upserting_the_same_cell_twice_updates_one_entry()
    {
        var (member, projectId) = await ArrangeAssignedMemberAsync();
        using var _ = member;

        var created = await UpsertAsync(member, projectId, Monday, 90, "Kickoff");
        var updated = await UpsertAsync(member, projectId, Monday, 150, "  Kickoff and planning  ");

        Assert.Equal(created["id"]!.GetValue<string>(), updated["id"]!.GetValue<string>());
        Assert.Equal(created["entry"]!["id"]!.GetValue<string>(), updated["entry"]!["id"]!.GetValue<string>());
        Assert.Equal(150, updated["durationMinutes"]!.GetValue<int>());
        Assert.Equal("Kickoff and planning", updated["note"]!.GetValue<string>());
        Assert.Equal(1, await CountEntriesAsync(projectId));
    }

    [Fact]
    public async Task Zero_minutes_clears_the_cell_but_keeps_its_id()
    {
        var (member, projectId) = await ArrangeAssignedMemberAsync();
        using var _ = member;
        var created = await UpsertAsync(member, projectId, Monday, 60);

        var cleared = await UpsertAsync(member, projectId, Monday, 0);

        Assert.Equal(created["id"]!.GetValue<string>(), cleared["id"]!.GetValue<string>());
        Assert.Equal(0, cleared["durationMinutes"]!.GetValue<int>());
        Assert.Null(cleared["entry"]);
        Assert.Equal(0, await CountEntriesAsync(projectId));
    }

    [Theory]
    [InlineData(-15)]
    [InlineData(24 * 60 + 1)]
    public async Task Durations_outside_one_day_are_validation_errors(int minutes)
    {
        var (member, projectId) = await ArrangeAssignedMemberAsync();
        using var _ = member;

        var response = await member.GraphQLAsync(Upsert, new { projectId, date = Monday, minutes });

        var error = Assert.Single(response.Data["upsertTimeEntry"]!["errors"]!.AsArray());
        Assert.Equal("durationMinutes", error!["field"]!.GetValue<string>());
    }

    [Fact]
    public async Task A_day_cannot_hold_more_than_24_hours_across_projects()
    {
        var (member, projectId) = await ArrangeAssignedMemberAsync();
        using var _ = member;
        var secondProjectId = await AssignNewProjectAsync(member, projectId);
        await UpsertAsync(member, projectId, Monday, 20 * 60);

        var response = await member.GraphQLAsync(
            Upsert, new { projectId = secondProjectId, date = Monday, minutes = 5 * 60 });

        Assert.Equal(["ValidationError"], response.PayloadErrors("upsertTimeEntry"));
    }

    [Fact]
    public async Task Time_cannot_be_logged_on_unassigned_or_archived_projects()
    {
        var (member, projectId) = await ArrangeAssignedMemberAsync();
        using var _ = member;
        var projectKey = await factory.DecodeIdAsync(projectId);
        await factory.WithDatabaseAsync(db => db.Projects
            .Where(p => p.Id == projectKey)
            .ExecuteUpdateAsync(p => p.SetProperty(x => x.IsArchived, true)));
        using var admin = await factory.SignedInClientAsync();
        var otherProjectId = await admin.CreateProjectAsync(await admin.CreateClientAsync(await admin.CreateWorkspaceAsync()));

        var archived = await member.GraphQLAsync(Upsert, new { projectId, date = Monday, minutes = 30 });
        var foreign = await member.GraphQLAsync(Upsert, new { projectId = otherProjectId, date = Monday, minutes = 30 });

        Assert.Equal(["ProjectNotAssignedError"], archived.PayloadErrors("upsertTimeEntry"));
        Assert.Equal(["NotFoundError"], foreign.PayloadErrors("upsertTimeEntry"));
    }

    [Fact]
    public async Task Cells_of_another_user_cannot_be_refetched()
    {
        var (member, projectId) = await ArrangeAssignedMemberAsync();
        using var _ = member;
        var cell = await UpsertAsync(member, projectId, Monday, 45);
        using var stranger = await factory.SignedInClientAsync();

        var response = await stranger.GraphQLAsync(
            "query($ids: [ID!]!) { nodes(ids: $ids) { id } }",
            new { ids = new[] { cell["id"]!.GetValue<string>(), cell["entry"]!["id"]!.GetValue<string>() } });

        Assert.All(response.Data["nodes"]!.AsArray(), Assert.Null);
    }

    private static async Task<JsonNode> UpsertAsync(OraClient client, string projectId, string date, int minutes, string? note = null)
    {
        var response = await client.GraphQLAsync(Upsert, new { projectId, date, minutes, note });
        var payload = response.Data["upsertTimeEntry"]!;
        Assert.Empty(payload["errors"]?.AsArray() ?? []);
        return payload["timesheetCell"]!;
    }

    private async Task<int> CountEntriesAsync(string projectId)
    {
        var projectKey = await factory.DecodeIdAsync(projectId);
        return await factory.WithDatabaseAsync(db => db.TimeEntries.CountAsync(e => e.ProjectId == projectKey));
    }

    /// <summary>
    /// Creates a workspace owned by a separate admin with one project assigned to a new member.
    /// </summary>
    private async Task<(OraClient Member, string ProjectId)> ArrangeAssignedMemberAsync()
    {
        using var admin = await factory.SignedInClientAsync();
        var member = await factory.SignedInClientAsync();
        var workspaceId = await admin.CreateWorkspaceAsync();
        await factory.AddMemberAsync(workspaceId, member, MembershipRole.Member);
        var projectId = await admin.CreateProjectAsync(await admin.CreateClientAsync(workspaceId));
        await admin.AssignAsync(projectId, await member.ViewerIdAsync());
        return (member, projectId);
    }

    /// <summary>
    /// Adds a second project to the same client and assigns the member, arranged directly in the database.
    /// </summary>
    private async Task<string> AssignNewProjectAsync(OraClient member, string existingProjectId)
    {
        var existingKey = await factory.DecodeIdAsync(existingProjectId);
        var project = await factory.WithDatabaseAsync(async db =>
        {
            var existing = await db.Projects.SingleAsync(p => p.Id == existingKey);
            var userId = await db.Users.Where(u => u.Email == member.Email).Select(u => u.Id).SingleAsync();
            var created = new Project
            {
                WorkspaceId = existing.WorkspaceId,
                ClientId = existing.ClientId,
                Name = "Second",
                Color = "#10b981",
            };
            db.Projects.Add(created);
            db.ProjectAssignments.Add(new ProjectAssignment { ProjectId = created.Id, UserId = userId });
            await db.SaveChangesAsync();
            return created;
        });

        var response = await member.GraphQLAsync(
            "query($id: ID!) { node(id: $id) { ... on Project { client { projects { nodes { id name } } } } } }",
            new { id = existingProjectId });
        return response.Data["node"]!["client"]!["projects"]!["nodes"]!.AsArray()
            .Single(n => n!["name"]!.GetValue<string>() == project.Name)!["id"]!.GetValue<string>();
    }
}
