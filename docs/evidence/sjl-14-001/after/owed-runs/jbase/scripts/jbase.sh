#!/usr/bin/env bash
# jbase.sh: reading aid, the V000002 seed and its 22 findings compiled one per process at the BASE commit 69a8a13b with the same single-case tool and bounds as the after commit's jsingle step
# ran it: the retained single-case aid built inside a clean archive, one process per file, bound 10000 ms, cap 120000 ms.
set -u
V=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/js-w2read/verify
AID=/home/user/js-w2read/docs/evidence/sjl-14-001/after/owed-runs/scripts/single-case
FIND=/home/user/js-w2read/docs/evidence/sjl-14-001/after/local/fuzz/session/findings
REPO=/home/user/broiler.js; BLD=/home/user/js-w2jbase-build; AFTER=69a8a13bc446a1714d3c8be9be3cfd742aebee6e
SUITE=/home/user/js-w2base-build/suite/test262-ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e
export PATH=/opt/node22/bin:/root/.dotnet:$PATH DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 UseSharedCompilation=false
RUNENV=(env -i PATH=/opt/node22/bin:/root/.dotnet:/usr/bin:/bin HOME=/root DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 TZ=UTC)
avail() { df -BG --output=avail / | tail -1 | tr -dc 0-9; }
[ "$(avail)" -ge 8 ] || { echo "less than 8 GiB free: $(avail)G" >&2; exit 9; }
S=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/js-w2jbase/runs; mkdir -p $S; W=$BLD/69a8a13b/jsingle; ART=$BLD/69a8a13b/art-jsingle
rm -rf $W $ART; mkdir -p $W; git -C $REPO archive --format=tar $AFTER | tar -xf - -C $W || exit 3
cd $W; mkdir -p docs/evidence/sjl-14-001/tools/single-case; cp $AID/Program.cs $AID/single-case.csproj docs/evidence/sjl-14-001/tools/single-case/
dotnet build -c Release -nologo --disable-build-servers -nodeReuse:false -p:UseSharedCompilation=false --artifacts-path $ART docs/evidence/sjl-14-001/tools/single-case/single-case.csproj > $S/build.log 2>&1 || { cd /; rm -rf $W $ART; exit 4; }
X=$ART/bin/single-case/release/single-case.dll; rc=0
printf 'file\tsha256\tgoal\toutcome\ttype\tmessage\telapsed-ms\twithin-bound\n' > $S/cases.tsv
one() { "${RUNENV[@]}" dotnet $X $1 $2 10000 120000 >> $S/cases.tsv 2>> $S/run.err || { echo "$1 rc!=0" >> $S/cases.tsv; rc=7; }; }
one $W/conformance/parse/inputs/broiler-vm/generated/refuse-more-locals-than-the-frame-admits.js script
for f in $(ls $FIND | grep '^V000002-' | sort -t- -k2 -n); do one $FIND/$f script; done
cd /; rm -rf $W $ART; exit $rc
