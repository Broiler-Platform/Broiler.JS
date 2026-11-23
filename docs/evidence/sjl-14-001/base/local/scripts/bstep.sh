#!/usr/bin/env bash
# bstep.sh <full-sha> <group> <step>: the base commit's runs of bundle sjl-14-001's parts C (the tool), D, E, G and H that
# are not step.sh's, under run.sh's lock and idle gate. Each step takes its own clean `git archive` of the commit on disk
# ($BLD/<c8>/<step>), builds in Release with no build server, and removes that tree when it ends.
#   tools    sjl-12-001's parse-corpus tool (part D: default mode, P and K; --dump --order manifest) and the side-by-side
#            tool (part C: P, B, K, K-on with --dump; then P and B with --dump --text for class S's span check), both
#            built outside the tree (--artifacts-path); the dumps' text is kept by digest only
#   phase5   part E: scripts/performance/collect_phase5.py --rid linux-x64 (the portable compiler on fibonacci.js, the
#            output against FibonacciProgram.g.cs, the Native AOT sample published and run)
#   pack     eng/pack.ps1 -ProjectSet Engine -Version $CANDV and -ProjectSet Shared, into $BLD/<c8>/feed (part G's base
#            candidate feed); eng/verify-feed.ps1 on it
#   api      part H: the API lister of docs/evidence/sjl-0-001/api (PublicApiGenerator 11.4.6) over every assembly the
#            packages of eng/pack.ps1's two sets carry, at this commit
#   jseal    part G: Broiler.JSeal at its named commit, in a scratch clone, eng/test-package-consumer.ps1 -CandidateFeed
#            <feed> -CandidateVersion Broiler.JavaScript=$CANDV, its pins unchanged
#   html     part G: Broiler.HtmlBridge at its named commit, in a scratch clone whose NuGet.config maps Broiler.JavaScript.* and
#            Broiler.ECMAScript.* (the engine's shared dependency, packed with it) to the candidate feed and whose Directory.Packages.props names $CANDV for Broiler.JavaScript.*, never
#            committed; dotnet build and dotnet test of its solution, a TRX
set -u
SHA=$1; GRP=$2; STEP=$3; C8=${SHA:0:8}
SP=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad
SCR=$SP/sjl-rest/js-w2base; BLD=/home/user/js-w2base-build; REPO=/home/user/js-w2base
SUITE=$BLD/suite/test262-ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e
OUT=$SCR/$GRP; W=$BLD/$C8/$STEP; ART=$BLD/$C8/art-$STEP; FEED=$BLD/$C8/feed
CANDV=0.1.0-preview.1401
JSEAL=a1216a80034e185d69d0d76d9ef855105698c00b; HTML=334974f90c655181bba2ea6587ebb65336154c5a
PWSH=$BLD/tools/pwsh/pwsh
export PATH=/opt/node22/bin:/root/.dotnet:$PATH DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 UseSharedCompilation=false
RUNENV=(env -i PATH=/opt/node22/bin:/root/.dotnet:/usr/bin:/bin HOME=/root DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 TZ=UTC)
L=$OUT/logs-$STEP; mkdir -p $L
T() { local label=$1; shift; local s=$(date -u +%FT%TZ); printf '%s %s: %s\n' "$s" "$label" "$*" | cut -c1-3000 >> $L/commands.log
      python3 -I $SP/sjl-rest/s2/ev2/scripts/timed.py $L/$label.time.txt "$@" > $L/$label.stdout.txt 2> $L/$label.stderr.txt; local rc=$?
      printf '%s\trc=%s\t%s\tstarted=%s\tended=%s\n' "$label" "$rc" "$(cat $L/$label.time.txt)" "$s" "$(date -u +%FT%TZ)" >> $L/runs.tsv; return $rc; }
archive() { rm -rf $W $ART; mkdir -p $W; git -C $REPO archive --format=tar $SHA | tar -xf - -C $W || exit 3
  { echo "commit: $(git -C $REPO rev-parse $SHA)"; echo "tree: $(git -C $REPO rev-parse $SHA^{tree})"; echo "files: $(find $W -type f | wc -l)"; echo "archived: $(date -u +%FT%TZ)"; } > $L/archive.txt; }
cleanup() { cd /; rm -rf $W $ART; }
rc=0
case "$STEP" in
tools)
  archive; cd $W; M=conformance/parse/corpus.manifest; D=$OUT/part-d; C=$OUT/part-c-tool; mkdir -p $D $C
  T build-parse-corpus dotnet build -c Release -nologo --disable-build-servers --artifacts-path $ART/pc docs/evidence/sjl-12-001/tools/parse-corpus/parse-corpus.csproj || { cleanup; exit 4; }
  PC=$ART/pc/bin/parse-corpus/release/parse-corpus.dll
  T parse-corpus-c "${RUNENV[@]}" dotnet $PC --manifest $M --test262 $SUITE --out $D/parse-corpus-c.tsv.gz || rc=5
  T parse-corpus-d1 "${RUNENV[@]}" dotnet $PC --dump --order manifest --manifest $M --test262 $SUITE --out $D/parse-corpus-d1.tsv.gz || rc=6
  T build-side-by-side dotnet build -c Release -nologo --disable-build-servers --artifacts-path $ART/sbs docs/evidence/sjl-14-001/tools/side-by-side/side-by-side.csproj || { cleanup; exit 7; }
  SB=$ART/sbs/bin/side-by-side/release/side-by-side.dll
  T side-by-side "${RUNENV[@]}" dotnet $SB --manifest $M --test262 $SUITE --entries P,B,K,K-on --dump --out $C/side-by-side.tsv.gz || rc=8
  # A reading aid for class E: the library's code and message id for every variant B refuses where P accepts.
  python3 -I -c "
import gzip,sys,collections
t=collections.defaultdict(dict)
for l in gzip.open(sys.argv[1],'rt',encoding='utf-8'):
    if l.startswith('#%') or l.startswith('id\t'): continue
    f=l.rstrip('\n').split('\t'); t[(f[0],f[1])][f[2]]=f[3]
for k,e in t.items():
    if e.get('B')=='refused' and e.get('P')!='refused': print(k[0]+'\t'+k[1])" $C/side-by-side.tsv.gz > $C/bridge-codes-ids.tsv
  cp -r $SCR/scripts/bridge-codes $BLD/$C8/bridge-codes
  T build-bridge-codes dotnet build -c Release -nologo --disable-build-servers -p:Tree=$W --artifacts-path $ART/codes $BLD/$C8/bridge-codes/bridge-codes.csproj && \
  T bridge-codes "${RUNENV[@]}" dotnet $ART/codes/bin/bridge-codes/release/bridge-codes.dll $W/$M $SUITE $C/bridge-codes-ids.tsv $C/bridge-codes.tsv
  rm -rf $BLD/$C8/bridge-codes
  mkdir -p $BLD/$C8/text
  T side-by-side-text "${RUNENV[@]}" dotnet $SB --manifest $M --test262 $SUITE --entries P,B --dump --text $BLD/$C8/text/dumps-p-b.txt.gz --out $BLD/$C8/text/side-by-side-p-b-only.tsv.gz || rc=9
  ( find $ART -name '*.dll' -path '*release*' | sort | xargs -r sha256sum ) > $L/binaries.sha256.txt
  # spancheck.py reads the corpus's own inputs beside the manifest, so it runs from this tree, before the tree goes.
  T spancheck python3 -I $W/docs/evidence/sjl-14-001/tools/side-by-side/spancheck.py --manifest $W/$M --test262 $SUITE $BLD/$C8/text/dumps-p-b.txt.gz $BLD/$C8/text/spans.tsv
  src=$?; cp $L/spancheck.stdout.txt $C/spancheck-counts.txt; [ $src -le 1 ] || rc=10
  ( cd $BLD/$C8/text && awk -F'\t' 'NR==1 || $9!="S"' spans.tsv | gzip -n -9 > $C/spans-not-s.tsv.gz && sha256sum spans.tsv dumps-p-b.txt.gz side-by-side-p-b-only.tsv.gz ) > $C/not-retained.sha256.txt
  cp $W/$M $BLD/$C8/text/corpus.manifest
  cleanup; exit $rc ;;
phase5)
  archive; cd $W; E=$OUT/part-e; mkdir -p $E
  T collect-phase5 "${RUNENV[@]}" python3 -I scripts/performance/collect_phase5.py --configuration Release --rid linux-x64 --output $BLD/$C8/phase5 --report $E/phase5-report.json; rc=$?
  cp $BLD/$C8/phase5/generated/FibonacciProgram.g.cs $E/FibonacciProgram.generated.g.cs 2>/dev/null
  ( cd $BLD/$C8/phase5/native-aot 2>/dev/null && find . -type f | sort | xargs -r sha256sum ) > $E/native-aot-files.sha256.txt
  sha256sum $W/Broiler.JS/samples/Broiler.JavaScript.NativeAotSample/FibonacciProgram.g.cs | sed "s#$W/##" > $E/checked-in.sha256.txt
  rm -rf $BLD/$C8/phase5; cleanup; exit $rc ;;
pack)
  archive; cd $W; rm -rf $FEED; mkdir -p $FEED $OUT/part-g
  T pack-engine $PWSH -NoProfile -File eng/pack.ps1 -Configuration Release -ProjectSet Engine -Version $CANDV -Output $FEED/engine || rc=11
  T pack-shared $PWSH -NoProfile -File eng/pack.ps1 -Configuration Release -ProjectSet Shared -Output $FEED/shared || rc=12
  T verify-feed-engine $PWSH -NoProfile -File eng/verify-feed.ps1 $FEED/engine
  T verify-feed-shared $PWSH -NoProfile -File eng/verify-feed.ps1 $FEED/shared
  mkdir -p $FEED/all; cp $FEED/engine/*.nupkg $FEED/shared/*.nupkg $FEED/all/ 2>/dev/null
  ( cd $FEED && find . -type f -name '*.nupkg' -o -type f -name '*.snupkg' | sort | xargs -r sha256sum ) > $OUT/part-g/candidate-feed.sha256.txt
  cleanup; exit $rc ;;
api)
  archive; cd $W; H=$OUT/part-h; mkdir -p $H/api; P=$BLD/$C8/apilister; rm -rf $P; mkdir -p $P
  cp docs/evidence/sjl-0-001/api/Program.cs $P/
  # every packable project of both sets with build output, by eng/pack.ps1's own selection read through msbuild
  python3 -I - "$W" > $H/projects.txt <<'PY'
import json, re, subprocess, sys
w = sys.argv[1]
s = open(w + "/Broiler.JS.slnx").read()
engine = [p for p in re.findall(r'Path="([^"]+\.csproj)"', s) if not p.replace("\\", "/").startswith("Broiler.ECMAScript/")]
shared = json.load(open(w + "/Broiler.ECMAScript.slnf"))["solution"]["projects"]
for setname, paths in (("Engine", engine), ("Shared", shared)):
    for p in paths:
        r = subprocess.run(["dotnet", "msbuild", p, "-nologo", "-p:Configuration=Release", "-getProperty:IsPackable,PackageId,IncludeBuildOutput,AssemblyName"], cwd=w, capture_output=True, text=True)
        m = json.loads(r.stdout)["Properties"]
        if m["IsPackable"] == "true" and m["IncludeBuildOutput"] != "false":
            print("\t".join([setname, p.replace("\\", "/"), m["PackageId"], m["AssemblyName"]]))
PY
  { echo '<Project Sdk="Microsoft.NET.Sdk">'; sed -n '/<PropertyGroup>/,/<\/PropertyGroup>/p' docs/evidence/sjl-0-001/api/api-lister.csproj
    echo '  <ItemGroup>'; echo '    <PackageReference Include="PublicApiGenerator" Version="11.4.6" />'
    cut -f2 $H/projects.txt | while read p; do echo "    <ProjectReference Include=\"$W/$p\" />"; done
    echo '  </ItemGroup>'; echo '</Project>'; } > $P/api-lister.csproj
  cp $P/api-lister.csproj $H/api-lister.generated.csproj
  T build-api-lister dotnet build $P/api-lister.csproj -c Release -nologo --disable-build-servers -o $P/out || { cleanup; exit 13; }
  T run-api-lister "${RUNENV[@]}" dotnet $P/out/api-lister.dll $H/api $(cut -f4 $H/projects.txt)
  rc=$?; rm -rf $P; cleanup; exit $rc ;;
jseal)
  G=$OUT/part-g/jseal; mkdir -p $(dirname $G); [ -d $G ] && { echo "output exists: $G" >&2; exit 14; }
  rm -rf $W; git clone -q --no-hardlinks /home/user/broiler-platform/broiler.jseal $W && git -C $W checkout -q --detach $JSEAL || exit 3
  echo "jseal: $(git -C $W rev-parse HEAD) (scratch clone of the read-only clone)" > $L/archive.txt
  cd $W
  # Broiler.JSeal eng/package-consumer/README.md: "The checkout's shipping projects must have been restored (built or
  # tested once, in `Release-VM` so the VM provider is included) before a candidate run." Added 2026-11-23 after the
  # first attempt stopped with "Restore Broiler.JSeal before a candidate run." (kept as part-g/jseal-attempt-1).
  T jseal-restore "${RUNENV[@]}" dotnet restore Broiler.JSeal.slnx -p:Configuration=Release-VM || { rc=15; cd /; rm -rf $W; exit $rc; }
  # Broiler.JSeal eng/package-consumer/README.md: "The candidate directory must therefore contain every package it
  # introduces, at the versions they declare". The base's packages declare Broiler.Regex, Broiler.Unicode.Algorithms
  # and Broiler.Unicode.Segmentation 0.1.0-preview.6 (published on nuget.org), which the candidate feed does not hold;
  # attempt 2 (part-g/jseal-attempt-2) failed NU1102 on Broiler.Regex. Added 2026-11-23: the candidate directory is
  # $FEED/all's packs plus those published archives from the NuGet cache, each checked against its recorded SHA-512.
  CD=$FEED/jseal-candidate; rm -rf $CD; mkdir -p $CD; cp $FEED/all/*.nupkg $CD/
  : > $OUT/part-g/jseal-candidate-external.txt
  for p in broiler.regex broiler.unicode.algorithms broiler.unicode.segmentation; do d=/root/.nuget/packages/$p/0.1.0-preview.6; f=$d/$p.0.1.0-preview.6.nupkg
    [ "$(openssl dgst -sha512 -binary $f | base64 -w0)" = "$(cat $f.sha512)" ] || { echo "sha512 mismatch: $f" >&2; exit 16; }
    cp $f $CD/; printf '%s\tsha512=%s\tsha256=%s\tfrom=NuGet global packages folder (restored from nuget.org)\n' "$(basename $f)" "$(cat $f.sha512)" "$(sha256sum $f | cut -d" " -f1)" >> $OUT/part-g/jseal-candidate-external.txt; done
  T jseal-candidate "${RUNENV[@]}" $PWSH -NoProfile -File eng/test-package-consumer.ps1 -OutputDirectory $G -CandidateFeed $CD -CandidateVersion "Broiler.JavaScript=$CANDV"
  rc=$?; cd /; rm -rf $W /tmp/jseal-package-consumer-*; exit $rc ;;
html)
  G=$OUT/part-g/htmlbridge; mkdir -p $G
  rm -rf $W; git clone -q --no-hardlinks /home/user/broiler-platform/broiler.htmlbridge $W && git -C $W checkout -q --detach $HTML || exit 3
  echo "htmlbridge: $(git -C $W rev-parse HEAD) (scratch clone of the read-only clone)" > $L/archive.txt
  cd $W
  python3 -I - "$W" "$FEED/all" "$CANDV" <<'PY'
import re, sys
w, feed, v = sys.argv[1:4]
p = w + "/Directory.Packages.props"; s = open(p).read()
s2 = re.sub(r'(<PackageVersion Include="Broiler\.JavaScript\.[^"]+" Version=")[^"]+(")', r'\g<1>' + v + r'\2', s)
open(p, "w").write(s2)
n = w + "/NuGet.config"; c = open(n).read()
c = c.replace('<add key="nuget.org"', '<add key="candidate" value="' + feed + '" />\n    <add key="nuget.org"', 1)
c = c.replace('<packageSource key="nuget.org">', '<packageSource key="candidate">\n      <package pattern="Broiler.JavaScript.*" />\n      <package pattern="Broiler.ECMAScript.*" />\n    </packageSource>\n    <packageSource key="nuget.org">', 1)
open(n, "w").write(c)
PY
  git -C $W diff > $G/scratch-overrides.diff
  T html-restore dotnet restore Broiler.HtmlBridge.slnx --packages $BLD/$C8/html-packages --no-http-cache || { rc=15; }
  T html-build dotnet build Broiler.HtmlBridge.slnx -c Release --no-restore -nologo --disable-build-servers || rc=16
  T html-test "${RUNENV[@]}" NUGET_PACKAGES=$BLD/$C8/html-packages dotnet test Broiler.HtmlBridge.slnx -c Release --no-build --nologo --logger "trx;LogFileName=htmlbridge.trx" --results-directory $G/trx || rc=17
  find $BLD/$C8/html-packages -maxdepth 2 -mindepth 2 -ipath '*broiler.javascript*' | sed "s#$BLD/$C8/html-packages/##" | sort > $G/resolved-broiler-javascript.txt
  cd /; rm -rf $W $BLD/$C8/html-packages; exit $rc ;;
*) echo "unknown step $STEP" >&2; exit 2 ;;
esac
