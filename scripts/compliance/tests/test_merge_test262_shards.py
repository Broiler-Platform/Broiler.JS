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
    ) -> dict[str, object]:
        counts = {
            "passed": sum(result["status"] == "passed" for result in results),
            "failed": sum(result["status"] == "failed" for result in results),
            "skipped": sum(result["status"] == "skipped" for result in results),
            "timedOut": sum(
                result["status"] == "timedOut" for result in results
            ),
        }
        payload: dict[str, object] = {
            "suiteRef": suite_ref,
            "shardIndex": index,
            "shardCount": 8,
            "expandedPaths": [result["path"] for result in results],
            "executed": (
                counts["passed"] + counts["failed"] + counts["timedOut"]
            ),
            **counts,
            "results": results,
        }
        if selected_before_sharding is not None:
            payload["selectedCountBeforeSharding"] = selected_before_sharding
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
    ) -> Path:
        suffix = "-retry" if retry else ""
        return self.write_json(
            f"test262-{phase}-shard-{index}{suffix}.json",
            self.report_payload(
                index,
                results,
                suite_ref=suite_ref,
                selected_before_sharding=selected_before_sharding,
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
            },
            merged["summary"],
        )

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
            ["test/a.js", "test/b.js", "test/z.js"],
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
        outputs = github_output.read_text(encoding="utf-8")
        self.assertIn("failed_count=2", outputs)
        self.assertIn("create_issue=true", outputs)
        self.assertIn("create_biggest_issue=true", outputs)
        self.assertIn("create_timeout_issue=true", outputs)
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
        self.assertIn("has_incomplete_shards=false", outputs)
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

    def test_cli_rejects_non_positive_limits(self) -> None:
        for option in (
            "--problem-limit",
            "--biggest-problem-limit",
            "--timeout-limit",
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
