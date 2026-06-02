from __future__ import annotations

from pathlib import Path
import unittest

import yaml


class Test262WorkflowTests(unittest.TestCase):
    """Regression tests for the unified `.github/workflows/test262.yml`."""

    @property
    def workflow_path(self) -> Path:
        return (
            Path(__file__).resolve().parents[3]
            / ".github"
            / "workflows"
            / "test262.yml"
        )

    def test_run_full_job_uses_always_to_survive_skipped_rerun_dependency(self) -> None:
        workflow_text = self.workflow_path.read_text(encoding="utf-8")

        self.assertIn("always()", workflow_text)
        self.assertIn("needs.plan.outputs.should-rerun-failed != 'true' || needs.rerun-failed.result == 'success'", workflow_text)

    def test_assembly_input_is_exposed_for_targeted_runs(self) -> None:
        workflow_text = self.workflow_path.read_text(encoding="utf-8")

        # The unified runner must expose an `assembly` workflow_dispatch input
        # so contributors can scope a run to a single Broiler.JS assembly.
        self.assertIn("assembly:", workflow_text)
        for assembly in ("parser", "compiler", "runtime", "builtins", "intl", "annexb"):
            self.assertIn(f"          - {assembly}", workflow_text)

    def test_single_test262_workflow_file_exists(self) -> None:
        workflows_dir = self.workflow_path.parent
        test262_workflows = sorted(p.name for p in workflows_dir.glob("test262*.yml"))
        # The refactor consolidates every test262 runner into a single
        # workflow file; superseded variants must not be reintroduced.
        self.assertEqual(test262_workflows, ["test262.yml"])

    def test_persist_failed_tests_is_gated_to_full_suite_runs(self) -> None:
        workflow_text = self.workflow_path.read_text(encoding="utf-8")
        self.assertIn(
            "inputs.assembly == '' || inputs.assembly == 'all'",
            workflow_text,
        )

    def test_logparser_creates_most_common_problems_issue(self) -> None:
        workflow_text = self.workflow_path.read_text(encoding="utf-8")
        self.assertIn("--most-common-problems", workflow_text)

    def test_runner_jobs_fan_out_across_multiple_shards(self) -> None:
        """The full and rerun phases must execute on multiple parallel runners.

        Sharding is what keeps the test262 suite tractable in CI; collapsing
        either runner job back to a single shard would silently regress wall
        time and per-shard memory headroom, so this test pins the invariant
        explicitly.
        """

        workflow = yaml.safe_load(self.workflow_path.read_text(encoding="utf-8"))

        # SHARD_COUNT controls how many parallel runners the `plan` job emits
        # in its matrix output, and must stay greater than one.
        shard_count = workflow["env"]["SHARD_COUNT"]
        self.assertIsInstance(shard_count, int)
        self.assertGreater(
            shard_count,
            1,
            "SHARD_COUNT must stay greater than 1 so test262 fans out across multiple runners.",
        )

        jobs = workflow["jobs"]
        for job_name, matrix_output in (
            ("rerun-failed", "failed-shard-matrix"),
            ("run-full", "full-shard-matrix"),
        ):
            with self.subTest(job=job_name):
                job = jobs[job_name]
                matrix = job["strategy"]["matrix"]
                include_expr = matrix["include"]
                # The matrix must be sourced from the `plan` job's per-phase
                # shard matrix output, which yields one entry per shard.
                self.assertIn(
                    f"needs.plan.outputs.{matrix_output}",
                    include_expr,
                    f"{job_name} must consume {matrix_output} from the plan job",
                )
                # The runner script must receive both the shard index and the
                # effective shard count so the workload is actually partitioned.
                run_steps = "\n".join(
                    step.get("run", "") for step in job["steps"] if isinstance(step, dict)
                )
                self.assertIn("--shard-count", run_steps)
                self.assertIn("--shard-index", run_steps)
                self.assertIn("${{ matrix.shard-index }}", run_steps)


if __name__ == "__main__":
    unittest.main()
