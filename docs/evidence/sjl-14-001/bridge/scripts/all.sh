#!/usr/bin/env bash
# All of part B's local runs and the side-by-side run at W1, one commit per hold of /dev/shm/s2-gate.lock behind the idle gate (run.sh).
SCR=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/js-s14b
RUNSH=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/s2/ev2/scripts/run.sh
LOG=$SCR/runs/gate
mkdir -p $LOG
while read sha modes; do
  bash $RUNSH s14b-${sha:0:8} $LOG /tmp -- bash $SCR/scripts/commit.sh $sha $modes
  echo "$(date -u +%FT%TZ) $sha $modes rc=$?" >> $SCR/runs/all.log
done <<'LIST'
e2ed7fae6384b484b3b9773fd89dd33e40c01884 pc
8e5519d8cf81808856e32f2275fcb4d3b98a73f3 pc,sbs
6f7284daa2d2fbec3104a9498d5f7c989f956946 pc,son
29061d65f0ca41c5bd93074083f2018ccedd5c57 pc,son
35da77eca574f5cbd1163c5cd26bd19c3b69e8d5 pc,son
LIST
