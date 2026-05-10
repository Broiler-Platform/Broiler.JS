#!/usr/bin/env python3

from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_BROILER_DLL = str(
    REPO_ROOT / "Broiler.JS/Broiler.JavaScript/bin/Debug/net8.0/BroilerJS.dll"
)


def run_process(command: list[str], program: str, suffix: str = ".js") -> dict[str, object]:
    with tempfile.NamedTemporaryFile(
        "w",
        suffix=suffix,
        delete=False,
        dir=tempfile.gettempdir(),
        encoding="utf-8",
    ) as handle:
        handle.write(program)
        path = handle.name

    try:
        process = subprocess.run(
            command + [path],
            capture_output=True,
            text=True,
            check=False,
        )
    finally:
        os.unlink(path)

    stdout_lines = [line.strip() for line in process.stdout.splitlines() if line.strip()]
    stderr_lines = [line.strip() for line in process.stderr.splitlines() if line.strip()]
    output = stdout_lines[-1] if stdout_lines else ""
    return {
        "exitCode": process.returncode,
        "stdout": process.stdout,
        "stderr": process.stderr,
        "output": output,
        "stderrSummary": stderr_lines[-1] if stderr_lines else "",
    }


def run_broiler(broiler_dll: str, scenario: dict[str, object]) -> dict[str, object]:
    expression = scenario.get("expression")
    program = scenario.get("broilerProgram")
    if program is None:
        program = f"{expression}\n"
    return run_process(["dotnet", broiler_dll, "--script-host"], str(program))


def run_node(node_bin: str, scenario: dict[str, object]) -> dict[str, object]:
    expression = scenario.get("expression")
    program = scenario.get("nodeProgram")
    if program is None:
        program = f"console.log(String({expression}));\n"
    return run_process([node_bin], str(program))


def run_engine262(engine262_bin: str, scenario: dict[str, object]) -> dict[str, object]:
    expression = scenario.get("expression")
    program = scenario.get("engine262Program")
    if program is None:
        program = f"print(String({expression}));\n"
    return run_process([engine262_bin], str(program))


def mark_result(result: dict[str, object], expected: str) -> dict[str, object]:
    success = result["exitCode"] == 0 and result["output"] == expected
    return {
        "passed": success,
        "output": result["output"],
        "exitCode": result["exitCode"],
        "stderrSummary": result["stderrSummary"],
    }


def read_version(command: list[str]) -> str:
    process = subprocess.run(command, capture_output=True, text=True, check=False)
    if process.returncode != 0:
        return "unavailable"
    return next((line.strip() for line in process.stdout.splitlines() if line.strip()), "unknown")


def summarize(results: list[dict[str, object]], engine: str) -> dict[str, object]:
    passed = sum(1 for result in results if result[engine]["passed"])
    failed = len(results) - passed
    return {"passed": passed, "failed": failed, "executed": len(results)}


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Compare Broiler, Node/V8, and engine262 on shared compliance scenarios."
    )
    parser.add_argument(
        "--manifest",
        required=True,
        help="Path to the engine comparison manifest JSON file",
    )
    parser.add_argument(
        "--broiler-dll",
        default=DEFAULT_BROILER_DLL,
        help="Path to BroilerJS.dll",
    )
    parser.add_argument(
        "--node-bin",
        default=shutil.which("node") or "node",
        help="Path to the Node.js binary",
    )
    parser.add_argument(
        "--engine262-bin",
        required=True,
        help="Path to the engine262 CLI binary",
    )
    parser.add_argument(
        "--output",
        help="Optional path for machine-readable JSON output",
    )
    args = parser.parse_args()

    scenarios = json.loads(Path(args.manifest).read_text(encoding="utf-8"))
    detailed_results: list[dict[str, object]] = []
    for scenario in scenarios:
        expected = str(scenario["expected"])
        detailed_results.append(
            {
                "name": scenario["name"],
                "category": scenario["category"],
                "expected": expected,
                "broiler": mark_result(run_broiler(args.broiler_dll, scenario), expected),
                "node": mark_result(run_node(args.node_bin, scenario), expected),
                "engine262": mark_result(run_engine262(args.engine262_bin, scenario), expected),
            }
        )

    summary = {
        "manifest": args.manifest,
        "broilerDll": args.broiler_dll,
        "nodeBin": args.node_bin,
        "engine262Bin": args.engine262_bin,
        "versions": {
            "node": read_version([args.node_bin, "--version"]),
            "engine262": read_version([args.engine262_bin, "--help"]),
        },
        "scenarios": detailed_results,
        "totals": {
            "broiler": summarize(detailed_results, "broiler"),
            "node": summarize(detailed_results, "node"),
            "engine262": summarize(detailed_results, "engine262"),
        },
    }

    text = json.dumps(summary, indent=2)
    if args.output:
        output_path = Path(args.output)
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_text(text, encoding="utf-8")

    print(text)
    return 0


if __name__ == "__main__":
    sys.exit(main())
