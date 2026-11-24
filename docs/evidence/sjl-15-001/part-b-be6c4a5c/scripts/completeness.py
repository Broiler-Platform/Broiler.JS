#!/usr/bin/env python3
"""completeness.py <manifest> <report.tsv.gz>...: for each report of bundle sjl-12-001's parse-corpus tool, whether it
holds exactly the manifest's inputs and variants (default mode: each at P and K; --dump: each once), its end line, and
its outcomes counted (so that a run that read no input, every row an input error, would show). Read-only."""
import collections, gzip, sys
man = sys.argv[1]
want = set()
for line in open(man, encoding="utf-8"):
    if line.startswith("#") or not line.strip() or line.startswith("id\t"):
        continue
    p = line.rstrip("\n").split("\t")
    for v in p[5].split(","):
        want.add((p[0], v))
print("manifest %s: %d inputs and variants" % (man, len(want)))
for path in sys.argv[2:]:
    with gzip.open(path, "rt", encoding="utf-8") as f:
        lines = f.read().split("\n")
    if lines[-1] == "":
        lines.pop()
    head = [l for l in lines if l.startswith("#")]
    rows = [l.split("\t") for l in lines if not l.startswith("#")]
    cols, rows = rows[0], rows[1:]
    mode = [l for l in head if l.startswith("#% mode\t")][0].split("\t")[1]
    end = lines[-1]
    keys = collections.Counter()
    outcomes = collections.Counter()
    ci = cols.index("outcome") if "outcome" in cols else cols.index("parse")
    for r in rows:
        keys[(r[0], r[1])] += 1
        outcomes[(r[2] if mode != "dump" else "dump", r[ci])] += 1
    per = 2 if mode != "dump" else 1
    ok = set(keys) == want and all(n == per for n in keys.values()) and end == "#%% end\t%d" % len(rows)
    print("%s: mode %s, columns %s, rows %d, end %r, keys %d (each %d time(s) expected), equal to the manifest's: %s"
          % (path, mode, ",".join(cols), len(rows), end, len(keys), per, "yes" if ok else "NO"))
    for k, n in sorted(outcomes.items()):
        print("  %s %s: %d" % (k[0], k[1], n))
