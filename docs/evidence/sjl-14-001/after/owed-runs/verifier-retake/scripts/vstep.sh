#!/usr/bin/env bash
# vstep.sh <commit>: the verifier's re-take of part D's dump text at <commit> (69a8a13b base, 83a9bd82 after), as the
# after driver's dtext step and the reading stage's dtextbase step ran it: bundle sjl-12-001's parse-corpus tool built
# in Release from a clean git archive, run with --dump --order manifest --text under env -i TZ=UTC. Run through run.sh.
set -u
SHA=$1; C8=${SHA:0:8}
V=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/js-w2read/verify
REPO=/home/user/broiler.js; BLD=/home/user/js-w2verify-build
SUITE=/home/user/js-w2base-build/suite/test262-ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e
export PATH=/opt/node22/bin:/root/.dotnet:$PATH DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 UseSharedCompilation=false
RUNENV=(env -i PATH=/opt/node22/bin:/root/.dotnet:/usr/bin:/bin HOME=/root DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 TZ=UTC)
avail() { df -BG --output=avail / | tail -1 | tr -dc 0-9; }
[ "$(avail)" -ge 8 ] || { echo "less than 8 GiB free: $(avail)G" >&2; exit 9; }
S=$V/runs/$C8; mkdir -p $S; W=$BLD/$C8/tree; ART=$BLD/$C8/art; TX=$BLD/$C8/text
rm -rf $W $ART; mkdir -p $W; git -C $REPO archive --format=tar $SHA | tar -xf - -C $W || exit 3
echo "commit $(git -C $REPO rev-parse $SHA) files $(find $W -type f | wc -l) free $(avail)G" > $S/archive.txt
cd $W
dotnet build -c Release -nologo --disable-build-servers -nodeReuse:false -p:UseSharedCompilation=false --artifacts-path $ART/pc docs/evidence/sjl-12-001/tools/parse-corpus/parse-corpus.csproj > $S/build.log 2>&1 || { cd /; rm -rf $W $ART; exit 4; }
( find $ART -name '*.dll' -path '*release*' | sort | xargs -r sha256sum | sed "s#$ART/##" ) > $S/binaries.sha256.txt
rm -rf $TX; mkdir -p $TX; rc=0
"${RUNENV[@]}" dotnet $ART/pc/bin/parse-corpus/release/parse-corpus.dll --dump --order manifest --text $TX/dumps-p.txt.gz \
  --manifest conformance/parse/corpus.manifest --test262 $SUITE --out $S/parse-corpus-d-text.tsv.gz > $S/run.out 2> $S/run.err || rc=6
sha256sum $TX/dumps-p.txt.gz > $S/text.sha256.txt
cd /; rm -rf $W $ART; exit $rc
