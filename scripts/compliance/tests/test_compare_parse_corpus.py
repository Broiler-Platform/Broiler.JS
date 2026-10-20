from __future__ import annotations

from contextlib import redirect_stderr, redirect_stdout
import gzip
import hashlib
from io import StringIO
import json
from pathlib import Path
import sys
import tempfile
import unittest


COMPLIANCE_DIR = Path(__file__).resolve().parents[1]
if str(COMPLIANCE_DIR) not in sys.path:
    sys.path.insert(0, str(COMPLIANCE_DIR))

import compare_parse_corpus as comparer


MANIFEST_HEADER = [
    "#% format\tbroiler-parse-corpus\t1",
    "#% corpus\tparse-1",
    "#% rows\t3",
]
MANIFEST_ROWS = [
    "T000001\ttest262\ttest/a.js\t" + "0" * 64 + "\tscript\tstrict,sloppy\t-\tno\ttest",
    "T000002\ttest262\ttest/b.js\t" + "1" * 64 + "\tmodule\tmodule\t-\tno\ttest",
    "V000001\tbroiler-vm\tinputs/c.json\t" + "2" * 64 + "\tjson\tjson\t-\tno\tjs-1",
]
ACCEPTED = ("accepted", "-", "-")
NOT_APPLICABLE = ("not-applicable", "-", "the\\u0020goal")


def manifest_text() -> str:
    return "\n".join(MANIFEST_HEADER + [comparer.MANIFEST_COLUMNS] + MANIFEST_ROWS) + "\n"


def report_text(mode: str, columns: list[str], rows: list[tuple[str, ...]], digest: str, **header: str) -> str:
    fields = {
        "format": "broiler-js-parse-corpus-report\t1",
        "mode": mode,
        "manifest-sha256": digest,
        "selection": "all",
        "order": "manifest",
        "threads": "1",
    }
    fields.update(header)
    lines = [f"#% {key}\t{value}" for key, value in fields.items()]
    lines.append(f"#% rows\t{len(rows)}")
    lines.append("\t".join(columns))
    lines.extend("\t".join(row) for row in rows)
    lines.append(f"#% end\t{len(rows)}")
    return "\n".join(lines) + "\n"


def acceptance_rows(overrides: dict[tuple[str, str, str], tuple[str, str, str]] | None = None) -> list[tuple[str, ...]]:
    outcomes = {
        ("T000001", "strict", "P"): ACCEPTED,
        ("T000001", "strict", "K"): ("refused", "Broiler.JavaScript.Runtime.JSException:SyntaxError", "Unexpected token"),
        ("T000001", "sloppy", "P"): ACCEPTED,
        ("T000001", "sloppy", "K"): ACCEPTED,
        ("T000002", "module", "P"): ("refused", "Broiler.JavaScript.Ast.Misc.FastParseException", "Unknown token"),
        ("T000002", "module", "K"): ("crashed", "System.NotImplementedException", "The method or operation is not implemented."),
        ("V000001", "json", "P"): NOT_APPLICABLE,
        ("V000001", "json", "K"): NOT_APPLICABLE,
    }
    outcomes.update(overrides or {})
    return [key + value for key, value in outcomes.items()]


class CompareParseCorpusTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_directory = tempfile.TemporaryDirectory()
        self.root = Path(self.temp_directory.name)
        self.manifest = self.root / "corpus.manifest"
        self.manifest.write_text(manifest_text(), encoding="utf-8", newline="\n")
        self.digest = hashlib.sha256(self.manifest.read_bytes()).hexdigest()

    def tearDown(self) -> None:
        self.temp_directory.cleanup()

    def write(self, name: str, text: str, compress: bool = False) -> Path:
        path = self.root / name
        if compress:
            path.write_bytes(gzip.compress(text.encode("utf-8"), mtime=0))
        else:
            path.write_text(text, encoding="utf-8", newline="\n")
        return path

    def acceptance(self, name: str, overrides: dict | None = None, **header: str) -> Path:
        return self.write(name, report_text("acceptance", comparer.ACCEPTANCE_COLUMNS, acceptance_rows(overrides), self.digest, **header))

    def run_main(self, *argv: str) -> tuple[int, str, str]:
        out, err = StringIO(), StringIO()
        with redirect_stdout(out), redirect_stderr(err):
            code = comparer.main(list(argv))
        return code, out.getvalue(), err.getvalue()

    # ---- part C ----------------------------------------------------------------------------

    def test_identical_runs_have_no_difference(self) -> None:
        base = self.acceptance("base.tsv")
        after = self.write("after.tsv.gz", base.read_text(encoding="utf-8"), compress=True)
        code, out, _ = self.run_main("acceptance", "--base", str(base), "--after", str(after), "--manifest", str(self.manifest))
        self.assertEqual(code, 0)
        self.assertIn("differences 0", out)

    def test_a_changed_outcome_is_a_difference_and_unclassified(self) -> None:
        base = self.acceptance("base.tsv")
        after = self.acceptance("after.tsv", {("T000001", "sloppy", "K"): ("refused", "Broiler.JavaScript.Runtime.JSException:SyntaxError", "x")})
        result = comparer.compare_acceptance(comparer.read_report(base), comparer.read_report(after))
        self.assertEqual([(row["id"], row["variant"], row["entry"]) for row in result["differences"]], [("T000001", "sloppy", "K")])
        self.assertEqual(result["differences"][0]["verdict"], "unclassified")
        self.assertFalse(result["allDifferencesAdmitted"])

    def test_a_crash_is_compared_by_its_type_alone(self) -> None:
        base = self.acceptance("base.tsv")
        after = self.acceptance("after.tsv", {("T000002", "module", "K"): ("crashed", "System.NotImplementedException", "another text")})
        result = comparer.compare_acceptance(comparer.read_report(base), comparer.read_report(after))
        self.assertEqual(result["differences"], [])

    def test_a_crash_of_another_type_is_a_difference(self) -> None:
        base = self.acceptance("base.tsv")
        after = self.acceptance("after.tsv", {("T000002", "module", "K"): ("crashed", "System.NullReferenceException", "-")})
        result = comparer.compare_acceptance(comparer.read_report(base), comparer.read_report(after))
        self.assertEqual(len(result["differences"]), 1)

    def test_class_m_needs_the_changelog_to_list_both_messages(self) -> None:
        base = self.acceptance("base.tsv")
        after = self.acceptance("after.tsv", {("T000001", "strict", "K"): ("refused", "Broiler.JavaScript.Runtime.JSException:SyntaxError", "Unexpected\\u00A0token")})
        classification = {"T000001|strict|K": {"class": "M", "reason": "listed"}}
        without = comparer.compare_acceptance(comparer.read_report(base), comparer.read_report(after), classifications=classification)
        self.assertEqual(without["differences"][0]["verdict"], "refused classification")
        changelog = "- message: `Unexpected token` is now `Unexpected\u00a0token`\n"
        with_log = comparer.compare_acceptance(comparer.read_report(base), comparer.read_report(after), classifications=classification, changelog=changelog)
        self.assertEqual(with_log["differences"][0]["verdict"], "admitted")
        self.assertTrue(with_log["allDifferencesAdmitted"])

    def test_class_m_refuses_a_changed_type(self) -> None:
        base = self.acceptance("base.tsv")
        after = self.acceptance("after.tsv", {("T000001", "strict", "K"): ("refused", "Other:SyntaxError", "Unexpected token")})
        classification = {"T000001|strict|K": {"class": "M", "reason": "listed"}}
        result = comparer.compare_acceptance(comparer.read_report(base), comparer.read_report(after), classifications=classification, changelog="Unexpected token")
        self.assertEqual(result["differences"][0]["verdict"], "refused classification")

    def test_class_x_admits_a_crash_resolved_at_k(self) -> None:
        base = self.acceptance("base.tsv")
        after = self.acceptance("after.tsv", {("T000002", "module", "K"): ("refused", "Broiler.JavaScript.Runtime.JSException:SyntaxError", "Unexpected token")})
        classification = {"T000002|module|K": {"class": "X", "reason": "a correction the changelog names"}}
        result = comparer.compare_acceptance(comparer.read_report(base), comparer.read_report(after), classifications=classification)
        self.assertEqual(result["differences"][0]["verdict"], "admitted")

    def test_class_x_refuses_a_crash_that_becomes_another_error(self) -> None:
        base = self.acceptance("base.tsv")
        after = self.acceptance("after.tsv", {("T000002", "module", "K"): ("crashed", "Broiler.JavaScript.Runtime.JSException:TypeError", "-")})
        classification = {"T000002|module|K": {"class": "X", "reason": "r"}}
        result = comparer.compare_acceptance(comparer.read_report(base), comparer.read_report(after), classifications=classification)
        self.assertEqual(result["differences"][0]["verdict"], "refused classification")

    def test_class_d_admits_a_desugaring_refusal_moved_out_of_the_parser(self) -> None:
        kept = ("refused", "Broiler.JavaScript.Runtime.JSException:SyntaxError", "Unknown token")
        base = self.acceptance("base.tsv", {("T000002", "module", "K"): kept})
        after = self.acceptance("after.tsv", {("T000002", "module", "P"): ACCEPTED, ("T000002", "module", "K"): kept})
        classification = {"T000002|module|P": {"class": "D", "reason": "the pass refuses it"}}
        result = comparer.compare_acceptance(comparer.read_report(base), comparer.read_report(after), classifications=classification)
        self.assertEqual(result["differences"][0]["verdict"], "admitted")

    def test_class_d_refuses_an_input_whose_k_outcome_changed(self) -> None:
        kept = ("refused", "Broiler.JavaScript.Runtime.JSException:SyntaxError", "Unknown token")
        base = self.acceptance("base.tsv", {("T000002", "module", "K"): kept})
        after = self.acceptance("after.tsv", {("T000002", "module", "P"): ACCEPTED, ("T000002", "module", "K"): ACCEPTED})
        classification = {"T000002|module|P": {"class": "D", "reason": "r"}, "T000002|module|K": {"class": "X", "reason": "r"}}
        result = comparer.compare_acceptance(comparer.read_report(base), comparer.read_report(after), classifications=classification)
        verdicts = {row["entry"]: row["verdict"] for row in result["differences"]}
        self.assertEqual(verdicts, {"P": "refused classification", "K": "refused classification"})

    def test_runs_over_different_manifests_are_not_comparable(self) -> None:
        base = self.acceptance("base.tsv")
        other = self.write("after.tsv", report_text("acceptance", comparer.ACCEPTANCE_COLUMNS, acceptance_rows(), "f" * 64))
        code, _, err = self.run_main("acceptance", "--base", str(base), "--after", str(other))
        self.assertEqual(code, 2)
        self.assertIn("different manifests", err)

    def test_a_run_without_its_end_line_is_not_comparable(self) -> None:
        base = self.acceptance("base.tsv")
        cut = self.write("cut.tsv", "\n".join(base.read_text(encoding="utf-8").split("\n")[:-2]) + "\n")
        code, _, err = self.run_main("acceptance", "--base", str(base), "--after", str(cut))
        self.assertEqual(code, 2)
        self.assertIn("end line", err)

    def test_a_partial_run_is_not_comparable(self) -> None:
        base = self.acceptance("base.tsv")
        partial = self.acceptance("partial.tsv", selection="only\tT000001")
        code, _, err = self.run_main("acceptance", "--base", str(base), "--after", str(partial))
        self.assertEqual(code, 2)
        self.assertIn("partial run", err)

    def test_a_run_missing_a_manifest_variant_is_not_comparable(self) -> None:
        rows = [row for row in acceptance_rows() if row[1] != "sloppy"]
        base = self.write("base.tsv", report_text("acceptance", comparer.ACCEPTANCE_COLUMNS, rows, self.digest))
        code, _, err = self.run_main("acceptance", "--base", str(base), "--after", str(base), "--manifest", str(self.manifest))
        self.assertEqual(code, 2)
        self.assertIn("every manifest input", err)

    def test_the_control_reports_its_two_differences(self) -> None:
        base = self.acceptance("base.tsv")
        control = self.root / "control.tsv.gz"
        output = self.root / "control.json"
        code, out, _ = self.run_main("control", "--base", str(base), "--out", str(control), "--output", str(output))
        self.assertEqual(code, 0, out)
        result = json.loads(output.read_text(encoding="utf-8"))
        self.assertTrue(result["controlReported"])
        self.assertEqual(len(result["differences"]), 2)
        self.assertEqual(result["planted"], [["T000001", "strict", "P"], ["T000001", "strict", "K"]])

    def test_unescape_reverses_the_tool_escapes(self) -> None:
        self.assertEqual(comparer.unescape("a\\tb\\nc\\\\d\\u00E9\\uD800"), "a\tb\nc\\d\u00e9\ud800")
        self.assertEqual(comparer.unescape("-"), "")

    # ---- part D ----------------------------------------------------------------------------

    def dump(self, name: str, rows: list[tuple[str, ...]], order: str, threads: str, passed: str = "absent: none") -> Path:
        return self.write(name, report_text("dump", comparer.DUMP_COLUMNS, rows, self.digest, order=order, threads=threads, **{"for-loop-pass": passed}))

    def dump_rows(self, slot: str, parser: str = "a" * 64, pass_hash: str = "-", status: str = "absent") -> list[tuple[str, ...]]:
        rows = []
        for identifier, variant in (("T000001", "strict"), ("T000001", "sloppy"), ("T000002", "module"), ("V000001", "json")):
            if identifier == "V000001":
                rows.append((identifier, variant, slot, "not-applicable", "-", "-", "-"))
            else:
                rows.append((identifier, variant, slot, "accepted", parser, status, pass_hash))
        return rows

    def test_four_equal_hashes_agree(self) -> None:
        run1 = self.dump("d1.tsv", self.dump_rows("1"), "manifest", "1")
        run2 = self.dump("d2.tsv", self.dump_rows("1"), "reverse", "1")
        run3 = self.dump("d3.tsv", self.dump_rows("A") + self.dump_rows("B"), "manifest", "2")
        code, out, _ = self.run_main("determinism", "--run1", str(run1), "--run2", str(run2), "--run3", str(run3), "--manifest", str(self.manifest))
        self.assertEqual(code, 0, out)
        self.assertIn("population 3; differences 0", out)

    def test_require_pass_fails_when_the_pass_is_absent(self) -> None:
        run1 = self.dump("d1.tsv", self.dump_rows("1"), "manifest", "1")
        run2 = self.dump("d2.tsv", self.dump_rows("1"), "reverse", "1")
        run3 = self.dump("d3.tsv", self.dump_rows("A") + self.dump_rows("B"), "manifest", "2")
        code, out, _ = self.run_main("determinism", "--run1", str(run1), "--run2", str(run2), "--run3", str(run3), "--require-pass")
        self.assertEqual(code, 1)
        self.assertIn("not found in every run", out)

    def test_a_thread_that_differs_is_reported(self) -> None:
        found = "found: Broiler.JavaScript.Compiler.ForLoopPreparation.Prepare"
        rows = self.dump_rows("1", pass_hash="b" * 64, status="prepared")
        run1 = self.dump("d1.tsv", rows, "manifest", "1", found)
        run2 = self.dump("d2.tsv", rows, "reverse", "1", found)
        run3 = self.dump("d3.tsv", self.dump_rows("A", pass_hash="b" * 64, status="prepared")
                         + self.dump_rows("B", pass_hash="c" * 64, status="prepared"), "manifest", "2", found)
        result = comparer.compare_determinism(*(comparer.read_report(path) for path in (run1, run2, run3)), require_pass=True)
        self.assertTrue(result["passFoundInEveryRun"])
        self.assertEqual({row["id"] for row in result["differences"]}, {"T000001", "T000002"})
        self.assertTrue(all(row["differs"] == ["pass"] for row in result["differences"]))

    def test_the_population_comes_from_the_part_c_report(self) -> None:
        population = self.acceptance("after.tsv", {("T000001", "sloppy", "P"): ("refused", "Broiler.JavaScript.Ast.Misc.FastParseException", "x")})
        rows1 = self.dump_rows("1")
        rows2 = [row if row[1] != "sloppy" else row[:4] + ("d" * 64,) + row[5:] for row in self.dump_rows("1")]
        run1 = self.dump("d1.tsv", rows1, "manifest", "1")
        run2 = self.dump("d2.tsv", rows2, "reverse", "1")
        run3 = self.dump("d3.tsv", self.dump_rows("A") + self.dump_rows("B"), "manifest", "2")
        result = comparer.compare_determinism(*(comparer.read_report(path) for path in (run1, run2, run3)), population_report=comparer.read_report(population))
        # T000001 sloppy differs, but P refused it in the population's run, and T000002 module was refused at P too.
        self.assertEqual(result["population"], 1)
        self.assertEqual(result["differences"], [])

    def test_expect_differ_checks_a_control(self) -> None:
        run1 = self.dump("d1.tsv", self.dump_rows("1"), "manifest", "1")
        run2 = self.dump("d2.tsv", self.dump_rows("1", parser="e" * 64), "reverse", "1")
        run3 = self.dump("d3.tsv", self.dump_rows("A") + self.dump_rows("B"), "manifest", "2")
        code, out, _ = self.run_main("determinism", "--run1", str(run1), "--run2", str(run2), "--run3", str(run3), "--expect-differ", "1:2")
        self.assertEqual(code, 0, out)
        code, out, _ = self.run_main("determinism", "--run1", str(run1), "--run2", str(run2), "--run3", str(run3), "--expect-differ", "3A:3B")
        self.assertEqual(code, 1, out)

    def test_runs_in_the_wrong_order_are_not_comparable(self) -> None:
        run1 = self.dump("d1.tsv", self.dump_rows("1"), "manifest", "1")
        run3 = self.dump("d3.tsv", self.dump_rows("A") + self.dump_rows("B"), "manifest", "2")
        code, _, err = self.run_main("determinism", "--run1", str(run1), "--run2", str(run1), "--run3", str(run3))
        self.assertEqual(code, 2)
        self.assertIn("not reverse", err)


if __name__ == "__main__":
    unittest.main()
