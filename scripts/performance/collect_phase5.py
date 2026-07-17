#!/usr/bin/env python3
"""Regenerate, publish, and validate the Phase 5 Native AOT sample."""

from __future__ import annotations

import argparse
import json
import platform
import shutil
import subprocess
from datetime import datetime, timezone
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
SOURCE_ROOT = ROOT / "Broiler.JS"
COMPILER_PROJECT = SOURCE_ROOT / "Broiler.JavaScript.Portable.Compiler" / "Broiler.JavaScript.Portable.Compiler.csproj"
SAMPLE_ROOT = SOURCE_ROOT / "samples" / "Broiler.JavaScript.NativeAotSample"
SAMPLE_PROJECT = SAMPLE_ROOT / "Broiler.JavaScript.NativeAotSample.csproj"
SAMPLE_SOURCE = SAMPLE_ROOT / "fibonacci.js"
CHECKED_IN_PROGRAM = SAMPLE_ROOT / "FibonacciProgram.g.cs"


def run(command: list[str]) -> subprocess.CompletedProcess[str]:
    return subprocess.run(command, cwd=ROOT, text=True, capture_output=True, check=False)


def directory_metrics(path: Path) -> dict[str, int]:
    files = [item for item in path.rglob("*") if item.is_file()]
    return {
        "fileCount": len(files),
        "totalBytes": sum(item.stat().st_size for item in files),
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--configuration", default="Release")
    parser.add_argument("--rid", default="win-x64" if platform.system() == "Windows" else "linux-x64")
    parser.add_argument("--output", type=Path, default=ROOT / "artifacts" / "phase5")
    parser.add_argument("--report", type=Path)
    args = parser.parse_args()

    output_root = args.output.resolve()
    generated = output_root / "generated" / "FibonacciProgram.g.cs"
    publish = output_root / "native-aot"
    output_root.mkdir(parents=True, exist_ok=True)
    if publish.exists():
        if output_root not in publish.resolve().parents:
            raise RuntimeError(f"Refusing to replace output outside {output_root}: {publish}")
        shutil.rmtree(publish)
    generated.parent.mkdir(parents=True, exist_ok=True)

    compile_result = run([
        "dotnet", "run", "--project", str(COMPILER_PROJECT),
        "-c", args.configuration, "--",
        str(SAMPLE_SOURCE), str(generated),
        "Broiler.JavaScript.NativeAotSample", "FibonacciProgram",
    ])
    deterministic = (
        compile_result.returncode == 0
        and generated.exists()
        and generated.read_text(encoding="utf-8")
        == CHECKED_IN_PROGRAM.read_text(encoding="utf-8")
    )

    publish_result = run([
        "dotnet", "publish", str(SAMPLE_PROJECT),
        "-c", args.configuration,
        "-r", args.rid,
        "-o", str(publish),
    ])

    executable_name = "Broiler.JavaScript.NativeAotSample.exe" if platform.system() == "Windows" else "Broiler.JavaScript.NativeAotSample"
    executable = publish / executable_name
    host_result = run([str(executable)]) if publish_result.returncode == 0 and executable.exists() else None
    measurement = None
    if host_result is not None:
        lines = [line for line in host_result.stdout.splitlines() if line.strip().startswith("{")]
        measurement = json.loads(lines[-1]) if lines else None

    validation_errors: list[str] = []
    if compile_result.returncode != 0:
        validation_errors.append(f"offline compiler exited with {compile_result.returncode}")
    elif not deterministic:
        validation_errors.append("offline compiler output differs from the checked-in program")
    if publish_result.returncode != 0:
        validation_errors.append(f"Native AOT publish exited with {publish_result.returncode}")
    elif not executable.exists():
        validation_errors.append("Native AOT publish emitted no executable")
    if host_result is None:
        validation_errors.append("Native AOT host was not run")
    elif host_result.returncode != 0:
        validation_errors.append(f"Native AOT host exited with {host_result.returncode}")
    if measurement is None:
        validation_errors.append("Native AOT host emitted no JSON measurement")
    else:
        if measurement.get("program") != "fibonacci" or measurement.get("result") != 9_227_465:
            validation_errors.append("Native AOT host returned the wrong program result")
        if measurement.get("dynamicCodeSupported") is not False or measurement.get("dynamicCodeCompiled") is not False:
            validation_errors.append("Native AOT host reported dynamic-code support")

    report = {
        "schemaVersion": "1.0.0",
        "timestampUtc": datetime.now(timezone.utc).isoformat(),
        "rid": args.rid,
        "configuration": args.configuration,
        "offlineCompiler": {
            "exitCode": compile_result.returncode,
            "matchesCheckedInProgram": deterministic,
            "output": str(generated),
            "tail": (compile_result.stdout + compile_result.stderr)[-4000:],
        },
        "nativeAot": {
            "publishExitCode": publish_result.returncode,
            "files": directory_metrics(publish) if publish.exists() else None,
            "hostExitCode": host_result.returncode if host_result is not None else None,
            "measurement": measurement,
            "publishTail": (publish_result.stdout + publish_result.stderr)[-8000:],
            "hostStderr": host_result.stderr[-4000:] if host_result is not None else "",
        },
        "validationErrors": validation_errors,
    }
    report_path = args.report or (output_root / "phase5-report.json")
    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(report_path)
    return 0 if not validation_errors else 1


if __name__ == "__main__":
    raise SystemExit(main())
