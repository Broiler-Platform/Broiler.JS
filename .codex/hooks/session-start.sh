#!/bin/bash
# SessionStart hook for Broiler.JS
# Ensures a .NET 10 SDK is available (the solution targets net10.0) and that
# the git submodules are initialized so the engine can be built.
set -euo pipefail

# Only run inside Claude Code on the web / remote execution environments.
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

log() { echo "[session-start] $*" >&2; }

# Pick a privilege escalation command if we are not already root.
SUDO=""
if [ "$(id -u)" -ne 0 ]; then
  if command -v sudo >/dev/null 2>&1; then
    SUDO="sudo"
  fi
fi

have_dotnet10() {
  command -v dotnet >/dev/null 2>&1 && dotnet --list-sdks 2>/dev/null | grep -q '^10\.'
}

# ── 1. Ensure a .NET 10 SDK is present ──────────────────────────────────
if have_dotnet10; then
  log ".NET 10 SDK already present: $(dotnet --version)"
else
  log "Installing .NET 10 SDK via apt (dotnet-sdk-10.0)..."
  export DEBIAN_FRONTEND=noninteractive
  # Refresh package lists; tolerate failures from unrelated third-party PPAs
  # that may be blocked by the environment's network allowlist.
  $SUDO apt-get update >/dev/null 2>&1 || log "apt-get update reported errors (continuing)"
  $SUDO apt-get install -y dotnet-sdk-10.0

  if have_dotnet10; then
    log ".NET 10 SDK installed: $(dotnet --version)"
  else
    log "ERROR: failed to provide a .NET 10 SDK" >&2
    exit 1
  fi
fi

# Make the .NET CLI non-telemetry / first-run friendly for the session.
echo 'export DOTNET_CLI_TELEMETRY_OPTOUT=1' >> "${CLAUDE_ENV_FILE:-/dev/null}"
echo 'export DOTNET_NOLOGO=1' >> "${CLAUDE_ENV_FILE:-/dev/null}"

# ── 2. Ensure git submodules are initialized ────────────────────────────
# Broiler.Unicode and Broiler.DateTime are required to build the engine.
if [ -f "${CLAUDE_PROJECT_DIR:-.}/.gitmodules" ]; then
  log "Initializing git submodules..."
  git -C "${CLAUDE_PROJECT_DIR:-.}" submodule update --init --recursive || \
    log "WARNING: submodule update failed (continuing)"
fi

log "Done."
