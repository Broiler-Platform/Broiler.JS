#!/usr/bin/env bash
# drive.sh <full-sha> <group> <step[:arg]>...: each step through run.sh (the flock'd lock and the hard idle gate), one hold
# of the lock per step, so other stages' runs interleave. Before each step the default branch's head is read by
# git ls-remote and must be 69a8a13b (the base commit); if it moved, the driver stops. A step already done (its marker
# in <group>/done/) is skipped, so the driver resumes after a restart. Stops at the first failing step.
set -u
SHA=$1; GRP=$2; shift 2
SP=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad
SCR=$SP/sjl-rest/js-w2base; OUT=$SCR/$GRP; RUNSH=$SP/sjl-rest/s2/ev2/scripts/run.sh
BASE=69a8a13bc446a1714d3c8be9be3cfd742aebee6e
mkdir -p $OUT/done $OUT/logs
log() { echo "$(date -u +%FT%TZ) $*" >> $OUT/driver.log; }
mainref() { local d r; for d in 0 2 4 8 16; do sleep $d; r=$(git -C /home/user/broiler.js ls-remote origin refs/heads/main 2>/dev/null | cut -f1) && [ -n "$r" ] && { echo $r; return 0; }; done; echo unreadable; }
for s in "$@"; do
  step=${s%%:*}; arg=; [ "$s" != "$step" ] && arg=${s#*:}
  mark=$OUT/done/b-$step${arg:+-$arg}
  [ -f $mark ] && { log "skip $step $arg (done)"; continue; }
  free=$(df -B1 / | awk 'NR==2{print $4}')
  while [ "$free" -lt 8589934592 ]; do log "wait: $free bytes free, under 8 GiB"; sleep 60; free=$(df -B1 / | awk 'NR==2{print $4}'); done
  m=$(mainref); log "main=$m before $step $arg"
  [ "$m" = "$BASE" ] || { log "STOP: main moved to $m"; echo "main moved" > $OUT/STOPPED; exit 9; }
  bash $RUNSH w2b-$GRP-b-$step${arg:+-$arg} $OUT/logs /tmp -- bash $SCR/scripts/bstep.sh $SHA $GRP $step $arg
  rc=$?; log "$step $arg rc=$rc"
  [ $rc = 0 ] || { echo "$step $arg rc=$rc" > $OUT/FAILED; exit $rc; }
  date -u +%FT%TZ > $mark
done
log "all given steps done"
