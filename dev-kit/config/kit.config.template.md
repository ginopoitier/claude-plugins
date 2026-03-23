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
