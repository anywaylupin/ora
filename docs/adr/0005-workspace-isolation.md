# 0005. Enforce workspace isolation in resolvers and in the database

- Status: accepted
- Date: 2026-09-27

## Context

Ora is multi-tenant: one database holds many workspaces, and a person can belong to several.
Leaking a client name or a timesheet across workspaces would be a serious breach, and a single missed check in one resolver is an easy mistake to make.

## Decision

Enforce isolation in two independent layers.

The first layer is authorization in the GraphQL resolvers.

- Every workspace-scoped read goes through a `Workspace` object, which only resolves for members.
- Every node resolver checks that the viewer belongs to the object's workspace and returns null otherwise.
- Every mutation checks the viewer's role through one `WorkspaceAccess` service, backed by a DataLoader so many checks cost one query.
- Non-members get `NotFoundError` rather than `AccessDeniedError`, so probing IDs reveals nothing about other workspaces.

The second layer is a named EF Core query filter on every workspace-owned table.

- The filter limits rows to workspaces where the current user has a membership.
- The current user comes from the HTTP request, so even a resolver that forgets a check cannot read another workspace's rows.
- Maintenance commands such as `migrate` and `seed` run with an explicit unrestricted scope.
- The memberships table itself is not filtered, because the filters are built on it; it is only reachable through the checked resolvers.
- Identity's users table has no filter, so user lookups go through a DataLoader that returns only the viewer and people who share a workspace with them.

## Consequences

- Integration tests prove that users cannot read or write another workspace's workspaces, members, users, clients, projects, time entries, or timesheet cells.
- Every query on a workspace-owned table carries an `EXISTS` subquery on memberships, served by the unique index on user and workspace.
- Code that genuinely needs to cross workspaces, such as a future admin console, must opt out explicitly, which makes it easy to review.
