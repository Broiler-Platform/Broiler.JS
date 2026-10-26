"""Tests of the Intl area's generator (corpus revision 27, milestone SJL-9.2, work package SJL-9.2a).

The committed files' shape runs everywhere. Regenerating the area needs the references: the reference Node.js v26.12.0
(BROILER_INTL_NODE), the U+202F witness Node.js 22.22.0 (BROILER_NODE, or `node` on PATH) and the directory holding
the cldr-json 48.2.0 tarballs (BROILER_CLDR_TARBALLS); those tests are skipped where any is absent. The check of the
clause ids against the pinned ECMA-402 text reads BROILER_ECMA402_SPEC (a directory holding its spec/*.html at the
pinned revision) and is skipped where it is not given.
"""

from __future__ import annotations

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

import generate_intl_vectors as generator  # noqa: E402

CORPUS = REPOSITORY / "conformance" / "vectors"
COMMITTED = CORPUS / "intl"
FILES = ("date-times", "durations", "locales", "numbers-and-text", "values-and-plurals")
REFERENCE_NODE = os.environ.get("BROILER_INTL_NODE")
WITNESS_NODE = os.environ.get("BROILER_NODE") or shutil.which("node")
CLDR = os.environ.get("BROILER_CLDR_TARBALLS")
ROW = r"[LMPNSTUZ]\d+[a-z]?"


def version(node: str | None) -> str | None:
    if not node:
        return None
    try:
        return subprocess.run([node, "--version"], capture_output=True, text=True, check=False).stdout.strip()
    except OSError:
        return None


NEEDS_REFERENCE = unittest.skipUnless(
    version(REFERENCE_NODE) == "v26.12.0" and version(WITNESS_NODE) == "v22.22.0" and CLDR and Path(CLDR).is_dir(),
    "no reference here; set BROILER_INTL_NODE (v26.12.0), BROILER_NODE (v22.22.0) and BROILER_CLDR_TARBALLS")


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


def rows_named(note: str) -> set[str]:
    """The correspondence rows a note names: "row P2", "rows U13 and U14", "rows T30 (...) and T31 (...)", up to the
    next colon or semicolon outside parentheses."""
    out = set()
    note = re.sub(r"\([^()]*\)", "", note)
    for match in re.finditer(r"\brows? ([^:;]*)", note):
        out.update(re.findall(rf"\b{ROW}\b", match.group(1)))
    return out


def arguments(out: Path) -> list[str]:
    return ["--out", str(out), "--node", str(REFERENCE_NODE), "--witness-node", str(WITNESS_NODE), "--cldr", str(CLDR)]


@NEEDS_REFERENCE
class GeneratorTests(unittest.TestCase):
    def test_the_committed_corpus_is_what_the_generator_writes(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            self.assertEqual(0, generator.main(arguments(Path(scratch))))
            produced = sorted(p.name for p in Path(scratch).iterdir())
            self.assertEqual(sorted(p.name for p in COMMITTED.glob("*.vectors")), produced)
            for name in produced:
                self.assertEqual((COMMITTED / name).read_bytes(), (Path(scratch) / name).read_bytes(), name)

    def test_a_node_that_is_not_the_reference_is_refused_and_nothing_is_written(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            out = Path(scratch) / "out"
            refused = ["--out", str(out), "--node", str(WITNESS_NODE), "--witness-node", str(WITNESS_NODE),
                       "--cldr", str(CLDR)]
            self.assertEqual(2, generator.main(refused))
            self.assertFalse(out.exists())


class DivergenceFileTests(unittest.TestCase):
    def test_a_malformed_or_unknown_classification_is_refused(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            path = Path(scratch) / "d.txt"
            for text in ("reason a b\n", "reason a: x\nreason a: y\n", "reason a: x\nbroiler-xx a intl/x/y/0001\n",
                         "reason a: x\nbroiler-js b intl/x/y/0001\n",
                         "reason a: x\nbroiler-js a intl/x/y/0001\nbroiler-js a intl/x/y/0001\n"):
                path.write_text(text, encoding="utf-8")
                with self.assertRaises(generator.GeneratorError, msg=text):
                    generator.read_divergences(path)
            path.write_text("# c\nreason a: row L2: x\nbroiler-js a intl/x/y/0001\n", encoding="utf-8")
            self.assertEqual({"intl/x/y/0001": {"broiler-js": "row L2: x"}}, generator.read_divergences(path))


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
        self.assertTrue(all(v["since"] == str(generator.SINCE) for v in self.vectors.values()))

    def test_one_file_per_sub_area_and_ids_by_file(self) -> None:
        self.assertEqual(sorted(stem for stem, _ in generator.FILES.values()), sorted(FILES))
        self.assertEqual(sorted(FILES), sorted(p.stem for p in COMMITTED.glob("*.vectors")))
        for vector_id in self.vectors:
            self.assertRegex(vector_id, r"^intl/[a-z-]+/[a-z0-9-]+/\d{4}$")
            self.assertIn(vector_id.split("/")[1], FILES)

    def test_every_script_is_one_line_of_ascii_that_declares_no_global_and_is_vectored_once(self) -> None:
        scripts = Counter(v["script"] for v in self.vectors.values())
        self.assertEqual([], [s for s, n in scripts.items() if n > 1])
        for vector_id, vector in self.vectors.items():
            self.assertEqual("intl", vector["area"], vector_id)
            self.assertTrue(all(0x20 <= ord(c) < 0x7F for c in vector["script"]), vector_id)
            self.assertNotRegex(vector["script"], r"^(var|let|const|function|class) ", vector_id)

    def test_every_expected_value_has_a_source_independent_of_both_engines(self) -> None:
        for vector_id, vector in self.vectors.items():
            entries = vector["prov"].split(" ")
            self.assertFalse(any(e.startswith("observed:") for e in entries), vector_id)
            self.assertTrue(all(e.startswith((generator.ECMA402, generator.TEMPORAL, generator.ERA, generator.REF,
                                              generator.REF_CLDR, generator.OVERRULED, generator.OVERRULED_V8))
                                for e in entries), vector_id)
            self.assertTrue(any(e.startswith(("spec:", "proposal:", "ref:")) for e in entries), vector_id)
            # The reference's answer is overruled only where the text (or CLDR, decision D34) gives another one,
            # and the note says why.
            if any(e.startswith(generator.OVERRULED) for e in entries):
                overruled = next(e for e in entries if e.startswith(generator.OVERRULED))[len(generator.OVERRULED):]
                self.assertNotEqual(overruled.replace(":", ": ", 1), vector["out"], vector_id)
                self.assertIn("the reference answered", vector["note"], vector_id)

    def test_every_ecma402_and_proposal_entry_names_its_pinned_revision(self) -> None:
        pins = {}
        for name in ("ecma402", "temporal", "intl-era-monthcode"):
            text = (CORPUS / "sources" / f"{name}.pin").read_text(encoding="utf-8")
            pins[name] = re.search(r"^revision ([0-9a-f]{40})$", text, re.M).group(1)
        for vector_id, vector in self.vectors.items():
            for entry in vector["prov"].split(" "):
                if entry.startswith(("spec:", "proposal:")):
                    name, _, rest = entry.split(":", 1)[1].partition("@")
                    self.assertTrue(pins[name].startswith(rest.split("#")[0]), vector_id)

    def test_date_time_strings_carry_no_character_beyond_the_witness_s_U_202F(self) -> None:
        # Decision D17 (b): a vector takes U+202F from the witness where ICU's pattern has it; the reference's raw
        # answer is recorded beside it, and the two differ in nothing else.
        for vector_id, vector in self.vectors.items():
            for entry in vector["prov"].split(" "):
                if entry.startswith(generator.OVERRULED_V8):
                    raw = entry[len(generator.OVERRULED_V8):].replace(":", ": ", 1)
                    self.assertIn("\\u202F", vector["out"], vector_id)
                    self.assertEqual(raw, vector["out"].replace("\\u202F", " "), vector_id)

    def test_every_class_is_one_the_schema_defines_with_its_fields(self) -> None:
        for vector_id, vector in self.vectors.items():
            per_engine = {k for k in vector if k.startswith("per-engine.")}
            divergence = {k for k in vector if k.startswith("divergence.")}
            self.assertIn(vector["class"], ("normative", "impl-defined", "divergence"), vector_id)
            self.assertEqual(vector["class"] == "impl-defined", bool(per_engine), vector_id)
            self.assertEqual(vector["class"] == "divergence", bool(divergence) and not per_engine, vector_id)
            if vector["class"] == "impl-defined":
                self.assertTrue({"per-engine." + k.split(".", 1)[1] for k in divergence} <= per_engine, vector_id)

    def test_every_divergence_field_is_one_intl_divergences_txt_lists(self) -> None:
        listed = generator.read_divergences(generator.DIVERGENCES)
        fields = {}
        for vector_id, vector in self.vectors.items():
            for key, value in vector.items():
                if key.startswith("divergence."):
                    fields.setdefault(vector_id, {})[key[len("divergence."):]] = value
        self.assertEqual(listed, fields)
        # Each reason names the correspondence row it confirms.
        for engines in listed.values():
            for text in engines.values():
                self.assertRegex(text, rf"^rows? {ROW}")

    def test_every_row_with_a_behavioural_difference_has_vectors(self) -> None:
        named = set()
        for vector in self.vectors.values():
            named.update(rows_named(vector["note"]))
        self.assertEqual([], [row for row in generator.BEHAVIOURAL if row not in named])
        self.assertEqual([], sorted(named - set(generator.ROWS)))

    def test_every_extra_locale_of_broiler_js_has_vectors(self) -> None:
        header = (COMMITTED / "locales.vectors").read_text(encoding="utf-8").split("\nschema: 1\n", 1)[0]
        listed = re.search(r"extra locales \(decision D5\): ([^.]+)\.", header.replace("\n# ", " ")).group(1).split(", ")
        self.assertGreater(len(listed), 50)
        inputs = Counter()
        for vector in self.vectors.values():
            for value in vector["in"].split(" "):
                if value.startswith('str:"'):
                    inputs[value[5:-1]] += 1
        self.assertEqual([], [tag for tag in listed if inputs[tag] < 5])

    def test_every_clause_cited_is_in_the_pinned_text(self) -> None:
        spec = os.environ.get("BROILER_ECMA402_SPEC")
        if not spec:
            self.skipTest("set BROILER_ECMA402_SPEC to a directory holding the pinned ECMA-402's spec/*.html")
        ids = set()
        for path in (Path(spec) / "spec").glob("*.html"):
            ids.update(re.findall(r'id="([^"]+)"', path.read_text(encoding="utf-8")))
        for vector_id, vector in self.vectors.items():
            for entry in vector["prov"].split(" "):
                if entry.startswith(generator.ECMA402):
                    self.assertIn(entry[len(generator.ECMA402):], ids, vector_id)


if __name__ == "__main__":
    unittest.main()
