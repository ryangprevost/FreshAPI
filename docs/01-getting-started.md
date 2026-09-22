# Getting Started with FreshAPI

FreshAPI is a production-ready .NET 10 Web API starter kit built on Clean Architecture.
This guide gets you from a fresh download to a working API in a few minutes.

## The guide collection

1. [01 — Getting started](01-getting-started.md) (you are here)
2. [02 — Architecture](02-architecture.md) — the six projects and the dependency rule
3. [03 — Adding an entity](03-adding-an-entity.md) — full tutorial: add a `Customer` aggregate end to end
4. [04 — EF Core](04-ef-core.md) — DbContext, configurations, migrations, repository pattern
5. [05 — Validation](05-validation.md) — FluentValidation pipeline + domain invariants
6. [06 — Error handling](06-error-handling.md) — the `Result` pattern and HTTP mapping
7. [07 — Testing](07-testing.md) — xUnit + NSubstitute samples
8. [08 — Docker](08-docker.md) — multi-stage Dockerfile and Docker Compose
9. [09 — GitHub Actions](09-github-actions.md) — the CI pipeline
10. [10 — Azure DevOps](10-azure-devops.md) — the Azure pipeline
11. [11 — Deployment](11-deployment.md) — App Service, containers, secrets, migrations in CD
12. [12 — Customizing FreshAPI](12-customizing-freshapi.md) — rename, swap the database, branding

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server (LocalDB, Docker, or any reachable instance) — or just use Docker Compose
- (Optional) [Docker](https://www.docker.com/) for the containerized quickstart
- (Optional) the EF Core CLI: `dotnet tool install -g dotnet-ef`

## Option A — Docker (fastest)

```bash
docker compose up --build
```

Then browse:

- API: <http://localhost:8080>
- Swagger: <http://localhost:8080/swagger> (Development environment)
- Health: <http://localhost:8080/health>
- SQL Server: `localhost,1433` — user `sa`, password `YourStrong!Passw0rd`

The compose file stands up the API and SQL Server 2022 together and waits for SQL
Server to be healthy before starting the API. See [08 — Docker](08-docker.md).

## Option B — Local .NET

1. Point the connection string at your SQL Server in
   `src/Api/appsettings.Development.json` (it defaults to LocalDB):

   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=FreshApi;Trusted_Connection=True;TrustServerCertificate=True;"
   }
   ```

2. Create the database schema (there are no pre-generated migrations — this is the
   first one):

   ```bash
   dotnet ef migrations add InitialCreate --project src/Infrastructure --startup-project src/Api
   dotnet ef database update --project src/Infrastructure --startup-project src/Api
   ```

   See [04 — EF Core](04-ef-core.md) for the full migrations workflow.

3. Run the API:

   ```bash
   dotnet run --project src/Api
   ```

4. Open Swagger (Development environment) and try `POST /api/products`.

## Your first API call

Create a product:

```bash
curl -X POST http://localhost:8080/api/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Widget",
    "sku": "WGT-001",
    "price": 19.99,
    "currency": "USD",
    "initialStock": 10
  }'
```

You get back `201 Created` with the new product's GUID. List products with
`GET /api/products?page=1&pageSize=20&search=widget`.

## Run the tests

```bash
dotnet test
```

This builds the solution and runs `FreshApi.Application.Tests` and
`FreshApi.Domain.Tests`. See [07 — Testing](07-testing.md).

## Solution layout at a glance

```text
FreshApi.sln
├── src/
│   ├── Domain/           # Business rules. References nothing.
│   ├── Application/      # Use cases (MediatR commands/queries). References Domain.
│   ├── Infrastructure/   # EF Core, SQL Server. References Application.
│   └── Api/              # Controllers, middleware, Program.cs. References all.
└── tests/
    ├── Domain.Tests/
    └── Application.Tests/
```

For the full picture, read [02 — Architecture](02-architecture.md) next.
