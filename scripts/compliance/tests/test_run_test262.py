from __future__ import annotations

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
            preexec_fn,
        ):
            self.assertTrue(text)
            self.assertIsNotNone(preexec_fn)
            self.assertTrue(start_new_session)
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
            mock.patch.object(run_test262.os, "killpg") as killpg,
        ):
            result = run_test262.run_test(repo, TEST_ENGINE_PATH, path, {}, 30.0, 0)

        self.assertEqual("timedOut", result["status"])
        self.assertEqual("timed out after 30s", result["reason"])
        self.assertEqual("partial stdout", result["stdout"])
        self.assertEqual("partial stderr", result["stderr"])
        killpg.assert_called_once_with(4321, run_test262.signal.SIGKILL)

    def test_create_process_limit_setup_applies_posix_limits(self) -> None:
        fake_resource = mock.Mock()
        fake_resource.RLIMIT_CORE = 1
        fake_resource.RLIMIT_CPU = 2
        fake_resource.RLIMIT_AS = 3
        fake_resource.RLIMIT_DATA = 4
        fake_resource.setrlimit = mock.Mock()

        with mock.patch.object(run_test262, "resource", fake_resource):
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

    def test_list_paths_uses_local_suite_root(self) -> None:
        file_path = self.write_test("test/language/example.js", "1 + 1;\n")
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))

        self.assertEqual([file_path], repo.list_paths(prefix="test/", suffix=".js"))

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
        self.write_test(
            "test/language/c3-temporal.js",
            "/*---\nfeatures: [Temporal]\n---*/\nTemporal.Duration();\n",
        )
        self.write_test("test/language/host-harness.js", "$262.createRealm();\n")
        second_path = self.write_test(
            "test/language/d-async.js",
            "/*---\nflags: [async]\n---*/\n$DONE();\n",
        )
        repo = run_test262.Test262Repository(TEST_SUITE_REF, str(self.suite_root))

        selected_paths, selection = run_test262.select_paths(
            repo,
            [],
            all_script_host_verifiable=True,
            shard_count=2,
            shard_index=1,
        )

        self.assertEqual([second_path], selected_paths)
        self.assertEqual("all-script-host-verifiable", selection["selectionMode"])
        self.assertEqual(7, selection["candidateCount"])
        self.assertEqual(2, selection["selectedCountBeforeSharding"])
        self.assertEqual(2, selection["shardCount"])
        self.assertEqual(1, selection["shardIndex"])

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
            "Completed 1/1 test(s) (passed=1, failed=0, skipped=0, timedOut=0).",
            log_output,
        )
        self.assertIn(f"Wrote machine-readable summary to {output_path}", log_output)
        self.assertIn(
            "Finished test262 run: executed=1, passed=1, failed=0, skipped=0, timedOut=0",
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


if __name__ == "__main__":
    unittest.main()
