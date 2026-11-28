#!/usr/bin/env bash
# rules.sh <full-sha> <outdir>: bundle sjl-13-001's part E as bundle sjl-15-001's rule applies it ("to the model at the
# library commit"), taken as checks (decision D-js-library-readings): from a clean `git archive` under
# /home/user/js-s158-lib-build/rules/src, in bundle sjl-13-001's library-commit/commands.txt form ("Part E"): the
# architecture tests (rules SL1 to SL10 with their witnesses, SL6 against PublicAPI.txt) built and tested in Release with
# no build server; eng/pack.ps1 -ProjectSet Shared rendered in bash (pack-shared.sh, no pwsh on the host) into an empty
# folder; the pristine consumer of the shared set; and the assurance check as the shared workflow's assurance job now
# runs it (.github/workflows/shared-libraries.yml: `dotnet tool restore --configfile NuGet.config`, then
# `dotnet tool run hairs -- assurance check --root Broiler.ECMAScript`). Every command under `env -i` (lib.sh). Written
# 2026-11-28; through run.sh.
set -u
source /tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/js-s158-lib/scripts/lib.sh
SHA=$1; OUT=$2; BLD=$BROOT/rules; SRC=$BLD/src; L=$OUT/logs
mkdir -p $OUT $L; setenv $BLD
# (Revised 2026-11-28, after part D's first step waited on its own tree: the archive leaves out the commit's top-level
# docs/ and logs/, records that no command here reads, as `git archive <commit> -- . ':(exclude)docs' ':(exclude)logs'`.)
newtree $BLD $SHA $L/disk.log . ':(exclude)docs' ':(exclude)logs' || exit 3
{ echo "commit: $(git -C $REPO rev-parse $SHA)"; echo "tree: $(git -C $REPO rev-parse $SHA^{tree})"; echo "sdk: $("${RUNENV[@]}" dotnet --version)"; echo "started: $(date -u +%FT%TZ)"; } > $L/commit.txt
cd $SRC
A=Broiler.ECMAScript/Broiler.ECMAScript.Architecture.Tests
diskok build $L/disk.log
T $L arch-build "${RUNENV[@]}" dotnet build $A -c Release --nologo --disable-build-servers; echo "arch-build rc=$?" >> $L/commit.txt
diskok arch-test $L/disk.log
T $L architecture-tests "${RUNENV[@]}" dotnet test $A -c Release --no-build --nologo --disable-build-servers --logger "console;verbosity=normal"; echo "architecture-tests rc=$?" >> $L/commit.txt
cp $L/architecture-tests.stdout.txt $OUT/architecture-tests.log
cp $NOTES/scripts/pack-shared.sh $OUT/pack-shared.sh
diskok pack $L/disk.log
T $L pack "${RUNENV[@]}" bash $OUT/pack-shared.sh $SRC $BLD/pack-out; echo "pack rc=$?" >> $L/commit.txt
{ cat $L/pack.stdout.txt $L/pack.stderr.txt; echo "exit $(awk -F'\t' '$1=="pack"{print $2}' $L/runs.tsv | tail -1 | sed 's/rc=//')"; } > $OUT/pack.log
( cd $BLD/pack-out && sha256sum * ) > $OUT/pack-out.sha256 2>&1
diskok pristine $L/disk.log
T $L pristine-consumer "${RUNENV[@]}" python3 -I scripts/packaging/test_pristine_consumer.py --set shared; echo "pristine-consumer rc=$?" >> $L/commit.txt
{ cat $L/pristine-consumer.stdout.txt $L/pristine-consumer.stderr.txt; echo "exit $(awk -F'\t' '$1=="pristine-consumer"{print $2}' $L/runs.tsv | tail -1 | sed 's/rc=//')"; } > $OUT/pristine-consumer.log
diskok assurance $L/disk.log
T $L hairs-restore "${RUNENV[@]}" dotnet tool restore --configfile NuGet.config; echo "hairs-restore rc=$?" >> $L/commit.txt
T $L assurance-check "${RUNENV[@]}" dotnet tool run hairs -- assurance check --root Broiler.ECMAScript; echo "assurance-check rc=$?" >> $L/commit.txt
{ cat $L/hairs-restore.stdout.txt $L/hairs-restore.stderr.txt $L/assurance-check.stdout.txt $L/assurance-check.stderr.txt; echo "exit $(awk -F'\t' '$1=="assurance-check"{print $2}' $L/runs.tsv | tail -1 | sed 's/rc=//')"; } > $OUT/assurance-check.log
echo "ended: $(date -u +%FT%TZ) first-failure=$FIRST" >> $L/commit.txt
deltree $BLD $L/disk.log
exit $FIRST
