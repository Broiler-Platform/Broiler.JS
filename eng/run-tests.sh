#!/usr/bin/env bash
# Run after dotnet build Broiler.JS.slnx using the same configuration:
#   eng/run-tests.sh [Debug|Release] [--engine-only] [dotnet test arguments...]
# Given a configuration alone, or nothing (CONFIGURATION, else Release), it runs every test of the solution. With
# --engine-only, after the configuration or in its place, it runs every project of the solution except those of
# Broiler.ECMAScript.slnf, which the Shared libraries workflow tests: it writes a solution filter of the rest to
# obj/run-tests/ and runs dotnet test on the filter, so no test host starts for a project whose tests are not this run's,
# and a project added to the solution is in the filter with no list to keep. It refuses to run (exit 2) unless the
# projects of Broiler.ECMAScript.slnf are exactly those Broiler.JS.slnx lists under Broiler.ECMAScript/, so a project
# added under that folder but not to that filter fails here instead of being tested by neither workflow. Arguments after
# these go to dotnet test as they are: CI's build job passes a --filter that leaves the engine corpus to its own job.
set -euo pipefail

if [ "${1:-}" = --engine-only ]; then
  configuration="${CONFIGURATION:-Release}"
else
  configuration="${1:-${CONFIGURATION:-Release}}"
  [ "$#" -eq 0 ] || shift
fi
case "$configuration" in
  Debug|Release) ;;
  *) echo "Unknown configuration '$configuration'; use Debug or Release." >&2; exit 2 ;;
esac

target=Broiler.JS.slnx
if [ "${1:-}" = --engine-only ]; then
  shift
  refuse() { echo "run-tests.sh --engine-only: $1" >&2; exit 2; }
  # Every Project element's Path, in the solution's order, whatever the order of its attributes; and how many Project
  # elements the solution has, so that one whose Path is not read here cannot drop out of the filter unnoticed.
  mapfile -t all < <({ grep -o '<Project[[:space:]][^>]*>' Broiler.JS.slnx || true; } | sed -n 's/.*[[:space:]]Path="\([^"]*\)".*/\1/p')
  elements="$({ grep -oE '<Project([[:space:]/>]|$)' Broiler.JS.slnx || true; } | wc -l)"
  elements=$((elements))
  # The projects Shared libraries tests: every .csproj path Broiler.ECMAScript.slnf names, with / for \.
  mapfile -t shared < <({ grep -o '"[^"]*\.csproj"' Broiler.ECMAScript.slnf || true; } | tr -d '"' | sed 's#\\\\#/#g')
  [ "${#all[@]}" -eq "$elements" ] ||
    refuse "read a Path from ${#all[@]} of the $elements Project elements of Broiler.JS.slnx."
  [ "${#shared[@]}" -gt 0 ] || refuse "Broiler.ECMAScript.slnf names no project."
  declare -A in_shared=() in_folder=()
  for p in "${shared[@]}"; do in_shared[$p]=1; done
  projects=()
  for p in "${all[@]}"; do
    case "$p" in
      Broiler.ECMAScript/*)
        in_folder[$p]=1
        [ -n "${in_shared[$p]:-}" ] ||
          refuse "$p is under Broiler.ECMAScript/ in Broiler.JS.slnx but not in Broiler.ECMAScript.slnf, so neither workflow would test it." ;;
    esac
    [ -n "${in_shared[$p]:-}" ] || projects+=("$p")
  done
  for p in "${shared[@]}"; do
    [ -n "${in_folder[$p]:-}" ] ||
      refuse "$p is in Broiler.ECMAScript.slnf but not under Broiler.ECMAScript/ in Broiler.JS.slnx."
  done
  [ "${#projects[@]}" -gt 0 ] && [ $((${#projects[@]} + ${#shared[@]})) -eq "$elements" ] ||
    refuse "${#projects[@]} project(s) left and ${#shared[@]} in Broiler.ECMAScript.slnf do not make the $elements of Broiler.JS.slnx."
  mkdir -p obj/run-tests
  target=obj/run-tests/Broiler.JS.engine.slnf
  trap 'rm -f "$target"' EXIT
  {
    printf '{\n  "solution": {\n    "path": "../../Broiler.JS.slnx",\n    "projects": [\n'
    for i in "${!projects[@]}"; do
      separator=,
      [ "$i" -lt $((${#projects[@]} - 1)) ] || separator=
      printf '      "%s"%s\n' "${projects[$i]}" "$separator"
    done
    printf '    ]\n  }\n}\n'
  } > "$target"
  echo "Testing through $target: the ${#projects[@]} projects of Broiler.JS.slnx outside Broiler.ECMAScript.slnf, not its ${#shared[@]}."
fi

dotnet test "$target" -c "$configuration" --no-build "$@"
