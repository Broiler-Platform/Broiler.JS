"""Tests of the Temporal area's generator (corpus revision 24, milestone SJL-10.2).

The committed files' shape runs everywhere. Regenerating the area needs the reference: the Temporal proposal's
polyfill at e8cc03fc with its npm dependencies, in BROILER_TEMPORAL_POLYFILL or scripts/conformance/temporalref/, and
Node 22.22.0, in BROILER_NODE or on PATH; those tests are skipped where either is absent. The check of the clause ids
against the pinned proposals' texts reads BROILER_TEMPORAL_SPEC (the proposal's spec/ directory) and
BROILER_ERA_SPEC (the era proposal's spec.emu), and is skipped where they are not given.
"""

from __future__ import annotations

import json
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

import generate_temporal_vectors as generator  # noqa: E402

CORPUS = REPOSITORY / "conformance" / "vectors"
COMMITTED = CORPUS / "temporal"
FILES = ("balancing", "calendars", "differences", "durations", "grammar", "rounding")
POLYFILL = Path(os.environ.get("BROILER_TEMPORAL_POLYFILL") or generator.DEFAULT_POLYFILL)
NODE = os.environ.get("BROILER_NODE") or shutil.which("node")


def node_version() -> str | None:
    if not NODE:
        return None
    try:
        return subprocess.run([NODE, "--version"], capture_output=True, text=True, check=False).stdout.strip()
    except OSError:
        return None


NEEDS_REFERENCE = unittest.skipUnless(
    (POLYFILL / "lib").is_dir() and (POLYFILL / "node_modules").is_dir() and node_version() == "v22.22.0",
    "no polyfill at e8cc03fc and Node 22.22.0 here; set BROILER_TEMPORAL_POLYFILL and BROILER_NODE")

# Every row of the correspondence with a behavioural difference between the engines, or between an engine and the
# specification: the vectors reach each.
DIFFERENCE_ROWS = ("R6", "R7", "B5", "D3", "D4", "D6", "D7", "D8", "D11", "F4", "F7", "G3", "G4", "G6", "G8", "G9",
                   "G11", "G12", "G13", "G16", "G17", "G18", "G19", "G20", "G22", "C2", "C5", "C6", "C9", "C12", "C13",
                   "C15", "C16", "C17")


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


def rows_named(note: str) -> set[str]:
    """The correspondence rows a note names: "row G3", "rows D7 and D8", "rows G11 to G14", "rows R1, R2 and R3"."""
    out = set()
    for match in re.finditer(r"\brows? ((?:[RBDFGCSZA]\d+)(?:(?:, | and | to )[RBDFGCSZA]\d+)*)", note):
        parts = re.split(r"(, | and | to )", match.group(1))
        previous = None
        for i in range(0, len(parts), 2):
            row = parts[i]
            if i and parts[i - 1] == " to " and previous and previous[0] == row[0]:
                out.update(f"{row[0]}{n}" for n in range(int(previous[1:]), int(row[1:]) + 1))
            out.add(row)
            previous = row
    return out


def arguments(out: Path) -> list[str]:
    return ["--out", str(out), "--polyfill", str(POLYFILL), "--node", str(NODE)]


@NEEDS_REFERENCE
class GeneratorTests(unittest.TestCase):
    def test_two_runs_write_the_same_bytes(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            first, second = Path(scratch) / "first", Path(scratch) / "second"
            for out in (first, second):
                result = subprocess.run([sys.executable, "-I", str(CONFORMANCE_DIR / "generate_temporal_vectors.py")]
                                        + arguments(out), capture_output=True, text=True, check=False)
                self.assertEqual(0, result.returncode, result.stderr)
            names = sorted(p.name for p in first.iterdir())
            self.assertEqual([f"{name}.vectors" for name in FILES], names)
            for name in names:
                self.assertEqual((first / name).read_bytes(), (second / name).read_bytes(), name)

    def test_the_committed_corpus_is_what_the_generator_writes(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            self.assertEqual(0, generator.main(arguments(Path(scratch))))
            produced = sorted(p.name for p in Path(scratch).iterdir())
            self.assertEqual(sorted(p.name for p in COMMITTED.glob("*.vectors")), produced)
            for name in produced:
                self.assertEqual((COMMITTED / name).read_bytes(), (Path(scratch) / name).read_bytes(), name)

    def test_a_polyfill_that_is_not_the_pinned_one_is_refused_and_nothing_is_written(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            copy = Path(scratch) / "polyfill"
            shutil.copytree(POLYFILL / "lib", copy / "lib")
            (copy / "node_modules").symlink_to((POLYFILL / "node_modules").resolve(), target_is_directory=True)
            target = copy / "lib" / "duration.mjs"
            target.write_text(target.read_text(encoding="utf-8") + "\n", encoding="utf-8")
            out = Path(scratch) / "out"
            self.assertEqual(2, generator.main(["--out", str(out), "--polyfill", str(copy), "--node", str(NODE)]))
            self.assertFalse(out.exists())


class CommittedFileTests(unittest.TestCase):
    """The committed files' shape, read the schema's way; no reference needed."""

    @classmethod
    def setUpClass(cls) -> None:
        cls.vectors = committed()

    def test_the_generator_writes_the_revision_REVISIONS_md_ends_at(self) -> None:
        revisions = (CORPUS / "REVISIONS.md").read_text(encoding="utf-8")
        rows = [int(line.split("|")[1]) for line in revisions.splitlines()
                if line[:2] == "| " and line.split("|")[1].strip().isdigit()]
        self.assertEqual(list(range(1, len(rows) + 1)), rows)
        self.assertEqual(generator.CORPUS_REVISION, rows[-1])
        self.assertTrue(all(v["since"] == "24" for v in self.vectors.values()))

    def test_the_area_is_one_vector_per_operation_and_input(self) -> None:
        self.assertGreater(len(self.vectors), 10000)
        self.assertLess(len(self.vectors), 12000)
        keys = Counter((v["op"], v["in"]) for v in self.vectors.values())
        self.assertEqual([], [k for k, n in keys.items() if n > 1])

    def test_every_script_is_one_line_of_ascii_that_declares_no_global(self) -> None:
        for vector_id, vector in self.vectors.items():
            self.assertEqual("temporal", vector["area"], vector_id)
            self.assertTrue(all(0x20 <= ord(c) < 0x7F for c in vector["script"]), vector_id)
            self.assertNotRegex(vector["script"], r"^(var|let|const|function|class) ", vector_id)

    def test_every_expected_value_is_the_reference_s_or_the_pinned_text_s_under_decision_D7(self) -> None:
        overruled = 0
        for vector_id, vector in self.vectors.items():
            entries = vector["prov"].split(" ")
            self.assertFalse(any(e.startswith("observed:") for e in entries), vector_id)
            self.assertTrue(all(e.startswith(("proposal:temporal@e8cc03fc#", "proposal:intl-era-monthcode@5833eae#",
                                              "spec:es2026@0248456c#", "ref:temporalref@1", "overruled:temporalref@1="))
                                for e in entries), vector_id)
            if any(e.startswith("overruled:") for e in entries):
                overruled += 1
                self.assertNotIn("ref:temporalref@1", entries, vector_id)
                self.assertIn("decision D7", vector["note"], vector_id)
                polyfill = entries[-1][len("overruled:temporalref@1="):]
                self.assertNotEqual(polyfill.replace(":", ": ", 1), vector["out"], vector_id)
            else:
                self.assertIn("ref:temporalref@1", entries, vector_id)
        self.assertEqual(len(generator.D7_OVERRIDES) + len(generator.D7_READ_OVERRIDES), overruled)

    def test_decision_D7_s_overrides_are_each_listed_once_with_both_answers(self) -> None:
        by_key = {(v["op"], v["in"]): v for v in self.vectors.values()}
        for override in generator.D7_OVERRIDES:
            vector = by_key[(override["op"], override["in"])]
            self.assertEqual(override["text"], vector["out"])
            self.assertIn(f"overruled:temporalref@1={generator.compact(override['polyfill'])}", vector["prov"])
            self.assertIn("row D8", vector["note"])
        for group, op, inputs, text, polyfill in generator.D7_READ_OVERRIDES:
            self.assertIn(group, generator.D7_READ_REASONS)
            vector = by_key[(op, inputs)]
            self.assertEqual(text, vector["out"])
            self.assertIn(f"overruled:temporalref@1={generator.compact(polyfill)}", vector["prov"])

    def test_every_class_is_normative_or_a_classified_divergence(self) -> None:
        for vector_id, vector in self.vectors.items():
            self.assertIn(vector["class"], ("normative", "divergence"), vector_id)
            self.assertEqual(vector["class"] == "divergence", any(k.startswith("divergence.") for k in vector), vector_id)

    def test_every_divergence_field_is_one_temporal_divergences_txt_lists(self) -> None:
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
        # Each reason names the correspondence row it confirms and the work package that answers it.
        for text in reasons.values():
            self.assertRegex(text, r"\brows? [RBDFGC]\d+")
            self.assertRegex(text, r"SJL-10\.[45]")

    def test_every_row_with_a_behavioural_difference_has_vectors(self) -> None:
        named = set()
        for vector in self.vectors.values():
            named.update(rows_named(vector["note"]))
        self.assertEqual([], [row for row in DIFFERENCE_ROWS if row not in named])

    def test_every_rounding_mode_and_increment_is_vectored(self) -> None:
        seen = set()
        for vector in self.vectors.values():
            if vector["op"] == "Temporal.Duration.prototype.round, toString":
                seen.add(vector["in"])
        for unit, increments in generator.INCREMENTS.items():
            for increment in increments:
                for mode in generator.MODES:
                    self.assertTrue(any(f'str:"{unit}" undefined int:{increment} str:"{mode}" undefined' in key
                                        for key in seen), (unit, increment, mode))

    def test_the_header_records_every_npm_package_the_lock_pins(self) -> None:
        lock = json.loads((generator.DEFAULT_POLYFILL / "package-lock.json").read_text(encoding="utf-8"))
        pinned = {k[len("node_modules/"):]: v["version"] for k, v in lock["packages"].items() if k}
        self.assertEqual(pinned, generator.NPM_PACKAGES)
        self.assertEqual(24, len(generator.POLYFILL_LIB))

    def test_every_proposal_cited_is_a_pinned_revision(self) -> None:
        pins = {}
        for name in ("temporal", "intl-era-monthcode"):
            text = (CORPUS / "sources" / f"{name}.pin").read_text(encoding="utf-8")
            pins[name] = re.search(r"^revision ([0-9a-f]{40})$", text, re.M).group(1)
        self.assertEqual(generator.TEMPORAL_REVISION, pins["temporal"])
        self.assertEqual(generator.ERA_REVISION, pins["intl-era-monthcode"])
        for vector_id, vector in self.vectors.items():
            for entry in vector["prov"].split(" "):
                if entry.startswith("proposal:"):
                    name, _, rest = entry[len("proposal:"):].partition("@")
                    self.assertTrue(pins[name].startswith(rest.split("#")[0]), vector_id)

    def test_every_clause_cited_is_in_the_pinned_texts(self) -> None:
        spec, era = os.environ.get("BROILER_TEMPORAL_SPEC"), os.environ.get("BROILER_ERA_SPEC")
        if not spec or not era:
            self.skipTest("set BROILER_TEMPORAL_SPEC and BROILER_ERA_SPEC to the pinned proposals' sources")
        ids = {"temporal": set(), "intl-era-monthcode": set()}
        for path in Path(spec).glob("*.html"):
            ids["temporal"].update(re.findall(r'id="([^"]+)"', path.read_text(encoding="utf-8")))
        ids["intl-era-monthcode"].update(re.findall(r'id="([^"]+)"', Path(era).read_text(encoding="utf-8")))
        for vector_id, vector in self.vectors.items():
            for entry in vector["prov"].split(" "):
                if entry.startswith("proposal:"):
                    name = entry[len("proposal:"):].split("@")[0]
                    self.assertIn(entry.split("#", 1)[1], ids[name], vector_id)


if __name__ == "__main__":
    unittest.main()
