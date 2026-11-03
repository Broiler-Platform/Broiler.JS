#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Part A's provider readings under decision (a) of decision-rule-revised-2026-11-02.md: which inputs' literals the
corpus runner's regular-expression provider refuses although the classification names no provider row first.

Usage:
    provider_readings.py write --classification C --stages S --catalogue K --js-baseline B --out F
    provider_readings.py check --classification C --stages S --catalogue K --js-baseline B --out F

An input, variant and configuration is listed, with EE0019 as the row the provider names, if and only if, all read
from files committed before this script:
  1. the classification (classification/classification.tsv.gz) expects it refused in that configuration and names no
     row among those that may come first whose stage-table basis (Broiler.ECMAScript/docs/frontend/stages.tsv) is
     `provider`, so that the rule's provider as first written admits every literal of it;
  2. Broiler.JS's SJL-12 after baseline, entry P (docs/evidence/sjl-12-001/after/parse-corpus-c.tsv.gz, an input the
     classification itself was built from), refused that input and variant with its scanner's refusal of a slash it read
     as the start of a regular-expression literal: the message `Unexpected token Divide: / at <line>, <column>`;
  3. in configuration V, the classification's basis also names Broiler.VM's first code (`VB:<code>`), and that code is
     the catalogue's vm_code of a provider row: Broiler.VM's refusal is one its regular-expression validator gives.
The row named is EE0019 (IsValidRegularExpressionLiteral), the catalogue's row for a literal the provider refuses
whatever the pattern's fault. `write` writes the list; `check` rebuilds it in memory and compares it byte for byte.
"""
import argparse
import gzip
import re
import sys

DIVIDE = re.compile(r"Unexpected token Divide: / at \d+, \d+")


def table(path):
    with open(path, encoding="utf-8") as handle:
        lines = handle.read().split("\n")
    columns = lines[0].split("\t")
    return [dict(zip(columns, line.split("\t"))) for line in lines[1:] if line]


def build(args):
    basis = {row["id"]: row["basis"] for row in table(args.stages)}
    provider_rows = {rid for rid, b in basis.items() if b == "provider"}
    vm_codes = set()
    for row in table(args.catalogue):
        if row["id"] in provider_rows:
            vm_codes.update(code for code in row["vm_code"].split(";") if re.fullmatch(r"\d+", code))
    refused_at_slash = set()
    with gzip.open(args.js_baseline, "rt", encoding="utf-8") as handle:
        for line in handle:
            if line.startswith("#"):
                continue
            fields = line.rstrip("\n").split("\t")
            if len(fields) >= 6 and fields[2] == "P" and fields[3] == "refused" and DIVIDE.fullmatch(fields[5]):
                refused_at_slash.add((fields[0], fields[1]))
    lines = []
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
            if any(r in provider_rows for r in row["first"].split(";")):
                continue
            if (row["id"], row["variant"]) not in refused_at_slash:
                continue
            if row["config"] == "V":
                codes = {b[3:] for b in row["basis"].split(";") if b.startswith("VB:")}
                if not codes & vm_codes:
                    continue
            lines.append("\t".join([row["id"], row["variant"], row["config"], "EE0019"]))
    header = ["# Part A's provider readings, decision (a) of decision-rule-revised-2026-11-02.md; written by",
              "# tools/provider_readings.py from the classification, the stage table, the catalogue and Broiler.JS's SJL-12",
              "# after baseline (entry P). Never edited by hand.",
              "id\tvariant\tconfig\trow"]
    return "\n".join(header + lines) + "\n"


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    parser.add_argument("command", choices=["write", "check"])
    for option in ("--classification", "--stages", "--catalogue", "--js-baseline", "--out"):
        parser.add_argument(option, required=True)
    args = parser.parse_args()
    text = build(args)
    if args.command == "write":
        with open(args.out, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(text)
        print("provider_readings: %d lines" % (text.count("\n") - 4))
        return 0
    with open(args.out, encoding="utf-8") as handle:
        same = handle.read() == text
    print("provider_readings: %s" % ("the list is the script's" if same else "the list differs from the script's"))
    return 0 if same else 1


if __name__ == "__main__":
    sys.exit(main())
