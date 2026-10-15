#!/bin/bash
export PATH=/root/.dotnet:$PATH DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 TMPDIR=<scratch>/tmp
S=<scratch>
SUITE=$S/suite/test262-ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e
cd <repo>
for side in unfixed fixed; do
  if [ $side = unfixed ]; then DLL=$S/js56after/Broiler.JS/Broiler.JavaScript/bin/Release/net10.0/BroilerJS.dll; else DLL=<repo>/Broiler.JS/Broiler.JavaScript/bin/Release/net10.0/BroilerJS.dll; fi
  python3 scripts/compliance/run_test262.py --suite-ref ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e --suite-root $SUITE --broiler-dll $DLL --minifier none --max-workers 4 --feature-match any --output $S/intl-$side.json --no-summary-stdout --path-file intl-paths.txt test/intl402 2>&1 | tail -1
done
echo done
