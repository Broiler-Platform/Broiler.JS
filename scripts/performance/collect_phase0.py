#!/usr/bin/env python3
"""Collect reproducible Phase 0 performance, build, package, IL, and publish evidence."""

from __future__ import annotations

import argparse
import datetime as dt
import json
import os
from pathlib import Path
import platform
import shutil
import statistics
import subprocess
import sys
import time
import xml.etree.ElementTree as ET


REPO_ROOT = Path(__file__).resolve().parents[2]
CONFIG_PATH = REPO_ROOT / "eng" / "performance" / "phase0.json"


class Collector:
    def __init__(self, output: Path) -> None:
        self.output = output
        self.output.mkdir(parents=True, exist_ok=True)
        self.commands: list[dict[str, object]] = []

    def run(
        self,
        argv: list[str],
        *,
        name: str,
        env: dict[str, str] | None = None,
        check: bool = True,
    ) -> subprocess.CompletedProcess[str]:
        log_path = self.output / f"{name}.log"
        print(f"[{name}] {' '.join(argv)}", flush=True)
        started = time.perf_counter()
        completed = subprocess.run(
            argv,
            cwd=REPO_ROOT,
            env=env,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            errors="replace",
        )
        elapsed = time.perf_counter() - started
        log_path.write_text(completed.stdout, encoding="utf-8")
        self.commands.append(
            {
                "argv": argv,
                "exitCode": completed.returncode,
                "elapsedSeconds": elapsed,
                "log": log_path.relative_to(self.output).as_posix(),
            }
        )
        if completed.returncode != 0:
            tail = "\n".join(completed.stdout.splitlines()[-30:])
            print(tail, file=sys.stderr, flush=True)
            if check:
                raise RuntimeError(f"Command '{name}' failed with exit code {completed.returncode}")
        return completed


def utc_now() -> str:
    return dt.datetime.now(dt.timezone.utc).isoformat().replace("+00:00", "Z")


def load_config() -> dict[str, object]:
    config = json.loads(CONFIG_PATH.read_text(encoding="utf-8"))
    for key in ("schemaVersion", "benchmark", "eventPipe", "build", "publish", "platformMatrix"):
        if key not in config:
            raise ValueError(f"Missing required Phase 0 configuration key: {key}")
    return config


def git_value(*args: str) -> str:
    completed = subprocess.run(
        ["git", *args],
        cwd=REPO_ROOT,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.DEVNULL,
        check=True,
    )
    return completed.stdout.strip()


def repository_metadata() -> dict[str, object]:
    return {
        "commit": git_value("rev-parse", "HEAD"),
        "dirty": bool(git_value("status", "--porcelain")),
    }


def runtime_identifier() -> str:
    system = {"Windows": "win", "Linux": "linux", "Darwin": "osx"}.get(platform.system(), platform.system().lower())
    machine = platform.machine().lower()
    architecture = {"amd64": "x64", "x86_64": "x64", "aarch64": "arm64"}.get(machine, machine)
    return f"{system}-{architecture}"


def environment_metadata(dotnet_info: str) -> dict[str, object]:
    version = subprocess.run(
        ["dotnet", "--version"],
        cwd=REPO_ROOT,
        text=True,
        stdout=subprocess.PIPE,
        check=True,
    ).stdout.strip()
    return {
        "runtime": version,
        "os": platform.platform(),
        "rid": runtime_identifier(),
        "architecture": platform.machine(),
        "processor": platform.processor() or os.environ.get("PROCESSOR_IDENTIFIER", "unknown"),
        "processorCount": os.cpu_count() or 1,
        "serverGc": True,
        "tieredPgo": os.environ.get("DOTNET_TieredPGO", "runtime-default"),
        "avx2Override": os.environ.get("DOTNET_EnableAVX2", "runtime-default"),
        "dotnetInfo": dotnet_info,
    }


def benchmark_dll(config: dict[str, object]) -> Path:
    benchmark = config["benchmark"]
    assert isinstance(benchmark, dict)
    project = REPO_ROOT / str(benchmark["project"])
    framework = str(benchmark["targetFramework"])
    configuration = str(benchmark["configuration"])
    return project.parent / "bin" / configuration / framework / f"{project.stem}.dll"


def parse_benchmark_reports(directory: Path) -> list[dict[str, object]]:
    benchmarks: list[dict[str, object]] = []
    for report in sorted(directory.rglob("*-report-full.json")):
        data = json.loads(report.read_text(encoding="utf-8-sig"))
        for item in data.get("Benchmarks", []):
            statistics_data = item.get("Statistics") or {}
            memory = item.get("Memory") or {}
            benchmarks.append(
                {
                    "name": item.get("FullName") or item.get("DisplayInfo") or item.get("Method"),
                    "meanNanoseconds": statistics_data.get("Mean"),
                    "errorNanoseconds": statistics_data.get("StandardError"),
                    "standardDeviationNanoseconds": statistics_data.get("StandardDeviation"),
                    "allocatedBytesPerOperation": memory.get("BytesAllocatedPerOperation"),
                    "report": report.relative_to(directory).as_posix(),
                }
            )
    return benchmarks


def collect_lifecycle(collector: Collector, dll: Path, samples: int, repetition: int) -> list[dict[str, object]]:
    results: list[dict[str, object]] = []
    for sample in range(samples):
        completed = collector.run(
            ["dotnet", str(dll), "--lifecycle-child", "all"],
            name=f"lifecycle-r{repetition + 1}-s{sample + 1}",
        )
        json_line = next((line for line in reversed(completed.stdout.splitlines()) if line.startswith("{")), None)
        if json_line is None:
            raise RuntimeError("Lifecycle child did not emit JSON")
        results.append(json.loads(json_line))
    return results


def collect_benchmark_repetition(
    collector: Collector,
    config: dict[str, object],
    profile_name: str,
    profile: dict[str, object],
    repetition: int,
    filters_override: list[str] | None,
) -> dict[str, object]:
    dll = benchmark_dll(config)
    benchmark_artifacts = collector.output / f"benchmark-r{repetition + 1}"
    environment = os.environ.copy()
    environment["BROILER_BENCHMARK_PROFILE"] = profile_name
    environment["BROILER_BENCHMARK_ARTIFACTS"] = str(benchmark_artifacts)
    filters = filters_override or [str(value) for value in profile["filters"]]
    collector.run(
        ["dotnet", str(dll), "--filter", *filters],
        name=f"benchmark-r{repetition + 1}",
        env=environment,
    )

    lifecycle = collect_lifecycle(
        collector,
        dll,
        int(profile["lifecycleSamplesPerRepetition"]),
        repetition,
    )
    return {
        "repetition": repetition + 1,
        "profile": profile_name,
        "lifecycle": lifecycle,
        "benchmarks": parse_benchmark_reports(benchmark_artifacts),
        "artifactDirectory": benchmark_artifacts.relative_to(collector.output).as_posix(),
    }


def collect_eventpipe(collector: Collector, config: dict[str, object], dll: Path) -> list[dict[str, object]]:
    executable = shutil.which("dotnet-trace")
    if executable is None:
        raise RuntimeError("dotnet-trace is required for --include-eventpipe. Install it with 'dotnet tool install -g dotnet-trace'.")

    eventpipe = config["eventPipe"]
    assert isinstance(eventpipe, dict)
    traces: list[dict[str, object]] = []
    trace_directory = collector.output / "eventpipe"
    trace_directory.mkdir(parents=True, exist_ok=True)
    for scenario, iterations in eventpipe["scenarios"].items():
        output = trace_directory / f"{scenario}.nettrace"
        completed = collector.run(
            [
                executable,
                "collect",
                "--output",
                str(output),
                "--providers",
                str(eventpipe["provider"]),
                "--",
                "dotnet",
                str(dll),
                "--profile",
                str(scenario),
                str(iterations),
            ],
            name=f"eventpipe-{scenario}",
            check=False,
        )
        traces.append(
            {
                "scenario": scenario,
                "iterations": iterations,
                "provider": eventpipe["provider"],
                "path": output.relative_to(collector.output).as_posix(),
                "bytes": output.stat().st_size if output.exists() else 0,
                "succeeded": completed.returncode == 0,
            }
        )
    return traces


def collect_build_baselines(collector: Collector, config: dict[str, object]) -> dict[str, object]:
    build = config["build"]
    assert isinstance(build, dict)
    project = str(REPO_ROOT / str(build["project"]))
    configuration = str(build["configuration"])
    touch_file = REPO_ROOT / str(build["touchFile"])
    build_directory = collector.output / "build"
    build_directory.mkdir(parents=True, exist_ok=True)

    collector.run(["dotnet", "clean", project, "-c", configuration, "-m:1"], name="build-clean-prepare")
    results: dict[str, object] = {}
    for name in ("clean", "noop"):
        before = len(collector.commands)
        collector.run(
            ["dotnet", "build", project, "-c", configuration, "-m:1", f"/bl:{build_directory / (name + '.binlog')}"],
            name=f"build-{name}",
        )
        results[name] = collector.commands[before]["elapsedSeconds"]

    original_stat = touch_file.stat()
    try:
        touch_file.touch()
        before = len(collector.commands)
        collector.run(
            ["dotnet", "build", project, "-c", configuration, "-m:1", f"/bl:{build_directory / 'one-file.binlog'}"],
            name="build-one-file",
        )
        results["oneFile"] = collector.commands[before]["elapsedSeconds"]
    finally:
        os.utime(touch_file, ns=(original_stat.st_atime_ns, original_stat.st_mtime_ns))

    results["project"] = str(build["project"])
    results["touchFile"] = str(build["touchFile"])
    return results


def read_project_graph() -> dict[str, object]:
    projects: list[dict[str, object]] = []
    for project in sorted(REPO_ROOT.rglob("*.csproj")):
        if "bin" in project.parts or "obj" in project.parts:
            continue
        root = ET.parse(project).getroot()
        references = [
            element.attrib["Include"].replace("\\", "/")
            for element in root.findall(".//ProjectReference")
            if "Include" in element.attrib
        ]
        packages = [
            {
                "name": element.attrib["Include"],
                "version": element.attrib.get("Version") or (element.findtext("Version") or ""),
                "privateAssets": element.attrib.get("PrivateAssets") or (element.findtext("PrivateAssets") or ""),
            }
            for element in root.findall(".//PackageReference")
            if "Include" in element.attrib
        ]
        projects.append(
            {
                "path": project.relative_to(REPO_ROOT).as_posix(),
                "projectReferences": references,
                "packageReferences": packages,
            }
        )
    return {"projects": projects}


def collect_package_graph(collector: Collector) -> dict[str, object]:
    graph = read_project_graph()
    path = collector.output / "package-project-graph.json"
    path.write_text(json.dumps(graph, indent=2), encoding="utf-8")
    completed = collector.run(
        ["dotnet", "list", "Broiler.JS.slnx", "package", "--include-transitive", "--format", "json"],
        name="package-transitive",
        check=False,
    )
    machine_path = collector.output / "package-transitive.json"
    if completed.returncode == 0:
        machine_path.write_text(completed.stdout, encoding="utf-8")
    return {
        "projectGraph": path.relative_to(collector.output).as_posix(),
        "transitiveGraph": machine_path.relative_to(collector.output).as_posix() if machine_path.exists() else None,
        "projectCount": len(graph["projects"]),
    }


def collect_assembly_metrics(collector: Collector, dll: Path) -> list[dict[str, object]]:
    assemblies: list[dict[str, object]] = []
    output_directory = dll.parent
    candidates = sorted(
        path
        for path in output_directory.glob("*.dll")
        if path.name.startswith(("Broiler.", "Unicode"))
    )
    for index, assembly in enumerate(candidates):
        completed = collector.run(
            ["dotnet", str(dll), "--assembly-metrics", str(assembly)],
            name=f"assembly-{index + 1:02d}-{assembly.stem}",
        )
        json_line = next(line for line in reversed(completed.stdout.splitlines()) if line.startswith("{"))
        metric = json.loads(json_line)
        metric["path"] = assembly.name
        assemblies.append(metric)
    path = collector.output / "assembly-metrics.json"
    path.write_text(json.dumps(assemblies, indent=2), encoding="utf-8")
    return assemblies


def collect_sparse_storage_metrics(collector: Collector, dll: Path) -> dict[str, object]:
    completed = collector.run(
        ["dotnet", str(dll), "--sparse-metrics"],
        name="sparse-storage-metrics",
    )
    json_line = next((line for line in reversed(completed.stdout.splitlines()) if line.startswith("{")), None)
    if json_line is None:
        raise RuntimeError("Sparse storage metrics did not emit JSON")
    result = json.loads(json_line)
    path = collector.output / "sparse-storage-metrics.json"
    path.write_text(json.dumps(result, indent=2), encoding="utf-8")
    return result


def collect_publish_baselines(
    collector: Collector,
    config: dict[str, object],
    rid: str,
) -> list[dict[str, object]]:
    publish = config["publish"]
    assert isinstance(publish, dict)
    project = str(REPO_ROOT / str(publish["project"]))
    results: list[dict[str, object]] = []
    for variant, properties in publish["variants"].items():
        output = collector.output / "publish" / rid / str(variant)
        output.mkdir(parents=True, exist_ok=True)
        argv = ["dotnet", "publish", project, "-c", "Release", "-r", rid, "-o", str(output)]
        for key, value in properties.items():
            argv.append(f"-p:{key}={str(value).lower() if isinstance(value, bool) else value}")
        completed = collector.run(argv, name=f"publish-{rid}-{variant}", check=False)
        files = [path for path in output.rglob("*") if path.is_file()]
        results.append(
            {
                "rid": rid,
                "variant": variant,
                "properties": properties,
                "succeeded": completed.returncode == 0,
                "fileCount": len(files),
                "totalBytes": sum(path.stat().st_size for path in files),
                "directory": output.relative_to(collector.output).as_posix(),
            }
        )
    return results


def relative_difference(left: float, right: float) -> float:
    denominator = (abs(left) + abs(right)) / 2.0
    return 0.0 if denominator == 0 else abs(left - right) / denominator * 100.0


def median_lifecycle(run: dict[str, object]) -> dict[str, float]:
    measurements = [sample["measurements"] for sample in run["lifecycle"]]
    if not measurements:
        return {}
    keys = measurements[0].keys()
    return {
        key: float(statistics.median(float(item[key]) for item in measurements))
        for key in keys
        if isinstance(measurements[0][key], (int, float))
    }


def compare_repetitions(
    runs: list[dict[str, object]],
    noise_band: float,
    repository: dict[str, object],
    environment: dict[str, object],
    commands: list[dict[str, object]],
) -> dict[str, object]:
    comparisons: list[dict[str, object]] = []
    if len(runs) >= 2:
        left_lifecycle = median_lifecycle(runs[0])
        right_lifecycle = median_lifecycle(runs[1])
        for name in sorted(left_lifecycle.keys() & right_lifecycle.keys()):
            if not name.endswith("Milliseconds"):
                continue
            difference = relative_difference(left_lifecycle[name], right_lifecycle[name])
            comparisons.append(
                {
                    "metric": f"lifecycle.{name}",
                    "left": left_lifecycle[name],
                    "right": right_lifecycle[name],
                    "differencePercent": difference,
                    "withinNoiseBand": difference <= noise_band,
                }
            )

        left_benchmarks = {
            item["name"]: item for item in runs[0]["benchmarks"] if item.get("meanNanoseconds") is not None
        }
        right_benchmarks = {
            item["name"]: item for item in runs[1]["benchmarks"] if item.get("meanNanoseconds") is not None
        }
        for name in sorted(left_benchmarks.keys() & right_benchmarks.keys()):
            left = float(left_benchmarks[name]["meanNanoseconds"])
            right = float(right_benchmarks[name]["meanNanoseconds"])
            difference = relative_difference(left, right)
            comparisons.append(
                {
                    "metric": f"benchmark.{name}.meanNanoseconds",
                    "left": left,
                    "right": right,
                    "differencePercent": difference,
                    "withinNoiseBand": difference <= noise_band,
                }
            )

    return {
        "schemaVersion": "1.0.0",
        "kind": "phase0-repeatability",
        "timestampUtc": utc_now(),
        "repository": repository,
        "environment": environment,
        "commands": commands,
        "artifacts": {"runCount": len(runs)},
        "noiseBandPercent": noise_band,
        "comparisons": comparisons,
        "withinNoiseBand": bool(comparisons) and all(item["withinNoiseBand"] for item in comparisons),
    }


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--profile", choices=("smoke", "baseline", "disassembly"), default="smoke")
    parser.add_argument("--output", type=Path)
    parser.add_argument("--repetitions", type=int)
    parser.add_argument("--filter", action="append", dest="filters")
    parser.add_argument("--include-eventpipe", action="store_true")
    parser.add_argument("--include-build-baselines", action="store_true")
    parser.add_argument("--include-publish", action="store_true")
    parser.add_argument("--rid", default=runtime_identifier())
    parser.add_argument("--enforce-noise", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    config = load_config()
    benchmark = config["benchmark"]
    assert isinstance(benchmark, dict)
    profile = benchmark["profiles"][args.profile]
    repetitions = args.repetitions if args.repetitions is not None else int(profile["repetitions"])
    if repetitions <= 0:
        raise ValueError("Repetitions must be positive")

    output = args.output or REPO_ROOT / "artifacts" / "performance" / dt.datetime.now().strftime("%Y%m%d-%H%M%S")
    output = output.resolve()
    collector = Collector(output)
    dotnet_info = collector.run(["dotnet", "--info"], name="dotnet-info").stdout
    repository = repository_metadata()
    environment = environment_metadata(dotnet_info)

    project = str(REPO_ROOT / str(benchmark["project"]))
    collector.run(
        ["dotnet", "build", project, "-c", str(benchmark["configuration"]), "-m:1"],
        name="benchmark-build",
    )
    dll = benchmark_dll(config)

    runs = [
        collect_benchmark_repetition(collector, config, args.profile, profile, repetition, args.filters)
        for repetition in range(repetitions)
    ]
    packages = collect_package_graph(collector)
    assemblies = collect_assembly_metrics(collector, dll)
    sparse_storage = collect_sparse_storage_metrics(collector, dll)
    build_results = collect_build_baselines(collector, config) if args.include_build_baselines else {}
    profiles = collect_eventpipe(collector, config, dll) if args.include_eventpipe else []
    publishes = collect_publish_baselines(collector, config, args.rid) if args.include_publish else []

    result = {
        "schemaVersion": "1.0.0",
        "kind": "phase0-run",
        "timestampUtc": utc_now(),
        "repository": repository,
        "environment": environment,
        "commands": collector.commands,
        "artifacts": {
            "root": str(output),
            "resultSchema": "eng/performance/schemas/phase0-result.schema.json",
            "configuration": str(CONFIG_PATH.relative_to(REPO_ROOT)).replace("\\", "/"),
        },
        "profile": args.profile,
        "runs": runs,
        "lifecycle": [sample for run in runs for sample in run["lifecycle"]],
        "benchmarks": [item for run in runs for item in run["benchmarks"]],
        "build": build_results,
        "packages": packages,
        "assemblies": assemblies,
        "sparseStorage": sparse_storage,
        "profiles": profiles,
        "publishes": publishes,
    }
    result_path = output / "phase0-result.json"
    result_path.write_text(json.dumps(result, indent=2), encoding="utf-8")

    repeatability = compare_repetitions(
        runs,
        float(profile["noiseBandPercent"]),
        repository,
        environment,
        collector.commands,
    )
    repeatability_path = output / "repeatability.json"
    repeatability_path.write_text(json.dumps(repeatability, indent=2), encoding="utf-8")

    print(f"Phase 0 result: {result_path}")
    print(f"Repeatability: {repeatability_path}")
    print(f"Within documented noise band: {repeatability['withinNoiseBand']}")
    if args.enforce_noise and not repeatability["withinNoiseBand"]:
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
