"""patch_astep.py <in> <out>: the review fixes of 2026-11-23 to astep.sh, each replacement found exactly once."""
import sys

src, dst = sys.argv[1], sys.argv[2]
s = open(src, encoding="utf-8").read()


def sub(old, new):
    global s
    assert s.count(old) == 1, ("not found once", old[:80])
    s = s.replace(old, new)


# header: the two new steps, and the fixes
sub("""#   dcontrol   part D's control: compare_parse_corpus.py control over the base report (a copy with one outcome flipped
#              and one message changed, which the comparison must report as two differences)
""", """#   dcontrol   part D's control: compare_parse_corpus.py control over the base report (a copy with one outcome flipped
#              and one message changed, which the comparison must report as two differences)
#   dcodes     part D, a reading aid (added 2026-11-23, after review): the library's diagnostic code and message id for
#              every input variant P accepts at the base and refuses at the after commit (read from the two reports),
#              by part C's corrected bridge-codes aid as the after commit carries it, with one change for the after
#              commit's three-parameter SharedSyntaxBridge.Request (dcodes/aid.patch); rev-code and class E read it
#   dtext      part D, the dump text (added 2026-11-23, after review): the same tool and build as partd, with --dump
#              --order manifest --text <build folder>/text/dumps-p.txt.gz; the text is kept in the build folder, by
#              digest here, and its report's rows are checked against partd's parse-corpus-d1; rev-eof, rev-bracket and
#              S read it
""")
sub("""#              the NuGet global packages folder, SHA-512 checked; and a TRX per suite (VSTestLogger), not taken at the base
""", """#              the NuGet global packages folder, SHA-512 checked; and a TRX per suite (VSTestLogger), not taken at the base,
#              listed after the run by digest, test count and configuration (jseal-trx.tsv); none is written to FAILED
""")
sub("""# The bundle's own tools are used as the after commit carries them.
""", """# The bundle's own tools are used as the after commit carries them.
# Review fixes of 2026-11-23 (the text as started is fix/astep.sh.before-fix): a step's build-output folder art-<step>
# is cleared with its tree at its start (as the base's archive() did); the consumers' temporary folders are removed by
# what this step made (tmpbefore/tmpafter/tmpretake), not by a glob over /tmp; html's package folder is cleared before
# its restore; the steps reading pack's feed record pack's marker; the steps dcodes and dtext.
""")

# archive() clears the step's build output with its tree
sub("""archive() { local d=${1:-$W}; rm -rf $d; mkdir -p $d;""",
    """archive() { local d=${1:-$W}; rm -rf $d; [ "$d" = "$W" ] && rm -rf $ART; mkdir -p $d;""")

# helpers
sub("""suitedigest() {""", """# The consumers' own temporary folders, /tmp/<prefix><id>: each consumer removes its folder when it ends. A folder this
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
suitedigest() {""")

# the new steps, after dcontrol
sub("""  sha256sum $BASEANS/part-d/parse-corpus-c.tsv.gz | sed "s#$SCR/##" > $S/base-report.sha256.txt
  cleanup; exit $rc ;;
""", """  sha256sum $BASEANS/part-d/parse-corpus-c.tsv.gz | sed "s#$SCR/##" > $S/base-report.sha256.txt
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
            x = line.rstrip("\\n").split("\\t")
            if header is None: header = x; continue
            if x[2] == "P": out[(x[0], x[1])] = x[3]
    return out
base, after = p_rows(sys.argv[1]), p_rows(sys.argv[2])
assert base.keys() == after.keys(), "the two reports name different input variants"
for key in base:
    if base[key] == "accepted" and after[key] != "accepted":
        print(key[0] + "\\t" + key[1])
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
""")

# pristine / pcontrol: no glob over /tmp
sub("""  archive; cd $W; [ -d $FEED/engine ] && [ -d $FEED/shared ] || { echo "no feed: take pack first" >&2; cleanup; exit 3; }
  extra=(); [ $STEP = pcontrol ] && extra=(--control-remove Broiler.ECMAScript.Frontend)
  T pristine-consumer "${RUNENV[@]}" python3 -I scripts/packaging/test_pristine_consumer.py --feed $FEED/engine --feed $FEED/shared "${extra[@]}"; rc=$?
""", """  archive; cd $W; [ -d $FEED/engine ] && [ -d $FEED/shared ] || { echo "no feed: take pack first" >&2; cleanup; exit 3; }
  packmark; tmpretake broiler-pristine-consumer-; tmpbefore broiler-pristine-consumer-
  extra=(); [ $STEP = pcontrol ] && extra=(--control-remove Broiler.ECMAScript.Frontend)
  T pristine-consumer "${RUNENV[@]}" python3 -I scripts/packaging/test_pristine_consumer.py --feed $FEED/engine --feed $FEED/shared "${extra[@]}"; rc=$?
  tmpafter broiler-pristine-consumer-
""")
sub("""  cleanup; rm -rf /tmp/broiler-pristine-consumer-*; exit $rc ;;""", """  cleanup; exit $rc ;;""")

# obsolete: pack's marker
sub("""  archive; cd $W; [ -d $FEED/engine ] || { echo "no feed: take pack first" >&2; cleanup; exit 3; }
""", """  archive; cd $W; [ -d $FEED/engine ] || { echo "no feed: take pack first" >&2; cleanup; exit 3; }
  packmark
""")

# jseal: pack's marker, its own temporary folder, the TRX listing
sub("""  [ -d $FEED/all ] || { echo "no feed: take pack first" >&2; exit 3; }
  rm -rf $W; git clone""", """  [ -d $FEED/all ] || { echo "no feed: take pack first" >&2; exit 3; }
  packmark; tmpretake jseal-package-consumer-
  rm -rf $W; git clone""")
sub("""  T jseal-candidate "${RUNENV[@]}" VSTestLogger=trx VSTestResultsDirectory=$S/jseal-trx $PWSH -NoProfile -File eng/test-package-consumer.ps1 -OutputDirectory $G -CandidateFeed $CD -CandidateVersion "Broiler.JavaScript=$CANDV"
  rc=$?; cd /; rm -rf $W /tmp/jseal-package-consumer-*; exit $rc ;;""",
    """  tmpbefore jseal-package-consumer-
  T jseal-candidate "${RUNENV[@]}" VSTestLogger=trx VSTestResultsDirectory=$S/jseal-trx $PWSH -NoProfile -File eng/test-package-consumer.ps1 -OutputDirectory $G -CandidateFeed $CD -CandidateVersion "Broiler.JavaScript=$CANDV"
  rc=$?; tmpafter jseal-package-consumer-
  # Every test's result retained (part G): the TRX files, by digest, result count, outcomes and configuration (Release or
  # Release-VM, by the test assemblies' code bases); none, or one unreadable, is written to FAILED.
  python3 -I - $S/jseal-trx > $S/jseal-trx.tsv <<'PY'
import collections, hashlib, pathlib, sys
import xml.etree.ElementTree as ET
root = pathlib.Path(sys.argv[1]); n = bad = 0
print("file\\tsha256\\tresults\\toutcomes\\tconfiguration\\tcode-bases")
for p in (sorted(root.rglob("*.trx")) if root.is_dir() else []):
    n += 1
    try:
        t = ET.parse(p).getroot()
    except ET.ParseError as e:
        bad += 1; print(str(p.relative_to(root)) + "\\t" + hashlib.sha256(p.read_bytes()).hexdigest() + "\\tunreadable: " + str(e)); continue
    named = lambda tag: [e for e in t.iter() if e.tag.rsplit("}", 1)[-1] == tag]
    results = named("UnitTestResult")
    outcomes = collections.Counter(e.get("outcome", "?") for e in results)
    bases = sorted({e.get("codeBase", "") for e in named("TestMethod")})
    conf = sorted({c for b in bases for c in ("Release", "Release-VM") if "/" + c + "/" in b.replace("\\\\", "/")})
    print("\\t".join([str(p.relative_to(root)), hashlib.sha256(p.read_bytes()).hexdigest(), str(len(results)),
                     ",".join(k + "=" + str(v) for k, v in sorted(outcomes.items())), ",".join(conf) or "?", ",".join(bases)]))
sys.exit(0 if n and not bad else 1)
PY
  [ $? = 0 ] || { echo "no TRX, or an unreadable one, under jseal-trx/: the consumer's tests kept no per-test result there (jseal-trx.tsv)" > $S/jseal-trx-missing.txt
    echo "jseal no-per-test-trx $(date -u +%FT%TZ) (jseal/jseal-trx-missing.txt)" >> $OUT/FAILED; }
  cd /; rm -rf $W; exit $rc ;;""")

# html: pack's marker, a fresh package folder
sub("""  G=$S/htmlbridge; mkdir -p $G; [ -d $FEED/all ] || { echo "no feed: take pack first" >&2; exit 3; }
""", """  G=$S/htmlbridge; mkdir -p $G; [ -d $FEED/all ] || { echo "no feed: take pack first" >&2; exit 3; }
  packmark; rm -rf $BLD/$C8/html-packages
""")

open(dst, "w", encoding="utf-8").write(s)
