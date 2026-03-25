# Project Config — Project Level
<!--
  This file lives at .claude/project.config.md inside each repository.
  Commit it to version control — it documents project decisions for the whole team.

  It captures PROJECT-SPECIFIC settings: tech stack choices, identifiers in
  external tools (Jira key, TeamCity project ID), and connection strings.

  Settings here OVERRIDE the user-level ~/.claude/kit.config.md where they overlap.
  Example: CONFLUENCE_SPACE_KEY here overrides CONFLUENCE_DEFAULT_SPACE_KEY in user config.

  Run /project-setup to generate this file interactively.
-->

## Project Identity
```
PROJECT_NAME=                             # e.g. OrderService or CustomerPortal
PROJECT_NAMESPACE=                        # e.g. Acme.OrderService (overrides DEFAULT_NAMESPACE)
SOLUTION_PATH=                            # relative path to .sln/.slnx, e.g. src/OrderService.slnx
```

## Architecture
```
ARCHITECTURE=modular-monolith          # clean-architecture | vsa | ddd | modular-monolith
# Run /architecture-advisor if unsure
```

## Tech Stack
```
DATABASE=sqlserver                        # sqlserver | postgresql | sqlite | none
ORM=ef-core                               # ef-core | dapper | none
MESSAGING=none                            # none | wolverine | masstransit
CACHING=memory                            # memory | redis | hybrid | none
FRONTEND=none                             # none | vue | blazor | react
```

## Database Connections (local dev)
```
SQLSERVER_CONNECTION_STRING=Server=localhost;Database=;Trusted_Connection=True;TrustServerCertificate=True
NEO4J_URI=bolt://localhost:7687
NEO4J_USERNAME=neo4j
```

