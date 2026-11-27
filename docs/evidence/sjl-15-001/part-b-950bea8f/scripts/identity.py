#!/usr/bin/env python3
"""identity.py <base-report.tsv.gz> <head-report.tsv.gz>: bundle sjl-15-001's part B at SJL-15.2 stage 7's model commit
950bea8f; copied 2026-11-27 from stage 6's part B script SCRATCH/sjl-rest/js-s156-pb/scripts/identity.py, only this
header changed (there it named stage 6's model commit 2c48beb1 and its own source, stage 5c's part B script; stage 5c's
named 30af0dad, stage 5b's 84d84cc3, stage 5a's 4d8aa93a, stage 4's 56272150, stage 3's d87044c9, stage 2's 6c61879b, and
stage 1's copy read "SJL-15.2 stage 1's part B rehearsal (not a run of the rule).", the rehearsal's label).

Compares two reports of bundle sjl-12-001's parse-corpus tool line by line: every row line (a line not starting with
'#') must be the same bytes in the same order; the header lines ('#%') are listed side by side where they differ.
Prints a summary and exits 0 when every row line is identical and both reports end with their '#% end' line.
"""
import gzip
import sys


def read(path):
    with gzip.open(path, "rb") as f:
        lines = f.read().split(b"\n")
    if lines and lines[-1] == b"":
        lines.pop()
    return lines


def main():
    a, b = read(sys.argv[1]), read(sys.argv[2])
    rows_a = [l for l in a if not l.startswith(b"#")]
    rows_b = [l for l in b if not l.startswith(b"#")]
    head_a = [l for l in a if l.startswith(b"#")]
    head_b = [l for l in b if l.startswith(b"#")]
    ends = [l for l in (a[-1:], b[-1:])]
    complete = all(e and e[0].startswith(b"#% end\t") for e in ends)
    differing = sum(1 for x, y in zip(rows_a, rows_b) if x != y) + abs(len(rows_a) - len(rows_b))
    print("base rows: %d, head rows: %d, differing rows: %d" % (len(rows_a), len(rows_b), differing))
    print("base end: %s" % (a[-1].decode() if a else "-"))
    print("head end: %s" % (b[-1].decode() if b else "-"))
    print("header lines differing:")
    for x, y in zip(head_a, head_b):
        if x != y:
            print("  base: " + x.decode("utf-8", "replace"))
            print("  head: " + y.decode("utf-8", "replace"))
    if len(head_a) != len(head_b):
        print("  header line counts differ: %d, %d" % (len(head_a), len(head_b)))
    shown = 0
    for i, (x, y) in enumerate(zip(rows_a, rows_b)):
        if x != y and shown < 20:
            print("row %d differs:\n  base: %s\n  head: %s" % (i, x.decode("utf-8", "replace")[:300], y.decode("utf-8", "replace")[:300]))
            shown += 1
    ok = complete and differing == 0 and len(rows_a) > 0
    print("identical in every row: %s; both complete: %s" % ("yes" if differing == 0 else "no", "yes" if complete else "no"))
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
