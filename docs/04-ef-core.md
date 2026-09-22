# EF Core and Data Access

FreshAPI uses **EF Core 10** with **SQL Server**. This doc covers the DbContext,
entity configurations, the repository pattern, and the migrations workflow.

## The DbContext

`src/Infrastructure/Persistence/ApplicationDbContext.cs`:

```csharp
public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
    ...
}
```

Two things to notice:

1. **`ApplyConfigurationsFromAssembly`** — every `IEntityTypeConfiguration<T>` in the
   `Infrastructure` assembly (e.g. `ProductConfiguration`) is picked up automatically.
   Adding a new aggregate means adding one configuration class — no context edits
   beyond the `DbSet` itself.
2. **Automatic audit timestamps** — `SaveChangesAsync` walks the change tracker and
   sets `CreatedAt` on adds and `UpdatedAt` on adds/modifies for every `Entity`.
   You never set these by hand.

## Entity configurations

`src/Infrastructure/Persistence/Configurations/ProductConfiguration.cs` maps the
`Product` aggregate to the `Products` table:

- `Name` / `Sku` — required, length-capped from `Product.MaxNameLength` /
  `Product.MaxSkuLength` (the constants live in `Domain` so C# and the schema agree).
- `Sku` — unique index (the database backstop for the `DuplicateSku` check).
- `Price` — an **owned type**: `builder.OwnsOne(p => p.Price, …)` maps the `Money`
  value object to `Price` (decimal 18,2) and `Currency` (char 3) columns *in the
  same table*. No separate table, no foreign key — `Money` has no identity of its own.

Note the entities use private constructors and private setters; EF Core binds to
them by convention — the `// Required by EF Core.` private constructors in
`Product` and `Money` exist for this reason.

## Repository and unit of work

`IRepository<T>` / `IUnitOfWork`
(`src/Application/Common/Interfaces/`) are declared in `Application` and
implemented in `Infrastructure` (`src/Infrastructure/Persistence/Repositories/`):

- `Repository<T>` wraps the `DbContext` set for one aggregate:
  `GetByIdAsync`, `ListAsync`, `FirstOrDefaultAsync`, `AddAsync`, `Update`, `Remove`.
- `UnitOfWork.Repository<T>()` hands out one cached repository per aggregate type
  per request, and `SaveChangesAsync` commits everything atomically.

The deliberate split (from the interface's own XML docs):

- **Commands** use `IUnitOfWork` — they change state and need the atomic save.
- **Queries** use `IApplicationDbContext` directly — reads don't need a unit of work,
  and complex reads belong on the `DbSet` (e.g. `GetProductsQueryHandler` uses
  `.AsNoTracking()` plus paging/search), not behind an ever-growing generic interface.

Both are registered in `src/Infrastructure/DependencyInjection.cs`:

```csharp
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

services.AddScoped<IApplicationDbContext>(sp =>
    sp.GetRequiredService<ApplicationDbContext>());

services.AddScoped<IUnitOfWork, UnitOfWork>();
```

## Migrations

There are **no pre-generated migrations** in the kit — you create the first one
yourself. The commands run from the repo root:

```bash
# Add a migration
dotnet ef migrations add InitialCreate --project src/Infrastructure --startup-project src/Api

# Apply it to the database
dotnet ef database update --project src/Infrastructure --startup-project src/Api

# Inspect what SQL a migration will run (useful for review / DBA sign-off)
dotnet ef migrations script --project src/Infrastructure --startup-project src/Api
```

These work without running the API because
`src/Infrastructure/Persistence/ApplicationDbContextFactory.cs` implements
`IDesignTimeDbContextFactory<ApplicationDbContext>` — `dotnet ef` uses the factory
to build a context at design time. Its connection string defaults to LocalDB and
the `FreshApi` database; edit the factory if your tooling needs a different server.

### Applying migrations to other environments

- **Local dev:** `dotnet ef database update` after pulling a branch with new migrations.
- **CI/CD:** either run `dotnet ef database update` as a release step (the pipeline
  has credentials) or generate `dotnet ef migrations script` SQL and apply it
  through your DBA-reviewed process. See [11 — Deployment](11-deployment.md).

### Evolving the schema

Every time you change an entity or configuration: `dotnet ef migrations add
<DescriptiveName>` → review the generated migration → `dotnet ef database update`.
The [03 — Adding an entity](03-adding-an-entity.md) tutorial walks through a full
example (`dotnet ef migrations add AddCustomers`).

## Connection strings

The runtime connection string comes from `src/Api/appsettings.json`
(`ConnectionStrings:DefaultConnection`) with a LocalDB override in
`appsettings.Development.json`:

| Environment | Connection string |
|---|---|
| `appsettings.json` | `Server=localhost,1433;Database=FreshApi;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;` |
| `appsettings.Development.json` | `Server=(localdb)\mssqllocaldb;Database=FreshApi;Trusted_Connection=True;TrustServerCertificate=True;` |
| Docker Compose | `Server=sqlserver,1433;...` (overridden via `ConnectionStrings__DefaultConnection` env var) |

Never commit production secrets — supply them as environment variables or app
settings at deploy time (see [11 — Deployment](11-deployment.md)). To use a
different provider (PostgreSQL, SQLite), see
[12 — Customizing FreshAPI](12-customizing-freshapi.md).

## Health checks

`AddInfrastructure` also registers a SQL Server health check
(`AspNetCore.HealthChecks.SqlServer`), exposed at `/health` via
`app.MapHealthChecks("/health")` in `Program.cs`. Point load-balancer probes at it —
and see [08 — Docker](08-docker.md) for how Compose uses it as a startup gate.
