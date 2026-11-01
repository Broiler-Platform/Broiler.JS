"""Tests of the collation vectors' generator (milestone SJL-17, work package SJL-17.2).

The committed files' shape, and the generator's pins and literals, run everywhere: the archived CollationTest files
and root.xml they read are the data home's, in this repository. Regenerating the area needs the reference Node.js
v26.12.0 (BROILER_INTL_NODE), and is skipped where it is absent. The check of the clause ids against the pinned
ECMA-402 text reads BROILER_ECMA402_SPEC (a directory holding its spec/*.html at the pinned revision) and is skipped
where it is not given. Run with `python3 -I`, as any reading of downloaded data.
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

import generate_collation_vectors as generator  # noqa: E402

CORPUS = REPOSITORY / "conformance" / "vectors"
COMMITTED = CORPUS / "collation"
FILES = ("options", "root", "surrogates", "tailorings")
REFERENCE_NODE = os.environ.get("BROILER_INTL_NODE")
ROW = r"O\d+[a-z]?"


def version(node: str | None) -> str | None:
    if not node:
        return None
    try:
        return subprocess.run([node, "--version"], capture_output=True, text=True, check=False).stdout.strip()
    except OSError:
        return None


NEEDS_REFERENCE = unittest.skipUnless(version(REFERENCE_NODE) == "v26.12.0",
                                      "no reference here; set BROILER_INTL_NODE (Node.js v26.12.0)")


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
    return set(re.findall(rf"\b{ROW}\b", note))


# The schema's typed values and outcomes (SCHEMA.md sections 3 and 4), as far as this area writes them.
STRING = r'str:"(?:[ !#-\[\]-~]|\\"|\\\\|\\u[0-9A-F]{4})*"'
VALUE = rf"(?:{STRING}|bool:true|bool:false)"
OUTCOME = re.compile(rf"^value: {VALUE}$")
INPUTS = re.compile(rf"^{STRING}(?: {STRING})*$")


class CommittedFileTests(unittest.TestCase):
    """The committed files' shape, read the schema's way; no reference needed. The shared Conformance library's
    validator and its negative controls are the schema's own check; these hold what this area adds."""

    @classmethod
    def setUpClass(cls) -> None:
        cls.vectors = committed()

    def test_the_generator_writes_the_revision_REVISIONS_md_ends_at(self) -> None:
        revisions = (CORPUS / "REVISIONS.md").read_text(encoding="utf-8")
        rows = [int(line.split("|")[1]) for line in revisions.splitlines()
                if line[:2] == "| " and line.split("|")[1].strip().isdigit()]
        self.assertEqual(list(range(1, len(rows) + 1)), rows)
        self.assertEqual(generator.CORPUS_REVISION, rows[-1])
        self.assertIn(generator.CORPUS_REVISION, (generator.SINCE, generator.SINCE + 1))
        self.assertTrue(all(v["since"] == str(generator.SINCE) for v in self.vectors.values()))

    def test_four_files_and_ids_by_file(self) -> None:
        self.assertEqual(sorted(generator.FILES), sorted(FILES))
        self.assertEqual(sorted(FILES), sorted(p.stem for p in COMMITTED.glob("*.vectors")))
        self.assertEqual([], [p.name for p in COMMITTED.iterdir() if p.suffix != ".vectors"])
        for vector_id in self.vectors:
            self.assertRegex(vector_id, r"^intl/collation/(options|root|surrogates|tailorings)/[a-z0-9-]+/\d{4}$")
        for name in FILES:
            for record in read_records(COMMITTED / f"{name}.vectors"):
                self.assertTrue(record["id"].startswith(f"intl/collation/{name}/"), record["id"])

    def test_every_record_has_the_schema_s_fields_and_values(self) -> None:
        known = {"id", "area", "op", "in", "out", "class", "prov", "script", "note", "since"}
        for vector_id, vector in self.vectors.items():
            extra = {k for k in vector if k not in known and not k.startswith(("per-engine.", "divergence."))}
            self.assertEqual(set(), extra, vector_id)
            self.assertLessEqual(known - {"note"}, set(vector), vector_id)
            self.assertEqual("intl", vector["area"], vector_id)
            self.assertRegex(vector["in"], INPUTS, vector_id)
            self.assertRegex(vector["out"], OUTCOME, vector_id)
            for key, value in vector.items():
                if key.startswith("per-engine."):
                    self.assertIn(key[len("per-engine."):], ("broiler-js", "broiler-vm"), vector_id)
                    self.assertRegex(value, OUTCOME, vector_id)

    def test_every_script_is_one_line_of_ascii_that_declares_no_global_and_is_vectored_once(self) -> None:
        scripts = Counter(v["script"] for v in self.vectors.values())
        self.assertEqual([], [s for s, n in scripts.items() if n > 1])
        for vector_id, vector in self.vectors.items():
            self.assertTrue(all(0x20 <= ord(c) < 0x7F for c in vector["script"]), vector_id)
            self.assertNotRegex(vector["script"], r"^(var|let|const|function|class) ", vector_id)

    def test_no_op_of_the_area_is_another_directory_s(self) -> None:
        # A library-level runner answers a vector by its op and inputs alone (IVectorLibrary.Call is given no area),
        # so the area's ops must be its own: the Intl area's Collator ops are written otherwise
        # ("Intl.Collator, resolvedOptions()"), and the Unicode area's localeCompare op names the type of its result.
        others = set()
        for path in CORPUS.rglob("*.vectors"):
            if path.parent.name not in ("collation", "controls"):
                others.update(record["op"] for record in read_records(path))
        self.assertEqual(set(), {v["op"] for v in self.vectors.values()} & others)

    def test_every_expected_value_has_a_source_independent_of_both_engines(self) -> None:
        pins = {}
        for name, path in (("ecma402", "ecma402.pin"), ("es2026", "ecma262.pin")):
            text = (CORPUS / "sources" / path).read_text(encoding="utf-8")
            pins[name] = re.search(r"^revision ([0-9a-f]{40})$", text, re.M).group(1)
        for vector_id, vector in self.vectors.items():
            entries = vector["prov"].split(" ")
            self.assertTrue(all(e.startswith((generator.ECMA402, generator.ES2026, generator.REF, generator.REF_TEST))
                                for e in entries), vector_id)
            self.assertIn(generator.REF, entries, vector_id)
            if "resolvedOptions" not in vector["op"]:
                self.assertIn(generator.COMPARE, entries, vector_id)
            for entry in entries:
                if entry.startswith("spec:"):
                    name, _, rest = entry[len("spec:"):].partition("@")
                    self.assertTrue(pins[name].startswith(rest.split("#")[0]), vector_id)

    def test_classes_and_per_engine_fields(self) -> None:
        for vector_id, vector in self.vectors.items():
            per_engine = {k for k in vector if k.startswith("per-engine.")}
            divergence = {k for k in vector if k.startswith("divergence.")}
            self.assertIn(vector["class"], ("normative", "impl-defined", "divergence"), vector_id)
            self.assertEqual(vector["class"] == "divergence", bool(divergence) and not per_engine, vector_id)
            # impl-defined only where the locale moves under Broiler.VM's list (decision E3), and then with both
            # engines' expectations: Broiler.JS's is out.
            moves = bool(re.search(r'(Collator\(|localeCompare\("(?:[^"\\]|\\.)*",)"(es|sv|th)(-[A-Za-z0-9-]*)?"',
                                   vector["script"]))
            self.assertEqual(moves, vector["class"] == "impl-defined", vector_id)
            if vector["class"] == "impl-defined":
                self.assertEqual({"per-engine.broiler-js", "per-engine.broiler-vm"}, per_engine, vector_id)
                self.assertEqual(vector["out"], vector["per-engine.broiler-js"], vector_id)
                self.assertIn("rule N28", vector["note"], vector_id)

    def test_every_divergence_field_is_one_collation_divergences_txt_lists(self) -> None:
        listed = generator.read_divergences(generator.DIVERGENCES)
        fields = {}
        for vector_id, vector in self.vectors.items():
            for key, value in vector.items():
                if key.startswith("divergence."):
                    fields.setdefault(vector_id, {})[key[len("divergence."):]] = value
        self.assertEqual(listed, fields)

    def test_every_row_with_a_behavioural_difference_has_vectors(self) -> None:
        named = set()
        for vector in self.vectors.values():
            named.update(rows_named(vector["note"]))
        self.assertEqual([], [row for row in generator.BEHAVIOURAL if row not in named])
        self.assertEqual([], sorted(named - set(generator.ROWS)))

    def test_the_canonically_equivalent_pairs_expect_equality(self) -> None:
        equivalence = [v for k, v in self.vectors.items() if "/equivalence/" in k]
        self.assertGreater(len(equivalence), 30)
        self.assertTrue(all(v["out"] == 'value: str:"="' for v in equivalence))

    def test_each_collation_test_vector_holds_two_consecutive_lines_of_its_file(self) -> None:
        cldr = generator.DEFAULT_CLDR
        seen = 0
        for name, (filename, _) in generator.COLLATION_TEST_FILES.items():
            lines = dict(generator.collation_test_lines(generator.pinned_bytes(cldr, f"{generator.UCA_DIRECTORY}/{filename}")))
            for vector_id, vector in self.vectors.items():
                if f"/collationtest-{name}/" not in vector_id:
                    continue
                seen += 1
                first, second = (int(n) for n in re.search(r"lines (\d+) and (\d+) of", vector["note"]).groups())
                self.assertIn(filename, vector["note"], vector_id)
                values = vector["in"].split(" ")
                self.assertEqual(['str:"en"', generator.typed(lines[first]), generator.typed(lines[second])], values,
                                 vector_id)
                self.assertNotEqual('value: str:">"', vector["out"], vector_id)
                self.assertIn(generator.REF_TEST, vector["prov"].split(" "), vector_id)
        self.assertGreater(seen, 1000)

    def test_han_across_blocks_is_vectored(self) -> None:
        han = [v for k, v in self.vectors.items() if "/root/han/" in k]
        blocks = set()
        for vector in han:
            for value in vector["in"].split(" ")[1:]:
                text = generator_string(value)
                code = ord(text[0])
                blocks.add("ext-a" if 0x3400 <= code <= 0x4DBF else "core" if 0x4E00 <= code <= 0x9FFF else
                           "ext" if code >= 0x20000 else "compat")
        self.assertEqual({"core", "ext-a", "ext", "compat"}, blocks)
        self.assertTrue(all("row O3" in v["note"] and "finding F1" in v["note"] for v in han))


def generator_string(typed: str) -> str:
    body = typed[len('str:"'):-1]
    units, i = [], 0
    while i < len(body):
        if body[i] == "\\" and body[i + 1] == "u":
            units.append(int(body[i + 2:i + 6], 16))
            i += 6
        elif body[i] == "\\":
            units.append(ord(body[i + 1]))
            i += 2
        else:
            units.append(ord(body[i]))
            i += 1
    return b"".join(u.to_bytes(2, "little") for u in units).decode("utf-16-le", "surrogatepass")


class LiteralTests(unittest.TestCase):
    def test_typed_values_and_literals_write_each_code_unit_one_way(self) -> None:
        self.assertEqual('str:"a\\"b\\\\c"', generator.typed('a"b\\c'))
        self.assertEqual('str:"\\u00E5\\uD840\\uDC00\\uD800"', generator.typed("å\U00020000\ud800"))
        self.assertEqual('"\\u00E5"', generator.js("å"))
        self.assertEqual("U+0020a(empty)", generator.show(" a") + generator.show(""))
        self.assertEqual("\U00020000\ud800", generator_string(generator.typed("\U00020000\ud800")))

    def test_broiler_vm_s_script_resolves_only_the_locales_its_list_lacks(self) -> None:
        script = generator.compare_script("sv", None, "z", "å")
        moved, moves = generator.vm_script(script)
        self.assertEqual(['sv to en-US'], moves)
        self.assertIn('new Intl.Collator("en-US")', moved)
        for locale in ("de", "de-u-co-phonebk", "en", "und"):
            self.assertEqual([], generator.vm_script(generator.compare_script(locale, None, "a", "b"))[1])
        moved, moves = generator.vm_script(generator.locale_compare_script("th-TH", None, "a", "b"))
        self.assertEqual(['th-TH to en-US'], moves)
        self.assertIn('.localeCompare("b","en-US")', moved)


class PinTests(unittest.TestCase):
    """The archived files are held to the data home's cldr.pin before a byte of them is used."""

    def test_the_archived_files_are_the_pinned_ones(self) -> None:
        for filename, _ in generator.COLLATION_TEST_FILES.values():
            self.assertTrue(generator.pinned_bytes(generator.DEFAULT_CLDR, f"{generator.UCA_DIRECTORY}/{filename}"))
        self.assertIn(generator.RADICAL_STROKE,
                      " ".join(generator.pinned_bytes(generator.DEFAULT_CLDR, generator.ROOT_XML).decode().split()))

    def test_a_changed_file_is_refused(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            root = Path(scratch)
            shutil.copy(generator.DEFAULT_CLDR / "cldr.pin", root / "cldr.pin")
            target = root / generator.ROOT_XML
            target.parent.mkdir(parents=True)
            data = bytearray((generator.DEFAULT_CLDR / generator.ROOT_XML).read_bytes())
            data[100] ^= 1
            target.write_bytes(bytes(data))
            with self.assertRaises(generator.GeneratorError):
                generator.pinned_bytes(root, generator.ROOT_XML)
            with self.assertRaises(generator.GeneratorError):
                generator.pinned_bytes(root, "cldr-48.2.0/common/collation/xx.xml")


@NEEDS_REFERENCE
class GeneratorTests(unittest.TestCase):
    def test_the_committed_files_are_what_the_generator_writes(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            self.assertEqual(0, generator.main(["--out", scratch, "--node", str(REFERENCE_NODE)]))
            produced = sorted(p.name for p in Path(scratch).iterdir())
            self.assertEqual(sorted(p.name for p in COMMITTED.glob("*.vectors")), produced)
            for name in produced:
                self.assertEqual((COMMITTED / name).read_bytes(), (Path(scratch) / name).read_bytes(), name)

    def test_a_node_that_is_not_the_reference_is_refused_and_nothing_is_written(self) -> None:
        other = shutil.which("node")
        if not other or version(other) == "v26.12.0":
            self.skipTest("no other Node.js on PATH")
        with tempfile.TemporaryDirectory() as scratch:
            out = Path(scratch) / "out"
            self.assertEqual(2, generator.main(["--out", str(out), "--node", other]))
            self.assertFalse(out.exists())


class ClauseTests(unittest.TestCase):
    def test_every_clause_cited_is_in_the_pinned_text(self) -> None:
        spec = os.environ.get("BROILER_ECMA402_SPEC")
        if not spec:
            self.skipTest("set BROILER_ECMA402_SPEC to a directory holding the pinned ECMA-402's spec/*.html")
        ids = set()
        for path in (Path(spec) / "spec").glob("*.html"):
            ids.update(re.findall(r'id="([^"]+)"', path.read_text(encoding="utf-8")))
        for vector_id, vector in committed().items():
            for entry in vector["prov"].split(" "):
                if entry.startswith(generator.ECMA402):
                    self.assertIn(entry[len(generator.ECMA402):], ids, vector_id)


if __name__ == "__main__":
    unittest.main()
