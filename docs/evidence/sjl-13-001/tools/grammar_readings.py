#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Part A's grammar readings under decision (e') of decision-rule-revised-2026-11-02-second.md: the lines the triage of
the run at 920f18f4 found refused first by one of the catalogue's two generic grammar rows where the classification names
the other.

Usage:
    grammar_readings.py write --report R --classification C --catalogue K --out F
    grammar_readings.py check (the same options)

An input, variant and configuration is listed, reading G, with the row it admits first, if and only if, all read from
files committed before this script:
  1. the classification (classification/classification.tsv.gz) expects it refused, and names exactly one of EX0008
     ("a token that no production admits where it stands") and EX0009 ("a token that the production being parsed
     requires is missing") among the rows that may come first;
  2. the run the triage read (corpus/report.tsv.gz, at 920f18f4579d729819bfcf3c02d18b75c78fd7b5) refused it with the other
     row's shared code first (the catalogue's, Broiler.ECMAScript/docs/frontend/early-errors.tsv).
The row admitted is that other row. `write` writes the list; `check` rebuilds it and compares it byte for byte.
"""
import argparse
import gzip
import sys

PAIR = ("EX0008", "EX0009")


def build(args):
    codes = {}
    with open(args.catalogue, encoding="utf-8") as handle:
        lines = handle.read().split("\n")
    columns = lines[0].split("\t")
    for line in lines[1:]:
        if line:
            row = dict(zip(columns, line.split("\t")))
            codes[row["id"]] = row["shared_code"]
    first_code = {}
    with gzip.open(args.report, "rt", encoding="utf-8") as handle:
        for line in handle:
            if line.startswith("#") or line.startswith("id\t"):
                continue
            fields = line.rstrip("\n").split("\t")
            if fields[3] == "refused":
                first_code[(fields[0], fields[1], fields[2])] = fields[4]
    out = []
    with gzip.open(args.classification, "rt", encoding="utf-8") as handle:
        columns = None
        for line in handle:
            if line.startswith("#"):
                continue
            fields = line.rstrip("\n").split("\t")
            if columns is None:
                columns = fields
                continue
            row = dict(zip(columns, fields))
            if row["expect"] != "refused":
                continue
            named = [r for r in PAIR if r in row["first"].split(";")]
            if len(named) != 1:
                continue
            other = PAIR[1] if named[0] == PAIR[0] else PAIR[0]
            key = (row["id"], row["variant"], row["config"])
            if first_code.get(key) == codes[other]:
                out.append("\t".join([row["id"], row["variant"], row["config"], "G", other]))
    header = ["# Part A's grammar readings, decision (e') of decision-rule-revised-2026-11-02-second.md; written by",
              "# tools/grammar_readings.py from the classification, the catalogue and the run the triage read",
              "# (corpus/report.tsv.gz, 920f18f4). Never edited by hand.",
              "id\tvariant\tconfig\treading\tadmit"]
    return "\n".join(header + out) + "\n"


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    sub = parser.add_subparsers(dest="command", required=True)
    for name in ("write", "check"):
        c = sub.add_parser(name)
        c.add_argument("--report", required=True)
        c.add_argument("--classification", required=True)
        c.add_argument("--catalogue", required=True)
        c.add_argument("--out", required=True)
    args = parser.parse_args()
    text = build(args)
    if args.command == "write":
        with open(args.out, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(text)
        print("grammar_readings: %d lines written" % (text.count("\n") - 4))
        return 0
    with open(args.out, encoding="utf-8") as handle:
        committed = handle.read()
    if committed != text:
        print("grammar_readings: %s differs from the list rebuilt" % args.out)
        return 1
    print("grammar_readings: %s rebuilt byte for byte (%d lines)" % (args.out, text.count("\n") - 4))
    return 0


if __name__ == "__main__":
    sys.exit(main())
