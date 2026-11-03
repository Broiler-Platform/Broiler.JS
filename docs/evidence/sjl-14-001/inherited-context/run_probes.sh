#!/bin/bash
# run_probes.sh - SJL-14.1 task 1.1 (decision D44): the inherited-context probes, run on one engine.
#
# Usage: [PROGRAM=ic-values-in-eval.js] bash run_probes.sh js <BroilerJS.dll> <out dir>
#        [PROGRAM=ic-values-in-eval.js] bash run_probes.sh vm <Conformance.dll> <out dir> <test262 harness root>
# PROGRAM names the probe program (default ic-values.js, the direct form).
#
# Each engine is built beforehand in Release, with --disable-build-servers, from a clean `git archive` of its
# repository's default branch, unpacked in scratch (never in a checkout). The program each engine runs is
# prelude.js + $PROGRAM + epilogue.js (SJL-13.1's value-probe form):
#   - Broiler.JS: the shell BroilerJS, `--script-host <program>`; the answers are the lines it prints;
#   - Broiler.VM: the conformance root's --test262 mode over a root holding the pinned test262's harness/ and
#     features.txt, the program as a raw test (front matter `flags: [raw]`, then `var __vmTest = true;`); the answers
#     are the lines between OUT<< and >>OUT of the uncaught error it reports.
# Every run is under TZ=UTC with LANG, LANGUAGE, every LC_* and every BROILER_* variable unset, and under timeout.
# The output directory receives program.js (what ran), transcript.txt (the command, its output, its exit code) and
# answers.tsv (id, answer).
set -u
ENGINE=${1:?engine}; DLL=${2:?dll}; OUT=${3:?out}
HERE=$(cd "$(dirname "$0")" && pwd)
PROGRAM=${PROGRAM:-ic-values.js}
export PATH=/root/.dotnet:$PATH DOTNET_ROOT=/root/.dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
mkdir -p "$OUT"
clean() {
  local unset=() v
  for v in $(compgen -e); do
    case $v in LANG|LANGUAGE|LC_*|BROILER_*) unset+=(-u "$v") ;; esac
  done
  env ${unset[@]+"${unset[@]}"} TZ=UTC "$@"
}
case $ENGINE in
  js)
    cat "$HERE/prelude.js" "$HERE/$PROGRAM" "$HERE/epilogue.js" > "$OUT/program.js"
    { printf '# command (in <out>): dotnet <BroilerJS.dll> --script-host program.js\n'
      ( cd "$OUT" && clean timeout 900 dotnet "$DLL" --script-host program.js 2>&1 ); printf '# exit=%s\n' "$?"; } > "$OUT/transcript.txt"
    ;;
  vm)
    ROOT=${4:?test262 root}
    mkdir -p "$ROOT/test/sjl14ic"
    { printf '/*---\ndescription: SJL-14.1 inherited-context probes\nflags: [raw]\n---*/\nvar __vmTest = true;\n'
      cat "$HERE/prelude.js" "$HERE/$PROGRAM" "$HERE/epilogue.js"; } > "$ROOT/test/sjl14ic/$PROGRAM"
    cp "$ROOT/test/sjl14ic/$PROGRAM" "$OUT/program.js"
    { printf '# command (in <root>/..): dotnet <Conformance.dll> --test262 <root> --test test/sjl14ic/%s --fuel 200000000000 --wall 600000\n' "$PROGRAM"
      ( cd "$(dirname "$ROOT")" && clean timeout 900 dotnet "$DLL" --test262 "$(basename "$ROOT")" --test test/sjl14ic/$PROGRAM --fuel 200000000000 --wall 600000 2>&1 ); printf '# exit=%s\n' "$?"; } > "$OUT/transcript.txt"
    ;;
  *) echo "engine js or vm" >&2; exit 2 ;;
esac
python3 -I - "$OUT/transcript.txt" "$HERE/probes.tsv" > "$OUT/answers.tsv" <<'PY'
import sys, re
text = open(sys.argv[1], encoding='utf-8', errors='replace').read()
m = re.search(r'OUT<<(.*?)>>OUT', text, re.S)
body = m.group(1) if m else text
ids = [l.split('\t')[0] for l in open(sys.argv[2], encoding='utf-8').read().split('\n')[1:] if l]
got = {}
for line in body.split('\n'):
    line = line.replace('\\n', '\n') if False else line
    mm = re.match(r'(\S+) => (.*)$', line)
    if mm and mm.group(1) in ids and mm.group(1) not in got:
        got[mm.group(1)] = mm.group(2)
print('id\tanswer')
for i in ids:
    print(i + '\t' + got.get(i, 'NO ANSWER'))
PY
echo "answers: $(grep -vc 'NO ANSWER' "$OUT/answers.tsv") lines (header included)"
