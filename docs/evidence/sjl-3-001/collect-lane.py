#!/usr/bin/env python3
"""Part B of decision-rule.md on a controlled lane: collect.py's measure step, into a sub-bundle named for the lane.

    python -X utf8 docs/evidence/sjl-3-001/collect-lane.py --lane <lane> \
        --tree <a clean checkout of the after commit> --bench-tree <a clean checkout of the rule commit> \
        --scratch <a scratch directory> --out docs/evidence/sjl-3-001

WHY NOT `collect.py --only measure`. Its measure step was written for the programme's container. It writes into
measurement/, deleting what is there first, and its manifest says "lane: smoke". The void run in measurement/ is
retained and is never overwritten, and this lane is the one recorded in docs/evidence/lanes/<lane>/. So this script
runs the same protocol with collect.py's Collector and its helpers. It departs from collect.py's measure step only
where a comment below says "DEPARTS":

  1. the sub-bundle is measurement-<lane>/, created new. The script stops if that folder exists, and it deletes
     nothing: a stopped collection is kept, renamed so its name says it stopped, and the collection starts again;
  2. the manifest's lane line names the lane, its record and the SHA-256 of that record's files, the machine and the
     RID, in place of "lane: smoke";
  3. builds run with node reuse, the MSBuild server and the shared compiler off, so no build process outlives its
     build (the void run's README records the same of its builds), and every child's output is decoded as UTF-8,
     with any byte that is not UTF-8 escaped rather than failing the collection;
  4. the run window is recorded: window-start.txt and window-end.txt (the lane's capture again, the load, the
     processes that used the CPU over a ten-second sample, the .NET build processes alive), and, before every run,
     its start time and the .NET build processes then alive, in window-runs.tsv.

Everything else is collect.py's measure step, line for line: the arms and their identity checks, the builds, the
assemblies' hashes, the rotation, the answers before and after every run, the filter, the profile, the build timeout,
the parsing of each report and the rule's arithmetic. The collector judges nothing; the sub-bundle's README reads its
files under the rule.
"""

from __future__ import annotations

import argparse
import importlib.util
import json
import os
import shutil
import statistics
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[2]

# DEPARTS (3): decode every child's output as UTF-8 and escape what is not, on a machine whose console is not UTF-8
# by default. Installed before collect.py is loaded, so its helpers and the base collector's use it too.
_subprocess_run = subprocess.run


def _utf8_run(*args, **kwargs):
    if kwargs.get("text") and "encoding" not in kwargs:
        kwargs["encoding"] = "utf-8"
        kwargs["errors"] = "backslashreplace"
    return _subprocess_run(*args, **kwargs)


subprocess.run = _utf8_run

_spec = importlib.util.spec_from_file_location("sjl_3_001_collect", HERE / "collect.py")
collect = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(collect)
base = collect.base
utc = collect.utc
LANES = collect.LANES

# DEPARTS (4): one PowerShell snapshot of the window. It reads only.
WINDOW = r"""
$ErrorActionPreference = 'Continue'
$a = @{}; foreach ($p in Get-Process) { if ($null -ne $p.CPU) { $a[$p.Id] = $p.CPU } }
Start-Sleep -Seconds 10
$b = Get-Process
"processes: $($b.Count)"
"CPU load % (Win32_Processor.LoadPercentage): $((Get-CimInstance Win32_Processor).LoadPercentage)"
"available memory MiB: $([int]((Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory / 1024))"
"current culture: $((Get-Culture).Name)"
"processes that used more than 1 % of one logical processor over a 10 s sample (CPU seconds):"
foreach ($p in ($b | Sort-Object Name)) {
    if ($null -ne $p.CPU -and $a.ContainsKey($p.Id)) {
        $d = $p.CPU - $a[$p.Id]
        if ($d -gt 0.1) { '  {0,-32} {1,8:N2}' -f $p.Name, $d }
    }
}
".NET build processes (dotnet, VBCSCompiler, MSBuild, testhost):"
$n = 0
foreach ($p in Get-CimInstance Win32_Process) {
    if ($p.Name -match '^(dotnet|VBCSCompiler|MSBuild|testhost)') {
        $c = [string]$p.CommandLine
        "  pid $($p.ProcessId) $($p.Name) $($c.Substring(0, [Math]::Min(200, $c.Length)))"; $n++
    }
}
if ($n -eq 0) { '  none' }
"""
BUILD_PROCESSES = ("dotnet.exe", "vbcscompiler.exe", "msbuild.exe", "testhost.exe")


class LaneCollector(collect.Collector):
    def __init__(self, args: argparse.Namespace) -> None:
        super().__init__(args)
        self.lane = args.lane
        self.record = REPO / "docs" / "evidence" / "lanes" / self.lane
        # DEPARTS (3): no build process outlives its build.
        self.env.update(MSBUILDDISABLENODEREUSE="1", DOTNET_CLI_USE_MSBUILD_SERVER="0", UseSharedCompilation="false")

    def window(self, relative: str, label: str) -> None:
        capture = subprocess.run(["powershell", "-NoProfile", "-File", str(self.record / "capture.ps1")],
                                 capture_output=True, text=True)
        snapshot = subprocess.run(["powershell", "-NoProfile", "-Command", WINDOW], capture_output=True, text=True)
        self.write(relative, f"{label}: {utc()}\n\n== the lane's capture (capture.ps1), again\n{capture.stdout}"
                   + (f"# stderr\n{capture.stderr}" if capture.stderr.strip() else "")
                   + f"\n== the window\n{snapshot.stdout}"
                   + (f"# stderr\n{snapshot.stderr}" if snapshot.stderr.strip() else ""))

    @staticmethod
    def build_processes() -> list[str]:
        listing = subprocess.run(["tasklist", "/FO", "CSV", "/NH"], capture_output=True, text=True).stdout
        found = []
        for line in listing.splitlines():
            fields = [f.strip('"') for f in line.split('","')]
            if fields and fields[0].lower() in BUILD_PROCESSES:
                found.append(f"{fields[0]}:{fields[1]}")
        return found

    def measure(self) -> None:
        if self.bench is None:
            raise SystemExit("measure needs --bench-tree")
        # DEPARTS (1): a new folder named for the lane; nothing is deleted.
        out = f"measurement-{self.lane}"
        if (self.out / out).exists():
            raise SystemExit(f"{out} exists: keep it, rename it as stopped, and start again")
        machine = self.record / "machine.txt"
        if not machine.is_file():
            raise SystemExit(f"no lane record at {self.record}")
        log: list[str] = []
        rule = self.git(self.bench, "rev-parse", "HEAD")
        after = self.git(self.tree, "rev-parse", "HEAD")
        engine_moved = [p for p in self.git(self.bench, "diff", "--name-only", self.base_commit, rule, "--", "Broiler.JS").splitlines()
                        if not p.startswith(base.BENCH_PROJECT + "/")]
        workloads_moved = self.git(self.tree, "diff", "--name-only", rule, after, "--",
                                   base.BENCH_PROJECT + "/NumberConversionBenchmarks.cs", base.BENCH_PROJECT + "/Program.cs",
                                   base.BENCH_PROJECT + "/BenchmarkConfiguration.cs").splitlines()
        identity = [f"base commit: {self.base_commit}", f"arm B: the rule commit {rule}",
                    f"arm A and A-again: the after commit {after}",
                    f"bench tree clean: {self.git(self.bench, 'status', '--porcelain') == ''}",
                    f"after tree clean: {self.git(self.tree, 'status', '--porcelain') == ''}",
                    f"arm B's engine projects identical to the base: {not engine_moved}",
                    f"the workloads, Program.cs and BenchmarkConfiguration.cs unchanged from the rule commit at A: {not workloads_moved}"]
        if engine_moved or workloads_moved:
            self.write(f"{out}/identity.txt", "\n".join(identity) + "\n")
            raise SystemExit("an arm is not the one the rule names")
        trees = {"B": self.bench, "A": self.tree, "A-again": self.tree}
        for tree in (self.bench, self.tree):
            self.run(["dotnet", "build", "-c", "Release", "-nologo", base.BENCH_PROJECT], tree, log, check=True)
        for lane, tree in (("B", self.bench), ("A and A-again (one build)", self.tree)):
            identity.append(f"arm {lane} binaries:")
            identity += [f"  {self.sha256(p)}  {p.name}" for p in sorted(Path(tree, base.BENCH_DLL).parent.glob("*.dll"))]
        identity.append(f"profile: baseline; repetitions per lane: {base.REPETITIONS}; lanes: B, A, A-again, rotated by repetition")
        # DEPARTS (2): the lane this run is taken on, and its record as it stood before the first run.
        facts = dict(line.split(": ", 1) for line in machine.read_text(encoding="utf-8").splitlines() if ": " in line)
        identity.append(f"lane: {self.lane}, the controlled lane recorded in docs/evidence/lanes/{self.lane}/ before this "
                        f"manifest; machine: {facts.get('model', '?')}, {facts.get('physical cores', '?')} cores, "
                        f"{facts.get('logical processors', '?')} logical processors; RID: {facts.get('RID', '?')}")
        identity += [f"  {self.sha256(p)}  docs/evidence/lanes/{self.lane}/{p.name}" for p in sorted(self.record.iterdir()) if p.is_file()]
        identity.append("builds: node reuse, the MSBuild server and the shared compiler off "
                        "(MSBUILDDISABLENODEREUSE=1, DOTNET_CLI_USE_MSBUILD_SERVER=0, UseSharedCompilation=false)")
        manifest = self.write(f"{out}/manifest.txt", "\n".join(identity) + "\n")
        manifest.chmod(0o444)

        # DEPARTS (4): the window, before the first run.
        self.window(f"{out}/window-start.txt", "window start")
        window_runs = ["repetition\tlane\tstarted\tbuild processes alive before the run"]

        def answers(tree: Path) -> dict:
            result = self.run(["dotnet", str(tree / base.BENCH_DLL), "--number-conversion-answers"], tree, log, check=True)
            return json.loads(result.stdout.strip().splitlines()[-1])

        reference = None
        conditions = ["repetition\tlane\twhen\tanswers equal B's first\tconfiguration equal B's first"]
        runs = ["repetition\tlane\tworkload\tmedian_ns\tmean_ns\tallocated_bytes\tgen0\tgen1\tgen2"]
        environments = []
        for repetition in range(base.REPETITIONS):
            order = LANES[repetition % 3:] + LANES[:repetition % 3]
            for lane in order:
                tree = trees[lane]
                artifacts = self.scratch / "bdn" / f"r{repetition}-{lane}"
                if artifacts.exists():
                    shutil.rmtree(artifacts)
                if reference is None:
                    reference = answers(self.bench)
                    conditions.append(f"-\tB\tfirst check\tTrue\tTrue")
                before = answers(tree)
                conditions.append(f"{repetition}\t{lane}\tbefore\t{before['answers'] == reference['answers']}\t{before['configuration'] == reference['configuration']}")
                env = dict(self.env, BROILER_BENCHMARK_PROFILE="baseline", BROILER_BENCHMARK_ARTIFACTS=str(artifacts))
                alive = self.build_processes()  # DEPARTS (4)
                started = utc()
                window_runs.append(f"{repetition}\t{lane}\t{started}\t{' '.join(alive) or 'none'}")  # DEPARTS (4)
                print(f"   r{repetition} {lane}: started {started}", flush=True)  # DEPARTS (4): progress on the console
                result = subprocess.run(["dotnet", str(tree / base.BENCH_DLL), "--filter", "*NumberConversionBenchmarks*",
                                         "--buildTimeout", "1800"], cwd=Path(tree, base.BENCH_DLL).parent, env=env,
                                        capture_output=True, text=True)
                log.append(f"# run r{repetition} {lane}: started {started}, exit {result.returncode}\n")
                after_answers = answers(tree)
                conditions.append(f"{repetition}\t{lane}\tafter\t{after_answers['answers'] == reference['answers']}\t{after_answers['configuration'] == reference['configuration']}")
                self.write_gz(f"{out}/runs/r{repetition}-{lane}.stdout.txt.gz", result.stdout + result.stderr)
                reports = sorted((artifacts / "results").glob("*-report-full.json"))
                if not reports:
                    runs.append(f"{repetition}\t{lane}\t(no report)\t\t\t\t\t\t")
                    continue
                report = json.loads(reports[0].read_text(encoding="utf-8"))
                self.write_gz(f"{out}/runs/r{repetition}-{lane}.report-full.json.gz", json.dumps(report, indent=1, sort_keys=True))
                environments.append(json.dumps({
                    "host": report.get("HostEnvironmentInfo", {}),
                    "hardwareIntrinsics": sorted({str(b.get("HardwareIntrinsics")) for b in report["Benchmarks"]}),
                }, sort_keys=True))
                for benchmark in report["Benchmarks"]:
                    workload = benchmark["Parameters"].split("=", 1)[1]
                    stats = benchmark.get("Statistics") or {}
                    memory = benchmark.get("Memory") or {}
                    runs.append("\t".join(str(v) for v in (repetition, lane, workload, stats.get("Median", ""), stats.get("Mean", ""),
                                                          memory.get("BytesAllocatedPerOperation", ""), memory.get("Gen0Collections", ""),
                                                          memory.get("Gen1Collections", ""), memory.get("Gen2Collections", ""))))
        # DEPARTS (4): the window, after the last run.
        self.window(f"{out}/window-end.txt", "window end")
        self.write(f"{out}/window-runs.tsv", "\n".join(window_runs) + "\n")
        self.write(f"{out}/conditions.tsv", "\n".join(conditions) + "\n")
        self.write(f"{out}/runs.tsv", "\n".join(runs) + "\n")
        self.write(f"{out}/answers.json", json.dumps(reference, indent=1, sort_keys=True) + "\n")
        self.write(f"{out}/environments.txt", f"distinct BenchmarkDotNet host environment records: {len(set(environments))}\n"
                   + "\n".join(sorted(set(environments))) + "\n")
        self.write_gz(f"{out}/collect.log.gz", "".join(log))

        # The rule's quantities, as collect.py computes them.
        medians: dict[tuple[str, str], list[float]] = {}
        for line in runs[1:]:
            fields = line.split("\t")
            if fields[3]:
                medians.setdefault((fields[1], fields[2]), []).append(float(fields[3]))
        q = {key: statistics.median(values) for key, values in medians.items()}
        summary = ["workload\truns B\truns A\truns A-again\tQ(B) ns\tQ(A) ns\tQ(A-again) ns\tN ns\tD ns\tE(B) ns\tE(A) ns"]
        for workload in base.MEASURED:
            control = workload + "Control"
            try:
                qb, qa, qaa = q[("B", workload)], q[("A", workload)], q[("A-again", workload)]
                eb, ea = qb - q[("B", control)], qa - q[("A", control)]
            except KeyError:
                summary.append(f"{workload}\t\t\t\t\t\t\t\t\t\tincomplete")
                continue
            summary.append(f"{workload}\t{len(medians[('B', workload)])}\t{len(medians[('A', workload)])}\t"
                           f"{len(medians[('A-again', workload)])}\t{qb:.1f}\t{qa:.1f}\t{qaa:.1f}\t{abs(qa - qaa):.1f}\t"
                           f"{qa - qb:.1f}\t{eb:.1f}\t{ea:.1f}")
        self.write(f"{out}/summary.tsv", "\n".join(summary) + "\n")
        # Each path written with "/", as collect.py's are on the container.
        hashes = [f"{self.sha256(p)}  {p.relative_to(self.out / out).as_posix()}" for p in sorted((self.out / out).rglob("*"))
                  if p.is_file() and p.name != "hashes.txt"]
        self.write(f"{out}/hashes.txt", "\n".join(hashes) + "\n")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--lane", required=True, help="the lane's name: docs/evidence/lanes/<lane>/ holds its record")
    parser.add_argument("--tree", required=True, help="a clean checkout of the after commit")
    parser.add_argument("--bench-tree", required=True, help="a clean checkout of the decision rule's commit")
    parser.add_argument("--out", required=True)
    parser.add_argument("--scratch", required=True)
    args = parser.parse_args(argv)
    args.suite_archive = None
    args.terser_module = None
    args.test262_from = None
    args.max_workers = 1
    collector = LaneCollector(args)
    print(f"== measure on {args.lane} {utc()}", flush=True)
    collector.measure()
    print(f"== done {utc()}", flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
