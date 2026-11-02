"""Tests of the regular-expression area's generator (corpus revisions 31 and 32, milestone SJL-11.3).

The committed files are checked against what the generator writes, where the pinned Unicode data is (unicoderef's
BROILER_UNICODE_PINS, or a Broiler.Unicode checkout beside this repository's); writing them takes a minute or two,
since some vectors' subjects are a million characters long. The check of every clause id against the pinned ECMA-262
text reads BROILER_ECMA262_SPEC, or Broiler.VM's archived edition beside this repository, and is skipped where neither
is. The shape of the committed files is checked everywhere.
"""

from __future__ import annotations

import os
import re
import sys
import tempfile
import unittest
from collections import Counter
from functools import lru_cache
from pathlib import Path

CONFORMANCE_DIR = Path(__file__).resolve().parents[1]
REPOSITORY = CONFORMANCE_DIR.parents[1]
if str(CONFORMANCE_DIR) not in sys.path:
    sys.path.insert(0, str(CONFORMANCE_DIR))

import generate_regexp_vectors as generator  # noqa: E402
import unicoderef as U  # noqa: E402

CORPUS = REPOSITORY / "conformance" / "vectors"
COMMITTED = CORPUS / "regexp"
DEFAULT_SPEC = REPOSITORY.parent / "Broiler.VM" / "src" / "Broiler.VM.Profile.JavaScript" / "docs" / "specification" / \
    "ecma-262-es2026-spec.html"
SPEC = Path(os.environ.get("BROILER_ECMA262_SPEC") or DEFAULT_SPEC)
NEEDS_DATA = unittest.skipUnless(U.pins_directory().is_dir(), f"no pinned Unicode data at {U.pins_directory()}")


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


def inputs(field: str) -> list:
    """A record's `in`, as Python values: str for str:, int for int:."""
    out, i = [], 0
    while i < len(field):
        if field.startswith('str:"', i):
            j, text = i + 5, []
            while field[j] != '"':
                if field[j] == "\\":
                    nxt = field[j + 1]
                    if nxt == "u":
                        text.append(chr(int(field[j + 2:j + 6], 16)))
                        j += 6
                    else:
                        text.append(nxt)
                        j += 2
                else:
                    text.append(field[j])
                    j += 1
            out.append("".join(text))
            i = j + 1
        else:
            j = field.find(" ", i)
            j = len(field) if j < 0 else j
            token = field[i:j]
            out.append(int(token[4:]) if token.startswith("int:") else token)
            i = j
        if i < len(field):
            assert field[i] == " ", field
            i += 1
    return out


@lru_cache(maxsize=1)
def committed() -> dict[str, list[dict[str, str]]]:
    return {path.stem: read_records(path) for path in sorted(COMMITTED.glob("*.vectors"))}


def all_records() -> list[dict[str, str]]:
    return [record for records in committed().values() for record in records]


@NEEDS_DATA
class GeneratorTests(unittest.TestCase):
    def test_the_committed_corpus_is_what_the_generator_writes(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            out = Path(scratch) / "regexp"
            self.assertEqual(0, generator.main(["--out", str(out)]))
            produced = sorted(p.name for p in out.iterdir())
            self.assertEqual(sorted(p.name for p in COMMITTED.glob("*.vectors")), produced)
            for name in produced:
                self.assertEqual((COMMITTED / name).read_bytes(), (out / name).read_bytes(), name)

    def test_the_two_serialisations_agree_on_a_result_written_by_hand(self) -> None:
        # /(?<a>x)|(?<a>y)/d on "y": index 0, elements "y", undefined, "y"; groups.a and indices.groups.a the second's.
        expected = ("lastIndex=0;index=0;length=3;0=1:y;1=u;2=1:y;groups=1;<a>=1:y;indices=3;0=0-1;1=u;2=0-1;"
                    "igroups=1;<a>=0-1")
        self.assertEqual("value: str:\"" + expected + "\"", generator.outcome_exec("(?<a>x)|(?<a>y)", "d", "y"))
        self.assertIn('t += ";index=" + m.index + ";length=" + m.length', generator.CANONICAL_JS)
        self.assertEqual("value: str:\"lastIndex=0;null\"", generator.outcome_exec("a", "", "b"))

    def test_a_vector_inside_the_budget_margin_is_refused(self) -> None:
        saved = list(generator.CASES)
        try:
            with self.assertRaises(generator.GeneratorError):
                # [a]* on 5,000 a needs about 5,000 steps: neither 100 times a budget of 1,000 nor a hundredth of it.
                generator.add_budget("budget", "control", "[a]*", "", ("a", 5000, ""), 1000, (), "a control")
        finally:
            generator.CASES[:] = saved


class CommittedFileTests(unittest.TestCase):
    def test_the_generator_writes_the_revision_REVISIONS_md_ends_at(self) -> None:
        revisions = (CORPUS / "REVISIONS.md").read_text(encoding="utf-8")
        rows = [int(m.group(1)) for m in re.finditer(r"^\| (\d+) \| \d{4}-\d{2}-\d{2} \|", revisions, re.M)]
        self.assertEqual(generator.CORPUS_REVISION, rows[-1])
        # The area was added at revision 31; revision 32 classifies both engines' first runs and adds no vector.
        self.assertEqual(31, generator.SINCE)
        self.assertLessEqual(generator.SINCE, generator.CORPUS_REVISION)

    def test_one_file_per_kind_and_ids_by_file(self) -> None:
        self.assertEqual(sorted(generator.FILES), sorted(committed()))
        for name, records in committed().items():
            ids = [r["id"] for r in records]
            self.assertEqual(len(ids), len(set(ids)), name)
            self.assertTrue(all(i.startswith(f"regexp/{name}/") for i in ids), name)
            self.assertTrue(all(r["area"] == "regexp" and r["since"] == "31" for r in records), name)

    def test_every_expected_value_cites_the_pinned_edition_and_most_the_reference(self) -> None:
        for record in all_records():
            entries = record["prov"].split(" ")
            self.assertTrue(any(e.startswith(generator.SPEC) for e in entries), record["id"])
            self.assertFalse(any(e.startswith("observed:") for e in entries), record["id"])
        cited = sum(1 for r in all_records() if generator.REF in r["prov"].split(" "))
        self.assertGreater(cited, 0.9 * len(all_records()))

    def test_divergence_fields_are_the_classified_differences_and_every_class_has_its_fields(self) -> None:
        classified = generator.read_divergences(generator.DIVERGENCES)
        self.assertTrue(classified)
        ids = {r["id"] for r in all_records()}
        self.assertEqual(set(), set(classified) - ids)
        for record in all_records():
            fields = {k[len("divergence."):]: v for k, v in record.items() if k.startswith("divergence.")}
            self.assertEqual(classified.get(record["id"], {}), fields, record["id"])
            self.assertIn(record["class"], ("normative", "impl-defined", "divergence"), record["id"])
            self.assertEqual(record["class"] == "divergence", bool(fields) and "per-engine.broiler-js" not in record,
                             record["id"])
            self.assertFalse(fields and record["out"].startswith("status: "), record["id"])
            if record["class"] == "impl-defined":
                self.assertIn("per-engine.broiler-js", record, record["id"])
                self.assertIn("per-engine.broiler-vm", record, record["id"])
            else:
                self.assertFalse(any(k.startswith("per-engine") for k in record), record["id"])
            for engine, text in fields.items():
                # Each reason names a row the vector's note names.
                row = re.match(r"row ([A-Z][0-9]+[a-z]?)\b", text)
                self.assertIsNotNone(row, (record["id"], engine))
                self.assertRegex(record["note"], rf"correspondence rows? [A-Z0-9a-z, ]*\b{row.group(1)}\b",
                                 (record["id"], engine))

    @NEEDS_DATA  # write_files builds the corpus, which reads the pinned UCD, as GeneratorTests' do
    def test_the_divergence_file_refuses_an_unknown_id_and_a_status_vector(self) -> None:
        with self.assertRaises(generator.GeneratorError):
            generator.write_files(Path(tempfile.mkdtemp()), {"regexp/syntax/none/0001": {"broiler-js": "x"}})
        status = next(r["id"] for r in committed()["budget"] if r["out"].startswith("status: "))
        with self.assertRaises(generator.GeneratorError):
            generator.write_files(Path(tempfile.mkdtemp()), {status: {"broiler-js": "x"}})

    def test_every_impl_defined_vector_is_annex_b_nesting_or_decision_d41(self) -> None:
        for record in all_records():
            if record["class"] != "impl-defined":
                continue
            note = record["note"]
            self.assertTrue("B.1.2 alone" in note or "nesting depth" in note or "decision D41" in note, record["id"])
            if "B.1.2 alone" in note:
                self.assertEqual(record["out"], record["per-engine.broiler-js"], record["id"])
                self.assertEqual(record["out"], record["per-engine.broiler-vm"], record["id"])
                values = inputs(record["in"])
                flags = values[-1] if record["op"] == "RegExpInitialize, nested pattern" else values[1]
                self.assertFalse("u" in flags or "v" in flags, record["id"])
            if "nesting depth" in note:
                self.assertEqual(generator.SE, record["per-engine.broiler-js"], record["id"])
                self.assertEqual(generator.SE, record["per-engine.broiler-vm"], record["id"])

    def test_status_vectors_are_the_budget_file_s_and_every_status_is_exhausted(self) -> None:
        for name, records in committed().items():
            for record in records:
                if record["out"].startswith("status: "):
                    self.assertEqual("budget", name, record["id"])
                    self.assertEqual("status: exhausted", record["out"], record["id"])
                    self.assertEqual("RegExpBuiltinExec, step budget", record["op"], record["id"])
        self.assertTrue(any(r["out"] == "status: exhausted" for r in committed()["budget"]))

    def test_every_script_is_one_line_of_printable_ascii(self) -> None:
        for record in all_records():
            self.assertTrue(all(0x20 <= ord(c) <= 0x7E for c in record["script"]), record["id"])
            self.assertTrue(all(0x20 <= ord(c) <= 0x7E for c in record["note"]), record["id"])

    def test_every_literal_vector_s_script_holds_its_body_and_flags(self) -> None:
        for record in committed()["literals"]:
            if record["op"] != "IsValidRegularExpressionLiteral":
                continue
            body, flags = inputs(record["in"])
            self.assertEqual(f"(function () {{ return /{body}/{flags}; }}), true", record["script"], record["id"])

    def test_every_row_with_a_behavioural_difference_is_reached(self) -> None:
        notes = " ".join(r["note"] for r in all_records())
        for row in generator.REACHED:
            self.assertRegex(notes, rf"correspondence rows? [A-Z0-9, ]*\b{row}\b", row)
        self.assertEqual(set(), set(generator.REACHED) & set(generator.NOT_REACHED))

    @unittest.skipUnless(SPEC.is_file(), "set BROILER_ECMA262_SPEC to the pinned edition's spec.html")
    def test_every_clause_id_is_one_the_pinned_text_has(self) -> None:
        text = SPEC.read_text(encoding="utf-8")
        ids = set(re.findall(r'id="([^"]+)"', text))
        cited = Counter()
        for record in all_records():
            for entry in record["prov"].split(" "):
                if entry.startswith(generator.SPEC):
                    cited[entry[len(generator.SPEC):]] += 1
        self.assertEqual([], sorted(c for c in cited if c not in ids))


if __name__ == "__main__":
    unittest.main()
