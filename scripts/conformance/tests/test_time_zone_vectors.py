"""Tests of the time-zone area's generator (corpus revision 21, milestone SJL-8.4).

The committed files' shape runs everywhere. Regenerating the area needs the host's `zic`, which tzref.py compiles the
pinned release with, and is skipped where it is absent; so is the check of the clause ids against the pinned
specification (BROILER_ECMA262_SPEC, or Broiler.VM's archive beside this repository).
"""

from __future__ import annotations

import hashlib
import os
import re
import shutil
import subprocess
import sys
import tempfile
import unittest
from collections import Counter
from pathlib import Path

CONFORMANCE_DIR = Path(__file__).resolve().parents[1]
REPOSITORY = CONFORMANCE_DIR.parents[1]
if str(CONFORMANCE_DIR) not in sys.path:
    sys.path.insert(0, str(CONFORMANCE_DIR))

import generate_time_zone_vectors as generator  # noqa: E402

CORPUS = REPOSITORY / "conformance" / "vectors"
COMMITTED = CORPUS / "time-zones"
FILES = ("backzone", "extrapolation", "identifiers", "lookup", "release", "transitions", "wall-times")
SPEC_VARIABLE = "BROILER_ECMA262_SPEC"
DEFAULT_SPEC = REPOSITORY.parent / "Broiler.VM" / "src" / "Broiler.VM.Profile.JavaScript" / "docs" / "specification" / \
    "ecma-262-es2026-spec.html"
NEEDS_ZIC = unittest.skipUnless(shutil.which("zic") is not None, "no zic on this host")

# Every operation the area pins, and the shape of its inputs.
OPERATIONS = {
    "GetOffsetNanosecondsFor": r'str:"[^"]*" bigint:-?\d+',
    "GetNamedTimeZoneNextTransition": r'str:"[^"]*" bigint:-?\d+',
    "GetNamedTimeZonePreviousTransition": r'str:"[^"]*" bigint:-?\d+',
    "GetNamedTimeZoneNextTransition, count": r'str:"[^"]*" bigint:-?\d+ bigint:-?\d+',
    "GetPossibleEpochNanoseconds": r'str:"[^"]*"( int:-?\d+){6}',
    "GetEpochNanosecondsFor": r'str:"[^"]*"( int:-?\d+){6} str:"(compatible|earlier|later|reject)"',
    "GetStartOfDay": r'str:"[^"]*"( int:-?\d+){3}',
    "Temporal.ZonedDateTime.prototype.hoursInDay": r'str:"[^"]*"( int:-?\d+){3}',
    "Temporal.ZonedDateTime.prototype.startOfDay": r'str:"[^"]*" bigint:-?\d+',
    "Temporal.ZonedDateTime, timeZoneId": r'str:"[^"]*"',
    "Temporal.Instant.prototype.toZonedDateTimeISO, timeZoneId": r'str:"[^"]*"',
    "TimeZoneEquals": r'str:"[^"]*" str:"[^"]*"',
    "AvailablePrimaryTimeZoneIdentifiers": r'str:"timeZone"',
    "AvailablePrimaryTimeZoneIdentifiers, count": r'str:"timeZone"',
    "AvailablePrimaryTimeZoneIdentifiers, includes": r'str:"[^"]*"',
    "Intl.DateTimeFormat, resolved timeZone": r'str:"[^"]*"',
    "Intl.DateTimeFormat, hour": r'str:"[^"]*" int:-?\d+',
    "Intl.Locale.prototype.getTimeZones": r'str:"en-[A-Z]{2}"',
}


def read_records(path: Path) -> list[dict[str, str]]:
    """The records of a vector file, by the schema's section 1."""
    records, current = [], {}
    for line in path.read_text(encoding="utf-8").split("\n"):
        if line.startswith("#") or line == "schema: 1":
            continue
        if not line:
            if current:
                records.append(current)
                current = {}
            continue
        key, _, value = line.partition(": ")
        assert key not in current, (path.name, line)
        current[key] = value
    if current:
        records.append(current)
    return records


def committed() -> dict[str, dict[str, str]]:
    out = {}
    for name in FILES:
        for record in read_records(COMMITTED / f"{name}.vectors"):
            out[record["id"]] = record
    return out


@NEEDS_ZIC
class GeneratorTests(unittest.TestCase):
    def test_two_runs_write_the_same_bytes(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            first, second = Path(scratch) / "first", Path(scratch) / "second"
            for out in (first, second):
                result = subprocess.run([sys.executable, "-I", str(CONFORMANCE_DIR / "generate_time_zone_vectors.py"),
                                         "--out", str(out)], capture_output=True, text=True, check=False)
                self.assertEqual(0, result.returncode, result.stderr)
            names = sorted(p.name for p in first.iterdir())
            self.assertEqual([f"{name}.vectors" for name in FILES], names)
            for name in names:
                self.assertEqual((first / name).read_bytes(), (second / name).read_bytes(), name)

    def test_the_committed_corpus_is_what_the_generator_writes(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            self.assertEqual(0, generator.main(["--out", scratch]))
            produced = sorted(p.name for p in Path(scratch).iterdir())
            self.assertEqual(sorted(p.name for p in COMMITTED.glob("*.vectors")), produced)
            for name in produced:
                self.assertEqual((COMMITTED / name).read_bytes(), (Path(scratch) / name).read_bytes(), name)

    def test_a_tampered_archive_is_refused_and_nothing_is_written(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            home = Path(scratch) / "time"
            shutil.copytree(generator.T.DEFAULT_HOME / "tzdb", home / "tzdb")
            shutil.copytree(generator.T.DEFAULT_HOME / "cldr", home / "cldr")
            archive = home / "tzdb" / "tzdata2026e.tar.gz"
            data = bytearray(archive.read_bytes())
            data[-100] ^= 1
            archive.write_bytes(bytes(data))
            out = Path(scratch) / "out"
            self.assertEqual(2, generator.main(["--out", str(out), "--home", str(home)]))
            self.assertFalse(out.exists())


class CommittedFileTests(unittest.TestCase):
    """The committed files' shape, read the schema's way; no compiler needed."""

    @classmethod
    def setUpClass(cls) -> None:
        cls.vectors = committed()

    def test_the_generator_writes_the_revision_REVISIONS_md_ends_at(self) -> None:
        revisions = (CORPUS / "REVISIONS.md").read_text(encoding="utf-8")
        rows = [int(line.split("|")[1]) for line in revisions.splitlines() if line[:2] == "| " and line.split("|")[1].strip().isdigit()]
        self.assertEqual(list(range(1, len(rows) + 1)), rows)
        self.assertEqual(generator.CORPUS_REVISION, rows[-1])
        self.assertTrue(all(v["since"] == "21" for v in self.vectors.values()))

    def test_the_area_stays_a_few_thousand_vectors_one_per_operation_and_input(self) -> None:
        self.assertGreater(len(self.vectors), 3000)
        self.assertLess(len(self.vectors), 5000)
        keys = Counter((v["op"], v["in"]) for v in self.vectors.values())
        self.assertEqual([], [k for k, n in keys.items() if n > 1])

    def test_every_operation_is_one_the_library_level_runner_answers(self) -> None:
        for vector_id, vector in self.vectors.items():
            self.assertEqual("time-zones", vector["area"], vector_id)
            self.assertIn(vector["op"], OPERATIONS, vector_id)
            self.assertRegex(vector["in"], "^" + OPERATIONS[vector["op"]] + "$", vector_id)

    def test_every_script_is_one_line_of_ascii_and_every_vector_is_classified_as_the_milestone_asks(self) -> None:
        for vector_id, vector in self.vectors.items():
            self.assertTrue(all(ord(c) < 0x80 for c in vector["script"]), vector_id)
            self.assertNotIn("observed:", vector["prov"], vector_id)
            self.assertIn("ref:tzref@1", vector["prov"].split(" "), vector_id)
            if vector["class"] == "impl-defined":
                # Only the formatter's resolved name of a non-primary identifier (row Z8), whose expectations
                # differ, and a region's zones (row Z12), whose expectations agree.
                self.assertIn(vector["op"], ("Intl.DateTimeFormat, resolved timeZone", "Intl.Locale.prototype.getTimeZones"),
                              vector_id)
                if vector["op"] == "Intl.Locale.prototype.getTimeZones":
                    self.assertEqual(vector["out"], vector["per-engine.broiler-js"], vector_id)
                    self.assertEqual(vector["out"], vector["per-engine.broiler-vm"], vector_id)
                else:
                    self.assertEqual(vector["out"], vector["per-engine.broiler-js"], vector_id)
                    self.assertNotEqual(vector["out"], vector["per-engine.broiler-vm"], vector_id)
                    self.assertIn("row Z8", vector["note"], vector_id)
            else:
                self.assertIn(vector["class"], ("normative", "divergence"), vector_id)
                self.assertEqual(vector["class"] == "divergence", any(k.startswith("divergence.") for k in vector), vector_id)
                if vector["op"] != "Intl.Locale.prototype.getTimeZones":
                    self.assertTrue(any(e.startswith("spec:es2026@0248456c#") for e in vector["prov"].split(" ")), vector_id)

    def test_every_divergence_field_is_one_time_zone_divergences_txt_lists(self) -> None:
        reasons, listed = {}, {}
        if generator.DIVERGENCES.exists():
            for line in generator.DIVERGENCES.read_text(encoding="utf-8").splitlines():
                if not line or line.startswith("#"):
                    continue
                if line.startswith("reason "):
                    key, _, text = line[len("reason "):].partition(": ")
                    reasons[key] = text
                    continue
                engine, key, vector_id = line.split(" ")
                listed[(engine, vector_id)] = reasons[key]
        fields = {(k[len("divergence."):], vector_id): v for vector_id, vector in self.vectors.items()
                  for k, v in vector.items() if k.startswith("divergence.")}
        self.assertEqual(listed, fields)
        # Each reason names the correspondence row it confirms and the decision it falls under.
        for text in reasons.values():
            self.assertRegex(text, r"rows? [ZA]\d+")
            self.assertRegex(text, r"SJL-D-(16|20)")

    def test_every_policy_difference_has_vectors_on_both_sides(self) -> None:
        groups = Counter(vector_id.split("/")[1] for vector_id in self.vectors)
        for group in ("release", "backzone", "extrapolation-offset", "extrapolation-transition", "extrapolation-wall",
                      "extrapolation-day", "extrapolation-format", "identifier", "factory", "primary", "supported",
                      "formatter-zone", "region", "case", "fold", "iso-string", "offset-id", "offset-digits", "etc-gmt",
                      "unknown", "offset", "transition", "wall", "day"):
            self.assertGreater(groups[group], 0, group)
        backzone = [v for v in self.vectors.values() if v["id"].startswith("time-zones/backzone/")]
        names = {v["in"].split('"')[1] for v in backzone}
        self.assertEqual(set(generator.BACKZONE_NAMES), names)
        # Before 1970 and after it, for each name.
        after = {v["in"].split('"')[1] for v in backzone if v["op"] == "GetOffsetNanosecondsFor"
                 and v["in"].endswith(f"bigint:{generator.E('1980-07-01T12:00')}")}
        self.assertEqual(names, after)

    def test_every_clause_cited_is_in_the_pinned_specification(self) -> None:
        spec = Path(os.environ.get(SPEC_VARIABLE) or DEFAULT_SPEC)
        if not spec.is_file():
            self.skipTest(f"no pinned specification at {spec}; set {SPEC_VARIABLE}")
        data = spec.read_bytes()
        pin = (CORPUS / "sources" / "ecma262.pin").read_text(encoding="utf-8")
        self.assertIn(f"sha256 {hashlib.sha256(data).hexdigest()}\n", pin)
        ids = set(re.findall(r'id="([^"]+)"', data.decode("utf-8")))
        cited = {e.split("#", 1)[1] for v in self.vectors.values() for e in v["prov"].split(" ") if e.startswith("spec:")}
        self.assertTrue(cited)
        self.assertEqual(set(), cited - ids)


if __name__ == "__main__":
    unittest.main()
