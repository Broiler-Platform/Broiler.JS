from __future__ import annotations

from pathlib import Path
import re
import unittest

import yaml


REPO_ROOT = Path(__file__).resolve().parents[3]
WORKFLOW_PATH = REPO_ROOT / ".github" / "workflows" / "test262.yml"
ACTION_PATH = REPO_ROOT / ".github" / "actions" / "run-test262-shard" / "action.yml"
COMMIT_HELPER_PATH = REPO_ROOT / "scripts" / "ci-commit-generated-results.sh"


class Test262WorkflowTests(unittest.TestCase):
    """Structural regression tests for the sharded test262 CI topology."""

    @classmethod
    def setUpClass(cls) -> None:
        cls.workflow_text = WORKFLOW_PATH.read_text(encoding="utf-8")
        cls.action_text = ACTION_PATH.read_text(encoding="utf-8")
        cls.workflow = yaml.safe_load(cls.workflow_text)
        cls.action = yaml.safe_load(cls.action_text)

    @staticmethod
    def _events(workflow: dict) -> dict:
        # PyYAML follows YAML 1.1 and resolves the plain key `on` as True.
        return workflow.get("on", workflow.get(True, {}))

    @staticmethod
    def _run_text(job: dict) -> str:
        return "\n".join(
            step.get("run", "") for step in job.get("steps", []) if isinstance(step, dict)
        )

    def test_workflow_remains_manual_only_and_consolidated(self) -> None:
        self.assertEqual(["workflow_dispatch"], list(self._events(self.workflow)))
        workflows = sorted(WORKFLOW_PATH.parent.glob("test262*.yml"))
        self.assertEqual([WORKFLOW_PATH], workflows)

    def test_embedded_python_blocks_compile(self) -> None:
        documents = (
            self.workflow["jobs"].values(),
            [self.action["runs"]],
        )
        blocks: list[str] = []
        for jobs in documents:
            for job in jobs:
                for step in job.get("steps", []):
                    script = step.get("run", "") if isinstance(step, dict) else ""
                    blocks.extend(
                        match.group("body")
                        for match in re.finditer(
                            r"(?:python|python3)(?:\s+-)?\s+<<'PY'\n"
                            r"(?P<body>.*?)\nPY(?:\n|$)",
                            script,
                            flags=re.DOTALL,
                        )
                    )
        self.assertGreaterEqual(len(blocks), 4)
        for index, block in enumerate(blocks):
            with self.subTest(index=index):
                compile(block, f"embedded-test262-{index}.py", "exec")

    def test_dispatch_exposes_every_selection_resource_and_report_input(self) -> None:
        inputs = self._events(self.workflow)["workflow_dispatch"]["inputs"]
        expected = {
            "assembly",
            "subset",
            "features",
            "feature_match",
            "suite_ref",
            "rerun_failed_only",
            "shard_index",
            "test_timeout_seconds",
            "memory_limit_mb",
            "max_workers",
            "shuffle_seed",
            "include_negative",
            "prioritize_fragile",
            "most_common_problems_limit",
            "biggest_problems_limit",
            "timeout_problems_limit",
        }
        self.assertEqual(expected, set(inputs))
        self.assertTrue(inputs["rerun_failed_only"]["default"])
        self.assertEqual("any", inputs["feature_match"]["default"])
        self.assertEqual(["any", "all"], inputs["feature_match"]["options"])
        self.assertEqual(-1, inputs["shard_index"]["default"])
        self.assertEqual(10, inputs["most_common_problems_limit"]["default"])
        self.assertEqual(3, inputs["biggest_problems_limit"]["default"])
        self.assertEqual(10, inputs["timeout_problems_limit"]["default"])

    def test_issue_permission_and_token_fallback_are_configured(self) -> None:
        self.assertEqual("write", self.workflow["permissions"]["contents"])
        self.assertEqual("write", self.workflow["permissions"]["issues"])

        issue_steps = [
            step
            for step in self.workflow["jobs"]["report"]["steps"]
            if step.get("uses") == "actions/github-script@v8"
        ]
        self.assertEqual(3, len(issue_steps))
        self.assertEqual(
            "${{ steps.merge.outputs.timeout_count }}",
            issue_steps[0]["env"]["TIMEOUT_COUNT"],
        )
        self.assertEqual(
            "${{ steps.merge.outputs.configuration_failure_count }}",
            issue_steps[0]["env"]["CONFIGURATION_FAILURE_COUNT"],
        )
        self.assertIn("non-passing test(s)", issue_steps[0]["with"]["script"])
        self.assertIn("timed out", issue_steps[0]["with"]["script"])
        for step in issue_steps:
            self.assertTrue(step["continue-on-error"])
            self.assertEqual(
                "${{ secrets.ISSUE_TOKEN || github.token }}",
                step["with"]["github-token"],
            )
            self.assertIn("github.rest.issues.create", step["with"]["script"])

    def test_numeric_zero_shard_is_not_lost_to_expression_fallback(self) -> None:
        self.assertIn(
            "SELECTED_SHARD_INDEX: ${{ github.event.inputs.shard_index || '-1' }}",
            self.workflow_text,
        )
        self.assertNotIn("inputs.shard_index || -1", self.workflow_text)
        self.assertNotIn("SELECTED_SHARD_INDEX: ${{ inputs.shard_index", self.workflow_text)

    def test_plan_validates_resource_and_report_parameters_before_sharding(self) -> None:
        run_text = self._run_text(self.workflow["jobs"]["plan"])
        self.assertIn("math.isfinite(timeout)", run_text)
        self.assertIn('checked_integer("MEMORY_LIMIT_MB", 0)', run_text)
        self.assertIn('checked_integer("MAX_WORKERS", 1, 256)', run_text)
        self.assertIn('checked_integer("COMMON_LIMIT", 1, 100)', run_text)
        self.assertIn('checked_integer("BIGGEST_LIMIT", 1, 100)', run_text)
        self.assertIn('checked_integer("TIMEOUT_LIMIT", 1, 100)', run_text)

    def test_prepare_resolves_and_caches_an_immutable_suite_commit(self) -> None:
        prepare = self.workflow["jobs"]["prepare"]
        self.assertIn("suite-commit", prepare["outputs"])
        self.assertIn("suite-cache-key", prepare["outputs"])
        run_text = self._run_text(prepare)
        self.assertIn("git", run_text)
        self.assertIn("ls-remote", run_text)
        self.assertIn("suite-commit=", run_text)
        self.assertIn("checkout --detach", run_text)
        self.assertTrue(
            any(step.get("uses") == "actions/cache@v5" for step in prepare["steps"])
        )

    def test_all_initial_and_retry_shards_reuse_the_composite_action(self) -> None:
        jobs = self.workflow["jobs"]
        for job_name in ("rerun-failed", "retry-rerun", "run-full", "retry-full"):
            with self.subTest(job=job_name):
                action_steps = [
                    step
                    for step in jobs[job_name]["steps"]
                    if step.get("uses") == "./.github/actions/run-test262-shard"
                ]
                self.assertEqual(1, len(action_steps))
                checkout = jobs[job_name]["steps"][0]
                self.assertEqual("actions/checkout@v5", checkout["uses"])
                self.assertEqual("recursive", checkout["with"]["submodules"])
                self.assertEqual(2, checkout["with"]["fetch-depth"])
                self.assertEqual(360, jobs[job_name]["timeout-minutes"])
                self.assertTrue(jobs[job_name]["continue-on-error"])
                self.assertFalse(jobs[job_name]["strategy"]["fail-fast"])

    def test_full_and_retry_forward_all_selection_and_runner_parameters(self) -> None:
        jobs = self.workflow["jobs"]
        required = {
            "phase",
            "shard-index",
            "shard-count",
            "suite-commit",
            "suite-root",
            "suite-cache-key",
            "assembly",
            "subset",
            "features",
            "feature-match",
            "rerun-failed",
            "failed-tests-file",
            "test-timeout-seconds",
            "memory-limit-mb",
            "max-workers",
            "shuffle-seed",
            "include-negative",
            "prioritize-fragile",
        }
        for job_name in ("run-full", "retry-full"):
            action_step = next(
                step
                for step in jobs[job_name]["steps"]
                if step.get("uses") == "./.github/actions/run-test262-shard"
            )
            self.assertTrue(required.issubset(action_step["with"]), job_name)
            self.assertIn("github.event.inputs.subset", action_step["with"]["subset"])
            self.assertIn("github.event.inputs.features", action_step["with"]["features"])
            self.assertIn(
                "github.event.inputs.feature_match", action_step["with"]["feature-match"]
            )

    def test_composite_action_wires_filters_without_shell_interpolation(self) -> None:
        expected_inputs = {
            "phase",
            "shard-index",
            "shard-count",
            "attempt-suffix",
            "suite-commit",
            "suite-root",
            "suite-cache-key",
            "assembly",
            "subset",
            "features",
            "feature-match",
            "rerun-failed",
            "failed-tests-file",
            "test-timeout-seconds",
            "memory-limit-mb",
            "max-workers",
            "shuffle-seed",
            "include-negative",
            "prioritize-fragile",
        }
        self.assertEqual(expected_inputs, set(self.action["inputs"]))
        self.assertIn('ARGS+=(--subset "$SUBSET")', self.action_text)
        self.assertIn('ARGS+=(--features "$FEATURES")', self.action_text)
        self.assertIn('--feature-match "$FEATURE_MATCH"', self.action_text)
        self.assertIn('--selection-label "$SELECTION_LABEL"', self.action_text)
        self.assertIn('--runner-os "${RUNNER_OS:-unknown}"', self.action_text)
        self.assertIn('--runner-arch "${RUNNER_ARCH:-unknown}"', self.action_text)
        self.assertIn('--dotnet-version "$DOTNET_VERSION"', self.action_text)
        self.assertIn("--no-summary-stdout", self.action_text)
        self.assertIn("> /dev/null", self.action_text)
        self.assertNotIn("${{ inputs.features }}\"", self._runner_script())

    def _runner_script(self) -> str:
        return next(
            step["run"]
            for step in self.action["runs"]["steps"]
            if step.get("id") == "test262"
        )

    def test_composite_emits_retry_aware_report_and_status_names(self) -> None:
        self.assertIn(
            'STEM="test262-${PHASE}-shard-${SHARD_INDEX}${ATTEMPT_SUFFIX}"',
            self.action_text,
        )
        self.assertIn('f"{stem}-status.json"', self.action_text)
        self.assertIn("RunnerDidNotProduceReport", self.action_text)
        self.assertIn("actions/upload-artifact@v6", self.action_text)

    def test_preflight_and_full_each_retry_only_incomplete_shards(self) -> None:
        jobs = self.workflow["jobs"]
        expected_jobs = {
            "validate",
            "plan",
            "prepare",
            "rerun-failed",
            "retry-plan-rerun",
            "retry-rerun",
            "evaluate-rerun",
            "run-full",
            "retry-plan-full",
            "retry-full",
            "report",
            "persist-failed",
            "conclusion",
        }
        self.assertEqual(expected_jobs, set(jobs))
        for plan_job in ("retry-plan-rerun", "retry-plan-full"):
            self.assertIn("has-retries", jobs[plan_job]["outputs"])
            self.assertIn("retry-matrix", jobs[plan_job]["outputs"])
            self.assertIn("--expected-shards", self._run_text(jobs[plan_job]))
            self.assertIn("--github-output", self._run_text(jobs[plan_job]))
        self.assertIn(
            "needs.evaluate-rerun.outputs.suite-passed == 'true'",
            jobs["run-full"]["if"],
        )
        self.assertIn(
            "steps.merge.outputs.suite_passed != 'true'",
            str(jobs["evaluate-rerun"]["steps"]),
        )

    def test_validation_suite_gates_planning_and_preparation(self) -> None:
        jobs = self.workflow["jobs"]
        validate = jobs["validate"]
        run_text = self._run_text(validate)
        self.assertIn("PyYAML==6.0.2", run_text)
        self.assertIn("unittest discover -s scripts/compliance/tests", run_text)
        self.assertEqual("validate", jobs["plan"]["needs"])
        self.assertEqual("validate", jobs["prepare"]["needs"])

    def test_report_selects_one_authoritative_phase_and_emits_three_reports(self) -> None:
        report = self.workflow["jobs"]["report"]
        self.assertIn("authoritative-phase", report["outputs"])
        run_text = self._run_text(report)
        for flag in (
            "--merged-json",
            "--issue-md",
            "--biggest-issue-md",
            "--timeout-issue-md",
            "--problem-limit",
            "--biggest-problem-limit",
            "--timeout-limit",
            "--run-url",
            "--artifact-name",
            "--broiler-commit",
            "--github-output",
        ):
            self.assertIn(flag, run_text)
        self.assertTrue(
            any(
                step.get("uses") == "actions/upload-artifact@v6"
                and step.get("with", {}).get("name") == "test262-merged"
                for step in report["steps"]
            )
        )

    def test_persistence_is_canonical_uncancelled_and_scope_aware(self) -> None:
        persist = self.workflow["jobs"]["persist-failed"]
        condition = persist["if"]
        self.assertIn("!cancelled()", condition)
        self.assertIn("ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e", condition)
        self.assertIn("include_negative != 'true'", condition)
        self.assertIn("configuration-failure-count == '0'", condition)
        self.assertFalse(persist["concurrency"]["cancel-in-progress"])
        self.assertIn(
            "test262-failure-manifest-",
            persist["concurrency"]["group"],
        )
        run_text = self._run_text(persist)
        self.assertIn("git merge-base --is-ancestor", run_text)
        self.assertIn("git diff --name-only", run_text)
        self.assertIn("--merged-input", run_text)
        self.assertIn("--merge-into", run_text)
        self.assertIn("--expected-base", run_text)
        self.assertIn("ci-commit-generated-results.sh", run_text)
        artifact_step = next(
            step
            for step in persist["steps"]
            if step.get("uses") == "actions/download-artifact@v7"
        )
        self.assertEqual("test262-merged", artifact_step["with"]["name"])
        self.assertNotIn("continue-on-error", artifact_step)
        helper = COMMIT_HELPER_PATH.read_text(encoding="utf-8")
        self.assertIn("git fetch origin", helper)
        self.assertIn("EXPECTED_BASE", helper)
        self.assertIn("skip_stale_results", helper)

    def test_terminal_conclusion_uses_only_authoritative_full_merge(self) -> None:
        conclusion = self.workflow["jobs"]["conclusion"]
        self.assertEqual(
            ["plan", "report", "persist-failed"],
            conclusion["needs"],
        )
        self.assertIn("always()", conclusion["if"])
        run_text = self._run_text(conclusion)
        self.assertIn('"$REPORT_RESULT" != "success"', run_text)
        self.assertIn('"$AUTHORITATIVE_PHASE" != "full"', run_text)
        self.assertIn('"$SUITE_PASSED" != "true"', run_text)
        self.assertIn('"$PERSIST_RESULT" = "failure"', run_text)

    def test_latest_action_majors_match_the_wpt_runner(self) -> None:
        combined = self.workflow_text + self.action_text
        for use in (
            "actions/checkout@v5",
            "actions/setup-dotnet@v5",
            "actions/setup-python@v6",
            "actions/cache@v5",
            "actions/download-artifact@v7",
            "actions/upload-artifact@v6",
            "actions/github-script@v8",
        ):
            self.assertIn(use, combined)
        for old in (
            "actions/checkout@v4",
            "actions/setup-dotnet@v4",
            "actions/setup-python@v5",
            "actions/cache@v4",
            "actions/upload-artifact@v4",
            "actions/github-script@v7",
        ):
            self.assertNotIn(old, combined)


if __name__ == "__main__":
    unittest.main()
