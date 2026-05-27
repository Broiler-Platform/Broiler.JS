from __future__ import annotations

from pathlib import Path
import unittest


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

        self.assertIn(
            "if: always() && (needs.plan.outputs.should-rerun-failed != 'true' || needs.rerun-failed.result == 'success')",
            workflow_text,
        )

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

    def test_logparser_creates_highest_impact_issue(self) -> None:
        workflow_text = self.workflow_path.read_text(encoding="utf-8")
        self.assertIn("--highest-impact-problem", workflow_text)


if __name__ == "__main__":
    unittest.main()
