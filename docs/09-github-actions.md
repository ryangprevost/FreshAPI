# GitHub Actions CI

`.github/workflows/ci.yml` runs on every push and pull request to `main`:
restore → build → test → publish, then uploads the published API as a build artifact.

## The workflow

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore -c Release

      - name: Test
        run: dotnet test --no-build -c Release --logger "trx;LogFileName=test-results.trx"

      - name: Publish API
        run: dotnet publish src/Api/FreshApi.Api.csproj -c Release -o publish --no-build

      - name: Upload API artifact
        uses: actions/upload-artifact@v4
        with:
          name: api
          path: publish
```

Step by step:

1. **Checkout** (`actions/checkout@v4`) — clones the repo.
2. **Setup .NET** (`actions/setup-dotnet@v4`, `dotnet-version: 10.0.x`) — installs the
   latest .NET 10 SDK patch. The floating `10.0.x` tracks security updates; pin an
   exact version if you need reproducible builds.
3. **Restore** — `dotnet restore` on the solution.
4. **Build** — Release build with `--no-restore` (faster, uses the previous step's output).
5. **Test** — `dotnet test --no-build` runs both test projects
   (`FreshApi.Application.Tests`, `FreshApi.Domain.Tests`) and writes TRX results.
6. **Publish API** — `dotnet publish src/Api/FreshApi.Api.csproj` to `publish/`.
7. **Upload API artifact** (`actions/upload-artifact@v4`) — stores the published
   output as the `api` artifact, downloadable from the workflow run and usable by a
   separate deploy job.

## Extending it

Common next steps, all as new steps or jobs in the same file:

- **Deploy job** — add a `deploy` job with `needs: build` that downloads the `api`
  artifact and pushes it to Azure App Service
  (`azure/webapps-deploy@v3`), using the publish profile from a GitHub secret.
- **Database migrations** — a release step that runs
  `dotnet ef database update` against the target connection string (from secrets),
  or applies a `dotnet ef migrations script` artifact. See
  [11 — Deployment](11-deployment.md).
- **Docker image** — build and push `freshapi` to a registry with
  `docker/build-push-action`, tagging by commit SHA.

The Azure DevOps equivalent lives in `azure-pipelines.yml` — see
[10 — Azure DevOps](10-azure-devops.md).
