# Kit Config — User / Device Level
<!--
  This file lives at ~/.claude/kit.config.md
  Run /kit-setup to configure interactively.

  SECRETS — never store tokens/passwords in plain text here.
  Use ${ENV_VAR_NAME} syntax to reference a system environment variable:
    BITBUCKET_API_TOKEN=${BITBUCKET_API_TOKEN}

  Claude resolves ${...} references at runtime from your system env.
  Set secrets once in your OS:
    Windows:  setx BITBUCKET_API_TOKEN "your-token"   (User scope)
    bash:     export BITBUCKET_API_TOKEN="your-token"  (add to ~/.bashrc)

  Non-sensitive values (URLs, usernames, paths) can be plain text.

  Atlassian (Jira + Confluence) — authentication is handled by the Atlassian MCP.
  Run: /mcp authenticate atlassian
  No API token needed here.

  PROJECT-SPECIFIC settings live in .claude/project.config.md per repo.
  Run /project-setup inside a repository to create it.
-->

## Identity
```
USER_NAME=
USER_EMAIL=
```

## Version Control
```
VCS_HOST=github                           # github | bitbucket | gitlab | azure-devops
VCS_BASE_URL=https://github.com
VCS_ORG=                                  # org or username
VCS_DEFAULT_BRANCH=main
VCS_PR_DRAFT_BY_DEFAULT=false

# Bitbucket-specific (leave blank on GitHub machine)
BITBUCKET_WORKSPACE=
BITBUCKET_API_TOKEN=${BITBUCKET_API_TOKEN}   # SECRET — set in system env
```

## CI / CD
```
CI_PROVIDER=github-actions                # github-actions | teamcity | azure-devops | none
CD_PROVIDER=github-actions                # github-actions | octopus | azure-devops | none

# TeamCity (work machine) — used as reference values in generated Kotlin DSL
TEAMCITY_BASE_URL=

# Octopus Deploy (work machine) — used as reference values in generated configs
OCTOPUS_URL=
OCTOPUS_SPACE=Default
```

## Project Management
```
PM_PROVIDER=none                          # jira | github-issues | none

# Jira / Atlassian (work machine)
# Auth is handled by the Atlassian MCP — no API token needed here.
# Run: /mcp authenticate atlassian
JIRA_BASE_URL=                            # used for building ticket URLs in output

# Sprint settings (used by /tech-refinement for capacity guidance)
SPRINT_DURATION_DAYS=14                   # sprint length in days (default: 2 weeks)
```

## SDLC
```
# Location of your company SDLC documentation in Confluence.
# Used by /sdlc-check and /tech-refinement to read process requirements.
# Auth is handled by the Atlassian MCP — no token needed.
SDLC_CONFLUENCE_SPACE=                    # Confluence space key, e.g. ENG or PLATFORM
SDLC_PARENT_PAGE=                         # Title of the parent SDLC page, e.g. "Software Development Lifecycle"
```

## Documentation
```
DOCS_PRIMARY=obsidian                     # obsidian | confluence | both | none

# Obsidian
OBSIDIAN_VAULT_PATH=
OBSIDIAN_DEV_FOLDER=Dev
OBSIDIAN_PROJECTS_FOLDER=Projects

# Confluence (work machine)
# Auth is handled by the Atlassian MCP — see JIRA_BASE_URL above.
CONFLUENCE_BASE_URL=                      # used for building page URLs in output
CONFLUENCE_DEFAULT_SPACE_KEY=

# GraphRAG (optional)
GRAPHRAG_MCP_URL=
```

## Dev Infrastructure (local)
```
SEQ_URL=http://localhost:5341
SEQ_API_KEY=                              # blank = no auth; or ${SEQ_API_KEY}
SQLSERVER_DEFAULT_HOST=localhost
SQLSERVER_TRUST_CERT=true
NEO4J_DEFAULT_URI=bolt://localhost:7687
```

## Scaffolding Defaults
```
DEFAULT_NAMESPACE=
NEW_PROJECT_BASE_PATH=
```

## Communication (optional)
```
SLACK_WORKSPACE=
SLACK_DEFAULT_CHANNEL=
TEAMS_WEBHOOK_URL=
```
