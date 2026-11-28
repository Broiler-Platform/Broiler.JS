#!/usr/bin/env bash
# partd.sh <full-sha> <outdir>: part G's half of bundle sjl-13-001's part D (that rule's part D as first written,
# decision D-js-library-readings), its local half on linux-x64, in bundle sjl-13-001's library-commit/commands.txt form
# ("Part D"): from a clean `git archive` under /home/user/js-s158-lib-build/partd/src, the probe published with Native AOT
# (`dotnet publish ... -c Release -r linux-x64`) and with the JIT (`-p:PublishAot=false`), Release, no build server, each
# publication's log kept and searched for a warning line, the AOT folder searched for a managed assembly; each run over
# conformance/vectors with --report; and the probe's JIT build and its run, as the shared workflow's probe job takes it;
# the three reports compared byte for byte. Every command under `env -i` (lib.sh). Written 2026-11-28 from stage 8's
# rehearsal script probe.sh; through run.sh.
set -u
source /tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/js-s158-lib/scripts/lib.sh
SHA=$1; OUT=$2; BLD=$BROOT/partd; SRC=$BLD/src; L=$OUT/logs
mkdir -p $OUT $L; setenv $BLD
# (Revised 2026-11-28, after part D's first step waited on its own tree: the archive leaves out the commit's top-level
# docs/ and logs/, records that no command here reads, as `git archive <commit> -- . ':(exclude)docs' ':(exclude)logs'`.)
newtree $BLD $SHA $L/disk.log . ':(exclude)docs' ':(exclude)logs' || exit 3
{ echo "commit: $(git -C $REPO rev-parse $SHA)"; echo "tree: $(git -C $REPO rev-parse $SHA^{tree})"; echo "sdk: $("${RUNENV[@]}" dotnet --version)"; echo "started: $(date -u +%FT%TZ)"; } > $L/commit.txt
cd $SRC
P=Broiler.ECMAScript/Broiler.ECMAScript.Probe
diskok build $L/disk.log
T $L probe-publish-aot "${RUNENV[@]}" dotnet publish $P -c Release -r linux-x64 --nologo --disable-build-servers
cp $L/probe-publish-aot.stdout.txt $OUT/probe-publish-aot.log
w1=$(grep -c -E "warning [A-Z]+[0-9]+" $OUT/probe-publish-aot.log); w1all=$(grep -c -i "warning" $OUT/probe-publish-aot.log)
if ls $P/bin/Release/net10.0/linux-x64/publish/*.dll > $OUT/probe-publish-aot-dlls.txt 2>/dev/null; then dll="yes"; [ $FIRST -eq 0 ] && FIRST=12; else dll="none"; fi
ls -la $P/bin/Release/net10.0/linux-x64/publish/ > $OUT/probe-publish-aot-folder.txt
diskok run-aot $L/disk.log
T $L probe-aot "${RUNENV[@]}" ./$P/bin/Release/net10.0/linux-x64/publish/Broiler.ECMAScript.Probe conformance/vectors --report $OUT/probe-aot.tsv
cp $L/probe-aot.stdout.txt $OUT/probe-aot.log
diskok build-jit-publish $L/disk.log
T $L probe-publish-jit "${RUNENV[@]}" dotnet publish $P -c Release -p:PublishAot=false --nologo --disable-build-servers
cp $L/probe-publish-jit.stdout.txt $OUT/probe-publish-jit.log
w2=$(grep -c -E "warning [A-Z]+[0-9]+" $OUT/probe-publish-jit.log); w2all=$(grep -c -i "warning" $OUT/probe-publish-jit.log)
diskok run-jit-published $L/disk.log
T $L probe-jit-published "${RUNENV[@]}" dotnet $P/bin/Release/net10.0/publish/Broiler.ECMAScript.Probe.dll conformance/vectors --report $OUT/probe-jit-published.tsv
cp $L/probe-jit-published.stdout.txt $OUT/probe-jit-published.log
diskok build-jit $L/disk.log
T $L probe-build-jit "${RUNENV[@]}" dotnet build $P -c Release --nologo --disable-build-servers
diskok run-jit $L/disk.log
T $L probe-jit "${RUNENV[@]}" dotnet $P/bin/Release/net10.0/Broiler.ECMAScript.Probe.dll conformance/vectors --report $OUT/probe-jit.tsv
cp $L/probe-jit.stdout.txt $OUT/probe-jit.log
{ echo "probe-publish-aot.log: $w1all lines naming a warning; $w1 coded warnings"; echo "probe-publish-jit.log: $w2all lines naming a warning; $w2 coded warnings"; echo "managed assemblies in the Native AOT publish folder: $dll"; } > $OUT/warnings.txt
[ "$w1" = 0 ] && [ "$w2" = 0 ] && [ "$w1all" = 0 ] && [ "$w2all" = 0 ] || { [ $FIRST -eq 0 ] && FIRST=11; }
( cd $OUT && sha256sum probe-aot.tsv probe-jit-published.tsv probe-jit.tsv > sha256.txt )
{ cmp $OUT/probe-aot.tsv $OUT/probe-jit-published.tsv && echo "probe-aot.tsv and probe-jit-published.tsv: the same bytes"; cmp $OUT/probe-aot.tsv $OUT/probe-jit.tsv && echo "probe-aot.tsv and probe-jit.tsv: the same bytes"; } > $OUT/compare.txt 2>&1 || { [ $FIRST -eq 0 ] && FIRST=13; }
echo "ended: $(date -u +%FT%TZ) first-failure=$FIRST" >> $L/commit.txt
deltree $BLD $L/disk.log
exit $FIRST
