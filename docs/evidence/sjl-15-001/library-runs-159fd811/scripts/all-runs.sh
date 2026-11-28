#!/usr/bin/env bash
# all-runs.sh: the runs of parts F and G and of bundle sjl-13-001's part E at stage 8's commit 159fd811 (written
# 2026-11-28), one at a time, each through run.sh (the build lock and the idle gate), one build tree at a time. (Revised 2026-11-28 after the first attempt, whose builds could not start (lib.sh's
# TMPDIR): part F's tools, which ran whole, are not taken again; every other step is, its label ending -2.)
S=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest
N=$S/js-s158-lib; R=$S/s2/ev2/scripts/run.sh; O=$N/logs/all-runs.rc; X=$N/runs; M8=159fd811f74d0f43e7ed7327081267a96f8f1196
mkdir -p $X/part-f $X/part-g $X/rules
step() { local label=$1; shift; echo "$(date -u +%FT%TZ) start $label; main $(git -C /home/user/js-s158-lib ls-remote origin refs/heads/main | cut -c1-40)" >> $O
  (date -u +%FT%TZ; git -C /home/user/js-s158-lib ls-remote origin refs/heads/main) >> $N/logs/main-checks.log
  bash $R $label $N/logs /home/user -- "$@"; echo "$(date -u +%FT%TZ) $label rc=$?" >> $O; }
step lib-f-run-2 bash $N/scripts/ctest.sh $M8 $X/part-f/run ctest-run
step lib-f-control-2 bash $N/scripts/ctest.sh $M8 $X/part-f/control ctest-control $N/patches/control-f-ee0018.patch $X/part-f/run/listing.txt
step lib-g-c-2 bash $N/scripts/partc.sh $M8 $X/part-g/part-c
step lib-g-k1-2 bash $N/scripts/ktest.sh $M8 $X/part-g/part-c/K1 ktest-k1 $N/patches/control-k1.patch SL2 $X/part-g/part-c/run-1.tsv
step lib-g-k2-2 bash $N/scripts/ktest.sh $M8 $X/part-g/part-c/K2 ktest-k2 $N/patches/control-k2.patch SL9 $X/part-g/part-c/run-1.tsv
step lib-g-d-2 bash $N/scripts/partd.sh $M8 $X/part-g/part-d/local
step lib-e-rules-2 bash $N/scripts/rules.sh $M8 $X/rules
echo "$(date -u +%FT%TZ) all done" >> $O
