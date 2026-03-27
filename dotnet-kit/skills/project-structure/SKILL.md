---
name: project-structure
description: >
  .NET solution and project structure conventions. Covers .slnx format,
  Directory.Build.props, Directory.Packages.props for central package management,
  global usings, and naming conventions.
  Load this skill when: "solution structure", ".slnx", "Directory.Build.props",
  "central package management", "Directory.Packages.props", "global usings",
  ".editorconfig", "project layout", "naming conventions", "new solution",
  "add project", "csproj setup", "build properties", "global.json".
user-invocable: true
argument-hint: "[--scaffold | --audit]"
allowed-tools: Read, Write, Edit, Bash, Glob, Grep
---

# Project Structure

## Core Principles

1. **Central package management** — Use `Directory.Packages.props` to manage NuGet package versions in one place. No version numbers in individual `.csproj` files.
2. **Shared build properties** — Use `Directory.Build.props` for common settings (target framework, nullable, implicit usings). Don't repeat in every project.
3. **.slnx for solutions** — The new XML-based solution format is cleaner and more merge-friendly than the legacy `.sln` format.
4. **src/tests separation** — Source projects in `src/`, test projects in `tests/`. Clear boundary, no mixing.
5. **Pin the SDK** — Use `global.json` to pin the .NET SDK version. Prevents "works on my machine" build failures when team members have different SDKs installed.

## Patterns

### Solution Layout

```
MyApp/
├── MyApp.slnx                       # Solution file (.slnx format)
├── Directory.Build.props             # Shared MSBuild properties (all projects)
├── Directory.Packages.props          # Central package management (one version per package)
├── .editorconfig                     # Code style rules (enforced by build)
├── .gitignore
├── global.json                       # SDK version pinning
├── src/
│   ├── MyApp.Api/                    # Web API (entry point)
│   │   ├── MyApp.Api.csproj
│   │   ├── Program.cs
│   │   └── Features/
│   ├── MyApp.Domain/                 # Domain entities, value objects
│   │   └── MyApp.Domain.csproj
│   └── MyApp.Infrastructure/         # EF Core, external services
│       └── MyApp.Infrastructure.csproj
└── tests/
    └── MyApp.Api.Tests/
        └── MyApp.Api.Tests.csproj
```

### Directory.Build.props

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>13</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <!-- Suppress NuGet audit warnings as errors — handled by /dependency-audit -->
    <WarningsNotAsErrors>NU1901;NU1902;NU1903;NU1904</WarningsNotAsErrors>
  </PropertyGroup>
</Project>
```

### Directory.Packages.props (Central Package Management)

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>
    <!-- CQRS -->
    <PackageVersion Include="MediatR" Version="12.4.1" />
    <PackageVersion Include="FluentValidation.DependencyInjectionExtensions" Version="11.11.0" />

    <!-- Data -->
    <PackageVersion Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.4" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.4" />

    <!-- Observability -->
    <PackageVersion Include="Serilog.AspNetCore" Version="8.0.3" />
    <PackageVersion Include="Serilog.Sinks.Seq" Version="8.0.0" />

    <!-- Testing -->
    <PackageVersion Include="xunit" Version="2.9.3" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.4" />
    <PackageVersion Include="Testcontainers.MsSql" Version="4.1.0" />
    <PackageVersion Include="FluentAssertions" Version="7.0.0" />
    <PackageVersion Include="Microsoft.Extensions.TimeProvider.Testing" Version="9.0.4" />
  </ItemGroup>
</Project>
```

### Project File (.csproj) with Central Package Management

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <!-- No TargetFramework here — inherited from Directory.Build.props -->

  <ItemGroup>
    <!-- No Version attribute — managed centrally in Directory.Packages.props -->
    <PackageReference Include="MediatR" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />
    <PackageReference Include="Serilog.AspNetCore" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\MyApp.Domain\MyApp.Domain.csproj" />
    <ProjectReference Include="..\MyApp.Infrastructure\MyApp.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

### global.json (SDK Pinning)

```json
{
  "sdk": {
    "version": "9.0.100",
    "rollForward": "latestFeature"
  }
}
```

### .slnx Solution Format

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/MyApp.Api/MyApp.Api.csproj" />
    <Project Path="src/MyApp.Domain/MyApp.Domain.csproj" />
    <Project Path="src/MyApp.Infrastructure/MyApp.Infrastructure.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/MyApp.Api.Tests/MyApp.Api.Tests.csproj" />
  </Folder>
</Solution>
```

### Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Solution | `CompanyName.AppName` or `AppName` | `MyApp.slnx` |
| Project | `AppName.Layer` | `MyApp.Api`, `MyApp.Domain` |
| Namespace | Matches folder path | `MyApp.Api.Features.Orders` |
| Feature folder | PascalCase, plural | `Features/Orders/` |
| Test project | `ProjectName.Tests` | `MyApp.Api.Tests` |

## Anti-patterns

### Don't Scatter Package Versions

```xml
<!-- BAD — version in every .csproj, version drift between projects -->
<!-- MyApp.Api.csproj -->
<PackageReference Include="MediatR" Version="12.3.0" />
<!-- MyApp.Infrastructure.csproj -->
<PackageReference Include="MediatR" Version="12.4.1" />

<!-- GOOD — central management, one version everywhere -->
<!-- Directory.Packages.props: -->
<PackageVersion Include="MediatR" Version="12.4.1" />
<!-- .csproj: no version attribute -->
<PackageReference Include="MediatR" />
```

### Don't Repeat Build Properties

```xml
<!-- BAD — same properties copy-pasted in every .csproj -->
<PropertyGroup>
  <TargetFramework>net9.0</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>

<!-- GOOD — once in Directory.Build.props, inherited by all projects -->
```

### Don't Mix Source and Test Projects

```
# BAD — tests mixed with source; easy to accidentally ship test code
src/
  MyApp.Api/
  MyApp.Api.Tests/    ← test project inside src/

# GOOD — clear separation prevents accidental inclusion
src/
  MyApp.Api/
tests/
  MyApp.Api.Tests/
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| New solution | Use `.slnx` format |
| Package version management | `Directory.Packages.props` (central) |
| Shared build settings | `Directory.Build.props` |
| SDK version pinning | `global.json` |
| Common using directives | Global usings in `Directory.Build.props` |
| Small API (1-2 devs) | Single project (`MyApp.Api`) |
| Medium API (3-5 devs) | 2-3 projects (`Api`, `Domain`, `Infrastructure`) |
| Large / modular app | Module-per-project with shared `Contracts` |
| Multiple versions of same package | Never — use central management to enforce one version |
| Legacy `.sln` file exists | Migrate to `.slnx` — run `dotnet sln migrate MyApp.sln` |

## Execution

Scaffold or audit the solution structure — generate `Directory.Build.props`, `Directory.Packages.props`, `.slnx`, `global.json`, and `.editorconfig` — or flag violations in an existing layout.

$ARGUMENTS
