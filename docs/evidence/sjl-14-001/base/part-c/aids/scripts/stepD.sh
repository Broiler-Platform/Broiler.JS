#!/usr/bin/env bash
# stepD.sh <full-sha> <group> <step> [arg]: records part 4 of bundle sjl-14-001's W2 base (2026-11-23): reading aids and
# single-case re-runs at one commit, under run.sh's lock and idle gate. Not a run the rule names; no retained run is
# replaced. group: base (69a8a13b, from /home/user/js-w2base) or w2 (W2's head, from /home/user/js-w2code, read-only git).
# The tree is a clean `git archive` of the commit on disk, $BLD/<c8>/tree; outputs go to $SD/<group>/.
#   archive     the clean tree (archive.txt)
#   build       dotnet build Broiler.JS.slnx, Release, as js-w2base/scripts/step.sh's build-off (the switch at its default)
#   terser      npm ci of the tree's pinned Terser, as step.sh's terser step
#   codes       the corrected bridge-codes reading aid (bundle base/part-c/scripts/bridge-codes/), built outside the tree
#               against the tree's projects, over every input variant P or B refused in the base commit's retained
#               side-by-side report (codes-ids.tsv)
#   ptime       the parse-time reading aid (bundle base/part-c/scripts/parse-time/), P, L and B over the case's three
#               eval sources, each entry and source in its own process timed by timedv.py
#   case <mode> run_test262.py over test/staging/sm/regress/regress-610026.js alone (both variants), with the shards'
#               options (step.sh's t262 step, the shard and selection options replaced by --path-file), mode off or on
#               (on adds --shell-arg=--parse-through-bridge; at w2, mode w2 adds nothing, as W2 has no switch), with a
#               recording dotnet shim first on PATH (scripts/shim/dotnet); timedv.py times the runner
set -u
SHA=$1; GRP=$2; STEP=$3; ARG=${4:-}; C8=${SHA:0:8}
SP=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad
SD=$SP/sjl-rest/js-w2baseD; BLD=/home/user/js-w2baseD-build
case $GRP in base) REPO=/home/user/js-w2base;; w2) REPO=/home/user/js-w2code;; *) echo "group?" >&2; exit 2;; esac
BUNDLE=/home/user/js-w2base/docs/evidence/sjl-14-001
SUITEREF=ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e
SUITE=/home/user/js-w2base-build/suite/test262-$SUITEREF
TREE=$BLD/$C8/tree; OUT=$SD/$GRP
DLL=$TREE/Broiler.JS/Broiler.JavaScript/bin/Release/net10.0/BroilerJS.dll
BASERUN=$SP/sjl-rest/js-w2base/base
export PATH=/opt/node22/bin:/root/.dotnet:$PATH DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 UseSharedCompilation=false
mkdir -p $OUT
RUNENV=(env -i PATH=/opt/node22/bin:/root/.dotnet:/usr/bin:/bin HOME=/root DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 TZ=UTC)
BUILD=(dotnet build Broiler.JS.slnx -c Release --nologo --disable-build-servers -nodeReuse:false -p:UseSharedCompilation=false)
AIDBUILD=(dotnet build -c Release -nologo --disable-build-servers -nodeReuse:false -p:UseSharedCompilation=false)
case "$STEP" in
archive)
  rm -rf $BLD/$C8; mkdir -p $TREE
  git -C $REPO archive --format=tar $SHA | tar -xf - -C $TREE || exit 3
  { echo "commit: $(git -C $REPO rev-parse $SHA)"; echo "tree: $(git -C $REPO rev-parse $SHA^{tree})"; echo "first-parent: $(git -C $REPO rev-parse $SHA^1)"
    echo "files: $(find $TREE -type f | wc -l)"; echo "archived: $(date -u +%FT%TZ) by git -C $REPO archive --format=tar $SHA | tar -x -C <tree>, on disk"
    echo "sdk: $(dotnet --version)"; echo "node: $(node --version)"; echo "python: $(python3 --version)"; echo "machine: $(uname -srm), $(nproc) processors, $(. /etc/os-release; echo $PRETTY_NAME)"; } > $OUT/archive.txt
  ;;
build)
  cd $TREE || exit 3
  "${BUILD[@]}" > $OUT/build.log 2>&1; rc=$?
  grep -E "warning|error" $OUT/build.log | grep -v "^\s*0 " | sort -u > $OUT/build-warnings.txt
  tail -5 $OUT/build.log > $OUT/build-summary.txt
  ( cd Broiler.JS/Broiler.JavaScript/bin/Release/net10.0 && sha256sum *.dll ) > $OUT/shell-binaries.sha256
  exit $rc ;;
terser)
  cd $TREE || exit 3; P=$TREE/.github/actions/run-test262-shard/terser
  npm ci --prefix "$P" --ignore-scripts --no-audit --no-fund > $OUT/terser.log 2>&1 || exit 4
  module="$(node -e 'const path=require("node:path"); process.stdout.write(path.dirname(require.resolve("terser/package.json", {paths:[process.argv[1]]})))' "$P")"
  version="$(node -e 'process.stdout.write(require(process.argv[1]).version)' "$module/package.json")"
  echo "module=$module" >> $OUT/terser.log; echo "version=$version" >> $OUT/terser.log
  [ "$version" = 5.50.0 ] || exit 5 ;;
codes)
  python3 -I -c "
import gzip,sys,collections
t=collections.defaultdict(dict)
for l in gzip.open(sys.argv[1],'rt',encoding='utf-8'):
    if l.startswith('#%') or l.startswith('id\t'): continue
    f=l.rstrip('\n').split('\t'); t[(f[0],f[1])][f[2]]=f[3]
for k,e in t.items():
    if e.get('B')=='refused' or e.get('P')=='refused': print(k[0]+'\t'+k[1])" $BASERUN/part-c-tool/side-by-side.tsv.gz > $OUT/codes-ids.tsv
  sha256sum $BASERUN/part-c-tool/side-by-side.tsv.gz > $OUT/codes-input.sha256
  rm -rf $BLD/$C8/bridge-codes $BLD/$C8/art-codes; cp -r $BUNDLE/base/part-c/scripts/bridge-codes $BLD/$C8/bridge-codes
  "${AIDBUILD[@]}" -p:Tree=$TREE --artifacts-path $BLD/$C8/art-codes $BLD/$C8/bridge-codes/bridge-codes.csproj > $OUT/codes-build.log 2>&1 || exit 4
  ( find $BLD/$C8/art-codes -name '*.dll' -path '*release*' | sort | xargs -r sha256sum ) > $OUT/codes-binaries.sha256
  "${RUNENV[@]}" python3 -I $SD/scripts/timedv.py $OUT/codes-run.time.txt dotnet $BLD/$C8/art-codes/bin/bridge-codes/release/bridge-codes.dll \
    $TREE/conformance/parse/corpus.manifest $SUITE $OUT/codes-ids.tsv $OUT/bridge-codes.tsv > $OUT/codes-run.stdout.txt 2> $OUT/codes-run.stderr.txt
  exit $? ;;
ptime)
  rm -rf $BLD/$C8/parse-time $BLD/$C8/art-ptime; cp -r $BUNDLE/base/part-c/scripts/parse-time $BLD/$C8/parse-time
  "${AIDBUILD[@]}" -p:Tree=$TREE --artifacts-path $BLD/$C8/art-ptime $BLD/$C8/parse-time/parse-time.csproj > $OUT/ptime-build.log 2>&1 || exit 4
  ( find $BLD/$C8/art-ptime -name '*.dll' -path '*release*' | sort | xargs -r sha256sum ) > $OUT/ptime-binaries.sha256
  : > $OUT/ptime.tsv; : > $OUT/ptime.time.txt; rc=0
  for e in P L B; do for n in 1 2 3; do
    "${RUNENV[@]}" python3 -I $SD/scripts/timedv.py $OUT/ptime-$e-$n.time.txt dotnet $BLD/$C8/art-ptime/bin/parse-time/release/parse-time.dll $e $n >> $OUT/ptime.tsv 2>> $OUT/ptime.stderr.txt || rc=1
    printf '%s\t%s\t%s' "$e" "$n" "$(cat $OUT/ptime-$e-$n.time.txt)" >> $OUT/ptime.time.txt; echo >> $OUT/ptime.time.txt
  done; done
  exit $rc ;;
case)
  mode=$ARG; D=$OUT/case-$mode; rm -rf $D; mkdir -p $D/shim
  cp $SD/scripts/shim/dotnet $D/shim/dotnet; chmod +x $D/shim/dotnet
  echo test/staging/sm/regress/regress-610026.js > $D/paths.txt
  module=$(sed -n 's/^module=//p' $OUT/terser.log)
  sa=(); [ $mode = on ] && sa=(--shell-arg=--parse-through-bridge)
  cd $TREE || exit 3
  env -i PATH=$D/shim:/opt/node22/bin:/root/.dotnet:/usr/bin:/bin HOME=/root DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 TZ=UTC \
    python3 -I $SD/scripts/timedv.py $D/runner.time.txt python3 scripts/compliance/run_test262.py --suite-ref $SUITEREF --suite-root $SUITE --broiler-dll $DLL \
    --test-timeout-seconds 30 --minifier terser --minifier-timeout-seconds 30 --memory-limit-mb 0 \
    --max-workers 4 --feature-match any --selection-label regress-610026-$mode --runner-os Linux --runner-arch X64 --dotnet-version "$(dotnet --version)" \
    --output $D/result.json --no-summary-stdout --terser-module "$module" --path-file $D/paths.txt --include-negative "${sa[@]}" \
    > $D/runner.stdout.txt 2> $D/runner.stderr.txt
  ec=$?; echo "runner exit: $ec" > $D/runner.exit.txt
  cp $D/shim/shim.log $D/shim.log 2>/dev/null
  [ -f $D/result.json ] || exit 7
  exit 0 ;;
*) echo "unknown step $STEP" >&2; exit 2 ;;
esac
