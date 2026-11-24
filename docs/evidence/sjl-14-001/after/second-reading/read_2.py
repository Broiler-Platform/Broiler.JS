#!/usr/bin/env python3
"""The second readings' aid for parts A, D and K (bundle sjl-14-001, under decision-rule-revised-2026-11-24.md, the
owner's answers 12, 13 and 15 of 2026-11-24): each part's retained tables read again under the revision's classes, with
nothing built or run. Written 2026-11-24.

    read_2.py <bundle folder> > <out>

Part A: after/part-a/comparison/comparison.json's every changed row (the rule's comparison, retained), its verdict
there (admitted by the classification file, whose class it gives, or not), and, for a row it does not admit, whether it
is the one path and variant rev-for-of or rev-minifier names at part A.
Part D: after/part-d/'s parser/ and pass/ differences.tsv.gz, every row with its class; a pass-tree dump row left
unclassified there is read under rev-pass: its input variant listed by pass/unclassified.tsv (checked by its SHA-256,
the blob the revision names), its parser-tree dump row's class one part D admits, no outcome or message row of the
variant outside part D's classes, and pass/unclassified-lines.txt's count of differing lines that are not span lines.
Part K: after/part-k/hits.tsv's every hit with its sort there, and, for every hit but a facade member,
after/part-k/listing.tsv's row: what it is, read against the kinds the revision's MET line names.
Run with python3 -I."""
import collections, gzip, hashlib, json, os, sys

B = sys.argv[1]
P = lambda *p: os.path.join(B, *p)
out = []
w = out.append

# Part A
c = json.load(open(P("after/part-a/comparison/comparison.json"), encoding="utf-8"))
cls = json.load(open(P("after/part-a/comparison/classifications.json"), encoding="utf-8"))
NAMED = {("test/language/statements/for-of/head-await-using-bound-names-in-stmt.js", "original", "failed", "passed"): "rev-for-of",
         ("test/language/expressions/dynamic-import/syntax/valid/new-covered-expression-is-valid.js", "terser", "passed", "skipped"): "rev-minifier"}
named = open(P("after/part-a/comparison/named-rows.log"), encoding="utf-8").read()
skip = "terser after: status=skipped; reason=node parses this test and cannot parse its minified body"
LISTED_WITH = {
    "rev-for-of": "K7 and its row D35 (\"Lexical redeclarations Broiler.JS misses\"); the library's row table reads in EE0056's "
                  "notes that the using heads are EP rows, while the library refuses this input with EE0056's code, JSE1417; "
                  "finding S14-F2",
    "rev-minifier": "the after run's record: skipKind=minifier-invalid-output (named-rows.log: %s)" % (
        "found" if skip in named and "skipKind=minifier-invalid-output" in named else "NOT FOUND"),
}
w("# read_2.py: parts A, D and K read again under decision-rule-revised-2026-11-24.md, from their retained tables")
w("")
w("## part A (after/part-a/comparison/comparison.json; the rule's comparison, retained)")
reading = collections.Counter()
rest = []
for r in c["changed"]:
    key = (r["path"], r["variant"], r["base"], r["after"])
    if r["verdict"] == "admitted":
        reading["admitted by the classification file (" + r["class"] + ")"] += 1
    elif key in NAMED:
        reading[NAMED[key]] += 1
        w("%s\t%s [%s]\t%s -> %s\tthe script's verdict: %s\tlisted with: %s" % (
            NAMED[key], r["path"], r["variant"], r["base"], r["after"], r["verdict"], LISTED_WITH[NAMED[key]]))
    else:
        reading["not admitted"] += 1
        rest.append(r)
classes = collections.Counter()
for e in cls.values():
    classes[e["class"]] += 1
w("# changed rows %d; %s" % (len(c["changed"]), "; ".join("%s %d" % kv for kv in sorted(reading.items()))))
w("# the classification file's rows by class: %s; the script's counts: admitted %s, unclassified %s, regressions %s" % (
    "; ".join("%s %d" % kv for kv in sorted(classes.items())), c["admitted"], c["unclassified"], c["regressions"]))
for r in rest:
    w("NOT ADMITTED\t" + json.dumps(r, sort_keys=True))


# Part D
def table(path):
    with gzip.open(path, "rt", encoding="utf-8", newline="\n") as f:
        cols = None
        for line in f:
            if line.startswith("#"):
                continue
            x = line.rstrip("\n").split("\t")
            if cols is None:
                cols = x
                continue
            yield dict(zip(cols, x))


ADMITTED = {"C1", "C3", "E", "T", "M", "S", "rev-code", "rev-eof", "rev-bracket"}
LISTED_SHA = "15ad36bfc52150b64e410c0b441c36a86503570bdb27a470856c7d61b82d1a05"
w("")
w("## part D (after/part-d/parser/ and pass/; pass/unclassified.tsv, the list rev-pass names)")
listed_path = P("after/part-d/pass/unclassified.tsv")
sha = hashlib.sha256(open(listed_path, "rb").read()).hexdigest()
w("# pass/unclassified.tsv SHA-256 %s: %s" % (sha, "the revision's" if sha == LISTED_SHA else "NOT THE REVISION'S"))
listed = set()
for line in open(listed_path, encoding="utf-8"):
    x = line.rstrip("\n").split("\t")
    if x[0] != "pair":
        listed.add((x[1], x[2]))
parser = list(table(P("after/part-d/parser/differences.tsv.gz")))
passed = list(table(P("after/part-d/pass/differences.tsv.gz")))
by_variant = collections.defaultdict(list)
for r in parser:
    by_variant[(r["id"], r["variant"])].append(r)
count = collections.Counter()
bad = []
for r in parser:
    k = r["class"] if r["class"] in ADMITTED else "NOT ADMITTED"
    count[("parser trees, outcome, message and dump rows", r["pair"], r["kind"], k)] += 1
    if k == "NOT ADMITTED":
        bad.append(("parser", r))
notspan = None
for line in open(P("after/part-d/pass/unclassified-lines.txt"), encoding="utf-8"):
    if line.startswith("# their differing lines that are not span lines"):
        notspan = int(line.rsplit(":", 1)[1])
w("# pass/unclassified-lines.txt: differing lines of the listed variants' pass-tree dumps that are not span lines: %s" % notspan)
for r in passed:
    if r["kind"] != "dump":
        continue  # the pass report's outcome rows are parser/'s (after/part-d/README.md)
    k = r["class"]
    if k == "unclassified":
        v = (r["id"], r["variant"])
        prow = [x for x in by_variant.get(v, []) if x["kind"] == "dump"]
        others = [x for x in by_variant.get(v, []) if x["kind"] != "dump" and x["class"] not in ADMITTED]
        if v in listed and len(prow) == 1 and prow[0]["class"] in ADMITTED and not others and notspan == 0:
            k = "rev-pass"
            count[("rev-pass, by the variant's parser-tree class", r["pair"], "dump", prow[0]["class"])] += 1
        else:
            k = "NOT ADMITTED"
            bad.append(("pass", r))
    elif k not in ADMITTED:
        k = "NOT ADMITTED"
        bad.append(("pass", r))
    count[("pass trees, dump rows", r["pair"], r["kind"], k)] += 1
for k, n in sorted(count.items()):
    w("%d\t%s" % (n, "\t".join(k)))
w("# listed variants %d; rows read NOT ADMITTED: %d" % (len(listed), len(bad)))
for where, r in bad:
    w("NOT ADMITTED\t%s\t%s" % (where, "\t".join(r[c] for c in ("pair", "id", "variant", "kind", "class", "basis"))))

# Part K
w("")
w("## part K (after/part-k/hits.tsv, with after/part-k/listing.tsv for every hit but a facade member)")
listing = {}
for line in open(P("after/part-k/listing.tsv"), encoding="utf-8"):
    if line.startswith("#") or line.startswith("section\t"):
        continue
    x = line.rstrip("\n").split("\t")
    listing[(x[1], x[2])] = x
KINDS = {
    "another type's member of the same name": "another type's member of the same name, listed with its declaration",
    "text: a changelog line": "a line of a changelog",
    "text: a comment line": "a code comment",
    "text: a record line, naming another type's member of the same name": "a line of a record",
    "text: a record line": "a line of a record",
}
kc = collections.Counter()
notk = []
for line in open(P("after/part-k/hits.tsv"), encoding="utf-8"):
    if line.startswith("#") or line.startswith("section\t"):
        continue
    x = line.rstrip("\n").split("\t")
    name, key, sort = x[1].split(" (")[0], x[2], x[3]
    if sort == "facade member":
        kc["a facade member (hits.tsv's sort)"] += 1
        continue
    row = listing[(name, key)]
    what = row[3]
    if what.startswith("the facade's own"):
        kc["a facade member: " + what] += 1
    elif all(part in KINDS for part in what.split(" + ")):
        for part in what.split(" + "):
            kc[KINDS[part]] += 1
    else:
        kc["NOT ADMITTED: " + what] += 1
        notk.append((key, name, what, row[4], row[5]))
for k, n in sorted(kc.items()):
    w("%d\t%s" % (n, k))
w("# hits read NOT ADMITTED by the revision's kinds: %d" % len(notk))
for r in notk:
    w("NOT ADMITTED\t" + "\t".join(r))
print("\n".join(out))
