#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Sort part C's differing lines after the cut-over by the kind of each difference, for the rule's classes.

    python3 -I classify.py --after <the after run> --compare <its comparison with the base> \
        --shell <the after run's BroilerJS.dll> --out <a directory for the probe and its answer>

Written at SJL-8.7 for bundle sjl-8-001's rule, part C (docs/evidence/sjl-8-001/decision-rule.md). The rule's
classes are:

- D1, the formatter's resolved identifier: a line whose difference is only the timeZone that
  Intl.DateTimeFormat's resolvedOptions reports for a non-primary name (row Z8);
- D2, zone display: a line whose difference is only a zone's display text, such as the longOffset name's
  seconds (row Z16);
- D3, outside the area: a line whose operation reads neither a zone nor a calendar table year, whose answer
  after equals its answer at the base.

This script assigns nothing. It splits each differing line of timezones.js into its two parts, the resolved
identifier and the eight longOffset names, and reports which part differs and how:

- the resolved part is marked "resolved-only-Z8" when the shell's answer is the name as written, the expected
  answer is another name, the name is not one Intl.supportedValuesOf('timeZone') lists in the after shell, the
  expected name is one it lists, and the two names are one zone in Temporal (ZonedDateTime.equals);
- a name is marked "seconds-only-Z16" when the expected text is GMT, a sign, hours, minutes and non-zero
  seconds, the shell's text writes one of the two whole minutes either side of that offset with no seconds
  (which one is recorded), and Temporal in the after shell gives the zone, at that instant, the offset the
  expected text writes, to the second.

Anything else in a differing line is marked "other". For temporal.js it lists each differing line with whether
its answer after equals its answer at the base. Its probe runs in the after shell, with TZ=UTC and no LANG, as
the harness runs the programs.
"""

from __future__ import annotations

import argparse
import csv
import json
import os
import re
import subprocess
import sys
from pathlib import Path

# The program's eight instants, written as it writes them.
INSTANTS = ("Date.UTC(1850, 0, 1)", "Date.UTC(1900, 0, 1)", "Date.UTC(1940, 6, 1)", "Date.UTC(1975, 0, 15)",
            "Date.UTC(2000, 6, 1)", "Date.UTC(2026, 0, 15)", "Date.UTC(2026, 6, 15)", "Date.UTC(2150, 6, 1)")
SECONDS = re.compile(r"^GMT([+-])(\d\d):(\d\d):(\d\d)$")
MINUTES = re.compile(r"^GMT(?:([+-])(\d\d):(\d\d))?$")
IDENTIFIER_LINE = re.compile(r"^(?P<name>\S+) => (?P<resolved>\S+) \|(?P<names>( \S+){8})$")


def read_lines(path: Path) -> list[dict]:
    csv.field_size_limit(sys.maxsize)
    with path.open(encoding="utf-8", newline="") as handle:
        return list(csv.DictReader(handle, delimiter="\t", quoting=csv.QUOTE_NONE))


def probe_program(rows: list[tuple[str, str]]) -> str:
    """For each (name, expected resolved name): whether each is listed, whether they are one zone, and the
    name's offset in seconds at each of the program's eight instants, through Temporal."""
    return "\n".join([
        "(function () {",
        "  var listed = {};",
        "  Intl.supportedValuesOf('timeZone').forEach(function (z) { listed[z] = true; });",
        f"  var instants = [{', '.join(INSTANTS)}];",
        f"  var rows = {json.dumps(rows)};",
        "  var out = [];",
        "  rows.forEach(function (row) {",
        "    var line = [row[0], row[1], listed[row[0]] === true, listed[row[1]] === true];",
        "    try {",
        "      line.push(new Temporal.ZonedDateTime(0n, row[0]).equals(new Temporal.ZonedDateTime(0n, row[1])));",
        "    } catch (e) { line.push(e.name); }",
        "    instants.forEach(function (t) {",
        "      try {",
        "        line.push(Temporal.Instant.fromEpochMilliseconds(t).toZonedDateTimeISO(row[0]).offsetNanoseconds / 1e9);",
        "      } catch (e) { line.push(e.name); }",
        "    });",
        "    out.push(line.join('\\t'));",
        "  });",
        "  print(out.join('\\n'));",
        "})();",
        "",
    ])


def signed_seconds(text: str) -> int | None:
    found = SECONDS.match(text)
    if not found:
        return None
    sign, hours, minutes, seconds = found.groups()
    value = int(hours) * 3600 + int(minutes) * 60 + int(seconds)
    return -value if sign == "-" else value


def signed_minutes(text: str) -> int | None:
    found = MINUTES.match(text)
    if not found:
        return None
    sign, hours, minutes = found.groups()
    if sign is None:
        return 0
    value = int(hours) * 60 + int(minutes)
    return -value if sign == "-" else value


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--after", type=Path, required=True)
    parser.add_argument("--compare", type=Path, required=True)
    parser.add_argument("--shell", type=Path, required=True)
    parser.add_argument("--out", type=Path, required=True)
    args = parser.parse_args()
    args.out.mkdir(parents=True, exist_ok=True)

    lines = read_lines(args.after / "timezones.lines.tsv")
    compared = {row["line"]: row for row in read_lines(args.compare / "timezones.compare.tsv")}
    differing = [row for row in lines if row["equal"] != "yes"]
    parsed = {}
    for row in differing:
        expected = IDENTIFIER_LINE.match(row["expected"])
        actual = IDENTIFIER_LINE.match(row["actual"])
        parsed[row["line"]] = (expected, actual)
    rows = sorted({(e.group("name"), e.group("resolved")) for e, a in parsed.values() if e and a})

    probe = args.out / "probe.js"
    probe.write_text(probe_program([list(r) for r in rows]), encoding="utf-8", newline="\n")
    env = {k: v for k, v in os.environ.items() if k not in ("LANG", "LANGUAGE") and not k.startswith("LC_")}
    env["TZ"] = "UTC"
    result = subprocess.run(["dotnet", str(args.shell), "--script-host", str(probe)], cwd=args.out, env=env,
                            capture_output=True, text=True, encoding="utf-8")
    (args.out / "probe.answer.tsv").write_text(
        "name\texpected resolved\tname listed\texpected listed\tone zone\t" +
        "\t".join(f"offset s at {t}" for t in INSTANTS) + "\n" + result.stdout, encoding="utf-8", newline="\n")
    if result.returncode != 0 or result.stderr:
        print(result.stderr, file=sys.stderr)
        return 1
    answers = {}
    for line in result.stdout.splitlines():
        fields = line.split("\t")
        answers[(fields[0], fields[1])] = fields

    report = [["program", "line", "key", "resolved part", "names part", "kind", "moved since the base", "detail"]]
    counts: dict[str, int] = {}
    for row in differing:
        expected, actual = parsed[row["line"]]
        moved = "no" if compared[row["line"]]["after_equals_base"] == "yes" else "yes"
        if not expected or not actual or expected.group("name") != actual.group("name"):
            kind = "other"
            report.append(["timezones", row["line"], row["key"], "?", "?", kind, moved, "not two identifier lines"])
            counts[kind] = counts.get(kind, 0) + 1
            continue
        name, want, got = expected.group("name"), expected.group("resolved"), actual.group("resolved")
        answer = answers[(name, want)]
        resolved = "equal"
        detail = []
        if want != got:
            if (got == name and want != name and answer[2] == "false" and answer[3] == "true"
                    and answer[4] == "true"):
                resolved = "resolved-only-Z8"
                detail.append(f"resolved {got}, expected its primary {want}, one zone")
            else:
                resolved = "other"
                detail.append(f"resolved {got}, expected {want}; probe {answer[2:5]}")
        names = "equal"
        tokens = list(zip(expected.group("names").split(), actual.group("names").split()))
        dropped = []
        for index, (e, a) in enumerate(tokens):
            if e == a:
                continue
            seconds = signed_seconds(e)
            minutes = signed_minutes(a)
            offset = answer[5 + index]
            if (seconds is not None and seconds % 60 != 0 and minutes is not None
                    and abs(minutes * 60 - seconds) < 60
                    and offset.lstrip("-").isdigit() and int(offset) == seconds):
                how = "the nearest minute" if abs(minutes * 60 - seconds) <= 30 else "the other adjacent minute"
                dropped.append(f"{e}->{a} ({how})")
                if names == "equal":
                    names = "seconds-only-Z16"
            else:
                names = "other"
                detail.append(f"name {index + 1}: expected {e}, shell {a}, Temporal offset {offset} s")
        if dropped:
            detail.append("seconds dropped: " + " ".join(dropped))
        parts = [p for p in (resolved, names) if p != "equal"]
        kind = "other" if "other" in parts else " and ".join(parts)
        counts[kind] = counts.get(kind, 0) + 1
        report.append(["timezones", row["line"], row["key"], resolved, names, kind, moved, "; ".join(detail)])

    temporal = read_lines(args.after / "temporal.lines.tsv")
    temporal_compared = {row["line"]: row for row in read_lines(args.compare / "temporal.compare.tsv")}
    for row in temporal:
        if row["equal"] == "yes":
            continue
        same = temporal_compared[row["line"]]["after_equals_base"] == "yes"
        kind = "after equals base" if same else "moved since the base"
        counts[f"temporal: {kind}"] = counts.get(f"temporal: {kind}", 0) + 1
        report.append(["temporal", row["line"], row["key"], "", "", kind, "no" if same else "yes",
                       f"expected {row['expected'].split(' => ', 1)[-1]}, after {row['actual'].split(' => ', 1)[-1]}"])

    with (args.out / "classification.tsv").open("w", encoding="utf-8", newline="\n") as handle:
        for line in report:
            handle.write("\t".join(line) + "\n")
    summary = [f"timezones.js: {len(differing)} differing lines; temporal.js: "
               f"{sum(1 for r in temporal if r['equal'] != 'yes')} differing lines"]
    summary += [f"  {kind}: {count}" for kind, count in sorted(counts.items())]
    (args.out / "classification.txt").write_text("\n".join(summary) + "\n", encoding="utf-8", newline="\n")
    print("\n".join(summary))
    return 0


if __name__ == "__main__":
    sys.exit(main())
