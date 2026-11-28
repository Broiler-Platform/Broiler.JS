#!/usr/bin/env bash
# Hard idle gate for SJL-13 step 2's stage-1 development reference (scratch, not a run bundle sjl-13-002's rule judges). Source it, then call:  gate <label> <logfile>
# Waits, with no fallback and no time limit, until (1) the one-minute load average is 1.0 or less and
# (2) no process other than the caller's own ancestors runs whose executable (/proc/PID/exe, not a
# shell's command line) is dotnet, testhost, VBCSCompiler, MSBuild, ilc, crossgen or a Broiler.* apphost.
# It appends one record per pass (and every sixth wait) to <logfile>: time (UTC), load, and every matching process seen.
# It kills nothing. stop_servers kills only a process whose own environment holds this run's tag
# (S2_RUN_TAG, exported by run.sh before the run starts); every other matching process is logged and left alone.

gate_scan() {
  local self=$$ out="" p exe base args
  local anc=" " q=$self
  while [ -n "$q" ] && [ "$q" != 0 ] && [ "$q" != 1 ]; do anc="$anc$q "; q=$(awk '{print $4}' /proc/$q/stat 2>/dev/null); done
  for p in /proc/[0-9]*; do
    p=${p#/proc/}
    case "$anc" in *" $p "*) continue;; esac
    exe=$(readlink /proc/$p/exe 2>/dev/null) || continue
    base=${exe##*/}
    case "$base" in
      dotnet|testhost|testhost.dll|VBCSCompiler|MSBuild|Broiler.*|ilc|crossgen*)
        args=$(tr '\0' ' ' < /proc/$p/cmdline 2>/dev/null | cut -c1-160)
        out="$out pid=$p exe=$exe args=[$args];";;
    esac
  done
  printf '%s' "$out"
}

gate() {
  local label=$1 log=$2 load procs n=0
  while :; do
    load=$(cut -d' ' -f1 /proc/loadavg)
    procs=$(gate_scan)
    if awk -v l="$load" 'BEGIN{exit !(l<=1.0)}' && [ -z "$procs" ]; then
      printf '%s gate %s PASS load1=%s procs=none checks=%s\n' "$(date -u +%FT%TZ)" "$label" "$load" "$n" >> "$log"
      return 0
    fi
    if [ $((n % 6)) -eq 0 ]; then
      printf '%s gate %s WAIT load1=%s procs=%s\n' "$(date -u +%FT%TZ)" "$label" "$load" "${procs:-none}" >> "$log"
    fi
    n=$((n+1)); sleep 10
  done
}

machine_record() {
  echo "time: $(date -u +%FT%TZ)"; echo "loadavg: $(cat /proc/loadavg)"; echo "nproc: $(nproc)"
  echo "sdk: $(PATH=/root/.dotnet:$PATH DOTNET_ROOT=/root/.dotnet dotnet --version 2>/dev/null)"
  echo "os: $(. /etc/os-release; echo $PRETTY_NAME)"; echo "disk: $(df -m / | awk 'NR==2{print $4}') MiB free"
  echo "procs-matching:$(gate_scan)"
}

stop_servers() {
  local log=$1 tag=$2 p exe args
  [ -n "$tag" ] || { printf '%s stop-servers: no run tag given, nothing killed\n' "$(date -u +%FT%TZ)" >> "$log"; return 0; }
  for p in /proc/[0-9]*; do
    p=${p#/proc/}; exe=$(readlink /proc/$p/exe 2>/dev/null) || continue
    case "${exe##*/}" in dotnet|VBCSCompiler|MSBuild)
      args=$(tr '\0' ' ' < /proc/$p/cmdline 2>/dev/null)
      case "$args" in *VBCSCompiler*|*MSBuild.dll*nodeReuse*|*MSBuild.dll*/nodemode*)
        if tr '\0' '\n' < /proc/$p/environ 2>/dev/null | grep -qxF "S2_RUN_TAG=$tag"; then
          printf '%s stop-servers: killing this run'"'"'s leftover pid=%s tag=%s args=[%s]\n' "$(date -u +%FT%TZ)" "$p" "$tag" "$(echo "$args"|cut -c1-200)" >> "$log"
          kill "$p" 2>/dev/null
        else
          printf '%s stop-servers: NOT this run'"'"'s, left alone pid=%s args=[%s]\n' "$(date -u +%FT%TZ)" "$p" "$(echo "$args"|cut -c1-200)" >> "$log"
        fi;;
      esac;;
    esac
  done
  printf '%s stop-servers: done, remaining:%s\n' "$(date -u +%FT%TZ)" "$(gate_scan)" >> "$log"
}
