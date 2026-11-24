#!/usr/bin/env bash
# compare-a.sh: part A's comparison, the rule's, from the retained files and the two evidence zips (each checked against
# the digest the bundle records for it), into after/part-a/comparison/: comparison.json, comparison.log,
# compare-checks.log (the script on the after run against itself, and a negative control), completeness.log,
# zone-records.log and named-rows.log, in the form of bundle sjl-11-001's after/part-a/comparison/.
set -uo pipefail
W=/tmp/claude-0/-home-user/0a6f6b61-1da7-5ce6-9020-88003835d4eb/scratchpad/sjl-rest/js-w2read
R=/home/user/js-w2read; S=$W/cmp; B=docs/evidence/sjl-14-001; C=$B/after/part-a/comparison
rm -rf $S; mkdir -p $S; cd $R
[ "$(sha256sum $W/parta/base/test262-evidence.zip | cut -d' ' -f1)" = 12163b9df6e680ddfbd50b3f9356d9bae2f2e2c1cb417ace2045ca7223c647c1 ] || exit 3
[ "$(sha256sum $W/parta/after/test262-evidence.zip | cut -d' ' -f1)" = 84f74d007d7376eccd96c14a9831c430095cf54bf3406b07544d94ddd3ab8c8e ] || exit 3
unzip -p $W/parta/base/test262-evidence.zip test262-results.json.gz > $S/base-test262-results.json.gz
unzip -p $W/parta/after/test262-evidence.zip test262-results.json.gz > $S/after-test262-results.json.gz
[ "$(sha256sum $S/base-test262-results.json.gz | cut -d' ' -f1)" = 20b632f9ebeb49ff926201ca436a86a3b22747cfd3d5cc69bc332610e99480e1 ] || exit 3
gunzip -c $S/base-test262-results.json.gz > $S/base-test262-results.json
gunzip -c $S/after-test262-results.json.gz > $S/after-test262-results.json
python3 -I - $S/after-test262-results.json $S/test262-negative-control.json <<'PY'
import json, sys
d = json.load(open(sys.argv[1], encoding="utf-8"))
k = [r for r in d["results"] if r["path"] == "test/annexB/built-ins/Date/prototype/getYear/B.2.4.js" and r["variant"] == "original"]
assert len(k) == 1 and k[0]["status"] == "passed"
k[0]["status"] = "failed"
json.dump(d, open(sys.argv[2], "w", encoding="utf-8"))
PY
sub() { sed -e "s#$S#<scratch>#g" -e "s#$R#<repo>#g"; }
run() { local log=$1; shift; local t; t=$(date -u +%FT%TZ)
  echo "\$ python3 -I scripts/compliance/compare_test262_paths.py $*" | sub >> $log
  python3 -I scripts/compliance/compare_test262_paths.py "$@" > $S/out.txt 2> $S/err.txt; local e=$?
  echo "# cwd <repo>; started $t; exit $e" >> $log; sub < $S/out.txt >> $log; sub < $S/err.txt >> $log; }
: > $S/comparison.log; : > $S/compare-checks.log
run $S/comparison.log --base $S/base-test262-results.json --after $S/after-test262-results.json --output $S/comparison.json \
  --base-rerun $B/base/part-a/rerun-timeouts.json --after-rerun $B/after/part-a/rerun-timeouts.json --classifications $C/classifications.json
run $S/compare-checks.log --base $S/after-test262-results.json --after $S/after-test262-results.json
echo "# negative control: test/annexB/built-ins/Date/prototype/getYear/B.2.4.js [original] turned from passed to failed in a copy" >> $S/compare-checks.log
run $S/compare-checks.log --base $S/after-test262-results.json --after $S/test262-negative-control.json
python3 -I - $S/comparison.json > $S/sub.json <<'PY'
import json, sys
t = open(sys.argv[1], encoding="utf-8").read()
print(t, end="")
PY
sub < $S/comparison.json > $S/comparison.sub.json; mv $S/comparison.sub.json $S/comparison.json
cp $S/comparison.json $S/comparison.log $S/compare-checks.log $R/$C/
python3 -I - $R $S <<'PY'
import gzip, json, os, re, sys
R, S = sys.argv[1:]; B = "docs/evidence/sjl-14-001"; OUT = os.path.join(R, B, "after/part-a/comparison")
L = ["# Part A's completeness conditions for each run, read from the files the workflow wrote:",
     "# every expected shard reported, no configuration failure, one suite revision, one shell, every",
     "# shard in the zone, includeNegative true. The merged results' summary of totals is not copied:",
     "# totals decide nothing. Each run's test262-results.json.gz is retained by digest only and is read",
     "# from its evidence zip, whose SHA-256 is checked first (imported.txt).", ""]
Z = []
for side in ("base", "after"):
    d = f"{B}/{side}/part-a/"; p = os.path.join(R, d)
    run = open(p + "run.txt", encoding="utf-8").read()
    field = lambda name: re.search(r"^" + re.escape(name) + r": (.*)$", run, re.M).group(1)
    m = json.load(gzip.open(os.path.join(S, side + "-test262-results.json.gz")))
    rc = m["runConfiguration"]
    rr = json.load(open(p + "rerun-timeouts.json", encoding="utf-8"))
    st = []
    for f in sorted(os.listdir(p + "shards")):
        if f.endswith("-status.json"):
            s = json.load(open(p + "shards/" + f)); st.append((s["phase"], s["exitCode"]))
    shell = "; ".join(l for l in open(p + "shell-hashes.log", encoding="utf-8").read().splitlines() if not l.startswith("#") and "shell-hashes/" not in l)
    zone = [l for l in open(p + "host-zone.log", encoding="utf-8").read().splitlines() if l.startswith("expected:") or l.startswith("shards:")]
    L += [f"## {side}: {d}",
          f"tested commit (run.txt): {field('tested commit')}",
          f"workflow run (run.txt): {field('workflow run')}",
          f"runner (run.txt): {field('runner')}; dotnetVersion {field('dotnetVersion')}",
          f"expected shards: {m['expectedShards']}; reported: {m['reportedShards']}; incomplete: {m['incompleteShards']}; retried: {m['retriedShards']}; status files: {len(st)}; their (phase, exit code): {sorted(set(st))}",
          f"configuration failures: {m['configurationFailures']}",
          f"suite revisions (suiteRefs): {m['suiteRefs']}",
          f"includeNegative: run.txt {field('includeNegative')}; merged results {rc['includeNegative']}; re-run {rr['includeNegative']}",
          f"selection: {rc['selectionMode']}; workers {rc['maxWorkers']}; timeout {rc['testTimeoutSeconds']} s; memory limit {rc['memoryLimitMb']}; minifier {rc['minifier']} {rc['minifierVersion']}",
          f"shell: {shell}",
          f"zone: {'; '.join(zone)}", ""]
    Z.append((side, d, zone, open(p + "host-zone.log", "rb").read()))
open(os.path.join(OUT, "completeness.log"), "w", encoding="utf-8", newline="\n").write("\n".join(L))
zl = []
for side, d, zone, _ in Z:
    zl.append(f"## {side}: {d}host-zone.log"); zl += zone
zl.append(f"# the two runs' expected zone lines read alike, line for line: {Z[0][2] == Z[1][2]}")
zl.append(f"# the two host-zone.log files are one file byte for byte: {Z[0][3] == Z[1][3]}")
open(os.path.join(OUT, "zone-records.log"), "w", encoding="utf-8", newline="\n").write("\n".join(zl) + "\n")
# The rows the bundle names for part A: decision B8's group 6, and part C's groups 5 and 7 (the revision of 2026-11-23,
# "what part A's after comparison reads for the paths of groups 5, 6 and 7 is read there as written").
named = [("test/staging/sm/regress/regress-610026.js", "group 6, decision B8"),
         ("test/language/statements/for-of/head-await-using-bound-names-in-stmt.js", "group 5, rev-for-of at part C only"),
         ("test/language/expressions/dynamic-import/syntax/valid/new-covered-expression-is-valid.js", "group 7, rev-minifier at part C only")]
recs = {}
for side in ("base", "after"):
    for x in json.load(gzip.open(os.path.join(S, side + "-test262-results.json.gz")))["results"]:
        if x["path"] in dict(named): recs.setdefault((x["path"], x["variant"]), {})[side] = x
nl = ["# Part A's rows the bundle names, read from each run's test262-results.json.gz (by digest; read from its zip): path,",
      "# variant, the status at the base and after, and the record's fields that say why (reason, skipKind,",
      "# referenceCrossCheck, engineFailure or stderr, up to 300 characters, durationMs). Neither run re-ran any of them",
      "# (timed-out-paths.txt names another path in both).", ""]
keep = ("status", "reason", "skipKind", "referenceCrossCheck", "engineFailure", "stderr", "durationMs", "hostMode", "negative")
for p, why in named:
    nl.append(f"## {p} ({why})")
    for (pp, v), s in sorted(recs.items()):
        if pp != p: continue
        for side in ("base", "after"):
            x = s.get(side, {})
            nl.append(f"{v} {side}: " + "; ".join(f"{k}={str(x[k]).replace(chr(10), ' ')[:300]}" for k in keep if k in x))
    nl.append("")
open(os.path.join(OUT, "named-rows.log"), "w", encoding="utf-8", newline="\n").write("\n".join(nl))
PY
grep -c "$S\|$R" $R/$C/comparison.json $R/$C/comparison.log $R/$C/compare-checks.log
