#!/usr/bin/env bash
# check-settings.sh — Inject ui-ux-kit runtime status into Claude's context.
# Runs on every UserPromptSubmit. Output is read by Claude before processing the prompt.
# Always exits 0 — advisory only.

KIT_CONFIG="${HOME}/.claude/kit.config.md"
CLAUDE_SETTINGS="${HOME}/.claude/settings.json"
UX_UI_MCP_BIN="G:/Claude/Kits/MCP/ux-ui-mcp/dist/index.js"

# ── Config check ────────────────────────────────────────────────────────────
if [[ ! -f "$KIT_CONFIG" ]]; then
  echo "NOTICE: ~/.claude/kit.config.md is missing. Run /ui-ux-setup to configure the ui-ux-kit."
  exit 0
fi

# ── Figma connector detection ────────────────────────────────────────────────
# Claude connectors enabled in claude.ai appear in settings.json.
# We grep for "Figma" as a signal — works regardless of exact key shape.
FIGMA_STATUS="unavailable"
if [[ -f "$CLAUDE_SETTINGS" ]] && grep -qi '"Figma"' "$CLAUDE_SETTINGS" 2>/dev/null; then
  FIGMA_STATUS="available"
fi

# ── ux-ui-mcp local binary check ─────────────────────────────────────────────
MCP_STATUS="unavailable"
if [[ -f "$UX_UI_MCP_BIN" ]]; then
  MCP_STATUS="available"
fi

# ── Inject status into Claude's context ──────────────────────────────────────
# Skills read these values instead of probing tool availability at runtime.
echo "UI_UX_KIT: FIGMA_CONNECTOR=${FIGMA_STATUS} | UX_UI_MCP=${MCP_STATUS}"

exit 0
