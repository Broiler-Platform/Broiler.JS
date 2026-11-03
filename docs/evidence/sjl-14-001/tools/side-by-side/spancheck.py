#!/usr/bin/env python3
"""Check the dump differences of P and B against the source text (decision rule part C, class S).

    spancheck.py --manifest <corpus.manifest> --test262 <suite root> <dumps.txt[.gz]> <listing.tsv>

<dumps> is the side-by-side tool's --text output with entries P and B. For every input variant whose two dumps
differ, the lines are compared in order. A line pair that differs only in node spans (the "@start..end" of a line's
head, and the "@start..end" of a token a field holds) is a span difference; each differing span is checked against
the node's source text, the composed source the tool parsed (the strict prologue before a strict variant), and
written to <listing> with both spans, both texts and both checks:

- a span agrees with the text when it is the text of one whole construct: it is not inverted, it neither begins nor
  ends inside white space or a comment, and its parentheses, brackets and braces balance (strings, template
  literals and comments skipped); a program's span agrees when it begins at offset 0 and ends at the end of the
  source;
- the pair is class S when P's span disagrees and B's agrees; "both-agree" when both pass the check (two readings of
  one construct's extent, such as a statement with and without its semicolon, which the check cannot tell apart and a
  reader decides from the two texts); "unclassified" otherwise.

An input variant whose dumps differ in any other way (a different line count, or a line that differs outside its
spans) is listed as "not-span", with its first differing line pair. The counts by node kind and class are printed.
The check is mechanical: each listed row carries both texts, so a reader can verify it.

Run with python3 -I; it reads only the files it is given.
"""

import collections
import gzip
import hashlib
import json
import os
import re
import sys

HEAD = re.compile(r"^(\d+ [^:]+: \S+ (\w+)) @(\d+|\?|eof)\.\.(\d+|\?|eof)(.*)$")
SPAN = re.compile(r"@(\d+|\?|eof)\.\.(\d+|\?|eof)")


def opener(path):
    return gzip.open(path, "rt", encoding="utf-8", newline="\n") if path.endswith(".gz") else open(path, encoding="utf-8", newline="\n")


def read_manifest(path):
    rows, prologue = {}, ""
    with open(path, encoding="utf-8", newline="\n") as handle:
        columns = None
        for line in handle:
            line = line.rstrip("\n")
            if line.startswith("#% strict-prologue\t"):
                prologue = json.loads(line.split("\t", 1)[1])
                continue
            if line.startswith("#"):
                continue
            fields = line.split("\t")
            if columns is None:
                columns = fields
                continue
            row = dict(zip(columns, fields))
            rows[row["id"]] = row
    return rows, prologue


def load(row, test262, corpus_directory, prologue, variant):
    root = test262 if row["origin"] == "test262" else corpus_directory
    with open(os.path.join(root, *row["path"].split("/")), "rb") as handle:
        data = handle.read()
    if hashlib.sha256(data).hexdigest() != row["sha256"]:
        raise SystemExit(f"spancheck: {row['id']}: SHA-256 differs from the manifest")
    text = data.decode("utf-8")
    return (prologue + text) if variant == "strict" else text


class Source:
    """The source as UTF-16 code units, the offsets the dumps use."""

    def __init__(self, text):
        self.units = text.encode("utf-16-le")
        self.length = len(self.units) // 2

    def char(self, i):
        return self.units[2 * i:2 * i + 2].decode("utf-16-le", errors="replace")

    def slice(self, a, b):
        return self.units[2 * a:2 * b].decode("utf-16-le", errors="replace")


TRIVIA = set(" \t\v\f\r\n ﻿  ")


def balanced(text):
    """Whether the brackets of text balance, strings, templates and comments skipped."""
    stack, i, n = [], 0, len(text)
    pairs = {")": "(", "]": "[", "}": "{"}
    while i < n:
        c = text[i]
        if c in "'\"":
            j = i + 1
            while j < n and text[j] != c and text[j] not in "\r\n":
                j += 2 if text[j] == "\\" else 1
            i = j + 1
            continue
        if c == "`":
            j = i + 1
            while j < n and text[j] != "`":
                j += 2 if text[j] == "\\" else 1
            i = j + 1
            continue
        if text.startswith("//", i):
            j = i
            while j < n and text[j] not in "\r\n  ":
                j += 1
            i = j
            continue
        if text.startswith("/*", i):
            j = text.find("*/", i + 2)
            i = n if j < 0 else j + 2
            continue
        if c in "([{":
            stack.append(c)
        elif c in ")]}":
            if not stack or stack.pop() != pairs[c]:
                return False
        i += 1
    return not stack


def agrees(source, kind, start, end):
    if kind == "Program":
        return start == "0" and end == "eof"
    if not (start.isdigit() and (end.isdigit() or end == "eof")):
        return False
    a, b = int(start), source.length if end == "eof" else int(end)
    if b <= a or b > source.length:
        return False
    text = source.slice(a, b)
    if text[0] in TRIVIA or text[-1] in TRIVIA or text.startswith("//") or text.startswith("/*"):
        return False
    return balanced(text)


def blocks(path):
    current, lines = None, []
    with opener(path) as handle:
        for line in handle:
            if line.startswith("=== "):
                if current:
                    yield current, lines
                current, lines = tuple(line[4:].split()), []
            else:
                lines.append(line.rstrip("\n"))
    if current:
        yield current, lines


def show(source, start, end):
    if not start.isdigit():
        return start + ".." + end
    a = int(start)
    b = source.length if end == "eof" else int(end) if end.isdigit() else a
    text = source.slice(min(a, b), max(a, b)) if a != b else ""
    text = text if len(text) <= 120 else text[:57] + " ... " + text[-58:]
    return json.dumps(text, ensure_ascii=False)


def main(argv):
    args = argv[1:]
    try:
        manifest = args[args.index("--manifest") + 1]
        test262 = args[args.index("--test262") + 1]
        rest = [a for i, a in enumerate(args) if a not in ("--manifest", "--test262") and (i == 0 or args[i - 1] not in ("--manifest", "--test262"))]
        dumps, listing = rest
    except (ValueError, IndexError):
        print(__doc__, file=sys.stderr)
        return 2

    rows, prologue = read_manifest(manifest)
    corpus_directory = os.path.dirname(os.path.abspath(manifest))
    pending = {}
    counts = collections.Counter()
    with open(listing, "w", encoding="utf-8", newline="\n") as out:
        out.write("id\tvariant\tline\tkind\tspan-P\tspan-B\tP-agrees\tB-agrees\tclass\ttext-P\ttext-B\n")
        for (ident, variant, entry), lines in blocks(dumps):
            key = (ident, variant)
            if entry not in ("P", "B"):
                continue
            pending.setdefault(key, {})[entry] = lines
            if len(pending[key]) < 2:
                continue
            pair = pending.pop(key)
            p, b = pair["P"], pair["B"]
            if p == b:
                continue
            source = Source(load(rows[ident], test262, corpus_directory, prologue, variant))
            if len(p) != len(b):
                counts["not-span", "-"] += 1
                out.write(f"{ident}\t{variant}\t-\t-\t-\t-\t-\t-\tnot-span\tline count {len(p)}\tline count {len(b)}\n")
                continue
            for number, (x, y) in enumerate(zip(p, b), 1):
                if x == y:
                    continue
                if SPAN.sub("@S", x) != SPAN.sub("@S", y):
                    counts["not-span", "-"] += 1
                    out.write(f"{ident}\t{variant}\t{number}\t-\t-\t-\t-\t-\tnot-span\t{json.dumps(x[:200])}\t{json.dumps(y[:200])}\n")
                    break
                hx, hy = HEAD.match(x), HEAD.match(y)
                kind = hx.group(2) if hx else "token"
                spans_x, spans_y = SPAN.findall(x), SPAN.findall(y)
                for (sx, ex), (sy, ey) in zip(spans_x, spans_y):
                    if (sx, ex) == (sy, ey):
                        continue
                    node_kind = kind if (hx and (sx, ex) == (hx.group(3), hx.group(4))) else "token"
                    pa, ba = agrees(source, node_kind, sx, ex), agrees(source, node_kind, sy, ey)
                    verdict = "S" if (not pa and ba) else "both-agree" if (pa and ba) else "unclassified"
                    counts[verdict, node_kind] += 1
                    out.write(f"{ident}\t{variant}\t{number}\t{node_kind}\t{sx}..{ex}\t{sy}..{ey}\t{str(pa).lower()}\t{str(ba).lower()}\t{verdict}\t{show(source, sx, ex)}\t{show(source, sy, ey)}\n")
    for (verdict, kind), count in sorted(counts.items()):
        print(f"{count}\t{verdict}\t{kind}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
