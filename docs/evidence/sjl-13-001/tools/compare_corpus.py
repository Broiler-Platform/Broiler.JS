#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Part A's comparison (docs/evidence/sjl-13-001/decision-rule.md): the corpus runner's report against the classification.

Usage:
    compare_corpus.py compare --report R --classification C --catalogue K --nesting V=64,J=512 --tree-depth 10000 --out DIR
    compare_corpus.py control --report R --classification C --out R2

`compare` reads the report of tools/frontend-corpus in the fields part A's configuration lists (the outcome, and for a
refusal the first diagnostic's shared code and the depth the library reports), the classification
(classification/classification.tsv.gz) and the catalogue's shared codes (Broiler.ECMAScript/docs/frontend/early-errors.tsv),
and holds no expectation of its own: every expectation is the classification's. The two limits' parameters are the
configurations' (part A: the nesting parameter V and J set, and the library's tree-depth constant), given on the command
line as the run took them. Per input, variant and configuration it gives one reason:

- admitted: `accepted` (an expected acceptance accepted); `refused-as-classified` (an expected refusal refused with a
  first code of the rows the classification names); `semantics-only-accepted` and `semantics-only-refused` (an input the
  classification marks `either`); `L-nesting` and `L-tree` (an expected acceptance refused with EX0011's shared code at
  the configuration's nesting parameter, or with EX0012's at the tree-depth constant: the depth the library reports is
  the bound it refused past);
- failures: `crashed`; `absent`; `expected-acceptance-refused` (outside L); `expected-refusal-accepted`;
  `first-code-not-allowed`; `missing` (in the classification, not in the report); `extra` (in the report, not in the
  classification).

It writes DIR/comparison.tsv.gz (every line with its reason), DIR/summary.txt (the counts by reason and configuration,
the digests read, and every failure) and exits 0 when no failure is found. `control` writes a copy of a report with one
outcome flipped (the first line the classification expects refused that was refused, made accepted) and one first code
changed to JSE0000, which no row has (the next such line), for the rule's control: the comparison must report both as
failures.
"""
import argparse
import collections
import gzip
import hashlib
import sys


def read_table(path):
    with open(path, encoding="utf-8") as handle:
        lines = handle.read().split("\n")
    columns = lines[0].split("\t")
    return [dict(zip(columns, line.split("\t"))) for line in lines[1:] if line]


def read_report(path):
    header, rows = {}, []
    with open(path, encoding="utf-8") as handle:
        for line in handle.read().split("\n"):
            if not line:
                continue
            if line.startswith("#% "):
                key, _, value = line[3:].partition("\t")
                header[key] = value
                continue
            if line.startswith("id\t"):
                continue
            rows.append(line.split("\t"))
    return header, rows


def read_classification(path):
    inputs, rows, columns = {}, [], None
    with gzip.open(path, "rt", encoding="utf-8") as handle:
        for line in handle.read().split("\n"):
            if not line:
                continue
            if line.startswith("#% input\t"):
                parts = line.split("\t")
                inputs[parts[1]] = parts[2]
                continue
            if line.startswith("#"):
                continue
            if columns is None:
                columns = line.split("\t")
                continue
            rows.append(dict(zip(columns, line.split("\t"))))
    return inputs, rows


def sha256(path):
    with open(path, "rb") as handle:
        return hashlib.sha256(handle.read()).hexdigest()


def compare(args):
    nesting = dict(item.split("=") for item in args.nesting.split(","))
    tree_depth = args.tree_depth
    codes = {row["id"]: row["shared_code"] for row in read_table(args.catalogue)}
    nesting_code, tree_code = codes["EX0011"], codes["EX0012"]
    inputs, expected = read_classification(args.classification)
    header, report = read_report(args.report)

    by_key = {}
    extra = []
    for fields in report:
        key = (fields[0], fields[1], fields[2])
        if key in by_key:
            extra.append(key)
        by_key[key] = fields

    reasons = collections.Counter()
    lines, failures = [], []
    seen = set()
    for row in expected:
        key = (row["id"], row["variant"], row["config"])
        seen.add(key)
        fields = by_key.get(key)
        if fields is None:
            reason, outcome, code, depth = "missing", "-", "-", "-"
        else:
            outcome, code, depth = fields[3], fields[4], fields[7]
            allowed = {codes[r] for r in row["first"].split(";") if r in codes} if row["first"] != "-" else set()
            if outcome in ("crashed", "absent"):
                reason = outcome
            elif row["expect"] == "either":
                reason = "semantics-only-" + outcome
            elif row["expect"] == "accepted":
                if outcome == "accepted":
                    reason = "accepted"
                elif code == nesting_code and depth == nesting[row["config"]]:
                    reason = "L-nesting"
                elif code == tree_code and depth == tree_depth:
                    reason = "L-tree"
                else:
                    reason = "expected-acceptance-refused"
            else:
                if outcome == "accepted":
                    reason = "expected-refusal-accepted"
                elif code in allowed:
                    reason = "refused-as-classified"
                else:
                    reason = "first-code-not-allowed"
        reasons[(reason, row["config"])] += 1
        line = "\t".join([key[0], key[1], key[2], row["expect"], row["first"], outcome, code, depth, reason])
        lines.append(line)
        if reason in ("crashed", "absent", "missing", "expected-acceptance-refused", "expected-refusal-accepted", "first-code-not-allowed"):
            failures.append(line)
    for key in by_key:
        if key not in seen:
            extra.append(key)
    for key in extra:
        failures.append("\t".join(key) + "\t-\t-\t-\t-\t-\t-\textra")
        reasons[("extra", key[2])] += 1

    import os
    os.makedirs(args.out, exist_ok=True)
    with gzip.open(os.path.join(args.out, "comparison.tsv.gz"), "wt", encoding="utf-8", newline="\n") as handle:
        handle.write("id\tvariant\tconfig\texpect\tfirst\toutcome\tcode\tdepth\treason\n")
        for line in lines:
            handle.write(line + "\n")

    summary = []
    summary.append("report\t" + sha256(args.report))
    summary.append("report-commit\t" + header.get("commit", "-"))
    summary.append("report-manifest\t" + header.get("manifest", "-"))
    summary.append("classification\t" + sha256(args.classification))
    summary.append("classification-manifest-input\t" + inputs.get("conformance/parse/corpus.manifest", "-"))
    summary.append("report-classification\t" + header.get("classification", "-"))
    summary.append("catalogue\t" + sha256(args.catalogue))
    summary.append("manifest-agrees\t" + ("yes" if header.get("manifest") == inputs.get("conformance/parse/corpus.manifest") else "no"))
    summary.append("classification-agrees\t" + ("yes" if header.get("classification") == sha256(args.classification) else "no"))
    summary.append("expected\t%d" % len(expected))
    summary.append("reported\t%d" % len(report))
    for (reason, config), count in sorted(reasons.items()):
        summary.append("reason\t%s\t%s\t%d" % (config, reason, count))
    summary.append("failures\t%d" % len(failures))
    summary.extend("failure\t" + line for line in failures)
    with open(os.path.join(args.out, "summary.txt"), "w", encoding="utf-8", newline="\n") as handle:
        handle.write("\n".join(summary) + "\n")
    print("compare_corpus: %d expected, %d reported, %d failures" % (len(expected), len(report), len(failures)))
    for line in failures[:50]:
        print("failure\t" + line)
    complete = header.get("manifest") == inputs.get("conformance/parse/corpus.manifest") and header.get("classification") == sha256(args.classification)
    return 0 if not failures and complete else 1


def control(args):
    _, expected = read_classification(args.classification)
    refused = {(r["id"], r["variant"], r["config"]) for r in expected if r["expect"] == "refused"}
    with open(args.report, encoding="utf-8") as handle:
        lines = handle.read().split("\n")
    flipped = changed = None
    for i, line in enumerate(lines):
        if not line or line.startswith("#") or line.startswith("id\t"):
            continue
        fields = line.split("\t")
        if fields[3] != "refused" or (fields[0], fields[1], fields[2]) not in refused:
            continue
        if flipped is None:
            fields[3:8] = ["accepted", "-", "-", "-", "-"]
            flipped = i
        elif changed is None:
            fields[4] = "JSE0000"
            changed = i
        else:
            break
        lines[i] = "\t".join(fields)
    with open(args.out, "w", encoding="utf-8", newline="\n") as handle:
        handle.write("\n".join(lines))
    print("control: line %d's outcome flipped to accepted, line %d's first code changed to JSE0000" % (flipped + 1, changed + 1))
    return 0


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    sub = parser.add_subparsers(dest="command", required=True)
    c = sub.add_parser("compare")
    c.add_argument("--report", required=True)
    c.add_argument("--classification", required=True)
    c.add_argument("--catalogue", required=True)
    c.add_argument("--nesting", required=True)
    c.add_argument("--tree-depth", required=True)
    c.add_argument("--out", required=True)
    k = sub.add_parser("control")
    k.add_argument("--report", required=True)
    k.add_argument("--classification", required=True)
    k.add_argument("--out", required=True)
    args = parser.parse_args()
    return compare(args) if args.command == "compare" else control(args)


if __name__ == "__main__":
    sys.exit(main())
