using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Ora.Api.Domain;

namespace Ora.Api.Data;

/// <summary>
/// The single database context for Ora, including Identity and data protection keys.
/// </summary>
/// <remarks>
/// Every workspace-owned table carries a named query filter that limits rows to workspaces the current user belongs to.
///
/// Memberships themselves are not filtered because the filters are built on them, and EF Core would otherwise recurse.
/// </remarks>
public sealed class OraDbContext(DbContextOptions<OraDbContext> options, IDataScope scope)
    : IdentityUserContext<User, Guid>(options), IDataProtectionKeyContext
{
    /// <summary>
    /// The name of the query filter that enforces workspace isolation.
    /// </summary>
    public const string WorkspaceFilter = "WorkspaceMember";

    public DbSet<Workspace> Workspaces => Set<Workspace>();

    public DbSet<Membership> Memberships => Set<Membership>();

    public DbSet<Client> Clients => Set<Client>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectAssignment> ProjectAssignments => Set<ProjectAssignment>();

    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    /// <summary>
    /// Read by the query filters on each query, so EF Core parameterizes it per context instance.
    /// </summary>
    private Guid? CurrentUserId => scope.UserId;

    private bool IsUnrestricted => scope.IsUnrestricted;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Workspace>(workspace =>
        {
            workspace.Property(w => w.Name).HasMaxLength(Workspace.NameMaxLength);
            workspace.HasQueryFilter(
                WorkspaceFilter,
                w => IsUnrestricted || Memberships.Any(m => m.WorkspaceId == w.Id && m.UserId == CurrentUserId));
        });

        builder.Entity<Membership>(membership =>
        {
            membership.Property(m => m.Role).HasConversion<string>().HasMaxLength(20);
            membership.HasIndex(m => new { m.UserId, m.WorkspaceId }).IsUnique();
            membership.HasIndex(m => m.WorkspaceId);
            membership.HasOne<User>().WithMany().HasForeignKey(m => m.UserId);
            membership.HasOne<Workspace>().WithMany().HasForeignKey(m => m.WorkspaceId);
        });

        builder.Entity<Client>(client =>
        {
            client.Property(c => c.Name).HasMaxLength(Client.NameMaxLength);
            client.HasIndex(c => new { c.WorkspaceId, c.Name });
            client.HasOne<Workspace>().WithMany().HasForeignKey(c => c.WorkspaceId);
            client.HasQueryFilter(
                WorkspaceFilter,
                c => IsUnrestricted || Memberships.Any(m => m.WorkspaceId == c.WorkspaceId && m.UserId == CurrentUserId));
        });

        builder.Entity<Project>(project =>
        {
            project.Property(p => p.Name).HasMaxLength(Project.NameMaxLength);
            project.Property(p => p.Color).HasMaxLength(7).IsFixedLength().IsUnicode(false);
            project.HasIndex(p => new { p.WorkspaceId, p.Name });
            project.HasIndex(p => p.ClientId);
            project.HasOne<Workspace>().WithMany().HasForeignKey(p => p.WorkspaceId);
            project.HasOne<Client>().WithMany().HasForeignKey(p => p.ClientId);
            project.HasQueryFilter(
                WorkspaceFilter,
                p => IsUnrestricted || Memberships.Any(m => m.WorkspaceId == p.WorkspaceId && m.UserId == CurrentUserId));
        });

        builder.Entity<ProjectAssignment>(assignment =>
        {
            assignment.HasKey(a => new { a.ProjectId, a.UserId });
            assignment.HasIndex(a => a.UserId);
            assignment.HasOne<Project>().WithMany().HasForeignKey(a => a.ProjectId);
            assignment.HasOne<User>().WithMany().HasForeignKey(a => a.UserId);
            assignment.HasQueryFilter(
                WorkspaceFilter,
                a => IsUnrestricted || Projects.Any(p => p.Id == a.ProjectId));
        });

        builder.Entity<TimeEntry>(entry =>
        {
            entry.Property(e => e.Note).HasMaxLength(TimeEntry.NoteMaxLength);
            entry.HasIndex(e => new { e.UserId, e.ProjectId, e.Date }).IsUnique();
            entry.HasIndex(e => new { e.WorkspaceId, e.UserId, e.Date });
            entry.HasIndex(e => e.ProjectId);
            entry.HasOne<Workspace>().WithMany().HasForeignKey(e => e.WorkspaceId);
            entry.HasOne<User>().WithMany().HasForeignKey(e => e.UserId);
            entry.HasOne<Project>().WithMany().HasForeignKey(e => e.ProjectId);
            entry.ToTable(t => t.HasCheckConstraint(
                "CK_TimeEntries_DurationMinutes",
                $"[DurationMinutes] BETWEEN 1 AND {TimeEntry.MaxMinutesPerDay}"));
            entry.HasQueryFilter(
                WorkspaceFilter,
                e => IsUnrestricted || Memberships.Any(m => m.WorkspaceId == e.WorkspaceId && m.UserId == CurrentUserId));
        });

        RestrictDomainDeletes(builder);
    }

    /// <summary>
    /// Ora never hard deletes workspaces, clients, or projects, and SQL Server rejects the multiple cascade paths that defaults would create.
    /// </summary>
    private static void RestrictDomainDeletes(ModelBuilder builder)
    {
        var domainTypes = new[]
        {
            typeof(Membership), typeof(Client), typeof(Project), typeof(ProjectAssignment), typeof(TimeEntry),
        };

        foreach (var foreignKey in domainTypes.SelectMany(t => builder.Model.FindEntityType(t)!.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
