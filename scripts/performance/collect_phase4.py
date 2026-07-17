#!/usr/bin/env python3
"""Publish and measure the Phase 4 selective sample host."""

from __future__ import annotations

import argparse
import json
import platform
import shutil
import subprocess
from datetime import datetime, timezone
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
PROJECT = ROOT / "Broiler.JS" / "samples" / "Broiler.JavaScript.StartupHost" / "Broiler.JavaScript.StartupHost.csproj"


def run(command: list[str]) -> subprocess.CompletedProcess[str]:
    return subprocess.run(command, cwd=ROOT, text=True, capture_output=True, check=False)


def directory_metrics(path: Path) -> dict[str, int]:
    files = [item for item in path.rglob("*") if item.is_file()]
    return {"fileCount": len(files), "totalBytes": sum(item.stat().st_size for item in files)}


def run_host(path: Path) -> dict[str, object]:
    executable = path / ("Broiler.JavaScript.StartupHost.exe" if platform.system() == "Windows" else "Broiler.JavaScript.StartupHost")
    if executable.exists():
        completed = run([str(executable)])
    else:
        completed = run(["dotnet", str(path / "Broiler.JavaScript.StartupHost.dll")])
    lines = [line for line in completed.stdout.splitlines() if line.strip().startswith("{")]
    return {
        "exitCode": completed.returncode,
        "measurement": json.loads(lines[-1]) if lines else None,
        "stderr": completed.stderr[-4000:],
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--configuration", default="Release")
    parser.add_argument("--rid", default="win-x64" if platform.system() == "Windows" else "linux-x64")
    parser.add_argument("--output", type=Path, default=ROOT / "artifacts" / "phase4")
    parser.add_argument("--report", type=Path)
    args = parser.parse_args()

    variants = [
        ("minimal-framework", "Minimal", False, False, False),
        ("full-framework", "Full", False, False, False),
        ("full-readytorun", "Full", True, False, False),
        ("full-trimmed", "Full", False, True, True),
    ]
    output_root = args.output.resolve()
    output_root.mkdir(parents=True, exist_ok=True)
    results: list[dict[str, object]] = []

    for name, profile, ready_to_run, trimmed, self_contained in variants:
        destination = (output_root / name).resolve()
        if output_root not in destination.parents:
            raise RuntimeError(f"Refusing to replace output outside {output_root}: {destination}")
        if destination.exists():
            shutil.rmtree(destination)
        command = [
            "dotnet", "publish", str(PROJECT),
            "-c", args.configuration,
            "-r", args.rid,
            "--self-contained", str(self_contained).lower(),
            "-p:BroilerHostProfile=" + profile,
            "-p:PublishReadyToRun=" + str(ready_to_run).lower(),
            "-p:PublishTrimmed=" + str(trimmed).lower(),
            "-o", str(destination),
        ]
        completed = run(command)
        item: dict[str, object] = {
            "name": name,
            "profile": profile.lower(),
            "readyToRun": ready_to_run,
            "trimmed": trimmed,
            "selfContained": self_contained,
            "publishExitCode": completed.returncode,
            "publishTail": (completed.stdout + completed.stderr)[-8000:],
        }
        if completed.returncode == 0:
            item["files"] = directory_metrics(destination)
            item["host"] = run_host(destination)
            host = item["host"]
            measurement = host["measurement"]
            validation_errors: list[str] = []
            if host["exitCode"] != 0:
                validation_errors.append(f"host exited with {host['exitCode']}")
            if measurement is None:
                validation_errors.append("host emitted no JSON measurement")
            else:
                diagnostics = measurement["diagnostics"]
                host_results = measurement["results"]
                if diagnostics["CompatibilityAssemblyProbes"] != 0:
                    validation_errors.append("explicit host used compatibility assembly probes")
                if profile == "Full":
                    if host_results["featureResult"] != "42|function|object":
                        validation_errors.append("full feature result was incorrect")
                    if host_results["featureAssemblyLoadedBeforeUse"]:
                        validation_errors.append("sample feature assembly loaded before realization")
                    if not host_results["featureAssemblyLoadedAfterUse"]:
                        validation_errors.append("sample feature assembly did not load on realization")
                else:
                    if host_results["featureResult"] != "undefined|undefined":
                        validation_errors.append("minimal host unexpectedly exposed Intl or Temporal")
                    if host_results["featureAssemblyLoadedBeforeUse"] or host_results["featureAssemblyLoadedAfterUse"]:
                        validation_errors.append("minimal host unexpectedly loaded the sample feature assembly")
            item["validationErrors"] = validation_errors
        results.append(item)

    report = {
        "schemaVersion": "1.0.0",
        "timestampUtc": datetime.now(timezone.utc).isoformat(),
        "rid": args.rid,
        "configuration": args.configuration,
        "variants": results,
    }
    report_path = args.report or (args.output / "phase4-report.json")
    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(report_path)
    return 0 if all(
        item["publishExitCode"] == 0 and not item.get("validationErrors", ["publish failed"])
        for item in results
    ) else 1


if __name__ == "__main__":
    raise SystemExit(main())
