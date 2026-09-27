using Ora.Api.Domain;
using Ora.Api.Tests.Infrastructure;

namespace Ora.Api.Tests.Timesheets;

public sealed class TimesheetQueryTests(OraApiFactory factory)
{
    private const string Week =
        """
        query($id: ID!, $weekStart: LocalDate!) {
          node(id: $id) {
            ... on Workspace {
              timesheet(weekStart: $weekStart) {
                days
                rows { isEditable project { name } cells { id date durationMinutes } }
              }
            }
          }
        }
        """;

    [Fact]
    public async Task The_week_has_a_row_per_assigned_project_and_a_cell_per_day()
    {
        using var admin = await factory.SignedInClientAsync();
        var workspaceId = await admin.CreateWorkspaceAsync();
        var clientId = await admin.CreateClientAsync(workspaceId);
        var website = await admin.CreateProjectAsync(clientId, "Website");
        await admin.CreateProjectAsync(clientId, "Not assigned");
        await admin.AssignAsync(website, await admin.ViewerIdAsync());
        await admin.GraphQLAsync(
            """
            mutation($projectId: ID!) {
              upsertTimeEntry(input: { projectId: $projectId, date: "2026-09-23", durationMinutes: 120 }) { errors { __typename } }
            }
            """,
            new { projectId = website });

        var response = await admin.GraphQLAsync(Week, new { id = workspaceId, weekStart = "2026-09-21" });

        var timesheet = response.Data["node"]!["timesheet"]!;
        Assert.Equal("2026-09-27", timesheet["days"]!.AsArray()[^1]!.GetValue<string>());
        var row = Assert.Single(timesheet["rows"]!.AsArray());
        Assert.Equal("Website", row!["project"]!["name"]!.GetValue<string>());
        Assert.True(row["isEditable"]!.GetValue<bool>());
        var cells = row["cells"]!.AsArray();
        Assert.Equal(7, cells.Count);
        Assert.Equal([0, 0, 120, 0, 0, 0, 0], cells.Select(c => c!["durationMinutes"]!.GetValue<int>()));
    }

    [Fact]
    public async Task Projects_with_time_stay_visible_but_read_only_after_unassignment()
    {
        using var admin = await factory.SignedInClientAsync();
        var workspaceId = await admin.CreateWorkspaceAsync();
        var projectId = await admin.CreateProjectAsync(await admin.CreateClientAsync(workspaceId));
        var viewerId = await admin.ViewerIdAsync();
        await admin.AssignAsync(projectId, viewerId);
        await admin.GraphQLAsync(
            """
            mutation($projectId: ID!) {
              upsertTimeEntry(input: { projectId: $projectId, date: "2026-09-21", durationMinutes: 30 }) { errors { __typename } }
            }
            """,
            new { projectId });
        await admin.GraphQLAsync(
            """
            mutation($projectId: ID!, $userId: ID!) {
              unassignProjectMember(input: { projectId: $projectId, userId: $userId }) { errors { __typename } }
            }
            """,
            new { projectId, userId = viewerId });

        var response = await admin.GraphQLAsync(Week, new { id = workspaceId, weekStart = "2026-09-21" });

        var row = Assert.Single(response.Data["node"]!["timesheet"]!["rows"]!.AsArray());
        Assert.False(row!["isEditable"]!.GetValue<bool>());
    }

    [Fact]
    public async Task Weeks_must_start_on_a_monday()
    {
        using var admin = await factory.SignedInClientAsync();
        var workspaceId = await admin.CreateWorkspaceAsync();

        var response = await admin.GraphQLAsync(Week, new { id = workspaceId, weekStart = "2026-09-22" });

        Assert.Contains("INVALID_WEEK_START", response.ErrorCodes);
    }

    [Fact]
    public async Task Timesheets_show_only_the_viewers_own_time()
    {
        using var admin = await factory.SignedInClientAsync();
        using var member = await factory.SignedInClientAsync();
        var workspaceId = await admin.CreateWorkspaceAsync();
        await factory.AddMemberAsync(workspaceId, member, MembershipRole.Member);
        var projectId = await admin.CreateProjectAsync(await admin.CreateClientAsync(workspaceId));
        await admin.AssignAsync(projectId, await admin.ViewerIdAsync());
        await admin.AssignAsync(projectId, await member.ViewerIdAsync());
        await admin.GraphQLAsync(
            """
            mutation($projectId: ID!) {
              upsertTimeEntry(input: { projectId: $projectId, date: "2026-09-21", durationMinutes: 480 }) { errors { __typename } }
            }
            """,
            new { projectId });

        var response = await member.GraphQLAsync(Week, new { id = workspaceId, weekStart = "2026-09-21" });

        var row = Assert.Single(response.Data["node"]!["timesheet"]!["rows"]!.AsArray());
        Assert.All(row!["cells"]!.AsArray(), c => Assert.Equal(0, c!["durationMinutes"]!.GetValue<int>()));
    }
}
