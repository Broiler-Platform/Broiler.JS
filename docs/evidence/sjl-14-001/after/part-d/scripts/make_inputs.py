#!/usr/bin/env python3
"""Part D's comparison inputs, in the form of part C's side-by-side report, so that part C's own classifier reads part D
(bundle sjl-14-001, decision rule part D as revised on 2026-11-23). Written 2026-11-24.

    make_inputs.py <base report.tsv.gz> <base dump report.tsv.gz> <after report.tsv.gz> <after dump report.tsv.gz>
                   <base dump text.txt.gz> <after dump text.txt.gz> <manifest> <out dir>

The reports are bundle sjl-12-001's tool's: its default mode (entries P and K) and its --dump --order manifest mode
(each variant's parser-sha256 and pass-sha256) at the base commit and at the after commit; the texts are the same
tool's --dump --order manifest --text output at each commit (blocks "=== <id> <variant> 1 parser" and "... 1 prepared").
Part D's comparison is "per input, variant and entry point: outcome, exception type and message; per input, the dump
hashes", the base commit against the after commit. This script writes, for part C's classifier
(base/part-c/scripts/classify_c.py) and span check (tools/side-by-side/spancheck.py), which read entry pairs P/B and
K/K-on:
  parser.tsv.gz   one row per input variant and entry: P := the base commit's P, B := the after commit's P (each with
                  its parser-sha256 as the dump hash, "-" where the dump report has none), K := the base commit's K,
                  K-on := the after commit's K
  pass.tsv.gz     the same rows, with each P and B dump hash the pass-sha256 (the tree after the compiler's for-loop pass)
  parser.txt.gz   the parser blocks of both texts, the base's named P and the after's B, interleaved in manifest order
  pass.txt.gz     the "prepared" blocks the same way
So "P/B" in the classifier's tables reads "P at the base commit against P at the after commit", and "K/K-on" reads "K
at the base commit against K at the after commit". Every row of the four reports is checked to be in the manifest, and
the rows of the two reports of each commit to name the same input variants. The gzip outputs carry no name and mtime 0.
Run with python3 -I.
"""
import gzip, sys

bc, bd, ac, ad, bt, at, manifest, out = sys.argv[1:9]


def rows(path):
    with gzip.open(path, "rt", encoding="utf-8", newline="\n") as f:
        for line in f:
            if line.startswith("#") or line.startswith("id\t"):
                continue
            yield line.rstrip("\n").split("\t")


order = {}
with open(manifest, encoding="utf-8", newline="\n") as f:
    cols = None
    for line in f:
        if line.startswith("#"):
            continue
        x = line.rstrip("\n").split("\t")
        if cols is None:
            cols = x
            continue
        for v in x[5].split(","):
            order[(x[0], v)] = len(order)


def acc(path):
    t = {}
    for x in rows(path):
        assert (x[0], x[1]) in order, x
        t[(x[0], x[1], x[2])] = x[3:6]
    return t


def dumps(path):
    t = {}
    for x in rows(path):
        assert (x[0], x[1]) in order and x[2] == "1", x
        t[(x[0], x[1])] = (x[4], x[6])
    return t


B, A, BD, AD = acc(bc), acc(ac), dumps(bd), dumps(ad)
keys = sorted({(i, v) for i, v, _ in B}, key=order.get)
assert keys == sorted({(i, v) for i, v, _ in A}, key=order.get) == sorted(BD, key=order.get) == sorted(AD, key=order.get)


def gz(path):
    return gzip.GzipFile(filename="", mode="wb", fileobj=open(path, "wb"), compresslevel=6, mtime=0)


for name, slot in (("parser", 0), ("pass", 1)):
    with gz(f"{out}/{name}.tsv.gz") as g:
        g.write(("#% format\tbroiler-js-side-by-side-report\t1\n#% made-by\tafter/part-d/scripts/make_inputs.py: P/B are P at the base and after commits, K/K-on are K at the base and after commits; the dump hash is the " + name + " tree's\n").encode())
        g.write("id\tvariant\tentry\toutcome\ttype\tmessage\tdump-sha256\n".encode())
        for i, v in keys:
            for e, src, d in (("P", B[(i, v, "P")], BD[(i, v)][slot]), ("B", A[(i, v, "P")], AD[(i, v)][slot]),
                              ("K", B[(i, v, "K")], "-"), ("K-on", A[(i, v, "K")], "-")):
                g.write(("\t".join([i, v, e] + src + [d if d else "-"]) + "\n").encode())


def blocks(path, kind):
    with gzip.open(path, "rt", encoding="utf-8", newline="\n") as f:
        cur, lines = None, []
        for line in f:
            if line.startswith("=== "):
                if cur:
                    yield cur, lines
                h = line[4:].split()
                cur, lines = ((h[0], h[1]) if h[3] == kind and h[2] == "1" else None), []
            elif cur:
                lines.append(line)
        if cur:
            yield cur, lines


for name, kind in (("parser", "parser"), ("pass", "prepared")):
    gb, ga = blocks(bt, kind), blocks(at, kind)
    nb, na = next(gb, None), next(ga, None)
    n = 0
    with gz(f"{out}/{name}.txt.gz") as g:
        while nb or na:
            if nb and (not na or order[nb[0]] <= order[na[0]]):
                g.write(("=== %s %s P\n" % nb[0]).encode()); g.write("".join(nb[1]).encode()); nb = next(gb, None)
            else:
                g.write(("=== %s %s B\n" % na[0]).encode()); g.write("".join(na[1]).encode()); na = next(ga, None)
            n += 1
    print(name, "blocks", n)
