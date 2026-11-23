#!/usr/bin/env bash
# step.sh <full-sha> <group> <step> [arg]: one step of bundle sjl-14-001's part C (and D, E, H) local runs at one commit,
# run under run.sh's lock and idle gate. group: w1 (W1, 8e5519d8, read and not judged) or base (the base commit, 69a8a13b).
# The tree is a clean `git archive` of the commit on disk, $BLD/<c8>/tree; outputs go to $SCR/<group>/.
# Steps:
#   archive        the clean tree (archive.txt: commit, tree, first parent, file count)
#   build-off      dotnet build Broiler.JS.slnx, Release, the switch at its default (off)
#   build-on       the same with -p:BroilerParseThroughBridge=true (the switch on for every compile, decision D43's note)
#   terser         npm ci of the tree's pinned Terser (.github/actions/run-test262-shard/terser), version checked
#   tests-<mode>   every test project of Broiler.JS.slnx under Broiler.JS/, one dotnet test each, --no-build, a TRX each
#   t262-<mode> i  shard i of 8 of run_test262.py over the whole selection part A takes (--all-script-host-verifiable,
#                  --include-negative, Terser and original variants, 30 s, no memory cap), one worker per core (4),
#                  through the tree's Release shell; mode on adds --shell-arg=--parse-through-bridge; the async
#                  self-check first
#   t262merge-<mode>  merge_test262_shards.py over the eight shards, the timed-out paths listed
#   t262rerun-<mode>  every timed-out path again alone, with one worker (as test262-evidence.yml's re-run)
set -u
SHA=$1; GRP=$2; STEP=$3; ARG=${4:-}; C8=${SHA:0:8}
SP=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad
SCR=$SP/sjl-rest/js-w2base; BLD=/home/user/js-w2base-build
REPO=/home/user/js-w2base
SUITEREF=ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e
SUITE=$BLD/suite/test262-$SUITEREF
TREE=$BLD/$C8/tree; OUT=$SCR/$GRP
DLL=$TREE/Broiler.JS/Broiler.JavaScript/bin/Release/net10.0/BroilerJS.dll
export PATH=/opt/node22/bin:/root/.dotnet:$PATH DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 UseSharedCompilation=false
mkdir -p $OUT
# A run's environment: TZ=UTC, no LANG, no LC_*, no BROILER_* (env -i with PATH, HOME, DOTNET_ROOT).
RUNENV=(env -i PATH=/opt/node22/bin:/root/.dotnet:/usr/bin:/bin HOME=/root DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 TZ=UTC)
BUILD=(dotnet build Broiler.JS.slnx -c Release --nologo --disable-build-servers -nodeReuse:false -p:UseSharedCompilation=false)
case "$STEP" in
archive)
  rm -rf $BLD/$C8; mkdir -p $TREE
  git -C $REPO archive --format=tar $SHA | tar -xf - -C $TREE || exit 3
  { echo "commit: $(git -C $REPO rev-parse $SHA)"; echo "tree: $(git -C $REPO rev-parse $SHA^{tree})"; echo "first-parent: $(git -C $REPO rev-parse $SHA^1)"
    echo "files: $(find $TREE -type f | wc -l)"; echo "archived: $(date -u +%FT%TZ) by git archive --format=tar $SHA | tar -x -C <tree>, on disk"
    echo "sdk: $(dotnet --version)"; echo "node: $(node --version)"; echo "python: $(python3 --version)"; echo "machine: $(uname -srm), $(nproc) processors, $(. /etc/os-release; echo $PRETTY_NAME)"; } > $OUT/archive.txt
  ;;
build-off|build-on)
  mode=${STEP#build-}; extra=(); [ $mode = on ] && extra=(-p:BroilerParseThroughBridge=true)
  cd $TREE || exit 3
  "${BUILD[@]}" "${extra[@]}" > $OUT/build-$mode.log 2>&1; rc=$?
  grep -E "warning|error" $OUT/build-$mode.log | grep -v "^\s*0 " | sort -u > $OUT/build-$mode-warnings.txt
  tail -5 $OUT/build-$mode.log > $OUT/build-$mode-summary.txt
  ( cd Broiler.JS/Broiler.JavaScript/bin/Release/net10.0 && sha256sum *.dll ) > $OUT/shell-binaries-$mode.sha256
  exit $rc ;;
terser)
  cd $TREE || exit 3; P=$TREE/.github/actions/run-test262-shard/terser
  npm ci --prefix "$P" --ignore-scripts --no-audit --no-fund > $OUT/terser.log 2>&1 || exit 4
  module="$(node -e 'const path=require("node:path"); process.stdout.write(path.dirname(require.resolve("terser/package.json", {paths:[process.argv[1]]})))' "$P")"
  version="$(node -e 'process.stdout.write(require(process.argv[1]).version)' "$module/package.json")"
  echo "module=$module" >> $OUT/terser.log; echo "version=$version" >> $OUT/terser.log
  [ "$version" = 5.50.0 ] || exit 5 ;;
tests-off|tests-on)
  mode=${STEP#tests-}; D=$OUT/tests-$mode; rm -rf $D; mkdir -p $D/trx $D/logs
  cd $TREE || exit 3; rc=0
  python3 -I -c "
import re,sys
s=open('Broiler.JS.slnx').read()
print('\n'.join(p for p in re.findall(r'Path=\"([^\"]+)\"',s) if p.startswith('Broiler.JS/') and p.endswith('Tests.csproj')))" > $D/projects.txt
  while read proj; do
    name=$(basename $proj .csproj)
    "${RUNENV[@]}" dotnet test $proj -c Release --no-build --nologo --logger "trx;LogFileName=$name.trx" --results-directory $D/trx > $D/logs/$name.log 2>&1
    r=$?; printf '%s\t%s\n' "$name" "$r" >> $D/exits.tsv; [ $r = 0 ] || rc=1
  done < $D/projects.txt
  echo "any project failed: $rc (each project's exit in exits.tsv; a test failure is a result, not a step failure)" > $D/summary.txt
  exit 0 ;;
t262-off|t262-on)
  mode=${STEP#t262-}; i=$ARG; D=$OUT/t262-$mode/shards; mkdir -p $D
  cd $TREE || exit 3
  module=$(sed -n 's/^module=//p' $OUT/terser.log)
  sa=(); [ $mode = on ] && sa=(--shell-arg=--parse-through-bridge)
  "${RUNENV[@]}" python3 scripts/compliance/run_test262.py --self-check --suite-ref $SUITEREF --suite-root $SUITE --broiler-dll $DLL --no-summary-stdout > $D/self-check-$i.log 2>&1 || { echo "self-check failed" >> $D/self-check-$i.log; exit 6; }
  stem=test262-full-shard-$i
  "${RUNENV[@]}" python3 scripts/compliance/run_test262.py --suite-ref $SUITEREF --suite-root $SUITE --broiler-dll $DLL \
    --shard-count 8 --shard-index $i --test-timeout-seconds 30 --minifier terser --minifier-timeout-seconds 30 --memory-limit-mb 0 \
    --max-workers 4 --feature-match any --selection-label all --runner-os Linux --runner-arch X64 --dotnet-version "$(dotnet --version)" \
    --output $D/$stem.json --no-summary-stdout --terser-module "$module" --all-script-host-verifiable --include-negative "${sa[@]}" \
    > /dev/null 2> $D/$stem.log
  ec=$?
  python3 -I -c "
import json,sys
from pathlib import Path
d,stem,i,ec=Path(sys.argv[1]),sys.argv[2],int(sys.argv[3]),int(sys.argv[4])
st={'phase':'full','shardIndex':i,'exitCode':ec}
if not (d/(stem+'.json')).is_file(): st['failureReason']='RunnerDidNotProduceReport'
(d/(stem+'-status.json')).write_text(json.dumps(st,indent=2)+'\n',encoding='utf-8')" $D $stem $i $ec
  ( cd Broiler.JS/Broiler.JavaScript/bin/Release/net10.0 && sha256sum *.dll ) > $D/shell-binaries-shard-$i.sha256
  [ -f $D/$stem.json ] || exit 7
  exit 0 ;;
t262merge-off|t262merge-on)
  mode=${STEP#t262merge-}; E=$OUT/t262-$mode; cd $TREE || exit 3
  python3 scripts/compliance/merge_test262_shards.py --shard-dir $E/shards --phase full --expected-shards 0,1,2,3,4,5,6,7 \
    --merged-json $E/test262-results.json > $E/merge.log 2>&1; rc=$?
  python3 -I -c "
import json,sys
from pathlib import Path
r=json.loads(Path(sys.argv[1]).read_text(encoding='utf-8'))
p=sorted({str(x['path']) for x in r.get('results',[]) if x.get('status')=='timedOut'})
Path(sys.argv[2]).write_text(''.join(x+'\n' for x in p),encoding='utf-8'); print(len(p),'timed-out path(s)')" $E/test262-results.json $E/timed-out-paths.txt >> $E/merge.log
  exit $rc ;;
t262rerun-off|t262rerun-on)
  mode=${STEP#t262rerun-}; E=$OUT/t262-$mode; cd $TREE || exit 3
  [ -s $E/timed-out-paths.txt ] || { echo "no timed-out path" > $E/rerun-timeouts.log; exit 0; }
  module=$(sed -n 's/^module=//p' $OUT/terser.log)
  sa=(); [ $mode = on ] && sa=(--shell-arg=--parse-through-bridge)
  "${RUNENV[@]}" python3 scripts/compliance/run_test262.py --suite-ref $SUITEREF --suite-root $SUITE --broiler-dll $DLL \
    --test-timeout-seconds 30 --minifier terser --minifier-timeout-seconds 30 --memory-limit-mb 0 \
    --max-workers 1 --feature-match any --selection-label rerun-failed --runner-os Linux --runner-arch X64 --dotnet-version "$(dotnet --version)" \
    --output $E/rerun-timeouts.json --no-summary-stdout --terser-module "$module" --path-file $E/timed-out-paths.txt --include-negative "${sa[@]}" \
    > /dev/null 2> $E/rerun-timeouts.log
  [ -f $E/rerun-timeouts.json ] || exit 7
  exit 0 ;;
*) echo "unknown step $STEP" >&2; exit 2 ;;
esac
