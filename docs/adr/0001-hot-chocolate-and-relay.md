# 0001. Use Hot Chocolate on the server and Relay on the client

- Status: accepted
- Date: 2026-09-27

## Context

Ora's screens are dense and nested: a timesheet shows projects, their clients, and a week of cells, and list pages page through clients and projects.
A REST API would need either many endpoints shaped for each screen or many round trips per screen.
GraphQL lets each screen ask for exactly the data it renders in one request.

On the server, the stack is .NET, and the GraphQL server has to fit ASP.NET Core authentication, dependency injection, and EF Core.
On the client, the app should stay fast as it grows, and data requirements should live next to the components that use them.

## Decision

Use Hot Chocolate 16 as the GraphQL server.

- It is the most complete GraphQL server for .NET, maintained, and built on ASP.NET Core.
- Its source generator turns static resolver classes into types, and generates DataLoaders from plain methods, which keeps resolvers small and checked at compile time.
- It implements the Relay server specification: global object identification, the `node` field, cursor connections, and mutation conventions with typed errors.
- It integrates with EF Core for keyset pagination, filtering, sorting, and projections through `QueryContext<T>`.
- It ships cost analysis, which rejects runaway nested queries by default.

Use Relay 21 as the GraphQL client.

- Each component declares its data needs in a colocated fragment, and the compiler builds one query per screen from those fragments.
- Data masking means a component only sees the fields it asked for, so changing one component cannot silently break another.
- The normalized store updates every component showing a record when a mutation returns it, which is what makes optimistic timesheet edits cheap.
- `usePaginationFragment` and `@connection` handle infinite lists and in-place inserts and deletes without hand-written cache code.

## Consequences

- The schema is the contract, so it is exported to `web/schema.graphql` and CI fails when it is out of date.
- Relay expects the server to follow its conventions; ADR 0002 covers IDs and connections.
- Relay needs a compile step and a Babel transform. Vite 8 compiles with Oxc, so the Relay Babel plugin runs through `@rolldown/plugin-babel` on source files only.
- Both libraries move quickly between major versions. Upgrades are deliberate tasks, checked against each project's migration guide.
- Contributors need to learn Relay's model of fragments and the store, which has a steeper start than a plain fetch client.
