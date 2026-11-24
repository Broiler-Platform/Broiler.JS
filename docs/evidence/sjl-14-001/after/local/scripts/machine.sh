#!/usr/bin/env bash
# machine.sh: one machine record for SJL-13 step 2's scratch runs: time, load, the SDK `dotnet --info` reports, and
# every process whose executable is dotnet, VBCSCompiler, MSBuild or testhost (a compiler server or build node runs
# under the dotnet executable, its command line naming VBCSCompiler or MSBuild). Reads only; kills nothing.
export PATH=/root/.dotnet:$PATH DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
echo "time: $(date -u +%FT%TZ)"
echo "loadavg: $(cat /proc/loadavg)"
echo "nproc: $(nproc)"
echo "dotnet --info, .NET SDK:"
dotnet --info 2>/dev/null | awk '/^\.NET SDK:?/{f=1;print "  "$0;next} f&&/^[^ ]/{f=0} f{print "  "$0}'
echo "processes (dotnet, VBCSCompiler, MSBuild, testhost):"
n=0
for p in /proc/[0-9]*; do
  p=${p#/proc/}; exe=$(readlink /proc/$p/exe 2>/dev/null) || continue
  args=$(tr '\0' ' ' < /proc/$p/cmdline 2>/dev/null | cut -c1-200)
  case "${exe##*/}" in
    dotnet|testhost*|VBCSCompiler|MSBuild*) echo "  pid=$p exe=$exe args=[$args]"; n=$((n+1));;
  esac
done
[ "$n" -eq 0 ] && echo "  none"
exit 0
