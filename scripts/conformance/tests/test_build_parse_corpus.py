"""Tests of the parse corpus builder (milestone SJL-12.4, conformance/parse/).

They hold the builder to the manifest format conformance/parse/README.md defines: the variants test262's
INTERPRETING.md gives, the eval-family markers of Broiler.VM's rule (part B, B3), the fixed header, the column
line, the identifiers and the row order, LF normalisation of stored inputs, and that two builds are byte-identical.
The front-matter tests need PyYAML, which the builder needs for test262; they are skipped where it is absent.
"""

from __future__ import annotations

import contextlib
import importlib.util
import io
from pathlib import Path
import sys
import tempfile
import unittest

CONFORMANCE_DIR = Path(__file__).resolve().parents[1]
if str(CONFORMANCE_DIR) not in sys.path:
    sys.path.insert(0, str(CONFORMANCE_DIR))

import build_parse_corpus as B  # noqa: E402

HAVE_YAML = importlib.util.find_spec("yaml") is not None


def suite_of(files: dict[str, bytes]) -> B.Suite:
    suite = object.__new__(B.Suite)
    suite.files = files
    return suite


class VariantTests(unittest.TestCase):
    def test_interpreting_and_precedence(self) -> None:
        self.assertEqual(B.variants_of(set()), ("script", "strict,sloppy"))
        self.assertEqual(B.variants_of({"onlyStrict"}), ("script", "strict"))
        self.assertEqual(B.variants_of({"noStrict"}), ("script", "sloppy"))
        self.assertEqual(B.variants_of({"raw"}), ("script", "raw"))
        self.assertEqual(B.variants_of({"module"}), ("module", "module"))
        # Broiler.VM's Test262Run reads module first, then raw, onlyStrict and noStrict.
        self.assertEqual(B.variants_of({"module", "raw"}), ("module", "module"))
        self.assertEqual(B.variants_of({"raw", "onlyStrict"}), ("script", "raw"))
        self.assertEqual(B.variants_of({"async", "CanBlockIsFalse"}), ("script", "strict,sloppy"))

    def test_kinds(self) -> None:
        self.assertEqual(B.test262_kind("test/a/b.js"), "test")
        self.assertEqual(B.test262_kind("test/a/b_FIXTURE.js"), "fixture")
        self.assertEqual(B.test262_kind("test/a/b_FIXTURE.json"), "json-fixture")
        self.assertIsNone(B.test262_kind("test/a/text_FIXTURE"))
        self.assertIsNone(B.test262_kind("test/a/bytes_FIXTURE.bin"))
        self.assertIsNone(B.test262_kind("test/a/missing-dotjs"))


class TextTests(unittest.TestCase):
    def test_eval_family_markers(self) -> None:
        for text in ("eval(1)", "arguments", "super.x", "new.target", "#x", "#!/usr/bin/env node"):
            self.assertEqual(B.eval_family("script", text), "yes", text)
        self.assertEqual(B.eval_family("script", "var evaluate"), "yes")
        self.assertEqual(B.eval_family("script", "new . target; 1 + 2"), "no")
        self.assertEqual(B.eval_family("module", "eval(1)"), "no")
        self.assertEqual(B.eval_family("json", '{"eval": 1}'), "no")

    def test_lf_normalisation(self) -> None:
        self.assertEqual(B.lf_normalise(b"a\r\nb\rc\nd"), b"a\nb\nc\nd")
        self.assertEqual(B.lf_normalise("x y".encode()), "x y".encode())

    def test_decode_refuses_a_bom_and_bad_utf8(self) -> None:
        with self.assertRaises(B.Refused):
            B.decode(b"\xef\xbb\xbfx", "bom")
        with self.assertRaises(B.Refused):
            B.decode(b"\xff", "bad")


class RenderTests(unittest.TestCase):
    ROWS = [
        B.Row("broiler-js", "inputs/broiler-js/b.js", "1" * 64, "script", "strict,sloppy", "-", "no", "site"),
        B.Row("test262", "test/b.js", "2" * 64, "script", "strict", "parse:SyntaxError", "yes", "test"),
        B.Row("broiler-vm", "inputs/broiler-vm/a.mjs", "3" * 64, "module", "module", "-", "no", "js-1:x"),
        B.Row("test262", "test/a.js", "4" * 64, "module", "module", "-", "no", "test"),
    ]

    def test_header_columns_identifiers_and_order(self) -> None:
        text = B.render(list(self.ROWS), "a" * 40, "b" * 40).decode("utf-8")
        lines = text.split("\n")
        self.assertEqual(lines[-1], "")
        fields = [line for line in lines if line.startswith("#%")]
        self.assertIn("#% format\tbroiler-parse-corpus\t1", fields)
        self.assertIn("#% rows\t4", fields)
        self.assertIn("#% virtual-root\t/test262/", fields)
        self.assertIn("#% stored-root\t/parse-1/", fields)
        self.assertIn('#% strict-prologue\t"\\"use strict\\";\\n"', fields)
        self.assertIn("#% eval-flags\tE5\t14\tInFunction,InMethod,InDerivedConstructor", fields)
        self.assertIn("#% eval-flags\tE7\t38\tInFunction,InMethod,InClassBody", fields)
        self.assertIn(f"#% pin-test262\t{B.TEST262_REVISION}", fields)
        self.assertEqual(len([field for field in fields if field.startswith("#% eval-flags\t")]), 8)
        body = [line for line in lines if line and not line.startswith("#")]
        self.assertEqual(body[0], "\t".join(B.COLUMNS))
        self.assertEqual(
            [row.split("\t")[:3] for row in body[1:]],
            [
                ["T000001", "test262", "test/a.js"],
                ["T000002", "test262", "test/b.js"],
                ["V000001", "broiler-vm", "inputs/broiler-vm/a.mjs"],
                ["J000001", "broiler-js", "inputs/broiler-js/b.js"],
            ],
        )
        self.assertTrue(all(len(row.split("\t")) == 9 for row in body))
        self.assertNotIn("\r", text)

    def test_deterministic(self) -> None:
        first = B.render(list(self.ROWS), "a" * 40, "b" * 40)
        second = B.render(list(reversed(self.ROWS)), "a" * 40, "b" * 40)
        self.assertEqual(first, second)

    def test_duplicate_rows_refused(self) -> None:
        with self.assertRaises(B.Refused):
            B.render([self.ROWS[0], self.ROWS[0]], "a" * 40, "b" * 40)


@unittest.skipUnless(HAVE_YAML, "PyYAML is not installed")
class FrontMatterTests(unittest.TestCase):
    def test_rows_from_front_matter(self) -> None:
        files = {
            "test/inline.js": b"/*---\nflags: [onlyStrict]\n---*/\neval('1');\n",
            "test/block.js": b"/*---\nflags:\n  - noStrict\nnegative:\n  phase: parse\n  type: SyntaxError\n---*/\n$DONOTEVALUATE();\n",
            "test/plain.js": b"/*---\ndescription: x\n---*/\r\nvar a;\r\n",
            "test/mod.js": b"/*---\nflags: [module, raw]\n---*/\nexport {};\n",
            "test/x_FIXTURE.js": b"export default 1;\n",
            "test/x_FIXTURE.json": b"{}\n",
            "test/text_FIXTURE": b"text",
        }
        rows, excluded, notes = B.test262_rows(suite_of(files))
        by_path = {row.path: row for row in rows}
        self.assertEqual(excluded, ["test/text_FIXTURE"])
        self.assertEqual(notes, ["test/mod.js\tmodule,raw"])
        self.assertEqual((by_path["test/inline.js"].variants, by_path["test/inline.js"].eval_family), ("strict", "yes"))
        self.assertEqual((by_path["test/block.js"].variants, by_path["test/block.js"].negative), ("sloppy", "parse:SyntaxError"))
        self.assertEqual(by_path["test/plain.js"].variants, "strict,sloppy")
        self.assertEqual(by_path["test/plain.js"].sha256, B.sha256(files["test/plain.js"]))
        self.assertEqual((by_path["test/mod.js"].goal, by_path["test/mod.js"].variants), ("module", "module"))
        self.assertEqual((by_path["test/x_FIXTURE.js"].goal, by_path["test/x_FIXTURE.js"].provenance), ("module", "fixture"))
        self.assertEqual((by_path["test/x_FIXTURE.json"].goal, by_path["test/x_FIXTURE.json"].variants), ("json", "json"))

    def test_missing_front_matter_refused(self) -> None:
        with self.assertRaises(B.Refused):
            B.test262_rows(suite_of({"test/none.js": b"var a;\n"}))


class StoredInputTests(unittest.TestCase):
    def test_capture_import_normalises_deduplicates_and_reads_back(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            root = Path(scratch)
            capture = root / "capture"
            capture.mkdir()
            records = []
            for text, goal, site in (
                (b"a\r\nb", "script", "P.Tests!T.One"),
                (b"a\nb", "script", "P.Tests!T.Two"),
                (b"a\nb", "module", "P.Tests!T.Three"),
            ):
                digest = B.sha256(text)
                (capture / f"{digest}.js").write_bytes(text)
                records.append(f"{digest}\t{len(text)}\t{goal}\t{site}")
            (capture / "index.tsv").write_text("\n".join(records) + "\n", encoding="utf-8")
            corpus = root / "corpus"
            arguments = B.argparse.Namespace(capture=[str(capture)], commit="c" * 40, corpus=str(corpus))
            with contextlib.redirect_stdout(io.StringIO()):
                self.assertEqual(B.command_import_js(arguments), 0)
            digest = B.sha256(b"a\nb")
            self.assertEqual((corpus / "inputs" / "broiler-js" / f"{digest}.js").read_bytes(), b"a\nb")
            self.assertEqual((corpus / "inputs" / "broiler-js" / f"{digest}.mjs").read_bytes(), b"a\nb")
            rows, commit = B.stored_rows(corpus, "broiler-js")
            self.assertEqual(commit, "c" * 40)
            self.assertEqual(
                [(row.path, row.goal, row.variants, row.provenance) for row in rows],
                [
                    (f"inputs/broiler-js/{digest}.js", "script", "strict,sloppy", "P.Tests!T.One (+1)"),
                    (f"inputs/broiler-js/{digest}.mjs", "module", "module", "P.Tests!T.Three"),
                ],
            )
            # A stored file changed after the import no longer matches its provenance.
            (corpus / "inputs" / "broiler-js" / f"{digest}.js").write_bytes(b"a\nc")
            with self.assertRaises(B.Refused):
                B.stored_rows(corpus, "broiler-js")


if __name__ == "__main__":
    unittest.main()
