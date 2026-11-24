#!/usr/bin/env bash
# Copied 2026-11-15 from SCRATCH/sjl-rest/s2/ev1/scripts/run.sh for S11 (task 7.3 of bundle sjl-13-002): only S changed, to the ev2 folder; the lock /dev/shm/s2-gate.lock, the tag S2_RUN_TAG and the gate are unchanged.
# run.sh <label> <logdir> <workdir> -- <command...>
# Holds /dev/shm/s2-gate.lock (flock, exclusive) from before the gate until it exits.
# Gate, machine.sh record and machine record before, machine record after, timed run, exit code, then this run's own leftover servers stopped (by tag).
# Output: <logdir>/<label>.{out,err,exit,machine,machine-sh,time}.txt, <logdir>/gate.log, <logdir>/commands.log, <logdir>/runs.tsv
S=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/s2/ev2
source $S/scripts/gate.sh
label=$1; logdir=$2; wd=$3; shift 4
mkdir -p "$logdir"
export PATH=/root/.dotnet:$PATH DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 UseSharedCompilation=false
export S2_RUN_TAG="s2-$label-$(date -u +%s)-$$"
# One run at a time (stage 2 fixer, 2026-11-14): an exclusive lock is held from before the gate check until this script
# exits, so two callers can no longer both pass the gate in the same second. The lock fd is closed for every child.
exec 9>/dev/shm/s2-gate.lock
printf '%s lock %s WAIT\n' "$(date -u +%FT%TZ)" "$label" >> "$logdir/gate.log"
flock 9
printf '%s lock %s HELD\n' "$(date -u +%FT%TZ)" "$label" >> "$logdir/gate.log"
gate "$label" "$logdir/gate.log"
$S/scripts/machine.sh > "$logdir/$label.machine-sh.txt" 2>&1 9>&-
{ echo "=== $label before"; machine_record; echo "kernel: $(uname -srm)"; echo "processes:"; ps -eo pid=,etime=,comm=,args= | cut -c1-200 | sed "s/^/  /"; } > "$logdir/$label.machine.txt"
printf '%s %s: cd %s && %s\n' "$(date -u +%FT%TZ)" "$label" "$wd" "$*" >> "$logdir/commands.log"
start=$(date -u +%FT%TZ)
( cd "$wd" && python3 -I $S/scripts/timed.py "$logdir/$label.time.txt" "$@" ) > "$logdir/$label.out.txt" 2> "$logdir/$label.err.txt" 9>&-
rc=$?
echo "$rc" > "$logdir/$label.exit.txt"
{ echo "=== $label after (rc=$rc started=$start)"; machine_record; } >> "$logdir/$label.machine.txt"
printf '%s\trc=%s\t%s\tstarted=%s\tended=%s\n' "$label" "$rc" "$(cat $logdir/$label.time.txt 2>/dev/null | tail -1)" "$start" "$(date -u +%FT%TZ)" >> "$logdir/runs.tsv"
stop_servers "$logdir/gate.log" "$S2_RUN_TAG"
exit $rc
