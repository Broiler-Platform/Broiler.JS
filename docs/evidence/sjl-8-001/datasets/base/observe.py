# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Groups the base run's differing lines by what each difference looks like: observations.txt.

An observation by the author of milestone SJL-8.4 for whoever classifies the after run, not a
classification: it assigns none of the rule's classes, and the base is not judged. It reads the run's
lines reports through the harness's own reader and writes to standard output. From the repository's root:

    python3 -I docs/evidence/sjl-8-001/datasets/base/observe.py docs/evidence/sjl-8-001/datasets/base \
        > docs/evidence/sjl-8-001/datasets/base/observations.txt

The rows named are those of Broiler.VM's correspondence, docs/evidence/sjl-8-001/correspondence.md in
Broiler.VM, which the rule classifies by; a row is named where a difference seems to be its case.
"""

from __future__ import annotations

from collections import Counter
from pathlib import Path
import re
import sys

REPOSITORY = Path(__file__).resolve().parents[5]
sys.path.insert(0, str(REPOSITORY / "scripts" / "conformance"))
import run_reference_datasets as H  # noqa: E402

INSTANTS = ["1850", "1900", "1940", "1975", "2000", "2026-01-15", "2026-07-15", "2150"]
OFFSET = re.compile(r"^GMT(?:([+-])(\d\d):(\d\d)(?::(\d\d))?)?$")
# The names whose offsets tzdb 2025c to 2026e changed from 1970 on (the correspondence's row Z2).
RELEASE = set("""America/Tijuana America/Ensenada America/Santa_Isabel Mexico/BajaNorte Europe/Chisinau Europe/Tiraspol
America/Vancouver Canada/Pacific America/Edmonton Canada/Mountain America/Yellowknife Africa/Casablanca Africa/El_Aaiun
America/Inuvik America/Bogota Asia/Tehran Iran America/Winnipeg Canada/Central America/Rainy_River""".split())

DISPLAY = "display"
IDENTIFIER = "identifier"
BEFORE_1970 = "before 1970"
AT_2150 = "at 2150"
FACETS = {
    DISPLAY: "the offset shown to the nearest minute where the expected answer shows its seconds, the same offset "
             "otherwise (GMT-00:16 for GMT-00:16:08)",
    IDENTIFIER: "the resolved identifier: this engine keeps the name it was given where the expected answer has the "
                "primary identifier",
    BEFORE_1970: "an offset before 1970 (1850, 1900 or 1940) differs by more than the display",
    AT_2150: "the offset at 2150 differs",
}
ROWS = {
    DISPLAY: "Z16: the longOffset name's seconds, zone display, SJL-9's",
    IDENTIFIER: "Z8: the formatter's resolved timeZone of a non-primary name",
    BEFORE_1970: "Z3: backzone",
    AT_2150: "Z4: offsets frozen after 2037; and Z1, the release, for the names 2025c to 2026e changed",
}


def seconds(text: str) -> int | None:
    match = OFFSET.match(text)
    if not match:
        return None
    if not match.group(1):
        return 0
    value = int(match.group(2)) * 3600 + int(match.group(3)) * 60 + int(match.group(4) or 0)
    return -value if match.group(1) == "-" else value


def parse(line: str) -> tuple[str, str, list[str] | None]:
    name, _, rest = line.partition(" => ")
    if " | " not in rest:
        return name, rest, None
    resolved, _, offsets = rest.partition(" | ")
    return name, resolved, offsets.split(" ")


def facets(expected: str, actual: str) -> tuple[set[str], list[str]]:
    """The facets of one identifier line, and the instants whose offsets differ beyond the display."""
    _, expected_id, expected_offsets = parse(expected)
    _, actual_id, actual_offsets = parse(actual)
    found: set[str] = set()
    instants: list[str] = []
    if expected_id != actual_id:
        found.add(IDENTIFIER)
    for index, (e, a) in enumerate(zip(expected_offsets, actual_offsets)):
        if e == a:
            continue
        es, as_ = seconds(e), seconds(a)
        if es is not None and as_ is not None and es % 60 != 0 and as_ % 60 == 0 and abs(as_ - es) <= 30:
            found.add(DISPLAY)
            continue
        instants.append(INSTANTS[index])
        if index <= 2:
            found.add(BEFORE_1970)
        elif index == 7:
            found.add(AT_2150)
        else:
            found.add("an offset from 1975 to 2026")
    return found, instants


def timezones(rows: list[dict[str, str]]) -> list[str]:
    differing = [r for r in rows if r["equal"] == "no"]
    out = [f"timezones.js: {len(rows)} lines, {len(rows) - len(differing)} equal to the expected answer, "
           f"{len(differing)} differing", ""]
    per_facet: Counter[str] = Counter()
    per_instant: Counter[str] = Counter()
    combinations: Counter[str] = Counter()
    names: dict[str, list[str]] = {}
    lists: list[str] = []
    others: list[str] = []
    release_2150: list[str] = []
    for row in differing:
        _, _, expected_offsets = parse(row["expected"])
        _, _, actual_offsets = parse(row["actual"])
        if expected_offsets is None or actual_offsets is None or len(expected_offsets) != 8 or len(actual_offsets) != 8:
            (lists if row["key"].startswith("zones of ") or row["key"] == "listed" else others).append(row["key"])
            continue
        found, instants = facets(row["expected"], row["actual"])
        per_facet.update(found)
        per_instant.update(instants)
        key = " + ".join(f for f in (IDENTIFIER, DISPLAY, BEFORE_1970, AT_2150) if f in found) or "(none)"
        combinations[key] += 1
        names.setdefault(key, []).append(row["key"])
        if AT_2150 in found and row["key"] in RELEASE:
            release_2150.append(row["key"])

    out.append("Each facet, with the number of lines that show it (a line may show several):")
    for facet in (DISPLAY, AT_2150, IDENTIFIER, BEFORE_1970):
        out.append(f"  {per_facet[facet]:4d}  {facet}: {FACETS[facet]}")
        out.append(f"        seems to be row {ROWS[facet]}")
    other_facets = sorted(set(per_facet) - set(FACETS))
    out.append(f"  {sum(per_facet[f] for f in other_facets):4d}  any other offset (1975, 2000, January or July 2026)")
    out.append("  Offsets that differ beyond the display, per instant: "
               + ", ".join(f"{i} {per_instant[i]}" for i in INSTANTS) + ".")
    out.append(f"  Of the lines whose 2150 offset differs, {len(release_2150)} are names whose offsets tzdb 2025c to 2026e "
               f"changed: {' '.join(sorted(release_2150))}.")
    out.append("")
    out.append("Each differing identifier line once, by the facets it shows, with its first names:")
    for key, count in combinations.most_common():
        shown = " ".join(names[key][:5]) + (" ..." if len(names[key]) > 5 else "")
        out.append(f"  {count:4d}  {key}: {shown}")
    out.append("")
    out.append(f"The other lines that differ ({len(lists) + len(others)}):")
    out.append(f"  {len(lists):4d}  the lists: {' '.join(lists)}")
    out.append("        the region lines seem to be row Z12 (this engine's hand table of one zone per region); `listed`, "
               "row Z7 (the primary identifiers), with Z5 (this engine lists Factory)")
    out.append(f"  {len(others):4d}  any other line{': ' + ' '.join(others) if others else ''}")
    equal_special = [r["key"] for r in rows if r["equal"] == "yes" and r["key"] in ("upper case", "unknown", "non-ASCII")]
    out.append(f"  No line differs by an error: the lines on lookup and errors ({', '.join(equal_special)}) are equal.")
    out.append("")
    only_identity_display = sum(c for k, c in combinations.items() if k in (IDENTIFIER, DISPLAY, f"{IDENTIFIER} + {DISPLAY}"))
    with_offset = sum(c for k, c in combinations.items() if BEFORE_1970 in k or AT_2150 in k)
    out.append("In sum, of the differing lines:")
    out.append(f"  {only_identity_display:4d}  differ only by the resolved identifier, the display of the offset, or both "
               f"({combinations[IDENTIFIER]} + {combinations[DISPLAY]} + {combinations[f'{IDENTIFIER} + {DISPLAY}']})")
    out.append(f"  {with_offset:4d}  have an offset that differs (offset and transition data), with or without those")
    out.append(f"  {len(lists) + len(others):4d}  are lists")
    return out


def temporal(rows: list[dict[str, str]]) -> list[str]:
    differing = [r for r in rows if r["equal"] == "no"]
    out = [f"temporal.js: {len(rows)} lines, {len(rows) - len(differing)} equal to the expected answer, "
           f"{len(differing)} differing", ""]
    modes = r" (ceil|floor|expand|trunc|halfCeil|halfFloor|halfExpand|halfTrunc|halfEven) "
    groups: Counter[str] = Counter(re.sub(modes, " <mode> ", r["key"]) for r in differing)
    for operation, count in groups.most_common():
        members = [r for r in differing if re.sub(modes, " <mode> ", r["key"]) == operation]
        found = sorted({re.search(modes, r["key"]).group(1) for r in members if re.search(modes, r["key"])})
        answers = Counter((r["expected"].split(" => ", 1)[1], r["actual"].split(" => ", 1)[1]) for r in members)
        out.append(f"  {count:4d}  {operation}")
        out.append(f"        modes: {' '.join(found)}; " + "; ".join(
            f"{e} expected, {a} answered ({n})" for (e, a), n in answers.most_common()))
    same_operation = [r for r in rows if re.sub(modes, " <mode> ", r["key"]) in groups and r["equal"] == "yes"]
    if same_operation:
        out.append("        the same operation in the other modes is equal: "
                   + "; ".join(f"line {r['line']}, {r['actual'].split(' => ', 1)[1]}" for r in same_operation))
    out.append("        It rounds a duration to months relative to a plain date in the ISO calendar: it reads no zone and")
    out.append("        no calendar table year. It is duration rounding. No row of the correspondence names temporal.js;")
    out.append("        the nearest is K17, which names the arithmetic and rounding lines of Broiler.VM's retained calendar")
    out.append("        dataset (temporal-calendars.js, not part C's population) SJL-10's.")
    return out


def main() -> None:
    run = Path(sys.argv[1])
    lines = ["Observations on a run of the reference datasets (bundle sjl-8-001's rule, part C)", "",
             "The differing lines grouped by what each difference looks like, with the rows of Broiler.VM's",
             "correspondence they seem to be cases of. An observation, written by observe.py from the lines reports",
             "beside it; not a classification. It assigns none of the rule's classes, and the base is not judged.", ""]
    lines += timezones(H.read_tsv(run / "timezones.lines.tsv"))
    lines.append("")
    lines += temporal(H.read_tsv(run / "temporal.lines.tsv"))
    sys.stdout.write("\n".join(lines) + "\n")


if __name__ == "__main__":
    main()
