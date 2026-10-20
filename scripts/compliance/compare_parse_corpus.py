#!/usr/bin/env python3
"""Compare the parse-corpus tool's runs: bundle SJL-12-001's decision rule, parts C and D.

The tool ``docs/evidence/sjl-12-001/tools/parse-corpus`` writes one report per run. This script
reads them and does three things, each a subcommand:

``acceptance``
    Part C's comparison. Joins the base run (the corpus commit) and the after run on (input,
    variant, entry point) and lists every difference in the outcome, the exception type and, for a
    refusal, the message. A crash is compared by its type alone, since the rule records a crash
    with its type. The script decides no class. A difference is admitted only when a classification
    file, written by a person and retained beside the report, names it with one of the rule's
    classes (M, X or D) and a reason, and the row meets that class's conditions as the rule states
    them; the script checks the conditions it can read from the two runs and the changelog given
    with ``--changelog``: for M, that it contains the old and the new message; for X, that it
    contains the text the classification cites (``"changelog"``) as the correction that names the
    resolved crash.

``control``
    Part C's control: writes a copy of the base report with one outcome flipped (the first accepted
    row becomes refused) and one message changed (the first refused row's message gains
    `` [control]``), compares the base with the copy, and succeeds only if the comparison reports
    exactly those two differences.

``determinism``
    Part D's comparison: per input and variant of the population, the four hashes of each dump -
    run 1 (manifest order), run 2 (reverse order) and the two slots of run 3 (two threads) - for
    the parser's tree and for the for-loop pass's. The population is every input and variant
    accepted at entry point P in the part C report given with ``--population`` (the after run's),
    or in run 1 if none is given. A dump the tool could not complete (``dump-failed:<type>`` in a
    hash column) is a failure in any run, never an agreement. ``--require-pass`` fails the
    comparison unless every run found the pass's entry point. ``--expect-differ`` turns the
    comparison into a control's check: it succeeds only if at least one input's hashes differ
    between the two runs named.

Both runs of a comparison must be complete (every row the header announces, and the end line),
over the same manifest (its SHA-256), with no ``--only`` selection, and every value must be one the
tool writes. ``acceptance`` and ``determinism`` require ``--manifest``, the corpus commit's
``conformance/parse/corpus.manifest``: its SHA-256 must be the runs', and the runs must report
every input and variant it names (part C's completeness), at both entry points for part C.

Exit status: 0 when every difference is admitted (``acceptance``), the control reports exactly its
two differences (``control``), or the four hashes agree (``determinism``, or differ as expected
with ``--expect-differ``); 1 otherwise; 2 when the runs are not comparable or an input is
malformed.
"""

from __future__ import annotations

import argparse
import gzip
import hashlib
import json
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

REPORT_FORMAT = ["broiler-js-parse-corpus-report", "1"]
ACCEPTANCE_COLUMNS = ["id", "variant", "entry", "outcome", "type", "message"]
DUMP_COLUMNS = ["id", "variant", "slot", "parse", "parser-sha256", "pass", "pass-sha256"]
MANIFEST_COLUMNS = "id\torigin\tpath\tsha256\tgoal\tvariants\tnegative\teval-family\tprovenance"
ADMITTED_CLASSES = ("M", "X", "D")
ENTRIES = ("P", "K")
# Part C's outcomes, as the rule defines them.
OUTCOMES = ("accepted", "refused", "crashed")
# Part D's pass column: what the tool did with the for-loop pass for an accepted input.
PASS_STATES = ("prepared", "threw", "absent")
# A hash column's value where the tool could not complete the dump.
DUMP_FAILED = "dump-failed:"
# The message the parser's for-loop desugaring refuses with at the rule commit (class D).
DESUGARING_MESSAGE = "Unknown token"
CONTROL_SUFFIX = " [control]"


class NotComparable(Exception):
    pass


@dataclass
class Report:
    path: str
    header: dict[str, list[list[str]]]
    columns: list[str]
    rows: list[list[str]]

    def field(self, key: str) -> list[str]:
        values = self.header.get(key)
        if not values:
            raise NotComparable(f"{self.path}: the header has no {key}")
        return values[0]

    def value(self, key: str) -> str:
        return "\t".join(self.field(key))


@dataclass
class Manifest:
    path: str
    digest: str
    rows: dict[str, dict[str, Any]] = field(default_factory=dict)


def _read_text(path: Path) -> str:
    data = path.read_bytes()
    if data[:2] == b"\x1f\x8b":
        data = gzip.decompress(data)
    return data.decode("utf-8")


def read_report(path: Path) -> Report:
    try:
        text = _read_text(path)
    except (OSError, UnicodeDecodeError, EOFError, gzip.BadGzipFile) as exc:
        raise NotComparable(f"{path}: {exc}") from exc
    if not text.endswith("\n"):
        raise NotComparable(f"{path}: the report does not end with a line feed (cut short?)")
    lines = text[:-1].split("\n")
    header: dict[str, list[list[str]]] = {}
    index = 0
    while index < len(lines) and lines[index].startswith("#% "):
        parts = lines[index][3:].split("\t")
        header.setdefault(parts[0], []).append(parts[1:])
        index += 1
    if index >= len(lines):
        raise NotComparable(f"{path}: no column line")
    columns = lines[index].split("\t")
    index += 1
    if not lines or not lines[-1].startswith("#% end\t"):
        raise NotComparable(f"{path}: no end line; the run did not complete")
    body = lines[index:-1]
    report = Report(str(path), header, columns, [line.split("\t") for line in body])
    if report.field("format") != REPORT_FORMAT:
        raise NotComparable(f"{path}: not a parse-corpus report of format {' '.join(REPORT_FORMAT)}")
    announced = int(report.value("rows"))
    ended = int(lines[-1].split("\t")[1])
    if not announced == ended == len(report.rows):
        raise NotComparable(
            f"{path}: the header announces {announced} rows, the end line {ended}, and the report has {len(report.rows)}"
        )
    for number, row in enumerate(report.rows, start=index + 1):
        if len(row) != len(columns):
            raise NotComparable(f"{path}:{number}: {len(row)} fields, the column line has {len(columns)}")
    return report


def read_manifest(path: Path) -> Manifest:
    try:
        data = path.read_bytes()
    except OSError as exc:
        raise NotComparable(f"{path}: {exc}") from exc
    manifest = Manifest(str(path), hashlib.sha256(data).hexdigest())
    lines = data.decode("utf-8").rstrip("\n").split("\n")
    index = 0
    while index < len(lines) and lines[index].startswith("#"):
        index += 1
    if index >= len(lines) or lines[index] != MANIFEST_COLUMNS:
        raise NotComparable(f"{path}: not a parse-corpus manifest of format 1 (column line)")
    for line in lines[index + 1 :]:
        parts = line.split("\t")
        if len(parts) != 9:
            raise NotComparable(f"{path}: a row with {len(parts)} fields")
        manifest.rows[parts[0]] = {"path": parts[2], "goal": parts[4], "variants": parts[5].split(",")}
    return manifest


def unescape(field_text: str) -> str:
    """The text of a report field, as the tool's escaping wrote it (``-`` is empty)."""
    if field_text == "-":
        return ""
    out: list[str] = []
    index = 0
    while index < len(field_text):
        character = field_text[index]
        if character != "\\" or index + 1 >= len(field_text):
            out.append(character)
            index += 1
            continue
        code = field_text[index + 1]
        if code == "u" and index + 6 <= len(field_text):
            out.append(chr(int(field_text[index + 2 : index + 6], 16)))
            index += 6
            continue
        out.append({"\\": "\\", "t": "\t", "n": "\n", "r": "\r", '"': '"'}.get(code, "\\" + code))
        index += 2
    return "".join(out)


def _check_run(report: Report, mode: str) -> None:
    if report.value("mode") != mode:
        raise NotComparable(f"{report.path}: a {report.value('mode')} run, not a {mode} run")
    if report.value("selection") != "all":
        raise NotComparable(f"{report.path}: a partial run ({report.value('selection')})")
    expected = ACCEPTANCE_COLUMNS if mode == "acceptance" else DUMP_COLUMNS
    if report.columns != expected:
        raise NotComparable(f"{report.path}: the columns are {report.columns}, not {expected}")


def _check_manifest(reports: list[Report], manifest: Manifest | None) -> str:
    digests = {report.value("manifest-sha256") for report in reports}
    if len(digests) != 1:
        raise NotComparable("the runs read different manifests: " + ", ".join(
            f"{report.path} {report.value('manifest-sha256')}" for report in reports))
    digest = digests.pop()
    if manifest is not None and manifest.digest != digest:
        raise NotComparable(f"the runs read the manifest {digest}; {manifest.path} is {manifest.digest}")
    return digest


def acceptance_rows(report: Report) -> dict[tuple[str, str, str], tuple[str, str, str]]:
    rows: dict[tuple[str, str, str], tuple[str, str, str]] = {}
    for row in report.rows:
        key = (row[0], row[1], row[2])
        if key in rows:
            raise NotComparable(f"{report.path}: the row {' '.join(key)} appears twice")
        _check_outcome(report, key, (row[3], row[4], row[5]))
        rows[key] = (row[3], row[4], row[5])
    return rows


def _check_outcome(report: Report, key: tuple[str, str, str], outcome: tuple[str, str, str]) -> None:
    """A part C row must hold one of the rule's outcomes as the tool writes it."""
    where = f"{report.path}: the row {' '.join(key)}"
    if key[2] not in ENTRIES:
        raise NotComparable(f"{where} names the entry point {key[2]!r}, not one of {', '.join(ENTRIES)}")
    kind, kind_type, _ = outcome
    if kind not in OUTCOMES:
        raise NotComparable(f"{where} has the outcome {kind!r}, not one of {', '.join(OUTCOMES)}")
    if kind == "accepted" and outcome[1:] != ("-", "-"):
        raise NotComparable(f"{where} is accepted with a type or message")
    if kind != "accepted" and kind_type == "-":
        raise NotComparable(f"{where} is {kind} with no exception type")


def is_hash(value: str) -> bool:
    return len(value) == 64 and all(character in "0123456789abcdef" for character in value)


def is_dump_failure(value: str) -> bool:
    return value.startswith(DUMP_FAILED) and len(value) > len(DUMP_FAILED)


def _check_dump_row(report: Report, row: list[str], pass_found: bool) -> None:
    """A part D row must be one the tool writes: P's outcome, then the dumps it took."""
    where = f"{report.path}: the row {row[0]} {row[1]} slot {row[2]}"
    parse, parser_hash, pass_state, pass_hash = row[3:7]
    if parse not in OUTCOMES:
        raise NotComparable(f"{where} has the parse outcome {parse!r}, not one of {', '.join(OUTCOMES)}")
    if parse != "accepted":
        if (parser_hash, pass_state, pass_hash) != ("-", "-", "-"):
            raise NotComparable(f"{where} is {parse} at P but has a dump")
        return
    if not (is_hash(parser_hash) or is_dump_failure(parser_hash)):
        raise NotComparable(f"{where} has the parser dump {parser_hash!r}, neither a SHA-256 nor {DUMP_FAILED}<type>")
    if pass_state not in PASS_STATES:
        raise NotComparable(f"{where} has the pass state {pass_state!r}, not one of {', '.join(PASS_STATES)}")
    if (pass_state == "absent") == pass_found:
        raise NotComparable(f"{where} has the pass state {pass_state!r}, but the header says the pass was "
                            + ("found" if pass_found else "not found"))
    if pass_state == "absent":
        if pass_hash != "-":
            raise NotComparable(f"{where} has a pass dump with no pass")
    elif not (is_hash(pass_hash) or (pass_state == "prepared" and is_dump_failure(pass_hash))):
        raise NotComparable(f"{where} has the pass dump {pass_hash!r}, neither a SHA-256 nor {DUMP_FAILED}<type>")


def _expected_acceptance_keys(manifest: Manifest) -> set[tuple[str, str, str]]:
    return {
        (identifier, variant, entry)
        for identifier, row in manifest.rows.items()
        for variant in row["variants"]
        for entry in ENTRIES
    }


def differs(base: tuple[str, str, str], after: tuple[str, str, str]) -> bool:
    """Part C's comparison: the outcome, the exception type and, for a refusal, the message."""
    if base[0] != after[0]:
        return True
    if base[0] in ("refused", "crashed") and base[1] != after[1]:
        return True
    return base[0] == "refused" and base[2] != after[2]


def _is_syntax_error_refusal(outcome: tuple[str, str, str]) -> bool:
    return outcome[0] == "refused" and outcome[1].endswith(":SyntaxError")


def class_conditions(
    klass: str,
    key: tuple[str, str, str],
    base: dict[tuple[str, str, str], tuple[str, str, str]],
    after: dict[tuple[str, str, str], tuple[str, str, str]],
    changelog: str | None,
    given: dict[str, Any] | None = None,
) -> list[str]:
    """The conditions of the rule's class that this row does not meet (empty: all are met)."""
    identifier, variant, entry = key
    was, now = base[key], after[key]
    unmet: list[str] = []
    if klass == "M":
        if not (was[0] == now[0] == "refused"):
            unmet.append("M needs a refusal at both commits")
        if was[1] != now[1]:
            unmet.append("M needs the same exception type at both commits")
        if was[2] == now[2]:
            unmet.append("M needs a changed message")
        if changelog is None:
            unmet.append("M needs the after commit's Broiler.JS/CHANGELOG.md (--changelog)")
        else:
            for side, message in (("old", unescape(was[2])), ("new", unescape(now[2]))):
                if not message or message not in changelog:
                    unmet.append(f"the changelog does not contain the {side} message")
    elif klass == "X":
        if entry != "K":
            unmet.append("X is a row of entry point K")
        if was[0] != "crashed":
            unmet.append("X needs a crash at the base")
        if not (now[0] == "accepted" or _is_syntax_error_refusal(now)):
            unmet.append("X needs acceptance, or a refusal with a SyntaxError, after")
        cited = (given or {}).get("changelog") if isinstance(given, dict) else None
        if changelog is None:
            unmet.append("X needs the after commit's Broiler.JS/CHANGELOG.md (--changelog)")
        elif not isinstance(cited, str) or not cited.strip():
            unmet.append('X needs the classification to cite the changelog text that names the correction ("changelog")')
        elif cited not in changelog:
            unmet.append("the changelog does not contain the text the classification cites")
    elif klass == "D":
        other = (identifier, variant, "K")
        if entry != "P":
            unmet.append("D is a row of entry point P")
        if not (was[0] == "refused" and was[1].endswith("FastParseException") and unescape(was[2]) == DESUGARING_MESSAGE):
            unmet.append(f'D needs a refusal at P at the base by the desugaring ("{DESUGARING_MESSAGE}")')
        if now[0] != "accepted":
            unmet.append("D needs acceptance at P after")
        if other not in base or other not in after:
            unmet.append("D needs the input's K rows")
        elif not (after[other][0] == "refused" and after[other] == base[other]):
            unmet.append("D needs a refusal at K after with the exception type and message it had at K at the base")
    else:
        unmet.append(f"{klass} is not a class of part C (M, X, D)")
    return unmet


def compare_acceptance(
    base_report: Report,
    after_report: Report,
    manifest: Manifest | None = None,
    classifications: dict[str, Any] | None = None,
    changelog: str | None = None,
) -> dict[str, Any]:
    for report in (base_report, after_report):
        _check_run(report, "acceptance")
    digest = _check_manifest([base_report, after_report], manifest)
    base = acceptance_rows(base_report)
    after = acceptance_rows(after_report)
    if set(base) != set(after):
        only_base = sorted(set(base) - set(after))
        only_after = sorted(set(after) - set(base))
        sample = [" ".join(key) for key in (only_base + only_after)[:5]]
        raise NotComparable(f"the row sets differ: {len(only_base)} only at base, {len(only_after)} only after, e.g. {sample}")
    if manifest is not None:
        expected = _expected_acceptance_keys(manifest)
        if set(base) != expected:
            missing = sorted(expected - set(base))
            extra = sorted(set(base) - expected)
            raise NotComparable(f"the runs do not report every manifest input and variant at P and K: {len(missing)} missing, {len(extra)} not in the manifest")

    classifications = classifications or {}
    used: set[str] = set()
    differences = []
    for key in sorted(base):
        if not differs(base[key], after[key]):
            continue
        identifier, variant, entry = key
        other_entry = "K" if entry == "P" else "P"
        other = (identifier, variant, other_entry)
        label = "|".join(key)
        entry_record: dict[str, Any] = {
            "id": identifier,
            "variant": variant,
            "entry": entry,
            "base": dict(zip(("outcome", "type", "message"), base[key])),
            "after": dict(zip(("outcome", "type", "message"), after[key])),
            "otherEntry": {
                "entry": other_entry,
                "base": dict(zip(("outcome", "type", "message"), base.get(other, ("-", "-", "-")))),
                "after": dict(zip(("outcome", "type", "message"), after.get(other, ("-", "-", "-")))),
            },
        }
        if manifest is not None and identifier in manifest.rows:
            entry_record["path"] = manifest.rows[identifier]["path"]
        given = classifications.get(label)
        if given is None:
            entry_record["verdict"] = "unclassified"
        else:
            used.add(label)
            klass = given.get("class") if isinstance(given, dict) else None
            reason = given.get("reason") if isinstance(given, dict) else None
            unmet = class_conditions(str(klass), key, base, after, changelog, given if isinstance(given, dict) else None) if klass else ["no class given"]
            if not reason:
                unmet.append("no reason given")
            entry_record["classification"] = given
            if unmet:
                entry_record["verdict"] = "refused classification"
                entry_record["unmetConditions"] = unmet
            else:
                entry_record["verdict"] = "admitted"
        differences.append(entry_record)

    admitted = sum(1 for row in differences if row["verdict"] == "admitted")
    return {
        "part": "C",
        "manifestSha256": digest,
        "base": base_report.path,
        "after": after_report.path,
        "baseAssemblies": base_report.header.get("assembly", []),
        "afterAssemblies": after_report.header.get("assembly", []),
        "rows": len(base),
        "differences": differences,
        "admitted": admitted,
        "notAdmitted": len(differences) - admitted,
        "unusedClassifications": sorted(set(classifications) - used),
        "allDifferencesAdmitted": admitted == len(differences),
    }


def make_control(base_report: Report) -> tuple[Report, list[tuple[str, str, str]]]:
    """A copy of a part C report with one outcome flipped and one message changed."""
    _check_run(base_report, "acceptance")
    rows = [list(row) for row in base_report.rows]
    flipped = next((index for index, row in enumerate(rows) if row[3] == "accepted"), None)
    changed = next((index for index, row in enumerate(rows) if row[3] == "refused" and index != flipped), None)
    if flipped is None or changed is None:
        raise NotComparable(f"{base_report.path}: the control needs an accepted row and a refused row")
    rows[flipped][3:6] = ["refused", "control", "control: an outcome flipped from accepted"]
    rows[changed][5] = (rows[changed][5] if rows[changed][5] != "-" else "") + CONTROL_SUFFIX
    planted = [tuple(rows[index][:3]) for index in (flipped, changed)]
    return Report(base_report.path + " (control)", base_report.header, base_report.columns, rows), planted  # type: ignore[return-value]


def write_report(report: Report, path: Path) -> None:
    lines = []
    for key, values in report.header.items():
        for value in values:
            lines.append("#% " + "\t".join([key, *value]))
    lines.append("\t".join(report.columns))
    lines.extend("\t".join(row) for row in report.rows)
    lines.append(f"#% end\t{len(report.rows)}")
    data = ("\n".join(lines) + "\n").encode("utf-8")
    if path.name.endswith(".gz"):
        data = gzip.compress(data, mtime=0)
    path.write_bytes(data)


DUMP_RUNS = ("1", "2", "3A", "3B")


def _dump_rows(report: Report, slots: tuple[str, ...]) -> dict[tuple[str, str], dict[str, tuple[str, str, str, str]]]:
    rows: dict[tuple[str, str], dict[str, tuple[str, str, str, str]]] = {}
    pass_found = report.value("for-loop-pass").startswith("found")
    for row in report.rows:
        if row[2] not in slots:
            raise NotComparable(f"{report.path}: a row of slot {row[2]}, not one of {slots}")
        _check_dump_row(report, row, pass_found)
        slot_rows = rows.setdefault((row[0], row[1]), {})
        if row[2] in slot_rows:
            raise NotComparable(f"{report.path}: the row {row[0]} {row[1]} slot {row[2]} appears twice")
        slot_rows[row[2]] = (row[3], row[4], row[5], row[6])
    return rows


def compare_determinism(
    run1: Report,
    run2: Report,
    run3: Report,
    population_report: Report | None = None,
    manifest: Manifest | None = None,
    require_pass: bool = False,
    expect_differ: tuple[str, str] | None = None,
) -> dict[str, Any]:
    for report in (run1, run2, run3):
        _check_run(report, "dump")
    for report, order, threads in ((run1, "manifest", "1"), (run2, "reverse", "1"), (run3, None, "2")):
        if order is not None and report.value("order") != order:
            raise NotComparable(f"{report.path}: order {report.value('order')}, not {order}")
        if report.value("threads") != threads:
            raise NotComparable(f"{report.path}: {report.value('threads')} thread(s), not {threads}")
    reports = [run1, run2, run3] + ([population_report] if population_report is not None else [])
    digest = _check_manifest(reports, manifest)

    by_run: dict[str, dict[tuple[str, str], tuple[str, str, str, str]]] = {}
    for name, report, slot in (("1", run1, "1"), ("2", run2, "1")):
        by_run[name] = {key: slots[slot] for key, slots in _dump_rows(report, ("1",)).items()}
    three = _dump_rows(run3, ("A", "B"))
    by_run["3A"] = {key: slots["A"] for key, slots in three.items() if "A" in slots}
    by_run["3B"] = {key: slots["B"] for key, slots in three.items() if "B" in slots}
    key_sets = {name: set(rows) for name, rows in by_run.items()}
    if len({frozenset(keys) for keys in key_sets.values()}) != 1:
        raise NotComparable("the dump runs report different inputs and variants: " + ", ".join(
            f"{name} {len(keys)}" for name, keys in key_sets.items()))
    if manifest is not None:
        expected = {(identifier, variant) for identifier, row in manifest.rows.items() for variant in row["variants"]}
        if key_sets["1"] != expected:
            raise NotComparable("the dump runs do not report every manifest input and variant")

    if population_report is not None:
        _check_run(population_report, "acceptance")
        population = sorted(
            (identifier, variant)
            for (identifier, variant, entry), outcome in acceptance_rows(population_report).items()
            if entry == "P" and outcome[0] == "accepted"
        )
        missing = [key for key in population if key not in key_sets["1"]]
        if missing:
            raise NotComparable(f"{len(missing)} input(s) of the population are not in the dump runs, e.g. {missing[:5]}")
    else:
        population = sorted(key for key, value in by_run["1"].items() if value[0] == "accepted")

    pass_status = {name: report.value("for-loop-pass") for name, report in (("1", run1), ("2", run2), ("3", run3))}
    pass_found = {name: status.startswith("found") for name, status in pass_status.items()}

    differences = []
    pair_differs = 0
    for key in population:
        values = {name: by_run[name][key] for name in DUMP_RUNS}
        problems = []
        if any(value[0] != "accepted" for value in values.values()):
            problems.append("parse")
        if any(is_dump_failure(value[1]) for value in values.values()):
            problems.append("parser dump failed")
        elif len({value[1] for value in values.values()}) != 1:
            problems.append("parser-sha256")
        if any(is_dump_failure(value[3]) for value in values.values()):
            problems.append("pass dump failed")
        elif len({(value[2], value[3]) for value in values.values()}) != 1:
            problems.append("pass")
        if expect_differ is not None:
            left, right = values[expect_differ[0]], values[expect_differ[1]]
            if left[1] != right[1] or (left[2], left[3]) != (right[2], right[3]):
                pair_differs += 1
        if problems:
            differences.append({
                "id": key[0],
                "variant": key[1],
                "differs": problems,
                "path": manifest.rows[key[0]]["path"] if manifest is not None and key[0] in manifest.rows else None,
                "runs": {name: dict(zip(("parse", "parser-sha256", "pass", "pass-sha256"), values[name])) for name in DUMP_RUNS},
            })

    passes_ok = all(pass_found.values()) or not require_pass
    result: dict[str, Any] = {
        "part": "D",
        "manifestSha256": digest,
        "runs": {"1": run1.path, "2": run2.path, "3": run3.path},
        "population": len(population),
        "populationSource": population_report.path if population_report is not None else run1.path + " (accepted at P)",
        "forLoopPass": pass_status,
        "passFoundInEveryRun": all(pass_found.values()),
        "differences": differences,
        "agree": not differences and passes_ok,
    }
    if expect_differ is not None:
        result["expectDiffer"] = {"runs": list(expect_differ), "inputsThatDiffer": pair_differs}
    return result


def _write_json(path: Path | None, payload: dict[str, Any]) -> None:
    if path is not None:
        path.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8", newline="\n")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    commands = parser.add_subparsers(dest="command", required=True)

    acceptance = commands.add_parser("acceptance", help="part C: the base run against the after run")
    acceptance.add_argument("--base", type=Path, required=True, help="the base run's report (the corpus commit)")
    acceptance.add_argument("--after", type=Path, required=True, help="the after run's report")
    acceptance.add_argument("--manifest", type=Path, required=True, help="the corpus commit's conformance/parse/corpus.manifest: the digest and every input and variant")
    acceptance.add_argument("--classifications", type=Path, help='the classification file: {"id|variant|entry": {"class": "M", "reason": "..."}}; an X also cites "changelog"')
    acceptance.add_argument("--changelog", type=Path, help="the after commit's Broiler.JS/CHANGELOG.md, for classes M and X")
    acceptance.add_argument("--output", type=Path, help="write the comparison here as JSON")

    control = commands.add_parser("control", help="part C's control: a copy of the base report with two planted differences")
    control.add_argument("--base", type=Path, required=True, help="the base run's report")
    control.add_argument("--out", type=Path, required=True, help="where to write the control copy (.gz to compress)")
    control.add_argument("--output", type=Path, help="write the comparison of the base with the copy here as JSON")

    determinism = commands.add_parser("determinism", help="part D: the four hashes of each dump")
    determinism.add_argument("--run1", type=Path, required=True, help="the dump run in the manifest's order")
    determinism.add_argument("--run2", type=Path, required=True, help="the dump run in the reverse order")
    determinism.add_argument("--run3", type=Path, required=True, help="the dump run on two threads")
    determinism.add_argument("--population", type=Path, help="the part C report whose P acceptances are the population")
    determinism.add_argument("--manifest", type=Path, required=True, help="the corpus commit's conformance/parse/corpus.manifest: the digest and every input and variant")
    determinism.add_argument("--require-pass", action="store_true", help="fail unless every run found the for-loop pass")
    determinism.add_argument("--expect-differ", metavar="RUN:RUN", help="a control: succeed only if some input differs between these runs (1, 2, 3A, 3B)")
    determinism.add_argument("--output", type=Path, help="write the comparison here as JSON")

    args = parser.parse_args(argv)
    try:
        manifest = read_manifest(args.manifest) if getattr(args, "manifest", None) is not None else None
        if args.command == "acceptance":
            classifications: dict[str, Any] = {}
            if args.classifications is not None:
                classifications = json.loads(args.classifications.read_text(encoding="utf-8"))
                if not isinstance(classifications, dict):
                    raise NotComparable(f"{args.classifications}: not a JSON object")
            changelog = args.changelog.read_text(encoding="utf-8") if args.changelog is not None else None
            result = compare_acceptance(read_report(args.base), read_report(args.after), manifest, classifications, changelog)
            _write_json(args.output, result)
            for row in result["differences"]:
                klass = row.get("classification", {}).get("class", "") if isinstance(row.get("classification"), dict) else ""
                print(f"{row['verdict']:<22} {row['id']} {row['variant']} {row['entry']}: "
                      f"{row['base']['outcome']} {row['base']['type']} -> {row['after']['outcome']} {row['after']['type']} {klass}".rstrip())
                for condition in row.get("unmetConditions", []):
                    print(f"{'':<22}   unmet: {condition}")
            print(f"manifest {result['manifestSha256']} ({args.manifest})")
            print(f"rows {result['rows']}; differences {len(result['differences'])}; admitted {result['admitted']}; "
                  f"not admitted {result['notAdmitted']}")
            return 0 if result["allDifferencesAdmitted"] else 1

        if args.command == "control":
            base = read_report(args.base)
            copy, planted = make_control(base)
            write_report(copy, args.out)
            result = compare_acceptance(base, read_report(args.out))
            found = sorted((row["id"], row["variant"], row["entry"]) for row in result["differences"])
            result["planted"] = [list(key) for key in planted]
            result["controlReported"] = found == sorted(planted)
            _write_json(args.output, result)
            for key in planted:
                print("planted " + " ".join(key))
            print(f"rows {result['rows']}; differences {len(found)}: " + ", ".join(" ".join(key) for key in found))
            print("control reported" if result["controlReported"] else "control NOT reported")
            return 0 if result["controlReported"] else 1

        expect = None
        if args.expect_differ is not None:
            parts = args.expect_differ.split(":")
            if len(parts) != 2 or any(part not in DUMP_RUNS for part in parts):
                raise NotComparable(f"--expect-differ {args.expect_differ}: name two of {', '.join(DUMP_RUNS)}")
            expect = (parts[0], parts[1])
        result = compare_determinism(
            read_report(args.run1),
            read_report(args.run2),
            read_report(args.run3),
            read_report(args.population) if args.population is not None else None,
            manifest,
            args.require_pass,
            expect,
        )
        _write_json(args.output, result)
        for name, status in result["forLoopPass"].items():
            print(f"run {name}: for-loop pass {status}")
        for row in result["differences"]:
            print(f"differs {row['id']} {row['variant']}: {', '.join(row['differs'])}")
        print(f"manifest {result['manifestSha256']} ({args.manifest})")
        print(f"population {result['population']}; differences {len(result['differences'])}")
        if expect is not None:
            count = result["expectDiffer"]["inputsThatDiffer"]
            print(f"inputs that differ between run {expect[0]} and run {expect[1]}: {count}")
            return 0 if count > 0 else 1
        if args.require_pass and not result["passFoundInEveryRun"]:
            print("the for-loop pass was not found in every run")
        return 0 if result["agree"] else 1
    except (NotComparable, OSError, ValueError) as exc:
        print(f"not comparable: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    sys.exit(main())
