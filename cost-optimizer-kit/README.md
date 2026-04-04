# Cost Optimizer Kit

A Claude Code plugin that generates or updates a CLAUDE.md file in your repository to make Claude use kits more effectively.

## Features

- **CLAUDE.md Generation/Update**: Creates a new CLAUDE.md file or adds cost optimization rules to existing ones
- **Kit Selection**: Interactive checklist to select only needed kits
- **Efficiency Rules**: Includes rules that force Claude to use agents and kits efficiently

## Installation

```bash
/claude plugin install cost-optimizer-kit
```

## Usage

1. Install the kit
2. Run `/optimize-cost` to generate or update the CLAUDE.md file
3. Claude will follow the CLAUDE.md guidelines for efficient kit usage

## What it does

The kit generates a new CLAUDE.md file or adds a cost optimization section to existing CLAUDE.md files that:
- Lists the selected kits for your project
- Contains rules for efficient Claude behavior
- Forces Claude to use agents and kits effectively
- Minimizes token usage through structured guidelines