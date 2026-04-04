---
name: optimize-cost
description: >
  Generates or updates a CLAUDE.md file for your repository.
  Prompts you to select kits and creates or updates a CLAUDE.md that forces efficient kit usage.
user-invocable: true
argument-hint: "optimize cost"
allowed-tools: Read, Write, Edit, Glob
---

# Optimize Cost Skill

This skill generates a new CLAUDE.md file or updates an existing one for your repository.

## Process

1. **Check Existing CLAUDE.md**: Look for existing CLAUDE.md file in repository root
2. **Kit Selection**: Present checklist of available kits
3. **CLAUDE.md Generation/Update**: Create new or update existing CLAUDE.md with selected kits
4. **Efficiency Rules**: Include rules that force Claude to use kits and agents efficiently

## Usage

Run this skill to optimize your repository's CLAUDE.md file for efficient kit usage.