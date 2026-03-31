---
name: memory-kit config
description: User-level configuration for the memory-kit plugin
type: user-config
---

# Memory Kit Config

Copy this file to `~/.claude/memory-kit.config.md` and fill in your values.
Run `/memory-setup` to be guided through configuration interactively.

---

## Storage

# Base directory for all project memory stores
# Each project gets its own subdirectory: {MEMORY_BASE_PATH}/{project-id}/memory/
MEMORY_BASE_PATH=~/.claude/projects

## Project ID Strategy

# How to identify the current project for memory scoping:
#   git-remote   — use git remote origin URL slug (recommended for code projects)
#   cwd          — use current working directory name (good for non-git projects)
#   manual       — always prompt for project ID (most explicit)
PROJECT_ID_STRATEGY=git-remote

## Auto-Inject (SessionStart hook)

# Inject MEMORY.md summary into Claude's context at session start
# Set to false to disable automatic memory loading (use /memory-recall manually instead)
AUTO_INJECT=true

# Maximum number of memory entries to show at session start (0 = all)
INJECT_MAX_ENTRIES=8

## Auto-Capture (Stop hook)

# Enable detection of correction/discovery patterns after each response
# When detected, writes a flag that surfaces an advisory on the next prompt
AUTO_CAPTURE_ENABLED=true

## MCP Integration

# Set to false to disable memory-mcp and use grep-based fallback only
# Useful if npx is unavailable or you want to skip the MCP server
MCP_ENABLED=true
