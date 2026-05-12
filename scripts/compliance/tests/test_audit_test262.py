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
        only_strict_path = self.write_test(
            "test/language/only-strict.js",
            "/*---\nflags: [onlyStrict]\n---*/\nthis;\n",
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
            "test/language/temporal.js",
            "/*---\nfeatures: [Temporal]\n---*/\nTemporal.Duration();\n",
        )
        self.write_test(
            "test/language/negative.js",
            "/*---\nnegative:\n  phase: runtime\n  type: ReferenceError\n---*/\nnotDeclared;\n",
        )
        self.write_test(
            "test/language/host-harness.js",
            "$262.createRealm();\n",
        )

        repo = Test262Repository("suite-ref", str(self.suite_root))
        summary = audit_test262.build_audit_summary(
            repo,
            self.suite_root,
            "suite-ref",
            [positive_path, async_path, only_strict_path],
            [],
        )

        self.assertEqual(8, summary["suiteTestsDiscovered"])
        self.assertEqual(1, summary["unsupportedFlaggedTests"])
        self.assertEqual({"module": 1}, summary["unsupportedFlagCounts"])
        self.assertEqual(1, summary["unsupportedFeaturedTests"])
        self.assertEqual({"Temporal": 1}, summary["unsupportedFeatureCounts"])
        self.assertEqual(1, summary["negativeTests"])
        self.assertEqual(1, summary["hostHarnessDependentTests"])
        self.assertEqual(4, summary["scriptHostExcludedTests"])
        self.assertEqual(
            {"Temporal": 1, "hostHarness": 1, "module": 1, "negative": 1},
            summary["scriptHostBlockerCounts"],
        )
        self.assertEqual(4, summary["scriptHostVerifiableTests"])
        self.assertEqual(1, summary["asyncScriptHostVerifiableTests"])
        self.assertEqual(3, summary["manifestEntries"])
        self.assertEqual(3, summary["manifestUniqueTests"])
        self.assertEqual(3, summary["manifestScriptHostVerifiableTests"])
        self.assertEqual(
            [{"bucket": "test/language", "count": 4}],
            summary["topLevelCounts"]["scriptHostVerifiable"],
        )
        self.assertEqual(
            [{"bucket": "test/language", "count": 4}],
            summary["topLevelCounts"]["excluded"],
        )
        self.assertEqual(
            [{"bucket": "test/language/no-strict.js", "count": 1}],
            summary["largestUncoveredScriptHostVerifiableBuckets"],
        )
        self.assertAlmostEqual(3 * 100.0 / 8, summary["manifestCoverageOfSuitePercent"])
        expected_script_host_coverage = 3 * 100.0 / 4
        self.assertAlmostEqual(
            expected_script_host_coverage,
            summary["manifestCoverageOfScriptHostVerifiablePercent"],
        )

    def test_manifest_includes_negative_and_unsupported_tests(self) -> None:
        unsupported_path = self.write_test(
            "test/language/module.js",
            "/*---\nflags: [module]\n---*/\nexport {};\n",
        )
        unsupported_feature_path = self.write_test(
            "test/language/temporal.js",
            "/*---\nfeatures: [Temporal]\n---*/\nTemporal.Duration();\n",
        )
        negative_path = self.write_test(
            "test/language/negative.js",
            "/*---\nnegative:\n  phase: runtime\n  type: TypeError\n---*/\nthrow new TypeError('boom');\n",
        )
        host_harness_path = self.write_test(
            "test/language/host-harness.js",
            "$262.detachArrayBuffer({});\n",
        )

        repo = Test262Repository("suite-ref", str(self.suite_root))
        summary = audit_test262.build_audit_summary(
            repo,
            self.suite_root,
            "suite-ref",
            [unsupported_path, unsupported_feature_path, negative_path, host_harness_path],
            [],
        )

        self.assertEqual(
            [unsupported_path, unsupported_feature_path],
            summary["manifestUnsupportedTests"],
        )
        self.assertEqual([negative_path], summary["manifestNegativeTests"])
        self.assertEqual([host_harness_path], summary["manifestHostHarnessTests"])
        self.assertEqual(0, summary["manifestScriptHostVerifiableTests"])

    def test_directory_bucket_rejects_non_positive_depth(self) -> None:
        with self.assertRaises(ValueError):
            audit_test262.directory_bucket("test/language/example.js", 0)


if __name__ == "__main__":
    unittest.main()
