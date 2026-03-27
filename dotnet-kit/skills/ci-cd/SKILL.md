---
name: ci-cd
description: >
  CI/CD pipeline file generation for .NET applications. Generates GitHub Actions YAML,
  TeamCity Kotlin DSL (.teamcity/settings.kts), Octopus deployment templates, and
  Azure DevOps pipelines. Reads CI_PROVIDER and CD_PROVIDER from kit config.
  Load this skill when: "CI/CD", "pipeline", "GitHub Actions", "TeamCity", "Octopus",
  "workflow", "deploy", "build pipeline", "publish", "release", "continuous integration",
  "continuous delivery", "azure devops", "kotlin dsl", "teamcity settings.kts".
user-invocable: true
argument-hint: "[provider: github-actions | teamcity | octopus | azure-devops]"
allowed-tools: Read, Write, Edit, Bash, Glob
---

# CI/CD

## Core Principles

1. **Read config first** — Check `CI_PROVIDER` and `CD_PROVIDER` in `~/.claude/kit.config.md` and `.claude/project.config.md` before generating anything. Generate for the user's actual toolchain.
2. **Generates files only** — This skill writes pipeline config files (`.github/workflows/*.yml`, `.teamcity/settings.kts`, `appsettings.octopus.json`). It never calls TeamCity, Octopus, or GitHub APIs directly. Commit the generated files and let the CI/CD system pick them up.
3. **Pipeline as code** — YAML/Kotlin pipelines committed to the repo. No click-ops in the UI; config is version-controlled and reviewable.
4. **Fast feedback** — Build and test on every push. Cache NuGet packages. Fail fast on format and test failures before reaching expensive steps.
5. **Build once, deploy many** — Build the artifact once, promote it through environments. TeamCity builds the package; Octopus deploys the same package to dev → staging → production.

## Patterns

### GitHub Actions — Build + Test

```yaml
# .github/workflows/ci.yml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

env:
  DOTNET_NOLOGO: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    permissions:
      contents: read

    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2022-latest
        env:
          SA_PASSWORD: "TestPassword123!"
          ACCEPT_EULA: "Y"
        ports:
          - 1433:1433
        options: >-
          --health-cmd "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P TestPassword123! -Q 'SELECT 1'"
          --health-interval 10s --health-timeout 5s --health-retries 5

    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.x'

      - uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: nuget-${{ hashFiles('**/Directory.Packages.props') }}
          restore-keys: nuget-

      - run: dotnet restore
      - run: dotnet build --no-restore -c Release
      - run: dotnet format --verify-no-changes --no-restore

      - run: dotnet test --no-build -c Release --logger trx --results-directory TestResults
        env:
          ConnectionStrings__Default: "Server=localhost;Database=testdb;User Id=sa;Password=TestPassword123!;TrustServerCertificate=True"

      - uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: TestResults/*.trx
```

### GitHub Actions — Publish (on tag)

```yaml
# .github/workflows/publish.yml
name: Publish

on:
  push:
    tags: ['v*']

jobs:
  publish:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write

    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.x'

      - name: Extract version
        id: version
        run: echo "VERSION=${GITHUB_REF#refs/tags/v}" >> $GITHUB_OUTPUT

      - name: Publish container
        run: |
          dotnet publish src/MyApp.Api/MyApp.Api.csproj \
            /t:PublishContainer --os linux --arch x64 \
            -p ContainerRegistry=ghcr.io \
            -p ContainerRepository=${{ github.repository }} \
            -p ContainerImageTag=${{ steps.version.outputs.VERSION }}
        env:
          CR_PAT: ${{ secrets.GITHUB_TOKEN }}
```

### TeamCity — Build Configuration (Kotlin DSL)

TeamCity uses Kotlin DSL for pipeline-as-code. Commit to `.teamcity/settings.kts` and push — TeamCity detects the change and applies it automatically.

```kotlin
// .teamcity/settings.kts
import jetbrains.buildServer.configs.kotlin.*
import jetbrains.buildServer.configs.kotlin.buildSteps.*
import jetbrains.buildServer.configs.kotlin.triggers.*

version = "2024.03"

project {
    buildType(Build)
    buildType(Pack)
}

object Build : BuildType({
    name = "Build & Test"
    id("OrderService_Build")

    vcs {
        root(DslContext.settingsRoot)
        cleanCheckout = true
    }

    steps {
        dotnetRestore { name = "Restore" }
        dotnetBuild {
            name = "Build"
            configuration = "Release"
            args = "--no-restore"
        }
        dotnetTest {
            name = "Test"
            configuration = "Release"
            args = "--no-build --logger trx"
            coverage = dotcover {}
        }
    }

    triggers {
        vcs { branchFilter = "+:*" }
    }

    features {
        xmlReport {
            reportType = XmlReport.XmlReportType.NUNIT
            rules = "**/*.trx"
        }
    }

    params {
        param("env.ConnectionStrings__Default",
            "Server=%sqlserver.host%;Database=testdb;Trusted_Connection=True;TrustServerCertificate=True")
    }
})

object Pack : BuildType({
    name = "Pack & Push to Octopus"
    id("OrderService_Pack")

    dependencies {
        snapshot(Build) { onDependencyFailure = FailureAction.FAIL_TO_START }
    }

    steps {
        dotnetPublish {
            name = "Publish"
            configuration = "Release"
            outputDir = "publish"
            args = "--no-restore"
        }
        exec {
            name = "Octopus Pack"
            path = "octo"
            arguments = "pack --id=OrderService --version=%build.number% --basePath=publish --outFolder=packages"
        }
        exec {
            name = "Octopus Push"
            path = "octo"
            arguments = "push --package=packages/OrderService.%build.number%.nupkg --server=%octopus.url% --apiKey=%octopus.api.key%"
        }
        exec {
            name = "Octopus Create Release"
            path = "octo"
            arguments = "create-release --project=OrderService --version=%build.number% --server=%octopus.url% --apiKey=%octopus.api.key%"
        }
    }

    params {
        param("octopus.url", "%env.OCTOPUS_URL%")
        password("octopus.api.key", "%env.OCTOPUS_API_KEY%")
    }
})
```

### Octopus Deploy — Deployment Process

Octopus deployment steps (configured in Octopus UI or Terraform provider):

```
Deployment Process (OrderService):
  1. Deploy Package       — Deploy OrderService.{version}.nupkg
     - Install to: C:\Services\OrderService
  2. Run EF Migrations    — Script step
     - dotnet ef database update --project OrderService.Infrastructure
  3. Health Check         — HTTP check
     - URL: https://{MachineName}/health/ready
     - Retries: 5, timeout: 30s
  4. Notify Slack         — Notification step (if SLACK_WEBHOOK_URL set)
```

Use Octostache in `appsettings.octopus.json` (committed to repo):
```json
{
  "ConnectionStrings": {
    "Default": "#{Project.ConnectionString}"
  },
  "Seq": {
    "ServerUrl": "#{Project.SeqUrl}"
  }
}
```

### Azure DevOps — Build + Test

```yaml
# azure-pipelines.yml
trigger:
  branches:
    include: [main]

pool:
  vmImage: ubuntu-latest

variables:
  dotnetVersion: '9.x'
  buildConfiguration: Release

steps:
  - task: UseDotNet@2
    inputs:
      version: $(dotnetVersion)

  - task: Cache@2
    inputs:
      key: 'nuget | **/Directory.Packages.props'
      path: '$(NUGET_PACKAGES)'

  - script: dotnet restore
  - script: dotnet build --no-restore -c $(buildConfiguration)
  - script: dotnet format --verify-no-changes --no-restore
  - script: dotnet test --no-build -c $(buildConfiguration) --logger trx

  - task: PublishTestResults@2
    inputs:
      testResultsFormat: VSTest
      testResultsFiles: '**/*.trx'
    condition: always()
```

## Anti-patterns

### Don't Call CI/CD APIs Directly

```
# BAD — Claude calls the Octopus or TeamCity REST API
curl -H "X-Octopus-ApiKey: $OCTOPUS_API_KEY" \
  "$OCTOPUS_URL/api/releases" -X POST ...

# GOOD — generate the Kotlin DSL file and commit it
# Write .teamcity/settings.kts → commit → TeamCity applies it
# Write appsettings.octopus.json → commit → Octopus uses it on next deploy
```

### Don't Hardcode Secrets

```yaml
# BAD — secret in plain text in the workflow file
env:
  DB_PASSWORD: "my-secret-password"

# GOOD — GitHub Actions secrets
env:
  DB_PASSWORD: ${{ secrets.DB_PASSWORD }}

# GOOD — TeamCity parameter (masked in UI)
args: "--password %db.password%"
```

### Don't Skip the Format Check

```yaml
# BAD — no format enforcement, style drift accumulates silently
- run: dotnet build
- run: dotnet test

# GOOD — fail fast on format violations before running tests
- run: dotnet format --verify-no-changes --no-restore
- run: dotnet test
```

### Don't Build Separately per Environment

```yaml
# BAD — building in every environment introduces non-determinism
- run: dotnet publish -c Debug   # dev build
- run: dotnet publish -c Release # prod build (different binary!)

# GOOD — build once in CI, promote through environments
# TeamCity builds the Release package once
# Octopus promotes the same artifact to dev → staging → prod
```

## Decision Guide

| Scenario | CI | CD |
|----------|----|----|
| Home / OSS project | GitHub Actions | GitHub Actions (releases) |
| Work with TeamCity | TeamCity Kotlin DSL | Octopus Deploy |
| Microsoft shop | Azure DevOps | Azure DevOps Releases |
| Docker deployment | Any CI | `/t:PublishContainer` |
| NuGet library | GitHub Actions | NuGet push on tag |
| Database migrations | Run in CI tests | Octopus step / pipeline step |
| Multiple environments | Any CI | Octopus (lifecycles) or GitHub Environments |

## How to Use

Before generating a pipeline file, check config:

```
1. Read ~/.claude/kit.config.md → CI_PROVIDER, CD_PROVIDER
2. Read .claude/project.config.md → TEAMCITY_PROJECT_ID, OCTOPUS_PROJECT, VCS_REPO
3. Generate the appropriate file(s) for the user's toolchain
4. Substitute project-specific values (project ID, package name, etc.)
5. Write the file — user commits and pushes
```

If no config exists, ask: "Are you using GitHub Actions, TeamCity, or Azure DevOps for CI?"

## Execution

Read `CI_PROVIDER` and `CD_PROVIDER` from config, then generate the appropriate pipeline configuration file(s) for the user's toolchain with project-specific values substituted — never call CI/CD APIs directly.

$ARGUMENTS
