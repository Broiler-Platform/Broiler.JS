#!/usr/bin/env bash
# partc.sh <full-sha> <outdir>: part G's half of bundle sjl-13-001's part C at the commit (that rule's part C, its control
# K2 as its first revision restates it; decision D-js-library-readings): from a clean `git archive` under
# /home/user/js-s158-lib-build/partc/src, the probe and the architecture tests built in Release with no build server;
# test262 checked by SHA-256 and extracted to /dev/shm; the probe's corpus digest mode (part C's dump mode, decision
# D-js-library-readings; with the model, decision D-js-digest-model) in three processes, run 1 in the manifest's order,
# run 2 with --reverse and run 3 with --two-threads (bundle sjl-13-001's library-commit/commands.txt, "Part C"); and the
# architecture tests filtered to SL2 and SL9 (the same file's form). Every command under `env -i` (lib.sh). Written
# 2026-11-28 from stage 8's rehearsal scripts digest.sh and sl29.sh; through run.sh.
set -u
source /tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/js-s158-lib/scripts/lib.sh
SHA=$1; OUT=$2; BLD=$BROOT/partc; SRC=$BLD/src; L=$OUT/logs
mkdir -p $OUT $L; setenv $BLD
newtree $BLD $SHA $L/disk.log || exit 3
{ echo "commit: $(git -C $REPO rev-parse $SHA)"; echo "tree: $(git -C $REPO rev-parse $SHA^{tree})"; echo "sdk: $("${RUNENV[@]}" dotnet --version)"; echo "started: $(date -u +%FT%TZ)"; } > $L/commit.txt
cd $SRC
P=Broiler.ECMAScript/Broiler.ECMAScript.Probe; A=Broiler.ECMAScript/Broiler.ECMAScript.Architecture.Tests
DLL=$P/bin/Release/net10.0/Broiler.ECMAScript.Probe.dll
diskok build $L/disk.log
T $L probe-build "${RUNENV[@]}" dotnet build $P -c Release --nologo --disable-build-servers
T $L arch-build "${RUNENV[@]}" dotnet build $A -c Release --nologo --disable-build-servers
suite $L/suite.log || { echo "suite failed" >> $L/commit.txt; deltree $BLD $L/disk.log; rm -rf $SUITEDIR; exit 7; }
n=0
for m in 1 2 3; do
  case $m in 1) extra="";; 2) extra="--reverse";; 3) extra="--two-threads";; esac
  diskok run-$m $L/disk.log
  T $L run-$m "${RUNENV[@]}" dotnet $DLL --digest-corpus conformance/parse --out $OUT/run-$m.tsv --test262 $SUITE $extra
  cp $L/run-$m.stdout.txt $OUT/run-$m.log; cat $L/run-$m.stderr.txt >> $OUT/run-$m.log
done
diskok sl2-sl9 $L/disk.log
T $L sl2-sl9 "${RUNENV[@]}" dotnet test $A -c Release --no-build --nologo --filter "FullyQualifiedName~SL2|FullyQualifiedName~SL9" --logger "console;verbosity=normal"
cp $L/sl2-sl9.stdout.txt $OUT/sl2-sl9.log
( cd $OUT && sha256sum run-1.tsv run-2.tsv run-3.tsv > sha256.txt )
{ cmp $OUT/run-1.tsv $OUT/run-2.tsv && echo "run-1.tsv and run-2.tsv: the same bytes"; cmp $OUT/run-1.tsv $OUT/run-3.tsv && echo "run-1.tsv and run-3.tsv: the same bytes"; } > $OUT/compare.txt 2>&1
echo "ended: $(date -u +%FT%TZ) first-failure=$FIRST" >> $L/commit.txt
rm -rf $SUITEDIR
deltree $BLD $L/disk.log
exit $FIRST
