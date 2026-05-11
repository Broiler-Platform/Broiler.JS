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

        def fake_run(args: list[str], capture_output: bool, text: bool, check: bool):
            script_path = Path(args[-1])
            captured_script["contents"] = script_path.read_text(encoding="utf-8")
            return subprocess.CompletedProcess(args, 0, "", "")

        with mock.patch.object(run_test262.subprocess, "run", side_effect=fake_run):
            result = run_test262.run_test(repo, TEST_ENGINE_PATH, path, {})

        self.assertEqual({"path": path, "status": "passed"}, result)
        self.assertIn('"use strict";\n\nthis;\n', captured_script["contents"])

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
        self.assertEqual(5, selection["candidateCount"])
        self.assertEqual(2, selection["selectedCountBeforeSharding"])
        self.assertEqual(2, selection["shardCount"])
        self.assertEqual(1, selection["shardIndex"])

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
        self.assertEqual(1, summary["passed"])
        self.assertEqual([path], summary["expandedPaths"])
        self.assertEqual(summary, run_test262.json.loads(output_path.read_text(encoding="utf-8")))

        log_output = stderr.getvalue()
        self.assertIn(f"Starting test262 run for suite ref {TEST_SUITE_REF}", log_output)
        self.assertIn("Selected 1 runnable test(s) for shard 1/1", log_output)
        self.assertIn(f"Using Broiler script host at {TEST_ENGINE_PATH}", log_output)
        self.assertIn("Running 1 test(s) for shard 1/1", log_output)
        self.assertIn("Completed 1/1 test(s) (passed=1, failed=0, skipped=0).", log_output)
        self.assertIn(f"Wrote machine-readable summary to {output_path}", log_output)
        self.assertIn("Finished test262 run: executed=1, passed=1, failed=0, skipped=0", log_output)


if __name__ == "__main__":
    unittest.main()
