---
name: container-publish
description: >
  Dockerfile-less containerization using the .NET SDK container publishing
  feature. Covers MSBuild properties, chiseled images, multi-arch builds, and
  registry publishing — all without writing a Dockerfile.
  Load this skill when the user wants to containerize without a Dockerfile, or
  mentions "dotnet publish container", "PublishContainer", "ContainerRepository",
  "ContainerFamily", "chiseled", "distroless", "container publish", "SDK
  container", "no Dockerfile", or "containerize without Docker".
user-invocable: true
argument-hint: "[project to containerize]"
allowed-tools: Read, Write, Edit, Bash
---

# Container Publishing (No Dockerfile)

## Core Principles

1. **No Dockerfile needed** — The .NET SDK builds OCI-compliant container images directly from `dotnet publish /t:PublishContainer`. No Dockerfile to write or maintain.
2. **Chiseled images for production** — Use `noble-chiseled` base images: no shell, no package manager, smallest attack surface.
3. **Non-root by default** — .NET container images run as the `app` user automatically. Never override to root in production.
4. **Configuration in the .csproj** — All container settings are MSBuild properties, versioned with your project.

## Patterns

### Minimal Container Publish

```bash
dotnet publish /t:PublishContainer --os linux --arch x64
```

Creates a container image in your local Docker daemon using the default `aspnet` base image.

### Production-Ready .csproj Configuration

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ContainerRepository>mycompany/myapp-api</ContainerRepository>
    <ContainerFamily>noble-chiseled</ContainerFamily>
  </PropertyGroup>

  <ItemGroup>
    <ContainerPort Include="8080" Type="tcp" />
    <ContainerEnvironmentVariable Include="ASPNETCORE_HTTP_PORTS" Value="8080" />
    <ContainerEnvironmentVariable Include="DOTNET_EnableDiagnostics" Value="0" />
  </ItemGroup>

</Project>
```

### Publishing to a Registry

```bash
# GitHub Container Registry
docker login ghcr.io
dotnet publish /t:PublishContainer --os linux --arch x64 \
    -p ContainerRegistry=ghcr.io \
    -p ContainerImageTag=1.0.0

# Azure Container Registry
az acr login --name myregistry
dotnet publish /t:PublishContainer --os linux --arch x64 \
    -p ContainerRegistry=myregistry.azurecr.io
```

### Multi-Architecture Images

```xml
<PropertyGroup>
    <RuntimeIdentifiers>linux-x64;linux-arm64</RuntimeIdentifiers>
    <ContainerRuntimeIdentifiers>linux-x64;linux-arm64</ContainerRuntimeIdentifiers>
</PropertyGroup>
```

```bash
dotnet publish /t:PublishContainer
```

### Chiseled Image Variants

| ContainerFamily | Use Case | Shell | Size |
|----------------|----------|-------|------|
| *(default)* | General purpose | Yes | ~220 MB |
| `noble-chiseled` | Production (no shell) | No | ~110 MB |
| `noble-chiseled-extra` | Production with localization (ICU) | No | ~120 MB |
| `alpine` | Small size, has shell | Yes | ~112 MB |

### CI/CD with GitHub Actions

```yaml
jobs:
  publish:
    runs-on: ubuntu-latest
    permissions:
      packages: write
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}
      - run: |
          dotnet publish src/MyApp.Api/MyApp.Api.csproj \
            /t:PublishContainer --os linux --arch x64 \
            -p ContainerRegistry=ghcr.io \
            -p ContainerRepository=${{ github.repository_owner }}/myapp \
            -p ContainerImageTag=${{ github.sha }}
```

## Anti-patterns

### Don't Use Deprecated Property Names

```xml
<!-- BAD — ContainerImageName is deprecated -->
<ContainerImageName>myapp</ContainerImageName>

<!-- GOOD — use ContainerRepository -->
<ContainerRepository>myapp</ContainerRepository>
```

### Don't Forget to Target Linux on Windows

```bash
# BAD on Windows — may produce a Windows container
dotnet publish /t:PublishContainer

# GOOD — explicitly target Linux
dotnet publish /t:PublishContainer --os linux --arch x64
```

### Don't Skip Authentication Before Push

```bash
# BAD — fails with CONTAINER1013 error
dotnet publish /t:PublishContainer -p ContainerRegistry=ghcr.io

# GOOD — authenticate first
docker login ghcr.io
dotnet publish /t:PublishContainer -p ContainerRegistry=ghcr.io
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| Standard ASP.NET Core API | SDK container publishing with `noble-chiseled` |
| Worker service / console app | SDK container publishing |
| Needs native OS packages | Dockerfile (or custom base image + SDK publishing) |
| CI without Docker daemon | Tarball output with `ContainerArchiveOutputPath` |
| Multi-arch deployment | `ContainerRuntimeIdentifiers` property |
| Production image size | `noble-chiseled` (~110 MB) |
| Registry push | `ContainerRegistry` + `docker login` |
