#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Part A's comparison (docs/evidence/sjl-13-001/decision-rule.md): the corpus runner's report against the classification.

Usage:
    compare_corpus.py compare --report R --classification C --catalogue K --nesting V=64,J=512 --tree-depth 10000 --out DIR
        [--provider-readings P --classification-readings Q [--limit-readings L --grammar-readings G]]
    compare_corpus.py control --report R --classification C --out R2

(Revised 2026-11-02, at stage SJL-13.2f, after review, before any run at the library commit: L is admitted at a reported
depth past the bound, as part A's text reads, not equal to it; and the completeness test compares the manifest's and the
classification's digests with the rule commit's, pinned below, as part A's completeness reads. The comparison first
committed admitted L at a reported depth equal to the bound, reading the library's report of the bound it refused past as
a depth past it, and compared the classification with the report's own record of it.)

(Revised 2026-11-02, at stage SJL-13.2f, under the rule's revision of that day, decision-rule-revised-2026-11-02.md,
before any run it judges. With --provider-readings and --classification-readings, the lists in readings/, it reads part
A as revised: decision (a), a line readings/provider-readings.tsv lists admits EE0019's shared code first beside the
classification's rows (`refused-by-provider-reading`); decision (b), a line readings/classification-readings.tsv lists
admits the row it names first (`refused-by-classification-reading`), or, where it names `either`, is read as
semantics-only (`semantics-only-read-accepted`, `semantics-only-read-refused`); decision (e), where the classification
names EX0008 or EX0009, the other's shared code is admitted too (`refused-by-grammar-pair`); decision (f), a refusal at a
limit reported at a depth equal to its bound is admitted as L (`L-nesting-at-bound`, `L-tree-at-bound`). Each list's
SHA-256 is written to the summary. Without the two options it reads the rule as first written, as before.)

(Revised 2026-11-02, at stage SJL-13.2f, under the rule's second revision of that day,
decision-rule-revised-2026-11-02-second.md, after review and before any run it judges. With --limit-readings and
--grammar-readings as well, the lists readings/limit-readings.tsv and readings/grammar-readings.tsv, it reads part A as
revised twice: decision (f') replaces (f), so that a refusal at a limit reported at a depth equal to its bound is admitted
as L only for a line limit-readings.tsv lists with that limit, a line whose input's own source tools/limit_readings.py
measured past the bound (`L-nesting-measured`, `L-tree-measured`), and no longer for any line at its bound; decision (e')
replaces (e), so that EX0008's or EX0009's shared code is admitted for the other only for a line grammar-readings.tsv
lists with that row (`refused-by-grammar-reading`), and no longer wherever the classification names either. Both lists'
SHA-256 are written to the summary; decisions (a) and (b) are read as before.)

`compare` reads the report of tools/frontend-corpus in the fields part A's configuration lists (the outcome, and for a
refusal the first diagnostic's shared code and the depth the library reports), the classification
(classification/classification.tsv.gz) and the catalogue's shared codes (Broiler.ECMAScript/docs/frontend/early-errors.tsv),
and holds no expectation of its own: every expectation is the classification's. The two limits' parameters are the
configurations' (part A: the nesting parameter V and J set, and the library's tree-depth constant), given on the command
line as the run took them. Per input, variant and configuration it gives one reason:

- admitted: `accepted` (an expected acceptance accepted); `refused-as-classified` (an expected refusal refused with a
  first code of the rows the classification names); `semantics-only-accepted` and `semantics-only-refused` (an input the
  classification marks `either`); `L-nesting` and `L-tree` (an expected acceptance refused with EX0011's shared code at
  a reported depth past the configuration's nesting parameter, or with EX0012's at a reported depth past the tree-depth
  constant, as part A reads; a refusal at either limit reported at a depth not past its bound, as the library reports
  the bound it refused past, is `expected-acceptance-refused`);
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

# The rule commit's digests (238069ab8150858a112461b42b95c1f49c1f4eb1, Broiler.JS#1081): part A's completeness reads "the
# manifest's digest and the classification's equal to the rule commit's".
RULE_COMMIT = "238069ab8150858a112461b42b95c1f49c1f4eb1"
RULE_MANIFEST_SHA256 = "1c5342104766f14d0c17d950537e4ed078a0c438c3652f8c65bff8c6c347e9cc"
RULE_CLASSIFICATION_SHA256 = "753b54fee1a530a91d2c58400eb52e700221fddbb4ed9670d4362f6d268a512f"


def past(depth, bound):
    """Whether a reported depth is past a bound: both whole numbers, the depth greater."""
    return depth.isdigit() and bound.isdigit() and int(depth) > int(bound)


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


def read_readings(path):
    """A readings list (readings/): its lines by (id, variant, config), each with its last column."""
    lines = {}
    if path is None:
        return lines
    with open(path, encoding="utf-8") as handle:
        for line in handle.read().split("\n"):
            if not line or line.startswith("#") or line.startswith("id\t"):
                continue
            fields = line.split("\t")
            lines[(fields[0], fields[1], fields[2])] = fields[-1]
    return lines


def read_limit_readings(path):
    """The limit readings list: the set of (id, variant, config, limit) it names, each checked to be past its bound."""
    listed = set()
    if path is None:
        return listed
    with open(path, encoding="utf-8") as handle:
        for line in handle.read().split("\n"):
            if not line or line.startswith("#") or line.startswith("id\t"):
                continue
            ident, variant, config, measure, bound, limit = line.split("\t")
            if not past(measure, bound):
                sys.exit("compare_corpus: %s lists %s at a measure not past its bound" % (path, ident))
            listed.add((ident, variant, config, limit, bound))
    return listed


def compare(args):
    nesting = dict(item.split("=") for item in args.nesting.split(","))
    tree_depth = args.tree_depth
    codes = {row["id"]: row["shared_code"] for row in read_table(args.catalogue)}
    nesting_code, tree_code = codes["EX0011"], codes["EX0012"]
    revised = args.provider_readings is not None and args.classification_readings is not None
    provider_readings = read_readings(args.provider_readings)
    classification_readings = read_readings(args.classification_readings)
    grammar_pair = {"EX0008", "EX0009"}
    second = revised and args.limit_readings is not None and args.grammar_readings is not None
    limit_readings = read_limit_readings(args.limit_readings)
    grammar_readings = read_readings(args.grammar_readings)
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
            first = row["first"].split(";") if row["first"] != "-" else []
            allowed = {codes[r] for r in first if r in codes}
            reading = classification_readings.get(key)
            if outcome in ("crashed", "absent"):
                reason = outcome
            elif row["expect"] == "either":
                reason = "semantics-only-" + outcome
            elif reading == "either":
                reason = "semantics-only-read-" + outcome
            elif row["expect"] == "accepted":
                if outcome == "accepted":
                    reason = "accepted"
                elif code == nesting_code and past(depth, nesting[row["config"]]):
                    reason = "L-nesting"
                elif code == tree_code and past(depth, tree_depth):
                    reason = "L-tree"
                elif second and code == nesting_code and depth == nesting[row["config"]] and key + ("nesting", nesting[row["config"]]) in limit_readings:
                    reason = "L-nesting-measured"
                elif second and code == tree_code and depth == tree_depth and key + ("tree", tree_depth) in limit_readings:
                    reason = "L-tree-measured"
                elif revised and not second and code == nesting_code and depth == nesting[row["config"]]:
                    reason = "L-nesting-at-bound"
                elif revised and not second and code == tree_code and depth == tree_depth:
                    reason = "L-tree-at-bound"
                else:
                    reason = "expected-acceptance-refused"
            else:
                if outcome == "accepted":
                    reason = "expected-refusal-accepted"
                elif code in allowed:
                    reason = "refused-as-classified"
                elif key in provider_readings and code == codes[provider_readings[key]]:
                    reason = "refused-by-provider-reading"
                elif reading is not None and code == codes[reading]:
                    reason = "refused-by-classification-reading"
                elif second and key in grammar_readings and code == codes[grammar_readings[key]]:
                    reason = "refused-by-grammar-reading"
                elif revised and not second and grammar_pair & set(first) and code in {codes[r] for r in grammar_pair}:
                    reason = "refused-by-grammar-pair"
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
    summary.append("rule-revision\t" + ("decision-rule-revised-2026-11-02-second.md" if second else "decision-rule-revised-2026-11-02.md" if revised else "-"))
    summary.append("provider-readings\t" + (sha256(args.provider_readings) if args.provider_readings else "-"))
    summary.append("classification-readings\t" + (sha256(args.classification_readings) if args.classification_readings else "-"))
    summary.append("report-provider-readings\t" + header.get("provider-readings", "-"))
    if second:
        summary.append("limit-readings\t" + sha256(args.limit_readings))
        summary.append("grammar-readings\t" + sha256(args.grammar_readings))
    classification_digest = sha256(args.classification)
    manifest_agrees = header.get("manifest") == RULE_MANIFEST_SHA256 and inputs.get("conformance/parse/corpus.manifest") == RULE_MANIFEST_SHA256
    classification_agrees = classification_digest == RULE_CLASSIFICATION_SHA256 and header.get("classification") == RULE_CLASSIFICATION_SHA256
    summary.append("rule-commit\t" + RULE_COMMIT)
    summary.append("rule-manifest\t" + RULE_MANIFEST_SHA256)
    summary.append("rule-classification\t" + RULE_CLASSIFICATION_SHA256)
    summary.append("manifest-agrees\t" + ("yes" if manifest_agrees else "no"))
    summary.append("classification-agrees\t" + ("yes" if classification_agrees else "no"))
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
    complete = manifest_agrees and classification_agrees
    if revised:
        # The report's provider read the list this comparison reads (decision (a) of the revision of 2026-11-02).
        readings_agree = header.get("provider-readings") == sha256(args.provider_readings)
        summary_note = "provider-readings-agree\t" + ("yes" if readings_agree else "no")
        with open(os.path.join(args.out, "summary.txt"), "a", encoding="utf-8", newline="\n") as handle:
            handle.write(summary_note + "\n")
        complete = complete and readings_agree
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
    c.add_argument("--provider-readings")
    c.add_argument("--classification-readings")
    c.add_argument("--limit-readings")
    c.add_argument("--grammar-readings")
    k = sub.add_parser("control")
    k.add_argument("--report", required=True)
    k.add_argument("--classification", required=True)
    k.add_argument("--out", required=True)
    args = parser.parse_args()
    return compare(args) if args.command == "compare" else control(args)


if __name__ == "__main__":
    sys.exit(main())
