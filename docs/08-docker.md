# Docker

The `Dockerfile` is multi-stage: the .NET 10 SDK builds and publishes, and the final
image contains only the ASP.NET runtime plus the published app.

## The Dockerfile

```dockerfile
# Multi-stage build: SDK for build/publish, ASP.NET runtime for the final image.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj files first for better layer caching.
COPY ["src/Api/FreshApi.Api.csproj", "src/Api/"]
COPY ["src/Application/FreshApi.Application.csproj", "src/Application/"]
COPY ["src/Domain/FreshApi.Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/FreshApi.Infrastructure.csproj", "src/Infrastructure/"]
RUN dotnet restore "src/Api/FreshApi.Api.csproj"

COPY . .
WORKDIR "/src/src/Api"
RUN dotnet build "FreshApi.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "FreshApi.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FreshApi.Api.dll"]
```

Stage by stage:

1. **`base`** — `aspnet:10.0` runtime image, exposes 8080/8081.
2. **`build`** — `sdk:10.0`. The csproj files are copied and restored *before* the
   source, so Docker layer caching means a code-only change doesn't re-restore
   packages. Then the full source is copied and built in Release.
3. **`publish`** — `dotnet publish` trims to the deployable output.
4. **`final`** — copies only `/app/publish` onto the slim runtime image. No SDK,
   no source, smaller attack surface.

Build and run it directly:

```bash
docker build -t freshapi .
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal,1433;Database=FreshApi;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;" \
  freshapi
```

Note the double-underscore: `ConnectionStrings__DefaultConnection` is how
ASP.NET Core configuration binding reads nested settings from environment
variables. This is the same mechanism compose uses below.

## Docker Compose

`docker-compose.yml` stands up the API plus SQL Server 2022:

```bash
docker compose up --build
```

- API: <http://localhost:8080> (Swagger at `/swagger`, since
  `ASPNETCORE_ENVIRONMENT=Development`)
- Health: <http://localhost:8080/health>
- SQL Server: `localhost,1433`, user `sa`, password `YourStrong!Passw0rd`
  (change this — see below)

Worth knowing about the compose file:

- The API's connection string is overridden via the `ConnectionStrings__DefaultConnection`
  env var to point at the `sqlserver` service — no code change needed.
- `depends_on` with `condition: service_healthy` holds the API until SQL Server
  passes its healthcheck (`sqlcmd -Q "SELECT 1"` every 10s, 10 retries). Without
  this the API would start, fail its own `/health` probe, and crash-loop while SQL
  Server was still booting.
- `sqlserver-data` is a named volume — your database survives `docker compose down`.
  (`docker compose down -v` deletes it.)

## Before you ship the compose setup anywhere real

- **Change the `sa` password.** `YourStrong!Passw0rd` is a placeholder. Use a strong
  generated password in both `SA_PASSWORD` and the API connection string.
- **Don't run `ASPNETCORE_ENVIRONMENT=Development` in production** — it enables the
  Swagger UI and detailed error pages. Compose is the *local dev* stack; for
  production images see [11 — Deployment](11-deployment.md).
- The JWT signing key (`Jwt:Key`) defaults to `REPLACE-WITH-A-SECURE-256-BIT-KEY-IN-PRODUCTION`
  in `appsettings.json` — override it via env var (`Jwt__Key`) or your platform's
  secret store before the API serves real traffic.
- The demo login credentials (`DemoAuth:Username` / `DemoAuth:Password`) also come
  from config — override via `DemoAuth__Username` / `DemoAuth__Password` env vars,
  and replace them with a real user store before production.
