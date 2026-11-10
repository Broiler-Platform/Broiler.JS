#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Part C's table of V, R, Z8, O, H and L19 lines for bundle sjl-9-001 (milestone SJL-9.6).

    python3 -I docs/evidence/sjl-9-001/datasets/classify.py probe --out <collations-probe.js>
    python3 -I docs/evidence/sjl-9-001/datasets/classify.py table --after datasets/after --base datasets/base \
        --reference datasets/reference --collations datasets/after/collations-probe.txt --out datasets/after

The rule's part C (decision-rule.md, as decision-rule-revised-2026-11-10.md restates it with class L19) compares each
line after with its expected answer, Broiler.VM's, and reads every line that still differs against the same line at
the base and against the reference's line. This script reads the harness's outputs, run_reference_datasets.py's
<program>.lines.tsv for the after run and the base, the reference run's <program>.answer.txt, and, for the Collator's
reading of class V, the probe this script writes (`probe`), run in the shell of the after run. It writes, for every
line that differs from its expected answer, the reading of the rule it meets, or FAILURE where it meets none:

  O    a line of segments.js whose answer after equals its answer at the base; read first for every line of it;
  L19  locales:128, the line whose key is "11 fr-CA hourCycles", only when its answer after is exactly
       "11 fr-CA hourCycles => [h23,h12]";
  Z8   a line of timezones.js whose difference is only the identifier the formatter reports: the answer after is the
       expected line with its resolved identifier, a primary other than the key, replaced by the key;
  V    a line whose expected answer comes from a divergence in a group divergence-groups.tsv marks coverage, read
       with the README's dated corrections as the rule commit holds them (line 49 read as spec), whose answer after
       equals the retained answer (V-retained); or, in the numbering-system group (line 43), equals the expected
       answer with each latn number symbol of the locale replaced by arab's CLDR 48.2.0 symbol (V-symbols); or, in
       the Collator's group (line 50), is a collations line of a Locale with no co keyword equal to the probe's
       sorted list (V-collator). A line the group's note names as also carrying a difference the pinned text
       decides is admitted only by the line the note names (line 43's two resolvedOptions lines by none, its
       years-and-months and hours-minutes-seconds lines by V-symbols alone);
  R    a line of V's kind that no reading of V admits and that equals the reference's line;
  H    not assigned by this script: a line no other class admits is written FAILURE, and a person reads it.

The script decides no verdict; the README reads the table under the rule.
"""

from __future__ import annotations

import argparse
import importlib.util
import json
import re
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
BUNDLE = HERE.parent
REPO = BUNDLE.parents[2]
_spec = importlib.util.spec_from_file_location("harness", REPO / "scripts/conformance/run_reference_datasets.py")
harness = importlib.util.module_from_spec(_spec)
sys.modules["harness"] = harness
_spec.loader.exec_module(harness)

COLLECTIONS = {"time": REPO / "conformance/datasets/time", "intl": REPO / "conformance/datasets/intl"}
GROUPS = BUNDLE / "divergence-groups.tsv"
# The README's dated corrections of 2026-10-26, as the rule commit holds them: line 49 is read as spec.
CORRECTED = {49: "spec"}
SYMBOLS_GROUP = 43
COLLATOR_GROUP = 50
# Line 43's note: the lines that also carry the draft's sign of the first group, admitted by V-symbols alone, and
# the two resolvedOptions lines, which owe Broiler.VM's line and which class V must not admit.
SIGN_KEYS = ('{"years":-1,"months":-2}', '{"hours":-1,"minutes":-2,"seconds":-3}')
# CLDR 48.2.0's arab symbols for en and de, which carry none of their own: root's (number-locales.txt, und's
# symbols@arab lines), as the program escapes them.
ARAB = {"decimal": "\\u066b", "group": "\\u066c", "minus": "\\u061c-", "plus": "\\u061c+", "percent": "\\u066a\\u061c"}
LATN = {"en": {".": "decimal", ",": "group"}, "de": {",": "decimal", ".": "group"}}
DIGIT = r"\\u066[0-9]"


def groups() -> list[dict]:
    rows = []
    lines = GROUPS.read_text(encoding="utf-8").split("\n")
    header = None
    for number, line in enumerate(lines, start=1):
        if not line or line.startswith("#"):
            continue
        fields = line.split("\t")
        if header is None:
            header = fields
            continue
        row = dict(zip(header, fields))
        first, last = (int(v) for v in row["divergence-lines"].split("-"))
        row.update(number=number, first=first, last=last, read=CORRECTED.get(number, row["class"]))
        rows.append(row)
    return rows


def sources() -> dict[str, tuple[Path, dict]]:
    """Each program's directory and its pin's files, by program name."""
    found = {}
    for directory in COLLECTIONS.values():
        pin = harness.load_datasets(directory)
        for program in pin.programs:
            found.setdefault(program.name, (directory, pin, program))
    return found


def divergence_line(directory: Path, program, key: str) -> int | None:
    """The line of the program's divergences file that the expected line's key names (a later one wins)."""
    if program.divergences == harness.NO_DIVERGENCES:
        return None
    text = harness.read_all_text((directory / program.divergences).read_bytes())
    found = None
    for number, line in enumerate(harness.read_all_lines(text), start=1):
        if line and not line.startswith("#") and harness.line_key(line) == key:
            found = number
    return found


def substitute(expected: str, locale: str) -> str:
    """The expected line with each latn number symbol of the locale, inside a number, replaced by arab's."""
    head, _, answer = expected.partition(" => ")
    latn = LATN[locale]

    def number(match: re.Match) -> str:
        text = match.group(0)
        out = ""
        if text[0] == "-":
            out, text = ARAB["minus"], text[1:]
        elif text[0] == "+":
            out, text = ARAB["plus"], text[1:]
        out += re.sub(r"(?<=[0-9a-f])([.,])(?=\\u066)", lambda m: ARAB[latn[m.group(1)]], text)
        if out.endswith("%"):
            out = out[:-1] + ARAB["percent"]
        return out

    pattern = rf"[-+]?{DIGIT}(?:[.,]?{DIGIT})*%?"
    return head + " => " + re.sub(pattern, number, answer)


def probe_script() -> str:
    text = (COLLECTIONS["intl"] / "locales.js").read_text(encoding="utf-8")
    cases = re.search(r"^var cases = \[.*?^\];$", text, re.M | re.S).group(0)
    escape = re.search(r"^function escape\(text\) \{.*?^\}$", text, re.M | re.S).group(0)
    return f"""// Bundle sjl-9-001, part C, class V's reading of the Collator's group (divergence-groups.tsv line 50): for each
// case of locales.js whose Locale has no co keyword, the collations this engine's Intl.Collator resolves for it, each
// value c of Intl.supportedValuesOf("collation") for which new Intl.Collator(tag, {{collation: c}}).resolvedOptions()
// .collation is c, tag being the Locale's toString(), sorted in code-unit order. Written by datasets/classify.py from
// conformance/datasets/intl/locales.js's cases, which it copies.
{cases}

{escape}

var lines = [];
var values = Intl.supportedValuesOf('collation');
for (var c = 0; c < cases.length; c++) {{
  var head = (c + 1) + ' ' + cases[c][0] + (cases[c][1] ? ' ' + JSON.stringify(cases[c][1]) : '');
  try {{
    var locale = new Intl.Locale(cases[c][0], cases[c][1]);
    if (locale.collation !== undefined) {{ lines.push(head + ' collations (co ' + locale.collation + ', not read)'); continue; }}
    var tag = locale.toString();
    var list = values.filter(function (v) {{ return new Intl.Collator(tag, {{ collation: v }}).resolvedOptions().collation === v; }});
    list.sort(function (a, b) {{ return a < b ? -1 : a > b ? 1 : 0; }});
    lines.push(head + ' collations => [' + list.join(',') + '] tag=' + tag);
  }} catch (error) {{
    lines.push(head + ' collations => ' + error.name);
  }}
}}
console.log('supportedValuesOf(collation) => [' + values.join(',') + ']\\n' + lines.map(escape).join('\\n'));
"""


def table(args: argparse.Namespace) -> int:
    after_dir, base_dir, ref_dir, out = (Path(p).resolve() for p in (args.after, args.base, args.reference, args.out))
    summary = json.loads((after_dir / "summary.json").read_text(encoding="utf-8"))
    all_groups = groups()
    found = sources()
    collator = {}
    for line in Path(args.collations).read_text(encoding="utf-8").splitlines():
        m = re.match(r"^(.*) collations => (\[.*\]) tag=", line)
        if m:
            collator[m.group(1) + " collations"] = m.group(1) + " collations => " + m.group(2)
    columns = ("program", "line", "key", "class", "reading", "group", "group-class", "source", "expected", "after",
               "base", "reference", "retained")
    rows, counts = [], {}
    for name in summary["programs"]:
        after = harness.read_tsv(after_dir / f"{name}.lines.tsv")
        base = {r["line"]: r for r in harness.read_tsv(base_dir / f"{name}.lines.tsv")}
        reference_path = ref_dir / f"{name}.answer.txt"
        reference = (harness.split_retained(reference_path.read_bytes().decode("utf-8"))
                     if reference_path.exists() else [])
        directory, pin, program = found[name]
        retained = harness.split_retained(harness.read_all_text((directory / program.answer).read_bytes()))
        vm_path = pin.files[program.divergences].source_path if program.divergences != harness.NO_DIVERGENCES else ""
        for row in after:
            if row["equal"] == "yes":
                continue
            number = int(row["line"])
            key, expected, actual = row["key"], row["expected"], row["actual"]
            at_base = base.get(row["line"], {}).get("actual", harness.NO_LINE)
            ref = reference[number - 1] if number <= len(reference) else harness.NO_LINE
            kept = retained[number - 1] if number <= len(retained) else harness.NO_LINE
            group = None
            if row["source"] == "divergence":
                at = divergence_line(directory, program, key)
                group = next((g for g in all_groups if g["file"] == vm_path and at is not None
                              and g["first"] <= at <= g["last"]), None)
            klass, reading = "FAILURE", "no reading of the rule admits it"
            if name == "segments":
                if actual == at_base:
                    klass, reading = "O", "equals its answer at the base"
                else:
                    reading = "a line of segments.js that differs from its answer at the base"
            elif name == "locales" and number == 128 and key == "11 fr-CA hourCycles":
                if actual == "11 fr-CA hourCycles => [h23,h12]":
                    klass, reading = "L19", "exactly the answer class L19 names"
                else:
                    reading = "locales:128 with an answer other than the one class L19 names"
            elif name == "timezones":
                m = re.match(r"^(.*?) => (\S+) \| (.*)$", expected)
                if m and m.group(2) != key and actual == f"{key} => {key} | {m.group(3)}":
                    klass, reading = "Z8", f"only the identifier differs: {m.group(2)} (the primary) for {key} as given"
            if klass == "FAILURE" and group is not None and group["read"] == "coverage" and name not in ("segments",):
                note_only = group["number"] == SYMBOLS_GROUP and any(k in key for k in SIGN_KEYS)
                refused = group["number"] == SYMBOLS_GROUP and "resolvedOptions" in key
                if refused:
                    reading = "line 43's note: a resolvedOptions line owes Broiler.VM's line"
                elif not note_only and actual == kept:
                    klass, reading = "V", "V-retained: equals the retained answer"
                elif group["number"] == SYMBOLS_GROUP and actual == substitute(expected, key.split(" ", 1)[0]):
                    klass, reading = "V", "V-symbols: the expected answer with arab's CLDR 48.2.0 symbols for latn's"
                elif group["number"] == COLLATOR_GROUP and key.endswith(" collations") and collator.get(key) == actual:
                    klass, reading = "V", "V-collator: the sorted list this engine's Intl.Collator resolves"
                elif actual == ref:
                    klass, reading = "R", "equals the reference's line"
            counts[klass] = counts.get(klass, 0) + 1
            rows.append([name, str(number), key, klass, reading, str(group["number"]) if group else "-",
                         (group["read"] + (" (read as spec by the dated correction)" if group["number"] in CORRECTED
                                           else "")) if group else "-",
                         row["source"], expected, actual, at_base, ref, kept])
    harness.write_tsv(out / "classification.tsv", columns, rows)
    text = ["Part C's table: every line after that differs from its expected answer, with the reading of the rule it "
            "meets (datasets/classify.py)", ""]
    text += [f"{k}: {v}" for k, v in sorted(counts.items())]
    text += ["", "By program and line:"]
    text += [f"  {r[0]}:{r[1]}\t{r[3]}\t{r[4]}\tgroup {r[5]}" for r in rows]
    (out / "classification.txt").write_text("\n".join(text) + "\n", encoding="utf-8", newline="\n")
    print("\n".join(text[:2 + len(counts)]))
    return 0 if "FAILURE" not in counts else 1


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    commands = parser.add_subparsers(dest="command", required=True)
    probe = commands.add_parser("probe")
    probe.add_argument("--out", required=True)
    tab = commands.add_parser("table")
    for name in ("after", "base", "reference", "collations", "out"):
        tab.add_argument(f"--{name}", required=True)
    args = parser.parse_args(argv)
    if args.command == "probe":
        Path(args.out).write_text(probe_script(), encoding="utf-8", newline="\n")
        return 0
    return table(args)


if __name__ == "__main__":
    sys.exit(main())
