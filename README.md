# Ora

Ora is an open source, self-hostable timesheet and approval tool for consultancies and agencies.
Teams log time against client projects in a weekly grid, and each workspace keeps its clients, projects, and people separate from every other.

This is v0.1: the foundation and a working weekly timesheet.
Approvals, invitations, and reports follow; see the [roadmap](docs/roadmap.md).

> **Screenshot placeholder.**
> Add `docs/screenshot.png` showing the weekly timesheet, then replace this note with `![The weekly timesheet](docs/screenshot.png)`.

## Features

- Sign up, sign in, and sign out with bearer tokens that refresh on their own.
- Workspaces with Admin, Manager, and Member roles; the creator is the admin, and people can switch between their workspaces.
- Clients and projects that admins create, edit, archive, restore, and staff.
- A weekly timesheet with a row per assigned project and a column per day, saved on blur with optimistic updates, and row, day, and week totals.
- Strict workspace isolation, enforced in the GraphQL resolvers and again by EF Core query filters.
- A layout that works on a phone, and dark mode that follows the system setting.

## Stack

| Area    | Choice                                                                       |
| ------- | ---------------------------------------------------------------------------- |
| API     | .NET 10, ASP.NET Core, C# with nullable reference types                      |
| GraphQL | Hot Chocolate 16.6, following the Relay server specification                 |
| Data    | EF Core 10 with SQL Server 2025                                              |
| Auth    | ASP.NET Core Identity API endpoints with bearer and refresh tokens           |
| Web     | React 19.3, TypeScript 6.0, Vite 8, Relay 21, React Router 8, Tailwind CSS 4 |
| Tests   | xUnit v3 on Microsoft.Testing.Platform, with SQL Server in Testcontainers    |
| Tooling | dotnet format, Prettier, ESLint, GitHub Actions                              |

The reasons behind the main choices live in the [architecture decision records](docs/adr/README.md).

## Run it locally

You need Docker with Compose.

```sh
cp .env.example .env
docker compose up --build
```

Compose starts SQL Server, applies migrations, then serves the API on http://localhost:5080/graphql and the web app on http://localhost:8080.

Load the demo data in a second terminal:

```sh
docker compose run --rm api seed
```

The seed command creates a workspace named Northwind Studio with four clients, seven projects, and four weeks of time.
It is safe to run twice.

| Email             | Role    |
| ----------------- | ------- |
| admin@ora.test    | Admin   |
| manager@ora.test  | Manager |
| member@ora.test   | Member  |
| designer@ora.test | Member  |

Every demo account uses the password `Demo-passw0rd`.

To start over with an empty database, run `docker compose down -v`.

## Develop without containers for the apps

Run only SQL Server in Docker, and the API and web app on your machine for hot reload.
You need the .NET 10 SDK and Node.js 24.

```sh
docker compose up -d sql

cd api
dotnet tool restore
dotnet user-secrets set "ConnectionStrings:Default" \
  "Server=localhost,1433;Database=Ora;User Id=sa;Password=<your MSSQL_SA_PASSWORD>;TrustServerCertificate=True" \
  --project Ora.Api
dotnet run --project Ora.Api -- migrate
dotnet run --project Ora.Api -- seed
dotnet run --project Ora.Api
```

```sh
cd web
cp .env.example .env
npm install
npm run dev
```

The API listens on http://localhost:5080 and serves the Nitro GraphQL IDE at `/graphql` in Development.
The web app runs on http://localhost:5173.

## Common tasks

| Task                                | Command                                                                                    |
| ----------------------------------- | ------------------------------------------------------------------------------------------ |
| Run the API tests                   | `cd api && dotnet test --solution Ora.slnx`                                                |
| Format the API                      | `cd api && dotnet format Ora.slnx`                                                         |
| Format the web app and docs         | `cd web && npm run format`                                                                 |
| Lint the web app                    | `cd web && npm run lint`                                                                   |
| Export the GraphQL schema for Relay | `cd web && npm run schema`                                                                 |
| Compile Relay artifacts             | `cd web && npm run relay`                                                                  |
| Add a migration                     | `cd api && dotnet ef migrations add <Name> --project Ora.Api --output-dir Data/Migrations` |

The tests start SQL Server in a container through Testcontainers, so Docker must be running.

After any change to the GraphQL schema, run `npm run schema` and commit `web/schema.graphql`; CI fails when it is stale.

## Configuration

| Setting                     | Environment variable         | Purpose                                              |
| --------------------------- | ---------------------------- | ---------------------------------------------------- |
| `ConnectionStrings:Default` | `ConnectionStrings__Default` | SQL Server connection string                         |
| `Cors:AllowedOrigins`       | `Cors__AllowedOrigins`       | Comma separated origins allowed to call the API      |
| Build time                  | `VITE_GRAPHQL_URL`           | The API's GraphQL endpoint, baked into the web build |

No secrets live in the repository.
Copy `.env.example` and `web/.env.example` to set local values.

## Deploy

The API is a plain ASP.NET Core app with no platform-specific code.

- In Docker, build `api/Dockerfile` from the repository root and run the image; `migrate` and `seed` are arguments to the same image.
- On Windows with IIS, run `dotnet publish api/Ora.Api -c Release` and deploy the output with the ASP.NET Core Hosting Bundle; publish generates the `web.config`. Run `dotnet Ora.Api.dll migrate` from the publish folder once per release.

The web app is static files.
Build it with `VITE_GRAPHQL_URL` pointing at the API, and serve `dist` from any static host that falls back to `index.html` for unknown paths, as `web/nginx.conf` does.
Add the web app's origin to `Cors__AllowedOrigins` on the API.

## Repository layout

```
api/
  Ora.Api/         ASP.NET Core app: domain, EF Core data, GraphQL types, commands
  Ora.Api.Tests/   Integration tests against a real SQL Server
web/
  src/             React app, with Relay fragments next to the components that use them
  schema.graphql   The exported schema the Relay compiler reads
docs/
  adr/             Architecture decision records
  roadmap.md       What comes after v0.1
docker-compose.yml
```

## License

[MIT](LICENSE)
