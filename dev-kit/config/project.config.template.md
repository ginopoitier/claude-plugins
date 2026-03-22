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
ARCHITECTURE=clean-architecture           # clean-architecture | vsa | ddd | modular-monolith
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

## Version Control (project-specific)
```
VCS_REPO=                                 # repo name, e.g. myorg/order-service (GitHub) or myworkspace/order-service (Bitbucket)
DEFAULT_BRANCH=main                       # overrides user VCS_DEFAULT_BRANCH if set
```

## CI / CD (project-specific)
```
# TeamCity (leave blank if using user CI_PROVIDER=github-actions)
TEAMCITY_PROJECT_ID=                      # e.g. OrderService_Build
TEAMCITY_BUILD_TYPE=                      # e.g. OrderService_Build_CI

# Octopus Deploy (leave blank if not using Octopus)
OCTOPUS_PROJECT=                          # e.g. OrderService
OCTOPUS_LIFECYCLE=                        # e.g. Default Lifecycle
```

## Project Management (project-specific)
```
# Jira (leave blank if PM_PROVIDER=none or github-issues)
JIRA_PROJECT_KEY=                         # e.g. ORD or CUST — overrides JIRA_DEFAULT_PROJECT_KEY

# Confluence (leave blank if not using Confluence)
CONFLUENCE_SPACE_KEY=                     # e.g. ORD — overrides CONFLUENCE_DEFAULT_SPACE_KEY

# SDR storage — where /sdr saves Software Decision Records in Confluence
# Defaults to CONFLUENCE_SPACE_KEY + "Software Decision Records" if blank.
SDR_CONFLUENCE_SPACE=                     # Confluence space key for SDRs (defaults to CONFLUENCE_SPACE_KEY)
SDR_PARENT_PAGE=Software Decision Records # Title of the parent page under which SDRs are stored
```

## Database Connections (local dev)
```
SQLSERVER_CONNECTION_STRING=Server=localhost;Database=;Trusted_Connection=True;TrustServerCertificate=True
NEO4J_URI=bolt://localhost:7687
NEO4J_USERNAME=neo4j
```

## Documentation (project-specific)
```
# Obsidian subfolder for this project (overrides OBSIDIAN_PROJECTS_FOLDER)
OBSIDIAN_PROJECT_FOLDER=                  # e.g. Projects/OrderService
```
