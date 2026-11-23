#!/usr/bin/env python3
"""Part C's classes over its local runs at the base commit (bundle sjl-14-001, decision rule part C): every test whose
result differs with the switch off and on, and every test262 path and variant whose verdict differs.

    classify_local.py <repo root> <reading dir> <run dir> <out dir>

<reading dir> holds read_local.py's tests-differ.tsv and t262-differ.tsv.gz for the run; <run dir> the run folder
(tests-on/trx/, for a failing test's whole message; t262-on/test262-results.json and rerun-timeouts.json, for each
case's record); <repo root> a checkout whose
corrections/ are the base commit's. The classes, as the rule reads them for these runs, with no row added:
  C1  a test262 case failing off and passing on whose path corrections/k-paths.tsv lists for a row of K1 to K10
  C3  a test or case passing off and failing on, refused for nesting past the library's bound ("nested too deeply" or
      "syntax tree is too deep" "to parse (maximum depth N)" in its failure), with the depth
  T   a negative test262 case failing off and passing on that test262-paths.tsv explains by T for the path
Anything else is unclassified, with both results and the case's record. Writes local-differences.tsv and prints the
counts. Run with python3 -I.
"""
import collections, gzip, json, os, re, sys
import xml.etree.ElementTree as ET

root, rd, run, out = sys.argv[1:5]
corr = os.path.join(root, "docs", "evidence", "sjl-14-001", "corrections")


def table(path):
    opener = gzip.open if path.endswith(".gz") else open
    cols, rows = None, []
    with opener(path, "rt", encoding="utf-8", newline="\n") as h:
        for line in h:
            if line.startswith("#"):
                continue
            f = line.rstrip("\n").split("\t")
            if cols is None:
                cols = f
                continue
            rows.append(dict(zip(cols, f)))
    return rows


kp = collections.defaultdict(set)
for r in table(os.path.join(corr, "k-paths.tsv")):
    if r["path"] not in ("-", ""):
        kp[r["path"]].add(r["k"] + ":" + r["row"])
tp = collections.defaultdict(set)
for r in table(os.path.join(corr, "test262-paths.tsv")):
    tp[r["path"]].add(r["explained_by"])
nest = re.compile(r"(nested too deeply|syntax tree is too deep) to parse \(maximum depth (\d+)\)")

records = {}
for name in ("test262-results.json", "rerun-timeouts.json"):
    p = os.path.join(run, "t262-on", name)
    if os.path.exists(p):
        for x in json.load(open(p, encoding="utf-8"))["results"]:
            records[(x["path"], x["variant"])] = x

lines = ["run\tcase\tvariant\toff\ton\tclass\tbasis\n"]
counts = collections.Counter()
NS = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"
full = {}
for r in table(os.path.join(rd, "tests-differ.tsv")):
    if r["project"] not in full:
        full[r["project"]] = {}
        trx = os.path.join(run, "tests-on", "trx", r["project"] + ".trx")
        for u in ET.parse(trx).getroot().iter(NS + "UnitTestResult"):
            m = u.find(NS + "Output/" + NS + "ErrorInfo/" + NS + "Message")
            if m is not None and m.text:
                full[r["project"]].setdefault(u.get("testName").replace("\t", " "), m.text)
for r in table(os.path.join(rd, "tests-differ.tsv")):
    m = nest.search(full.get(r["project"], {}).get(r["test"], r["on-message"]))
    if r["off"] == "Passed" and r["on"] == "Failed" and m:
        cls, basis = "C3", "refused for nesting past the library's bound, depth %s (the switch-on TRX: %s)" % (m.group(2), m.group(0))
    else:
        cls, basis = "unclassified", "no class admits it: %s" % r["on-message"]
    counts[("tests", r["off"] + "->" + r["on"], cls)] += 1
    lines.append("\t".join(["tests", r["project"] + ":" + r["test"], "-", r["off"], r["on"], cls, basis]) + "\n")
for r in table(os.path.join(rd, "t262-differ.tsv.gz")):
    key = (r["path"], r["variant"])
    rec = records.get(key, {})
    detail = "; ".join("%s=%s" % (k, str(rec[k]).replace("\t", " ").replace("\n", " ")[:300]) for k in ("status", "engineFailure", "reason", "skipKind", "durationMs", "stderr") if k in rec)
    ks = sorted(kp.get(r["path"], ()))
    if r["off"] == "failed" and r["on"] == "passed" and ks:
        cls, basis = "C1", "k-paths.tsv lists the path for " + ",".join(ks)
    elif r["off"] == "failed" and r["on"] == "passed" and tp.get(r["path"]) == {"T"}:
        cls, basis = "T", "test262-paths.tsv explains the path by T"
    elif r["off"] == "passed" and r["on"] in ("failed", "timedOut", "skipped") and nest.search(detail):
        cls, basis = "C3", "refused for nesting past the library's bound, depth " + nest.search(detail).group(2)
    else:
        why = "test262-paths.tsv explains the path by %s, and k-paths.tsv does not list it" % ",".join(sorted(tp[r["path"]])) if r["path"] in tp else "no corrections table names the path"
        cls, basis = "unclassified", "%s; the switch-on record: %s" % (why, detail or "-")
    counts[("t262", r["off"] + "->" + r["on"], cls)] += 1
    lines.append("\t".join(["t262", r["path"], r["variant"], r["off"], r["on"], cls, basis]) + "\n")
os.makedirs(out, exist_ok=True)
with open(os.path.join(out, "local-differences.tsv"), "w", encoding="utf-8", newline="\n") as w:
    w.writelines(lines)
for k, v in sorted(counts.items()):
    print("%d\t%s\t%s\t%s" % (v, *k))
