# 0002. Expose global object IDs and cursor connections

- Status: accepted
- Date: 2026-09-27

## Context

Relay normalizes its store by ID and assumes every ID is unique across all types.
Database keys are only unique per table, and exposing them raw would couple clients to storage details.

Lists in Ora grow without bound: clients, projects, and later time entries and reports.
Offset pagination skips or repeats rows when data changes between pages, and gets slower with every page.

## Decision

Every entity implements the `Node` interface with an opaque global ID, and the schema exposes the `node` and `nodes` fields.

- IDs encode the type name and the key, so a client and a project with the same database key never collide.
- Clients treat IDs as opaque strings and never parse them.
- Node resolvers go through the same membership checks as every other read, so a guessed or copied ID from another workspace resolves to null.
- The timesheet cell uses a composite ID of user, project, and date. An empty cell and the entry later saved into it share one ID, so the client can apply an optimistic update by ID without any store updater code.

Lists are Relay connections built with `[UsePaging]`, `PagingArguments`, and `ToPageAsync` from GreenDonut.Data.

- Pagination is keyset based: cursors encode the sort values of the last row, and the next page starts after them with an index-friendly `WHERE` clause.
- Every list sorts by a user-facing field and then by ID, so cursors stay unique and stable.
- Nested connections, such as a client's projects, load through paged DataLoaders with `ToBatchPageAsync`, which fetches pages for many parents in one query.
- Filtering and sorting use restricted input types that expose only the fields the list pages need.

## Consequences

- Clients never see database keys, and the storage model can change without breaking them.
- Keyset pagination cannot jump to page 7 directly; Ora's lists do not need that.
- Global IDs are base64 and can contain slashes, so the web app always encodes them in URLs.
- Composite ID parts are serialized as hex rather than compressed bytes, because a compressed Guid ending in a backslash byte breaks part splitting in Hot Chocolate 16.6. A round-trip test guards this.
- List DataLoaders apply projections, so resolvers that read a parent's property declare it with `[Parent(requires: ...)]`. A regression test covers nested fields through projected lists.
