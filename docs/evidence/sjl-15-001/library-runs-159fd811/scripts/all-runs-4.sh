#!/usr/bin/env bash
# all-runs-4.sh: bundle sjl-13-001's part E taken again at 159fd811 (written 2026-11-28), after its first run stopped in
# pack-shared.sh's packing loop (a fault of the bash rendering, not of the commit), through run.sh, from an archive that
# leaves out the top-level docs/ and logs/.
S=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest
N=$S/js-s158-lib; R=$S/s2/ev2/scripts/run.sh; O=$N/logs/all-runs.rc; X=$N/runs; M8=159fd811f74d0f43e7ed7327081267a96f8f1196
step() { local label=$1; shift; echo "$(date -u +%FT%TZ) start $label; main $(git -C /home/user/js-s158-lib ls-remote origin refs/heads/main | cut -c1-40)" >> $O
  (date -u +%FT%TZ; git -C /home/user/js-s158-lib ls-remote origin refs/heads/main) >> $N/logs/main-checks.log
  bash $R $label $N/logs /home/user -- "$@"; echo "$(date -u +%FT%TZ) $label rc=$?" >> $O; }
step lib-e-rules-4 bash $N/scripts/rules.sh $M8 $X/rules
echo "$(date -u +%FT%TZ) all done (4)" >> $O
