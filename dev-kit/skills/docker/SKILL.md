---
name: docker
description: >
  Docker and Docker Compose management — start/stop services, view logs, inspect
  containers, manage the dev environment, and scaffold Dockerfiles and compose files.
  Load this skill when: "docker", "docker compose", "container", "dockerfile",
  "/docker", "docker up", "docker logs", "dev environment", "compose scaffold".
user-invocable: true
argument-hint: "[up|down|logs|ps|build|scaffold] [service]"
allowed-tools: Read, Write, Bash, Glob
---

# Docker — Container and Compose Management

## Core Principles

1. **Always use named volumes for persistent data** — Never mount raw host paths for database data in compose; named volumes survive container recreation and avoid permission issues on Windows/Mac.
2. **Health checks gate dependent services** — Use `depends_on` with `condition: service_healthy` so the API container doesn't start before SQL Server is ready to accept connections.
3. **Never put secrets in compose files** — Connection strings with passwords belong in environment variables or `.env` files (gitignored), not hardcoded in `docker-compose.yml`.
4. **Ask before destroying volumes** — Running `docker compose down --volumes` is irreversible. Always confirm with the user before including the `--volumes` flag.
5. **Detect project type before scaffolding** — Read existing project files (`.csproj`, `package.json`, etc.) to infer the correct Dockerfile base images and compose structure rather than generating a generic template.

## Patterns

### Standard Dev Stack Compose File

```yaml
# docker-compose.yml — dev environment for a .NET + SQL Server + Seq stack
services:
  api:
    build: .
    ports:
      - "5000:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__DefaultConnection: "Server=sqlserver;Database=MyApp;User Id=sa;Password=YourStr0ng!Pass;TrustServerCertificate=True;"
      Seq__ServerUrl: "http://seq:5341"
    depends_on:
      sqlserver:
        condition: service_healthy
      seq:
        condition: service_started
    networks: [backend]

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "YourStr0ng!Pass"
      ACCEPT_EULA: "Y"
      MSSQL_PID: "Express"
    ports:
      - "1433:1433"
    volumes:
      - sqlserver-data:/var/opt/mssql
    healthcheck:
      test: /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStr0ng!Pass" -Q "SELECT 1"
      interval: 10s
      timeout: 5s
      retries: 10
    networks: [backend]

  seq:
    image: datalust/seq:latest
    environment:
      ACCEPT_EULA: "Y"
    ports:
      - "5341:5341"
      - "8081:80"
    volumes:
      - seq-data:/data
    networks: [backend]

volumes:
  sqlserver-data:
  seq-data:

networks:
  backend:
```

### Multistage Dockerfile for .NET API

```dockerfile
# Dockerfile — multistage build for .NET 9 API
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore dependencies first (layer cache optimization)
COPY ["src/MyApp.Api/MyApp.Api.csproj", "src/MyApp.Api/"]
COPY ["src/MyApp.Application/MyApp.Application.csproj", "src/MyApp.Application/"]
COPY ["src/MyApp.Domain/MyApp.Domain.csproj", "src/MyApp.Domain/"]
COPY ["src/MyApp.Infrastructure/MyApp.Infrastructure.csproj", "src/MyApp.Infrastructure/"]
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]
RUN dotnet restore "src/MyApp.Api/MyApp.Api.csproj"

COPY . .
RUN dotnet publish "src/MyApp.Api/MyApp.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "MyApp.Api.dll"]
```

### Common Additional Services

```yaml
# Redis — cache and session
redis:
  image: redis:7-alpine
  ports:
    - "6379:6379"
  volumes:
    - redis-data:/data
  healthcheck:
    test: redis-cli ping
    interval: 5s
  networks: [backend]

# RabbitMQ — message broker
rabbitmq:
  image: rabbitmq:3-management-alpine
  ports:
    - "5672:5672"
    - "15672:15672"  # management UI
  environment:
    RABBITMQ_DEFAULT_USER: guest
    RABBITMQ_DEFAULT_PASS: guest
  volumes:
    - rabbitmq-data:/var/lib/rabbitmq
  networks: [backend]
```

## Anti-patterns

### Hardcoding Secrets in Compose

```yaml
# BAD — password committed to version control
services:
  sqlserver:
    environment:
      SA_PASSWORD: "MyProductionPassword123!"

# GOOD — use .env file (gitignored) or environment variables
services:
  sqlserver:
    environment:
      SA_PASSWORD: "${SQL_SA_PASSWORD}"
# .env file (gitignored):
# SQL_SA_PASSWORD=YourStr0ng!Pass
```

### No Health Checks on Dependent Services

```yaml
# BAD — API starts before SQL Server is ready, connection fails on startup
services:
  api:
    depends_on:
      - sqlserver  # only waits for container start, not readiness

# GOOD — wait for actual health
services:
  api:
    depends_on:
      sqlserver:
        condition: service_healthy
  sqlserver:
    healthcheck:
      test: /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "${SQL_SA_PASSWORD}" -Q "SELECT 1"
      interval: 10s
      retries: 10
```

### Single-Stage Dockerfile

```dockerfile
# BAD — ships the full SDK (700MB+) in the final image
FROM mcr.microsoft.com/dotnet/sdk:9.0
WORKDIR /app
COPY . .
RUN dotnet publish -c Release -o /app/publish
ENTRYPOINT ["dotnet", "/app/publish/MyApp.Api.dll"]

# GOOD — multistage: SDK for build, runtime-only for final image (~200MB)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
# ... build steps ...
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
COPY --from=build /app/publish .
```

## Decision Guide

| Scenario | Action |
|----------|--------|
| Start all dev services | `/docker up` |
| Start only one service (e.g., just seq) | `/docker up seq` |
| Stop without removing data | `/docker down` |
| Stop AND wipe all volumes | `/docker down` then confirm `--volumes` with user |
| View recent logs for a service | `/docker logs seq 100` |
| Service is stuck in restart loop | `/docker logs <service>` then investigate |
| First time setting up project | `/docker scaffold` |
| SQL Server won't start | Check logs, verify SA_PASSWORD meets complexity requirements |
| Image changes need rebuilding | `/docker build <service>` |
| Show running containers and health | `/docker ps` |

## Execution

### `/docker up [service]`
Start services:
- No service: `docker compose up -d` (all services)
- With service: `docker compose up -d {service}`
- Poll `docker compose ps` until all services show `healthy` or `running`
- Show: which services started, exposed ports, any errors

### `/docker down`
Stop services: `docker compose down`
- Ask before running with `--volumes` (destroys data)

### `/docker logs [service] [lines=50]`
Show logs:
- `docker compose logs --tail={lines} -f {service}` (or all if no service)
- Filter and highlight ERROR/WARN lines

### `/docker ps`
Show running containers with:
- Name, image, status, health, ports, uptime
- Flag any containers in `Restarting` or `Exited` state

### `/docker build [service]`
Rebuild image(s): `docker compose build --no-cache {service}`

### `/docker scaffold`
Generate Docker files for the current project:
1. Detect project type by reading existing files (`.csproj`, `package.json`, etc.)
2. Generate appropriate `Dockerfile` with multistage build
3. Generate `docker-compose.yml` with the stack appropriate for the detected project type
4. Include health checks, named volumes, and a backend network
5. Generate `.env.example` with all required variables and gitignore `.env`

## Dev Environment Services Reference
- `sqlserver` — SQL Server 2022 (port 1433, UI: SSMS or Azure Data Studio)
- `seq` — structured log viewer (port 5341 API, port 8081 UI)
- `redis` — cache/session (port 6379)
- `rabbitmq` — message broker (port 5672, management UI port 15672)
