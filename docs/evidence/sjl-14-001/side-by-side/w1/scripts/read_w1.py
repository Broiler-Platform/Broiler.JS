#!/usr/bin/env python3
"""Read the side-by-side run at W1 (bundle sjl-14-001, part C's tool; read, not judged).

    read_w1.py <side-by-side report.tsv.gz> <bundle dir> <out dir>

Writes <out>/outcome-moves.tsv: every input variant whose outcome differs between P and B, or between K and K-on
(accepted, refused or crashed; a refusal's type or message alone is not an outcome move), with both outcomes, the
messages, and whether corrections/test262-paths.tsv lists the variant (with its expected outcome, its explanation
and rows) or corrections/k-paths.tsv names the id. It prints the counts by entry pair and by listing. It classifies
nothing: the classes are part C's, judged at the base commit. Run with python3 -I.
"""
import collections, gzip, sys

report, bundle, out = sys.argv[1:4]
t = collections.defaultdict(dict)
order = []
with gzip.open(report, "rt", encoding="utf-8", newline="\n") as h:
    for line in h:
        if line.startswith("#%") or line.startswith("id\t"):
            continue
        f = line.rstrip("\n").split("\t")
        key = (f[0], f[1])
        if key not in t:
            order.append(key)
        t[key][f[2]] = f
paths = {}
with open(bundle + "/corrections/test262-paths.tsv", encoding="utf-8") as h:
    cols = None
    for line in h:
        if line.startswith("#"):
            continue
        f = line.rstrip("\n").split("\t")
        if cols is None:
            cols = f
            continue
        r = dict(zip(cols, f))
        paths[(r["id"], r["variant"])] = r
kids = collections.defaultdict(set)
with open(bundle + "/corrections/k-paths.tsv", encoding="utf-8") as h:
    for line in h:
        f = line.rstrip("\n").split("\t")
        if line.startswith("#") or len(f) < 4 or f[0] == "k":
            continue
        kids[f[2]].add(f[0] + ":" + f[1])
counts = collections.Counter()
with open(out + "/outcome-moves.tsv", "w", encoding="utf-8", newline="\n") as w:
    w.write("# Every input variant whose outcome moves between P and B or between K and K-on in the side-by-side run at W1,\n")
    w.write("# with corrections/test262-paths.tsv's listing (expect, explained_by, rows) and corrections/k-paths.tsv's (k:row). Read, not judged.\n")
    w.write("pair\tid\tvariant\tfrom\tto\tfrom-type\tfrom-message\tto-type\tto-message\tlisted-expect\tlisted-explained-by\tlisted-rows\tk-paths\n")
    for key in order:
        e = t[key]
        for a, b in (("P", "B"), ("K", "K-on")):
            x, y = e[a], e[b]
            if x[3] == y[3]:
                continue
            lp = paths.get(key)
            kp = ",".join(sorted(kids.get(key[0], ())))
            listed = "test262-paths" if lp else ("k-paths" if kp else "unlisted")
            agrees = ""
            if lp:
                agrees = " expect-" + ("matches" if lp["expect"] == y[3] else "differs")
            counts[(a + "/" + b, x[3] + "->" + y[3], listed + agrees)] += 1
            w.write("\t".join([a + "/" + b, key[0], key[1], x[3], y[3], x[4], x[5], y[4], y[5],
                               lp["expect"] if lp else "-", lp["explained_by"] if lp else "-", lp["rows"] if lp else "-", kp or "-"]) + "\n")
for k, v in sorted(counts.items()):
    print(f"{v}\t{k[0]}\t{k[1]}\t{k[2]}")
