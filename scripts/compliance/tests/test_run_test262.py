from __future__ import annotations

import argparse
import os
import tempfile
from pathlib import Path
import subprocess
import sys
import unittest
from contextlib import redirect_stderr, redirect_stdout
from io import StringIO
from unittest import mock

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import run_test262

TEST_SUITE_REF = "test-suite-ref"
TEST_ENGINE_PATH = "BroilerJS.test.dll"


class FakeMinifier:
    """In-memory minifier double; no Node.js installation is required."""

    name = run_test262.TERSER_VARIANT
    version = "5.test"
    profile = run_test262.TERSER_PROFILE

    def __init__(
        self,
        code: str = "MINIFIED_BODY();",
        error: Exception | None = None,
        unsupported_syntax: str | None = None,
    ) -> None:
        self.code = code
        self.error = error
        self.unsupported_syntax = unsupported_syntax
        self.calls: list[tuple[str, str]] = []

    def minify(self, source: str, path: str) -> dict[str, object]:
        self.calls.append((source, path))
        if self.error is not None:
            raise self.error
        result: dict[str, object] = {
            "code": self.code,
            "originalBytes": len(source.encode("utf-8")),
            "minifiedBytes": len(self.code.encode("utf-8")),
        }
        if self.unsupported_syntax is not None:
            result["unsupportedSyntax"] = self.unsupported_syntax
        return result


class FakeReferenceEngine:
    """Reference-engine double; no Node.js installation is required.

    Answers are keyed by the BODY the program was assembled from, so a test states what the
    second engine thinks of the original and of the minified source separately.
    """

    name = "fake-engine"
    version = "1.test"

    def __init__(
        self,
        verdicts: dict[str, str] | None = None,
        unparsable: tuple[str, ...] = (),
        error: Exception | None = None,
    ) -> None:
        self.verdicts = verdicts or {}
        self.unparsable = unparsable
        self.error = error
        self.programs: list[str] = []
        self.parse_checks: list[str] = []

    def _verdict_for(self, program: str) -> str:
        for marker, verdict in self.verdicts.items():
            if marker in program:
                return verdict
        return "passed"

    def run(self, program: str, is_async: bool) -> str:
        if self.error is not None:
            raise self.error
        self.programs.append(program)
        return self._verdict_for(program)

    def parses(self, program: str) -> bool:
        self.parse_checks.append(program)
        return not any(marker in program for marker in self.unparsable)


class RecordingPopen:
    """Successful Broiler process double that records each assembled script."""

    def __init__(self) -> None:
        self.scripts: list[str] = []

    def __call__(self, args: list[str], **kwargs: object) -> object:
        self.scripts.append(Path(args[-1]).read_text(encoding="utf-8"))

        class SuccessfulProcess:
            pid = 1234
            returncode = 0

            @staticmethod
            def communicate(*, timeout: float) -> tuple[str, str]:
                return "", ""

        return SuccessfulProcess()


def passing_run_test(
    repo: object,
    dll: str,
    path: str,
    cache: dict[str, str],
    timeout: float,
    memory_limit: int,
    include_negative: bool = False,
    *,
    variant: str | None = None,
    **kwargs: object,
) -> dict[str, object]:
    result: dict[str, object] = {"path": path, "status": "passed"}
    if variant is not None:
        result["variant"] = variant
    return result


class RunTest262Tests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp_directory.cleanup)
        self.suite_root = Path(self.temp_directory.name)
        (self.suite_root / "harness").mkdir(parents=True)
        (self.suite_root / "test" / "language").mkdir(parents=True)
        (self.suite_root / "harness" / "assert.js").write_text(
            "// assert harness\n",
            encoding="utf-8",
        )
        (self.suite_root / "harness" / "sta.js").write_text(
            "// sta harness\n",
            encoding="utf-8",
        )

    def write_test(self, relative_path: str, source: str) -> str:
        path = self.suite_root / relative_path
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(source, encoding="utf-8")
        return relative_path

    def test_run_test_prepends_use_strict_for_only_strict_files(self) -> None:
        path = self.write_test(
            "test/language/only-strict.js",
            '/*---\nflags: [onlyStrict]\n---*/\nthis;\n',
        )
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))
        captured_script = {}

        class FakeProcess:
            def __init__(self, args: list[str]):
                self.args = args
                self.pid = 1234
                self.returncode = 0

            def communicate(self, *, timeout: float):
                captured_script["timeout"] = timeout
                script_path = Path(self.args[-1])
                captured_script["contents"] = script_path.read_text(encoding="utf-8")
                return "", ""

        def fake_run(
            args: list[str],
            stdout: int,
            stderr: int,
            text: bool,
            start_new_session: bool,
            **kwargs,
        ):
            self.assertTrue(text)
            self.assertNotIn("preexec_fn", kwargs)
            self.assertEqual(os.name == "posix", start_new_session)
            return FakeProcess(args)

        with mock.patch.object(run_test262.subprocess, "Popen", side_effect=fake_run):
            result = run_test262.run_test(repo, TEST_ENGINE_PATH, path, {}, 12.5, 256)

        self.assertEqual({"path": path, "status": "passed"}, result)
        self.assertEqual(12.5, captured_script["timeout"])
        lines = [line for line in captured_script["contents"].splitlines() if line]
        self.assertEqual('"use strict";', lines[0])
        self.assertEqual("// assert harness", lines[1])
        self.assertEqual("// sta harness", lines[2])
        self.assertIn("this;", lines)

    def test_run_test_times_out_and_kills_process_group(self) -> None:
        path = self.write_test("test/language/timeout.js", "for (;;) {}\n")
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))
        process = mock.Mock()
        process.pid = 4321
        process.returncode = -9
        process.communicate.side_effect = [
            subprocess.TimeoutExpired(cmd=["dotnet"], timeout=30.0),
            ("partial stdout", "partial stderr"),
        ]

        with (
            mock.patch.object(run_test262.subprocess, "Popen", return_value=process),
            mock.patch.object(run_test262, "terminate_process_tree") as terminate,
        ):
            result = run_test262.run_test(repo, TEST_ENGINE_PATH, path, {}, 30.0, 0)

        self.assertEqual("timedOut", result["status"])
        self.assertEqual("timed out after 30s", result["reason"])
        self.assertEqual("partial stdout", result["stdout"])
        self.assertEqual("partial stderr", result["stderr"])
        terminate.assert_called_once_with(process)

    def test_create_process_limit_setup_applies_posix_limits(self) -> None:
        fake_resource = mock.Mock()
        fake_resource.RLIMIT_CORE = 1
        fake_resource.RLIMIT_CPU = 2
        fake_resource.RLIMIT_AS = 3
        fake_resource.RLIMIT_DATA = 4
        fake_resource.setrlimit = mock.Mock()

        with (
            mock.patch.object(run_test262, "resource", fake_resource),
            mock.patch.object(run_test262.os, "name", "posix"),
        ):
            limit_setup = run_test262.create_process_limit_setup(30.0, 256)
            self.assertIsNotNone(limit_setup)
            limit_setup()
        self.assertEqual(
            [
                mock.call(fake_resource.RLIMIT_CORE, (0, 0)),
                mock.call(fake_resource.RLIMIT_CPU, (30, 35)),
                mock.call(fake_resource.RLIMIT_AS, (268435456, 268435456)),
                mock.call(fake_resource.RLIMIT_DATA, (268435456, 268435456)),
            ],
            fake_resource.setrlimit.call_args_list,
        )

    def test_internal_posix_launcher_applies_limits_before_exec(self) -> None:
        limit_setup = mock.Mock()
        with (
            mock.patch.object(
                run_test262,
                "create_process_limit_setup",
                return_value=limit_setup,
            ) as create_limit_setup,
            mock.patch.object(run_test262.os, "execvp") as execvp,
        ):
            exit_code = run_test262.exec_with_test_process_limits(
                [
                    "12.5",
                    "256",
                    "--",
                    "dotnet",
                    "BroilerJS.dll",
                    "--script-host",
                    "test.js",
                ]
            )

        self.assertEqual(127, exit_code)
        create_limit_setup.assert_called_once_with(12.5, 256)
        limit_setup.assert_called_once_with()
        execvp.assert_called_once_with(
            "dotnet",
            ["dotnet", "BroilerJS.dll", "--script-host", "test.js"],
        )

    def test_posix_test_command_uses_the_single_threaded_limit_launcher(self) -> None:
        fake_os = mock.Mock()
        fake_os.name = "posix"
        with (
            mock.patch.object(run_test262, "os", fake_os),
            mock.patch.object(run_test262, "resource", object()),
        ):
            command = run_test262.build_test_process_command(
                "BroilerJS.dll",
                "test.js",
                12.5,
                256,
            )

        self.assertEqual(run_test262.sys.executable, command[0])
        self.assertEqual(run_test262.INTERNAL_EXEC_WITH_LIMITS, command[2])
        self.assertEqual(
            [
                "12.5",
                "256",
                "--",
                "dotnet",
                "BroilerJS.dll",
                "--script-host",
                "test.js",
            ],
            command[3:],
        )

    def test_list_paths_uses_local_suite_root(self) -> None:
        file_path = self.write_test("test/language/example.js", "1 + 1;\n")
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))

        self.assertEqual([file_path], repo.list_paths(prefix="test/", suffix=".js"))

    def test_list_paths_can_exclude_test262_fixtures(self) -> None:
        file_path = self.write_test("test/language/example.js", "1 + 1;\n")
        self.write_test("test/language/example_FIXTURE.js", "0++;\n")
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))

        self.assertEqual(
            [file_path],
            repo.list_paths(prefix="test/", suffix=".js", include_fixtures=False),
        )

    def test_select_paths_all_script_host_verifiable_filters_blocked_tests_and_shards(self) -> None:
        first_path = self.write_test("test/language/a.js", "1 + 1;\n")
        self.write_test(
            "test/language/b-negative.js",
            "/*---\nnegative:\n  phase: runtime\n  type: ReferenceError\n---*/\nmissing;\n",
        )
        self.write_test(
            "test/language/c-module.js",
            "/*---\nflags: [module]\n---*/\nexport {};\n",
        )
        self.write_test(
            "test/language/c2-module-block.js",
            "/*---\nflags:\n  - module\n---*/\nexport {};\n",
        )
        # A feature-tagged test is no longer excluded (feature metadata never
        # filters a test out), so it is script-host-verifiable like any other.
        temporal_path = self.write_test(
            "test/language/c3-temporal.js",
            "/*---\nfeatures: [Temporal]\n---*/\nTemporal.Duration();\n",
        )
        self.write_test("test/language/host-harness.js", "$262.createRealm();\n")
        async_path = self.write_test(
            "test/language/d-async.js",
            "/*---\nflags: [async]\n---*/\n$DONE();\n",
        )
        self.write_test(
            "test/language/instn-resolve-empty-export_FIXTURE.js",
            "0++;\n",
        )
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))

        selected_paths, selection = run_test262.select_paths(
            repo,
            [],
            all_script_host_verifiable=True,
            shard_count=2,
            shard_index=1,
        )

        # Verifiable: a.js, c3-temporal.js, d-async.js (module/negative/host-harness
        # blocked, fixture excluded). Stable FNV-1a assignment selects shard 1.
        verifiable = sorted([first_path, temporal_path, async_path])
        self.assertEqual(
            [
                path
                for path in verifiable
                if run_test262.shard_index_for_path(path, 2) == 1
            ],
            selected_paths,
        )
        self.assertEqual("all-script-host-verifiable", selection["selectionMode"])
        self.assertEqual(7, selection["candidateCount"])
        self.assertEqual(3, selection["selectedCountBeforeSharding"])
        self.assertEqual(2, selection["shardCount"])
        self.assertEqual(1, selection["shardIndex"])

    def test_select_paths_includes_feature_tests(self) -> None:
        # Feature metadata (e.g. iterator-helpers) never excludes a test.
        first_path = self.write_test("test/language/a.js", "1 + 1;\n")
        feature_path = self.write_test(
            "test/built-ins/Iterator/from/name.js",
            "/*---\nfeatures: [iterator-helpers]\n---*/\nIterator.from([]);\n",
        )
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))

        selected_paths, selection = run_test262.select_paths(
            repo,
            [],
            all_script_host_verifiable=True,
            shard_count=1,
            shard_index=0,
        )

        self.assertEqual(sorted([first_path, feature_path]), selected_paths)
        self.assertEqual(2, selection["candidateCount"])
        self.assertEqual(2, selection["selectedCountBeforeSharding"])

    def test_subset_filters_support_semicolon_prefix_and_glob_patterns(self) -> None:
        language_a = self.write_test("test/language/a.js", "1;\n")
        language_b = self.write_test("test/language/nested/b.js", "2;\n")
        array_test = self.write_test("test/built-ins/Array/c.js", "3;\n")
        self.write_test("test/built-ins/Array/nested/not-selected.js", "3;\n")
        self.write_test("test/built-ins/String/d.js", "4;\n")
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))
        patterns = run_test262.parse_semicolon_patterns(
            " test\\language ; test/built-ins/Array/*.js ; test/language ",
            normalize_paths=True,
        )

        selected, selection = run_test262.select_paths(
            repo,
            ["test"],
            all_script_host_verifiable=False,
            shard_count=1,
            shard_index=0,
            subset_patterns=patterns,
        )

        self.assertEqual(sorted([language_a, language_b, array_test]), selected)
        self.assertEqual(
            ["test/language", "test/built-ins/Array/*.js"],
            selection["subsetPatterns"],
        )
        self.assertEqual(5, selection["candidateCount"])
        self.assertEqual(3, selection["selectedCountBeforeSharding"])

        self.assertTrue(
            run_test262.path_matches_subset(
                "test/Built-Ins/Array/c.js",
                ["test/built-ins/array/*.js"],
            )
        )
        self.assertFalse(
            run_test262.path_matches_subset(
                "test/built-ins/Array/nested/c.js",
                ["test/built-ins/Array/*.js"],
            )
        )

    def test_feature_filters_support_any_all_wildcards_and_deduplication(self) -> None:
        temporal = self.write_test(
            "test/language/temporal.js",
            "/*---\nfeatures: [Temporal]\n---*/\n1;\n",
        )
        temporal_intl = self.write_test(
            "test/language/temporal-intl.js",
            "/*---\nfeatures: [Temporal, Intl.DateTimeFormat]\n---*/\n2;\n",
        )
        self.write_test(
            "test/language/iterator.js",
            "/*---\nfeatures: [iterator-helpers]\n---*/\n3;\n",
        )
        self.write_test("test/language/untagged.js", "4;\n")
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))
        patterns = run_test262.parse_semicolon_patterns(
            "Temporal; Intl.* ;Temporal"
        )

        any_selected, any_selection = run_test262.select_paths(
            repo,
            ["test/language"],
            False,
            1,
            0,
            feature_patterns=patterns,
            feature_match="any",
        )
        all_selected, all_selection = run_test262.select_paths(
            repo,
            ["test/language"],
            False,
            8,
            run_test262.shard_index_for_path(temporal_intl, 8),
            feature_patterns=patterns,
            feature_match="all",
        )

        self.assertEqual(sorted([temporal, temporal_intl]), any_selected)
        self.assertEqual([temporal_intl], all_selected)
        self.assertEqual(["Temporal", "Intl.*"], any_selection["featurePatterns"])
        self.assertEqual("any", any_selection["featureMatch"])
        self.assertEqual("all", all_selection["featureMatch"])
        # The feature intersection is counted before the stable shard is applied.
        self.assertEqual(1, all_selection["selectedCountBeforeSharding"])

    def test_parse_metadata_supports_inline_and_block_style_lists(self) -> None:
        metadata, _ = run_test262.parse_metadata(
            """/*---
features:
  - Temporal
  - SharedArrayBuffer
flags:
  - module
includes: [assert.js, sta.js]
---*/
1 + 1;
"""
        )

        self.assertEqual(["Temporal", "SharedArrayBuffer"], metadata["features"])
        self.assertEqual(["module"], metadata["flags"])
        self.assertEqual(["assert.js", "sta.js"], metadata["includes"])

    def test_apply_shard_rejects_invalid_parameters(self) -> None:
        with self.assertRaisesRegex(ValueError, "shard_count must be greater than 0"):
            run_test262.apply_shard(["test/language/example.js"], 0, 0)

        self.assertEqual(
            ["test/language/example.js"],
            run_test262.apply_shard(["test/language/example.js"], 2, -1),
        )
        self.assertEqual(
            ["a.js", "b.js", "c.js"],
            run_test262.apply_shard(["a.js", "b.js", "c.js"], 8, -1),
        )

        with self.assertRaisesRegex(ValueError, "shard_index must be -1 or between 0 and 1"):
            run_test262.apply_shard(["test/language/example.js"], 2, -2)

        with self.assertRaisesRegex(ValueError, "shard_index must be -1 or between 0 and 1"):
            run_test262.apply_shard(["test/language/example.js"], 2, 2)

    def test_fnv_sharding_is_stable_and_normalizes_path_separators(self) -> None:
        # Published FNV-1a 32-bit test vector for UTF-8/ASCII "hello".
        self.assertEqual(0x4F9F2CAB, run_test262.shard_index_for_path("hello", 2**32))
        self.assertEqual(
            run_test262.shard_index_for_path("test/language/café.js", 8),
            run_test262.shard_index_for_path("test\\language\\café.js", 8),
        )

        original = [
            "test/language/a.js",
            "test/language/b.js",
            "test/built-ins/Array/c.js",
        ]
        assignments = {
            path: run_test262.shard_index_for_path(path, 8) for path in original
        }
        with_unrelated_insert = ["test/annexB/earlier.js", *original]

        for path, shard_index in assignments.items():
            self.assertIn(
                path,
                run_test262.apply_shard(with_unrelated_insert, 8, shard_index),
            )
        self.assertEqual(
            sorted(original),
            sorted(
                path
                for shard_index in range(8)
                for path in run_test262.apply_shard(original, 8, shard_index)
            ),
        )

    def test_timeout_and_worker_validators_reject_non_positive_or_non_finite_values(self) -> None:
        for value in ("0", "-1", "nan", "inf", "-inf"):
            with self.subTest(timeout=value), self.assertRaises(argparse.ArgumentTypeError):
                run_test262.positive_float(value)
        for value in ("0", "-1"):
            with self.subTest(workers=value), self.assertRaises(argparse.ArgumentTypeError):
                run_test262.positive_int(value)

        self.assertEqual(0.25, run_test262.positive_float("0.25"))
        self.assertEqual(3, run_test262.positive_int("3"))

    def test_main_accepts_shard_index_minus_one_for_all_selected_paths(self) -> None:
        first_path = self.write_test("test/language/a.js", "1 + 1;\n")
        second_path = self.write_test("test/language/b.js", "2 + 2;\n")
        stdout = StringIO()
        stderr = StringIO()

        with (
            mock.patch.object(
                sys,
                "argv",
                [
                    "run_test262.py",
                    "--suite-ref",
                    TEST_SUITE_REF,
                    "--suite-root",
                    str(self.suite_root),
                    "--broiler-dll",
                    TEST_ENGINE_PATH,
                    "--all-script-host-verifiable",
                    "--shard-count",
                    "8",
                    "--shard-index",
                    "-1",
                ],
            ),
            mock.patch.object(
                run_test262,
                "run_test",
                side_effect=[
                    {"path": first_path, "status": "passed"},
                    {"path": second_path, "status": "passed"},
                ],
            ) as run_test_mock,
            redirect_stdout(stdout),
            redirect_stderr(stderr),
        ):
            exit_code = run_test262.main()

        self.assertEqual(0, exit_code)
        summary = run_test262.json.loads(stdout.getvalue())
        self.assertEqual(-1, summary["shardIndex"])
        self.assertEqual(8, summary["shardCount"])
        self.assertEqual([first_path, second_path], summary["expandedPaths"])
        self.assertIn("Selected 2 runnable test(s) for shard all/8", stderr.getvalue())
        self.assertIn("Running 2 test(s) for shard all/8", stderr.getvalue())
        self.assertEqual(2, run_test_mock.call_count)
        self.assertEqual(
            [first_path, second_path],
            [call.args[2] for call in run_test_mock.call_args_list],
        )
        self.assertEqual(30.0, run_test_mock.call_args.args[4])
        self.assertEqual(0, run_test_mock.call_args.args[5])

    def test_main_logs_major_checkpoints_to_stderr_and_preserves_json_stdout(self) -> None:
        path = self.write_test("test/language/example.js", "1 + 1;\n")
        output_path = Path(self.temp_directory.name) / "summary.json"
        stdout = StringIO()
        stderr = StringIO()

        with (
            mock.patch.object(
                sys,
                "argv",
                [
                    "run_test262.py",
                    "--suite-ref",
                    TEST_SUITE_REF,
                    "--suite-root",
                    str(self.suite_root),
                    "--broiler-dll",
                    TEST_ENGINE_PATH,
                    "--output",
                    str(output_path),
                    "--test-timeout-seconds",
                    "12.5",
                    "--memory-limit-mb",
                    "512",
                    path,
                ],
            ),
            mock.patch.object(
                run_test262,
                "run_test",
                return_value={"path": path, "status": "passed"},
            ),
            redirect_stdout(stdout),
            redirect_stderr(stderr),
        ):
            exit_code = run_test262.main()

        self.assertEqual(0, exit_code)
        summary = run_test262.json.loads(stdout.getvalue())
        self.assertEqual(TEST_SUITE_REF, summary["suiteRef"])
        self.assertEqual(12.5, summary["testTimeoutSeconds"])
        self.assertEqual(512, summary["memoryLimitMb"])
        self.assertEqual(1, summary["passed"])
        self.assertEqual([path], summary["expandedPaths"])
        self.assertEqual(summary, run_test262.json.loads(output_path.read_text(encoding="utf-8")))

        log_output = stderr.getvalue()
        self.assertIn(f"Starting test262 run for suite ref {TEST_SUITE_REF}", log_output)
        self.assertIn("Selected 1 runnable test(s) for shard 1/1", log_output)
        self.assertIn(f"Using Broiler script host at {TEST_ENGINE_PATH}", log_output)
        self.assertIn("Running 1 test(s) for shard 1/1", log_output)
        self.assertIn("Per-test timeout is 12.5s; POSIX memory cap is 512 MiB.", log_output)
        self.assertIn(
            "Completed 1/1 path(s), 1 variant case(s) "
            "(passed=1, failed=0, skipped=0, timedOut=0).",
            log_output,
        )
        self.assertIn(f"Wrote machine-readable summary to {output_path}", log_output)
        self.assertIn(
            "Finished test262 run: variantCases=1, executed=1, "
            "passed=1, failed=0, skipped=0, timedOut=0",
            log_output,
        )

    def test_build_summary_includes_failed_and_timed_out_paths(self) -> None:
        summary = run_test262.build_summary(
            TEST_SUITE_REF,
            TEST_ENGINE_PATH,
            ["test/language"],
            ["test/language/a.js", "test/language/b.js", "test/language/c.js"],
            [
                {"path": "test/language/a.js", "status": "passed"},
                {"path": "test/language/b.js", "status": "failed"},
                {"path": "test/language/c.js", "status": "timedOut"},
            ],
            {
                "selectionMode": "requested",
                "candidateCount": 3,
                "selectedCountBeforeSharding": 3,
                "shardCount": 1,
                "shardIndex": 0,
            },
            30.0,
            0,
        )

        self.assertEqual(["test/language/b.js"], summary["failedPaths"])
        self.assertEqual(["test/language/c.js"], summary["timedOutPaths"])

    def test_collect_requested_paths_ignores_comment_lines_in_path_files(self) -> None:
        path_file = Path(self.temp_directory.name) / "failed-tests.txt"
        path_file.write_text(
            "\n".join(
                [
                    "# Auto-generated by CI",
                    "",
                    "test/language/a.js",
                    "  # another comment",
                    "test/language/b.js",
                ]
            )
            + "\n",
            encoding="utf-8",
        )

        requested_paths = run_test262.collect_requested_paths([], [str(path_file)])

        self.assertEqual(
            ["test/language/a.js", "test/language/b.js"],
            requested_paths,
        )

    def test_shuffle_seed_reorders_paths_deterministically(self) -> None:
        for name in ("a.js", "b.js", "c.js", "d.js", "e.js"):
            self.write_test(f"test/language/{name}", "1;\n")
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))

        paths_no_shuffle, sel_no = run_test262.select_paths(
            repo, [], True, 1, 0, shuffle_seed=None,
        )
        paths_seed_42a, sel_42a = run_test262.select_paths(
            repo, [], True, 1, 0, shuffle_seed=42,
        )
        paths_seed_42b, _ = run_test262.select_paths(
            repo, [], True, 1, 0, shuffle_seed=42,
        )
        paths_seed_99, _ = run_test262.select_paths(
            repo, [], True, 1, 0, shuffle_seed=99,
        )

        self.assertIsNone(sel_no["shuffleSeed"])
        self.assertEqual(42, sel_42a["shuffleSeed"])
        # Same seed => same order
        self.assertEqual(paths_seed_42a, paths_seed_42b)
        # Different seeds => (very likely) different order
        self.assertNotEqual(paths_seed_42a, paths_seed_99)
        # Shuffled order differs from the default sorted order
        self.assertNotEqual(paths_no_shuffle, paths_seed_42a)
        # All paths present
        self.assertEqual(sorted(paths_no_shuffle), sorted(paths_seed_42a))

    def test_shuffle_preserves_fragile_priority_group(self) -> None:
        paths = [
            self.write_test(f"test/language/{name}.js", "1;\n")
            for name in ("a", "b", "c", "d", "e", "f")
        ]
        fragile = {paths[0], paths[2], paths[4]}
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))

        with (
            mock.patch.object(run_test262, "_load_fragile_paths", return_value=fragile),
            mock.patch.object(
                run_test262,
                "_detect_recently_changed_directories",
                return_value=set(),
            ),
        ):
            selected, selection = run_test262.select_paths(
                repo,
                ["test/language"],
                False,
                1,
                0,
                shuffle_seed=42,
                prioritize_fragile=True,
            )

        promoted_count = selection["promotedCount"]
        self.assertEqual(3, promoted_count)
        self.assertEqual(fragile, set(selected[:promoted_count]))
        self.assertTrue(all(path not in fragile for path in selected[promoted_count:]))
        self.assertEqual(sorted(paths), sorted(selected))

    def test_select_paths_include_negative_adds_negative_tests(self) -> None:
        positive_path = self.write_test("test/language/a.js", "1 + 1;\n")
        negative_path = self.write_test(
            "test/language/b-negative.js",
            "/*---\nnegative:\n  phase: runtime\n  type: ReferenceError\n---*/\nmissing;\n",
        )
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))

        paths_default, sel_default = run_test262.select_paths(
            repo, [], True, 1, 0, include_negative=False,
        )
        paths_neg, sel_neg = run_test262.select_paths(
            repo, [], True, 1, 0, include_negative=True,
        )

        self.assertEqual([positive_path], paths_default)
        self.assertFalse(sel_default["includeNegative"])
        self.assertIn(positive_path, paths_neg)
        self.assertIn(negative_path, paths_neg)
        self.assertTrue(sel_neg["includeNegative"])

    def test_run_test_negative_passes_on_matching_error(self) -> None:
        path = self.write_test(
            "test/language/neg.js",
            "/*---\nnegative:\n  phase: runtime\n  type: ReferenceError\n---*/\nmissing;\n",
        )
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))
        process = mock.Mock()
        process.pid = 9999
        process.returncode = 1
        process.communicate.return_value = ("", "ReferenceError: missing is not defined")

        with mock.patch.object(run_test262.subprocess, "Popen", return_value=process):
            result = run_test262.run_test(
                repo, TEST_ENGINE_PATH, path, {}, 30.0, 0, include_negative=True,
            )

        self.assertEqual("passed", result["status"])
        self.assertTrue(result.get("negative"))

    def test_run_test_negative_fails_on_wrong_error(self) -> None:
        path = self.write_test(
            "test/language/neg.js",
            "/*---\nnegative:\n  phase: runtime\n  type: ReferenceError\n---*/\nmissing;\n",
        )
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))
        process = mock.Mock()
        process.pid = 9999
        process.returncode = 1
        process.communicate.return_value = ("", "TypeError: something else")

        with mock.patch.object(run_test262.subprocess, "Popen", return_value=process):
            result = run_test262.run_test(
                repo, TEST_ENGINE_PATH, path, {}, 30.0, 0, include_negative=True,
            )

        self.assertEqual("failed", result["status"])
        self.assertIn("expected ReferenceError", result["reason"])

    def test_run_test_negative_fails_when_test_succeeds(self) -> None:
        path = self.write_test(
            "test/language/neg.js",
            "/*---\nnegative:\n  phase: runtime\n  type: SyntaxError\n---*/\n1 + 1;\n",
        )
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))
        process = mock.Mock()
        process.pid = 9999
        process.returncode = 0
        process.communicate.return_value = ("", "")

        with mock.patch.object(run_test262.subprocess, "Popen", return_value=process):
            result = run_test262.run_test(
                repo, TEST_ENGINE_PATH, path, {}, 30.0, 0, include_negative=True,
            )

        self.assertEqual("failed", result["status"])
        self.assertIn("expected SyntaxError but succeeded", result["reason"])

    def test_run_test_negative_skipped_when_not_included(self) -> None:
        path = self.write_test(
            "test/language/neg.js",
            "/*---\nnegative:\n  phase: runtime\n  type: ReferenceError\n---*/\nmissing;\n",
        )
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))

        result = run_test262.run_test(
            repo, TEST_ENGINE_PATH, path, {}, 30.0, 0, include_negative=False,
        )

        self.assertEqual("skipped", result["status"])

    def test_minifier_receives_only_metadata_stripped_test_body(self) -> None:
        (self.suite_root / "harness" / "custom.js").write_text(
            "// custom harness\nCUSTOM_HARNESS();\n",
            encoding="utf-8",
        )
        path = self.write_test(
            "test/language/minifier-boundary.js",
            "/*---\nincludes: [custom.js]\nfeatures: [example]\n---*/\n"
            "ORIGINAL_BODY();\n",
        )
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))
        minifier = FakeMinifier("MINIFIED_BODY();")
        engine = RecordingPopen()

        with mock.patch.object(run_test262.subprocess, "Popen", side_effect=engine):
            results = run_test262.run_test_variants_with_metadata(
                repo,
                TEST_ENGINE_PATH,
                path,
                {},
                30.0,
                0,
                minifier=minifier,
            )

        self.assertEqual(
            [(path, "original"), (path, "terser")],
            [(result["path"], result["variant"]) for result in results],
        )
        self.assertEqual(1, len(minifier.calls))
        minifier_input, minifier_path = minifier.calls[0]
        self.assertEqual(path, minifier_path)
        self.assertIn("ORIGINAL_BODY();", minifier_input)
        self.assertNotIn("/*---", minifier_input)
        self.assertNotIn("assert harness", minifier_input)
        self.assertNotIn("sta harness", minifier_input)
        self.assertNotIn("CUSTOM_HARNESS", minifier_input)
        self.assertNotIn("__broilerDonePromise", minifier_input)

        self.assertEqual(2, len(engine.scripts))
        minified_script = engine.scripts[1]
        self.assertIn("// assert harness", minified_script)
        self.assertIn("// sta harness", minified_script)
        self.assertIn("CUSTOM_HARNESS();", minified_script)
        self.assertIn("MINIFIED_BODY();", minified_script)
        self.assertNotIn("ORIGINAL_BODY();", minified_script)

    def test_async_minified_body_is_separated_from_unchanged_completion_tail(self) -> None:
        path = self.write_test(
            "test/language/minified-async.js",
            "/*---\nflags: [async]\n---*/\nORIGINAL_ASYNC_BODY()\n",
        )
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))
        minifier = FakeMinifier("MINIFIED_ASYNC_BODY()")
        engine = RecordingPopen()

        with mock.patch.object(run_test262.subprocess, "Popen", side_effect=engine):
            results = run_test262.run_test_variants_with_metadata(
                repo,
                TEST_ENGINE_PATH,
                path,
                {},
                30.0,
                0,
                minifier=minifier,
            )

        self.assertEqual(["original", "terser"], [r["variant"] for r in results])
        minifier_input = minifier.calls[0][0]
        self.assertIn("ORIGINAL_ASYNC_BODY()", minifier_input)
        self.assertNotIn("globalThis.$DONE", minifier_input)
        self.assertNotIn("__broilerDonePromise", minifier_input)

        minified_script = engine.scripts[1]
        setup_index = minified_script.index("const __broilerDonePromise")
        body_index = minified_script.index("MINIFIED_ASYNC_BODY()")
        tail_index = minified_script.index("\n;__broilerDonePromise")
        self.assertLess(setup_index, body_index)
        self.assertLess(body_index, tail_index)
        self.assertTrue(minified_script.endswith(";__broilerDonePromise"))

    def test_only_strict_variant_minifies_and_executes_under_strict_mode(self) -> None:
        path = self.write_test(
            "test/language/minified-only-strict.js",
            "/*---\nflags: [onlyStrict]\n---*/\nSTRICT_BODY();\n",
        )
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))
        minifier = FakeMinifier("MINIFIED_STRICT_BODY();")
        engine = RecordingPopen()

        with mock.patch.object(run_test262.subprocess, "Popen", side_effect=engine):
            results = run_test262.run_test_variants_with_metadata(
                repo,
                TEST_ENGINE_PATH,
                path,
                {},
                30.0,
                0,
                minifier=minifier,
            )

        self.assertEqual(["original", "terser"], [r["variant"] for r in results])
        self.assertTrue(minifier.calls[0][0].startswith('"use strict";\n'))
        self.assertIn("STRICT_BODY();", minifier.calls[0][0])
        self.assertTrue(
            engine.scripts[1].startswith('"use strict";\n// assert harness')
        )
        self.assertIn("MINIFIED_STRICT_BODY();", engine.scripts[1])

    def test_negative_phase_controls_terser_eligibility(self) -> None:
        cases = (
            ("runtime", "runtime", True),
            ("quoted-runtime", "'RUNTIME'", True),
            ("parse", "parse", False),
            ("early", "early", False),
            ("resolution", "resolution", False),
            ("unknown", "future-phase", False),
            ("missing", None, False),
        )
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))

        for name, phase, eligible in cases:
            with self.subTest(phase=name):
                phase_line = f"  phase: {phase}\n" if phase is not None else ""
                path = self.write_test(
                    f"test/language/negative-{name}.js",
                    "/*---\nnegative:\n"
                    f"{phase_line}"
                    "  type: ReferenceError\n---*/\nmissing;\n",
                )
                minifier = FakeMinifier()
                with mock.patch.object(
                    run_test262,
                    "run_test",
                    side_effect=passing_run_test,
                ):
                    results = run_test262.run_test_variants_with_metadata(
                        repo,
                        TEST_ENGINE_PATH,
                        path,
                        {},
                        30.0,
                        0,
                        include_negative=True,
                        minifier=minifier,
                    )

                self.assertEqual(["original", "terser"], [r["variant"] for r in results])
                self.assertEqual(1 if eligible else 0, len(minifier.calls))
                if eligible:
                    self.assertEqual("passed", results[1]["status"])
                else:
                    self.assertEqual("skipped", results[1]["status"])
                    self.assertTrue(results[1]["notApplicable"])
                    self.assertEqual("minifier-not-applicable", results[1]["skipKind"])

    def test_function_tostring_observers_are_not_minified(self) -> None:
        cases = (
            (
                "test/built-ins/Function/prototype/toString/source.js",
                "function observed() {}\n",
            ),
            (
                "test/language/direct-function-tostring.js",
                "Function . prototype . toString.call(function observed() {});\n",
            ),
            # A test that names no Function.prototype.toString and still measures source
            # text: the literal is a transcript of the generator declared above it.
            (
                "test/language/quoted-source-tostring.js",
                'function* g() { yield 1; }\n'
                'assert.sameValue("function* g() { yield 1; }", g.toString());\n',
            ),
        )
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))

        for path, source in cases:
            with self.subTest(path=path):
                self.write_test(path, source)
                minifier = FakeMinifier()
                with mock.patch.object(
                    run_test262,
                    "run_test",
                    side_effect=passing_run_test,
                ):
                    results = run_test262.run_test_variants_with_metadata(
                        repo,
                        TEST_ENGINE_PATH,
                        path,
                        {},
                        30.0,
                        0,
                        minifier=minifier,
                    )

                self.assertEqual([], minifier.calls)
                self.assertEqual("passed", results[0]["status"])
                self.assertEqual("skipped", results[1]["status"])
                self.assertTrue(results[1]["notApplicable"])
                self.assertIn("observes source text", results[1]["reason"])

    def test_a_quoted_expression_in_an_assertion_message_is_still_minified(self) -> None:
        # The suite is full of messages that quote the expression they are about, and one
        # of those is not a test measuring its own source text: only a quoted DEFINITION
        # standing next to a toString is.
        path = self.write_test(
            "test/language/quoted-assertion-message.js",
            "var array = [1, 2, 3];\n"
            "assert(array.indexOf(3, 2) === 2, 'array.indexOf(3, 2) !== 2');\n"
            "assert.sameValue(array.toString(), '1,2,3', 'array.toString() is wrong');\n"
            "function helper(value) { return value; }\n"
            "assert.sameValue(helper(1), 1, 'function helper(value) { return value; }');\n",
        )
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))
        minifier = FakeMinifier()

        with mock.patch.object(
            run_test262, "run_test", side_effect=passing_run_test
        ):
            results = run_test262.run_test_variants_with_metadata(
                repo, TEST_ENGINE_PATH, path, {}, 30.0, 0, minifier=minifier,
            )

        self.assertEqual(1, len(minifier.calls))
        self.assertEqual(["passed", "passed"], [r["status"] for r in results])

    def test_syntax_the_minifier_silently_rewrites_is_not_applicable(self) -> None:
        # Terser does not reject `accessor #x = 1`; it reads it as two class elements and
        # emits a different program. There is no minified variant of this test to run, so
        # nothing may be attributed to the engine either way.
        path = self.write_test(
            "test/language/auto-accessor.js",
            "class C { accessor #x = 1; }\n",
        )
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))
        minifier = FakeMinifier(unsupported_syntax="auto-accessor")
        engine = FakeReferenceEngine()

        def failing_minified(repo, dll, path, cache, timeout, mem,
                             include_negative=False, *, variant=None,
                             body_override=None, **kwargs):
            self.fail("the minified variant must not be executed")

        with mock.patch.object(
            run_test262,
            "run_test",
            side_effect=lambda *args, **kwargs: (
                failing_minified(*args, **kwargs)
                if kwargs.get("body_override") is not None
                else passing_run_test(*args, **kwargs)
            ),
        ):
            results = run_test262.run_test_variants_with_metadata(
                repo, TEST_ENGINE_PATH, path, {}, 30.0, 0,
                minifier=minifier, reference_engine=engine,
            )

        self.assertEqual("passed", results[0]["status"])
        self.assertEqual("skipped", results[1]["status"])
        self.assertIs(True, results[1]["notApplicable"])
        self.assertEqual("minifier-unsupported-syntax", results[1]["skipKind"])
        self.assertIn("auto-accessor", str(results[1]["reason"]))
        self.assertEqual([], engine.programs)

    def test_skipped_original_produces_not_applicable_terser_case(self) -> None:
        path = self.write_test("test/language/skipped-original.js", "1;\n")
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))
        minifier = FakeMinifier()

        def skipped_run_test(*args: object, **kwargs: object) -> dict[str, object]:
            return {
                "path": path,
                "status": "skipped",
                "reason": "unsupported host requirement",
            }

        with mock.patch.object(
            run_test262,
            "run_test",
            side_effect=skipped_run_test,
        ):
            results = run_test262.run_test_variants_with_metadata(
                repo,
                TEST_ENGINE_PATH,
                path,
                {},
                30.0,
                0,
                minifier=minifier,
            )

        self.assertEqual([], minifier.calls)
        self.assertEqual(["original", "terser"], [r["variant"] for r in results])
        self.assertEqual(["skipped", "skipped"], [r["status"] for r in results])
        self.assertTrue(results[1]["notApplicable"])
        self.assertIn("original variant is not runnable", results[1]["reason"])

    def test_minifier_errors_and_timeouts_do_not_replace_original_result(self) -> None:
        path = self.write_test("test/language/minifier-failure.js", "1;\n")
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))
        cases = (
            (run_test262.MinifierError("transform failed"), "MinifierError"),
            (run_test262.MinifierTimeoutError("transform hung"), "MinifierTimeout"),
        )

        for error, failure_type in cases:
            with self.subTest(failure_type=failure_type):
                minifier = FakeMinifier(error=error)
                with mock.patch.object(
                    run_test262,
                    "run_test",
                    side_effect=passing_run_test,
                ) as run_test_mock:
                    results = run_test262.run_test_variants_with_metadata(
                        repo,
                        TEST_ENGINE_PATH,
                        path,
                        {},
                        30.0,
                        0,
                        minifier=minifier,
                    )

                self.assertEqual(1, run_test_mock.call_count)
                self.assertEqual("passed", results[0]["status"])
                self.assertEqual("original", results[0]["variant"])
                self.assertEqual("failed", results[1]["status"])
                self.assertEqual("terser", results[1]["variant"])
                self.assertTrue(results[1]["infrastructure"])
                self.assertEqual(failure_type, results[1]["failureType"])
                self.assertIn(str(error), results[1]["reason"])

    def test_minifier_parse_rejection_is_not_applicable_not_a_failure(self) -> None:
        # Terser refuses valid ECMAScript the engine runs (an escaped keyword used as an
        # IdentifierName, sloppy-mode `let`, an Annex B assignment target, decorators,
        # import attributes with a trailing comma). Nothing was minified, so there is no
        # minified variant whose result could be attributed to the engine.
        path = self.write_test("test/language/minifier-cannot-parse.js", "1;\n")
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))
        error = run_test262.MinifierSyntaxError(
            "Terser SyntaxError at line 8, column 2: "
            "Escaped characters are not allowed in keywords"
        )
        minifier = FakeMinifier(error=error)

        with mock.patch.object(
            run_test262,
            "run_test",
            side_effect=passing_run_test,
        ) as run_test_mock:
            results = run_test262.run_test_variants_with_metadata(
                repo,
                TEST_ENGINE_PATH,
                path,
                {},
                30.0,
                0,
                minifier=minifier,
            )

        self.assertEqual(1, run_test_mock.call_count)
        self.assertEqual("passed", results[0]["status"])
        self.assertEqual("skipped", results[1]["status"])
        self.assertEqual("terser", results[1]["variant"])
        self.assertTrue(results[1]["notApplicable"])
        self.assertEqual("minifier-unsupported-syntax", results[1]["skipKind"])
        self.assertNotIn("infrastructure", results[1])
        self.assertIn(str(error), results[1]["reason"])

    def test_terser_engine_timeout_is_kept_as_a_separate_variant_case(self) -> None:
        path = self.write_test("test/language/terser-engine-timeout.js", "1;\n")
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))
        minifier = FakeMinifier()

        def variant_result(
            *args: object,
            variant: str | None = None,
            **kwargs: object,
        ) -> dict[str, object]:
            if variant == "terser":
                return {"path": path, "variant": variant, "status": "timedOut"}
            return {"path": path, "status": "passed"}

        with mock.patch.object(
            run_test262,
            "run_test",
            side_effect=variant_result,
        ):
            results = run_test262.run_test_variants_with_metadata(
                repo,
                TEST_ENGINE_PATH,
                path,
                {},
                30.0,
                0,
                minifier=minifier,
            )

        self.assertEqual(
            [("original", "passed"), ("terser", "timedOut")],
            [(result["variant"], result["status"]) for result in results],
        )

    def test_parse_metadata_handles_non_lf_line_terminators(self) -> None:
        # A test whose source uses CR / CRLF line terminators (e.g. the toString
        # line-terminator-normalisation cases) must still have its metadata recognised, and the
        # returned body must keep its original terminators so the script host sees the exact source.
        source = (
            "/*---\r\n"
            "description: crlf\r\n"
            "includes: [nativeFunctionMatcher.js]\r\n"
            "flags: [onlyStrict]\r\n"
            "---*/\r\n"
            "var f = function () {};\r\n"
        )
        metadata, body = run_test262.parse_metadata(source)

        self.assertEqual(["nativeFunctionMatcher.js"], metadata["includes"])
        self.assertEqual(["onlyStrict"], metadata["flags"])
        self.assertIn("\r\n", body)
        self.assertNotIn("description: crlf", body)

    def test_read_text_preserves_line_terminators(self) -> None:
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))
        (self.suite_root / "test" / "language" / "cr.js").write_bytes(
            b"var s = 'a\\rb';\rthrow s;\r"
        )

        text = repo.read_text("test/language/cr.js")

        self.assertIn("\r", text)
        self.assertNotIn("\n", text)

    def test_can_block_is_false_test_skipped(self) -> None:
        # Broiler's agent can block, so a CanBlockIsFalse-flagged test (which assumes a
        # host whose agent cannot block) is not applicable and is skipped.
        path = self.write_test(
            "test/built-ins/Atomics/wait/cannot-suspend.js",
            "/*---\nflags: [CanBlockIsFalse]\n---*/\nthrow 0;\n",
        )
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))

        result = run_test262.run_test(
            repo, TEST_ENGINE_PATH, path, {}, 30.0, 0,
        )

        self.assertEqual("skipped", result["status"])
        self.assertIn("CanBlockIsFalse", result["reason"])

    def test_can_block_is_true_test_not_skipped(self) -> None:
        # The complementary CanBlockIsTrue tests exercise the real wait behaviour and
        # remain script-host-verifiable.
        classification = run_test262.classify_test(
            "/*---\nflags: [CanBlockIsTrue]\n---*/\nok;\n"
        )
        self.assertEqual([], classification["unsupportedFlags"])
        self.assertTrue(classification["isScriptHostVerifiable"])

    def test_build_summary_includes_new_metadata_fields(self) -> None:
        summary = run_test262.build_summary(
            TEST_SUITE_REF,
            TEST_ENGINE_PATH,
            ["test/language"],
            ["test/language/a.js"],
            [{"path": "test/language/a.js", "status": "passed"}],
            {
                "selectionMode": "requested",
                "candidateCount": 1,
                "selectedCountBeforeSharding": 1,
                "shardCount": 1,
                "shardIndex": 0,
                "subsetPatterns": ["test/language"],
                "featurePatterns": ["Temporal", "Intl.*"],
                "featureMatch": "all",
            },
            30.0,
            0,
            max_workers=4,
            shuffle_seed=42,
            include_negative=True,
            selection_label="runtime",
            runner_os="Linux",
            runner_arch="X64",
            dotnet_version="10.0.100",
        )

        self.assertEqual(4, summary["maxWorkers"])
        self.assertEqual(42, summary["shuffleSeed"])
        self.assertTrue(summary["includeNegative"])
        self.assertEqual(["test/language"], summary["subsetPatterns"])
        self.assertEqual(["Temporal", "Intl.*"], summary["featurePatterns"])
        self.assertEqual("all", summary["featureMatch"])
        self.assertEqual("runtime", summary["selectionLabel"])
        self.assertEqual("Linux", summary["runnerOs"])
        self.assertEqual("X64", summary["runnerArch"])
        self.assertEqual("10.0.100", summary["dotnetVersion"])

    def test_build_summary_counts_variant_cases_and_unique_path_outcomes(self) -> None:
        paths = [f"test/language/{name}.js" for name in ("a", "b", "c", "d")]
        results = [
            {"path": paths[0], "variant": "original", "status": "passed"},
            {"path": paths[0], "variant": "terser", "status": "passed"},
            {"path": paths[1], "variant": "original", "status": "passed"},
            {
                "path": paths[1],
                "variant": "terser",
                "status": "skipped",
                "notApplicable": True,
            },
            {"path": paths[2], "variant": "original", "status": "failed"},
            {"path": paths[2], "variant": "terser", "status": "passed"},
            {"path": paths[3], "variant": "original", "status": "passed"},
            {"path": paths[3], "variant": "terser", "status": "timedOut"},
        ]
        summary = run_test262.build_summary(
            TEST_SUITE_REF,
            TEST_ENGINE_PATH,
            ["test/language"],
            paths,
            results,
            {
                "selectionMode": "requested",
                "candidateCount": 4,
                "selectedCountBeforeSharding": 4,
                "shardCount": 1,
                "shardIndex": 0,
            },
            30.0,
            0,
            minifier="terser",
            minifier_version="5.test",
            minifier_profile=run_test262.TERSER_PROFILE,
            minifier_timeout_seconds=12.5,
        )

        self.assertEqual(8, summary["total"])
        self.assertEqual(7, summary["executed"])
        self.assertEqual(5, summary["passed"])
        self.assertEqual(1, summary["failed"])
        self.assertEqual(1, summary["skipped"])
        self.assertEqual(1, summary["timedOut"])
        self.assertEqual(1, summary["notApplicable"])
        self.assertEqual([paths[2]], summary["failedPaths"])
        self.assertEqual([paths[3]], summary["timedOutPaths"])
        self.assertEqual(
            {
                "total": 4,
                "executed": 4,
                "passed": 2,
                "failed": 1,
                "skipped": 0,
                "timedOut": 1,
            },
            summary["pathSummary"],
        )
        self.assertEqual(
            {
                "total": 4,
                "executed": 4,
                "passed": 3,
                "failed": 1,
                "skipped": 0,
                "timedOut": 0,
                "notApplicable": 0,
            },
            summary["variantSummary"]["original"],
        )
        self.assertEqual(
            {
                "total": 4,
                "executed": 3,
                "passed": 2,
                "failed": 0,
                "skipped": 1,
                "timedOut": 1,
                "notApplicable": 1,
            },
            summary["variantSummary"]["terser"],
        )
        self.assertEqual(["original", "terser"], summary["variants"])
        self.assertEqual(2, summary["expectedVariantCountPerPath"])
        self.assertEqual("5.test", summary["minifierVersion"])
        self.assertEqual(run_test262.TERSER_PROFILE, summary["minifierProfile"])
        self.assertEqual(12.5, summary["minifierTimeoutSeconds"])
        self.assertEqual(False, summary["minifierOptions"]["compress"])
        self.assertEqual(True, summary["minifierOptions"]["mangle"])
        self.assertEqual(False, summary["minifierOptions"]["toplevel"])

    def test_run_selected_tests_enriches_results_with_triage_metadata(self) -> None:
        path = self.write_test(
            "test/language/metadata.js",
            "/*---\nfeatures: [Temporal, Intl.DateTimeFormat]\nflags: [onlyStrict]\n---*/\n1;\n",
        )
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))
        selection = {
            "selectionMode": "requested",
            "candidateCount": 1,
            "selectedCountBeforeSharding": 1,
            "shardCount": 1,
            "shardIndex": 0,
        }

        with (
            mock.patch.object(
                run_test262,
                "run_test",
                return_value={"path": path, "status": "passed"},
            ),
            mock.patch.object(
                run_test262.time,
                "perf_counter",
                side_effect=[100.0, 100.012345],
            ),
        ):
            results = run_test262.run_selected_tests(
                repo, TEST_ENGINE_PATH, [path], selection, 30.0, 0,
            )

        self.assertEqual(1, len(results))
        result = results[0]
        self.assertEqual(len(repo.read_text(path).encode("utf-8")), result["sourceSizeBytes"])
        self.assertEqual(["Temporal", "Intl.DateTimeFormat"], result["features"])
        self.assertEqual(["onlyStrict"], result["flags"])
        self.assertAlmostEqual(12.345, result["durationMs"], places=3)

    def test_worker_exception_becomes_failed_infrastructure_result(self) -> None:
        paths = [
            self.write_test(f"test/language/{name}.js", "1;\n")
            for name in ("a", "b", "c")
        ]
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))
        selection = {
            "selectionMode": "requested",
            "candidateCount": 3,
            "selectedCountBeforeSharding": 3,
            "shardCount": 1,
            "shardIndex": 0,
        }

        def fake_run_test(repo, dll, path, cache, timeout, mem, include_negative=False):
            if path.endswith("/b.js"):
                raise OSError("worker exploded")
            return {"path": path, "status": "passed"}

        with mock.patch.object(run_test262, "run_test", side_effect=fake_run_test):
            results = run_test262.run_selected_tests(
                repo,
                TEST_ENGINE_PATH,
                paths,
                selection,
                30.0,
                0,
                max_workers=2,
            )

        self.assertEqual(paths, [result["path"] for result in results])
        failed = results[1]
        self.assertEqual("failed", failed["status"])
        self.assertTrue(failed["infrastructure"])
        self.assertEqual("OSError", failed["exceptionType"])
        self.assertIn("worker exploded", failed["reason"])
        self.assertIsInstance(failed["sourceSizeBytes"], int)
        self.assertIn("durationMs", failed)

    def test_no_summary_stdout_writes_only_the_requested_output_file(self) -> None:
        path = self.write_test("test/language/quiet.js", "1;\n")
        output_path = Path(self.temp_directory.name) / "quiet-summary.json"
        stdout = StringIO()

        with (
            mock.patch.object(
                sys,
                "argv",
                [
                    "run_test262.py",
                    "--suite-ref",
                    TEST_SUITE_REF,
                    "--suite-root",
                    str(self.suite_root),
                    "--broiler-dll",
                    TEST_ENGINE_PATH,
                    "--output",
                    str(output_path),
                    "--no-summary-stdout",
                    path,
                ],
            ),
            mock.patch.object(
                run_test262,
                "run_test",
                return_value={"path": path, "status": "passed"},
            ),
            redirect_stdout(stdout),
            redirect_stderr(StringIO()),
        ):
            exit_code = run_test262.main()

        self.assertEqual(0, exit_code)
        self.assertEqual("", stdout.getvalue())
        summary = run_test262.json.loads(output_path.read_text(encoding="utf-8"))
        self.assertEqual(1, summary["passed"])

    def test_original_and_terser_order_is_stable_in_serial_and_parallel_runs(self) -> None:
        paths = [
            self.write_test(f"test/language/variant-{name}.js", "1;\n")
            for name in ("a", "b", "c")
        ]
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))
        selection = {
            "selectionMode": "requested",
            "candidateCount": 3,
            "selectedCountBeforeSharding": 3,
            "shardCount": 1,
            "shardIndex": 0,
        }
        expected = [
            (path, variant)
            for path in paths
            for variant in ("original", "terser")
        ]

        for workers in (1, 2):
            with self.subTest(max_workers=workers):
                minifier = FakeMinifier()
                with mock.patch.object(
                    run_test262,
                    "run_test",
                    side_effect=passing_run_test,
                ):
                    results = run_test262.run_selected_tests(
                        repo,
                        TEST_ENGINE_PATH,
                        paths,
                        selection,
                        30.0,
                        0,
                        max_workers=workers,
                        minifier=minifier,
                    )

                self.assertEqual(
                    expected,
                    [(result["path"], result["variant"]) for result in results],
                )
                self.assertEqual(sorted(paths), sorted(path for _, path in minifier.calls))

    def test_parallel_run_returns_results_in_path_order(self) -> None:
        paths = [
            self.write_test(f"test/language/{name}.js", "1;\n")
            for name in ("a", "b", "c")
        ]
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))
        selection = {
            "selectionMode": "requested",
            "candidateCount": 3,
            "selectedCountBeforeSharding": 3,
            "shardCount": 1,
            "shardIndex": 0,
        }

        def fake_run_test(repo, dll, path, cache, timeout, mem, include_negative=False):
            return {"path": path, "status": "passed"}

        with mock.patch.object(run_test262, "run_test", side_effect=fake_run_test):
            results = run_test262.run_selected_tests(
                repo, TEST_ENGINE_PATH, paths, selection,
                30.0, 0, max_workers=2,
            )

        self.assertEqual(3, len(results))
        self.assertEqual(
            paths,
            [r["path"] for r in results],
        )


    # ---- reference-engine cross-check ----

    def _cross_check_case(
        self,
        reference_engine: object,
        *,
        source: str = "ORIGINAL_BODY();\n",
        minified: str = "MINIFIED_BODY();",
        engine_reason: str = "Test262Error: Expected SameValue",
    ) -> dict[str, object]:
        path = self.write_test("test/language/cross-check.js", source)
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))

        def run_test(repo, dll, path, cache, timeout, mem, include_negative=False,
                     *, variant=None, body_override=None, **kwargs):
            result: dict[str, object] = {"path": path, "status": "passed"}
            if variant is not None:
                result["variant"] = variant
            if body_override is not None:
                result["status"] = "failed"
                result["stderr"] = engine_reason
            return result

        with mock.patch.object(run_test262, "run_test", side_effect=run_test):
            results = run_test262.run_test_variants_with_metadata(
                repo,
                TEST_ENGINE_PATH,
                path,
                {},
                30.0,
                0,
                minifier=FakeMinifier(code=minified),
                reference_engine=reference_engine,
            )

        self.assertEqual(["original", "terser"], [r["variant"] for r in results])
        return results[1]

    def test_reference_engine_failing_the_minified_body_attributes_it_to_the_minifier(
        self,
    ) -> None:
        # The second engine runs the test and fails its minified body, so the failure is
        # about the transformation and the case leaves the engine's column entirely.
        engine = FakeReferenceEngine({"MINIFIED_BODY": "failed"})
        result = self._cross_check_case(engine)

        self.assertEqual("skipped", result["status"])
        self.assertIs(True, result["notApplicable"])
        self.assertEqual("minifier-changed-semantics", result["skipKind"])
        self.assertEqual("minifier-changed-semantics", result["referenceCrossCheck"])
        self.assertEqual("fake-engine", result["referenceEngine"])
        self.assertEqual("1.test", result["referenceEngineVersion"])
        # What the engine said is kept: a skipped case still has to be readable.
        self.assertIn("Test262Error", str(result["engineFailure"]))

    def test_minified_body_the_reference_engine_cannot_parse_is_its_own_kind(self) -> None:
        engine = FakeReferenceEngine(
            {"MINIFIED_BODY": "failed"}, unparsable=("MINIFIED_BODY",)
        )
        result = self._cross_check_case(engine)

        self.assertEqual("skipped", result["status"])
        self.assertEqual("minifier-invalid-output", result["skipKind"])
        self.assertIn("cannot parse", str(result["reason"]))

    def test_reference_engine_passing_the_minified_body_keeps_the_engine_failure(
        self,
    ) -> None:
        # The whole point of the check: a divergence the second engine does not reproduce
        # is Broiler's, and stays reported as a failure.
        engine = FakeReferenceEngine()
        result = self._cross_check_case(engine)

        self.assertEqual("failed", result["status"])
        self.assertNotIn("skipKind", result)
        self.assertEqual("engine-divergence", result["referenceCrossCheck"])

    def test_reference_engine_that_cannot_run_the_original_is_inconclusive(self) -> None:
        # A feature the second engine does not implement fails both variants and decides
        # nothing, so the engine's failure stands rather than being explained away.
        engine = FakeReferenceEngine({"BODY": "failed"})
        result = self._cross_check_case(engine)

        self.assertEqual("failed", result["status"])
        self.assertEqual("inconclusive", result["referenceCrossCheck"])
        # It was still asked to READ the minified body, which needs no support for what
        # the test does — only for the grammar it is written in.
        self.assertEqual(1, len(engine.parse_checks))

    def test_unrunnable_original_still_gets_a_verdict_on_unparsable_output(self) -> None:
        # `import((1, 0, "./m.js"))`: the second engine cannot RUN the test — it never
        # resolves the fixture — and still rejects the three-argument call the minifier
        # printed. That answer is about syntax alone, so it settles the case.
        engine = FakeReferenceEngine(
            {"BODY": "failed"}, unparsable=("MINIFIED_BODY",)
        )
        result = self._cross_check_case(engine)

        self.assertEqual("skipped", result["status"])
        self.assertIs(True, result["notApplicable"])
        self.assertEqual("minifier-invalid-output", result["skipKind"])
        self.assertIn("parses this test", str(result["reason"]))
        # Nothing was run to reach that verdict beyond the minified body itself.
        self.assertEqual(1, len(engine.programs))

    def test_an_original_the_reference_engine_cannot_parse_decides_nothing(self) -> None:
        # `using` in Node: the second engine refuses the test's own syntax, so its refusal
        # of the minified body is about the same gap and attributes nothing.
        engine = FakeReferenceEngine(
            {"BODY": "failed"}, unparsable=("BODY",)
        )
        result = self._cross_check_case(engine)

        self.assertEqual("failed", result["status"])
        self.assertEqual("inconclusive", result["referenceCrossCheck"])
        self.assertEqual(2, len(engine.parse_checks))

    def test_unusable_reference_engine_leaves_the_failure_reported(self) -> None:
        engine = FakeReferenceEngine(error=run_test262.ReferenceEngineError("no node"))
        result = self._cross_check_case(engine)

        self.assertEqual("failed", result["status"])
        self.assertEqual("inconclusive", result["referenceCrossCheck"])
        self.assertIn("no node", str(result["referenceCrossCheckError"]))

    def test_passing_minified_cases_are_never_cross_checked(self) -> None:
        path = self.write_test("test/language/cross-check-pass.js", "OK();\n")
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))
        engine = FakeReferenceEngine()

        with mock.patch.object(run_test262, "run_test", side_effect=passing_run_test):
            run_test262.run_test_variants_with_metadata(
                repo, TEST_ENGINE_PATH, path, {}, 30.0, 0,
                minifier=FakeMinifier(), reference_engine=engine,
            )

        self.assertEqual([], engine.programs)

    def test_negative_tests_keep_their_own_verdict(self) -> None:
        # A negative test passes BY failing, which the cross-check's exit-code comparison
        # cannot read, so it is not consulted at all.
        engine = FakeReferenceEngine({"MINIFIED_BODY": "failed"})
        result = self._cross_check_case(
            engine,
            source="/*---\nnegative:\n  phase: runtime\n  type: Test262Error\n---*/\nthrow 1;\n",
        )

        self.assertEqual("failed", result["status"])
        self.assertEqual([], engine.programs)

    def test_cross_check_compares_the_same_program_the_engine_ran(self) -> None:
        engine = FakeReferenceEngine({"MINIFIED_BODY": "failed"})
        self._cross_check_case(engine)

        self.assertEqual(2, len(engine.programs))
        minified_program, original_program = engine.programs
        for program in (minified_program, original_program):
            self.assertIn("// assert harness", program)
            self.assertIn("// sta harness", program)
        self.assertIn("MINIFIED_BODY();", minified_program)
        self.assertIn("ORIGINAL_BODY();", original_program)


if __name__ == "__main__":
    unittest.main()
