#!/usr/bin/env python3
"""cross_check.py <out dir> <base dump report> <after dump report>: part D's differences, read two ways, must be the same
set: every difference compare_parse_corpus.py acceptance lists (acceptance.json.gz) is a row of parser/differences.tsv.gz
of kind outcome or message (and the converse), and every input variant whose parser-sha256 or pass-sha256 differs
between the two dump reports is a dump row of parser/ or pass/ (and the converse). Prints the counts by class of both
tables and whether each set agrees. Written 2026-11-24. Run with python3 -I."""
import collections, gzip, json, sys
out, bd, ad = sys.argv[1:4]
acc = json.load(gzip.open(out + "/acceptance.json.gz", "rt", encoding="utf-8"))
def table(path):
    with gzip.open(path, "rt", encoding="utf-8", newline="\n") as f:
        cols = None
        for line in f:
            if line.startswith("#"): continue
            x = line.rstrip("\n").split("\t")
            if cols is None: cols = x; continue
            yield dict(zip(cols, x))
dif = {t: list(table(f"{out}/{t}/differences.tsv.gz")) for t in ("parser", "pass")}
listed = set()
for d in acc.get("differences", acc.get("changed", [])):
    listed.add((d["id"], d["variant"], d["entry"]))
mine = {(r["id"], r["variant"], "P" if r["pair"] == "P/B" else "K") for r in dif["parser"] if r["kind"] in ("outcome", "message", "type")}
print("acceptance.json.gz lists %d differences; parser/differences.tsv.gz has %d outcome, type or message rows; the same set: %s" % (len(listed), len(mine), listed == mine))
def dumps(path):
    t = {}
    with gzip.open(path, "rt", encoding="utf-8") as f:
        for line in f:
            if line.startswith("#") or line.startswith("id\t"): continue
            x = line.rstrip("\n").split("\t"); t[(x[0], x[1])] = (x[3], x[4], x[6])
    return t
b, a = dumps(bd), dumps(ad)
for t, slot in (("parser", 1), ("pass", 2)):
    hd = {k for k in b if b[k][0] == a[k][0] == "accepted" and b[k][slot] != a[k][slot]}
    rows = {(r["id"], r["variant"]) for r in dif[t] if r["kind"] == "dump"}
    print("%s trees: %d input variants accepted at both commits whose %s-sha256 differs; %s/differences.tsv.gz has %d dump rows; the same set: %s" % (t, len(hd), "parser" if t == "parser" else "pass", t, len(rows), hd == rows))
for t in ("parser", "pass"):
    c = collections.Counter((r["pair"], r["kind"], r["class"]) for r in dif[t] if t == "parser" or r["kind"] == "dump")
    print("## %s/differences.tsv.gz by pair (P/B: P at the base against P after; K/K-on: K at the base against K after), kind and class%s" % (t, "" if t == "parser" else ", dump rows only (its outcome rows are parser/'s)"))
    for k, v in sorted(c.items()): print("%d\t%s\t%s\t%s" % (v, *k))
    print("unclassified: %d" % sum(v for k, v in c.items() if k[2] == "unclassified"))
