#!/usr/bin/env bash
# astep.sh <full-sha> <group> <step>: one of bundle sjl-14-001's local runs at the after commit (W2's merge, 83a9bd82),
# under run.sh's lock and idle gate. Adapted on 2026-11-23 from the base's scripts/bstep.sh and scripts/step.sh
# (SCRATCH/sjl-rest/js-w2base/scripts/, retained in docs/evidence/sjl-14-001/base/local/scripts/), keeping their
# conventions: each step takes its own clean `git archive` of the commit on disk ($BLD/<c8>/<step>), outside every working
# tree, builds in Release with no build server, and removes that tree when it ends; every test, tool and engine run is
# under `env -i` with TZ=UTC and no LANG, LC_* or BROILER_* variable ($RUNENV); each command's line, exit and timing go
# to <step>/logs/commands.log, runs.tsv and <label>.{stdout,stderr,time}.txt. Outputs go to $OUT/<step>/ alone.
# The bundle's own tools are used as the after commit carries them.
# Review fixes of 2026-11-23 (the text as started is fix/astep.sh.before-fix): a step's build-output folder art-<step>
# is cleared with its tree at its start (as the base's archive() did); the consumers' temporary folders are removed by
# what this step made (tmpbefore/tmpafter/tmpretake), not by a glob over /tmp; html's package folder is cleared before
# its restore; the steps reading pack's feed record pack's marker; the steps dcodes and dtext.
#   archive    the after commit's identity (tree, first parent = the base commit, second parent), a test archive's file
#              count, the machine; the test262 suite and pwsh copied from the base's build folder if absent, the suite's
#              content digest beside the source's
#   partd      part D: bundle sjl-12-001's parse-corpus tool, byte-unchanged, built in Release outside the tree and run in
#              its default mode (entries P and K; P is the adapter Parser package's FastParser facade at this commit)
#              and with --dump --order manifest, as the base's `tools` step took part D
#   dcontrol   part D's control: compare_parse_corpus.py control over the base report (a copy with one outcome flipped
#              and one message changed, which the comparison must report as two differences)
#   dcodes     part D, a reading aid (added 2026-11-23, after review): the library's diagnostic code and message id for
#              every input variant P accepts at the base and refuses at the after commit (read from the two reports),
#              by part C's corrected bridge-codes aid as the after commit carries it, with one change for the after
#              commit's three-parameter SharedSyntaxBridge.Request (dcodes/aid.patch); rev-code and class E read it
#   dtext      part D, the dump text (added 2026-11-23, after review): the same tool and build as partd, with --dump
#              --order manifest --text <build folder>/text/dumps-p.txt.gz; the text is kept in the build folder, by
#              digest here, and its report's rows are checked against partd's parse-corpus-d1; rev-eof, rev-bracket and
#              S read it
#   ptests     part E: dotnet test of Broiler.JS/Broiler.JavaScript.Portable.Tests in Release, a TRX
#   phase5     part E: scripts/performance/collect_phase5.py --configuration Release --rid linux-x64, as at the base
#   closure    part E: the closure of Broiler.JS/Broiler.JavaScript.Portable.Compiler: its transitive project and
#              package references (project.assets.json, dotnet list package --include-transitive, dotnet list
#              reference) and the assemblies its publish output holds
#   pack       parts F and G: eng/pack.ps1 -ProjectSet Engine -Version $CANDV and -ProjectSet Shared into $FEED, and
#              eng/verify-feed.ps1 on each (part G's after candidate feed; part F's local feed)
#   pristine   part F: scripts/packaging/test_pristine_consumer.py --feed <Engine output> --feed <Shared output>
#   pcontrol   part F's control: the same with --control-remove Broiler.ECMAScript.Frontend
#   obsolete   part F: the bundle's metadata reader (tools/obsolete-reader) over the feed's Broiler.JavaScript.Ast and
#              Broiler.JavaScript.Parser packages
#   api        part H: the API lister of docs/evidence/sjl-0-001/api over every assembly the packages of eng/pack.ps1's
#              two sets carry, as at the base
#   jseal      part G: Broiler.JSeal at its named commit, the base's harness: restored in Release-VM first, then
#              eng/test-package-consumer.ps1 -CandidateFeed <dir> -CandidateVersion Broiler.JavaScript=$CANDV with a
#              candidate directory of the feed's packages and every external Broiler.* package they declare, each from
#              the NuGet global packages folder, SHA-512 checked; and a TRX per suite (VSTestLogger), not taken at the base,
#              listed after the run by digest, test count and configuration (jseal-trx.tsv); none is written to FAILED
#   html       part G: Broiler.HtmlBridge at its named commit, as at the base, with $CANDV
#   fuzz       part J: the bundle's seeded mutation session (tools/fuzz) built in Release, over inputs/fuzz-seeds.txt
#   fcontrol   part J's control: a scratch copy in which the bridge throws on a template literal
#              (BridgeTreeBuilder.Template), built and run over the seeds; reverted from the commit's blob and the
#              revert checked against a clean archive
#   delsearch  part K: tools/deletion-search/search.py over a clean tree, and its --control in a second clean copy, the
#              revert checked against the first
set -u
SHA=$1; GRP=$2; STEP=$3; C8=${SHA:0:8}
SP=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad
SCR=$SP/sjl-rest/js-w2after; BLD=/home/user/js-w2after-build; REPO=/home/user/broiler.js
BASECOMMIT=69a8a13bc446a1714d3c8be9be3cfd742aebee6e
SUITEREF=ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e
SUITE=$BLD/suite/test262-$SUITEREF; SUITESRC=/home/user/js-w2base-build/suite/test262-$SUITEREF
BASEANS=$SCR/base/docs/evidence/sjl-14-001/base/local
OUT=$SCR/$GRP; W=$BLD/$C8/$STEP; ART=$BLD/$C8/art-$STEP; FEED=$BLD/$C8/feed
CANDV=0.1.0-preview.1402
JSEAL=a1216a80034e185d69d0d76d9ef855105698c00b; HTML=334974f90c655181bba2ea6587ebb65336154c5a
PWSH=$BLD/tools/pwsh/pwsh; PWSHSRC=/home/user/js-w2base-build/tools/pwsh
export PATH=/opt/node22/bin:/root/.dotnet:$PATH DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0 UseSharedCompilation=false
RUNENV=(env -i PATH=/opt/node22/bin:/root/.dotnet:/usr/bin:/bin HOME=/root DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 TZ=UTC)
S=$OUT/$STEP; L=$S/logs; mkdir -p $L
T() { local label=$1; shift; local s=$(date -u +%FT%TZ); printf '%s %s: %s\n' "$s" "$label" "$*" | cut -c1-3000 >> $L/commands.log
      python3 -I $SP/sjl-rest/s2/ev2/scripts/timed.py $L/$label.time.txt "$@" > $L/$label.stdout.txt 2> $L/$label.stderr.txt; local rc=$?
      printf '%s\trc=%s\t%s\tstarted=%s\tended=%s\n' "$label" "$rc" "$(cat $L/$label.time.txt)" "$s" "$(date -u +%FT%TZ)" >> $L/runs.tsv; return $rc; }
archive() { local d=${1:-$W}; rm -rf $d; [ "$d" = "$W" ] && rm -rf $ART; mkdir -p $d; git -C $REPO archive --format=tar $SHA | tar -xf - -C $d || exit 3
  { echo "commit: $(git -C $REPO rev-parse $SHA)"; echo "tree: $(git -C $REPO rev-parse $SHA^{tree})"; echo "path: $d"; echo "files: $(find $d -type f | wc -l)"; echo "archived: $(date -u +%FT%TZ)"; } >> $L/archive.txt; }
cleanup() { cd /; rm -rf $W $ART "$@"; }
# The consumers' own temporary folders, /tmp/<prefix><id>: each consumer removes its folder when it ends. A folder this
# step made and left (an attempt killed with the container) is told from other stages' by the listing taken before the
# run and the machine's boot time; every other folder is left alone and listed (logs/tmp-folders.txt).
TMPREC=$BLD/$C8/tmp-$STEP
tmplist() { ls -d /tmp/$1* 2>/dev/null | LC_ALL=C sort; }
tmpbefore() { mkdir -p $BLD/$C8; tmplist $1 > $TMPREC.before; date -u +%s > $TMPREC.started; }
tmpafter() { LC_ALL=C comm -13 $TMPREC.before <(tmplist $1) | while read -r d; do echo "removed after the run: $d" >> $L/tmp-folders.txt; rm -rf "$d"; done
  rm -f $TMPREC.before $TMPREC.started; }
tmpretake() { [ -f $TMPREC.started ] && [ -f $TMPREC.before ] || return 0; local s b d m; s=$(cat $TMPREC.started); b=$(awk '/^btime/{print $2}' /proc/stat)
  LC_ALL=C comm -13 $TMPREC.before <(tmplist $1) | while read -r d; do m=$(stat -c %Y "$d")
    if [ "$b" -gt "$s" ] && [ "$m" -ge "$s" ] && [ "$m" -le "$b" ]; then echo "removed, made by an attempt begun at $s and killed before the boot at $b: $d" >> $L/tmp-folders.txt; rm -rf "$d"
    else echo "left alone, not shown to be this step's: $d" >> $L/tmp-folders.txt; fi; done
  rm -f $TMPREC.before $TMPREC.started; }
packmark() { { cat $OUT/done/a-pack 2>/dev/null || echo "no marker done/a-pack"; } > $S/pack-marker.txt; }
suitedigest() { ( cd "$1" && find . -type f -print0 | sort -z | xargs -0 sha256sum ) | sha256sum | cut -d' ' -f1; }
BUILD=(dotnet build -c Release -nologo --disable-build-servers -nodeReuse:false -p:UseSharedCompilation=false)
rc=0
case "$STEP" in
archive)
  [ "$(git -C $REPO rev-parse $SHA)" = "$SHA" ] || exit 3
  [ "$(git -C $REPO rev-parse $SHA^1)" = "$BASECOMMIT" ] || { echo "first parent is not the base commit" >&2; exit 3; }
  mkdir -p $BLD/suite $BLD/tools
  [ -d $SUITE ] || cp -a $SUITESRC $BLD/suite/ || exit 4
  [ -x $PWSH ] || cp -a $PWSHSRC $BLD/tools/ || exit 4
  archive $BLD/$C8/archive
  { echo "commit: $(git -C $REPO rev-parse $SHA) (the after commit, W2's merge, Broiler-Platform/Broiler.JS#1129)"
    echo "tree: $(git -C $REPO rev-parse $SHA^{tree})"; echo "first-parent: $(git -C $REPO rev-parse $SHA^1) (the base commit)"
    echo "second-parent: $(git -C $REPO rev-parse $SHA^2)"; echo "files: $(find $BLD/$C8/archive -type f | wc -l)"
    echo "archived: $(date -u +%FT%TZ) by git -C $REPO archive --format=tar $SHA | tar -x -C <dir>, on disk, read-only git"
    echo "suite: $SUITE, content digest $(suitedigest $SUITE) (sha256 of the sorted sha256sum listing of every file)"
    [ -d $SUITESRC ] && echo "suite source: $SUITESRC (the base runs' copy), content digest $(suitedigest $SUITESRC)"
    echo "pwsh: $PWSH ($($PWSH -NoProfile -Command '$PSVersionTable.PSVersion.ToString()' 2>&1)), copied from $PWSHSRC"
    echo "candidate version: $CANDV (Engine set; the base's was 0.1.0-preview.1401)"
    echo "sdk: $(dotnet --version)"; echo "node: $(node --version)"; echo "python: $(python3 --version)"
    echo "machine: $(uname -srm), $(nproc) processors, $(. /etc/os-release; echo $PRETTY_NAME)"; } > $S/archive.txt
  rm -rf $BLD/$C8/archive; exit 0 ;;
partd)
  archive; cd $W; M=conformance/parse/corpus.manifest; D=$S
  T build-parse-corpus "${BUILD[@]}" --artifacts-path $ART/pc docs/evidence/sjl-12-001/tools/parse-corpus/parse-corpus.csproj || { cleanup; exit 4; }
  PC=$ART/pc/bin/parse-corpus/release/parse-corpus.dll
  T parse-corpus-c "${RUNENV[@]}" dotnet $PC --manifest $M --test262 $SUITE --out $D/parse-corpus-c.tsv.gz || rc=5
  T parse-corpus-d1 "${RUNENV[@]}" dotnet $PC --dump --order manifest --manifest $M --test262 $SUITE --out $D/parse-corpus-d1.tsv.gz || rc=6
  ( find $ART -name '*.dll' -path '*release*' | sort | xargs -r sha256sum ) > $L/binaries.sha256.txt
  sha256sum $M | sed "s#  # #" > $D/manifest.sha256.txt
  cleanup; exit $rc ;;
dcontrol)
  archive; cd $W
  T control "${RUNENV[@]}" python3 -I scripts/compliance/compare_parse_corpus.py control --base $BASEANS/part-d/parse-corpus-c.tsv.gz \
    --out $S/control-copy.tsv.gz --output $S/control.json; rc=$?
  sha256sum $BASEANS/part-d/parse-corpus-c.tsv.gz | sed "s#$SCR/##" > $S/base-report.sha256.txt
  cleanup; exit $rc ;;
dcodes)
  [ -s $OUT/partd/parse-corpus-c.tsv.gz ] || { echo "no after report: take partd first" >&2; exit 3; }
  archive; cd $W; M=conformance/parse/corpus.manifest; AID=$BLD/$C8/bridge-codes; A=docs/evidence/sjl-14-001/base/part-c/scripts/bridge-codes
  # the input variants: P accepted in the base report, not accepted in the after report (partd)
  python3 -I - $BASEANS/part-d/parse-corpus-c.tsv.gz $OUT/partd/parse-corpus-c.tsv.gz > $S/ids.tsv <<'PY' || { cleanup; exit 3; }
import gzip, sys
def p_rows(path):
    out, header = {}, None
    with gzip.open(path, "rt", encoding="utf-8") as f:
        for line in f:
            if line.startswith("#"): continue
            x = line.rstrip("\n").split("\t")
            if header is None: header = x; continue
            if x[2] == "P": out[(x[0], x[1])] = x[3]
    return out
base, after = p_rows(sys.argv[1]), p_rows(sys.argv[2])
assert base.keys() == after.keys(), "the two reports name different input variants"
for key in base:
    if base[key] == "accepted" and after[key] != "accepted":
        print(key[0] + "\t" + key[1])
PY
  rm -rf $AID; cp -r $A $AID
  python3 -I - $AID/Program.cs <<'PY' || { cleanup $AID; exit 3; }
import sys
p = sys.argv[1]; s = open(p, "rb").read()
old = b'request.Invoke(null, [span, variant == "module" ? module : script])'
new = b'request.Invoke(null, [span, variant == "module" ? module : script, false])'
assert s.count(old) == 1, "the call is not found once"
open(p, "wb").write(s.replace(old, new))
PY
  { echo "# The after commit's SharedSyntaxBridge.Request takes a third parameter, bool retainTokens = false"
    echo "# (Broiler.JS/Broiler.JavaScript.Parser/Bridge/SharedSyntaxBridge.cs); reflection fills no optional parameter, so the"
    echo "# aid passes false, the value the bridge's own Parse passes. Nothing else is changed."
    diff -u --label a/$A/Program.cs --label b/$A/Program.cs $W/$A/Program.cs $AID/Program.cs; } > $S/aid.patch
  T build-bridge-codes "${BUILD[@]}" -p:Tree=$W --artifacts-path $ART/codes $AID/bridge-codes.csproj || { cleanup $AID; exit 4; }
  T bridge-codes "${RUNENV[@]}" dotnet $ART/codes/bin/bridge-codes/release/bridge-codes.dll $W/$M $SUITE $S/ids.tsv $S/bridge-codes.tsv; rc=$?
  ( find $ART -name '*.dll' -path '*release*' | sort | xargs -r sha256sum ) > $L/binaries.sha256.txt
  sha256sum $BASEANS/part-d/parse-corpus-c.tsv.gz $OUT/partd/parse-corpus-c.tsv.gz | sed "s#$SCR/##" > $S/reports-read.sha256.txt
  cleanup $AID; exit $rc ;;
dtext)
  [ -s $OUT/partd/parse-corpus-d1.tsv.gz ] || { echo "no after dump report: take partd first" >&2; exit 3; }
  archive; cd $W; M=conformance/parse/corpus.manifest; TX=$BLD/$C8/text
  T build-parse-corpus "${BUILD[@]}" --artifacts-path $ART/pc docs/evidence/sjl-12-001/tools/parse-corpus/parse-corpus.csproj || { cleanup; exit 4; }
  PC=$ART/pc/bin/parse-corpus/release/parse-corpus.dll
  rm -rf $TX; mkdir -p $TX
  T parse-corpus-d-text "${RUNENV[@]}" dotnet $PC --dump --order manifest --text $TX/dumps-p.txt.gz --manifest $M --test262 $SUITE --out $S/parse-corpus-d-text.tsv.gz || rc=6
  ( find $ART -name '*.dll' -path '*release*' | sort | xargs -r sha256sum ) > $L/binaries.sha256.txt
  { echo "# the dump text, kept outside the notes (by digest here), to be kept until part D's comparison has read it"
    ls -l $TX/dumps-p.txt.gz; sha256sum $TX/dumps-p.txt.gz; } > $S/text.sha256.txt 2>&1
  python3 -I - $S/parse-corpus-d-text.tsv.gz $OUT/partd/parse-corpus-d1.tsv.gz > $S/identity-with-partd.txt <<'PY'
import gzip, sys
def rows(path):
    with gzip.open(path, "rt", encoding="utf-8") as f:
        return [line for line in f if not line.startswith("#")]
a, b = rows(sys.argv[1]), rows(sys.argv[2])
same = sum(1 for x, y in zip(a, b) if x == y)
print("rows (header line included, '#' lines left out): this run " + str(len(a)) + ", partd's parse-corpus-d1 " + str(len(b)))
print("identical, line by line: " + str(same) + (" (all)" if same == len(a) == len(b) else " (NOT all)"))
PY
  cleanup; exit $rc ;;
ptests)
  archive; cd $W; P=Broiler.JS/Broiler.JavaScript.Portable.Tests/Broiler.JavaScript.Portable.Tests.csproj
  T build-portable-tests "${BUILD[@]}" $P || { cleanup; exit 4; }
  T test-portable "${RUNENV[@]}" dotnet test $P -c Release --no-build --nologo --logger "trx;LogFileName=Broiler.JavaScript.Portable.Tests.trx" --results-directory $S/trx; rc=$?
  ( cd $W/Broiler.JS/Broiler.JavaScript.Portable.Tests/bin/Release && find . -name '*.dll' | sort | xargs -r sha256sum ) > $L/binaries.sha256.txt
  cleanup; exit $rc ;;
phase5)
  archive; cd $W; E=$S
  T collect-phase5 "${RUNENV[@]}" python3 -I scripts/performance/collect_phase5.py --configuration Release --rid linux-x64 --output $BLD/$C8/phase5 --report $E/phase5-report.json; rc=$?
  cp $BLD/$C8/phase5/generated/FibonacciProgram.g.cs $E/FibonacciProgram.generated.g.cs 2>/dev/null
  ( cd $BLD/$C8/phase5/native-aot 2>/dev/null && find . -type f | sort | xargs -r sha256sum ) > $E/native-aot-files.sha256.txt
  sha256sum $W/Broiler.JS/samples/Broiler.JavaScript.NativeAotSample/FibonacciProgram.g.cs | sed "s#$W/##" > $E/checked-in.sha256.txt
  cleanup $BLD/$C8/phase5; exit $rc ;;
closure)
  archive; cd $W; P=Broiler.JS/Broiler.JavaScript.Portable.Compiler/Broiler.JavaScript.Portable.Compiler.csproj; PUB=$BLD/$C8/closure-publish
  rm -rf $PUB
  T publish-portable-compiler dotnet publish $P -c Release -nologo --disable-build-servers -nodeReuse:false -p:UseSharedCompilation=false -o $PUB || { cleanup $PUB; exit 4; }
  T list-references dotnet list $P reference
  T list-packages dotnet list $P package --include-transitive
  cp $L/list-references.stdout.txt $S/dotnet-list-reference.txt; cp $L/list-packages.stdout.txt $S/dotnet-list-package-transitive.txt
  python3 -I - $W/Broiler.JS/Broiler.JavaScript.Portable.Compiler/obj/project.assets.json > $S/assets-closure.tsv <<'PY'
import json, sys
a = json.load(open(sys.argv[1], encoding="utf-8"))
print("kind\tname\tversion\tpath")
for key, lib in sorted(a["libraries"].items()):
    name, version = key.split("/", 1)
    print("\t".join([lib.get("type", "?"), name, version, lib.get("path", "")]))
for tfm, entries in sorted(a["targets"].items()):
    print("# target " + tfm + ": " + str(len(entries)) + " entries")
PY
  ( cd $PUB && find . -type f | sort | xargs -r sha256sum ) > $S/publish-files.sha256.txt
  { echo "# a reading aid, not a reading: lines naming Broiler.JavaScript.Runtime or Broiler.JavaScript.Storage"
    for f in assets-closure.tsv dotnet-list-reference.txt dotnet-list-package-transitive.txt publish-files.sha256.txt; do
      echo "== $f"; grep -n -E "Broiler\.JavaScript\.(Runtime|Storage)" $S/$f; echo "(grep exit $?)"; done; } > $S/runtime-storage-lines.txt
  cleanup $PUB; exit $rc ;;
pack)
  archive; cd $W; rm -rf $FEED; mkdir -p $FEED
  T pack-engine $PWSH -NoProfile -File eng/pack.ps1 -Configuration Release -ProjectSet Engine -Version $CANDV -Output $FEED/engine || rc=11
  T pack-shared $PWSH -NoProfile -File eng/pack.ps1 -Configuration Release -ProjectSet Shared -Output $FEED/shared || rc=12
  T verify-feed-engine $PWSH -NoProfile -File eng/verify-feed.ps1 $FEED/engine || rc=13
  T verify-feed-shared $PWSH -NoProfile -File eng/verify-feed.ps1 $FEED/shared || rc=13
  mkdir -p $FEED/all; cp $FEED/engine/*.nupkg $FEED/shared/*.nupkg $FEED/all/ 2>/dev/null
  ( cd $FEED && find . -type f -name '*.nupkg' -o -type f -name '*.snupkg' | sort | xargs -r sha256sum ) > $S/candidate-feed.sha256.txt
  cleanup; exit $rc ;;
pristine|pcontrol)
  archive; cd $W; [ -d $FEED/engine ] && [ -d $FEED/shared ] || { echo "no feed: take pack first" >&2; cleanup; exit 3; }
  packmark; tmpretake broiler-pristine-consumer-; tmpbefore broiler-pristine-consumer-
  extra=(); [ $STEP = pcontrol ] && extra=(--control-remove Broiler.ECMAScript.Frontend)
  T pristine-consumer "${RUNENV[@]}" python3 -I scripts/packaging/test_pristine_consumer.py --feed $FEED/engine --feed $FEED/shared "${extra[@]}"; rc=$?
  tmpafter broiler-pristine-consumer-
  cp $L/pristine-consumer.stdout.txt $S/consumer.stdout.txt; cp $L/pristine-consumer.stderr.txt $S/consumer.stderr.txt
  echo "$rc" > $S/exit.txt
  cleanup; exit $rc ;;
obsolete)
  archive; cd $W; [ -d $FEED/engine ] || { echo "no feed: take pack first" >&2; cleanup; exit 3; }
  packmark
  T build-obsolete-reader "${BUILD[@]}" --artifacts-path $ART/or docs/evidence/sjl-14-001/tools/obsolete-reader/obsolete-reader.csproj || { cleanup; exit 4; }
  OR=$(find $ART/or/bin -name 'obsolete-reader.dll' -path '*release*' | head -1)
  T obsolete-reader "${RUNENV[@]}" dotnet $OR $FEED/engine/Broiler.JavaScript.Ast.$CANDV.nupkg $FEED/engine/Broiler.JavaScript.Parser.$CANDV.nupkg; rc=$?
  cp $L/obsolete-reader.stdout.txt $S/obsolete-marks.tsv
  sha256sum $FEED/engine/Broiler.JavaScript.Ast.$CANDV.nupkg $FEED/engine/Broiler.JavaScript.Parser.$CANDV.nupkg | sed "s#$FEED/##" > $S/packages-read.sha256.txt
  ( find $ART -name '*.dll' -path '*release*' | sort | xargs -r sha256sum ) > $L/binaries.sha256.txt
  cleanup; exit $rc ;;
api)
  archive; cd $W; H=$S; mkdir -p $H/api; P=$BLD/$C8/apilister; rm -rf $P; mkdir -p $P
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
  T build-api-lister dotnet build $P/api-lister.csproj -c Release -nologo --disable-build-servers -o $P/out || { rm -rf $P; cleanup; exit 13; }
  T run-api-lister "${RUNENV[@]}" dotnet $P/out/api-lister.dll $H/api $(cut -f4 $H/projects.txt)
  rc=$?; rm -rf $P; cleanup; exit $rc ;;
jseal)
  G=$S/jseal; [ -d $G ] && { echo "output exists: $G" >&2; exit 14; }
  [ -d $FEED/all ] || { echo "no feed: take pack first" >&2; exit 3; }
  packmark; tmpretake jseal-package-consumer-
  rm -rf $W; git clone -q --no-hardlinks /home/user/broiler-platform/broiler.jseal $W && git -C $W checkout -q --detach $JSEAL || exit 3
  echo "jseal: $(git -C $W rev-parse HEAD) (scratch clone of the read-only clone)" > $L/archive.txt
  cd $W
  # The base's harness (base/local/scripts/bstep.sh, attempt 3): Broiler.JSeal's eng/package-consumer/README.md, "The
  # checkout's shipping projects must have been restored (built or tested once, in `Release-VM` so the VM provider is
  # included) before a candidate run."
  T jseal-restore "${RUNENV[@]}" dotnet restore Broiler.JSeal.slnx -p:Configuration=Release-VM || { rc=15; cd /; rm -rf $W; exit $rc; }
  # The base's harness: "The candidate directory must therefore contain every package it introduces, at the versions
  # they declare". The external Broiler.* packages are read from the after packages' own nuspecs here (the base named
  # Broiler.Regex, Broiler.Unicode.Algorithms and Broiler.Unicode.Segmentation 0.1.0-preview.6), each copied from the
  # NuGet global packages folder and checked against its recorded SHA-512.
  CD=$FEED/jseal-candidate; rm -rf $CD; mkdir -p $CD; cp $FEED/all/*.nupkg $CD/
  python3 -I - $FEED/all > $S/jseal-external-declared.tsv <<'PY' || { cd /; rm -rf $W; exit 16; }
import re, sys, zipfile, pathlib
import xml.etree.ElementTree as ET
ids, deps = set(), {}
for p in sorted(pathlib.Path(sys.argv[1]).glob("*.nupkg")):
    with zipfile.ZipFile(p) as z:
        n = ET.fromstring(z.read(next(x for x in z.namelist() if x.endswith(".nuspec"))))
    for e in n.iter():
        t = e.tag.rsplit("}", 1)[-1]
        if t == "id" and e.text: ids.add(e.text)
        if t == "dependency" and e.get("id", "").startswith("Broiler."):
            v = re.sub(r"^[\[\(]\s*|\s*,.*$|[\]\)]$", "", e.get("version", ""))
            deps.setdefault((e.get("id"), v), set()).add(p.name)
for (i, v), by in sorted(deps.items()):
    if i not in ids:
        print(i + "\t" + v + "\t" + ",".join(sorted(by)))
PY
  : > $S/jseal-candidate-external.txt
  while IFS=$'\t' read id ver by; do p=$(echo "$id" | tr 'A-Z' 'a-z'); d=/root/.nuget/packages/$p/$ver; f=$d/$p.$ver.nupkg
    [ -f $f ] && [ -f $f.sha512 ] || { echo "not in the NuGet global packages folder: $id $ver" >&2; cd /; rm -rf $W; exit 16; }
    [ "$(openssl dgst -sha512 -binary $f | base64 -w0)" = "$(cat $f.sha512)" ] || { echo "sha512 mismatch: $f" >&2; cd /; rm -rf $W; exit 16; }
    cp $f $CD/; printf '%s\tsha512=%s\tsha256=%s\tfrom=NuGet global packages folder (restored from nuget.org)\tdeclared-by=%s\n' "$(basename $f)" "$(cat $f.sha512)" "$(sha256sum $f | cut -d' ' -f1)" "$by" >> $S/jseal-candidate-external.txt
  done < $S/jseal-external-declared.tsv
  ( cd $CD && sha256sum *.nupkg ) > $S/jseal-candidate-dir.sha256.txt
  # Not in the base's harness: a TRX of each test project, through the VSTest MSBuild properties taken from the
  # environment (the consumer's own commands are unchanged; the base's report kept no passed test by name).
  tmpbefore jseal-package-consumer-
  T jseal-candidate "${RUNENV[@]}" VSTestLogger=trx VSTestResultsDirectory=$S/jseal-trx $PWSH -NoProfile -File eng/test-package-consumer.ps1 -OutputDirectory $G -CandidateFeed $CD -CandidateVersion "Broiler.JavaScript=$CANDV"
  rc=$?; tmpafter jseal-package-consumer-
  # Every test's result retained (part G): the TRX files, by digest, result count, outcomes and configuration (Release or
  # Release-VM, by the test assemblies' code bases); none, or one unreadable, is written to FAILED.
  python3 -I - $S/jseal-trx > $S/jseal-trx.tsv <<'PY'
import collections, hashlib, pathlib, sys
import xml.etree.ElementTree as ET
root = pathlib.Path(sys.argv[1]); n = bad = 0
print("file\tsha256\tresults\toutcomes\tconfiguration\tcode-bases")
for p in (sorted(root.rglob("*.trx")) if root.is_dir() else []):
    n += 1
    try:
        t = ET.parse(p).getroot()
    except ET.ParseError as e:
        bad += 1; print(str(p.relative_to(root)) + "\t" + hashlib.sha256(p.read_bytes()).hexdigest() + "\tunreadable: " + str(e)); continue
    named = lambda tag: [e for e in t.iter() if e.tag.rsplit("}", 1)[-1] == tag]
    results = named("UnitTestResult")
    outcomes = collections.Counter(e.get("outcome", "?") for e in results)
    bases = sorted({e.get("codeBase", "") for e in named("TestMethod")})
    conf = sorted({c for b in bases for c in ("Release", "Release-VM") if "/" + c + "/" in b.replace("\\", "/")})
    print("\t".join([str(p.relative_to(root)), hashlib.sha256(p.read_bytes()).hexdigest(), str(len(results)),
                     ",".join(k + "=" + str(v) for k, v in sorted(outcomes.items())), ",".join(conf) or "?", ",".join(bases)]))
sys.exit(0 if n and not bad else 1)
PY
  [ $? = 0 ] || { echo "no TRX, or an unreadable one, under jseal-trx/: the consumer's tests kept no per-test result there (jseal-trx.tsv)" > $S/jseal-trx-missing.txt
    echo "jseal no-per-test-trx $(date -u +%FT%TZ) (jseal/jseal-trx-missing.txt)" >> $OUT/FAILED; }
  cd /; rm -rf $W; exit $rc ;;
html)
  G=$S/htmlbridge; mkdir -p $G; [ -d $FEED/all ] || { echo "no feed: take pack first" >&2; exit 3; }
  packmark; rm -rf $BLD/$C8/html-packages
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
  find $BLD/$C8/html-packages -maxdepth 2 -mindepth 2 -ipath '*broiler.ecmascript*' | sed "s#$BLD/$C8/html-packages/##" | sort > $G/resolved-broiler-ecmascript.txt
  cd /; rm -rf $W $BLD/$C8/html-packages; exit $rc ;;
fuzz|fcontrol)
  archive; cd $W; F=Broiler.JS/Broiler.JavaScript.Parser/Bridge/BridgeTreeBuilder.Expressions.cs; CLEAN=$BLD/$C8/fcontrol-clean
  if [ $STEP = fcontrol ]; then
    archive $CLEAN
    # The control's edit: the bridge throws on one node kind the seeds produce, a template literal (README of tools/fuzz).
    python3 -I - $W/$F <<'PY' || { cleanup $CLEAN; exit 3; }
import sys
p = sys.argv[1]; s = open(p, "rb").read()
old = (b"    private AstTemplateExpression Template(TemplateLiteral template)\n    {\n"
       b"        var parts = TemplateParts(template);\n")
new = (b"    private AstTemplateExpression Template(TemplateLiteral template)\n    {\n"
       b"        if (template is not null) throw new System.InvalidOperationException(\"part J control: the bridge throws on a template literal\");\n"
       b"        var parts = TemplateParts(template);\n")
assert s.count(old) == 1, "the method's head is not found once"
open(p, "wb").write(s.replace(old, new))
PY
    ( cd $BLD/$C8 && diff -u --label a/$F --label b/$F fcontrol-clean/$F fcontrol/$F ) > $S/control.patch
  fi
  T build-fuzz "${BUILD[@]}" --artifacts-path $ART/fz docs/evidence/sjl-14-001/tools/fuzz/fuzz.csproj || { cleanup $CLEAN; exit 4; }
  FZ=$(find $ART/fz/bin -name 'sjl-14-fuzz.dll' -path '*release*' | head -1)
  ( find $ART -name '*.dll' -path '*release*' | sort | xargs -r sha256sum ) > $L/binaries.sha256.txt
  T fuzz-session "${RUNENV[@]}" dotnet $FZ --seeds docs/evidence/sjl-14-001/inputs/fuzz-seeds.txt --test262 $SUITE --out $S/session; rc=$?
  if [ $STEP = fcontrol ]; then
    git -C $REPO show $SHA:$F > $W/$F
    { echo "revert: the file written again from the after commit's blob (git show $SHA:$F)"
      echo "sha256 of $F after the revert: $(sha256sum < $W/$F | cut -d' ' -f1)"
      echo "sha256 of $F at the after commit: $(git -C $REPO show $SHA:$F | sha256sum | cut -d' ' -f1)"
      echo "diff -r -q -x bin -x obj against a clean archive of the after commit:"; ( cd $BLD/$C8 && diff -r -q -x bin -x obj fcontrol-clean fcontrol ); echo "diff exit $?"; } > $S/revert-check.txt
  fi
  cleanup $CLEAN; exit $rc ;;
delsearch)
  archive; W2=$BLD/$C8/delsearch-control; archive $W2
  T search "${RUNENV[@]}" python3 -I $W/docs/evidence/sjl-14-001/tools/deletion-search/search.py --tree $W --commit $SHA --repository $REPO --out $S/search.txt || rc=18
  T control "${RUNENV[@]}" python3 -I $W/docs/evidence/sjl-14-001/tools/deletion-search/search.py --control --tree $W2 --commit $SHA --repository $REPO --out $S/control.txt || rc=19
  { echo "the control's copy after its own revert, against the clean tree the search read:"; ( cd $BLD/$C8 && diff -r -q delsearch delsearch-control ); echo "diff exit $?"; } > $S/revert-check.txt
  cleanup $W2; exit $rc ;;
*) echo "unknown step $STEP" >&2; exit 2 ;;
esac
