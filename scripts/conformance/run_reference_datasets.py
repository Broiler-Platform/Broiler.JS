#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Run Broiler.VM's reference datasets in this engine's shell and compare them line by line.

The harness of bundle sjl-8-001's rule, part C (docs/evidence/sjl-8-001/decision-rule.md), committed at
milestone SJL-8.4 beside the copies in conformance/datasets/time/. The rule's configuration is "each
program's text evaluated by indirect eval in the shell BroilerJS built in Release, through the harness
SJL-8.4 commits beside the copies, which prints the program's completion value; no LANG and TZ=UTC", and
"the expected answer of each line is Broiler.VM's: the retained answer with Broiler.VM's divergences
applied, which Broiler.VM's slice checks hold its own answer equal to". Its comparison is "per program and
line, the line after against its expected answer; and per line that still differs, the line after against
the same line at the base".

**Two rules, since SJL-10.2.** Bundle sjl-10-001's rule, part C (docs/evidence/sjl-10-001/decision-rule.md),
takes the same configuration and comparison over another population, temporal.js and temporal-calendars.js,
"through scripts/conformance/run_reference_datasets.py and its driver ... the harness extended at SJL-10.2 to
the second program". A run names its bundle with ``--bundle``, which fixes the rule it is taken for and the
programs it runs (``BUNDLES``); its outputs name both, and ``compare`` joins only two runs of one bundle.

**Three rules, since SJL-9.2.** Bundle sjl-9-001's rule, part C (docs/evidence/sjl-9-001/decision-rule.md), takes
the same configuration and comparison over Broiler.VM's reference datasets of the Intl area, "through
scripts/conformance/run_reference_datasets.py --bundle sjl-9-001 and its driver": the twelve programs milestone SJL-9.2
copied into conformance/datasets/intl/, with their own pin, and timezones.js, which conformance/datasets/time/ holds
since SJL-8.4. So a bundle names the collections its programs come from (``COLLECTIONS``: ``time`` and ``intl``, each a
directory with its datasets.pin), and a program's name is looked up in each, a name pinned in two refused. Its part D
takes "part C's harness with --invariant-globalization, which sets DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 for the
shell": ``run --invariant-globalization`` does that, a run without it removes the variable from the shell's
environment, a run's outputs say which it was, and ``compare`` joins only two runs taken the same way.

Two commands do that:

- ``run`` builds the shell in Release (``--build``) or takes the one already built, checks every copy
  against ``datasets.pin``, runs each program through ``reference_dataset_driver.js`` in a fresh shell
  process, and writes, per program, every line with its expected answer, the shell's answer and whether
  they are equal (``<program>.lines.tsv``), the answer itself (``<program>.answer.txt``), and the counts
  (``summary.json``, ``report.txt``), with the run's identity (``identity.txt``).
- ``compare`` joins an after run with a base run, line by line (``<program>.compare.tsv``,
  ``compare.json``, ``compare.txt``).

**What is Broiler.VM's, exactly.** The expected answer and the line split are those of
``IntlChecks.RetainedMatches`` in Broiler.VM's
``src/compositions/Broiler.VM.Composition.JavaScript.SliceCompiler/IntlChecks.cs`` at 9508bd5, which the
checks ``tzdb/time-zone-offsets-match-icu`` and ``temporal/t1/temporal-matches-the-reference-polyfill``
run, and which ``temporal/t3/calendars-match-the-reference-polyfill`` runs the same way at 14072b0 and
99c9c9f, and every check of the Intl area's datasets (``intl/i2/...``, ``intl/i3/...``, ``intl/i4/...``,
``temporal/t2/...``, ``temporal/t4/...``, ``temporal/t5/...``) at 4cf9ca4, whose ``RetainedMatches`` is 9508bd5's
line for line:

- the retained answer is ``File.ReadAllText(retained).TrimEnd('\\n').Split('\\n')``: every LF at its end
  removed, then split at each LF (``split_retained``);
- the engine's answer is its completion value ``.Split('\\n')``, nothing trimmed (``split_answer``);
- the divergences file is read with ``File.ReadAllLines``, which ends a line at CR LF, CR or LF; a line
  that is empty or starts with ``#`` is skipped, and every other one is stored under its key, a later
  one replacing an earlier one with the same key (``read_divergences``);
- a line's key is its text before the first `` => ``, or else before the first `` resolved ``, or else the
  whole line (``line_key``);
- line *i*, for *i* up to the longer of the two answers, expects the retained line, or ``(no line)`` past
  its end; if that text's key names a divergence, the divergence replaces it and counts as used; the
  engine's line, or ``(no line)`` past its end, must equal it ordinally (``expected_answer`` and
  ``compare_answers``).

Broiler.VM's check also asks that every divergence be used and that the retained answer have more lines
than a floor (600, 1,500 and 3,500); both are reported here as facts about the copies, not as part of a
comparison.

**What it decides.** Nothing. It reports equal and different lines and their counts; the rules' classes
(sjl-8-001's D1 to D3, sjl-9-001's V, R, Z8, O and H) are assigned by a person.
"""

from __future__ import annotations

import argparse
from dataclasses import dataclass, field
import datetime
import hashlib
import json
import os
from pathlib import Path
import platform
import subprocess
import sys
import time
from typing import Iterable, Sequence

SCRIPTS = Path(__file__).resolve().parent
REPOSITORY = SCRIPTS.parents[1]
DATASETS = REPOSITORY / "conformance" / "datasets" / "time"
INTL_DATASETS = REPOSITORY / "conformance" / "datasets" / "intl"
PIN_NAME = "datasets.pin"
DRIVER_TEMPLATE = SCRIPTS / "reference_dataset_driver.js"
DRIVER_MARKER = "/*PROGRAM_FILE*/"
SHELL_PROJECT = Path("Broiler.JS/Broiler.JavaScript/Broiler.JavaScript.csproj")
SHELL_DLL = Path("Broiler.JS/Broiler.JavaScript/bin/Release/net10.0/BroilerJS.dll")
# Format 2 (SJL-10.2): a run names its bundle, and its copies are its population's, each with its commit.
# Format 3 (SJL-9.2): a run also names each program's collection and whether it ran under invariant globalization.
FORMAT = 3

# IntlChecks.RetainedMatches's text for a line past the end of either answer.
NO_LINE = "(no line)"
# The `least` each check passes to RetainedMatches: the retained answer must have more lines. The Intl area's are
# those of IntlChecks.cs at Broiler.VM's 4cf9ca4, the commit its datasets were copied from (milestone SJL-9.2).
VM_LEAST = {"timezones": 600, "temporal": 1500, "temporal-calendars": 3500,
            "numbers": 2000, "dates": 1000, "locales": 700, "plurals": 1200, "lists": 450, "relativetimes": 2400,
            "segments": 110, "durations": 500, "displaynames": 800, "temporal-intl": 1200, "calendar-formats": 5500,
            "zone-names": 2600}

# The environment variable part D of bundle sjl-9-001's rule sets for the shell, and its value there.
INVARIANT_VARIABLE = "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"


@dataclass(frozen=True)
class Bundle:
    """An evidence bundle whose rule's part C this harness runs: the rule, the programs it takes, and the
    collections (``COLLECTIONS``) whose pins hold them."""
    name: str
    rule: str
    programs: tuple[str, ...]
    collections: tuple[str, ...] = ("time",)


# The directories of copies, each with its own datasets.pin: the time area's (SJL-8.4, SJL-10.2) and the Intl
# area's (SJL-9.2). `run` and `verify` take each from its own option, so a test can hand them another directory.
COLLECTIONS = {"time": DATASETS, "intl": INTL_DATASETS}

INTL_PROGRAMS = ("dates", "displaynames", "durations", "lists", "locales", "numbers", "plurals", "relativetimes",
                 "segments", "temporal-intl", "calendar-formats", "zone-names")

BUNDLES = {
    "sjl-8-001": Bundle("sjl-8-001", "docs/evidence/sjl-8-001/decision-rule.md, part C", ("timezones", "temporal")),
    "sjl-10-001": Bundle("sjl-10-001", "docs/evidence/sjl-10-001/decision-rule.md, part C",
                         ("temporal", "temporal-calendars")),
    # Part C of bundle sjl-9-001's rule, and its part D with --invariant-globalization: the Intl area's twelve
    # programs, then timezones.js from block 1 of the time area's pin.
    "sjl-9-001": Bundle("sjl-9-001", "docs/evidence/sjl-9-001/decision-rule.md, parts C and D",
                        INTL_PROGRAMS + ("timezones",), ("intl", "time")),
}

DRIVER_LINE = "reference-dataset-driver"
COMPLETION_LINE = "reference-dataset-completion"
UNCAUGHT_LINE = "reference-dataset-uncaught"

TSV_COLUMNS = ("line", "equal", "source", "key", "expected", "actual")
COMPARE_COLUMNS = ("line", "after_equals_expected", "after_equals_base", "base_equals_expected", "key",
                   "expected", "base", "after")


class HarnessError(RuntimeError):
    """The harness cannot do what it was asked, and says why."""


# ---- The pin ------------------------------------------------------------------------------------------


@dataclass(frozen=True)
class PinnedFile:
    name: str
    length: int
    sha256: str
    blob: str
    source_path: str
    source_commit: str


@dataclass(frozen=True)
class Program:
    name: str
    program: str
    answer: str
    divergences: str


@dataclass(frozen=True)
class Pin:
    source_commits: tuple[str, ...]
    files: dict[str, PinnedFile]
    programs: tuple[Program, ...]

    def program(self, name: str) -> Program:
        return next(p for p in self.programs if p.name == name)


def read_pin(text: str) -> Pin:
    """Reads datasets.pin's lines; comments and blank lines are skipped. A `source-commit` line names the
    Broiler.VM commit of the `file` lines after it, up to the next one (pin 2, milestone SJL-10.2: the copies
    of bundle sjl-8-001's rule came from Broiler.VM's 9508bd5, and SJL-10.2's from its 14072b0)."""
    source_commit = ""
    source_commits: list[str] = []
    files: dict[str, PinnedFile] = {}
    programs: list[Program] = []
    for number, raw in enumerate(text.split("\n"), start=1):
        if not raw or raw.startswith("#"):
            continue
        fields = raw.split(" ")
        if fields[0] == "source-commit" and len(fields) == 2:
            source_commit = fields[1]
            if source_commit in source_commits:
                raise HarnessError(f"{PIN_NAME} line {number}: source commit {source_commit} opens a second block")
            source_commits.append(source_commit)
        elif fields[0] == "file":
            if len(fields) != 6 or not fields[2].isdigit():
                raise HarnessError(f"{PIN_NAME} line {number}: a file line is `file <name> <bytes> <sha256> <blob> <path>`")
            if not source_commit:
                raise HarnessError(f"{PIN_NAME} line {number}: a file line comes before any source-commit line")
            if fields[1] in files:
                raise HarnessError(f"{PIN_NAME} line {number}: {fields[1]} is pinned twice")
            files[fields[1]] = PinnedFile(fields[1], int(fields[2]), fields[3], fields[4], fields[5], source_commit)
        elif fields[0] == "program":
            if len(fields) != 5:
                raise HarnessError(f"{PIN_NAME} line {number}: a program line is `program <name> <program> <answer> <divergences>`")
            if any(p.name == fields[1] for p in programs):
                raise HarnessError(f"{PIN_NAME} line {number}: program {fields[1]} is named twice")
            programs.append(Program(*fields[1:]))
    if not source_commits or not files or not programs:
        raise HarnessError(f"{PIN_NAME} names no source commit, no file or no program")
    for program in programs:
        for name in (program.program, program.answer, program.divergences):
            if name not in files:
                raise HarnessError(f"{PIN_NAME}: program {program.name} names {name}, which no file line pins")
    return Pin(tuple(source_commits), files, tuple(programs))


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def sha256_file(path: Path) -> str:
    return sha256_bytes(path.read_bytes())


def verify_copies(directory: Path, pin: Pin) -> list[str]:
    """Each pinned file's length and SHA-256 against its line; an empty list when every copy is the population."""
    problems = []
    for pinned in pin.files.values():
        path = directory / pinned.name
        if not path.is_file():
            problems.append(f"{pinned.name}: missing")
            continue
        data = path.read_bytes()
        if len(data) != pinned.length:
            problems.append(f"{pinned.name}: {len(data)} bytes where the pin says {pinned.length}")
        if sha256_bytes(data) != pinned.sha256:
            problems.append(f"{pinned.name}: SHA-256 {sha256_bytes(data)} where the pin says {pinned.sha256}")
    return problems


def load_datasets(directory: Path) -> Pin:
    pin = read_pin((directory / PIN_NAME).read_text(encoding="utf-8"))
    problems = verify_copies(directory, pin)
    if problems:
        raise HarnessError("a copy whose digest differs is not the population: " + "; ".join(problems))
    return pin


# ---- Broiler.VM's line semantics (IntlChecks.RetainedMatches) ----------------------------------------


def read_all_text(data: bytes) -> str:
    """File.ReadAllText for the copies: UTF-8, a byte order mark dropped. Strict, because the copies are well-formed
    UTF-8 (the time area's are ASCII, and some of the Intl area's are not) and held by their digests; .NET would
    write U+FFFD for a malformed byte instead."""
    return data.decode("utf-8-sig")


def read_all_lines(text: str) -> list[str]:
    """File.ReadAllLines: a line ends at CR LF, CR or LF, and a terminator at the end starts no line."""
    lines: list[str] = []
    start = 0
    index = 0
    length = len(text)
    while index < length:
        character = text[index]
        if character == "\r" or character == "\n":
            lines.append(text[start:index])
            if character == "\r" and index + 1 < length and text[index + 1] == "\n":
                index += 1
            start = index + 1
        index += 1
    if start < length:
        lines.append(text[start:])
    return lines


def line_key(line: str) -> str:
    """RetainedMatches's Key: the text before the first ' => ', or else before the first ' resolved '."""
    arrow = line.find(" => ")
    resolved = line.find(" resolved ")
    if arrow >= 0:
        return line[:arrow]
    if resolved >= 0:
        return line[:resolved]
    return line


def read_divergences(text: str) -> dict[str, str]:
    """Every line that is not empty and does not start with '#', under its key; a later line wins."""
    replaced: dict[str, str] = {}
    for line in read_all_lines(text):
        if len(line) != 0 and line[0] != "#":
            replaced[line_key(line)] = line
    return replaced


def split_retained(text: str) -> list[str]:
    """File.ReadAllText(retained).TrimEnd('\\n').Split('\\n')."""
    return text.rstrip("\n").split("\n")


def split_answer(completion: str) -> list[str]:
    """The engine's completion value .Split('\\n'): nothing trimmed."""
    return completion.split("\n")


@dataclass(frozen=True)
class Expected:
    lines: list[str]
    sources: list[str]
    used: int


def expected_answer(retained: Sequence[str], replaced: dict[str, str], length: int) -> Expected:
    """The expected text of each line up to `length`, the longer of the two answers: the retained line or
    (no line), replaced by the divergence its key names. `used` counts replacements, as the check does."""
    lines: list[str] = []
    sources: list[str] = []
    used = 0
    for index in range(length):
        want = retained[index] if index < len(retained) else NO_LINE
        divergent = replaced.get(line_key(want))
        if divergent is not None:
            lines.append(divergent)
            sources.append("divergence")
            used += 1
        else:
            lines.append(want)
            sources.append("retained")
    return Expected(lines, sources, used)


@dataclass(frozen=True)
class LineResult:
    number: int
    key: str
    source: str
    expected: str
    actual: str
    equal: bool


def compare_answers(expected: Expected, answered: Sequence[str]) -> list[LineResult]:
    """Line by line, ordinally: the engine's line, or (no line) past its end, against the expected one."""
    results = []
    for index, want in enumerate(expected.lines):
        got = answered[index] if index < len(answered) else NO_LINE
        results.append(LineResult(index + 1, line_key(want), expected.sources[index], want, got, want == got))
    return results


def compare_program(retained_text: str, divergences_text: str, answered: Sequence[str]) -> tuple[Expected, list[LineResult]]:
    """RetainedMatches's loop, for one program: the retained answer and divergences as text, the engine's
    answer as lines."""
    retained = split_retained(retained_text)
    replaced = read_divergences(divergences_text)
    expected = expected_answer(retained, replaced, max(len(retained), len(answered)))
    return expected, compare_answers(expected, answered)


def negative_control(expected: Expected, answered: Sequence[str], results: Sequence[LineResult]) -> dict:
    """Changes one line of the expected answer in memory, the first line that is equal (or line 1), by its
    last character, and compares again: that line, and no other, must change from what it was."""
    index = next((r.number - 1 for r in results if r.equal), 0)
    original = expected.lines[index]
    changed = original[:-1] + ("Y" if original.endswith("X") else "X")
    mutated = Expected(list(expected.lines), list(expected.sources), expected.used)
    mutated.lines[index] = changed
    again = compare_answers(mutated, answered)
    shown = not again[index].equal
    others = all(a.equal == r.equal for i, (a, r) in enumerate(zip(again, results)) if i != index)
    return {
        "line": index + 1,
        "wasEqual": results[index].equal if results else False,
        "expectedChangedTo": changed,
        "shownAsDifference": shown,
        "otherLinesUnchanged": others,
    }


# ---- The transport from the driver ---------------------------------------------------------------------


def utf16_length(text: str) -> int:
    return sum(2 if ord(c) > 0xFFFF else 1 for c in text)


def units_to_text(units: Iterable[int]) -> str:
    """UTF-16 code units to a string: a high and a low surrogate join, a lone surrogate stays itself."""
    out: list[str] = []
    pending: int | None = None
    for unit in units:
        if pending is not None:
            if 0xDC00 <= unit <= 0xDFFF:
                out.append(chr(0x10000 + ((pending - 0xD800) << 10) + (unit - 0xDC00)))
                pending = None
                continue
            out.append(chr(pending))
            pending = None
        if 0xD800 <= unit <= 0xDBFF:
            pending = unit
        else:
            out.append(chr(unit))
    if pending is not None:
        out.append(chr(pending))
    return "".join(out)


HEX = frozenset("0123456789abcdef")


def decode_escaped(text: str) -> str:
    """Reverses the driver's escaping: printable ASCII but the backslash stands for itself, and \\uXXXX, in
    lower case, for one UTF-16 code unit. Anything else is refused."""
    units: list[int] = []
    index = 0
    length = len(text)
    while index < length:
        character = text[index]
        if character == "\\":
            digits = text[index + 2:index + 6]
            if text[index + 1:index + 2] != "u" or len(digits) != 4 or not set(digits) <= HEX:
                raise HarnessError(f"the driver's output has a malformed escape at character {index}")
            units.append(int(digits, 16))
            index += 6
        elif " " <= character <= "~":
            units.append(ord(character))
            index += 1
        else:
            raise HarnessError(f"the driver's output has an unescaped character U+{ord(character):04X} at character {index}")
    return units_to_text(units)


@dataclass
class DriverOutcome:
    completed: bool = False
    completion: str | None = None
    completion_type: str | None = None
    uncaught: str | None = None
    problems: list[str] = field(default_factory=list)
    extra_lines: list[str] = field(default_factory=list)


def parse_driver_output(stdout: str, program_file: str, program_text: str, exit_code: int) -> DriverOutcome:
    """The driver's lines out of the shell's standard output. Anything else it printed is kept as extra
    output; a CR before each LF, as a Windows console writes, is dropped."""
    outcome = DriverOutcome()
    drivers: list[str] = []
    completions: list[str] = []
    uncaughts: list[str] = []
    lines = stdout.split("\n")
    if lines and lines[-1] == "":
        lines.pop()
    for line in lines:
        if line.endswith("\r"):
            line = line[:-1]
        head, _, rest = line.partition(" ")
        if head == DRIVER_LINE:
            drivers.append(rest)
        elif head == COMPLETION_LINE:
            completions.append(rest)
        elif head == UNCAUGHT_LINE:
            uncaughts.append(rest)
        else:
            outcome.extra_lines.append(line)

    if len(drivers) != 1:
        outcome.problems.append(f"the driver reported itself {len(drivers)} times, not once")
    else:
        expected_header = f"1 {program_file} {utf16_length(program_text)}"
        if drivers[0] != expected_header:
            outcome.problems.append(f"the driver read `{drivers[0]}` where `{expected_header}` was expected")

    if uncaughts:
        try:
            outcome.uncaught = decode_escaped(uncaughts[0])
        except HarnessError as error:
            outcome.uncaught = uncaughts[0]
            outcome.problems.append(str(error))
    if len(completions) + len(uncaughts) != 1:
        outcome.problems.append(
            f"the driver printed {len(completions)} completion and {len(uncaughts)} uncaught lines, not one of them")
    if len(completions) == 1:
        kind, _, rest = completions[0].partition(" ")
        length_text, _, escaped = rest.partition(" ")
        try:
            value = decode_escaped(escaped)
        except HarnessError as error:
            outcome.problems.append(str(error))
            value = None
        if value is not None:
            if not length_text.isdigit() or utf16_length(value) != int(length_text):
                outcome.problems.append(
                    f"the completion value decoded to {utf16_length(value)} code units where the driver printed {length_text}")
            else:
                outcome.completion = value
                outcome.completion_type = kind
    if exit_code != 0:
        outcome.problems.append(f"the shell exited with {exit_code}")
    outcome.completed = outcome.completion is not None and not outcome.problems
    return outcome


# ---- TSV --------------------------------------------------------------------------------------------


def tsv_escape(text: str) -> str:
    """A field of the reports: backslash, tab, CR and LF escaped as \\\\, \\t, \\r and \\n, and a lone
    surrogate as \\uXXXX, so every field is one line of valid UTF-8 and reads back exactly."""
    out = []
    for character in text:
        if character == "\\":
            out.append("\\\\")
        elif character == "\t":
            out.append("\\t")
        elif character == "\r":
            out.append("\\r")
        elif character == "\n":
            out.append("\\n")
        elif 0xD800 <= ord(character) <= 0xDFFF:
            out.append(f"\\u{ord(character):04x}")
        else:
            out.append(character)
    return "".join(out)


def tsv_unescape(text: str) -> str:
    out = []
    index = 0
    while index < len(text):
        character = text[index]
        if character != "\\":
            out.append(character)
            index += 1
            continue
        following = text[index + 1:index + 2]
        if following == "\\":
            out.append("\\")
        elif following == "t":
            out.append("\t")
        elif following == "r":
            out.append("\r")
        elif following == "n":
            out.append("\n")
        elif following == "u" and len(text) >= index + 6 and set(text[index + 2:index + 6]) <= HEX:
            out.append(chr(int(text[index + 2:index + 6], 16)))
            index += 6
            continue
        else:
            raise HarnessError(f"a malformed escape in a report field at character {index}")
        index += 2
    return "".join(out)


def write_tsv(path: Path, header: Sequence[str], rows: Iterable[Sequence[str]]) -> None:
    with path.open("w", encoding="utf-8", newline="\n") as handle:
        handle.write("\t".join(header) + "\n")
        for row in rows:
            handle.write("\t".join(tsv_escape(value) for value in row) + "\n")


def read_tsv(path: Path) -> list[dict[str, str]]:
    lines = path.read_bytes().decode("utf-8").split("\n")
    if lines and lines[-1] == "":
        lines.pop()
    header = lines[0].split("\t")
    rows = []
    for number, line in enumerate(lines[1:], start=2):
        values = line.split("\t")
        if len(values) != len(header):
            raise HarnessError(f"{path.name} line {number}: {len(values)} fields where the header has {len(header)}")
        rows.append({name: tsv_unescape(value) for name, value in zip(header, values)})
    return rows


def yes_no(value: bool) -> str:
    return "yes" if value else "no"


# ---- Running the shell -----------------------------------------------------------------------------


def child_environment(parent: dict[str, str], invariant: bool = False) -> dict[str, str]:
    """The rule's configuration: no LANG, and TZ=UTC. LANGUAGE and every LC_ variable go too, so nothing
    else names a locale. DOTNET_SYSTEM_GLOBALIZATION_INVARIANT is 1 when the run is under invariant
    globalization (bundle sjl-9-001's part D) and absent otherwise, whatever the harness's own environment holds,
    so a run is taken the way its outputs say (milestone SJL-9.2; until then the variable was passed through)."""
    environment = {k: v for k, v in parent.items()
                   if k not in ("LANG", "LANGUAGE", "TZ", INVARIANT_VARIABLE) and not k.startswith("LC_")}
    environment["TZ"] = "UTC"
    if invariant:
        environment[INVARIANT_VARIABLE] = "1"
    return environment


def environment_description(invariant: bool) -> str:
    return ("TZ=UTC; LANG, LANGUAGE and every LC_ variable removed; "
            + (f"{INVARIANT_VARIABLE}=1 (invariant globalization)" if invariant else f"{INVARIANT_VARIABLE} removed"))


def write_driver(path: Path, program_file: str) -> None:
    template = DRIVER_TEMPLATE.read_text(encoding="utf-8")
    if template.count(DRIVER_MARKER) != 1:
        raise HarnessError(f"{DRIVER_TEMPLATE.name} must hold {DRIVER_MARKER} exactly once")
    path.write_text(template.replace(DRIVER_MARKER, json.dumps(program_file)), encoding="utf-8", newline="\n")


def build_shell(tree: Path, log: Path) -> list[str]:
    command = ["dotnet", "build", str(SHELL_PROJECT), "-c", "Release"]
    with log.open("wb") as handle:
        process = subprocess.run(command, cwd=tree, stdout=handle, stderr=subprocess.STDOUT)
    if process.returncode != 0:
        raise HarnessError(f"`{' '.join(command)}` in {tree} exited with {process.returncode}; see {log}")
    return command


def git(tree: Path, *arguments: str) -> str | None:
    """A git command's output, or None when git is missing or the command fails."""
    try:
        process = subprocess.run(["git", "-C", str(tree), *arguments], capture_output=True, text=True, check=False)
    except OSError:
        return None
    return process.stdout.strip() if process.returncode == 0 else None


def command_output(*command: str) -> str:
    try:
        process = subprocess.run(list(command), capture_output=True, text=True, check=False, timeout=120)
    except (OSError, subprocess.SubprocessError):
        return ""
    return process.stdout.strip() if process.returncode == 0 else ""


def os_release() -> str:
    try:
        for line in Path("/etc/os-release").read_text(encoding="utf-8").splitlines():
            if line.startswith("PRETTY_NAME="):
                return line.split("=", 1)[1].strip('"')
    except OSError:
        pass
    return platform.platform()


def cpu_model() -> str:
    try:
        for line in Path("/proc/cpuinfo").read_text(encoding="utf-8").splitlines():
            if line.startswith("model name"):
                return line.split(":", 1)[1].strip()
    except OSError:
        pass
    return platform.processor() or "unknown"


def host_tzdata() -> str:
    try:
        first = Path("/usr/share/zoneinfo/tzdata.zi").read_text(encoding="utf-8").split("\n", 1)[0]
        return first.replace("# version ", "") if first.startswith("# version ") else "unknown"
    except OSError:
        return "absent"


def host_icu() -> str:
    listing = command_output("ldconfig", "-p")
    names = sorted({line.split()[0] for line in listing.splitlines() if "libicuuc.so." in line})
    return " ".join(names) or "none found by ldconfig"


@dataclass
class ProgramRun:
    program: Program
    seconds: float
    exit_code: int
    stdout: bytes
    stderr: bytes
    outcome: DriverOutcome
    expected: Expected
    results: list[LineResult]
    divergence_entries: int
    retained_lines: int
    control: dict


def run_program(dll: Path, directory: Path, program: Program, out: Path, timeout: float,
                invariant: bool = False) -> ProgramRun:
    program_text = read_all_text((directory / program.program).read_bytes())
    driver = out / f"driver-{program.name}.js"
    write_driver(driver, program.program)
    started = time.monotonic()
    try:
        process = subprocess.run(
            ["dotnet", str(dll), "--script-host", str(driver)],
            cwd=directory, env=child_environment(dict(os.environ), invariant), capture_output=True, timeout=timeout)
        exit_code, stdout, stderr = process.returncode, process.stdout, process.stderr
    except subprocess.TimeoutExpired as expired:
        exit_code = -1
        stdout = expired.stdout or b""
        stderr = (expired.stderr or b"") + f"\n(the harness stopped the shell after {timeout:g} seconds)\n".encode()
    seconds = time.monotonic() - started

    outcome = parse_driver_output(stdout.decode("utf-8", errors="replace"), program.program, program_text, exit_code)
    answered = split_answer(outcome.completion) if outcome.completed else []
    retained_text = read_all_text((directory / program.answer).read_bytes())
    divergences_text = read_all_text((directory / program.divergences).read_bytes())
    expected, results = compare_program(retained_text, divergences_text, answered)
    return ProgramRun(program, seconds, exit_code, stdout, stderr, outcome, expected, results,
                      len(read_divergences(divergences_text)), len(split_retained(retained_text)),
                      negative_control(expected, answered, results))


def write_program_outputs(run: ProgramRun, out: Path) -> dict:
    name = run.program.name
    write_tsv(out / f"{name}.lines.tsv", TSV_COLUMNS,
              ([str(r.number), yes_no(r.equal), r.source, r.key, r.expected, r.actual] for r in run.results))
    answer_sha256 = None
    if run.outcome.completion is not None:
        # The answer as Node's reference command writes one, the value and a LF. A lone surrogate, which
        # UTF-8 cannot hold, is written as \\udXXX here; the lines report holds it exactly.
        data = (run.outcome.completion + "\n").encode("utf-8", errors="backslashreplace")
        (out / f"{name}.answer.txt").write_bytes(data)
        answer_sha256 = sha256_bytes(data)
    (out / f"{name}.stderr.txt").write_bytes(run.stderr)
    equal = sum(1 for r in run.results if r.equal)
    used = run.expected.used
    return {
        "completed": run.outcome.completed,
        "completionType": run.outcome.completion_type,
        "uncaught": run.outcome.uncaught,
        "problems": run.outcome.problems,
        "extraOutput": run.outcome.extra_lines,
        "exitCode": run.exit_code,
        "seconds": round(run.seconds, 3),
        "stdoutSha256": sha256_bytes(run.stdout),
        "stdoutBytes": len(run.stdout),
        "stderrBytes": len(run.stderr),
        "answerSha256": answer_sha256,
        "retainedLines": run.retained_lines,
        "answeredLines": len(split_answer(run.outcome.completion)) if run.outcome.completed else 0,
        "comparedLines": len(run.results),
        "equal": equal,
        "differing": len(run.results) - equal,
        "divergenceEntries": run.divergence_entries,
        "divergencesUsed": used,
        "vmLeast": VM_LEAST.get(name),
        "negativeControl": run.control,
    }


def population(pin: Pin, programs: Sequence[Program]) -> list[PinnedFile]:
    """The copies the programs read: each one's program, retained answer and divergences, in pin order."""
    names = {name for p in programs for name in (p.program, p.answer, p.divergences)}
    return [pinned for pinned in pin.files.values() if pinned.name in names]


def identity_lines(bundle: Bundle, label: str, tree: Path, dll: Path, built: list[str] | None,
                   sources: Sequence["Source"], programs: Sequence[Program], started: str, invariant: bool) -> list[str]:
    commit = git(tree, "rev-parse", "HEAD")
    status = git(tree, "status", "--porcelain", "--untracked-files=all")
    harness_commit = git(REPOSITORY, "rev-parse", "HEAD")
    directories = sorted({str(source.directory) for source in sources})
    harness_status = git(REPOSITORY, "status", "--porcelain", "--untracked-files=all", "--",
                         str(Path(__file__).resolve()), str(DRIVER_TEMPLATE), *directories)
    runtimes = [line for line in command_output("dotnet", "--list-runtimes").splitlines()
                if line.startswith("Microsoft.NETCore.App ")]
    lines = [
        f"harness: scripts/conformance/run_reference_datasets.py, format {FORMAT}, for {bundle.rule}",
        f"bundle: {bundle.name}; programs: {', '.join(p.name for p in programs)}",
        f"label: {label}",
        f"started (UTC): {started}",
        f"shell tree: {tree}",
        f"shell commit: {commit or 'unknown'}",
        f"shell commit subject: {git(tree, 'log', '-1', '--format=%s') or 'unknown'}",
        f"shell tree clean (git status --porcelain --untracked-files=all is empty): {yes_no(bool(commit) and status == '')}",
        f"shell: {dll}",
        f"shell BroilerJS.dll SHA-256: {sha256_file(dll)}",
        "shell binaries: shell-binaries.sha256, every *.dll beside it",
        f"shell built by the harness: {('yes, `' + ' '.join(built) + '` in the shell tree') if built else 'no, the one already built was used'}",
        f"dotnet SDK: {command_output('dotnet', '--version') or 'unknown'}",
        f"dotnet runtime: {runtimes[-1] if runtimes else 'unknown'}",
        f"machine: {os_release()}; {platform.system()} {platform.release()} {platform.machine()}; {cpu_model()}; {os.cpu_count()} logical CPUs",
        f"child environment: {environment_description(invariant)}",
        f"harness environment: TZ={os.environ.get('TZ', '(unset)')}, LANG={os.environ.get('LANG', '(unset)')}, "
        f"LC_ALL={os.environ.get('LC_ALL', '(unset)')}",
        f"DOTNET_SYSTEM_GLOBALIZATION_INVARIANT: {os.environ.get('DOTNET_SYSTEM_GLOBALIZATION_INVARIANT', '(unset)')}",
        f"host ICU: {host_icu()}",
        f"host tzdata: {host_tzdata()}",
        f"harness commit: {harness_commit or 'unknown'}",
        f"harness, driver and copies unchanged from that commit: {yes_no(bool(harness_commit) and harness_status == '')}",
        f"harness SHA-256: {sha256_file(Path(__file__))}",
        f"driver template SHA-256: {sha256_file(DRIVER_TEMPLATE)}",
    ]
    for source in sources:
        chosen = [p for p in programs if p in source.pin.programs]
        if not chosen:
            continue
        lines.append(f"datasets ({source.collection}): {source.directory}, every copy equal to its {PIN_NAME}; the run's"
                     " population there, copies of Broiler.VM at the commits it names:")
        for pinned in population(source.pin, chosen):
            lines.append(f"  {pinned.name} {pinned.length} {pinned.sha256} (Broiler.VM {pinned.source_commit[:7]}:{pinned.source_path})")
    return lines


@dataclass(frozen=True)
class Source:
    """One collection a bundle reads: its name, its directory, and its pin, every copy checked."""
    collection: str
    directory: Path
    pin: Pin


def load_sources(bundle: Bundle, directories: dict[str, Path]) -> list[Source]:
    """The bundle's collections, each loaded and checked against its pin; a program pinned in two of them, or a
    bundle's program pinned in none, is refused."""
    sources = [Source(name, directories[name].resolve(), load_datasets(directories[name].resolve()))
               for name in bundle.collections]
    seen: dict[str, str] = {}
    for source in sources:
        for program in source.pin.programs:
            if program.name in seen:
                raise HarnessError(f"program {program.name} is pinned in both the {seen[program.name]} and the "
                                   f"{source.collection} collections")
            seen[program.name] = source.collection
    missing = [name for name in bundle.programs if name not in seen]
    if missing:
        raise HarnessError(f"bundle {bundle.name}'s programs {missing} are not in the {PIN_NAME} of "
                           f"{', '.join(bundle.collections)}")
    return sources


def source_of(sources: Sequence[Source], name: str) -> Source:
    return next(source for source in sources if any(p.name == name for p in source.pin.programs))


def collection_directories(arguments: argparse.Namespace) -> dict[str, Path]:
    return {"time": Path(arguments.datasets), "intl": Path(arguments.intl_datasets)}


def command_run(arguments: argparse.Namespace) -> int:
    bundle = BUNDLES[arguments.bundle]
    sources = load_sources(bundle, collection_directories(arguments))
    invariant = bool(arguments.invariant_globalization)
    tree = Path(arguments.tree).resolve()
    out = Path(arguments.out).resolve()
    out.mkdir(parents=True, exist_ok=True)
    started = datetime.datetime.now(datetime.timezone.utc).replace(microsecond=0).isoformat()

    built = build_shell(tree, out / "build.log") if arguments.build else None
    dll = Path(arguments.shell).resolve() if arguments.shell else tree / SHELL_DLL
    if not dll.is_file():
        raise HarnessError(f"no shell at {dll}; build it with --build, or name it with --shell")
    with (out / "shell-binaries.sha256").open("w", encoding="utf-8", newline="\n") as handle:
        for binary in sorted(dll.parent.glob("*.dll")):
            handle.write(f"{sha256_file(binary)}  {binary.name}\n")

    if arguments.program and not set(arguments.program) <= set(bundle.programs):
        raise HarnessError(f"--program names a program bundle {bundle.name}'s rule does not take; it takes "
                           f"{', '.join(bundle.programs)}")
    selected = [source_of(sources, name).pin.program(name) for name in bundle.programs
                if not arguments.program or name in arguments.program]
    copies = [pinned for source in sources
              for pinned in population(source.pin, [p for p in selected if p in source.pin.programs])]

    identity = identity_lines(bundle, arguments.label, tree, dll, built, sources, selected, started, invariant)
    summary = {
        "format": FORMAT,
        "bundle": bundle.name,
        "rule": bundle.rule,
        "label": arguments.label,
        "started": started,
        "invariantGlobalization": invariant,
        "shellCommit": git(tree, "rev-parse", "HEAD") or "unknown",
        "shellSha256": sha256_file(dll),
        "collections": {p.name: source_of(sources, p.name).collection for p in selected},
        "datasetsSourceCommits": {p.name: p.source_commit for p in copies},
        "copies": {p.name: p.sha256 for p in copies},
        "programs": {},
    }
    report = [f"Reference datasets, bundle {bundle.name}'s rule, part {'D' if invariant else 'C'}: one run"
              + (" under invariant globalization" if invariant else ""), ""] + identity + [""]
    failed = False
    for program in selected:
        run = run_program(dll, source_of(sources, program.name).directory, program, out, arguments.timeout, invariant)
        entry = write_program_outputs(run, out)
        summary["programs"][program.name] = entry
        control = entry["negativeControl"]
        failed |= not entry["completed"] or not control["shownAsDifference"] or not control["otherLinesUnchanged"]
        report += [
            f"{program.name} ({program.program}, expected: {program.answer} with {program.divergences})",
            f"  completed: {yes_no(entry['completed'])}"
            + (f"; uncaught {entry['uncaught']}" if entry["uncaught"] else "")
            + (f"; {'; '.join(entry['problems'])}" if entry["problems"] else ""),
            f"  shell exit code {entry['exitCode']}, {entry['seconds']} s",
            f"  lines: {entry['comparedLines']} compared ({entry['retainedLines']} retained, {entry['answeredLines']} answered)",
            f"  equal to the expected answer: {entry['equal']}; differing: {entry['differing']}",
            f"  divergences: {entry['divergenceEntries']} entries, {entry['divergencesUsed']} used",
            f"  negative control: line {control['line']}'s expected text changed in memory; shown as a difference: "
            f"{yes_no(control['shownAsDifference'])}; every other line unchanged: {yes_no(control['otherLinesUnchanged'])}",
        ]
        if entry["extraOutput"]:
            report.append(f"  other output from the shell: {len(entry['extraOutput'])} lines (summary.json)")
        differing = [r for r in run.results if not r.equal]
        report.append(f"  differing lines ({len(differing)}), by number and key; {program.name}.lines.tsv has each one's expected and actual text:")
        report += [f"    {r.number}\t{tsv_escape(r.key)}" for r in differing]
        report.append("")
    (out / "identity.txt").write_text("\n".join(identity) + "\n", encoding="utf-8", newline="\n")
    (out / "summary.json").write_text(json.dumps(summary, indent=2, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")
    (out / "report.txt").write_text("\n".join(report), encoding="utf-8", newline="\n")
    for program in selected:
        entry = summary["programs"][program.name]
        print(f"{program.name}: completed {yes_no(entry['completed'])}; {entry['equal']} of {entry['comparedLines']} "
              f"lines equal the expected answer, {entry['differing']} differ")
    return 1 if failed else 0


# ---- Comparing an after run with a base run ------------------------------------------------------------


def compare_rows(base: Sequence[dict[str, str]], after: Sequence[dict[str, str]]) -> tuple[list[list[str]], dict]:
    """Joins two runs' lines reports on the line number. Each line after against its expected answer, and
    against the same line at the base; a line one run lacks reads (no line) there."""
    by_number = {int(row["line"]): row for row in base}
    after_by_number = {int(row["line"]): row for row in after}
    numbers = sorted(set(by_number) | set(after_by_number))
    rows = []
    counts = {"lines": len(numbers), "afterEqual": 0, "afterDiffering": 0, "differingSameAsBase": 0,
              "differingMovedSinceBase": 0, "baseDifferingAfterEqual": 0, "baseEqualAfterDiffering": 0,
              "expectedDiffers": 0}
    for number in numbers:
        b = by_number.get(number)
        a = after_by_number.get(number)
        expected = (a or b)["expected"]
        if a and b and a["expected"] != b["expected"]:
            counts["expectedDiffers"] += 1
        base_actual = b["actual"] if b else NO_LINE
        after_actual = a["actual"] if a else NO_LINE
        after_equal = a is not None and a["equal"] == "yes"
        base_equal = b is not None and b["equal"] == "yes"
        same = after_actual == base_actual
        if after_equal:
            counts["afterEqual"] += 1
            if not base_equal:
                counts["baseDifferingAfterEqual"] += 1
        else:
            counts["afterDiffering"] += 1
            counts["differingSameAsBase" if same else "differingMovedSinceBase"] += 1
            if base_equal:
                counts["baseEqualAfterDiffering"] += 1
        rows.append([str(number), yes_no(after_equal), yes_no(same), yes_no(base_equal), line_key(expected),
                     expected, base_actual, after_actual])
    return rows, counts


def invariant_of(summary: dict) -> bool:
    """Whether a run was under invariant globalization; a run before format 3 was not taken so (its shell had the
    harness's own environment, which no retained run of bundles sjl-8-001 and sjl-10-001 set)."""
    return bool(summary.get("invariantGlobalization", False))


def bundle_of(summary: dict) -> str:
    """A run's bundle; a run of format 1 was bundle sjl-8-001's, the only one there was."""
    return summary.get("bundle", "sjl-8-001")


def source_commits(summary: dict) -> dict[str, str]:
    """A run's copies' Broiler.VM commits: per copy from pin 2 on, and one for every copy in a run of format 1."""
    if "datasetsSourceCommits" in summary:
        return summary["datasetsSourceCommits"]
    return {name: summary["datasetsSourceCommit"] for name in summary["copies"]}


def command_compare(arguments: argparse.Namespace) -> int:
    base_dir = Path(arguments.base).resolve()
    after_dir = Path(arguments.after).resolve()
    out = Path(arguments.out).resolve()
    base = json.loads((base_dir / "summary.json").read_text(encoding="utf-8"))
    after = json.loads((after_dir / "summary.json").read_text(encoding="utf-8"))
    if bundle_of(base) != bundle_of(after):
        raise HarnessError(f"the base is a run for bundle {bundle_of(base)} and the after run for {bundle_of(after)}")
    if base["copies"] != after["copies"] or source_commits(base) != source_commits(after):
        raise HarnessError("the two runs did not read the same copies, so they are not runs of one population")
    if invariant_of(base) != invariant_of(after):
        raise HarnessError("one run was under invariant globalization and the other was not")
    if set(base["programs"]) != set(after["programs"]):
        raise HarnessError(f"the base ran {sorted(base['programs'])} and the after run {sorted(after['programs'])}")
    out.mkdir(parents=True, exist_ok=True)
    bundle = BUNDLES[bundle_of(after)]
    result = {"format": FORMAT, "bundle": bundle.name, "rule": bundle.rule, "invariantGlobalization": invariant_of(after),
              "base": {"label": base["label"], "shellCommit": base["shellCommit"], "shellSha256": base["shellSha256"]},
              "after": {"label": after["label"], "shellCommit": after["shellCommit"], "shellSha256": after["shellSha256"]},
              "copies": after["copies"], "programs": {}}
    text = [f"Reference datasets, bundle {bundle.name}'s rule, part {'D' if invariant_of(after) else 'C'}: an after run"
            " against a base run" + (" under invariant globalization" if invariant_of(after) else ""), "",
            f"base: {base['label']}, shell commit {base['shellCommit']}, BroilerJS.dll {base['shellSha256']}",
            f"after: {after['label']}, shell commit {after['shellCommit']}, BroilerJS.dll {after['shellSha256']}",
            f"copies: Broiler.VM {' '.join(sorted(set(source_commits(after).values())))}, the same SHA-256 in both runs", ""]
    for name in after["programs"]:
        rows, counts = compare_rows(read_tsv(base_dir / f"{name}.lines.tsv"), read_tsv(after_dir / f"{name}.lines.tsv"))
        write_tsv(out / f"{name}.compare.tsv", COMPARE_COLUMNS, rows)
        counts["baseCompleted"] = base["programs"][name]["completed"]
        counts["afterCompleted"] = after["programs"][name]["completed"]
        result["programs"][name] = counts
        text += [
            f"{name}: base completed {yes_no(counts['baseCompleted'])}, after completed {yes_no(counts['afterCompleted'])}",
            f"  lines: {counts['lines']}; expected text the same in both runs on every line: {yes_no(counts['expectedDiffers'] == 0)}",
            f"  after, equal to the expected answer: {counts['afterEqual']}; differing: {counts['afterDiffering']}",
            f"  of those differing, the same as at the base: {counts['differingSameAsBase']}; moved since the base: {counts['differingMovedSinceBase']}",
            f"  differing at the base and equal after: {counts['baseDifferingAfterEqual']}; equal at the base and differing after: {counts['baseEqualAfterDiffering']}",
        ]
        moved = [r for r in rows if r[1] == "no" and r[2] == "no"]
        if moved:
            text.append(f"  differing lines that moved since the base ({len(moved)}), by number and key:")
            text += [f"    {r[0]}\t{tsv_escape(r[4])}" for r in moved]
        text.append("")
    (out / "compare.json").write_text(json.dumps(result, indent=2, ensure_ascii=False) + "\n", encoding="utf-8", newline="\n")
    (out / "compare.txt").write_text("\n".join(text), encoding="utf-8", newline="\n")
    print("\n".join(text))
    return 0


def command_verify(arguments: argparse.Namespace) -> int:
    directories = collection_directories(arguments)
    for name in ([arguments.collection] if arguments.collection else list(COLLECTIONS)):
        directory = directories[name].resolve()
        pin = load_datasets(directory)
        for pinned in pin.files.values():
            print(f"{pinned.sha256}  {name}/{pinned.name}  ({pinned.length} bytes, Broiler.VM {pinned.source_commit[:7]}:{pinned.source_path})")
        print(f"every copy of the {name} collection equals its {PIN_NAME}")
    return 0


def main(argv: Sequence[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n", 1)[0])
    commands = parser.add_subparsers(dest="command", required=True)

    run = commands.add_parser("run", help="run the programs in one shell and compare each line with its expected answer")
    run.add_argument("--bundle", required=True, choices=sorted(BUNDLES),
                     help="the bundle whose rule's part C the run is taken for, which fixes the programs it runs")
    run.add_argument("--tree", default=str(REPOSITORY), help="the checkout whose shell runs (default: this one)")
    run.add_argument("--build", action="store_true", help="build the shell in Release in that checkout first")
    run.add_argument("--shell", help="a BroilerJS.dll to run instead of the checkout's Release build")
    run.add_argument("--out", required=True, help="the directory the run's outputs are written to")
    run.add_argument("--label", default="run", help="a name for the run, such as base or after")
    run.add_argument("--program", action="append", help="run only this one of the bundle's programs (repeatable)")
    run.add_argument("--timeout", type=float, default=1800.0, help="seconds each program may take (default 1800)")
    run.add_argument("--invariant-globalization", action="store_true",
                     help=f"run the shell with {INVARIANT_VARIABLE}=1 (bundle sjl-9-001's part D)")
    run.add_argument("--datasets", default=str(DATASETS), help=argparse.SUPPRESS)
    run.add_argument("--intl-datasets", default=str(INTL_DATASETS), help=argparse.SUPPRESS)
    run.set_defaults(handler=command_run)

    compare = commands.add_parser("compare", help="join an after run with a base run, line by line")
    compare.add_argument("--base", required=True)
    compare.add_argument("--after", required=True)
    compare.add_argument("--out", required=True)
    compare.set_defaults(handler=command_compare)

    verify = commands.add_parser("verify", help="check every copy against its collection's datasets.pin")
    verify.add_argument("--collection", choices=sorted(COLLECTIONS), help="check this collection only (default: both)")
    verify.add_argument("--datasets", default=str(DATASETS), help=argparse.SUPPRESS)
    verify.add_argument("--intl-datasets", default=str(INTL_DATASETS), help=argparse.SUPPRESS)
    verify.set_defaults(handler=command_verify)

    arguments = parser.parse_args(argv)
    try:
        return arguments.handler(arguments)
    except HarnessError as error:
        print(f"run_reference_datasets.py: {error}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    sys.exit(main())
