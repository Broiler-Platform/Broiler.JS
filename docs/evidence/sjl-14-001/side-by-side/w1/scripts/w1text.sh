#!/usr/bin/env bash
# w1text.sh <full-sha> <ids-file> <out-dir>: the side-by-side tool at entries P and B with --dump and --text, for the
# manifest ids named (one per line), or every input when <ids-file> is "all", so that spancheck.py can read the dumps of the variants whose P and B trees differ.
# Built and run as commit.sh does: a clean git archive on /dev/shm, Release, outside the tree, no build servers, env -i.
set -u
SHA=$1; IDS=$2; OUT=$3; C8=${SHA:0:8}
SCR=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/js-s14b
SHM=/dev/shm/js-s14b; SUITE=$SHM/suite/test262-ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e
SRC=$SHM/src/$C8-text; ART=$SHM/art/$C8-text; L=$OUT/logs
export PATH=/root/.dotnet:$PATH DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 UseSharedCompilation=false
mkdir -p $OUT $L
T() { local label=$1; shift; local s=$(date -u +%FT%TZ); printf '%s %s: %s\n' "$s" "$label" "$*" | cut -c1-2000 >> $L/commands.log
      python3 -I $SCR/scripts/timed.py $L/$label.time.txt "$@" > $L/$label.stdout.txt 2> $L/$label.stderr.txt; local rc=$?
      printf '%s\trc=%s\t%s\tstarted=%s\tended=%s\n' "$label" "$rc" "$(cat $L/$label.time.txt)" "$s" "$(date -u +%FT%TZ)" >> $L/runs.tsv; return $rc; }
rm -rf $SRC $ART; mkdir -p $SRC
git -C /home/user/js-s14b archive --format=tar $SHA | tar -xf - -C $SRC || exit 3
cd $SRC
T build-side-by-side-text dotnet build -c Release -nologo --disable-build-servers --artifacts-path $ART docs/evidence/sjl-14-001/tools/side-by-side/side-by-side.csproj || exit 4
if [ "$IDS" = all ]; then SEL=(); else SEL=(--only "$(paste -sd, $IDS)"); fi
T side-by-side-text env -i PATH=/root/.dotnet:/usr/bin:/bin HOME=/root DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 TZ=UTC dotnet $ART/bin/side-by-side/release/side-by-side.dll --manifest conformance/parse/corpus.manifest --test262 $SUITE --entries P,B --dump "${SEL[@]}" --text $OUT/dumps-p-b.txt.gz --out $OUT/side-by-side-p-b-only.tsv.gz; rc=$?
cd /; rm -rf $SRC $ART
exit $rc
