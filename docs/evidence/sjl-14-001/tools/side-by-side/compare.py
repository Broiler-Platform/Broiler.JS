#!/usr/bin/env python3
"""Compare two entry points of a side-by-side report, or one entry point of two reports.

    compare.py <report[.gz]> <entry-a> <entry-b> [<kind> [<count>]]
    compare.py --same <report-a[.gz]> <report-b[.gz]> <entry>[,<entry>...]
    compare.py --messages <report[.gz]> <entry-a> <entry-b> <table.tsv>

The first form counts every input variant by the pair of outcomes at the two entry points (accepted, refused,
crashed or absent), splitting accepted/accepted by the dump digest and refused/refused by the exception type and
then the message, and lists up to <count> (default 20; "all" for every one) variants of one <kind> as the counts
print it. The second form says whether two reports' rows at the named entry points are identical, byte for byte,
as decision rule part B asks of P and K at W1's first parent and at W1 with the switch off; it exits 1 when they
are not. The third form writes the generated message table of class M (decision D46): every input variant refused
at both entry points with the same exception type and different messages, with both texts, one row each, in
manifest order; refusals whose types differ are not class M and are counted on standard error.

Dump differences of accepted/accepted pairs are read with spancheck.py beside this file (class S).

Run with python3 -I; it reads only the files it is given.
"""

import collections
import gzip
import sys


def rows(path):
    opener = gzip.open if path.endswith(".gz") else open
    with opener(path, "rt", encoding="utf-8", newline="\n") as handle:
        for line in handle:
            if line.startswith("#%") or line.startswith("id\t"):
                continue
            yield line.rstrip("\n").split("\t")


def pairs(path, a, b):
    table = collections.defaultdict(dict)
    for field in rows(path):
        table[(field[0], field[1])][field[2]] = field
    counts = collections.Counter()
    examples = collections.defaultdict(list)
    for key, entries in table.items():
        if a not in entries or b not in entries:
            continue
        x, y = entries[a], entries[b]
        if x[3] == y[3] == "accepted":
            kind = "accepted/accepted same-dump" if x[6] == y[6] else "accepted/accepted dump-differs"
        elif x[3] == y[3] == "refused":
            if x[4] != y[4]:
                kind = "refused/refused type-differs"
            else:
                kind = "refused/refused same-message" if x[5] == y[5] else "refused/refused message-differs"
        else:
            kind = x[3] + "/" + y[3]
        counts[kind] += 1
        examples[kind].append((key[0], key[1], x[4], x[5], y[4], y[5]))
    return counts, examples


def same(path_a, path_b, entries):
    def select(path):
        return [field for field in rows(path) if field[2] in entries]

    left, right = select(path_a), select(path_b)
    if left == right:
        print(f"identical: {len(left)} row(s) at {','.join(sorted(entries))}")
        return 0
    differing = sum(1 for x, y in zip(left, right) if x != y) + abs(len(left) - len(right))
    print(f"differ: {differing} row(s) of {len(left)} and {len(right)}")
    return 1


def messages(path, a, b, out):
    counts, examples = pairs(path, a, b)
    rows_written = 0
    with open(out, "w", encoding="utf-8", newline="\n") as handle:
        handle.write("# The generated message table of class M (bundle sjl-14-001's rule, part C; decision D46): every input variant\n")
        handle.write(f"# refused at entry points {a} and {b} with the same exception type and different messages, from {path}.\n")
        handle.write("id\tvariant\ttype\tmessage-" + a + "\tmessage-" + b + "\n")
        for (ident, variant, type_a, message_a, type_b, message_b) in examples["refused/refused message-differs"]:
            handle.write("\t".join((ident, variant, type_a, message_a, message_b)) + "\n")
            rows_written += 1
    print(f"{rows_written}\trefused/refused message-differs written to {out}")
    other = counts.get("refused/refused type-differs", 0)
    if other:
        print(f"{other}\trefused/refused type-differs: not class M, not written", file=sys.stderr)
    return 0


def main(argv):
    if len(argv) == 5 and argv[1] == "--same":
        return same(argv[2], argv[3], set(argv[4].split(",")))
    if len(argv) == 6 and argv[1] == "--messages":
        return messages(argv[2], argv[3], argv[4], argv[5])
    if len(argv) < 4:
        print(__doc__, file=sys.stderr)
        return 2
    counts, examples = pairs(argv[1], argv[2], argv[3])
    for kind, count in sorted(counts.items()):
        print(f"{count}\t{kind}")
    if len(argv) > 4:
        limit = None if len(argv) > 5 and argv[5] == "all" else int(argv[5]) if len(argv) > 5 else 20
        for example in examples[argv[4]][:limit]:
            print("\t".join(example))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
