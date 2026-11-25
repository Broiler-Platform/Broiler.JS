#!/usr/bin/env bash
# Run after dotnet build Broiler.JS.slnx using the same configuration:
#   eng/run-tests.sh [Debug|Release] [--engine-only] [dotnet test arguments...]
# Given a configuration alone, or nothing (Release), it runs every test of the solution. With --engine-only, right after
# the configuration, it runs every test project of the solution except those under Broiler.ECMAScript/, which the Shared
# libraries workflow runs: it writes a solution filter of every project Broiler.JS.slnx lists outside that folder to
# obj/run-tests/ and runs dotnet test on the filter, so no test host starts for a project whose tests are not this run's,
# and a project added to the solution is in the filter with no list to keep. Arguments after these go to dotnet test as
# they are: CI's build job passes a --filter that leaves the engine corpus to its own job.
set -euo pipefail

configuration="${1:-${CONFIGURATION:-Release}}"
case "$configuration" in
  Debug|Release) ;;
  *) echo "Unknown configuration '$configuration'; use Debug or Release." >&2; exit 2 ;;
esac
[ "$#" -eq 0 ] || shift

target=Broiler.JS.slnx
if [ "${1:-}" = --engine-only ]; then
  shift
  # Every <Project Path="..."> of the solution, in its order, but those under Broiler.ECMAScript/.
  mapfile -t projects < <(sed -n 's/.*<Project Path="\([^"]*\)".*/\1/p' Broiler.JS.slnx | grep -v '^Broiler\.ECMAScript/' || true)
  shared="$(grep -c '<Project Path="Broiler\.ECMAScript/' Broiler.JS.slnx || true)"
  if [ "${#projects[@]}" -eq 0 ] || [ "$shared" -eq 0 ]; then
    echo "Broiler.JS.slnx gave ${#projects[@]} project(s) outside Broiler.ECMAScript/ and $shared inside it; expected both." >&2
    exit 2
  fi
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
  echo "Testing through $target: the ${#projects[@]} projects of Broiler.JS.slnx outside Broiler.ECMAScript/, not the $shared under it."
fi

dotnet test "$target" -c "$configuration" --no-build "$@"
