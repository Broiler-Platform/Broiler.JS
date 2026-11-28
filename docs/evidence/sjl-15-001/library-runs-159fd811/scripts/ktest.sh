#!/usr/bin/env bash
# ktest.sh <full-sha> <outdir> <label> <patch-file> <SL2|SL9> <part C's run-1.tsv>: part C's control K1 or K2 (bundle
# sjl-13-001's rule, part C; K2 as its first revision restates it), in bundle sjl-13-001's library-commit/commands.txt
# form: a second clean `git archive` of the commit under /home/user/js-s158-lib-build/<label>/src with the control's patch
# applied (a scratch copy, never merged); the probe and the architecture tests built there in Release with no build
# server; test262 checked by SHA-256 and extracted to /dev/shm; run 1 (manifest order) and run 2 (--reverse) of the corpus
# digest mode; the architecture tests filtered to the named rule; then the patched file restored from the commit,
# Broiler.ECMAScript/ compared with a fresh archive's, the probe built again and run 1 taken again (revert-run-1.tsv),
# compared with part C's run 1 byte for byte (revert-check.txt). Every command under `env -i` (lib.sh). Written
# 2026-11-28 from stage 8's rehearsal script ktest.sh; through run.sh.
set -u
source /tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/js-s158-lib/scripts/lib.sh
SHA=$1; OUT=$2; LABEL=$3; PATCHFILE=$4; RULE=$5; RUN1=$6
BLD=$BROOT/$LABEL; SRC=$BLD/src; L=$OUT/logs
mkdir -p $OUT $L; setenv $BLD
newtree $BLD $SHA $L/disk.log || exit 3
{ echo "commit: $(git -C $REPO rev-parse $SHA)"; echo "patch: $PATCHFILE"; echo "rule: $RULE"; echo "sdk: $("${RUNENV[@]}" dotnet --version)"; echo "started: $(date -u +%FT%TZ)"; } > $L/commit.txt
cd $SRC
cp $PATCHFILE $OUT/control.patch
patch -p1 --no-backup-if-mismatch < $PATCHFILE > $L/patch.txt 2>&1 || { echo "patch failed" >> $L/commit.txt; deltree $BLD $L/disk.log; exit 9; }
P=Broiler.ECMAScript/Broiler.ECMAScript.Probe; A=Broiler.ECMAScript/Broiler.ECMAScript.Architecture.Tests
DLL=$P/bin/Release/net10.0/Broiler.ECMAScript.Probe.dll
rc=0
diskok build $L/disk.log
T $L probe-build "${RUNENV[@]}" dotnet build $P -c Release --nologo --disable-build-servers || rc=1
T $L arch-build "${RUNENV[@]}" dotnet build $A -c Release --nologo --disable-build-servers || rc=1
{ cat $L/probe-build.stdout.txt $L/arch-build.stdout.txt | grep -E "Build succeeded|error|warning [A-Z]+[0-9]+|Warning\(s\)|Error\(s\)"; } > $OUT/build-summary.txt
suite $L/suite.log || { deltree $BLD $L/disk.log; rm -rf $SUITEDIR; exit 7; }
diskok run-1 $L/disk.log
T $L run-1 "${RUNENV[@]}" dotnet $DLL --digest-corpus conformance/parse --out $OUT/run-1.tsv --test262 $SUITE; echo "run-1 rc=$?" >> $L/commit.txt
cp $L/run-1.stdout.txt $OUT/run-1.log; cat $L/run-1.stderr.txt >> $OUT/run-1.log
diskok run-2 $L/disk.log
T $L run-2 "${RUNENV[@]}" dotnet $DLL --digest-corpus conformance/parse --out $OUT/run-2.tsv --test262 $SUITE --reverse; echo "run-2 rc=$?" >> $L/commit.txt
cp $L/run-2.stdout.txt $OUT/run-2.log; cat $L/run-2.stderr.txt >> $OUT/run-2.log
lc=$(echo $RULE | tr A-Z a-z)
diskok rule $L/disk.log
T $L $lc "${RUNENV[@]}" dotnet test $A -c Release --no-build --nologo --filter "FullyQualifiedName~$RULE" --logger "console;verbosity=normal"; echo "rule $RULE rc=$?" >> $L/commit.txt
cp $L/$lc.stdout.txt $OUT/$lc.log
{ if cmp $OUT/run-1.tsv $OUT/run-2.tsv; then echo "run 1 and run 2: the same bytes"; else echo "run 1 and run 2 differ"; echo "lines of run 1 not in run 2 (diff '<' lines): $(diff $OUT/run-1.tsv $OUT/run-2.tsv | grep -c '^<')"; fi; } > $OUT/run-1-against-run-2.txt 2>&1
for f in $(grep '^+++ ' $PATCHFILE | awk '{print $2}' | sed 's|^b/||'); do git -C $REPO show $SHA:$f > $f; echo "restored $f from $SHA" >> $OUT/revert-check.txt; done
mkdir -p $BLD/fresh; git -C $REPO archive --format=tar $SHA Broiler.ECMAScript | tar -xf - -C $BLD/fresh
diff -r -q -x bin -x obj -x TestResults Broiler.ECMAScript $BLD/fresh/Broiler.ECMAScript >> $OUT/revert-check.txt 2>&1 && echo "Broiler.ECMAScript/ equals a fresh archive's (bin, obj, TestResults aside)" >> $OUT/revert-check.txt
rm -rf $BLD/fresh
diskok revert-build $L/disk.log
T $L revert-build "${RUNENV[@]}" dotnet build $P -c Release --nologo --disable-build-servers || rc=2
diskok revert-run-1 $L/disk.log
T $L revert-run-1 "${RUNENV[@]}" dotnet $DLL --digest-corpus conformance/parse --out $OUT/revert-run-1.tsv --test262 $SUITE; echo "revert-run-1 rc=$?" >> $L/commit.txt
cp $L/revert-run-1.stdout.txt $OUT/revert-run-1.log; cat $L/revert-run-1.stderr.txt >> $OUT/revert-run-1.log
if cmp $RUN1 $OUT/revert-run-1.tsv >> $OUT/revert-check.txt 2>&1; then echo "revert-run-1.tsv equals part C's run-1.tsv byte for byte" >> $OUT/revert-check.txt; else echo "revert-run-1.tsv DIFFERS from part C's run-1.tsv" >> $OUT/revert-check.txt; rc=5; fi
( cd $OUT && sha256sum run-1.tsv run-2.tsv revert-run-1.tsv > sha256.txt )
echo "ended: $(date -u +%FT%TZ) rc=$rc" >> $L/commit.txt
rm -rf $SUITEDIR
deltree $BLD $L/disk.log
exit $rc
