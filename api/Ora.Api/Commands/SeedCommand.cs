using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Ora.Api.Data;
using Ora.Api.Domain;

namespace Ora.Api.Commands;

/// <summary>
/// Creates a demo workspace with a user per role, clients, projects, and a few weeks of time.
/// </summary>
/// <remarks>
/// Running it twice is safe: it stops early when the demo admin already exists.
/// </remarks>
public static class SeedCommand
{
    /// <summary>
    /// Shared by every demo user; the demo holds no real data, so a known password is the point.
    /// </summary>
    public const string DemoPassword = "Demo-passw0rd";

    public const string WorkspaceName = "Northwind Studio";

    private const int WeeksOfHistory = 4;

    private static readonly (string Email, MembershipRole Role)[] People =
    [
        ("admin@ora.test", MembershipRole.Admin),
        ("manager@ora.test", MembershipRole.Manager),
        ("member@ora.test", MembershipRole.Member),
        ("designer@ora.test", MembershipRole.Member),
    ];

    private static readonly (string Client, bool IsArchived, (string Name, string Color, bool IsArchived)[] Projects)[] Catalog =
    [
        ("Globex", false, [("Website redesign", "#3b82f6", false), ("Mobile app", "#8b5cf6", false)]),
        ("Initech", false, [("Data migration", "#f59e0b", false), ("Support retainer", "#10b981", false)]),
        ("Umbrella", false, [("Brand refresh", "#ef4444", false), ("Launch campaign", "#ec4899", true)]),
        ("Hooli", true, [("Discovery workshop", "#64748b", false)]),
    ];

    /// <summary>
    /// Which projects each person works on, by index into the flattened catalog.
    /// </summary>
    private static readonly int[][] Assignments = [[0, 2], [0, 1, 3], [0, 1, 2], [1, 4]];

    public static async Task<int> RunAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await DatabaseCommands.MigrateAsync(services, cancellationToken);

        await using var scope = services.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(SeedCommand));
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        await using var db = DatabaseCommands.CreateUnrestrictedContext(scope.ServiceProvider);

        if (await userManager.FindByEmailAsync(People[0].Email) is not null)
        {
            logger.LogInformation("Demo data already exists, nothing to do");
            return 0;
        }

        var users = new List<User>();
        foreach (var (email, _) in People)
        {
            var user = new User { UserName = email, Email = email, EmailConfirmed = true };
            var result = await userManager.CreateAsync(user, DemoPassword);
            if (!result.Succeeded)
            {
                logger.LogError("Could not create {Email}: {Errors}", email, string.Join(" ", result.Errors.Select(e => e.Description)));
                return 1;
            }

            users.Add(user);
        }

        var workspace = new Workspace { Name = WorkspaceName };
        db.Workspaces.Add(workspace);
        db.Memberships.AddRange(People.Select((person, i) =>
            new Membership { UserId = users[i].Id, WorkspaceId = workspace.Id, Role = person.Role }));

        var projects = new List<Project>();
        foreach (var (clientName, clientArchived, clientProjects) in Catalog)
        {
            var client = new Client { WorkspaceId = workspace.Id, Name = clientName, IsArchived = clientArchived };
            db.Clients.Add(client);
            projects.AddRange(clientProjects.Select(p => new Project
            {
                WorkspaceId = workspace.Id,
                ClientId = client.Id,
                Name = p.Name,
                Color = p.Color,
                IsArchived = p.IsArchived,
            }));
        }

        db.Projects.AddRange(projects);
        db.ProjectAssignments.AddRange(Assignments.SelectMany((projectIndexes, userIndex) =>
            projectIndexes.Select(i => new ProjectAssignment { ProjectId = projects[i].Id, UserId = users[userIndex].Id })));
        db.TimeEntries.AddRange(BuildEntries(workspace, users, projects, DateOnly.FromDateTime(DateTime.Today)));

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Seeded {Workspace}. Sign in as {Emails} with the password {Password}",
            WorkspaceName,
            string.Join(", ", People.Select(p => p.Email)),
            DemoPassword);
        return 0;
    }

    /// <summary>
    /// Fills past weekdays with six to eight hours split across each person's projects, in quarter hours.
    /// </summary>
    /// <remarks>
    /// A fixed random seed keeps the demo identical on every machine.
    /// </remarks>
    private static IEnumerable<TimeEntry> BuildEntries(
        Workspace workspace,
        List<User> users,
        List<Project> projects,
        DateOnly today)
    {
        var random = new Random(20260101);
        var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        var firstMonday = today.AddDays(-daysSinceMonday - (7 * (WeeksOfHistory - 1)));
        string?[] notes = [null, null, "Planning", "Client call", "Review feedback", "Implementation", null];

        for (var date = firstMonday; date <= today; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                continue;
            }

            for (var userIndex = 0; userIndex < users.Count; userIndex++)
            {
                var active = Assignments[userIndex].Select(i => projects[i]).Where(p => !p.IsArchived).ToList();
                var quarters = random.Next(24, 33);
                var split = random.Next(1, quarters);
                var shares = active.Count == 1 ? [quarters] : new[] { split, quarters - split };

                for (var i = 0; i < Math.Min(active.Count, shares.Length); i++)
                {
                    yield return new TimeEntry
                    {
                        WorkspaceId = workspace.Id,
                        UserId = users[userIndex].Id,
                        ProjectId = active[(i + date.DayNumber) % active.Count].Id,
                        Date = date,
                        DurationMinutes = shares[i] * 15,
                        Note = notes[random.Next(notes.Length)],
                    };
                }
            }
        }
    }
}
