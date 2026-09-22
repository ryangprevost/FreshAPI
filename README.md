# FreshAPI — .NET 10 Web API Starter Kit

A production-grade starting point for building Web APIs with **Clean Architecture** on **.NET 10**.
Skip the boilerplate: solution structure, EF Core data access, MediatR pipeline with validation,
JWT authentication, Serilog logging, health checks, Docker, and CI pipelines — all wired up and working.

## What's included

- **Clean Architecture** layering: `Domain` → `Application` → `Infrastructure` → `Api`
- **Sample domain**: `Product` aggregate with full CRUD (create, read, update, delete)
- **Domain primitives**: `Result<T>` / `Error` pattern, `Entity` base with audit timestamps and domain events,
  `ValueObject` base, `Money` value object, `DomainException`
- **CQRS with MediatR**: one command/query + handler per operation, auto-registered from the assembly
- **FluentValidation** pipeline behavior — requests are validated before handlers run
- **EF Core 10 + SQL Server**: `DbContext`, entity configurations, generic repository, unit of work,
  design-time factory (`dotnet ef` ready), automatic `CreatedAt`/`UpdatedAt`
- **API essentials**: controllers, global exception middleware (RFC 7807 problem details),
  `Result` → HTTP status mapping, Swagger/OpenAPI with JWT support, `/health` endpoint,
  **JWT login endpoint** (`POST /api/auth/login` issues bearer tokens; demo credentials
  in `DemoAuth` config — swap for a real user store), CORS, Serilog
- **Tests**: xUnit + NSubstitute samples for domain rules and a command handler
- **DevOps**: multi-stage `Dockerfile`, `docker-compose.yml` (API + SQL Server),
  GitHub Actions CI and Azure DevOps pipeline

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server (LocalDB, Docker, or any reachable instance) — or just use Docker Compose
- (Optional) [Docker](https://www.docker.com/) for the containerized quickstart
- (Optional) `dotnet-ef` tool for migrations: `dotnet tool install -g dotnet-ef`

## Quickstart

### Option A — Docker (fastest)

```bash
docker compose up --build
```

- API: http://localhost:8080
- Swagger: http://localhost:8080/swagger (Development)
- Health: http://localhost:8080/health
- SQL Server: `localhost,1433` — user `sa`, password `YourStrong!Passw0rd`

### Option B — Local .NET

1. Update the connection string in `src/Api/appsettings.Development.json`
   (defaults to LocalDB).
2. Create and apply the database:
   ```bash
   dotnet ef migrations add InitialCreate --project src/Infrastructure --startup-project src/Api
   dotnet ef database update --project src/Infrastructure --startup-project src/Api
   ```
3. Run:
   ```bash
   dotnet run --project src/Api
   ```
4. Open https://localhost:7000/swagger (port from `Properties/launchSettings.json`
   — add one if your tooling needs it) and try `POST /api/products`.

### Run the tests

```bash
dotnet test
```

## Project structure

```text
FreshApi.sln
├── src/
│   ├── Domain/                  # Enterprise logic. No dependencies on anything.
│   │   ├── Common/              # Result<T>, Error, Entity, ValueObject, IDomainEvent
│   │   ├── Entities/            # Product aggregate + ProductErrors
│   │   ├── ValueObjects/        # Money (+ MoneyErrors)
│   │   ├── Events/              # ProductCreatedDomainEvent
│   │   └── Exceptions/          # DomainException
│   ├── Application/             # Use cases. Depends on Domain only.
│   │   ├── Common/
│   │   │   ├── Interfaces/      # IRepository<T>, IUnitOfWork, IApplicationDbContext
│   │   │   ├── Models/          # PagedResult<T>
│   │   │   └── Behaviors/       # ValidationBehavior (MediatR pipeline)
│   │   ├── Products/
│   │   │   ├── Commands/        # CreateProduct, UpdateProduct, DeleteProduct
│   │   │   ├── Queries/         # GetProducts (paged + search), GetProductById
│   │   │   ├── Mappings/        # Product -> ProductDto
│   │   │   └── ProductDto.cs
│   │   └── DependencyInjection.cs
│   ├── Infrastructure/          # External concerns. Depends on Application.
│   │   ├── Persistence/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   ├── ApplicationDbContextFactory.cs   # design-time (dotnet ef)
│   │   │   ├── Configurations/  # ProductConfiguration (EF mappings)
│   │   │   └── Repositories/    # Repository<T>, UnitOfWork
│   │   └── DependencyInjection.cs
│   └── Api/                     # Composition root + HTTP. Depends on all.
│       ├── Controllers/         # ProductsController (thin: send + map Result)
│       ├── Middleware/          # ExceptionHandlingMiddleware
│       ├── Common/              # ResultExtensions (Result -> HTTP)
│       ├── Program.cs
│       └── appsettings.json
└── tests/
    ├── Domain.Tests/            # Product/Money unit tests (xUnit)
    └── Application.Tests/       # Handler tests with NSubstitute mocks
```

### Dependency rule

`Api → Infrastructure → Application → Domain`. The `Domain` project references nothing.
`Application` references `Domain` only. If you find yourself adding a reference that
violates this, that's a design smell — reach for an interface in `Application` instead.

## How a request flows

```text
HTTP → ProductsController → MediatR ISender
      → ValidationBehavior (FluentValidation)
      → Handler (command: IUnitOfWork / query: IApplicationDbContext)
      → Domain (Product.Create / UpdateDetails / AdjustStock return Result)
      → ResultExtensions → 200 / 201 / 204 / 400 / 404
```

Errors are data, not exceptions: handlers return `Result` / `Result<T>` carrying a
typed `Error` (`Product.NotFound`, `Validation.Failed`, …). The controller maps them
to HTTP status codes. Truly unexpected failures are caught by
`ExceptionHandlingMiddleware` and returned as RFC 7807 problem details.

## Configuration guide

All settings live in `src/Api/appsettings.json` (+ `appsettings.Development.json` overrides).

| Setting | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string |
| `Jwt:Issuer` / `Jwt:Audience` | Expected token issuer/audience |
| `Jwt:Key` | HMAC signing key — **replace before deploying** (min 256 bits) |
| `Jwt:ExpiryMinutes` | Token lifetime used by your token service |
| `Cors:AllowedOrigins` | Front-end origins allowed to call the API |
| `Serilog` | Console + rolling file sinks (`logs/api-.log`) |

### Authentication

JWT bearer **validation** is fully configured. Token **issuing** (login/refresh) is
deliberately not included — every project does this differently (Identity, Entra ID,
Auth0, …). To protect endpoints, uncomment `// [Authorize]` on the controller once
your token service is in place. The Swagger "Authorize" button is pre-wired for testing.

## Database migrations

```bash
# Add a migration
dotnet ef migrations add <Name> --project src/Infrastructure --startup-project src/Api

# Apply to the database
dotnet ef database update --project src/Infrastructure --startup-project src/Api
```

The `ApplicationDbContextFactory` makes these commands work without running the API.

## Deployment notes

- **Azure App Service**: publish the API (`dotnet publish`), set `ConnectionStrings:DefaultConnection`
  and `Jwt:Key` as app settings (never commit secrets), use a managed identity or Key Vault
  reference for the DB password in production.
- **Containers**: `docker build -t freshapi .` then run with
  `ConnectionStrings__DefaultConnection` supplied as an environment variable.
  The image is multi-stage — the final layer contains only the ASP.NET runtime + published app.
- **Health checks**: `/health` reports SQL Server reachability. Point your load balancer /
  orchestrator probes at it.
- **EF migrations in CI/CD**: either run `dotnet ef database update` as a release step or
  generate SQL scripts (`dotnet ef migrations script`) for DBA-reviewed deployments.

## Extending the kit

1. New aggregate: add entity + errors in `Domain`, factory methods enforcing invariants.
2. New operation: add a command/query record, handler, and validator in `Application`.
3. Persistence: add `DbSet` to `IApplicationDbContext`/`ApplicationDbContext` and an
   `IEntityTypeConfiguration` in `Infrastructure`.
4. HTTP: add a thin controller action that sends the request and returns
   `result.ToActionResult()`.

## Documentation

Step-by-step guides live in [`docs/`](docs/01-getting-started.md):

1. Getting started
2. Architecture — the six projects and the dependency rule
3. Adding an entity — full tutorial: a `Customer` aggregate end to end
4. EF Core — DbContext, configurations, migrations, repository pattern
5. Validation — FluentValidation pipeline + domain invariants
6. Error handling — the `Result` pattern and HTTP mapping
7. Testing — xUnit + NSubstitute
8. Docker — multi-stage Dockerfile and Docker Compose
9. GitHub Actions — the CI pipeline
10. Azure DevOps — the Azure pipeline
11. Deployment — App Service, containers, secrets, migrations in CD
12. Customizing FreshAPI — rename, swap the database, branding

## Tech stack

.NET 10 · ASP.NET Core · EF Core 10 · MediatR 12 · FluentValidation 11 · Serilog ·
Swashbuckle · xUnit · NSubstitute · SQL Server · Docker

## License

MIT — see [LICENSE](LICENSE). Replace the copyright holder with your name.
