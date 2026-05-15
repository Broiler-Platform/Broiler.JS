from __future__ import annotations

from pathlib import Path
import unittest


class Test262FullScriptHostWorkflowTests(unittest.TestCase):
    def test_run_full_job_uses_always_to_survive_skipped_rerun_dependency(self) -> None:
        workflow_path = (
            Path(__file__).resolve().parents[3]
            / ".github"
            / "workflows"
            / "test262-full-script-host.yml"
        )
        workflow_text = workflow_path.read_text(encoding="utf-8")

        self.assertIn(
            "if: always() && (needs.plan.outputs.should-rerun-failed != 'true' || needs.rerun-failed.result == 'success')",
            workflow_text,
        )


if __name__ == "__main__":
    unittest.main()
