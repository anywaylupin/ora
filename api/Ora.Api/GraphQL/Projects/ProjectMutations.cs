using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using Ora.Api.Auth;
using Ora.Api.Data;
using Ora.Api.Domain;
using Ora.Api.GraphQL.Errors;

namespace Ora.Api.GraphQL.Projects;

/// <summary>
/// Project changes and assignments are for workspace admins only.
/// </summary>
[MutationType]
public static partial class ProjectMutations
{
    /// <summary>
    /// The project joins the client's workspace, so the client decides where it lives.
    /// </summary>
    [Authorize]
    [Error<ValidationError>]
    [Error<NotFoundError>]
    [Error<AccessDeniedError>]
    public static async Task<Project> CreateProjectAsync(
        [ID<Client>] Guid clientId,
        string name,
        string color,
        WorkspaceAccess access,
        OraDbContext db,
        CancellationToken cancellationToken)
    {
        var client = await FindActiveClientAsync(clientId, nameof(clientId), db, cancellationToken);
        await access.RequireRoleAsync(client.WorkspaceId, MembershipRole.Admin, cancellationToken);

        var project = new Project
        {
            WorkspaceId = client.WorkspaceId,
            ClientId = client.Id,
            Name = Validate.Name(name, nameof(name), Project.NameMaxLength),
            Color = Validate.Color(color, nameof(color)),
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync(cancellationToken);
        return project;
    }

    [Authorize]
    [Error<ValidationError>]
    [Error<NotFoundError>]
    [Error<AccessDeniedError>]
    public static async Task<Project> UpdateProjectAsync(
        [ID<Project>] Guid id,
        [ID<Client>] Guid clientId,
        string name,
        string color,
        WorkspaceAccess access,
        OraDbContext db,
        CancellationToken cancellationToken)
    {
        var project = await FindForAdminAsync(id, access, db, cancellationToken);

        if (clientId != project.ClientId)
        {
            var client = await FindActiveClientAsync(clientId, nameof(clientId), db, cancellationToken);
            if (client.WorkspaceId != project.WorkspaceId)
            {
                throw new NotFoundException("The client was not found.");
            }

            project.ClientId = client.Id;
        }

        project.Name = Validate.Name(name, nameof(name), Project.NameMaxLength);
        project.Color = Validate.Color(color, nameof(color));
        await db.SaveChangesAsync(cancellationToken);
        return project;
    }

    /// <summary>
    /// Archiving removes the project from timesheets but keeps logged time.
    /// </summary>
    [Authorize]
    [Error<NotFoundError>]
    [Error<AccessDeniedError>]
    public static async Task<Project> ArchiveProjectAsync(
        [ID<Project>] Guid id,
        WorkspaceAccess access,
        OraDbContext db,
        CancellationToken cancellationToken) =>
        await SetArchivedAsync(id, true, access, db, cancellationToken);

    [Authorize]
    [Error<NotFoundError>]
    [Error<AccessDeniedError>]
    public static async Task<Project> RestoreProjectAsync(
        [ID<Project>] Guid id,
        WorkspaceAccess access,
        OraDbContext db,
        CancellationToken cancellationToken) =>
        await SetArchivedAsync(id, false, access, db, cancellationToken);

    /// <summary>
    /// Assigning twice is a no-op, so a retried request cannot fail.
    /// </summary>
    [Authorize]
    [Error<NotFoundError>]
    [Error<AccessDeniedError>]
    public static async Task<Project> AssignProjectMemberAsync(
        [ID<Project>] Guid projectId,
        [ID<User>] Guid userId,
        WorkspaceAccess access,
        OraDbContext db,
        CancellationToken cancellationToken)
    {
        var project = await FindForAdminAsync(projectId, access, db, cancellationToken);

        var isMember = await db.Memberships
            .AnyAsync(m => m.WorkspaceId == project.WorkspaceId && m.UserId == userId, cancellationToken);
        if (!isMember)
        {
            throw new NotFoundException("The member was not found.");
        }

        var isAssigned = await db.ProjectAssignments
            .AnyAsync(a => a.ProjectId == projectId && a.UserId == userId, cancellationToken);
        if (!isAssigned)
        {
            db.ProjectAssignments.Add(new ProjectAssignment { ProjectId = projectId, UserId = userId });
            await db.SaveChangesAsync(cancellationToken);
        }

        return project;
    }

    /// <summary>
    /// Unassigning keeps the member's logged time; the row stays visible in weeks that have entries.
    /// </summary>
    [Authorize]
    [Error<NotFoundError>]
    [Error<AccessDeniedError>]
    public static async Task<Project> UnassignProjectMemberAsync(
        [ID<Project>] Guid projectId,
        [ID<User>] Guid userId,
        WorkspaceAccess access,
        OraDbContext db,
        CancellationToken cancellationToken)
    {
        var project = await FindForAdminAsync(projectId, access, db, cancellationToken);
        await db.ProjectAssignments
            .Where(a => a.ProjectId == projectId && a.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
        return project;
    }

    private static async Task<Project> SetArchivedAsync(
        Guid id,
        bool isArchived,
        WorkspaceAccess access,
        OraDbContext db,
        CancellationToken cancellationToken)
    {
        var project = await FindForAdminAsync(id, access, db, cancellationToken);
        project.IsArchived = isArchived;
        await db.SaveChangesAsync(cancellationToken);
        return project;
    }

    private static async Task<Project> FindForAdminAsync(
        Guid id,
        WorkspaceAccess access,
        OraDbContext db,
        CancellationToken cancellationToken)
    {
        var project = await db.Projects.SingleOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException("The project was not found.");
        await access.RequireRoleAsync(project.WorkspaceId, MembershipRole.Admin, cancellationToken);
        return project;
    }

    /// <summary>
    /// New work cannot go to an archived client.
    /// </summary>
    private static async Task<Client> FindActiveClientAsync(
        Guid clientId,
        string field,
        OraDbContext db,
        CancellationToken cancellationToken)
    {
        var client = await db.Clients.SingleOrDefaultAsync(c => c.Id == clientId, cancellationToken)
            ?? throw new NotFoundException("The client was not found.");

        return client.IsArchived
            ? throw new ValidationException(field, "Choose a client that is not archived.")
            : client;
    }
}
