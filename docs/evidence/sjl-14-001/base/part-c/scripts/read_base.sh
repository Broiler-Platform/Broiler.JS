#!/usr/bin/env bash
# read_base.sh <repo root> <run dir> <text dir> <out dir>: part C's readings and classes at the base commit (bundle
# sjl-14-001), from the retained run folder <run dir> (the chain's base/ folder: part-c-tool/, tests-*/, t262-*/) and the
# tool's --text run <text dir> (dumps-p-b.txt.gz, spans.tsv, side-by-side-p-b-only.tsv.gz, by digest in
# part-c-tool/not-retained.sha256.txt). Writes into <out dir> what the bundle's base/part-c/ holds as generated:
#   counts.txt, messages-p-b.tsv.gz, messages-k.tsv.gz, text-run-identity.txt   compare.py, as at W1
#   differences.tsv.gz, span-readings.tsv.gz, unclassified.tsv, classes.txt      scripts/classify_c.py
#   tests-and-test262/{counts.txt,tests-*.tsv.gz,tests-differ.tsv,t262-*.tsv.gz,t262-differ.tsv.gz}
#                                                                                side-by-side/w1's read_local.py, unchanged
#   tests-and-test262/local-differences.tsv, tests-and-test262/classes.txt     scripts/classify_local.py
# It reads only; it builds and runs nothing of the engine. Run twice, it writes the same bytes (gzip -n, no clock in
# any output). Written 2026-11-23. A fifth argument, the corrected bridge-codes reading aid's output, is passed to
# classify_c.py, which then reads class E by the library's code (added 2026-11-23, records part 4).
set -eu
ROOT=$1; RUN=$2; TEXT=$3; OUT=$4; CODES=${5:-}
HERE=$(cd "$(dirname "$0")" && pwd)
TOOL=$ROOT/docs/evidence/sjl-14-001/tools/side-by-side
mkdir -p "$OUT/tests-and-test262"
cd "$RUN/part-c-tool"
{ echo '== P B'; python3 -I $TOOL/compare.py side-by-side.tsv.gz P B
  echo '== K K-on'; python3 -I $TOOL/compare.py side-by-side.tsv.gz K K-on
  echo '== P K'; python3 -I $TOOL/compare.py side-by-side.tsv.gz P K; } > "$OUT/counts.txt"
python3 -I $TOOL/compare.py --messages side-by-side.tsv.gz P B "$OUT/messages-p-b.tsv" > /dev/null
python3 -I $TOOL/compare.py --messages side-by-side.tsv.gz K K-on "$OUT/messages-k.tsv" > /dev/null
gzip -n -9 -f "$OUT/messages-p-b.tsv" "$OUT/messages-k.tsv"
python3 -I $TOOL/compare.py --same side-by-side.tsv.gz "$TEXT/side-by-side-p-b-only.tsv.gz" P,B > "$OUT/text-run-identity.txt"
python3 -I "$HERE/classify_c.py" "$ROOT" side-by-side.tsv.gz "$TEXT/spans.tsv" "$TEXT/dumps-p-b.txt.gz" "$HERE/readings.tsv" "$OUT" ${CODES:+"$CODES"} > /dev/null
python3 -I $ROOT/docs/evidence/sjl-14-001/side-by-side/w1/tests-and-test262/scripts/read_local.py "$RUN" "$ROOT/docs/evidence/sjl-14-001/corrections" "$OUT/tests-and-test262" > /dev/null
python3 -I "$HERE/classify_local.py" "$ROOT" "$OUT/tests-and-test262" "$RUN" "$OUT/tests-and-test262" > "$OUT/tests-and-test262/classes.txt"
