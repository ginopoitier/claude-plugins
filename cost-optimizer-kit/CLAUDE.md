# Cost Optimizer Kit

This kit generates or updates a CLAUDE.md file in your repository that makes Claude use kits more effectively and cost-efficiently.

## What it does

When you run the optimize-cost skill, it will:
1. Check if a CLAUDE.md file already exists in your repository
2. Ask you to select which kits you want to use
3. Generate a new CLAUDE.md file or add cost optimization rules to the existing one
4. The CLAUDE.md file contains rules that force Claude to use agents and kits efficiently

## Usage

Run `/optimize-cost` to generate or update the CLAUDE.md file for your project.

## Kit Selection Process

Before starting work, Claude will present a checklist of available kits and ask you to select only those relevant to your current project. This ensures minimal context loading and maximum efficiency.