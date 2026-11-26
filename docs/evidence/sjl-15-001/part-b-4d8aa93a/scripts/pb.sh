#!/usr/bin/env bash
# pb.sh <full-sha> <outdir>: bundle sjl-15-001's part B at one commit (SJL-15.2 stage 5a's model commit 4d8aa93a, the
# fifth model commit, or its first parent 746591d2). Copied 2026-11-26 from stage 4's part B script
# SCRATCH/sjl-rest/js-s154-pb/scripts/pb.sh (the same method as stages 1, 2 and 3); only the worktree, the build tree,
# the suite folder and this header changed. Method as there: a clean `git archive` of the commit on disk under
# /home/user/js-s155a-pb-build/<c8>/src, outside every working tree; bundle sjl-12-001's tool
# docs/evidence/sjl-12-001/tools/parse-corpus, byte-unchanged as the commit carries it, built in Release with no build
# server, outside the tree; run under `env -i` with TZ=UTC and no LANG, LC_* or BROILER_* variable, on the tool's own
# 256 MB thread: the default mode (entry points P and K) and --dump --order manifest, over every input and variant of
# conformance/parse/corpus.manifest. The suite is Broiler.VM's test262 archive at the manifest's pin: before each run its
# SHA-256 is checked against the manifest's line pin-test262-archive and it is extracted afresh to
# /dev/shm/js-s155a-pb-suite (so no run can read a missing or stale folder). The disk check before each run is taken
# after the suite's extraction.
# Called through SCRATCH/sjl-rest/s2/ev2/scripts/run.sh, which holds the machine-wide lock behind the idle gate.
set -u
SHA=$1; OUT=$2; C8=${SHA:0:8}
SP=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad
REPO=/home/user/js-s155a-pb; BLD=/home/user/js-s155a-pb-build
SUITEREF=ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e
SUITEDIR=/dev/shm/js-s155a-pb-suite
SUITE=$SUITEDIR/test262-$SUITEREF
ARCHIVE=/home/user/Broiler.VM/src/tests/conformance/pins/test262-$SUITEREF.tar.gz
SRC=$BLD/$C8/src; ART=$BLD/$C8/art; L=$OUT/logs
export PATH=/root/.dotnet:$PATH DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 UseSharedCompilation=false
RUNENV=(env -i PATH=/root/.dotnet:/usr/bin:/bin HOME=/root DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 TZ=UTC)
mkdir -p $OUT $L
T() { local label=$1; shift; local s=$(date -u +%FT%TZ); printf '%s %s: %s\n' "$s" "$label" "$*" >> $L/commands.log
      python3 -I $SP/sjl-rest/s2/ev2/scripts/timed.py $L/$label.time.txt "$@" > $L/$label.stdout.txt 2> $L/$label.stderr.txt; local rc=$?
      printf '%s\trc=%s\t%s\tstarted=%s\tended=%s\n' "$label" "$rc" "$(cat $L/$label.time.txt)" "$s" "$(date -u +%FT%TZ)" >> $L/runs.tsv; return $rc; }
diskok() { while [ "$(df -B1 --output=avail / | tail -1)" -lt $((8*1024*1024*1024)) ]; do
             echo "$(date -u +%FT%TZ) waiting: $(df -h / | tail -1)" >> $L/disk.log; sleep 30; done
           echo "$(date -u +%FT%TZ) disk ok: $(df -B1 --output=avail / | tail -1) bytes free ($1)" >> $L/disk.log; }
suite() { # $1: label. Checks the archive's SHA-256 against the manifest's and extracts it afresh.
  local want got
  want=$(awk -F'\t' '$1=="#% pin-test262-archive"{print $3}' $SRC/conformance/parse/corpus.manifest)
  got=$(sha256sum $ARCHIVE | cut -d' ' -f1)
  echo "$(date -u +%FT%TZ) suite before $1: archive $ARCHIVE sha256 $got; manifest's pin-test262-archive $want; $([ "$got" = "$want" ] && echo equal || echo DIFFERENT)" >> $L/suite.log
  [ -n "$want" ] && [ "$got" = "$want" ] || return 1
  rm -rf $SUITEDIR; mkdir -p $SUITEDIR
  tar -xzf $ARCHIVE -C $SUITEDIR || return 1
  [ -d $SUITE/test ] || { echo "$(date -u +%FT%TZ) suite before $1: $SUITE/test missing after extraction" >> $L/suite.log; return 1; }
  echo "$(date -u +%FT%TZ) suite before $1: extracted to $SUITE, $(find $SUITE -type f | wc -l) files" >> $L/suite.log
}
rm -rf $SRC $ART; mkdir -p $SRC
{ echo "commit: $SHA"; echo "tree: $(git -C $REPO rev-parse $SHA^{tree})"; echo "first-parent: $(git -C $REPO rev-parse $SHA^1)";
  echo "parse-corpus tool tree: $(git -C $REPO rev-parse $SHA:docs/evidence/sjl-12-001/tools/parse-corpus)";
  echo "manifest blob: $(git -C $REPO rev-parse $SHA:conformance/parse/corpus.manifest)";
  echo "suite: $SUITE, from $ARCHIVE, sha256 $(sha256sum $ARCHIVE | cut -d' ' -f1)";
  echo "sdk: $(dotnet --version)"; echo "machine: $(uname -srm), $(nproc) processors"; echo "started: $(date -u +%FT%TZ)"; } > $L/commit.txt
git -C $REPO archive --format=tar $SHA | tar -xf - -C $SRC || { echo "archive failed" >> $L/commit.txt; exit 3; }
echo "archive files: $(find $SRC -type f | wc -l)" >> $L/commit.txt
cd $SRC
M=conformance/parse/corpus.manifest
rc_all=0
diskok build
T build-parse-corpus dotnet build -c Release -nologo --disable-build-servers --artifacts-path $ART/pc docs/evidence/sjl-12-001/tools/parse-corpus/parse-corpus.csproj || { echo "tool build failed" >> $L/commit.txt; cd /; rm -rf $BLD/$C8; exit 4; }
PC=$ART/pc/bin/parse-corpus/release/parse-corpus.dll
suite parse-corpus-c || { echo "suite check failed before parse-corpus-c" >> $L/commit.txt; cd /; rm -rf $BLD/$C8 $SUITEDIR; exit 7; }
diskok parse-corpus-c
T parse-corpus-c "${RUNENV[@]}" dotnet $PC --manifest $M --test262 $SUITE --out $OUT/parse-corpus-c.tsv.gz || rc_all=5
suite parse-corpus-d1 || { echo "suite check failed before parse-corpus-d1" >> $L/commit.txt; cd /; rm -rf $BLD/$C8 $SUITEDIR; exit 8; }
diskok parse-corpus-d1
T parse-corpus-d1 "${RUNENV[@]}" dotnet $PC --dump --order manifest --manifest $M --test262 $SUITE --out $OUT/parse-corpus-d1.tsv.gz || rc_all=6
( cd $OUT && find . -path ./logs -prune -o -name '*.tsv.gz' -print | sort | xargs -r sha256sum > reports.sha256.txt )
find $ART -name '*.dll' -path '*release*' | sort | xargs -r sha256sum > $L/binaries.sha256.txt
echo "ended: $(date -u +%FT%TZ) rc=$rc_all" >> $L/commit.txt
cd /; rm -rf $BLD/$C8 $SUITEDIR
exit $rc_all
