#!/usr/bin/env python3
"""Compare two entry points of a side-by-side report, or one entry point of two reports.

    compare.py <report[.gz]> <entry-a> <entry-b> [<kind> [<count>]]
    compare.py --same <report-a[.gz]> <report-b[.gz]> <entry>[,<entry>...]

The first form counts every input variant by the pair of outcomes at the two entry points (accepted, refused,
crashed or absent), splitting accepted/accepted by the dump digest and refused/refused by the message, and lists
up to <count> (default 20) variants of one <kind> as the counts print it. The second form says whether two reports'
rows at the named entry points are identical, byte for byte, as decision rule part B asks of P and K at W1's first
parent and at W1 with the switch off; it exits 1 when they are not.

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


def main(argv):
    if len(argv) == 5 and argv[1] == "--same":
        return same(argv[2], argv[3], set(argv[4].split(",")))
    if len(argv) < 4:
        print(__doc__, file=sys.stderr)
        return 2
    counts, examples = pairs(argv[1], argv[2], argv[3])
    for kind, count in sorted(counts.items()):
        print(f"{count}\t{kind}")
    if len(argv) > 4:
        limit = int(argv[5]) if len(argv) > 5 else 20
        for example in examples[argv[4]][:limit]:
            print("\t".join(example))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
