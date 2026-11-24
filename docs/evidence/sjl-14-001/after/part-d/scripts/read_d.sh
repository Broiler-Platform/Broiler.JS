#!/usr/bin/env bash
# read_d.sh <repo root> <work dir> <base text.txt.gz> <after text.txt.gz> <out dir>: part D's comparison and classes
# (bundle sjl-14-001, decision rule part D as revised on 2026-11-23), from the retained reports alone: the base
# commit's base/local/part-d/ and the after commit's after/local/partd/, the dumps' text of both commits (outside the
# repository, by digest) and the after commit's reading aid after/local/dcodes/bridge-codes.tsv. Written 2026-11-24.
# It reads only; it builds and runs nothing of the engine. Writes into <out dir>:
#   acceptance.json.gz, acceptance.log scripts/compliance/compare_parse_corpus.py acceptance, the comparison the rule
#                                     names, with no classification file (it decides no class of this rule's)
#   control.log                        the control the after run took (after/local/dcontrol/), read again
#   parser/, pass/                     part C's classifier base/part-c/scripts/classify_c.py, unchanged, over
#                                     make_inputs.py's inputs: P/B read as P at the base against P at the after commit,
#                                     K/K-on as K at the base against K at the after commit; with the after commit's
#                                     codes and the word revised-2026-11-23; parser/ on the parser trees' dumps, pass/
#                                     on the dumps after the compiler's for-loop pass (its outcome rows are parser/'s)
#   cross-check.txt                    every difference acceptance.json.gz lists against parser/differences.tsv.gz, and
#                                     the dump hash differences of both trees against the dump rows of parser/ and pass/
#   pass/unclassified-lines.txt        the lines of the pass trees' unclassified input variants, by kind and reading
#   work.sha256.txt, texts.sha256.txt  the digests of make_inputs.py's and spancheck.py's outputs, kept outside the
#                                     repository, and of the two commits' dump texts the script read
# Run twice, it writes the same bytes (gzip -n, no clock in any output).
set -eu
ROOT=$1; WORK=$2; BT=$3; AT=$4; OUT=$5
HERE=$(cd "$(dirname "$0")" && pwd)
B=$ROOT/docs/evidence/sjl-14-001; BL=$B/base/local; AL=$B/after/local; M=$ROOT/conformance/parse/corpus.manifest
SUITE=/home/user/js-w2base-build/suite/test262-ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e
mkdir -p "$WORK" "$OUT/parser" "$OUT/pass"
cd "$ROOT"
set +e
python3 -I scripts/compliance/compare_parse_corpus.py acceptance --base $BL/part-d/parse-corpus-c.tsv.gz --after $AL/partd/parse-corpus-c.tsv.gz \
  --manifest $M --output "$WORK/acceptance.json" > "$WORK/acceptance.out" 2> "$WORK/acceptance.err"; e=$?
set -e
{ echo "\$ python3 -I scripts/compliance/compare_parse_corpus.py acceptance --base docs/evidence/sjl-14-001/base/local/part-d/parse-corpus-c.tsv.gz --after docs/evidence/sjl-14-001/after/local/partd/parse-corpus-c.tsv.gz --manifest conformance/parse/corpus.manifest --output acceptance.json"
  echo "# exit $e (1: differences no classification file admits; this rule's classes are read in parser/ and pass/)"
  tail -n 5 "$WORK/acceptance.out"; cat "$WORK/acceptance.err"; } | sed "s#$ROOT/##g; s#$WORK/##g" > "$OUT/acceptance.log"
sed "s#$ROOT/##g; s#$WORK/##g" "$WORK/acceptance.json" | gzip -n -9 > "$OUT/acceptance.json.gz"
python3 -I - $AL/dcontrol/control.json > "$OUT/control.log" <<'PY'
import json, sys
c = json.load(open(sys.argv[1], encoding="utf-8"))
print("# part D's control, as the after run took it (after/local/dcontrol/control.json): the copy of the base report with")
print("# one outcome flipped and one message changed, compared with the base report by compare_parse_corpus.py control")
for k in sorted(c):
    if k != "comparison": print(k + ": " + json.dumps(c[k], sort_keys=True)[:400])
cmp = c.get("comparison", {})
for k in sorted(cmp):
    v = cmp[k]
    print("comparison." + k + ": " + (json.dumps(v, sort_keys=True)[:600]))
PY
python3 -I "$HERE/make_inputs.py" $BL/part-d/parse-corpus-c.tsv.gz $BL/part-d/parse-corpus-d1.tsv.gz $AL/partd/parse-corpus-c.tsv.gz \
  $AL/partd/parse-corpus-d1.tsv.gz "$BT" "$AT" $M "$WORK" > "$WORK/make_inputs.out"
for t in parser pass; do
  python3 -I $B/tools/side-by-side/spancheck.py --manifest $M --test262 $SUITE "$WORK/$t.txt.gz" "$WORK/$t-spans.tsv" > "$OUT/$t/spancheck-counts.txt"
  python3 -I $B/base/part-c/scripts/classify_c.py "$ROOT" "$WORK/$t.tsv.gz" "$WORK/$t-spans.tsv" "$WORK/$t.txt.gz" \
    $B/base/part-c/scripts/readings.tsv "$OUT/$t" $AL/dcodes/bridge-codes.tsv revised-2026-11-23 > "$OUT/$t/classify.out"
done
python3 -I "$HERE/cross_check.py" "$OUT" $BL/part-d/parse-corpus-d1.tsv.gz $AL/partd/parse-corpus-d1.tsv.gz > "$OUT/cross-check.txt"
python3 -I "$HERE/pass_lines.py" "$OUT" "$WORK/pass-spans.tsv" > "$OUT/pass/unclassified-lines.txt"
( cd "$WORK" && sha256sum parser.tsv.gz parser.txt.gz parser-spans.tsv pass.tsv.gz pass.txt.gz pass-spans.tsv ) > "$OUT/work.sha256.txt"
{ sha256sum "$BT" "$AT"; } | sed "s#  .*/js-w2read-build/#  /home/user/js-w2read-build/#" > "$OUT/texts.sha256.txt"
