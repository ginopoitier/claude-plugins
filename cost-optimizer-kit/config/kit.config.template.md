---
name: cost-optimizer-kit config
description: User-level configuration for the cost-optimizer-kit plugin
type: user-config
---

# Cost Optimizer Kit Config

Copy this file to `~/.claude/cost-optimizer-kit.config.md` and fill in your values.
Run `/optimize-cost` to be guided through configuration interactively.

---

## Budget Tracking

# Monthly token budget alert threshold (USD). Set to 0 to disable alerts.
MONTHLY_BUDGET_USD=0

# Path to usage log file (blank = no logging)
USAGE_LOG_PATH=

## Model Defaults

# Default model for code generation (sonnet | opus | haiku)
DEFAULT_CODE_MODEL=claude-sonnet-4-5

# Default model for simple lookup tasks (haiku | sonnet)
DEFAULT_LOOKUP_MODEL=claude-haiku-4-5

# Default model for architecture/security reviews (opus | sonnet)
DEFAULT_REVIEW_MODEL=claude-opus-4-5

## ReasoningBank

# Enable ReasoningBank pattern storage and retrieval
REASONING_BANK_ENABLED=true

# Base path for storing reasoning patterns (blank = ~/.claude/reasoning-bank)
REASONING_BANK_PATH=

# Minimum confidence threshold for pattern retrieval (0.0-1.0)
REASONING_BANK_MIN_CONFIDENCE=0.75

## Reporting

# Include token cost estimates in audit reports
SHOW_COST_ESTIMATES=true

# Currency for cost display (USD | EUR | GBP)
CURRENCY=USD
