# CI/CD Pipelines — GitHub Actions & TeamCity Reference

## Overview

This document covers pipeline patterns for .NET 9 applications: GitHub Actions for open-source and home-machine projects, and TeamCity Kotlin DSL for work-machine projects. Both follow the same conceptual stages — restore, build, format-check, test, publish, Docker push. See `knowledge/teamcity-octopus.md` for the full TeamCity + Octopus Deploy integration including Kotlin DSL build configurations, Octopus variable substitution, and the Bitbucket → TeamCity → Octopus flow.

## Pattern: GitHub Actions — Full CI Pipeline

```yaml
# .github/workflows/ci.yml
name: CI

on:
  push:
    branches: [main, 'feature/**', 'fix/**']
  pull_request:
    branches: [main]

env:
  DOTNET_VERSION: '9.0.x'
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true  # avoids first-run setup noise in CI logs
  DOTNET_NOLOGO: true
  NUGET_PACKAGES: ${{ github.workspace }}/.nuget/packages

jobs:
  build-and-test:
    name: Build & Test
    runs-on: ubuntu-latest

    # SQL Server sidecar for integration tests — avoids needing Testcontainers in CI
    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2022-latest
        env:
          ACCEPT_EULA: Y
          SA_PASSWORD: "CI_Password123!"
        ports:
          - 1433:1433
        options: >-
          --health-cmd "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'CI_Password123!' -C -Q 'SELECT 1'"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
      - uses: actions/checkout@v4

      # Cache NuGet packages — dramatically reduces restore time on repeat runs
      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ${{ env.NUGET_PACKAGES }}
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj', '**/Directory.Packages.props') }}
          restore-keys: |
            ${{ runner.os }}-nuget-

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore dependencies
        run: dotnet restore --locked-mode  # fail if lockfile is out of date

      - name: Build
        run: dotnet build --configuration Release --no-restore

      # Format check: enforces code style on every PR — catches whitespace/brace-style drift
      - name: Check formatting
        run: dotnet format --verify-no-changes --no-restore

      - name: Run tests
        env:
          # Connection string injected into integration tests via IConfiguration
          ConnectionStrings__DefaultConnection: >-
            Server=localhost,1433;Database=MyAppTest;
            User Id=sa;Password=CI_Password123!;
            TrustServerCertificate=True;
        run: >-
          dotnet test
          --configuration Release
          --no-build
          --logger "trx;LogFileName=results.trx"
          --results-directory ./TestResults
          --collect:"XPlat Code Coverage"
          --settings coverlet.runsettings

      # Publish test results so they appear in the PR checks UI
      - name: Publish test results
        uses: dorny/test-reporter@v1
        if: always()  # run even when tests fail so you can see the report
        with:
          name: xUnit Tests
          path: TestResults/*.trx
          reporter: dotnet-trx

      - name: Upload coverage to Codecov
        uses: codecov/codecov-action@v4
        with:
          token: ${{ secrets.CODECOV_TOKEN }}
          files: TestResults/**/coverage.cobertura.xml
```

## Pattern: GitHub Actions — Build, Publish, and Docker Push

```yaml
# .github/workflows/publish.yml
name: Publish

on:
  push:
    branches: [main]

env:
  DOTNET_VERSION: '9.0.x'
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true
  DOTNET_NOLOGO: true
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository }}

jobs:
  publish:
    name: Build & Push Docker Image
    runs-on: ubuntu-latest
    # Grant write access to GHCR
    permissions:
      contents: read
      packages: write

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore
        run: dotnet restore --locked-mode

      - name: Build
        run: dotnet build --configuration Release --no-restore

      # SDK container publish: no Dockerfile needed — dotnet SDK builds the image
      # Uses chiseled Ubuntu base (smaller attack surface, no shell, no package manager)
      - name: Publish Docker image
        run: >-
          dotnet publish src/MyApp.Api/MyApp.Api.csproj
          --configuration Release
          --no-restore
          /t:PublishContainer
          /p:ContainerRegistry=${{ env.REGISTRY }}
          /p:ContainerImageName=${{ env.IMAGE_NAME }}
          /p:ContainerImageTag=${{ github.sha }}
          /p:ContainerBaseImage=mcr.microsoft.com/dotnet/aspnet:9.0-jammy-chiseled
        env:
          # GITHUB_TOKEN automatically has permission to push to GHCR with packages:write
          NUGET_AUTH_TOKEN: ${{ secrets.GITHUB_TOKEN }}

      # Log in to GHCR before pushing
      - name: Log in to GHCR
        uses: docker/login-action@v3
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      # Also tag as 'latest' for convenience
      - name: Tag and push latest
        run: |
          docker tag ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:${{ github.sha }} \
                     ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:latest
          docker push ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:${{ github.sha }}
          docker push ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:latest
```

## Pattern: GitHub Actions — Reusable Workflow

Extract the build job into a reusable workflow to share it across multiple repositories without copy-paste.

```yaml
# .github/workflows/reusable-build.yml
name: Reusable Build

on:
  workflow_call:
    inputs:
      dotnet-version:
        required: false
        type: string
        default: '9.0.x'
      project-path:
        required: true
        type: string
    secrets:
      codecov-token:
        required: false

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ inputs.dotnet-version }}
      - run: dotnet restore --locked-mode
      - run: dotnet build ${{ inputs.project-path }} --configuration Release --no-restore
      - run: dotnet format --verify-no-changes --no-restore
      - run: dotnet test --configuration Release --no-build --logger trx

---
# Caller — in another repository or workflow
# .github/workflows/ci.yml
jobs:
  build:
    uses: my-org/shared-workflows/.github/workflows/reusable-build.yml@main
    with:
      project-path: src/OrderService.Api/OrderService.Api.csproj
    secrets:
      codecov-token: ${{ secrets.CODECOV_TOKEN }}
```

## Pattern: TeamCity — Docker Image Build Step

This extends the Kotlin DSL in `knowledge/teamcity-octopus.md` with a Docker build and push step.

```kotlin
// .teamcity/settings.kts (add to existing PackAndPush build type)

// Inside the steps block of PackAndPush:
dockerBuild {
    name = "Docker Build"
    // Uses SDK container publish — no Dockerfile required
    commandType = build {
        source = customContent {
            // Run SDK publish as a Docker build step
            content = """
                FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
                WORKDIR /src
                COPY . .
                RUN dotnet publish src/OrderService.Api -c Release -o /app/publish \
                    /p:UseAppHost=false

                FROM mcr.microsoft.com/dotnet/aspnet:9.0-jammy-chiseled AS final
                WORKDIR /app
                COPY --from=build /app/publish .
                ENTRYPOINT ["dotnet", "OrderService.Api.dll"]
            """.trimIndent()
        }
        namesAndTags = "%docker.registry%/order-service:%build.number%"
        platform = ImagePlatform.Linux
    }
}

dockerPush {
    name = "Docker Push"
    namesAndTags = "%docker.registry%/order-service:%build.number%"
    server = "%docker.registry%"
}

// Parameters section — add to project or build type params
params {
    param("docker.registry", "registry.mycompany.com")
    password("docker.registry.password", "%env.DOCKER_REGISTRY_PASSWORD%")
}
```

## Pattern: EF Core Migrations in CI

Run migrations as part of the deployment pipeline — never apply them manually in production.

```yaml
# GitHub Actions: apply migrations in a deployment job
  migrate:
    name: Apply EF Core Migrations
    runs-on: ubuntu-latest
    needs: [publish]   # run after the Docker image is pushed
    environment: staging

    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Install EF Core tools
        run: dotnet tool install --global dotnet-ef --version 9.*

      - name: Apply migrations
        env:
          # Connection string from GitHub Environment secrets — staging only
          ConnectionStrings__DefaultConnection: ${{ secrets.STAGING_CONNECTION_STRING }}
        run: >-
          dotnet ef database update
          --project src/MyApp.Infrastructure
          --startup-project src/MyApp.Api
          --configuration Release
          --no-build
```

## Pattern: Security Scanning in CI

Run vulnerability scanning on every PR to catch known CVEs before they reach main.

```yaml
# Add to ci.yml after the test job
  security:
    name: Security Scan
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      # NuGet vulnerability audit — built into dotnet restore since .NET 8
      - name: NuGet vulnerability audit
        run: dotnet restore --locked-mode
        # dotnet restore will warn on vulnerable packages; add --warnaserror to fail the build

      # Trivy: scans the Docker image for OS-level CVEs
      - name: Scan Docker image for vulnerabilities
        uses: aquasecurity/trivy-action@master
        with:
          image-ref: '${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:${{ github.sha }}'
          format: 'sarif'
          output: 'trivy-results.sarif'
          severity: 'CRITICAL,HIGH'
          exit-code: '1'   # fail the pipeline on critical/high CVEs

      - name: Upload Trivy results to GitHub Security tab
        uses: github/codeql-action/upload-sarif@v3
        if: always()
        with:
          sarif_file: 'trivy-results.sarif'
```

## Anti-patterns

### Don't use `latest` as the only Docker tag

```yaml
# BAD — "latest" is mutable; you can't roll back to a specific build because the tag
#       points to whichever image was last pushed; production references are ambiguous
- run: docker build -t myapp:latest .
- run: docker push myapp:latest

# GOOD — tag with an immutable identifier (SHA or semantic version) AND latest
- run: |
    docker build -t myapp:${{ github.sha }} -t myapp:latest .
    docker push myapp:${{ github.sha }}
    docker push myapp:latest
    # Deployments reference the SHA tag for reproducibility
```

### Don't skip `--locked-mode` on restore

```yaml
# BAD — restores whatever NuGet resolves at runtime; a new transitive dependency version
#       can silently enter production without a code review
- run: dotnet restore

# GOOD — --locked-mode enforces that packages.lock.json matches the csproj graph;
#         any discrepancy fails the build, requiring an explicit lockfile update
- run: dotnet restore --locked-mode
# Commit packages.lock.json to the repository so CI can enforce it
```

### Don't store secrets in workflow files or appsettings committed to the repo

```yaml
# BAD — hardcoded connection string visible to anyone with repo access
env:
  ConnectionStrings__Default: "Server=prod.db;Database=MyApp;User=sa;Password=P@ssw0rd!"

# GOOD — use GitHub Environments with protection rules; secrets are injected at runtime
environment: production
env:
  ConnectionStrings__Default: ${{ secrets.PROD_CONNECTION_STRING }}
# Prod environment requires a manual approval gate in GitHub — secrets only injected after approval
```

## Reference

**GitHub Actions versions (pin to major version):**
```
actions/checkout@v4
actions/setup-dotnet@v4
actions/cache@v4
docker/login-action@v3
dorny/test-reporter@v1
codecov/codecov-action@v4
aquasecurity/trivy-action@master
github/codeql-action/upload-sarif@v3
```

**NuGet Tools:**
```
dotnet-ef        9.*   (global tool — dotnet tool install -g dotnet-ef)
dotnet-format    (built into SDK 6+, no separate install)
```

**coverlet.runsettings (place in repo root):**
```xml
<?xml version="1.0" encoding="utf-8" ?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat Code Coverage">
        <Configuration>
          <Format>cobertura</Format>
          <Exclude>[*.Tests]*,[*.IntegrationTests]*</Exclude>
          <ExcludeByAttribute>GeneratedCodeAttribute,ExcludeFromCodeCoverageAttribute</ExcludeByAttribute>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

**See also:** `knowledge/teamcity-octopus.md` — Kotlin DSL build configurations, Octopus Deploy integration, Bitbucket commit status publishing.
