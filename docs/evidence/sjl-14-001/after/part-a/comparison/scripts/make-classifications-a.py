#!/usr/bin/env python3
"""make-classifications-a.py <first-look comparison.json> <repo root> <out>: part A's classification file, one entry per
changed row that a class of part A's rule (decision-rule.md, part A; the revision of 2026-11-23 leaves part A unchanged)
admits, read by the corrections as the after commit holds them (no row added):
  C1  a row failing at the base that passes after whose path corrections/k-paths.tsv lists for a row of K1 to K10
      (the reading part C's classify_local.py makes of a test262 case: "a test262 case failing off and passing on whose
      path corrections/k-paths.tsv lists for a row of K1 to K10")
  C3  a row passing at the base and failing after, refused for nesting past the library's bound
  T   a negative row failing at the base and passing after that corrections/test262-paths.tsv explains by T alone
Every other changed row is left out of the file, so the comparison reports it unclassified, or, for a row passing at
the base, a regression whatever the file says. compare_test262_paths.py admits C1, C3 and C5, not T; a T row, if any,
is written with class "T" and the script reports it unclassified, which the reading then names (none is found)."""
import collections, json, os, re, sys
cmp = json.load(open(sys.argv[1], encoding="utf-8")); root = sys.argv[2]
corr = os.path.join(root, "docs", "evidence", "sjl-14-001", "corrections")
def table(p):
    cols, rows = None, []
    for l in open(p, encoding="utf-8"):
        if l.startswith("#"): continue
        f = l.rstrip("\n").split("\t")
        if cols is None: cols = f; continue
        rows.append(dict(zip(cols, f)))
    return rows
kp = collections.defaultdict(set)
for r in table(os.path.join(corr, "k-paths.tsv")):
    if r["path"] not in ("-", ""): kp[r["path"]].add((r["k"], r["row"]))
tp = collections.defaultdict(set)
for r in table(os.path.join(corr, "test262-paths.tsv")): tp[r["path"]].add(r["explained_by"])
krow = {r["row"]: r for r in table(os.path.join(corr, "k-rows.tsv"))}
nest = re.compile(r"(nested too deeply|syntax tree is too deep) to parse \(maximum depth (\d+)\)")
out, counts = {}, collections.Counter()
for row in cmp["changed"]:
    key = f"{row['path']}|{row['variant']}"
    if row["base"] == "failed" and row["after"] == "passed" and kp.get(row["path"]):
        ks = sorted(kp[row["path"]])
        names = "; ".join(f"{k}, row {r} ({krow[r]['what']})" for k, r in ks)
        out[key] = {"class": "C1", "reason": "correction: corrections/k-paths.tsv lists this path for " + names +
                    ". At the base the row failed; after, the shared frontend refuses or accepts it as the pinned text does, and the row passes."}
        counts["C1"] += 1
    elif row["base"] == "failed" and row["after"] == "passed" and tp.get(row["path"]) == {"T"}:
        out[key] = {"class": "T", "reason": "timing: corrections/test262-paths.tsv explains the path by T"}; counts["T"] += 1
    else:
        counts["left out: %s->%s" % (row["base"], row["after"])] += 1
open(sys.argv[3], "w", encoding="utf-8", newline="\n").write(json.dumps(out, indent=1, sort_keys=True) + "\n")
print(dict(counts))
