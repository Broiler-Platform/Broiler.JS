#!/usr/bin/env bash
# Run after dotnet build Broiler.JS.slnx using the same configuration. Given a configuration alone, or
# nothing (Release), it runs every test of the solution. Arguments after the configuration go to
# dotnet test as they are: CI's build job passes a --filter that leaves the engine corpus to its own job.
set -euo pipefail

configuration="${1:-${CONFIGURATION:-Release}}"
case "$configuration" in
  Debug|Release) ;;
  *) echo "Unknown configuration '$configuration'; use Debug or Release." >&2; exit 2 ;;
esac
[ "$#" -eq 0 ] || shift

dotnet test Broiler.JS.slnx -c "$configuration" --no-build "$@"
