#!/usr/bin/env python3
"""pass_lines.py <out dir> <pass spans.tsv>: the dump lines of the input variants whose dump after the compiler's for-loop
pass part C's classifier leaves unclassified (pass/differences.tsv.gz), counted by node kind, by span check verdict
(spancheck.py's S, both-agree or unclassified, with whether P's and B's spans agree with the text) and, for a both-agree
line, by the classifier's reading (pass/span-readings.tsv.gz); and each such input variant's class at the parser trees
(parser/differences.tsv.gz). Written 2026-11-24. Run with python3 -I."""
import collections, gzip, sys
out, spans = sys.argv[1:3]
def table(path):
    with gzip.open(path, "rt", encoding="utf-8", newline="\n") as f:
        cols = None
        for line in f:
            if line.startswith("#"): continue
            x = line.rstrip("\n").split("\t")
            if cols is None: cols = x; continue
            yield dict(zip(cols, x))
par = {(r["id"], r["variant"]): r["class"] for r in table(out + "/parser/differences.tsv.gz") if r["kind"] == "dump"}
unc = {(r["id"], r["variant"]) for r in table(out + "/pass/differences.tsv.gz") if r["kind"] == "dump" and r["class"] == "unclassified"}
reading = {}
for r in table(out + "/pass/span-readings.tsv.gz"):
    reading[(r.get("id"), r.get("variant"), r.get("line"))] = r.get("reading", "")
lines = collections.Counter(); per = collections.Counter(); notspan = 0
with open(spans, encoding="utf-8", newline="\n") as f:
    next(f)
    for line in f:
        x = line.rstrip("\n").split("\t")
        if (x[0], x[1]) not in unc: continue
        if x[8] == "not-span": notspan += 1; continue
        rd = reading.get((x[0], x[1], x[2]), "") if x[8] == "both-agree" else ""
        lines[(x[3], x[8], "P-agrees=" + x[6], "B-agrees=" + x[7], rd)] += 1
print("# the input variants whose pass-tree dump difference part C's classifier leaves unclassified: %d" % len(unc))
print("# their class at the parser trees (parser/differences.tsv.gz): " + "; ".join("%s %d" % kv for kv in sorted(collections.Counter(par.get(k, "NO PARSER-TREE DIFFERENCE") for k in unc).items())))
print("# their differing lines that are not span lines (spancheck.py's not-span): %d" % notspan)
print("# their span lines, by node kind, verdict, both checks and reading:")
for k, v in sorted(lines.items(), key=lambda kv: (-kv[1], kv[0])):
    print("%d\t%s" % (v, "\t".join(k)))
