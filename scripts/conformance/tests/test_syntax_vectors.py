"""Tests of the syntax area's generator (corpus revisions 29 and 30, milestone SJL-13, work package SJL-13.2).

The generator states every expected value itself, from the specification, so the committed files are checked against
what it writes everywhere. The check against the reference Node.js v26.12.0 that bundle sjl-9-001's reference.txt
names reads BROILER_SYNTAX_NODE (or BROILER_INTL_NODE, the same reference) and is skipped where neither names it. The
checks of every clause id against the pinned ECMA-262 text and of every test262 path against the pinned checkout read
BROILER_ECMA262_SPEC (the edition's spec.html, sources/ecma262.pin) and BROILER_TEST262 (a test262 checkout at
ccaac100), and are skipped where they are not given.
"""

from __future__ import annotations

import os
import re
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

import generate_syntax_vectors as generator  # noqa: E402

CORPUS = REPOSITORY / "conformance" / "vectors"
COMMITTED = CORPUS / "syntax"
FILES = ("annex-b", "expressions", "functions-and-classes", "lexical", "scripts", "statements")
REFERENCE_NODE = os.environ.get("BROILER_SYNTAX_NODE") or os.environ.get("BROILER_INTL_NODE")
SPEC = os.environ.get("BROILER_ECMA262_SPEC")
TEST262 = os.environ.get("BROILER_TEST262")


def version(node: str | None) -> str | None:
    if not node:
        return None
    try:
        return subprocess.run([node, "--version"], capture_output=True, text=True, check=False).stdout.strip()
    except OSError:
        return None


NEEDS_REFERENCE = unittest.skipUnless(version(REFERENCE_NODE) == generator.REFERENCE_VERSION,
                                      "no reference here; set BROILER_SYNTAX_NODE to Node.js v26.12.0")


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
            assert record["id"] not in out, record["id"]
            out[record["id"]] = record
    return out


def unquote(value: str) -> str:
    """A `str:` value's String, as code units joined into Python text (surrogates kept as they are)."""
    assert value.startswith('str:"') and value.endswith('"'), value
    body, out, i = value[5:-1], [], 0
    while i < len(body):
        if body[i] == "\\":
            if body[i + 1] == "u":
                out.append(chr(int(body[i + 2:i + 6], 16)))
                i += 6
                continue
            out.append(body[i + 1])
            i += 2
            continue
        out.append(body[i])
        i += 1
    return "".join(out)


class GeneratorTests(unittest.TestCase):
    def test_the_committed_corpus_is_what_the_generator_writes(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            first, second = Path(scratch) / "a", Path(scratch) / "b"
            self.assertEqual(0, generator.main(["--out", str(first)]))
            self.assertEqual(0, generator.main(["--out", str(second)]))
            produced = sorted(p.name for p in first.iterdir())
            self.assertEqual(sorted(p.name for p in COMMITTED.glob("*.vectors")), produced)
            for name in produced:
                self.assertEqual((first / name).read_bytes(), (second / name).read_bytes(), name)
                self.assertEqual((COMMITTED / name).read_bytes(), (first / name).read_bytes(), name)

    def test_every_syntax_row_is_covered_or_named_with_its_reason(self) -> None:
        cases = generator.build()
        rows = generator.syntax_rows()
        self.assertEqual([], generator.check_rows(cases, rows))
        coverage = generator.coverage(cases, rows)
        self.assertEqual(len(rows), sum(len(v) for v in coverage.values()))
        self.assertTrue(all(reason for _, reason in generator.UNCOVERED.values()))
        self.assertTrue(set(generator.UNCOVERED) <= set(rows))

    def test_a_row_that_loses_its_vector_or_an_uncovered_row_that_gains_one_is_refused(self) -> None:
        cases = generator.build()
        rows = generator.syntax_rows()
        covered = next(row for row in rows if row not in generator.UNCOVERED)
        without = [case for case in cases if covered not in case.rows or case.refused]
        self.assertIn(f"{covered}: no accepted vector, against UNCOVERED", generator.check_rows(without, rows))
        uncovered = next(row for row, (half, _) in generator.UNCOVERED.items() if half == "both")
        extra = cases + [generator.Case("scripts", "x", "ParseScript", "1", "1", generator.V(1), (uncovered,), (),
                                        ("sec-scripts",), (), "x")]
        self.assertIn(f"{uncovered}: an accepted vector, against UNCOVERED", generator.check_rows(extra, rows))

    def test_a_vector_with_no_source_or_a_script_outside_printable_ascii_is_refused(self) -> None:
        saved = list(generator.CASES)
        try:
            with self.assertRaises(generator.GeneratorError):
                generator.add("scripts", "x", "1", generator.V(1), note="no source")
            with self.assertRaises(generator.GeneratorError):
                generator.add("scripts", "x", "1", generator.V(1), clauses=("sec-scripts",), op="ParseScript",
                              script="eval(\"1\")")
            with self.assertRaises(generator.GeneratorError):
                generator.add("scripts", "x", "1", generator.V(1), clauses=("sec-scripts",), op="PerformEval",
                              script="eval(\"é\")")
        finally:
            generator.CASES[:] = saved

    @unittest.skipUnless(SPEC and Path(SPEC).is_file(), "set BROILER_ECMA262_SPEC to the pinned edition's spec.html")
    def test_every_clause_id_is_one_the_pinned_text_has(self) -> None:
        self.assertEqual([], generator.check_clauses(generator.build(), Path(SPEC)))

    @unittest.skipUnless(TEST262 and Path(TEST262).is_dir(), "set BROILER_TEST262 to a test262 checkout at ccaac100")
    def test_every_test262_path_is_one_the_pinned_checkout_has(self) -> None:
        self.assertEqual([], generator.check_tests(generator.build(), Path(TEST262)))


@NEEDS_REFERENCE
class ReferenceTests(unittest.TestCase):
    def test_every_vector_agrees_with_the_reference_where_no_disagreement_is_named(self) -> None:
        self.assertEqual(0, generator.main(["--check-node", str(REFERENCE_NODE)]))

    def test_the_named_disagreements_are_the_reference_s(self) -> None:
        problems, found = generator.check_node(str(REFERENCE_NODE), generator.identifiers(generator.build()))
        self.assertEqual([], problems)
        self.assertEqual(generator.NODE_DISAGREES, found)


class ReferenceVersionTests(unittest.TestCase):
    def test_a_node_that_is_not_the_reference_is_refused(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            fake = Path(scratch) / "node"
            fake.write_text("#!/bin/sh\necho v0.0.0\n", encoding="utf-8")
            fake.chmod(0o755)
            problems, found = generator.check_node(str(fake), generator.identifiers(generator.build()))
            self.assertEqual({}, found)
            self.assertEqual(1, len(problems))
            self.assertIn("not the reference v26.12.0", problems[0])


class DivergenceFileTests(unittest.TestCase):
    def test_a_malformed_or_unknown_classification_is_refused(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            path = Path(scratch) / "d.txt"
            for text in ("reason a b\n", "reason a: x\nreason a: y\n", "reason a: x\nbroiler-xx a syntax/x/y/0001\n",
                         "reason a: x\nbroiler-js b syntax/x/y/0001\n",
                         "reason a: x\nbroiler-js a syntax/x/y/0001\nbroiler-js a syntax/x/y/0001\n"):
                path.write_text(text, encoding="utf-8")
                with self.assertRaises(generator.GeneratorError, msg=text):
                    generator.read_divergences(path)
            path.write_text("# c\nreason a: row T18: x\nbroiler-js a syntax/x/y/0001\n", encoding="utf-8")
            self.assertEqual({"syntax/x/y/0001": {"broiler-js": "row T18: x"}}, generator.read_divergences(path))
            self.assertEqual({}, generator.read_divergences(Path(scratch) / "absent.txt"))

    def test_a_classification_of_an_id_the_area_does_not_have_is_refused(self) -> None:
        vectors = generator.identifiers(generator.build())
        with tempfile.TemporaryDirectory() as scratch:
            with self.assertRaises(generator.GeneratorError):
                generator.write_files(vectors, Path(scratch), {"syntax/lexical/none/0001": {"broiler-js": "x"}})

    def test_a_classified_normative_vector_becomes_divergence_and_an_impl_defined_one_stays(self) -> None:
        normative = generator.Case("scripts", "g", "ParseScript", "1", "1", generator.V(1), (), (), ("sec-scripts",),
                                   (), "accepted, x")
        record = generator.render_record("syntax/scripts/g/0001", normative, {"broiler-js": "row T18: x"})
        self.assertIn("class: divergence\nprov:", record.replace("divergence.broiler-js: row T18: x\n", ""))
        self.assertIn("divergence.broiler-js: row T18: x\n", record)
        both = {"broiler-js": generator.V(1), "broiler-vm": generator.V(1)}
        stated = generator.Case("scripts", "g", "ParseScript", "1", "1", generator.V(1), (), (), ("sec-scripts",), (),
                                "accepted, x", "impl-defined", dict(both))
        record = generator.render_record("syntax/scripts/g/0001", stated, {"broiler-vm": "row R08: x"})
        self.assertIn("class: impl-defined\n", record)
        silent = generator.Case("scripts", "g", "ParseScript", "1", "1", generator.V(1), (), (), ("sec-scripts",), (),
                                "accepted, x", "impl-defined", {"broiler-js": generator.V(1)})
        with self.assertRaises(generator.GeneratorError):
            generator.render_record("syntax/scripts/g/0001", silent, {"broiler-vm": "row R08: x"})


class CommittedFileTests(unittest.TestCase):
    """The committed files' shape, read the schema's way."""

    @classmethod
    def setUpClass(cls) -> None:
        cls.vectors = committed()

    def test_the_generator_writes_the_revision_REVISIONS_md_ends_at(self) -> None:
        revisions = (CORPUS / "REVISIONS.md").read_text(encoding="utf-8")
        rows = [int(line.split("|")[1]) for line in revisions.splitlines()
                if line[:2] == "| " and line.split("|")[1].strip().isdigit()]
        self.assertEqual(list(range(1, len(rows) + 1)), rows)
        self.assertEqual(generator.CORPUS_REVISION, rows[-1])
        self.assertTrue(all(v["since"] == str(generator.SINCE) for v in self.vectors.values()))

    def test_one_file_per_class_of_row_and_ids_by_file(self) -> None:
        self.assertEqual(sorted(generator.FILES), sorted(FILES))
        self.assertEqual(sorted(FILES), sorted(p.stem for p in COMMITTED.glob("*.vectors")))
        for vector_id in self.vectors:
            self.assertRegex(vector_id, r"^syntax/[a-z-]+/[a-z0-9-]+/\d{4}$")
            self.assertIn(vector_id.split("/")[1], FILES)

    def test_every_vector_is_a_parse_of_its_in_and_its_script_carries_it(self) -> None:
        for vector_id, vector in self.vectors.items():
            self.assertEqual("syntax", vector["area"], vector_id)
            self.assertIn(vector["op"], ("ParseScript", "PerformEval"), vector_id)
            self.assertTrue(all(0x20 <= ord(c) < 0x7F for c in vector["script"]), vector_id)
            source = unquote(vector["in"])
            if vector["op"] == "ParseScript":
                self.assertEqual(source, vector["script"], vector_id)
            else:
                # A direct eval of the source, from the top level of a Script: the source as a string literal, after
                # at most a directive that makes the caller strict.
                self.assertRegex(vector["script"], r'^("use strict"; )?eval\(".*"\)$', vector_id)
                self.assertTrue(vector["script"].endswith("eval(" + generator.js(source) + ")"), vector_id)
            self.assertTrue(vector["out"] == generator.SE or vector["out"].startswith("value: "), vector_id)

    def test_no_op_of_the_area_is_another_area_s(self) -> None:
        # A library-level runner answers a vector by its op and inputs alone, so the area's two ops must be its own.
        others = set()
        for path in CORPUS.rglob("*.vectors"):
            if path.parent.name not in ("syntax", "controls"):
                others.update(record["op"] for record in read_records(path))
        self.assertEqual(set(), {"ParseScript", "PerformEval"} & others)

    def test_no_script_is_vectored_twice(self) -> None:
        scripts = Counter(v["script"] for v in self.vectors.values())
        self.assertEqual([], [s for s, n in scripts.items() if n > 1])

    def test_every_expected_value_cites_the_pinned_edition_or_a_test262_test(self) -> None:
        pin = (CORPUS / "sources" / "ecma262.pin").read_text(encoding="utf-8")
        revision = re.search(r"^revision ([0-9a-f]{40})$", pin, re.M).group(1)
        for vector_id, vector in self.vectors.items():
            entries = vector["prov"].split(" ")
            self.assertTrue(all(e.startswith(("spec:es2026@", "test262:test/")) for e in entries), vector_id)
            for entry in entries:
                if entry.startswith("spec:"):
                    self.assertTrue(revision.startswith(entry.split("@", 1)[1].split("#")[0]), vector_id)
            if not any(e.startswith("spec:") for e in entries):
                # Only the explicit-resource-management rules, which the pinned edition lacks, cite test262 alone.
                self.assertIn("ECMA-262 main at 4feb0272", vector["note"], vector_id)

    def test_every_class_is_one_the_schema_defines_with_its_fields(self) -> None:
        for vector_id, vector in self.vectors.items():
            self.assertIn(vector["class"], ("normative", "impl-defined", "divergence"), vector_id)
            engines = {k.split(".", 1)[1] for k in vector if k.startswith("per-engine.")}
            diverging = {k.split(".", 1)[1] for k in vector if k.startswith("divergence.")}
            if vector["class"] == "impl-defined":
                self.assertEqual({"broiler-js", "broiler-vm"}, engines, vector_id)
                self.assertTrue(diverging <= engines, vector_id)
            else:
                self.assertEqual(set(), engines, vector_id)
            if vector["class"] == "divergence":
                self.assertNotEqual(set(), diverging, vector_id)

    def test_every_impl_defined_vector_is_an_annex_b_outcome_and_cites_both_engines_records(self) -> None:
        for vector_id, vector in self.vectors.items():
            if vector["class"] != "impl-defined":
                continue
            self.assertNotEqual(generator.SE, vector["out"], vector_id)
            self.assertIn("JSD-0083, decision D17", vector["note"], vector_id)
            self.assertIn("docs/roadmap/SharedLibraries.md", vector["note"], vector_id)

    def test_every_divergence_names_a_correspondence_row_the_note_names(self) -> None:
        # Revision 30: each reason names the row of bundle sjl-13-001's correspondence whose answer the engine gives,
        # which is a row the vector's note names (syntax-divergences.txt's header).
        classified = 0
        for vector_id, vector in self.vectors.items():
            for key, text in vector.items():
                if not key.startswith("divergence."):
                    continue
                classified += 1
                row = re.match(r"^row ([A-Z]\d+[a-z]?|[A-Z]\d+-\d+): ", text)
                self.assertIsNotNone(row, (vector_id, text))
                named = re.match(r"^(?:catalogue rows? [^;:]*; )?correspondence rows? ([^:]*):", vector["note"])
                self.assertIsNotNone(named, vector_id)
                self.assertIn(row.group(1), re.split(r", | and ", named.group(1)), vector_id)
        self.assertGreater(classified, 0)

    def test_every_note_names_its_verdict(self) -> None:
        for vector_id, vector in self.vectors.items():
            verdict = "refused" if vector["out"] == generator.SE else "accepted"
            self.assertRegex(vector["note"], rf"(^|: ){verdict}, ", vector_id)


if __name__ == "__main__":
    unittest.main()
