# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Groups the base run's differing lines by the correspondence row each seems to be a case of: observations.txt.

An observation by the author of milestone SJL-10.2 for whoever reads the after run, not a classification: the
rule's part C admits no class, and the base is read, not judged. It reads the run's lines reports through the
harness's own reader and writes to standard output. From the repository's root:

    python3 -I docs/evidence/sjl-10-001/datasets/base/observe.py docs/evidence/sjl-10-001/datasets/base \
        > docs/evidence/sjl-10-001/datasets/base/observations.txt

The rows are those of Broiler.VM's correspondence, docs/evidence/sjl-10-001/correspondence.md in Broiler.VM,
which the rule names: "every line this engine answered otherwise when the correspondence ran the two programs is
a row SJL-10.5 corrects - in temporal.js row D6, and in temporal-calendars.js rows C5, C15, C16 and C17, F4 and
D11 - and the correspondence classifies no other difference on them". A line is put under a row by the shape of
its operation, the shape the row's text names; a differing line no shape matches is listed under no row, and
is a finding against that sentence.
"""

from __future__ import annotations

from collections import Counter
from pathlib import Path
import re
import sys

REPOSITORY = Path(__file__).resolve().parents[5]
sys.path.insert(0, str(REPOSITORY / "scripts" / "conformance"))
import run_reference_datasets as H  # noqa: E402

# Each row: its name, what the correspondence's row says, the number of lines the correspondence's run of the
# programs found under it (correspondence.md, "Other facts", and the row's own text), and the shape of the
# operation (the line's key, the calendar dropped for temporal-calendars.js).
ROWS = {
    "temporal": [
        ("D6", "round with a plain-date relativeTo: the total months added to relativeTo and re-derived, where the "
               "nudge's end duration is kept (P1Y3M expected, P1Y2M30D answered)", 7,
         re.compile(r"^duration \S+ round \w+ \w+ rel \d{4}-\d{2}-\d{2}$")),
    ],
    "temporal-calendars": [
        ("C5", "invalid month codes constrained: M13 where the calendar has no thirteenth month", 5,
         re.compile(r"^from M13$")),
        ("C15", "the year-month a date gives: the date's own ISO day, not the calendar month's first", 15,
         re.compile(r"^toPlainYearMonth$")),
        ("C16", "the month-day a date gives in the Gregorian family: iso8601, not the date's calendar", 4,
         re.compile(r"^toPlainMonthDay$")),
        ("C17", "the date a month-day gives in buddhist and roc: the calendar year read as an ISO year", 2,
         re.compile(r"^month-day to date$")),
        ("F4", "differences in another calendar with rounding options: the rounding ignored", 55,
         re.compile(r"^until \S+ round months$")),
        ("D11", "round and total relative to a date in another calendar: a plain Error", 22,
         re.compile(r"^duration (round|total) relativeTo$")),
    ],
}


def operation(program: str, key: str) -> tuple[str, str]:
    """The line's calendar (none in temporal.js) and its operation."""
    if program == "temporal-calendars":
        calendar, _, rest = key.partition(" ")
        return calendar, rest
    return "", key


def observe(program: str, file: str, rows: list[dict[str, str]]) -> list[str]:
    differing = [row for row in rows if row["equal"] != "yes"]
    out = [f"{file}: {len(rows)} lines, {len(rows) - len(differing)} equal to the expected answer, "
           f"{len(differing)} differing", ""]
    grouped: dict[str, list[dict[str, str]]] = {name: [] for name, _, _, _ in ROWS[program]}
    unmatched = []
    for row in differing:
        calendar, what = operation(program, row["key"])
        for name, _, _, shape in ROWS[program]:
            if shape.match(what):
                grouped[name].append(row)
                break
        else:
            unmatched.append(row)

    out.append("Each row the rule names, with the lines whose operation has its shape, and the number the")
    out.append("correspondence's run of the program found under it:")
    for name, said, counted, _ in ROWS[program]:
        found = grouped[name]
        out.append(f"  {name:4} {len(found):4}  (the correspondence: {counted}) {said}")
        calendars = Counter(operation(program, row["key"])[0] for row in found)
        if program == "temporal-calendars" and found:
            out.append("             calendars: " + " ".join(f"{c} {n}" for c, n in sorted(calendars.items())))
    out.append(f"  none {len(unmatched):4}  differing lines no row's shape matches"
               + (": FINDINGS, listed below" if unmatched else ""))
    out.append("")
    for name, _, _, _ in ROWS[program]:
        if not grouped[name]:
            continue
        out.append(f"{name}, line by line (number, operation: expected, then this engine's answer):")
        for row in grouped[name]:
            expected = row["expected"].split(" => ", 1)[-1]
            actual = row["actual"].split(" => ", 1)[-1] if row["actual"].startswith(row["key"] + " => ") \
                else row["actual"]
            out.append(f"  {row['line']:>5} {H.tsv_escape(row['key'])}: {H.tsv_escape(expected)} | "
                       f"{H.tsv_escape(actual)}")
        out.append("")
    if unmatched:
        out.append("No row (findings), line by line:")
        for row in unmatched:
            out.append(f"  {row['line']:>5} {H.tsv_escape(row['expected'])} | {H.tsv_escape(row['actual'])}")
        out.append("")
    return out


def main() -> None:
    run = Path(sys.argv[1])
    lines = ["Observations on a run of the reference datasets (bundle sjl-10-001's rule, part C)", "",
             "The differing lines grouped by the row of Broiler.VM's correspondence each seems to be a case of, by",
             "the shape of its operation. An observation, written by observe.py from the lines reports beside it;",
             "not a classification. The rule admits no class, and the base is read, not judged.", ""]
    lines += observe("temporal", "temporal.js", H.read_tsv(run / "temporal.lines.tsv"))
    lines += observe("temporal-calendars", "temporal-calendars.js", H.read_tsv(run / "temporal-calendars.lines.tsv"))
    sys.stdout.write("\n".join(lines).rstrip("\n") + "\n")


if __name__ == "__main__":
    main()
