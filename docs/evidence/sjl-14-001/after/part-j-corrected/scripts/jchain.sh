#!/usr/bin/env bash
# jchain.sh <step>...: part J's corrected session and its control, one after the other, each through run.sh (lock and
# idle gate), the default branch's head read before each and logged (recorded, not a stop).
R=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/js-w2read2
RUN=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/s2/ev2/scripts/run.sh
export TOOL=${TOOL:?the tool commit}
mkdir -p $R/runs/logs $R/runs/done
for s in "$@"; do
  [ -f $R/runs/done/$s ] && { echo "$(date -u +%FT%TZ) skip $s" >> $R/runs/chain.log; continue; }
  echo "$(date -u +%FT%TZ) main=$(git -C /home/user/js-w2read ls-remote origin refs/heads/main 2>/dev/null | cut -f1) before $s (tool commit $TOOL)" >> $R/runs/chain.log
  bash $RUN w2j-$s $R/runs/logs /tmp -- bash $R/scripts/jstep.sh $s; rc=$?
  echo "$(date -u +%FT%TZ) rc=$rc" > $R/runs/done/$s
  echo "$(date -u +%FT%TZ) $s rc=$rc" >> $R/runs/chain.log
done
echo "$(date -u +%FT%TZ) chain done" >> $R/runs/chain.log
