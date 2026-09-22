# Customizing FreshAPI

Making FreshAPI yours means three things: renaming the namespaces, pointing it at
your database, and applying your branding. All three are mechanical.

## 1. Rename the namespaces

The kit uses `FreshApi` as the product name and root namespace (`FreshApi.Domain`,
`FreshApi.Application`, `FreshApi.Infrastructure`, `FreshApi.Api`,
`FreshApi.Application.Tests`, `FreshApi.Domain.Tests`). Pick your name — say
`Acme` — and rename:

1. **Find and replace across the repo** (excluding `bin/`, `obj/`, `.git/`):

   ```bash
   # PowerShell
   Get-ChildItem -Recurse -Include *.cs,*.csproj,*.sln,*.json,*.yml,*.md,*.ps1,*.sh `
     | Where-Object { $_.FullName -notmatch '\\(bin|obj|\.git)\\' } `
     | ForEach-Object {
         (Get-Content $_.FullName -Raw) -replace 'FreshApi', 'Acme' |
           Set-Content $_.FullName
       }
   ```

   This rewrites namespaces (`FreshApi.Domain` → `Acme.Domain`), the Swagger title,
   JWT issuer/audience (`"Issuer": "FreshApi"` in `appsettings.json`), the database
   name (`Database=FreshApi` in connection strings and `docker-compose.yml`), and
   the README.

2. **Rename the projects and folders**: `src/Domain/FreshApi.Domain.csproj` →
   `src/Domain/Acme.Domain.csproj`, folder names, and the solution
   (`FreshApi.sln` → `Acme.sln`; update the project paths inside it or re-add the
   projects with `dotnet sln add`).
3. **Rebuild and re-test:**

   ```bash
   dotnet build
   dotnet test
   ```

4. Don't forget the **Docker image name** (`docker build -t freshapi .` →
   `docker build -t acme .`) and the Swagger title in `Program.cs`
   (`"FreshAPI API"`).

Checklist of places the name appears: `*.csproj` (`RootNamespace`), namespaces in
every `.cs` file, `FreshApi.sln`, `Dockerfile` entrypoint (`FreshApi.Api.dll`),
`docker-compose.yml`, `azure-pipelines.yml`, `.github/workflows/ci.yml`
(`src/Api/FreshApi.Api.csproj`), `appsettings*.json`, README, and these docs.

## 2. Swap the database provider

The kit targets SQL Server via `Microsoft.EntityFrameworkCore.SqlServer`
(`src/Infrastructure/FreshApi.Infrastructure.csproj`). To switch — e.g. to
PostgreSQL:

1. Replace the package reference with `Npgsql.EntityFrameworkCore.PostgreSQL`
   (EF Core 10-compatible version).
2. In `src/Infrastructure/DependencyInjection.cs`, change
   `options.UseSqlServer(connectionString)` to `options.UseNpgsql(connectionString)`.
3. Swap the health check: `AddSqlServer(...)` (from
   `AspNetCore.HealthChecks.SqlServer`) → the Npgsql equivalent
   (`AspNetCore.HealthChecks.Npgsql`).
4. Update the connection strings in `appsettings.json`,
   `appsettings.Development.json`, and `docker-compose.yml`.
5. Regenerate migrations: delete the existing `Migrations` folder (if any) and run
   `dotnet ef migrations add InitialCreate` fresh — migration SQL is
   provider-specific.
6. Update `src/Infrastructure/Persistence/ApplicationDbContextFactory.cs` to use
   `UseNpgsql` so `dotnet ef` keeps working.

The owned `Money` type (`OwnsOne` in `ProductConfiguration`) maps cleanly to
columns on PostgreSQL too — no model changes needed.

## 3. Branding

- **Swagger** — `Program.cs` sets `Title = "FreshAPI API"` and the description
  ("Clean Architecture Web API starter kit (.NET 10)."). Change both, and the
  `Jwt` issuer/audience if you renamed.
- **Assembly/product name** — `RootNamespace` in each csproj, plus the `ENTRYPOINT`
  dll name in the `Dockerfile`.
- **Health endpoint** — `/health` is generic; keep it, it's what probes expect.
- **README and docs** — the README's project-structure tree and these docs use the
  `FreshApi` names; update them to match your rename (or delete the docs folder if
  you're writing your own).
- **License** — `LICENSE` is MIT with a placeholder copyright holder; replace it
  with your name or company.

## What to keep vs. what to delete

- **Keep** until you understand them: `ValidationBehavior`, `ResultExtensions`,
  `ExceptionHandlingMiddleware` — they encode the kit's core patterns.
- **Delete** when ready: the `Product` aggregate, its commands/queries, and
  `ProductsController` are scaffolding. Once you've run the
  [03 — Adding an entity](03-adding-an-entity.md) tutorial for your own first
  aggregate, delete the `Products` folders — `dotnet build` will tell you if you
  missed a reference.
