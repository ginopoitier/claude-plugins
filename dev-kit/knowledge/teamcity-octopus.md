# TeamCity + Octopus Deploy — .NET Reference

## Config Keys Used

From `~/.claude/kit.config.md`:
```
CI_PROVIDER=teamcity
CD_PROVIDER=octopus
TEAMCITY_BASE_URL=https://build.mycompany.com
TEAMCITY_TOKEN=...
OCTOPUS_URL=https://deploy.mycompany.com
OCTOPUS_API_KEY=API-...
OCTOPUS_SPACE=Default
```

From `.claude/project.config.md`:
```
TEAMCITY_PROJECT_ID=OrderService_Build
TEAMCITY_BUILD_TYPE=OrderService_Build_CI
OCTOPUS_PROJECT=OrderService
OCTOPUS_LIFECYCLE=Default Lifecycle
```

---

## TeamCity

### Setup — Kotlin DSL

Enable versioned settings in TeamCity project settings → "Versioned Settings" → Kotlin DSL. This stores pipeline config in `.teamcity/` in your repository.

```
.teamcity/
  settings.kts          # main project config
  pom.xml               # TeamCity DSL dependencies
```

### Full Build Configuration

```kotlin
// .teamcity/settings.kts
import jetbrains.buildServer.configs.kotlin.*
import jetbrains.buildServer.configs.kotlin.buildFeatures.*
import jetbrains.buildServer.configs.kotlin.buildSteps.*
import jetbrains.buildServer.configs.kotlin.triggers.*
import jetbrains.buildServer.configs.kotlin.vcs.*

version = "2024.03"

project {
    description = "Order Service — .NET 9 Clean Architecture"

    vcsRoot(OrderServiceVcs)
    buildType(CI)
    buildType(PackAndPush)

    params {
        param("env.ASPNETCORE_ENVIRONMENT", "Test")
        param("sqlserver.host", "tc-sqlserver.internal")
        param("octopus.url", "%env.OCTOPUS_URL%")
        password("octopus.api.key", "%env.OCTOPUS_API_KEY%")
    }
}

object OrderServiceVcs : GitVcsRoot({
    name = "Order Service"
    url = "https://bitbucket.mycompany.com/scm/myworkspace/order-service.git"
    branch = "refs/heads/main"
    branchSpec = "+:refs/heads/*"
    authMethod = token {
        userName = "x-token-auth"
        password = "%env.BITBUCKET_API_TOKEN%"
    }
})

object CI : BuildType({
    name = "CI — Build & Test"
    id("OrderService_CI")
    description = "Build and test on every commit"

    vcs {
        root(OrderServiceVcs)
        cleanCheckout = true
    }

    steps {
        dotnetRestore {
            name = "Restore"
            args = "--locked-mode"  // enforce lockfile if using nuget lock files
        }
        dotnetBuild {
            name = "Build"
            configuration = "Release"
            args = "--no-restore"
        }
        dotnetCustom {
            name = "Format check"
            args = "format --verify-no-changes --no-restore"
        }
        dotnetTest {
            name = "Test"
            configuration = "Release"
            args = "--no-build --logger trx --results-directory TestResults"
            coverage = dotcover {
                assemblyFilters = "+:OrderService.*"
            }
        }
    }

    triggers {
        vcs {
            branchFilter = "+:*\n-:main"  // trigger on all branches except main (main uses PackAndPush)
        }
    }

    features {
        pullRequests {
            vcsRootExtId = "${OrderServiceVcs.id}"
            provider = bitbucketServer {
                authType = token {
                    token = "%env.BITBUCKET_API_TOKEN%"
                }
            }
        }
        commitStatusPublisher {
            vcsRootExtId = "${OrderServiceVcs.id}"
            publisher = bitbucketServer {
                url = "https://bitbucket.mycompany.com"
                authType = token {
                    token = "%env.BITBUCKET_API_TOKEN%"
                }
            }
        }
        xmlReport {
            reportType = XmlReport.XmlReportType.NUNIT
            rules = "+:**/TestResults/*.trx"
        }
        dotCover {
            toolPath = "%teamcity.tool.JetBrains.dotCover.CommandLineTools.DEFAULT%"
        }
    }

    params {
        param("env.ConnectionStrings__Default",
            "Server=%sqlserver.host%;Database=OrderServiceTest;Trusted_Connection=True;TrustServerCertificate=True")
    }

    artifactRules = "TestResults/** => TestResults"
})

object PackAndPush : BuildType({
    name = "Pack & Push to Octopus"
    id("OrderService_PackAndPush")
    description = "Build, pack, and push release to Octopus on merge to main"

    vcs {
        root(OrderServiceVcs)
        cleanCheckout = true
        branchFilter = "+:main"
    }

    dependencies {
        // Run after CI passes on main
        snapshot(CI) {
            onDependencyFailure = FailureAction.FAIL_TO_START
        }
    }

    steps {
        dotnetPublish {
            name = "Publish"
            projects = "src/OrderService.Api/OrderService.Api.csproj"
            configuration = "Release"
            outputDir = "publish/OrderService.Api"
            args = "--no-restore --self-contained false"
        }
        // Install Octopus CLI first time: dotnet tool install octopus.cli -g
        exec {
            name = "Octopus Pack"
            path = "octo"
            arguments = """pack --id=OrderService --version=%build.number% --basePath=publish/OrderService.Api --outFolder=packages --format=Zip"""
        }
        exec {
            name = "Octopus Push"
            path = "octo"
            arguments = """push --package=packages/OrderService.%build.number%.zip --server=%octopus.url% --apiKey=%octopus.api.key% --space=%env.OCTOPUS_SPACE%"""
        }
        exec {
            name = "Octopus Create Release"
            path = "octo"
            arguments = """create-release --project=OrderService --version=%build.number% --deployTo=Staging --server=%octopus.url% --apiKey=%octopus.api.key% --space=%env.OCTOPUS_SPACE% --waitForDeployment --deploymentTimeout=00:10:00"""
        }
    }

    triggers {
        vcs {
            branchFilter = "+:main"
        }
    }

    artifactRules = "packages/*.zip => packages"
})
```

### TeamCity REST API

```bash
# Trigger a build
curl -u "$TEAMCITY_TOKEN:" \
  -X POST "$TEAMCITY_BASE_URL/app/rest/buildQueue" \
  -H "Content-Type: application/json" \
  -d '{"buildType": {"id": "OrderService_CI"}}'

# Get build status
curl -u "$TEAMCITY_TOKEN:" \
  "$TEAMCITY_BASE_URL/app/rest/builds/id:12345"

# List running builds
curl -u "$TEAMCITY_TOKEN:" \
  "$TEAMCITY_BASE_URL/app/rest/builds?locator=running:true"
```

---

## Octopus Deploy

### Project Setup

```
Project: OrderService
  Deployment Process:
    1. Deploy Package          → OrderService.zip to #{Octopus.Machine.Name}
    2. Run EF Migrations       → Script step
    3. Update app config       → Replace #{...} tokens
    4. Start service           → Restart Windows service / IIS app pool
    5. Health check            → HTTP GET /health/ready → expect 200
    6. Notify Slack            → (if SLACK_WEBHOOK_URL set in variables)

  Lifecycle: Default (Dev → Staging → Production)

  Variables:
    Project.ConnectionString   → per environment, sensitive
    Project.SeqUrl             → per environment
    Project.ApiKey             → per environment, sensitive (masked)
    Octopus.Action.Package.FeedId → NuGet/built-in feed
```

### Structured Variables with appsettings.octopus.json

Add an `appsettings.octopus.json` to the publish output for Octopus variable substitution:

```json
// src/OrderService.Api/appsettings.octopus.json
{
  "ConnectionStrings": {
    "Default": "#{Project.ConnectionString}"
  },
  "Seq": {
    "ServerUrl": "#{Project.SeqUrl}",
    "ApiKey": "#{Project.SeqApiKey}"
  },
  "Authentication": {
    "Jwt": {
      "Secret": "#{Project.JwtSecret}"
    }
  }
}
```

Include in the `.csproj` publish output:
```xml
<ItemGroup>
  <Content Include="appsettings.octopus.json">
    <CopyToPublishDirectory>Always</CopyToPublishDirectory>
  </Content>
</ItemGroup>
```

### Octopus REST API

```bash
# List projects
curl -H "X-Octopus-ApiKey: $OCTOPUS_API_KEY" \
  "$OCTOPUS_URL/api/$OCTOPUS_SPACE/projects"

# Create a release
curl -H "X-Octopus-ApiKey: $OCTOPUS_API_KEY" \
  -X POST "$OCTOPUS_URL/api/$OCTOPUS_SPACE/releases" \
  -H "Content-Type: application/json" \
  -d '{"ProjectId": "Projects-1", "Version": "1.2.3"}'

# Deploy a release to an environment
curl -H "X-Octopus-ApiKey: $OCTOPUS_API_KEY" \
  -X POST "$OCTOPUS_URL/api/$OCTOPUS_SPACE/deployments" \
  -H "Content-Type: application/json" \
  -d '{"ReleaseId": "Releases-1", "EnvironmentId": "Environments-2"}'

# Get deployment status
curl -H "X-Octopus-ApiKey: $OCTOPUS_API_KEY" \
  "$OCTOPUS_URL/api/$OCTOPUS_SPACE/deployments/Deployments-1"
```

---

## Complete Pipeline: Bitbucket → TeamCity → Octopus

```
Developer pushes feature branch
    │
    ▼
Bitbucket webhook → TeamCity
    │ Triggers: OrderService_CI
    │   ├── dotnet restore
    │   ├── dotnet build -c Release
    │   ├── dotnet format --verify-no-changes
    │   └── dotnet test
    │ Publishes: commit status → Bitbucket (green/red checkmark on PR)
    │
Developer merges PR to main
    │
    ▼
Bitbucket webhook → TeamCity
    │ Triggers: OrderService_PackAndPush (main branch only)
    │   ├── dotnet publish -c Release
    │   ├── octo pack (creates OrderService.{version}.zip)
    │   ├── octo push (uploads to Octopus built-in feed)
    │   └── octo create-release --deployTo=Staging
    │
    ▼
Octopus Deploy
    │ Auto-deploys to Staging:
    │   ├── Deploy OrderService.zip
    │   ├── Run EF migrations
    │   ├── Health check /health/ready
    │   └── Notify Slack
    │
    ▼
Manual approval in Octopus UI
    │
    ▼
Octopus Deploy to Production
    ├── Same steps as Staging
    └── Notify Slack (#releases channel)
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| New project at work | Kotlin DSL in `.teamcity/`, Octopus project via UI |
| PR validation | CI build type + Bitbucket commit status publisher |
| Main branch deployment | PackAndPush build type triggering on main |
| Multiple environments | Octopus lifecycles (Dev → Staging → Prod) |
| DB migrations in deploy | Octopus script step after package deploy |
| Secrets management | Octopus sensitive variables (masked, not in source) |
| App config per environment | `appsettings.octopus.json` with Octostache |
| Monitoring deploys | Octopus deployment dashboard + Seq correlation |
