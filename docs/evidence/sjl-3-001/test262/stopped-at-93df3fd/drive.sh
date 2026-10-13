#!/usr/bin/env bash
# Broiler.JS whole-suite test262 at the pin, AFTER milestone SJL-3.5's cut-over (93df3fd), in the
# configuration of bundle sjl-0-001's base run (its test262/drive.sh): the test262 workflow's full
# phase, all script-host-verifiable paths, eight shards, the terser variant, 30 s per test, run here
# shard after shard on one machine with four workers per shard, from a clean worktree of 93df3fd.
set -u
S=<scratch>
OUT=$S/brjs-test262-after/shard-out
SUITE=$S/suite/test262-ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e
REF=ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e
export PATH=/root/.dotnet:$PATH DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 TMPDIR=$S/tmp
cd $S/jsafter
DLL=Broiler.JS/Broiler.JavaScript/bin/Release/net10.0/BroilerJS.dll
TERSER=<repo>/.github/actions/run-test262-shard/terser/node_modules/terser
(cd Broiler.JS/Broiler.JavaScript/bin/Release/net10.0 && sha256sum *.dll) > $S/brjs-test262-after/shell-binaries.sha256
echo "self-check start $(date -u +%FT%TZ)"
python3 scripts/compliance/run_test262.py --self-check --suite-ref $REF --suite-root $SUITE --broiler-dll $DLL --no-summary-stdout > $OUT/self-check.log 2>&1
echo "self-check exit $?"
for i in 0 1 2 3 4 5 6 7; do
  if [ -f $OUT/test262-full-shard-$i-status.json ]; then echo "shard $i already done"; continue; fi
  echo "shard $i start $(date -u +%FT%TZ)"
  python3 scripts/compliance/run_test262.py --suite-ref $REF --suite-root $SUITE --broiler-dll $DLL \
    --shard-count 8 --shard-index $i --test-timeout-seconds 30 --minifier terser --terser-module $TERSER \
    --minifier-timeout-seconds 30 --memory-limit-mb 0 --max-workers 4 --feature-match any \
    --selection-label all --runner-os Linux --runner-arch X64 --dotnet-version "$(dotnet --version)" \
    --output $OUT/test262-full-shard-$i.json --no-summary-stdout --all-script-host-verifiable \
    > /dev/null 2> $OUT/test262-full-shard-$i.log
  status=$?
  printf '{"phase": "full", "shardIndex": %d, "exitCode": %d}\n' $i $status > $OUT/test262-full-shard-$i-status.json
  echo "shard $i exit $status $(date -u +%FT%TZ)"
done
python3 scripts/compliance/merge_test262_shards.py --shard-dir $OUT --phase full --expected-shards 0,1,2,3,4,5,6,7 \
  --merged-json $S/brjs-test262-after/test262-results.json > $S/brjs-test262-after/merge.log 2>&1
echo "merge exit $? $(date -u +%FT%TZ)"
echo DONE
