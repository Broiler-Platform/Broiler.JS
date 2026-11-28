#!/usr/bin/env bash
# all-runs-3.sh: the last two steps of the runs at stage 8's commit 159fd811 (written 2026-11-28), after part D's step of
# all-runs.sh was stopped while its disk check waited on its own tree: part G's local half of part D and bundle
# sjl-13-001's part E, each through run.sh, from an archive that leaves out the top-level docs/ and logs/.
S=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest
N=$S/js-s158-lib; R=$S/s2/ev2/scripts/run.sh; O=$N/logs/all-runs.rc; X=$N/runs; M8=159fd811f74d0f43e7ed7327081267a96f8f1196
step() { local label=$1; shift; echo "$(date -u +%FT%TZ) start $label; main $(git -C /home/user/js-s158-lib ls-remote origin refs/heads/main | cut -c1-40)" >> $O
  (date -u +%FT%TZ; git -C /home/user/js-s158-lib ls-remote origin refs/heads/main) >> $N/logs/main-checks.log
  bash $R $label $N/logs /home/user -- "$@"; echo "$(date -u +%FT%TZ) $label rc=$?" >> $O; }
step lib-g-d-3 bash $N/scripts/partd.sh $M8 $X/part-g/part-d/local
step lib-e-rules-3 bash $N/scripts/rules.sh $M8 $X/rules
echo "$(date -u +%FT%TZ) all done (3)" >> $O
