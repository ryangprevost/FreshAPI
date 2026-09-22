# Deployment

FreshAPI is deployable as a plain `dotnet publish` output, a container, or to
Azure App Service. Whichever you choose, the same checklist applies: secrets,
database, migrations, and health probes.

## Pre-deploy checklist

1. **`Jwt:Key`** — `appsettings.json` ships with
   `REPLACE-WITH-A-SECURE-256-BIT-KEY-IN-PRODUCTION`. Replace it with a real
   256-bit key supplied as an environment variable (`Jwt__Key`) or app setting.
   Never commit it.
2. **`ConnectionStrings:DefaultConnection`** — supply per environment; never commit
   production credentials. In containers: `ConnectionStrings__DefaultConnection`.
3. **Database** — the schema must exist. Either apply migrations at deploy time
   (below) or pre-create the schema from a reviewed SQL script.
4. **Auth** — JWT *validation* is configured and token *issuing* ships out of the
   box: `POST /api/auth/login` returns a bearer token. The default credentials
   come from the `DemoAuth` config section (username `demo`, password
   `ChangeMe123!`) — replace them with a real user store (ASP.NET Core
   Identity, your database, Entra ID, Auth0, …) before production, and
   uncomment `// [Authorize]` on the controllers you want to protect.

## Option A — Azure App Service

1. Publish the API (or use the CI artifact — see
   [09 — GitHub Actions](09-github-actions.md) / [10 — Azure DevOps](10-azure-devops.md)):

   ```bash
   dotnet publish src/Api/FreshApi.Api.csproj -c Release -o publish
   ```

2. Deploy the `publish` folder to the App Service (zip deploy, or
   `azure/webapps-deploy@v3` in GitHub Actions).
3. Set app settings in the portal (or via IaC):
   - `ConnectionStrings__DefaultConnection` — the Azure SQL connection string
   - `Jwt__Key` — the signing key (prefer a Key Vault reference)
   - `ASPNETCORE_ENVIRONMENT` — `Production` (keep `Development` off: it enables
     Swagger UI and detailed error pages)
4. Point the App Service health check / your load balancer at `/health` — it
   reports SQL Server reachability via the registered health check
   (`AspNetCore.HealthChecks.SqlServer` in `src/Infrastructure/DependencyInjection.cs`).

## Option B — Containers

The image is multi-stage and production-ready (see [08 — Docker](08-docker.md)):

```bash
docker build -t freshapi .
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="<prod connection string>" \
  -e Jwt__Key="<256-bit key>" \
  freshapi
```

For orchestrators (Kubernetes, ACI, App Service for Containers): liveness/readiness
probes → `/health`.

## Database migrations in CD

There are no pre-generated migrations — you created them with `dotnet ef`
(see [04 — EF Core](04-ef-core.md)). Two ways to apply them in a release:

- **Run `dotnet ef database update` as a release step** with the target connection
  string. Simple, and the migration history table tracks what's applied.
- **Generate SQL and apply it through a reviewed process:**
  `dotnet ef migrations script --project src/Infrastructure --startup-project src/Api`
  produces the full migration SQL — hand it to your DBA or apply it in the release
  pipeline. This is the safer choice for regulated environments.

Either way, run migrations *before* the new app version starts serving traffic.

## Serilog logs

File logging writes to `logs/api-.log` (rolling daily, 7 days retained) relative to
the working directory — in containers, mount a volume or ship logs to your
platform's log sink (the console sink is already on, so container platforms collect
it automatically). Tune levels in the `Serilog` section of `appsettings.json`.

## CORS

`Cors:AllowedOrigins` defaults to `http://localhost:3000` and
`http://localhost:4200`. Replace with your real front-end origins before
deploying — the "Default" CORS policy in `Program.cs` reads this setting.
