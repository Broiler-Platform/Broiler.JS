from __future__ import annotations

import tempfile
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import audit_test262
from run_test262 import Test262Repository


class AuditTest262Tests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp_directory.cleanup)
        self.suite_root = Path(self.temp_directory.name)
        (self.suite_root / "test" / "language").mkdir(parents=True)

    def write_test(self, relative_path: str, source: str) -> str:
        path = self.suite_root / relative_path
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(source, encoding="utf-8")
        return relative_path

    def write_manifest(self, name: str, *entries: str) -> str:
        path = self.suite_root / name
        path.write_text("\n".join(entries) + "\n", encoding="utf-8")
        return str(path)

    def test_build_audit_summary_reports_suite_and_manifest_coverage(self) -> None:
        positive_path = self.write_test("test/language/positive.js", "1 + 1;\n")
        async_path = self.write_test(
            "test/language/async.js",
            "/*---\nflags: [async]\n---*/\n$DONE();\n",
        )
        self.write_test(
            "test/language/no-strict.js",
            "/*---\nflags: [noStrict]\n---*/\nthis;\n",
        )
        self.write_test(
            "test/language/module.js",
            "/*---\nflags: [module]\n---*/\nexport {};\n",
        )
        self.write_test(
            "test/language/negative.js",
            "/*---\nnegative:\n  phase: runtime\n  type: ReferenceError\n---*/\nnotDeclared;\n",
        )

        repo = Test262Repository("suite-ref", str(self.suite_root))
        summary = audit_test262.build_audit_summary(
            repo,
            self.suite_root,
            "suite-ref",
            [positive_path, async_path],
            [],
        )

        self.assertEqual(5, summary["suiteTestsDiscovered"])
        self.assertEqual(1, summary["unsupportedFlaggedTests"])
        self.assertEqual({"module": 1}, summary["unsupportedFlagCounts"])
        self.assertEqual(1, summary["negativeTests"])
        self.assertEqual(3, summary["scriptHostVerifiableTests"])
        self.assertEqual(1, summary["asyncScriptHostVerifiableTests"])
        self.assertEqual(2, summary["manifestEntries"])
        self.assertEqual(2, summary["manifestUniqueTests"])
        self.assertEqual(2, summary["manifestScriptHostVerifiableTests"])
        self.assertEqual(40.0, summary["manifestCoverageOfSuitePercent"])
        expected_script_host_coverage = 2 * 100.0 / 3
        self.assertAlmostEqual(
            expected_script_host_coverage,
            summary["manifestCoverageOfScriptHostVerifiablePercent"],
        )

    def test_manifest_includes_negative_and_unsupported_tests(self) -> None:
        unsupported_path = self.write_test(
            "test/language/module.js",
            "/*---\nflags: [module]\n---*/\nexport {};\n",
        )
        negative_path = self.write_test(
            "test/language/negative.js",
            "/*---\nnegative:\n  phase: runtime\n  type: TypeError\n---*/\nthrow new TypeError('boom');\n",
        )

        repo = Test262Repository("suite-ref", str(self.suite_root))
        summary = audit_test262.build_audit_summary(
            repo,
            self.suite_root,
            "suite-ref",
            [unsupported_path, negative_path],
            [],
        )

        self.assertEqual([unsupported_path], summary["manifestUnsupportedTests"])
        self.assertEqual([negative_path], summary["manifestNegativeTests"])
        self.assertEqual(0, summary["manifestScriptHostVerifiableTests"])


if __name__ == "__main__":
    unittest.main()
