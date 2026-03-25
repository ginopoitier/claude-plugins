# Agents — Bitbucket Kit

Specialist agents available in bitbucket-kit. Invoke via the `Agent` tool with `subagent_type: "bitbucket-kit:{agent-name}"`.

## Agent Roster

| Agent | Model | Domain | When to Use |
|-------|-------|--------|-------------|
| `bb-reviewer` | Opus | Deep PR review via Bitbucket API — fetches diff, posts inline + summary comments | "Review PR {id}", "code review PR", "leave comments on PR" |
| `bb-historian` | Sonnet | PR history analytics — cycle time, author breakdown, hotspot files, open backlog | "PR stats", "PR throughput", "who reviews most PRs", "PR trends" |

## Routing Table

| User Intent | Agent |
|-------------|-------|
| "Review PR 42 and post comments" | `bb-reviewer` |
| "Code review PR on my current branch" | `bb-reviewer` |
| "Leave comments on PR 42" | `bb-reviewer` |
| "Approve PR 42" | `bb-reviewer` |
| "PR stats for the last 30 days" | `bb-historian` |
| "Who reviews the most PRs?" | `bb-historian` |
| "How long do PRs take to merge?" | `bb-historian` |
| "Show open PR backlog" | `bb-historian` |
| "Which files change most in PRs?" | `bb-historian` |

## Why Agents vs Skills

- **Agents** run in a separate context window — large PR diffs and multi-page API responses don't consume main context tokens
- **Skills** (like `/review`) load into the main context — better for quick reviews where you want the findings woven into the conversation
- Use an agent when the PR diff is large (> 300 lines) or when posting comments back to Bitbucket programmatically

## Token Budget Guidance

| Task | Approach |
|------|----------|
| Quick diff check | `/review` skill in main context |
| Full PR review + post comments to Bitbucket | `bb-reviewer` agent (Opus, separate context) |
| PR analytics / history | `bb-historian` agent (Sonnet, all API calls via shell) |
| Create or describe a PR | `/pr` skill in main context |
| Tag a release | `/release` skill in main context |
