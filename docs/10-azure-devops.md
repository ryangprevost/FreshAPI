# Azure DevOps Pipeline

`azure-pipelines.yml` is the Azure DevOps equivalent of the GitHub Actions CI:
trigger on `main`, install the .NET 10 SDK, restore → build → test → publish, and
publish the API output as a build artifact.

## The pipeline

```yaml
# Azure DevOps pipeline: restore -> build -> test -> publish.
trigger:
  - main

pr:
  - main

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'

steps:
  - task: UseDotNet@2
    displayName: 'Install .NET 10 SDK'
    inputs:
      packageType: 'sdk'
      version: '10.0.x'

  - task: DotNetCoreCLI@2
    displayName: 'Restore'
    inputs:
      command: 'restore'

  - task: DotNetCoreCLI@2
    displayName: 'Build'
    inputs:
      command: 'build'
      arguments: '--no-restore -c $(buildConfiguration)'

  - task: DotNetCoreCLI@2
    displayName: 'Test'
    inputs:
      command: 'test'
      arguments: '--no-build -c $(buildConfiguration)'

  - task: DotNetCoreCLI@2
    displayName: 'Publish API'
    inputs:
      command: 'publish'
      publishWebProjects: false
      projects: 'src/Api/FreshApi.Api.csproj'
      arguments: '-c $(buildConfiguration) -o $(Build.ArtifactStagingDirectory) --no-build'

  - task: PublishBuildArtifacts@1
    displayName: 'Publish artifact'
    inputs:
      pathToPublish: '$(Build.ArtifactStagingDirectory)'
      artifactName: 'api'
```

Step by step:

1. **Triggers** — runs on pushes to `main` and on pull requests targeting `main`.
2. **Pool** — `ubuntu-latest` hosted agent.
3. **UseDotNet@2** — installs the .NET 10 SDK (`10.0.x` tracks the latest patch).
4. **DotNetCoreCLI@2: Restore / Build / Test** — mirrors the GitHub Actions steps:
   restore the solution, Release build with `--no-restore`, then test both test
   projects with `--no-build`.
5. **DotNetCoreCLI@2: Publish API** — publishes `src/Api/FreshApi.Api.csproj` to
   `$(Build.ArtifactStagingDirectory)`. (`publishWebProjects: false` plus the
   explicit `projects` path keeps it to the API project only.)
6. **PublishBuildArtifacts@1** — publishes the staging directory as the `api`
   artifact, which a release pipeline can then deploy.

## Release pipeline (your next step)

The YAML covers CI only — there's no CD stage. In Azure DevOps, create a classic
or YAML release pipeline that consumes the `api` artifact:

- **Azure App Service deploy** — `AzureWebApp@1` task with the artifact's zip,
  plus app settings for `ConnectionStrings:DefaultConnection` and `Jwt:Key`
  (use Key Vault references, never plain variables).
- **Database migrations** — a release task running `dotnet ef database update`
  against the target connection string, or apply a reviewed
  `dotnet ef migrations script`. See [11 — Deployment](11-deployment.md).

To run this pipeline, point an Azure DevOps pipeline at the repo and select
"Existing Azure Pipelines YAML file" → `azure-pipelines.yml`.
