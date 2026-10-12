from __future__ import annotations

from contextlib import redirect_stderr, redirect_stdout
from io import StringIO
import json
from pathlib import Path
import sys
import tempfile
import unittest


COMPLIANCE_DIR = Path(__file__).resolve().parents[1]
if str(COMPLIANCE_DIR) not in sys.path:
    sys.path.insert(0, str(COMPLIANCE_DIR))

import compare_test262_paths as comparer


def report(results: list[dict[str, object]], **overrides: object) -> dict[str, object]:
    payload: dict[str, object] = {
        "suiteRef": "abc123",
        "selectionMode": "all-script-host-verifiable",
        "testTimeoutSeconds": 30,
        "memoryLimitMb": 0,
        "includeNegative": False,
        "minifier": "terser",
        "minifierVersion": "5.0.0",
        "minifierProfile": "test262-safe-mangle-v2",
        "minifierTimeoutSeconds": 30,
        "minifierOptions": {},
        "variants": ["original", "terser"],
        "hostModes": ["script", "module", "raw"],
        "maxWorkers": 1,
        "results": results,
    }
    payload.update(overrides)
    return payload


def row(path: str, status: str, variant: str = "original") -> dict[str, object]:
    return {"path": path, "variant": variant, "status": status}


class CompareTest262PathsTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_directory = tempfile.TemporaryDirectory()
        self.root = Path(self.temp_directory.name)

    def tearDown(self) -> None:
        self.temp_directory.cleanup()

    def write_json(self, name: str, payload: object) -> Path:
        path = self.root / name
        path.write_text(json.dumps(payload), encoding="utf-8")
        return path

    def run_main(self, *args: str) -> tuple[int, str, str]:
        out, err = StringIO(), StringIO()
        with redirect_stdout(out), redirect_stderr(err):
            code = comparer.main(list(args))
        return code, out.getvalue(), err.getvalue()

    def test_identical_runs_have_no_difference(self) -> None:
        rows = [row("a.js", "passed"), row("a.js", "passed", "terser"), row("b.js", "failed")]
        result = comparer.compare(report(rows), report(rows), {})
        self.assertEqual([], result["changed"])
        self.assertTrue(result["allChangesAdmitted"])

    def test_a_pass_that_fails_after_is_a_regression_even_when_classified(self) -> None:
        base = report([row("a.js", "passed")])
        after = report([row("a.js", "failed")])
        result = comparer.compare(base, after, {"a.js|original": {"class": "C1", "reason": "no"}})
        self.assertEqual(1, result["regressions"])
        self.assertIn("refusedClassification", result["changed"][0])
        self.assertFalse(result["allChangesAdmitted"])

    def test_a_pass_that_times_out_or_is_skipped_after_is_a_regression(self) -> None:
        base = report([row("a.js", "passed"), row("b.js", "passed")])
        after = report([row("a.js", "timedOut"), row("b.js", "skipped")])
        self.assertEqual(2, comparer.compare(base, after, {})["regressions"])

    def test_an_improvement_needs_an_admitted_class_and_a_reason(self) -> None:
        base = report([row("a.js", "failed"), row("b.js", "failed"), row("c.js", "failed")])
        after = report([row("a.js", "passed"), row("b.js", "passed"), row("c.js", "passed")])
        result = comparer.compare(
            base,
            after,
            {
                "a.js|original": {"class": "C1", "reason": "radix 0 is a RangeError"},
                "b.js|original": {"class": "C4", "reason": "harness"},
                "c.js|original": {"class": "C3"},
            },
        )
        verdicts = {entry["path"]: entry["verdict"] for entry in result["changed"]}
        self.assertEqual(
            {"a.js": "admitted", "b.js": "unclassified", "c.js": "unclassified"}, verdicts
        )

    def test_variants_are_joined_separately(self) -> None:
        base = report([row("a.js", "passed"), row("a.js", "passed", "terser")])
        after = report([row("a.js", "passed"), row("a.js", "failed", "terser")])
        result = comparer.compare(base, after, {})
        self.assertEqual([("a.js", "terser")], [(r["path"], r["variant"]) for r in result["changed"]])

    def test_different_row_sets_are_not_comparable(self) -> None:
        with self.assertRaises(comparer.NotComparable):
            comparer.compare(report([row("a.js", "passed")]), report([row("b.js", "passed")]), {})

    def test_duplicate_rows_are_not_comparable(self) -> None:
        rows = [row("a.js", "passed"), row("a.js", "failed")]
        with self.assertRaises(comparer.NotComparable):
            comparer.compare(report(rows), report(rows[:1]), {})

    def test_a_configuration_difference_is_not_comparable(self) -> None:
        rows = [row("a.js", "passed")]
        with self.assertRaises(comparer.NotComparable):
            comparer.compare(report(rows), report(rows, testTimeoutSeconds=60), {})
        with self.assertRaises(comparer.NotComparable):
            comparer.compare(report(rows), report(rows, suiteRef="def456"), {})

    def test_the_worker_count_is_reported_and_not_compared(self) -> None:
        rows = [row("a.js", "passed")]
        result = comparer.compare(report(rows), report(rows, maxWorkers=4), {})
        self.assertEqual({"base": 1, "after": 4}, result["uncomparedFields"]["maxWorkers"])

    def test_an_incomplete_merged_run_is_not_comparable(self) -> None:
        rows = [row("a.js", "passed")]
        merged = {
            "suiteRef": "abc123",
            "suiteRefs": ["abc123"],
            "runConfiguration": {k: v for k, v in report(rows).items() if k != "results"},
            "incompleteShards": [{"shardIndex": 3}],
            "configurationFailures": [],
            "results": rows,
        }
        with self.assertRaises(comparer.NotComparable):
            comparer.compare(merged, report(rows), {})

    def test_a_rerun_replaces_its_row_and_is_reported(self) -> None:
        base = report([row("a.js", "passed")])
        after = report([row("a.js", "timedOut")])
        rerun = self.write_json("rerun.json", report([row("a.js", "passed")]))
        result = comparer.compare(base, after, {}, after_reruns=[rerun])
        self.assertEqual([], result["changed"])
        self.assertEqual("timedOut", result["replacedByRerun"]["after"][0]["before"])

    def test_main_exit_codes(self) -> None:
        base = self.write_json("base.json", report([row("a.js", "passed")]))
        same = self.write_json("same.json", report([row("a.js", "passed")]))
        worse = self.write_json("worse.json", report([row("a.js", "failed")]))
        other = self.write_json("other.json", report([row("b.js", "passed")]))
        output = self.root / "report.json"
        self.assertEqual(0, self.run_main("--base", str(base), "--after", str(same), "--output", str(output))[0])
        self.assertTrue(json.loads(output.read_text(encoding="utf-8"))["allChangesAdmitted"])
        self.assertEqual(1, self.run_main("--base", str(base), "--after", str(worse))[0])
        self.assertEqual(2, self.run_main("--base", str(base), "--after", str(other))[0])


if __name__ == "__main__":
    unittest.main()
