#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Part F's listing, per semantics row, of the library's site that reports it (bundle sjl-15-001's rule, part F's
configuration: "a listing, per semantics row, of the library's site that reports it, read from the library's code and its
stage table"; decision D-js-part-f-tools). It judges nothing.

Usage:
    row_sites.py --stages FILE --frontend DIR --semantic-rows FILE --vectors DIR --rule FILE [--out FILE]

FILE for --stages is the stage table Broiler.ECMAScript/docs/frontend/stages.tsv; DIR for --frontend is the library's
project folder Broiler.ECMAScript/Broiler.ECMAScript.Frontend (its own sources, not its tests); --semantic-rows is its
SemanticRows.cs; --vectors is conformance/vectors/semantics; --rule is the bundle's decision-rule.md.

For each row of the stage table whose stage is `semantics`, in the table's order, it lists:
- a `row` line: the row, the table's `basis` column (never its engines' `vm_site` or `js_site` columns), whether
  SemanticRows.cs has an entry for it ("entry" with its line, or "no entry"), and whether a `semantics` vector's
  identifier names it ("vector" or "no vector");
- a `site` line for every line of the library's sources, outside SemanticRows.cs, that names `SemanticRows.<row>`: its
  path and line, "reports here" where the same line calls `Report(`, else "report not tied by this listing", and the
  line's text;
- for a row with no such line: a `no-site` line, "carried" with its SemanticRows.cs entry's line where it has one, or
  "run-time" where the table's basis is `run-time`;
- a `label` line for each label below that applies, which names a fact of the code or the corpus and judges nothing;
- a `reason` line for a row named as no row the model can report, quoting the committed line that states the reason,
  with its path and line.
Then the stage table's `rows` cell of EA0008 verbatim, with "stale for the library: a site exists" where the library
has a site for it. It exits 0, or 2 when an input cannot be read.
"""
import argparse
import os
import re
import sys

# Labels predeclared before any run of part F (decision D-js-part-f-tools); each names a fact, none decides.
LABELS = {
    "EE0021": "site; no source reaches it",
    "EA0008": "site; reachable only with Annex B off; no vector",
    "EE0170": "site; no vector names it",
    "EE0171": "site; no vector names it",
    "EP0017": "no site, carried",
    "EP0018": "no site, carried",
}

# The committed lines that state why a row is named as no row the model can report, found by their words.
REASONS = {
    "EA0006": ("rule", "the stage table's rows that only a run reaches"),
    "EP0017": ("semantic-rows", "EP0017 and EP0018 have no report of their own"),
    "EP0018": ("semantic-rows", "EP0017 and EP0018 have no report of their own"),
}


def read_lines(path):
    with open(path, encoding="utf-8") as handle:
        return handle.read().split("\n")


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    for name in ("--stages", "--frontend", "--semantic-rows", "--vectors", "--rule"):
        parser.add_argument(name, required=True)
    parser.add_argument("--out")
    args = parser.parse_args()

    try:
        stage_lines = read_lines(args.stages)
        semantic_rows = read_lines(args.semantic_rows)
        rule = read_lines(args.rule)
        sources = []
        for root, _, files in os.walk(args.frontend):
            for name in files:
                if name.endswith(".cs"):
                    sources.append(os.path.join(root, name))
        sources.sort()
        vector_files = sorted(os.path.join(args.vectors, n) for n in os.listdir(args.vectors) if n.endswith(".vectors"))
        vector_rows = set()
        for path in vector_files:
            for line in read_lines(path):
                match = re.match(r"^id: semantics/[^/]+/([^/]+)/", line)
                if match:
                    vector_rows.add(match.group(1).upper())
    except OSError as error:
        sys.stderr.write("row_sites.py: %s\n" % error)
        return 2

    header = stage_lines[0].split("\t")
    column = {name: i for i, name in enumerate(header)}
    rows = [line.split("\t") for line in stage_lines[1:] if line]
    semantics = [row for row in rows if row[column["stage"]] == "semantics"]
    entries = {}
    for number, line in enumerate(semantic_rows, 1):
        match = re.search(r"static readonly LexicalRefusal (\w+)\s*=", line)
        if match:
            entries[match.group(1)] = number

    semantic_rows_name = os.path.basename(args.semantic_rows)
    sites = {}
    for path in sources:
        if os.path.basename(path) == semantic_rows_name:
            continue
        relative = os.path.relpath(path, os.path.dirname(os.path.dirname(os.path.abspath(args.frontend))))
        for number, line in enumerate(read_lines(path), 1):
            for row in re.findall(r"SemanticRows\.(E[EAP]\d{4})\b", line):
                tie = "reports here" if "Report(" in line else "report not tied by this listing"
                sites.setdefault(row, []).append("%s:%d\t%s\t%s" % (relative.replace(os.sep, "/"), number, tie, line.strip()))

    def quote(source, words):
        lines, path = (rule, args.rule) if source == "rule" else (semantic_rows, args.semantic_rows)
        for number, line in enumerate(lines, 1):
            if words in line:
                return "%s:%d\t%s" % (path.replace(os.sep, "/"), number, line.strip())
        return "-\tnot found: " + words

    out = ["# Per semantics row of the stage table, the library's sites (bundle sjl-15-001's part F; this listing judges nothing)"]
    for row in semantics:
        name = row[0]
        entry = ("entry\t%s:%d" % (args.semantic_rows.replace(os.sep, "/"), entries[name])) if name in entries else "no entry\t-"
        vector = "vector" if name in vector_rows else "no vector"
        out.append("row\t%s\tbasis\t%s\t%s\t%s" % (name, row[column["basis"]], entry, vector))
        for site in sites.get(name, []):
            out.append("site\t%s\t%s" % (name, site))
        if name not in sites:
            if name in entries:
                out.append("no-site\t%s\tcarried\t%s:%d" % (name, args.semantic_rows.replace(os.sep, "/"), entries[name]))
            elif row[column["basis"]] == "run-time":
                out.append("no-site\t%s\trun-time" % name)
            else:
                out.append("no-site\t%s\t-" % name)
        if name in LABELS:
            out.append("label\t%s\t%s" % (name, LABELS[name]))
        if vector == "no vector" and LABELS.get(name, "").find("vector") < 0:
            out.append("label\t%s\tno vector" % name)
        if name in REASONS:
            out.append("reason\t%s\t%s" % (name, quote(*REASONS[name])))

    ea0008 = next((row for row in semantics if row[0] == "EA0008"), None)
    if ea0008 is not None:
        stale = "stale for the library: a site exists" if "EA0008" in sites else "-"
        out.append("stage-table-cell\tEA0008\trows\t%s\t%s" % (ea0008[column["rows"]], stale))

    text = "\n".join(out) + "\n"
    if args.out:
        with open(args.out, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(text)
    sys.stdout.write(text)
    return 0


if __name__ == "__main__":
    sys.exit(main())
