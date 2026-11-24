#!/usr/bin/env python3
"""identity_with_part_c.py <repo root>: part D's reports against part C's side-by-side report at the base commit, row by
row (bundle sjl-14-001). Written 2026-11-24. For every input variant: the base commit's P and K rows (outcome, type,
message; base/local/part-d/parse-corpus-c.tsv.gz) against the side-by-side report's P and K
(base/local/part-c-tool/side-by-side.tsv.gz), the after commit's P and K (after/local/partd/parse-corpus-c.tsv.gz)
against its B and K-on, and the parser-sha256 of each commit's dump report (base/local/part-d/parse-corpus-d1.tsv.gz,
after/local/partd/parse-corpus-d1.tsv.gz) against the side-by-side report's P and B dump hashes. Run with python3 -I."""
import gzip, os, sys
B = os.path.join(sys.argv[1], "docs/evidence/sjl-14-001")
def rows(p):
    for l in gzip.open(os.path.join(B, p), "rt", encoding="utf-8"):
        if l.startswith("#") or l.startswith("id\t"): continue
        yield l.rstrip("\n").split("\t")
sbs = {}
for r in rows("base/local/part-c-tool/side-by-side.tsv.gz"): sbs[(r[0], r[1], r[2])] = r[3:]
bc = {(r[0], r[1], r[2]): r[3:6] for r in rows("base/local/part-d/parse-corpus-c.tsv.gz")}
ac = {(r[0], r[1], r[2]): r[3:6] for r in rows("after/local/partd/parse-corpus-c.tsv.gz")}
bd = {(r[0], r[1]): r[4] for r in rows("base/local/part-d/parse-corpus-d1.tsv.gz")}
ad = {(r[0], r[1]): r[4] for r in rows("after/local/partd/parse-corpus-d1.tsv.gz")}
print("# part D's reports against part C's side-by-side report at the base commit, row by row")
for name, mine, pair in (("base P", bc, ("P", "P")), ("base K", bc, ("K", "K")), ("after P", ac, ("P", "B")), ("after K", ac, ("K", "K-on"))):
    keys = [k for k in mine if k[2] == pair[0]]
    same = sum(1 for k in keys if mine[k] == sbs[(k[0], k[1], pair[1])][:3])
    print("%s (outcome, type, message) against the side-by-side report's %s: %d rows, equal %d%s" % (name, pair[1], len(keys), same, " (all)" if same == len(keys) else " (NOT all)"))
for name, mine, entry in (("base", bd, "P"), ("after", ad, "B")):
    keys = [k for k in mine if mine[k] not in ("", "-")]
    same = sum(1 for k in keys if mine[k] == sbs[(k[0], k[1], entry)][3])
    print("%s parser-sha256 against the side-by-side report's %s dump hash: %d dumps, equal %d%s" % (name, entry, len(keys), same, " (all)" if same == len(keys) else " (NOT all)"))
