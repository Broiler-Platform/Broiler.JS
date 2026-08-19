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

import merge_test262_shards as merger
import run_test262


class MergeTest262ShardsTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_directory = tempfile.TemporaryDirectory()
        self.root = Path(self.temp_directory.name)

    def tearDown(self) -> None:
        self.temp_directory.cleanup()

    def write_text(
        self, name: str, content: str, directory: str = ""
    ) -> Path:
        path = self.root / directory / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(content, encoding="utf-8")
        return path

    def write_json(
        self, name: str, payload: object, directory: str = ""
    ) -> Path:
        return self.write_text(
            name,
            json.dumps(payload),
            directory=directory,
        )

    def report_payload(
        self,
        index: int,
        results: list[dict[str, object]],
        *,
        suite_ref: str = "abc123",
        selected_before_sharding: int | None = None,
        minifier: str | None = None,
        minifier_version: str | None = None,
        minifier_profile: str | None = None,
        minifier_timeout_seconds: float = 15.0,
        variants: list[str] | None = None,
    ) -> dict[str, object]:
        counts = {
            "passed": sum(result["status"] == "passed" for result in results),
            "failed": sum(result["status"] == "failed" for result in results),
            "skipped": sum(result["status"] == "skipped" for result in results),
            "timedOut": sum(
                result["status"] == "timedOut" for result in results
            ),
            "notApplicable": sum(
                result.get("notApplicable") is True for result in results
            ),
        }
        expanded_paths = list(
            dict.fromkeys(str(result["path"]) for result in results)
        )
        payload: dict[str, object] = {
            "suiteRef": suite_ref,
            "shardIndex": index,
            "shardCount": 8,
            "expandedPaths": expanded_paths,
            "total": len(results),
            "executed": (
                counts["passed"] + counts["failed"] + counts["timedOut"]
            ),
            **counts,
            "results": results,
        }
        if selected_before_sharding is not None:
            payload["selectedCountBeforeSharding"] = selected_before_sharding
        if minifier is not None:
            minified = minifier in merger._MINIFIED_VARIANTS
            configured_variants = variants or (
                ["original", minifier] if minified else ["original"]
            )
            # Callers that do not care about provenance get the pinned profile for
            # whichever minifier they asked for; a caller testing a mismatch passes
            # its own.
            if minifier_profile is None:
                minifier_profile = merger._MINIFIER_PROFILES.get(minifier, "")
            if minifier_version is None:
                minifier_version = (
                    "20260816.0.0" if minifier == "closure" else "5.31.0"
                )
            payload["minifier"] = minifier
            payload["minifierVersion"] = minifier_version if minified else ""
            payload["minifierProfile"] = minifier_profile if minified else ""
            payload["minifierTimeoutSeconds"] = (
                minifier_timeout_seconds if minified else 0
            )
            payload["minifierOptions"] = (
                dict(merger._MINIFIER_OPTIONS[minifier]) if minified else {}
            )
            payload["variants"] = configured_variants
            payload["expectedVariantCountPerPath"] = len(configured_variants)

            variant_summary: dict[str, dict[str, int]] = {}
            for variant in configured_variants:
                variant_results = [
                    result
                    for result in results
                    if str(result.get("variant", "original")) == variant
                ]
                variant_counts = {
                    status: sum(
                        result["status"] == status for result in variant_results
                    )
                    for status in ("passed", "failed", "skipped", "timedOut")
                }
                variant_summary[variant] = {
                    "total": len(variant_results),
                    "executed": (
                        variant_counts["passed"]
                        + variant_counts["failed"]
                        + variant_counts["timedOut"]
                    ),
                    **variant_counts,
                    "notApplicable": sum(
                        result.get("notApplicable") is True
                        for result in variant_results
                    ),
                }
            payload["variantSummary"] = variant_summary

            statuses_by_path: dict[str, set[str]] = {}
            for result in results:
                statuses_by_path.setdefault(str(result["path"]), set()).add(
                    str(result["status"])
                )
            aggregate_statuses: list[str] = []
            for statuses in statuses_by_path.values():
                if "timedOut" in statuses:
                    aggregate_statuses.append("timedOut")
                elif "failed" in statuses:
                    aggregate_statuses.append("failed")
                elif "passed" in statuses:
                    aggregate_statuses.append("passed")
                else:
                    aggregate_statuses.append("skipped")
            payload["pathSummary"] = {
                "total": len(aggregate_statuses),
                "executed": sum(status != "skipped" for status in aggregate_statuses),
                "passed": aggregate_statuses.count("passed"),
                "failed": aggregate_statuses.count("failed"),
                "skipped": aggregate_statuses.count("skipped"),
                "timedOut": aggregate_statuses.count("timedOut"),
            }
        return payload

    def write_report(
        self,
        index: int,
        results: list[dict[str, object]],
        *,
        phase: str = "full",
        retry: bool = False,
        directory: str = "",
        selected_before_sharding: int | None = None,
        suite_ref: str = "abc123",
        minifier: str | None = None,
        minifier_version: str | None = None,
        minifier_profile: str | None = None,
        minifier_timeout_seconds: float = 15.0,
        variants: list[str] | None = None,
    ) -> Path:
        suffix = "-retry" if retry else ""
        return self.write_json(
            f"test262-{phase}-shard-{index}{suffix}.json",
            self.report_payload(
                index,
                results,
                suite_ref=suite_ref,
                selected_before_sharding=selected_before_sharding,
                minifier=minifier,
                minifier_version=minifier_version,
                minifier_profile=minifier_profile,
                minifier_timeout_seconds=minifier_timeout_seconds,
                variants=variants,
            ),
            directory,
        )

    def write_status(
        self,
        index: int,
        exit_code: int,
        *,
        phase: str = "full",
        retry: bool = False,
        reason: str | None = None,
        detail: str | None = None,
        directory: str = "",
    ) -> Path:
        payload: dict[str, object] = {
            "phase": phase,
            "shardIndex": index,
            "exitCode": exit_code,
        }
        if reason is not None:
            payload["failureReason"] = reason
        if detail is not None:
            payload["failureDetail"] = detail
        suffix = "-retry" if retry else ""
        return self.write_json(
            f"test262-{phase}-shard-{index}{suffix}-status.json",
            payload,
            directory,
        )

    def test_discovers_recursively_and_filters_exact_phase(self) -> None:
        self.write_report(
            0,
            [{"path": "test/language/full.js", "status": "passed"}],
            directory="nested/full",
        )
        self.write_report(
            0,
            [{"path": "test/language/rerun.js", "status": "failed"}],
            phase="rerun-failed",
            directory="nested/rerun",
        )
        self.write_json(
            "some-unrelated.json",
            {"results": [{"path": "wrong.js", "status": "failed"}]},
        )

        merged = merger.merge(
            self.root, phase="full", expected_shard_indexes={0}
        )

        self.assertEqual(
            ["test/language/full.js"],
            [result["path"] for result in merged["results"]],
        )
        self.assertEqual(1, merged["summary"]["passed"])
        self.assertEqual(0, merged["summary"]["failed"])

    def test_missing_expected_shard_is_incomplete(self) -> None:
        self.write_report(
            0,
            [{"path": "test/language/a.js", "status": "passed"}],
        )

        merged = merger.merge(
            self.root, expected_shard_indexes={0, 1, 2}
        )

        self.assertEqual([0], merged["reportedShards"])
        self.assertEqual(
            [1, 2],
            [item["shardIndex"] for item in merged["incompleteShards"]],
        )
        self.assertTrue(
            all(
                item["failureReason"] == "MissingReport"
                for item in merged["incompleteShards"]
            )
        )

    def test_expected_shards_exclude_unexpected_artifacts_from_manifest(self) -> None:
        manifest = self.write_text(
            "failures.txt",
            "test/from-unexpected-shard.js\n",
        )
        self.write_report(
            0,
            [{"path": "test/in-scope.js", "status": "passed"}],
        )
        self.write_report(
            1,
            [
                {
                    "path": "test/from-unexpected-shard.js",
                    "status": "passed",
                }
            ],
        )

        merged = merger.merge(self.root, expected_shard_indexes={0})
        merged = merger.merge_into_manifest(merged, manifest)

        self.assertEqual([0], merged["reportedShards"])
        self.assertEqual(
            ["test/in-scope.js"],
            [result["path"] for result in merged["results"]],
        )
        self.assertEqual(
            ["test/from-unexpected-shard.js"],
            merged["manifestPaths"],
        )

    def test_malformed_report_is_incomplete(self) -> None:
        self.write_text("test262-full-shard-3.json", "{not-json")

        merged = merger.merge(
            self.root, expected_shard_indexes={3}
        )

        self.assertEqual(0, merged["shardCount"])
        self.assertEqual("MalformedReport", merged["incompleteShards"][0]["failureReason"])
        self.assertIn("invalid JSON", merged["incompleteShards"][0]["failureDetail"])

    def test_structurally_incomplete_report_is_malformed(self) -> None:
        self.write_json(
            "test262-full-shard-1.json",
            {"shardIndex": 1, "failed": 4},
        )

        merged = merger.merge(self.root, expected_shard_indexes={1})

        self.assertEqual("MalformedReport", merged["incompleteShards"][0]["failureReason"])
        self.assertIn("no results array", merged["incompleteShards"][0]["failureDetail"])

    def test_invalid_selected_count_makes_report_malformed(self) -> None:
        payload = self.report_payload(
            1,
            [{"path": "test/a.js", "status": "passed"}],
        )
        payload["selectedCountBeforeSharding"] = "not-a-number"
        self.write_json("test262-full-shard-1.json", payload)

        merged = merger.merge(self.root, expected_shard_indexes={1})

        self.assertEqual("MalformedReport", merged["incompleteShards"][0]["failureReason"])
        self.assertIn(
            "selectedCountBeforeSharding is not an integer",
            merged["incompleteShards"][0]["failureDetail"],
        )

    def test_malformed_status_without_report_is_incomplete(self) -> None:
        self.write_text(
            "test262-full-shard-2-status.json",
            "{bad",
            directory="artifact",
        )

        merged = merger.merge(self.root, expected_shard_indexes={2})

        self.assertEqual("MalformedStatus", merged["incompleteShards"][0]["failureReason"])
        self.assertIn("invalid JSON", merged["incompleteShards"][0]["failureDetail"])

    def test_status_failure_reason_is_preserved_and_preferred(self) -> None:
        self.write_status(
            4,
            69,
            reason="BrowserDownloadBlocked",
            detail="The browser CDN returned HTTP 403.",
        )

        merged = merger.merge(self.root, expected_shard_indexes={4})
        incomplete = merged["incompleteShards"][0]

        self.assertEqual(69, incomplete["exitCode"])
        self.assertEqual("BrowserDownloadBlocked", incomplete["failureReason"])
        self.assertEqual(
            "The browser CDN returned HTTP 403.",
            incomplete["failureDetail"],
        )
        self.assertIn("BrowserDownloadBlocked", incomplete["reason"])
        self.assertIn(
            "BrowserDownloadBlocked",
            merger.render_issue_markdown(merged),
        )

    def test_status_reason_wins_when_report_is_also_malformed(self) -> None:
        self.write_text("test262-full-shard-4.json", "not json")
        self.write_status(
            4,
            70,
            reason="RunnerCrashed",
            detail="Worker process terminated.",
        )

        merged = merger.merge(self.root, expected_shard_indexes={4})
        incomplete = merged["incompleteShards"][0]

        self.assertEqual("RunnerCrashed", incomplete["failureReason"])
        self.assertIn("Worker process terminated.", incomplete["failureDetail"])
        self.assertIn("invalid JSON", incomplete["failureDetail"])

    def test_complete_report_with_ordinary_failure_is_conclusive(self) -> None:
        self.write_report(
            0,
            [
                {
                    "path": "test/language/failure.js",
                    "status": "failed",
                    "stderr": "assertion failed",
                }
            ],
        )
        self.write_status(0, 1)

        merged = merger.merge(self.root, expected_shard_indexes={0})

        self.assertEqual([], merged["incompleteShards"])
        self.assertEqual(1, merged["summary"]["failed"])
        self.assertEqual(["test/language/failure.js"], merged["failedPaths"])

    def test_retry_report_supersedes_initial_without_double_counting(self) -> None:
        self.write_report(
            0,
            [{"path": "test/language/old.js", "status": "failed"}],
            directory="initial",
        )
        self.write_report(
            0,
            [{"path": "test/language/new.js", "status": "passed"}],
            retry=True,
            directory="retry",
        )

        merged = merger.merge(self.root, expected_shard_indexes={0})

        self.assertEqual([0], merged["retriedShards"])
        self.assertEqual(1, merged["summary"]["total"])
        self.assertEqual(1, merged["summary"]["passed"])
        self.assertEqual(
            ["test/language/new.js"],
            [result["path"] for result in merged["results"]],
        )

    def test_retry_with_test_failures_is_conclusive(self) -> None:
        self.write_status(1, 137)
        self.write_report(
            1,
            [
                {
                    "path": "test/built-ins/retry-failure.js",
                    "status": "failed",
                    "reason": "expected true but got false",
                }
            ],
            retry=True,
        )
        self.write_status(1, 1, retry=True)

        merged = merger.merge(self.root, expected_shard_indexes={1})

        self.assertEqual([1], merged["retriedShards"])
        self.assertEqual([], merged["incompleteShards"])
        self.assertEqual(1, merged["summary"]["failed"])

    def test_failed_retry_does_not_fall_back_to_complete_initial_report(self) -> None:
        self.write_report(
            2,
            [{"path": "test/language/initial.js", "status": "passed"}],
        )
        self.write_status(
            2,
            137,
            retry=True,
            reason="RunnerTerminated",
            detail="Hosted runner was evicted.",
        )

        merged = merger.merge(self.root, expected_shard_indexes={2})

        self.assertEqual([], merged["results"])
        self.assertEqual([2], merged["retriedShards"])
        self.assertTrue(merged["incompleteShards"][0]["retried"])
        self.assertEqual(
            "RunnerTerminated",
            merged["incompleteShards"][0]["failureReason"],
        )

    def test_malformed_retry_does_not_fall_back_to_initial_report(self) -> None:
        self.write_report(
            2,
            [{"path": "test/language/initial.js", "status": "passed"}],
        )
        self.write_text("test262-full-shard-2-retry.json", "{")

        merged = merger.merge(self.root, expected_shard_indexes={2})

        self.assertEqual([], merged["results"])
        self.assertEqual("MalformedReport", merged["incompleteShards"][0]["failureReason"])
        self.assertTrue(merged["incompleteShards"][0]["retried"])

    def test_merge_deduplicates_paths_with_deterministic_severity(self) -> None:
        self.write_report(
            0,
            [
                {"path": "test/language/z.js", "status": "passed"},
                {"path": "test/language/shared.js", "status": "passed"},
            ],
            directory="z-artifact",
        )
        self.write_report(
            1,
            [
                {
                    "path": "test/language/shared.js",
                    "status": "failed",
                    "reason": "duplicate shard failure",
                },
                {"path": "./test/language/a.js", "status": "skipped"},
            ],
            directory="a-artifact",
        )

        merged = merger.merge(
            self.root, expected_shard_indexes={0, 1}
        )

        self.assertEqual(
            [
                "test/language/a.js",
                "test/language/shared.js",
                "test/language/z.js",
            ],
            [result["path"] for result in merged["results"]],
        )
        self.assertEqual("failed", merged["results"][1]["status"])
        self.assertEqual(
            {
                "total": 3,
                "executed": 2,
                "passed": 1,
                "failed": 1,
                "skipped": 1,
                "timedOut": 0,
                "notApplicable": 0,
                "notApplicableByKind": {},
                "uniquePaths": 3,
            },
            merged["summary"],
        )

    def test_result_identity_preserves_original_and_terser_cases(self) -> None:
        path = "test/language/variant.js"
        self.write_report(
            0,
            [
                {"path": path, "variant": "original", "status": "passed"},
                {
                    "path": path,
                    "variant": "terser",
                    "status": "failed",
                    "reason": "minified source changed behavior",
                },
            ],
            minifier="terser",
        )

        merged = merger.merge(self.root, expected_shard_indexes={0})

        self.assertEqual(
            [(path, "original"), (path, "terser")],
            [(result["path"], result["variant"]) for result in merged["results"]],
        )
        self.assertEqual(2, merged["summary"]["total"])
        self.assertEqual(1, merged["summary"]["uniquePaths"])
        self.assertEqual([], merged["passedPaths"])
        self.assertEqual([path], merged["failedPaths"])
        self.assertEqual(
            [{"path": path, "variant": "terser"}],
            merged["failedCases"],
        )
        self.assertEqual(1, merged["variantSummary"]["original"]["passed"])
        self.assertEqual(1, merged["variantSummary"]["terser"]["failed"])
        self.assertEqual([], merged["executedPaths"])

    def test_legacy_result_without_variant_defaults_to_original(self) -> None:
        path = "test/language/legacy.js"
        self.write_report(0, [{"path": path, "status": "passed"}])

        merged = merger.merge(self.root, expected_shard_indexes={0})

        self.assertEqual("original", merged["results"][0]["variant"])
        self.assertEqual(["original"], merged["observedVariants"])
        self.assertEqual([path], merged["executedPaths"])

    def test_minified_report_missing_terser_case_is_incomplete(self) -> None:
        path = "test/language/partial.js"
        self.write_report(
            0,
            [{"path": path, "variant": "original", "status": "passed"}],
            minifier="terser",
        )

        merged = merger.merge(self.root, expected_shard_indexes={0})

        self.assertEqual([], merged["results"])
        self.assertEqual("MalformedReport", merged["incompleteShards"][0]["failureReason"])
        self.assertIn("missing terser", merged["incompleteShards"][0]["failureDetail"])

    def test_duplicate_path_variant_identity_is_malformed(self) -> None:
        path = "test/language/duplicate.js"
        self.write_report(
            0,
            [
                {"path": path, "variant": "original", "status": "passed"},
                {"path": path, "variant": "original", "status": "failed"},
            ],
        )

        merged = merger.merge(self.root, expected_shard_indexes={0})

        self.assertEqual("MalformedReport", merged["incompleteShards"][0]["failureReason"])
        self.assertIn("duplicate result", merged["incompleteShards"][0]["failureDetail"])

    def test_explicit_variant_must_use_the_exact_schema_value(self) -> None:
        self.write_report(
            0,
            [
                {
                    "path": "test/language/bad-variant.js",
                    "variant": "Original",
                    "status": "passed",
                }
            ],
        )

        merged = merger.merge(self.root, expected_shard_indexes={0})

        self.assertEqual("MalformedReport", merged["incompleteShards"][0]["failureReason"])
        self.assertIn("unknown variant", merged["incompleteShards"][0]["failureDetail"])

    def test_not_applicable_terser_case_is_a_conclusive_manifest_exemption(self) -> None:
        path = "test/language/source-sensitive.js"
        manifest = self.write_text("failures.txt", f"{path}\n")
        self.write_report(
            0,
            [
                {"path": path, "variant": "original", "status": "passed"},
                {
                    "path": path,
                    "variant": "terser",
                    "status": "skipped",
                    "notApplicable": True,
                    "skipKind": "minifier-not-applicable",
                    "reason": "source-text-sensitive test",
                },
            ],
            minifier="terser",
        )

        merged = merger.merge(self.root, expected_shard_indexes={0})
        persisted = merger.merge_into_manifest(merged, manifest)

        self.assertEqual(1, merged["summary"]["skipped"])
        self.assertEqual(1, merged["summary"]["notApplicable"])
        self.assertEqual([path], merged["passedPaths"])
        self.assertEqual([path], merged["notApplicablePaths"])
        self.assertEqual([path], merged["executedPaths"])
        self.assertEqual(1, merged["pathSummary"]["executed"])
        self.assertEqual(1, merged["pathSummary"]["refreshable"])
        self.assertEqual(
            [{"path": path, "variant": "terser"}],
            merged["notApplicableCases"],
        )
        self.assertEqual([], persisted["manifestPaths"])

    def test_minifier_unsupported_syntax_is_a_not_applicable_skip_kind(self) -> None:
        # A source the minifier's own parser rejects yields no minified variant, so the
        # case is exempt in the same way a source-text-sensitive test is — under its own
        # skip kind, so a report can still tell the two apart.
        path = "test/language/escaped-keyword-method.js"
        manifest = self.write_text("failures.txt", f"{path}\n")
        self.write_report(
            0,
            [
                {"path": path, "variant": "original", "status": "passed"},
                {
                    "path": path,
                    "variant": "terser",
                    "status": "skipped",
                    "notApplicable": True,
                    "skipKind": "minifier-unsupported-syntax",
                    "reason": (
                        "minifier cannot parse this source: Terser SyntaxError at "
                        "line 8, column 2: Escaped characters are not allowed in keywords"
                    ),
                },
            ],
            minifier="terser",
        )

        merged = merger.merge(self.root, expected_shard_indexes={0})
        persisted = merger.merge_into_manifest(merged, manifest)

        self.assertEqual([], merged["incompleteShards"])
        self.assertEqual(1, merged["summary"]["notApplicable"])
        self.assertEqual(0, merged["summary"]["failed"])
        self.assertEqual([path], merged["notApplicablePaths"])
        self.assertEqual([], persisted["manifestPaths"])

    def test_cross_checked_skip_kinds_leave_the_minifier_only_report(self) -> None:
        # The two verdicts the reference engine can return. Both are conclusive exemptions:
        # they clear the manifest, they are counted per kind so they stay visible, and
        # neither reaches the Terser-only report, which exists to name ENGINE defects.
        invalid = "test/language/minifier-wrote-invalid-source.js"
        changed = "test/language/mangler-renamed-what-it-measures.js"
        manifest = self.write_text("failures.txt", f"{invalid}\n{changed}\n")
        self.write_report(
            0,
            [
                {"path": invalid, "variant": "original", "status": "passed"},
                {
                    "path": invalid,
                    "variant": "terser",
                    "status": "skipped",
                    "notApplicable": True,
                    "skipKind": "minifier-invalid-output",
                    "reason": "node runs this test but cannot parse its minified body",
                },
                {"path": changed, "variant": "original", "status": "passed"},
                {
                    "path": changed,
                    "variant": "terser",
                    "status": "skipped",
                    "notApplicable": True,
                    "skipKind": "minifier-changed-semantics",
                    "reason": "node passes this test and fails its minified body",
                },
            ],
            minifier="terser",
        )

        merged = merger.merge(self.root, expected_shard_indexes={0})
        persisted = merger.merge_into_manifest(merged, manifest)

        self.assertEqual([], merged["incompleteShards"])
        self.assertEqual(0, merged["summary"]["failed"])
        self.assertEqual(2, merged["summary"]["notApplicable"])
        self.assertEqual(
            {"minifier-changed-semantics": 1, "minifier-invalid-output": 1},
            merged["summary"]["notApplicableByKind"],
        )
        self.assertEqual([changed, invalid], sorted(merged["notApplicablePaths"]))
        self.assertEqual(0, merged["minifierOnlyFailureCount"])
        self.assertEqual([], persisted["manifestPaths"])

        body = merger.render_minifier_only_issue_markdown(merged)
        self.assertIn("Attributed to the minifier", body)

    def test_a_minifier_crash_is_a_conclusive_exemption_of_its_own(self) -> None:
        # Closure accepting `for (x.y of [23])` and then crashing on it produced no
        # minified body, so — like a source its parser declines — the case is not
        # applicable, clears the manifest, and stays visible under its own kind rather
        # than being folded into "the minifier could not read it".
        crashed = "test/language/minifier-crashed-on-this.js"
        manifest = self.write_text("failures.txt", f"{crashed}\n")
        self.write_report(
            0,
            [
                {"path": crashed, "variant": "original", "status": "passed"},
                {
                    "path": crashed,
                    "variant": "closure",
                    "status": "skipped",
                    "notApplicable": True,
                    "skipKind": "minifier-internal-error",
                    "reason": "minifier crashed on this source: NullPointerException",
                },
            ],
            minifier="closure",
        )

        merged = merger.merge(self.root, expected_shard_indexes={0})
        persisted = merger.merge_into_manifest(merged, manifest)

        self.assertEqual([], merged["incompleteShards"])
        self.assertEqual(0, merged["summary"]["failed"])
        self.assertEqual(
            {"minifier-internal-error": 1},
            merged["summary"]["notApplicableByKind"],
        )
        self.assertEqual(0, merged["minifierOnlyFailureCount"])
        self.assertEqual([], persisted["manifestPaths"])
        self.assertIn(
            "source the minifier crashed on",
            merger.render_minifier_only_issue_markdown(merged),
        )

    def test_not_applicable_marker_requires_terser_skip_contract(self) -> None:
        path = "test/language/bad-marker.js"
        self.write_report(
            0,
            [
                {
                    "path": path,
                    "variant": "original",
                    "status": "passed",
                    "notApplicable": True,
                    "skipKind": "minifier-not-applicable",
                }
            ],
        )

        merged = merger.merge(self.root, expected_shard_indexes={0})

        self.assertEqual("MalformedReport", merged["incompleteShards"][0]["failureReason"])
        self.assertIn("non-skipped", merged["incompleteShards"][0]["failureDetail"])

    def test_all_requested_variants_passing_clear_base_manifest_path(self) -> None:
        path = "test/language/fixed.js"
        manifest = self.write_text("failures.txt", f"{path}\n")
        self.write_report(
            0,
            [
                {"path": path, "variant": "original", "status": "passed"},
                {"path": path, "variant": "terser", "status": "passed"},
            ],
            minifier="terser",
        )

        merged = merger.merge(self.root, expected_shard_indexes={0})
        persisted = merger.merge_into_manifest(merged, manifest)

        self.assertEqual([path], merged["executedPaths"])
        self.assertEqual([], persisted["manifestPaths"])

    def test_one_variant_failure_retains_base_manifest_path_once(self) -> None:
        path = "test/language/still-broken.js"
        manifest = self.write_text("failures.txt", "")
        self.write_report(
            0,
            [
                {"path": path, "variant": "original", "status": "passed"},
                {
                    "path": path,
                    "variant": "terser",
                    "status": "failed",
                    "infrastructure": True,
                    "failureType": "MinifierError",
                    "reason": "Terser rejected the input",
                },
            ],
            minifier="terser",
        )

        merged = merger.merge(self.root, expected_shard_indexes={0})
        persisted = merger.merge_into_manifest(merged, manifest)

        self.assertEqual([], merged["executedPaths"])
        self.assertEqual(1, merged["pathSummary"]["executed"])
        self.assertEqual(0, merged["pathSummary"]["refreshable"])
        self.assertEqual([path], persisted["manifestPaths"])
        self.assertEqual(1, merged["summary"]["failed"])
        self.assertEqual(0, merged["timeoutCount"])
        minifier_group = merged["problemGroups"][0]
        self.assertEqual("MinifierFailure", minifier_group["kind"])
        self.assertIn("MinifierError", minifier_group["label"])

    def test_ordinary_terser_skip_does_not_clear_base_manifest_path(self) -> None:
        path = "test/language/ordinary-skip.js"
        manifest = self.write_text("failures.txt", f"{path}\n")
        self.write_report(
            0,
            [
                {"path": path, "variant": "original", "status": "passed"},
                {
                    "path": path,
                    "variant": "terser",
                    "status": "skipped",
                    "reason": "worker chose not to run this case",
                },
            ],
            minifier="terser",
        )

        merged = merger.merge(self.root, expected_shard_indexes={0})
        persisted = merger.merge_into_manifest(merged, manifest)

        self.assertEqual([], merged["executedPaths"])
        self.assertEqual(1, merged["pathSummary"]["executed"])
        self.assertEqual(0, merged["pathSummary"]["refreshable"])
        self.assertEqual([path], persisted["manifestPaths"])

    def test_minifier_timeout_failure_is_not_an_engine_timeout(self) -> None:
        path = "test/language/minifier-timeout.js"
        self.write_report(
            0,
            [
                {"path": path, "variant": "original", "status": "passed"},
                {
                    "path": path,
                    "variant": "terser",
                    "status": "failed",
                    "infrastructure": True,
                    "failureType": "MinifierTimeout",
                    "reason": "Terser exceeded 15 seconds",
                },
            ],
            minifier="terser",
        )

        merged = merger.merge(self.root, expected_shard_indexes={0})

        self.assertEqual(0, merged["timeoutCount"])
        self.assertEqual([], merged["timeouts"])
        self.assertEqual("MinifierFailure", merged["problemGroups"][0]["kind"])
        self.assertEqual("MinifierFailure", merged["biggestProblems"][0]["kind"])

    def test_base_path_status_uses_variant_severity_precedence(self) -> None:
        path = "test/language/mixed-outcome.js"
        self.write_report(
            0,
            [
                {
                    "path": path,
                    "variant": "original",
                    "status": "failed",
                    "reason": "original assertion failed",
                },
                {
                    "path": path,
                    "variant": "terser",
                    "status": "timedOut",
                },
            ],
            minifier="terser",
        )

        merged = merger.merge(self.root, expected_shard_indexes={0})

        self.assertEqual([], merged["failedPaths"])
        self.assertEqual([path], merged["timedOutPaths"])
        self.assertEqual(1, len(merged["failedCases"]))
        self.assertEqual(1, len(merged["timedOutCases"]))
        self.assertEqual(1, merged["pathSummary"]["timedOut"])
        self.assertEqual([], merged["executedPaths"])

    def test_same_path_variant_timeouts_remain_distinct_and_ordered(self) -> None:
        path = "test/language/both-timeout.js"
        self.write_report(
            0,
            [
                {
                    "path": path,
                    "variant": "terser",
                    "status": "timedOut",
                    "sourceSizeBytes": 64,
                    "features": ["Promise"],
                },
                {
                    "path": path,
                    "variant": "original",
                    "status": "timedOut",
                    "sourceSizeBytes": 64,
                    "features": ["Promise"],
                },
            ],
            minifier="terser",
        )

        merged = merger.merge(
            self.root, expected_shard_indexes={0}, timeout_limit=5
        )

        self.assertEqual(2, merged["timeoutCount"])
        self.assertEqual(
            ["original", "terser"],
            [timeout["variant"] for timeout in merged["timeouts"]],
        )
        self.assertEqual(2, merged["timeoutFeatureGroups"][0]["count"])
        self.assertEqual([path], merged["timedOutPaths"])
        markdown = merger.render_timeout_issue_markdown(merged)
        self.assertIn(f"{path} [original]", markdown)
        self.assertIn(f"{path} [terser]", markdown)

    def test_variant_retry_supersedes_both_initial_cases(self) -> None:
        path = "test/language/retried-variants.js"
        self.write_report(
            0,
            [
                {"path": path, "variant": "original", "status": "failed"},
                {"path": path, "variant": "terser", "status": "failed"},
            ],
            minifier="terser",
            directory="initial",
        )
        self.write_report(
            0,
            [
                {"path": path, "variant": "original", "status": "passed"},
                {"path": path, "variant": "terser", "status": "passed"},
            ],
            minifier="terser",
            retry=True,
            directory="retry",
        )

        merged = merger.merge(self.root, expected_shard_indexes={0})

        self.assertEqual([0], merged["retriedShards"])
        self.assertEqual(2, merged["summary"]["total"])
        self.assertEqual(2, merged["summary"]["passed"])
        self.assertEqual(0, merged["summary"]["failed"])
        self.assertEqual([path], merged["executedPaths"])

    def test_problem_groups_separate_variants_and_show_case_samples(self) -> None:
        path = "test/language/both-fail.js"
        self.write_report(
            0,
            [
                {
                    "path": path,
                    "variant": "original",
                    "status": "failed",
                    "reason": "same diagnostic",
                },
                {
                    "path": path,
                    "variant": "terser",
                    "status": "failed",
                    "reason": "same diagnostic",
                },
            ],
            minifier="terser",
        )

        merged = merger.merge(self.root, expected_shard_indexes={0})
        groups = merged["problemGroups"]

        self.assertEqual(2, len(groups))
        self.assertEqual(
            ["original", "terser"],
            sorted(group["variants"][0]["variant"] for group in groups),
        )
        self.assertTrue(all(group["caseSamples"] for group in groups))
        markdown = merger.render_issue_markdown(merged)
        self.assertIn(f"{path} [original]", markdown)
        self.assertIn(f"{path} [terser]", markdown)

    def test_totals_and_path_sets_are_sorted(self) -> None:
        self.write_report(
            0,
            [
                {"path": "test/z.js", "status": "timedOut"},
                {"path": "test/b.js", "status": "failed"},
                {"path": "test/a.js", "status": "passed"},
                {"path": "test/s.js", "status": "skipped"},
            ],
        )

        merged = merger.merge(self.root, expected_shard_indexes={0})

        self.assertEqual(["test/a.js"], merged["passedPaths"])
        self.assertEqual(["test/b.js"], merged["failedPaths"])
        self.assertEqual(["test/s.js"], merged["skippedPaths"])
        self.assertEqual(["test/z.js"], merged["timedOutPaths"])
        self.assertEqual(
            ["test/a.js"],
            merged["executedPaths"],
        )

    def test_manifest_refreshes_only_conclusively_executed_paths(self) -> None:
        manifest = self.write_text(
            "failures.txt",
            "\n".join(
                [
                    "# Existing header",
                    "",
                    "test/out-of-scope.js",
                    "test/now-passes.js",
                    "test/still-fails.js",
                    "",
                ]
            ),
        )
        self.write_report(
            0,
            [
                {"path": "test/now-passes.js", "status": "passed"},
                {"path": "test/still-fails.js", "status": "failed"},
                {"path": "test/new-timeout.js", "status": "timedOut"},
            ],
        )

        merged = merger.merge(self.root, expected_shard_indexes={0})
        merged = merger.merge_into_manifest(merged, manifest)

        self.assertEqual(
            [
                "test/new-timeout.js",
                "test/out-of-scope.js",
                "test/still-fails.js",
            ],
            merged["manifestPaths"],
        )
        self.assertEqual(
            "\n".join(
                [
                    "# Existing header",
                    "",
                    "test/new-timeout.js",
                    "test/out-of-scope.js",
                    "test/still-fails.js",
                    "",
                ]
            ),
            manifest.read_text(encoding="utf-8"),
        )

    def test_manifest_preserves_skipped_old_failure(self) -> None:
        manifest = self.write_text(
            "failures.txt",
            "test/skipped.js\n",
        )
        self.write_report(
            0,
            [{"path": "test/skipped.js", "status": "skipped"}],
        )

        merged = merger.merge(self.root, expected_shard_indexes={0})
        result = merger.merge_into_manifest(merged, manifest)

        self.assertEqual(["test/skipped.js"], result["manifestPaths"])

    def test_manifest_preserves_paths_from_incomplete_retry(self) -> None:
        manifest = self.write_text(
            "failures.txt",
            "test/from-incomplete-shard.js\n",
        )
        self.write_report(
            0,
            [
                {
                    "path": "test/from-incomplete-shard.js",
                    "status": "passed",
                }
            ],
        )
        self.write_status(
            0,
            137,
            retry=True,
            reason="RunnerTerminated",
        )

        merged = merger.merge(self.root, expected_shard_indexes={0})
        result = merger.merge_into_manifest(merged, manifest)

        self.assertEqual([], merged["executedPaths"])
        self.assertEqual(
            ["test/from-incomplete-shard.js"],
            result["manifestPaths"],
        )

    def test_manifest_is_created_when_it_does_not_exist(self) -> None:
        manifest = self.root / "new" / "failures.txt"
        self.write_report(
            0,
            [{"path": "test/new.js", "status": "failed"}],
        )

        result = merger.merge_into_manifest(
            merger.merge(self.root, expected_shard_indexes={0}),
            manifest,
        )

        self.assertEqual(["test/new.js"], result["manifestPaths"])
        self.assertEqual("test/new.js\n", manifest.read_text(encoding="utf-8"))

    def test_common_groups_normalise_crashes_and_include_generic_no_output(self) -> None:
        self.write_report(
            0,
            [
                {
                    "path": "test/language/crash-a.js",
                    "status": "failed",
                    "stderr": (
                        "Unhandled exception. System.InvalidOperationException: "
                        "Bad item 123\n"
                        "at Compile in /repo/Compiler.cs:line 17\n"
                    ),
                    "features": ["class"],
                },
                {
                    "path": "test/language/crash-b.js",
                    "status": "failed",
                    "stderr": (
                        "Unhandled exception. System.InvalidOperationException: "
                        "Bad item 456\n"
                        "at Compile in /repo/Compiler.cs:line 99\n"
                    ),
                    "features": ["class"],
                },
                {
                    "path": "test/language/reason.js",
                    "status": "failed",
                    "reason": "negative test expected TypeError but succeeded",
                },
                {
                    "path": "test/language/no-output.js",
                    "status": "failed",
                },
            ],
        )
        self.write_status(
            1,
            137,
            reason="RunnerTerminated",
            detail="VM disappeared",
        )

        merged = merger.merge(
            self.root, expected_shard_indexes={0, 1}
        )
        groups = merged["problemGroups"]

        crash = next(group for group in groups if group["kind"] == "Crash")
        self.assertEqual(2, crash["count"])
        self.assertEqual(
            [{"feature": "class", "count": 2}],
            crash["features"],
        )
        self.assertTrue(any(group["kind"] == "Failure" for group in groups))
        self.assertTrue(any(group["kind"] == "NoOutput" for group in groups))
        self.assertTrue(
            any(group["kind"] == "IncompleteShard" for group in groups)
        )

    def test_unhandled_javascript_exception_is_a_failure_not_engine_crash(self) -> None:
        self.write_report(
            0,
            [
                {
                    "path": "test/language/assertion.js",
                    "status": "failed",
                    "stderr": (
                        "Unhandled exception. "
                        "Broiler.JavaScript.Runtime.JSException: "
                        "Test262Error: Expected SameValue to be true\n"
                        "at Throw in /repo/JSException.cs:line 111\n"
                    ),
                },
                {
                    "path": "test/language/engine-crash.js",
                    "status": "failed",
                    "stderr": (
                        "Unhandled exception. System.InvalidOperationException: "
                        "Sequence contains no elements\n"
                        "at Compile in /repo/Compiler.cs:line 17\n"
                    ),
                },
            ],
        )

        groups = merger.merge(
            self.root, expected_shard_indexes={0}
        )["problemGroups"]
        javascript_group = next(
            group for group in groups if "JSException" in group["label"]
        )
        system_group = next(
            group for group in groups if "InvalidOperationException" in group["label"]
        )

        self.assertEqual("Failure", javascript_group["kind"])
        self.assertEqual("Crash", system_group["kind"])

    def test_empty_global_selection_is_a_configuration_failure(self) -> None:
        self.write_report(0, [], selected_before_sharding=0)
        self.write_report(1, [], selected_before_sharding=0)
        github_output = self.root / "github-output.txt"

        merged = merger.merge(self.root, expected_shard_indexes={0, 1})
        merger._write_github_outputs(github_output, merged)

        self.assertEqual("EmptySelection", merged["configurationFailures"][0]["kind"])
        self.assertIn("Configuration failures", merger.render_issue_markdown(merged))
        outputs = github_output.read_text(encoding="utf-8")
        self.assertIn("configuration_failure_count=1", outputs)
        self.assertIn("create_issue=true", outputs)
        self.assertIn("suite_passed=false", outputs)

    def test_empty_individual_shard_is_valid_when_global_selection_exists(self) -> None:
        self.write_report(3, [], selected_before_sharding=5)
        github_output = self.root / "github-output.txt"

        merged = merger.merge(self.root, expected_shard_indexes={3})
        merger._write_github_outputs(github_output, merged)

        self.assertEqual([], merged["configurationFailures"])
        self.assertIn(
            "suite_passed=true",
            github_output.read_text(encoding="utf-8"),
        )

    def test_all_skipped_selection_is_a_configuration_failure(self) -> None:
        self.write_report(
            0,
            [{"path": "test/unsupported.js", "status": "skipped"}],
            selected_before_sharding=1,
        )

        merged = merger.merge(self.root, expected_shard_indexes={0})

        self.assertEqual("NoExecutedTests", merged["configurationFailures"][0]["kind"])

    def test_mixed_suite_refs_and_shard_configuration_fail_the_run(self) -> None:
        self.write_report(
            0,
            [{"path": "test/a.js", "status": "passed"}],
            suite_ref="commit-a",
            selected_before_sharding=2,
        )
        self.write_report(
            1,
            [{"path": "test/b.js", "status": "passed"}],
            suite_ref="commit-b",
            selected_before_sharding=3,
        )
        github_output = self.root / "github-output.txt"

        merged = merger.merge(self.root, expected_shard_indexes={0, 1})
        merger._write_github_outputs(github_output, merged)

        kinds = [failure["kind"] for failure in merged["configurationFailures"]]
        self.assertEqual(
            ["InconsistentShardConfiguration", "InconsistentShardConfiguration"],
            kinds,
        )
        self.assertEqual("", merged["suiteRef"])
        self.assertIn(
            "suite_passed=false",
            github_output.read_text(encoding="utf-8"),
        )

    def test_complete_shard_space_must_cover_global_selection(self) -> None:
        for index, path in enumerate(("test/a.js", "test/b.js")):
            payload = self.report_payload(
                index,
                [{"path": path, "status": "passed"}],
                selected_before_sharding=3,
            )
            payload["shardCount"] = 2
            self.write_json(f"test262-full-shard-{index}.json", payload)

        merged = merger.merge(self.root, expected_shard_indexes={0, 1})

        self.assertEqual(
            "IncompleteSelectionCoverage",
            merged["configurationFailures"][0]["kind"],
        )

    def test_full_minified_shard_space_counts_unique_paths_not_variant_cases(self) -> None:
        for index, path in enumerate(("test/a.js", "test/b.js")):
            payload = self.report_payload(
                index,
                [
                    {"path": path, "variant": "original", "status": "passed"},
                    {"path": path, "variant": "terser", "status": "passed"},
                ],
                selected_before_sharding=2,
                minifier="terser",
            )
            payload["shardCount"] = 2
            self.write_json(f"test262-full-shard-{index}.json", payload)

        merged = merger.merge(self.root, expected_shard_indexes={0, 1})

        self.assertEqual([], merged["configurationFailures"])
        self.assertEqual(4, merged["summary"]["total"])
        self.assertEqual(2, merged["summary"]["uniquePaths"])
        self.assertEqual(2, merged["pathSummary"]["total"])
        self.assertEqual(2, merged["variantSummary"]["original"]["total"])
        self.assertEqual(2, merged["variantSummary"]["terser"]["total"])

    def test_inconsistent_minifier_provenance_blocks_manifest_clearance(self) -> None:
        paths = ["test/a.js", "test/b.js"]
        manifest = self.write_text("failures.txt", "\n".join(paths) + "\n")
        for index, path in enumerate(paths):
            payload = self.report_payload(
                index,
                [
                    {"path": path, "variant": "original", "status": "passed"},
                    {"path": path, "variant": "terser", "status": "passed"},
                ],
                minifier="terser",
                minifier_version=f"5.3{index}.0",
            )
            self.write_json(f"test262-full-shard-{index}.json", payload)

        merged = merger.merge(self.root, expected_shard_indexes={0, 1})
        persisted = merger.merge_into_manifest(merged, manifest)

        self.assertTrue(
            any(
                failure["kind"] == "InconsistentShardConfiguration"
                and failure.get("field") == "minifierVersion"
                for failure in merged["configurationFailures"]
            )
        )
        self.assertEqual([], merged["executedPaths"])
        self.assertEqual(paths, persisted["manifestPaths"])

    def test_invalid_minifier_profile_and_options_fail_configuration(self) -> None:
        path = "test/language/bad-profile.js"
        payload = self.report_payload(
            0,
            [
                {"path": path, "variant": "original", "status": "passed"},
                {"path": path, "variant": "terser", "status": "passed"},
            ],
            minifier="terser",
        )
        payload["minifierProfile"] = "unsafe-profile"
        payload["minifierOptions"] = {
            **payload["minifierOptions"],  # type: ignore[arg-type]
            "compress": True,
        }
        self.write_json("test262-full-shard-0.json", payload)

        merged = merger.merge(self.root, expected_shard_indexes={0})

        messages = "\n".join(
            failure["message"] for failure in merged["configurationFailures"]
        )
        self.assertIn("minifierProfile=test262-safe-mangle-v2", messages)
        self.assertIn("exact test262-safe-mangle-v2 minifierOptions", messages)
        self.assertEqual([], merged["executedPaths"])

    def test_wrong_expected_variant_count_makes_report_incomplete(self) -> None:
        path = "test/language/wrong-expected-count.js"
        payload = self.report_payload(
            0,
            [
                {"path": path, "variant": "original", "status": "passed"},
                {"path": path, "variant": "terser", "status": "passed"},
            ],
            minifier="terser",
        )
        payload["expectedVariantCountPerPath"] = 1
        self.write_json("test262-full-shard-0.json", payload)

        merged = merger.merge(self.root, expected_shard_indexes={0})

        self.assertEqual([], merged["results"])
        self.assertEqual("MalformedReport", merged["incompleteShards"][0]["failureReason"])
        self.assertIn(
            "expectedVariantCountPerPath",
            merged["incompleteShards"][0]["failureDetail"],
        )

    def test_mismatched_variant_summary_makes_report_incomplete(self) -> None:
        path = "test/language/wrong-variant-summary.js"
        payload = self.report_payload(
            0,
            [
                {"path": path, "variant": "original", "status": "passed"},
                {"path": path, "variant": "terser", "status": "passed"},
            ],
            minifier="terser",
        )
        payload["variantSummary"]["terser"]["passed"] = 0  # type: ignore[index]
        self.write_json("test262-full-shard-0.json", payload)

        merged = merger.merge(self.root, expected_shard_indexes={0})

        self.assertEqual("MalformedReport", merged["incompleteShards"][0]["failureReason"])
        self.assertIn(
            "variantSummary.terser.passed",
            merged["incompleteShards"][0]["failureDetail"],
        )

    def test_absolute_terser_module_path_is_not_persisted(self) -> None:
        path = "test/language/no-secret-path.js"
        module_path = "C:/private/node_modules/terser/index.js"
        payload = self.report_payload(
            0,
            [
                {"path": path, "variant": "original", "status": "passed"},
                {
                    "path": path,
                    "variant": "terser",
                    "status": "passed",
                    "terserModule": module_path,
                },
            ],
            minifier="terser",
        )
        payload["terserModule"] = module_path
        self.write_json("test262-full-shard-0.json", payload)

        merged = merger.merge(self.root, expected_shard_indexes={0})

        self.assertNotIn("terserModule", merged["runConfiguration"])
        self.assertNotIn(module_path, json.dumps(merged, sort_keys=True))

    def test_biggest_report_includes_incomplete_and_multiple_crashes_up_to_limit(self) -> None:
        self.write_report(
            0,
            [
                {
                    "path": "test/language/a.js",
                    "status": "failed",
                    "stderr": (
                        "Unhandled exception. System.AlphaException: alpha\n"
                        "at Alpha in /repo/A.cs:line 1"
                    ),
                },
                {
                    "path": "test/built-ins/b.js",
                    "status": "failed",
                    "stderr": (
                        "Unhandled exception. System.BetaException: beta\n"
                        "at Beta in /repo/B.cs:line 2"
                    ),
                },
            ],
        )

        merged = merger.merge(
            self.root,
            expected_shard_indexes={0, 1},
            biggest_problem_limit=3,
        )

        self.assertEqual(
            ["IncompleteShards", "Crash", "Crash"],
            [problem["kind"] for problem in merged["biggestProblems"]],
        )
        markdown = merger.render_biggest_problems_markdown(merged)
        self.assertIn("incomplete shard", markdown)
        self.assertIn("System.AlphaException", markdown)
        self.assertIn("System.BetaException", markdown)

    def test_problem_and_biggest_limits_are_honoured(self) -> None:
        self.write_report(
            0,
            [
                {
                    "path": f"test/language/{index}.js",
                    "status": "failed",
                    "reason": f"different reason {index}",
                }
                for index in range(5)
            ],
        )

        merged = merger.merge(
            self.root,
            expected_shard_indexes={0},
            problem_limit=2,
            biggest_problem_limit=1,
        )

        self.assertEqual(5, merged["problemGroupCount"])
        self.assertEqual(2, len(merged["problemGroups"]))
        self.assertEqual(1, len(merged["biggestProblems"]))

    def test_common_report_always_explains_incomplete_shards_beyond_limit(self) -> None:
        self.write_report(
            0,
            [
                {
                    "path": f"test/language/{index}.js",
                    "status": "failed",
                    "reason": "frequent failure",
                }
                for index in range(5)
            ],
        )
        self.write_status(
            1,
            137,
            reason="RunnerTerminated",
            detail="Hosted runner disappeared.",
        )

        merged = merger.merge(
            self.root,
            expected_shard_indexes={0, 1},
            problem_limit=1,
        )
        markdown = merger.render_issue_markdown(merged)

        self.assertEqual(1, len(merged["problemGroups"]))
        self.assertIn("### Incomplete shards", markdown)
        self.assertIn("RunnerTerminated", markdown)
        self.assertIn("Hosted runner disappeared.", markdown)

    def test_timeouts_sort_smallest_source_first_and_missing_size_last(self) -> None:
        self.write_report(
            0,
            [
                {
                    "path": "test/language/medium.js",
                    "status": "timedOut",
                    "sourceSizeBytes": 500,
                    "features": ["async-functions"],
                },
                {
                    "path": "test/language/small.js",
                    "status": "timedOut",
                    "sourceSizeBytes": 100,
                    "features": ["async-functions", "Promise"],
                },
                {
                    "path": "test/language/unknown.js",
                    "status": "timedOut",
                    "features": ["Promise"],
                },
            ],
        )

        merged = merger.merge(
            self.root,
            expected_shard_indexes={0},
            timeout_limit=2,
        )

        self.assertEqual(3, merged["timeoutCount"])
        self.assertEqual(
            ["test/language/small.js", "test/language/medium.js"],
            [timeout["path"] for timeout in merged["timeouts"]],
        )
        self.assertEqual(
            "async-functions",
            merged["timeoutFeatureGroups"][0]["feature"],
        )
        markdown = merger.render_timeout_issue_markdown(merged)
        self.assertIn("test/language/small.js", markdown)
        self.assertIn("async-functions", markdown)
        self.assertLess(
            markdown.index("test/language/small.js"),
            markdown.index("test/language/medium.js"),
        )
        # The ranked list is limited to two, while feature-cluster samples are
        # deliberately drawn from the complete timeout set.
        self.assertIn("test/language/unknown.js", markdown)

    def test_minifier_only_failures_isolate_minification_regressions(self) -> None:
        self.write_report(
            0,
            [
                # Original passes, minified body fails: a Terser-only case.
                {
                    "path": "test/language/mangled-large.js",
                    "variant": "original",
                    "status": "passed",
                    "sourceSizeBytes": 900,
                },
                {
                    "path": "test/language/mangled-large.js",
                    "variant": "terser",
                    "status": "failed",
                    "reason": "expected 1 but got 2",
                    "sourceSizeBytes": 900,
                    "minifiedSourceSizeBytes": 300,
                    "minificationRatio": 0.333,
                    "features": ["let"],
                },
                # Smaller minified body, so it must be reduced first.
                {
                    "path": "test/built-ins/mangled-small.js",
                    "variant": "original",
                    "status": "passed",
                    "sourceSizeBytes": 120,
                },
                {
                    "path": "test/built-ins/mangled-small.js",
                    "variant": "terser",
                    "status": "timedOut",
                    "sourceSizeBytes": 120,
                    "minifiedSourceSizeBytes": 80,
                },
                # Failing as original source too, so not a Terser-only case.
                {
                    "path": "test/language/broken-both-ways.js",
                    "variant": "original",
                    "status": "failed",
                    "reason": "expected 1 but got 2",
                },
                {
                    "path": "test/language/broken-both-ways.js",
                    "variant": "terser",
                    "status": "failed",
                    "reason": "expected 1 but got 2",
                },
                # Terser itself could not transform the body: reported, but
                # counted apart from genuine engine divergences.
                {
                    "path": "test/built-ins/minifier-broke.js",
                    "variant": "original",
                    "status": "passed",
                },
                {
                    "path": "test/built-ins/minifier-broke.js",
                    "variant": "terser",
                    "status": "failed",
                    "infrastructure": True,
                    "failureType": "MinifierError",
                    "reason": "Terser rejected the input",
                },
            ],
            minifier="terser",
        )

        merged = merger.merge(
            self.root, expected_shard_indexes={0}, minifier_only_limit=2
        )

        self.assertEqual(3, merged["minifierOnlyFailureCount"])
        self.assertEqual(2, merged["minifierOnlyDivergenceCount"])
        self.assertEqual(1, merged["minifierOnlyMinifierFailureCount"])
        self.assertEqual(3, merged["minifierOnlyPathCount"])
        self.assertEqual(
            [
                "test/built-ins/mangled-small.js",
                "test/built-ins/minifier-broke.js",
                "test/language/mangled-large.js",
            ],
            merged["minifierOnlyPaths"],
        )
        self.assertNotIn(
            "test/language/broken-both-ways.js", merged["minifierOnlyPaths"]
        )
        # The ranked list honours its limit and leads with the cheapest
        # reduction, while the group list keeps every distinct symptom.
        self.assertEqual(
            [
                "test/built-ins/mangled-small.js",
                "test/language/mangled-large.js",
            ],
            [case["path"] for case in merged["minifierOnlyFailures"]],
        )
        self.assertEqual(
            ["MinifierFailure", "Timeout", "Failure"],
            [group["kind"] for group in merged["minifierOnlyGroups"]],
        )

        markdown = merger.render_minifier_only_issue_markdown(merged)
        self.assertIn("3 Terser-only failure(s)", markdown)
        self.assertIn("Minifier infrastructure failures: 1", markdown)
        self.assertIn("test/built-ins/minifier-broke.js", markdown)
        self.assertNotIn("test/language/broken-both-ways.js", markdown)
        self.assertLess(
            markdown.index("test/built-ins/mangled-small.js"),
            markdown.index("test/language/mangled-large.js"),
        )

    def test_merger_and_runner_agree_on_every_minifier_profile(self) -> None:
        """The merger REJECTS a report whose options it does not recognise byte for byte.

        So the two tables are one contract in two files, and a profile changed on the
        runner side without the merger following would turn every shard of the next run
        into a configuration failure.
        """
        self.assertEqual(
            set(run_test262.MINIFIER_VARIANTS), set(merger._MINIFIED_VARIANTS)
        )
        self.assertEqual(
            {
                run_test262.TERSER_VARIANT: run_test262.TERSER_PROFILE,
                run_test262.CLOSURE_VARIANT: run_test262.CLOSURE_PROFILE,
            },
            merger._MINIFIER_PROFILES,
        )
        self.assertEqual(run_test262.MINIFIER_OPTIONS, merger._MINIFIER_OPTIONS)

    def test_closure_run_is_merged_under_its_own_variant_and_profile(self) -> None:
        self.write_report(
            0,
            [
                {
                    "path": "test/language/compiled.js",
                    "variant": "original",
                    "status": "passed",
                    "sourceSizeBytes": 400,
                },
                {
                    "path": "test/language/compiled.js",
                    "variant": "closure",
                    "status": "failed",
                    "reason": "expected 'fn' but got 'a'",
                    "sourceSizeBytes": 400,
                    "minifiedSourceSizeBytes": 120,
                },
                # ADVANCED renames properties, so a case the transformation moved out of
                # being the test is the common outcome, not an engine defect.
                {
                    "path": "test/language/renamed-property.js",
                    "variant": "original",
                    "status": "passed",
                },
                {
                    "path": "test/language/renamed-property.js",
                    "variant": "closure",
                    "status": "skipped",
                    "notApplicable": True,
                    "skipKind": "minifier-changed-semantics",
                    "reason": "node passes this test and fails its minified body",
                },
            ],
            minifier="closure",
        )

        merged = merger.merge(self.root, expected_shard_indexes={0})

        self.assertEqual([], merged["configurationFailures"])
        self.assertEqual(["original", "closure"], merged["observedVariants"])
        self.assertEqual("closure", merged["minifiedVariant"])
        self.assertEqual(
            "test262-closure-advanced-v1",
            merged["runConfiguration"]["minifierProfile"],
        )
        self.assertEqual(
            "ADVANCED_OPTIMIZATIONS",
            merged["runConfiguration"]["minifierOptions"]["compilationLevel"],
        )
        self.assertEqual(
            2026, merged["runConfiguration"]["minifierOptions"]["ecmaScriptYear"]
        )
        self.assertEqual(1, merged["minifierOnlyFailureCount"])
        self.assertEqual(
            ["test/language/compiled.js"], merged["minifierOnlyPaths"]
        )
        self.assertEqual(
            {"minifier-changed-semantics": 1},
            merged["summary"]["notApplicableByKind"],
        )
        markdown = merger.render_minifier_only_issue_markdown(merged)
        self.assertIn("Closure-only failure(s)", markdown)
        self.assertIn("### Normalized Closure-only failure groups", markdown)
        self.assertIn("test262-closure-advanced-v1", markdown)
        self.assertIn("--minifier closure --closure-module", markdown)
        self.assertIn(
            "Attributed to the minifier "
            "(minification changed what the test measures): 1",
            markdown,
        )

    def test_closure_run_recording_the_terser_profile_is_a_configuration_failure(
        self,
    ) -> None:
        self.write_report(
            0,
            [
                {
                    "path": "test/language/compiled.js",
                    "variant": "original",
                    "status": "passed",
                },
                {
                    "path": "test/language/compiled.js",
                    "variant": "closure",
                    "status": "passed",
                },
            ],
            minifier="closure",
            minifier_profile="test262-safe-mangle-v2",
        )

        merged = merger.merge(self.root, expected_shard_indexes={0})

        kinds = {failure["kind"] for failure in merged["configurationFailures"]}
        self.assertIn("InvalidMinifierConfiguration", kinds)
        messages = " ".join(
            failure["message"] for failure in merged["configurationFailures"]
        )
        self.assertIn("test262-closure-advanced-v1", messages)

    def test_a_report_cannot_declare_two_minified_variants(self) -> None:
        self.write_report(
            0,
            [
                {
                    "path": "test/language/two.js",
                    "variant": "original",
                    "status": "passed",
                },
                {
                    "path": "test/language/two.js",
                    "variant": "terser",
                    "status": "passed",
                },
                {
                    "path": "test/language/two.js",
                    "variant": "closure",
                    "status": "passed",
                },
            ],
            minifier="terser",
            variants=["original", "terser", "closure"],
        )

        merged = merger.merge(self.root, expected_shard_indexes={0})

        self.assertEqual([0], [item["shardIndex"] for item in merged["incompleteShards"]])
        self.assertEqual(
            ["MalformedReport"],
            [item["failureReason"] for item in merged["incompleteShards"]],
        )
        self.assertIn(
            "report variants must be ['original'] or ['original', <minifier>]",
            merged["incompleteShards"][0]["failureDetail"],
        )

    def test_original_only_run_reports_no_minifier_only_failures(self) -> None:
        self.write_report(
            0,
            [
                {
                    "path": "test/language/fails.js",
                    "variant": "original",
                    "status": "failed",
                    "reason": "expected 1 but got 2",
                }
            ],
            minifier="none",
        )

        merged = merger.merge(self.root, expected_shard_indexes={0})

        self.assertEqual(1, merged["summary"]["failed"])
        self.assertEqual(0, merged["minifierOnlyFailureCount"])
        self.assertEqual([], merged["minifierOnlyGroups"])
        self.assertIn("- None", merger.render_minifier_only_issue_markdown(merged))

    def test_minifier_only_cases_without_sizes_rank_last(self) -> None:
        self.write_report(
            0,
            [
                {
                    "path": "test/language/unknown-size.js",
                    "variant": "original",
                    "status": "passed",
                },
                {
                    "path": "test/language/unknown-size.js",
                    "variant": "terser",
                    "status": "failed",
                    "reason": "mangled binding resolved to the wrong value",
                },
                {
                    "path": "test/language/known-size.js",
                    "variant": "original",
                    "status": "passed",
                    "sourceSizeBytes": 4096,
                },
                {
                    "path": "test/language/known-size.js",
                    "variant": "terser",
                    "status": "failed",
                    "reason": "mangled binding resolved to the wrong value",
                    "sourceSizeBytes": 4096,
                    "minifiedSourceSizeBytes": 2048,
                },
            ],
            minifier="terser",
        )

        merged = merger.merge(self.root, expected_shard_indexes={0})

        self.assertEqual(
            ["test/language/known-size.js", "test/language/unknown-size.js"],
            [case["path"] for case in merged["minifierOnlyFailures"]],
        )
        markdown = merger.render_minifier_only_issue_markdown(merged)
        self.assertIn("2.0 KiB minified of 4.0 KiB original", markdown)
        self.assertIn("unknown size", markdown)
        # Both cases share one normalized symptom, so they are one group.
        self.assertEqual(1, len(merged["minifierOnlyGroups"]))
        self.assertEqual(2, merged["minifierOnlyGroups"][0]["count"])

    def test_not_applicable_minified_case_is_not_a_minifier_only_failure(self) -> None:
        path = "test/built-ins/Function/prototype/toString/source.js"
        self.write_report(
            0,
            [
                {"path": path, "variant": "original", "status": "passed"},
                {
                    "path": path,
                    "variant": "terser",
                    "status": "skipped",
                    "notApplicable": True,
                    "skipKind": "minifier-not-applicable",
                    "reason": "source-text-sensitive test",
                },
            ],
            minifier="terser",
        )

        merged = merger.merge(self.root, expected_shard_indexes={0})

        self.assertEqual(0, merged["minifierOnlyFailureCount"])
        self.assertEqual([], merged["minifierOnlyFailures"])

    def test_cli_writes_reports_manifest_and_failure_outputs(self) -> None:
        self.write_report(
            0,
            [
                {
                    "path": "test/language/fail.js",
                    "status": "failed",
                    "reason": "bad result",
                },
                {
                    "path": "test/language/timeout.js",
                    "status": "timedOut",
                    "sourceSizeBytes": 42,
                },
            ],
        )
        manifest = self.write_text(
            "manifest.txt",
            "# Header\n\ntest/old.js\n",
        )
        merged_json = self.root / "out" / "merged.json"
        common_md = self.root / "out" / "common.md"
        biggest_md = self.root / "out" / "biggest.md"
        timeout_md = self.root / "out" / "timeout.md"
        minifier_only_md = self.root / "out" / "minifier-only.md"
        github_output = self.root / "out" / "github-output.txt"

        stdout = StringIO()
        with redirect_stdout(stdout):
            exit_code = merger.main(
                [
                    "--shard-dir",
                    str(self.root),
                    "--phase",
                    "full",
                    "--expected-shards",
                    "0,1",
                    "--merged-json",
                    str(merged_json),
                    "--merge-into",
                    str(manifest),
                    "--issue-md",
                    str(common_md),
                    "--biggest-issue-md",
                    str(biggest_md),
                    "--timeout-issue-md",
                    str(timeout_md),
                    "--minifier-only-issue-md",
                    str(minifier_only_md),
                    "--problem-limit",
                    "1",
                    "--biggest-problem-limit",
                    "2",
                    "--timeout-limit",
                    "1",
                    "--run-url",
                    "https://example.invalid/run/7",
                    "--broiler-commit",
                    "deadbeef",
                    "--artifact-name",
                    "test262-full-merged",
                    "--github-output",
                    str(github_output),
                ]
            )

        self.assertEqual(0, exit_code)
        payload = json.loads(merged_json.read_text(encoding="utf-8"))
        self.assertEqual("deadbeef", payload["broilerCommit"])
        self.assertEqual("https://example.invalid/run/7", payload["runUrl"])
        self.assertEqual("test262-full-merged", payload["artifactName"])
        self.assertEqual(
            ["test/language/fail.js", "test/language/timeout.js", "test/old.js"],
            payload["manifestPaths"],
        )
        self.assertIn(
            "test262-full-merged",
            common_md.read_text(encoding="utf-8"),
        )
        self.assertIn(
            "https://example.invalid/run/7",
            biggest_md.read_text(encoding="utf-8"),
        )
        self.assertIn(
            "test/language/timeout.js",
            timeout_md.read_text(encoding="utf-8"),
        )
        # An original-only run cannot produce Terser-only evidence, so the
        # dedicated report is still written and still says so.
        self.assertIn("- None", minifier_only_md.read_text(encoding="utf-8"))
        outputs = github_output.read_text(encoding="utf-8")
        self.assertIn("failed_count=2", outputs)
        self.assertIn("create_issue=true", outputs)
        self.assertIn("create_biggest_issue=true", outputs)
        self.assertIn("create_timeout_issue=true", outputs)
        self.assertIn("create_minifier_only_issue=false", outputs)
        self.assertIn("incomplete_shard_indexes=1", outputs)
        self.assertIn(
            'incomplete_shard_matrix=[{"shard-index":1}]',
            outputs,
        )
        self.assertIn("has_incomplete_shards=true", outputs)
        self.assertIn("suite_passed=false", outputs)

    def test_cli_reuses_canonical_merged_artifact_for_manifest_update(self) -> None:
        self.write_report(
            0,
            [{"path": "test/new-failure.js", "status": "failed"}],
        )
        canonical = merger.merge(
            self.root,
            expected_shard_indexes={0},
            broiler_commit="abc123",
            run_url="https://example.invalid/run/9",
        )
        canonical_path = self.root / "canonical.json"
        canonical_path.write_text(
            json.dumps(canonical),
            encoding="utf-8",
        )
        manifest = self.write_text(
            "failures.txt",
            "test/old-out-of-scope.js\n",
        )

        with redirect_stdout(StringIO()):
            exit_code = merger.main(
                [
                    "--merged-input",
                    str(canonical_path),
                    "--merge-into",
                    str(manifest),
                ]
            )

        self.assertEqual(0, exit_code)
        self.assertEqual(
            ["test/new-failure.js", "test/old-out-of-scope.js"],
            manifest.read_text(encoding="utf-8").splitlines(),
        )

    def test_green_suite_outputs_disable_all_issue_gates(self) -> None:
        self.write_report(
            0,
            [{"path": "test/language/pass.js", "status": "passed"}],
        )
        github_output = self.root / "github-output.txt"

        with redirect_stdout(StringIO()):
            merger.main(
                [
                    "--shard-dir",
                    str(self.root),
                    "--expected-shards",
                    "0",
                    "--github-output",
                    str(github_output),
                ]
            )

        outputs = github_output.read_text(encoding="utf-8")
        self.assertIn("create_issue=false", outputs)
        self.assertIn("create_biggest_issue=false", outputs)
        self.assertIn("create_timeout_issue=false", outputs)
        self.assertIn("create_minifier_only_issue=false", outputs)
        self.assertIn("has_incomplete_shards=false", outputs)
        self.assertIn("suite_passed=true", outputs)

    def test_variant_case_and_unique_path_counts_are_github_outputs(self) -> None:
        path = "test/language/source-sensitive-output.js"
        self.write_report(
            0,
            [
                {"path": path, "variant": "original", "status": "passed"},
                {
                    "path": path,
                    "variant": "terser",
                    "status": "skipped",
                    "notApplicable": True,
                    "skipKind": "minifier-not-applicable",
                    "reason": "source-text-sensitive test",
                },
            ],
            minifier="terser",
        )
        github_output = self.root / "github-output.txt"

        merged = merger.merge(self.root, expected_shard_indexes={0})
        merger._write_github_outputs(github_output, merged)

        outputs = github_output.read_text(encoding="utf-8")
        self.assertIn("total_count=2", outputs)
        self.assertIn("unique_path_count=1", outputs)
        self.assertIn("original_case_count=1", outputs)
        self.assertIn("minified_case_count=1", outputs)
        self.assertIn("not_applicable_count=1", outputs)
        self.assertIn("suite_passed=true", outputs)

    def test_timeout_only_run_uses_timeout_issue_not_biggest_issue(self) -> None:
        self.write_report(
            0,
            [
                {
                    "path": "test/language/hang.js",
                    "status": "timedOut",
                    "sourceSizeBytes": 100,
                }
            ],
        )
        github_output = self.root / "github-output.txt"

        with redirect_stdout(StringIO()):
            merger.main(
                [
                    "--shard-dir",
                    str(self.root),
                    "--expected-shards",
                    "0",
                    "--github-output",
                    str(github_output),
                ]
            )

        outputs = github_output.read_text(encoding="utf-8")
        self.assertIn("create_issue=true", outputs)
        self.assertIn("create_biggest_issue=false", outputs)
        self.assertIn("create_timeout_issue=true", outputs)
        self.assertIn("suite_passed=false", outputs)

    def test_minifier_only_run_opens_its_own_issue_through_the_cli(self) -> None:
        path = "test/language/mangled.js"
        self.write_report(
            0,
            [
                {
                    "path": path,
                    "variant": "original",
                    "status": "passed",
                    "sourceSizeBytes": 400,
                },
                {
                    "path": path,
                    "variant": "terser",
                    "status": "failed",
                    "reason": "mangled binding resolved to the wrong value",
                    "sourceSizeBytes": 400,
                    "minifiedSourceSizeBytes": 150,
                },
            ],
            minifier="terser",
        )
        minifier_only_md = self.root / "out" / "minifier-only.md"
        github_output = self.root / "out" / "github-output.txt"

        with redirect_stdout(StringIO()):
            exit_code = merger.main(
                [
                    "--shard-dir",
                    str(self.root),
                    "--expected-shards",
                    "0",
                    "--minifier-only-issue-md",
                    str(minifier_only_md),
                    "--minifier-only-limit",
                    "1",
                    "--github-output",
                    str(github_output),
                ]
            )

        self.assertEqual(0, exit_code)
        body = minifier_only_md.read_text(encoding="utf-8")
        self.assertIn("1 Terser-only failure(s)", body)
        self.assertIn("mangled binding resolved to the wrong value", body)
        self.assertIn(path, body)
        outputs = github_output.read_text(encoding="utf-8")
        self.assertIn("create_minifier_only_issue=true", outputs)
        self.assertIn("minifier_only_failure_count=1", outputs)
        self.assertIn("minifier_only_divergence_count=1", outputs)
        self.assertIn("minifier_only_infrastructure_failure_count=0", outputs)
        self.assertIn("minifier_only_path_count=1", outputs)
        self.assertIn("suite_passed=false", outputs)

    def test_cli_rejects_non_positive_limits(self) -> None:
        for option in (
            "--problem-limit",
            "--biggest-problem-limit",
            "--timeout-limit",
            "--minifier-only-limit",
        ):
            with self.subTest(option=option), redirect_stderr(StringIO()):
                with self.assertRaises(SystemExit) as raised:
                    merger.main(
                        [
                            "--shard-dir",
                            str(self.root),
                            option,
                            "0",
                        ]
                    )
                self.assertEqual(2, raised.exception.code)

    def test_cli_rejects_invalid_expected_shard_csv(self) -> None:
        for value in ("0,nope,2", "-1", ""):
            with self.subTest(value=value), redirect_stderr(StringIO()):
                with self.assertRaises(SystemExit) as raised:
                    merger.main(
                        [
                            "--shard-dir",
                            str(self.root),
                            "--expected-shards",
                            value,
                        ]
                    )
                self.assertEqual(2, raised.exception.code)


if __name__ == "__main__":
    unittest.main()
