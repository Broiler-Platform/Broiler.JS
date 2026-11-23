#!/usr/bin/env python3
"""Part I of decision-rule.md (Broiler.JS's parse measurement, clause 6) on a controlled lane, into a sub-bundle named
for the lane: measurement/<lane>/.

    python -X utf8 docs/evidence/sjl-14-001/collect-part-i.py --lane win-x64-r7-5800x \
        --base-tree <a clean checkout of the base commit> --after-tree <a clean checkout of the after commit> \
        --octane <a checkout of chromium/octane at the revision inputs/workloads/corpora.tsv names> \
        --scratch <a scratch directory> --out docs/evidence/sjl-14-001

A DRAFT, written on 2026-11-19 on window W2's branch before any run of part I and before the base commit exists, as
bundle sjl-3-001's collect-lane.py was committed before its run (that bundle's measurement-win-x64-r7-5800x/README.md).
It is modelled on collect-lane.py and departs from it where part I's rule does: the arms are commits of Broiler.JS, not
a rule commit and an after commit of one benchmark, and the quantity is the harness's `parse` column, not a
BenchmarkDotNet report. It judges nothing; the sub-bundle's README reads its files under the rule.

WHAT IT RUNS, and where each point comes from (the rule's part I; inputs/workloads/README.md; Measurement.md):
  * arms: B, the base commit's build of Broiler.JS/benchmarks/Broiler.JavaScript.Engine.Benchmarks; A, the after
    commit's build of the same project; A-again, A's build measured again under another name (the A/A lane). Each arm
    runs its own commit's harness (CompilePhaseMetrics, `--compile-phases`), whose `parse` column times
    FastParser.ParseProgram on the calling thread: at the base the old parser; after, the facade, which takes the
    bridge's hop to the parse thread, the library's parse, the bridge and the return (decision D47; the session's
    decision B7 of 2026-11-19, recorded in the bundle's README);
  * workloads: the six of inputs/workloads/corpora.tsv, checked by SHA-256 at the pinned Octane revision before the
    first run; a file that differs stops the collection before any run (the inputs' README: "a file that differs makes
    the lane's runs void");
  * one corpus per process (`--compile-phases`' own rule, Measurement.md); its warm-up is the harness's own untimed pass
    of every phase before the timed ones, in its own order (Measurement.md, "`--compile-phases` takes its end-to-end
    check first, and that ordering is the measurement");
  * repetitions: --repetitions launches per workload and arm (nine by default), in an order that rotates by
    repetition: B, A, A-again; then A, A-again, B; then A-again, B, A; and again; each launch takes the harness's
    in-process median over --inner repetitions (five by default, as Measurement.md's example passes it);
  * the condition: every workload parses to the outcome the base gives, checked before and after every measured launch,
    as collect-lane.py checks its answers before and after each repetition and lane: a launch of that arm with one
    in-process repetition for that workload, whose outcome (accepted, the row present for the workload; or refused,
    with the exception type its standard error names, or its exit code) is compared with arm B's first outcome for the
    workload, taken once before the first run; conditions.tsv holds each check with "equal to B's first";
  * Q is the median over the launches of the harness's `parseMs`, per workload and arm; N = |Q(A) - Q(A-again)|;
    D = Q(A) - Q(B). summary.tsv holds them; the README compares D with N.
The repetitions, the inner count and the order are the points of part I's protocol the bundle README files before any
part I run (its reading B10 of 2026-11-19, under the owner's direction of 2026-10-18): nine repetitions in the rotating
order, five in each process, the harness's own warm-up, one corpus per process, each arm its own commit's harness, and
a fresh confirmation run of the whole protocol when the first would read MET, taken into a folder of its own
(--folder, for example win-x64-r7-5800x-confirmation). The condition's schedule, around every launch against B's first
outcome, is this draft's, modelled on collect-lane.py's (revised 2026-11-19, in review of W2, before any part I run: the
draft read "The condition's schedule, before the first run and after the last, is this draft's" and recorded only
accepted or not, compared with nothing); the owner may override any point before the first run, and the script takes
the counts as arguments so that the brief, not this file, fixes them.

The manifest, written and made read-only before the first run, names the commits, the identity checks, every assembly
of each arm by SHA-256, the Octane revision and each workload's SHA-256, the protocol, and the lane with the SHA-256 of
each file of its record, machine.txt among them (docs/evidence/lanes/<lane>/README.md: "A sub-bundle measured on the
lane names the folder and the SHA-256 of its machine.txt in its own manifest, written before its first run").

The sub-bundle folder is created new: the script stops if it exists, and deletes nothing.
"""

from __future__ import annotations

import argparse
import datetime
import gzip
import hashlib
import json
import os
import platform
import re
import statistics
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[2]
BENCH_PROJECT = "Broiler.JS/benchmarks/Broiler.JavaScript.Engine.Benchmarks"
BENCH_DLL = BENCH_PROJECT + "/bin/Release/net10.0/Broiler.JavaScript.Engine.Benchmarks.dll"
ARMS = ["B", "A", "A-again"]
WORKLOADS_TSV = HERE / "inputs" / "workloads" / "corpora.tsv"
BUILD_PROCESSES = ("dotnet", "vbcscompiler", "msbuild", "testhost")

# The window snapshot sjl-3-001's collect-lane.py takes on a Windows lane, unchanged. It reads only.
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


def utc() -> str:
    return datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with open(path, "rb") as stream:
        for block in iter(lambda: stream.read(1 << 20), b""):
            digest.update(block)
    return digest.hexdigest()


def run(arguments: list[str], cwd: Path, log: list[str], env: dict[str, str] | None = None, check: bool = False) -> subprocess.CompletedProcess:
    log.append(f"# {utc()} cd {cwd} && {' '.join(arguments)}\n")
    result = subprocess.run(arguments, cwd=cwd, env=env, capture_output=True, text=True, encoding="utf-8", errors="backslashreplace")
    log.append(f"# exit {result.returncode}\n")
    if check and result.returncode != 0:
        log.append(result.stdout[-4000:] + result.stderr[-4000:])
        raise SystemExit(f"{' '.join(arguments)} failed with {result.returncode}")
    return result


def git(tree: Path, *arguments: str) -> str:
    return subprocess.run(["git", "-C", str(tree), *arguments], capture_output=True, text=True, check=True).stdout.strip()


def workloads() -> tuple[str, list[dict[str, str]]]:
    revision = None
    rows = []
    header = None
    for line in WORKLOADS_TSV.read_text(encoding="utf-8").splitlines():
        if line.startswith("#% octane"):
            revision = line.split("\t")[2]
        if not line or line.startswith("#"):
            continue
        fields = line.split("\t")
        if header is None:
            header = fields
            continue
        rows.append(dict(zip(header, fields)))
    if revision is None or not rows:
        raise SystemExit(f"{WORKLOADS_TSV} names no Octane revision or no workload")
    return revision, rows


class PartICollector:
    def __init__(self, args: argparse.Namespace) -> None:
        self.lane = args.lane
        self.record = REPO / "docs" / "evidence" / "lanes" / self.lane
        self.base_tree = Path(args.base_tree).resolve()
        self.after_tree = Path(args.after_tree).resolve()
        self.octane = Path(args.octane).resolve()
        self.scratch = Path(args.scratch).resolve()
        self.out = Path(args.out).resolve()
        self.repetitions = args.repetitions
        self.inner = args.inner
        self.folder = self.out / "measurement" / (args.folder or self.lane)
        # No build process outlives its build, as collect-lane.py's builds.
        self.env = dict(os.environ, MSBUILDDISABLENODEREUSE="1", DOTNET_CLI_USE_MSBUILD_SERVER="0", UseSharedCompilation="false",
                        DOTNET_CLI_TELEMETRY_OPTOUT="1", DOTNET_NOLOGO="1")
        self.log: list[str] = []

    def write(self, relative: str, text: str) -> Path:
        path = self.folder / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8", newline="\n")
        return path

    def write_gz(self, relative: str, text: str) -> None:
        path = self.folder / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        with gzip.GzipFile(path, "wb", mtime=0) as stream:
            stream.write(text.encode("utf-8"))

    def window(self, relative: str, label: str) -> None:
        if platform.system() != "Windows":
            self.write(relative, f"{label}: {utc()}\n\n(no window snapshot: the lane's capture is a Windows script)\n")
            return
        capture = subprocess.run(["powershell", "-NoProfile", "-File", str(self.record / "capture.ps1")],
                                 capture_output=True, text=True, encoding="utf-8", errors="backslashreplace")
        snapshot = subprocess.run(["powershell", "-NoProfile", "-Command", WINDOW], capture_output=True, text=True,
                                  encoding="utf-8", errors="backslashreplace")
        self.write(relative, f"{label}: {utc()}\n\n== the lane's capture (capture.ps1), again\n{capture.stdout}"
                   + f"\n== the window\n{snapshot.stdout}")

    @staticmethod
    def build_processes() -> list[str]:
        if platform.system() == "Windows":
            listing = subprocess.run(["tasklist", "/FO", "CSV", "/NH"], capture_output=True, text=True).stdout
            names = [line.split('","')[0].strip('"') + ":" + line.split('","')[1] for line in listing.splitlines() if '","' in line]
        else:
            listing = subprocess.run(["ps", "-eo", "pid=,comm="], capture_output=True, text=True).stdout
            names = [f"{c}:{p}" for p, c in (line.split(None, 1) for line in listing.splitlines() if line.strip())]
        return [n for n in names if n.split(":")[0].lower().removesuffix(".exe") in BUILD_PROCESSES]

    def launch(self, arm: str, workload: str, inner: int) -> tuple[subprocess.CompletedProcess, dict | None]:
        tree = self.after_tree if arm != "B" else self.base_tree
        dll = tree / BENCH_DLL
        result = run(["dotnet", str(dll), "--compile-phases", str(self.octane), str(inner), workload], dll.parent, self.log, env=self.env)
        report = None
        if result.returncode == 0:
            start = result.stdout.find("{")
            if start >= 0:
                try:
                    report = json.loads(result.stdout[start:])
                except json.JSONDecodeError:
                    report = None
        return result, report

    @staticmethod
    def outcome(result: subprocess.CompletedProcess, report: dict | None, workload: str) -> str:
        # Accepted when the harness printed the workload's row; refused otherwise, by the exception type its standard
        # error names first, or by its exit code when it names none.
        if PartICollector.row(report, workload) is not None:
            return "accepted"
        match = re.search(r"\b([A-Z]\w*(?:\.[A-Z]\w*)*Exception)\b", result.stderr + result.stdout)
        return f"refused: {match.group(1)}" if match else f"refused: exit {result.returncode}"

    @staticmethod
    def row(report: dict | None, workload: str) -> dict | None:
        if not report:
            return None
        return next((r for r in report.get("rows", []) if r.get("corpus") == workload), None)

    def collect(self) -> None:
        if self.folder.exists():
            raise SystemExit(f"{self.folder} exists: keep it, rename it as stopped, and start again")
        machine = self.record / "machine.txt"
        if not machine.is_file():
            raise SystemExit(f"no lane record at {self.record}")
        revision, rows = workloads()

        # The arms: the after commit's first parent is the base commit (the rule's "The commits, named before they exist").
        base = git(self.base_tree, "rev-parse", "HEAD")
        after = git(self.after_tree, "rev-parse", "HEAD")
        first_parent = git(self.after_tree, "rev-parse", "HEAD^1")
        identity = [f"base commit (arm B): {base}", f"after commit (arms A and A-again): {after}",
                    f"the after commit's first parent: {first_parent}",
                    f"the after commit's first parent is the base commit: {first_parent == base}",
                    f"base tree clean: {git(self.base_tree, 'status', '--porcelain') == ''}",
                    f"after tree clean: {git(self.after_tree, 'status', '--porcelain') == ''}"]

        # The workloads: the Octane revision and every file's SHA-256, before anything runs.
        octane_head = git(self.octane, "rev-parse", "HEAD")
        identity.append(f"octane: {octane_head} (corpora.tsv names {revision}; equal: {octane_head == revision})")
        files_ok = True
        for row in rows:
            actual = sha256(self.octane / row["file"])
            identity.append(f"  {row['workload']}: {row['file']} {actual} (named {row['sha256']}; equal: {actual == row['sha256']})")
            files_ok &= actual == row["sha256"]
        self.folder.mkdir(parents=True)
        if first_parent != base or octane_head != revision or not files_ok:
            self.write("identity.txt", "\n".join(identity) + "\n")
            raise SystemExit("an arm or a workload is not the one the rule names; see identity.txt")

        for tree in (self.base_tree, self.after_tree):
            run(["dotnet", "build", "-c", "Release", "-nologo", BENCH_PROJECT], tree, self.log, env=self.env, check=True)
        for label, tree in (("B", self.base_tree), ("A and A-again (one build)", self.after_tree)):
            identity.append(f"arm {label} binaries:")
            identity += [f"  {sha256(p)}  {p.name}" for p in sorted((tree / BENCH_DLL).parent.glob("*.dll"))]
        identity.append(f"protocol: --compile-phases <octane> {self.inner} <workload>, one workload per launch; "
                        f"{self.repetitions} repetitions per workload and arm; arms B, A, A-again rotated by repetition; "
                        f"the condition checked before and after every launch, by a launch of the same arm and workload "
                        f"with one in-process repetition, against arm B's first outcome for the workload; "
                        f"Q the median of the launches' parseMs")
        facts = dict(line.split(": ", 1) for line in machine.read_text(encoding="utf-8").splitlines() if ": " in line)
        identity.append(f"lane: {self.lane}, the controlled lane recorded in docs/evidence/lanes/{self.lane}/ before this "
                        f"manifest; machine: {facts.get('model', '?')}; RID: {facts.get('RID', '?')}")
        identity += [f"  {sha256(p)}  docs/evidence/lanes/{self.lane}/{p.name}" for p in sorted(self.record.iterdir()) if p.is_file()]
        identity.append("builds: node reuse, the MSBuild server and the shared compiler off "
                        "(MSBUILDDISABLENODEREUSE=1, DOTNET_CLI_USE_MSBUILD_SERVER=0, UseSharedCompilation=false)")
        manifest = self.write("manifest.txt", "\n".join(identity) + "\n")
        manifest.chmod(0o444)

        self.window("window-start.txt", "window start")
        names = [row["workload"] for row in rows]
        conditions = ["repetition\tarm\tworkload\twhen\texit\toutcome\tequal to B's first"]
        reference: dict[str, str] = {}
        for workload in names:
            result, report = self.launch("B", workload, 1)
            reference[workload] = self.outcome(result, report, workload)
            conditions.append(f"-\tB\t{workload}\tfirst check\t{result.returncode}\t{reference[workload]}\tTrue")

        def condition(repetition: int, arm: str, workload: str, when: str) -> None:
            result, report = self.launch(arm, workload, 1)
            found = self.outcome(result, report, workload)
            conditions.append(f"{repetition}\t{arm}\t{workload}\t{when}\t{result.returncode}\t{found}\t{found == reference[workload]}")

        runs = ["repetition\tarm\tworkload\texit\tparseMs\tparseStubMs\tendToEndMs"]
        window_runs = ["repetition\tarm\tworkload\tstarted\tbuild processes alive before the run"]
        for repetition in range(self.repetitions):
            order = ARMS[repetition % 3:] + ARMS[:repetition % 3]
            for workload in names:
                for arm in order:
                    alive = self.build_processes()
                    started = utc()
                    window_runs.append(f"{repetition}\t{arm}\t{workload}\t{started}\t{' '.join(alive) or 'none'}")
                    print(f"   r{repetition} {arm} {workload}: started {started}", flush=True)
                    condition(repetition, arm, workload, "before")
                    result, report = self.launch(arm, workload, self.inner)
                    condition(repetition, arm, workload, "after")
                    self.write_gz(f"runs/r{repetition}-{arm}-{workload}.txt.gz", result.stdout + "\n# stderr\n" + result.stderr)
                    found = self.row(report, workload) or {}
                    runs.append("\t".join(str(v) for v in (repetition, arm, workload, result.returncode, found.get("parseMs", ""),
                                                          found.get("parseStubMs", ""), found.get("endToEndMs", ""))))
        self.window("window-end.txt", "window end")
        self.write("window-runs.tsv", "\n".join(window_runs) + "\n")
        self.write("conditions.tsv", "\n".join(conditions) + "\n")
        self.write("runs.tsv", "\n".join(runs) + "\n")
        self.write_gz("collect.log.gz", "".join(self.log))

        # The rule's quantities: Q the median per workload and arm, N = |Q(A) - Q(A-again)|, D = Q(A) - Q(B).
        samples: dict[tuple[str, str], list[float]] = {}
        for line in runs[1:]:
            fields = line.split("\t")
            if fields[4]:
                samples.setdefault((fields[1], fields[2]), []).append(float(fields[4]))
        summary = ["workload\truns B\truns A\truns A-again\tQ(B) ms\tQ(A) ms\tQ(A-again) ms\tN ms\tD ms"]
        for workload in names:
            try:
                qb, qa, qaa = (statistics.median(samples[(arm, workload)]) for arm in ARMS)
            except KeyError:
                summary.append(f"{workload}\t\t\t\t\t\t\t\tincomplete")
                continue
            summary.append(f"{workload}\t{len(samples[('B', workload)])}\t{len(samples[('A', workload)])}\t"
                           f"{len(samples[('A-again', workload)])}\t{qb:.3f}\t{qa:.3f}\t{qaa:.3f}\t{abs(qa - qaa):.3f}\t{qa - qb:.3f}")
        self.write("summary.tsv", "\n".join(summary) + "\n")
        hashes = [f"{sha256(p)}  {p.relative_to(self.folder).as_posix()}" for p in sorted(self.folder.rglob("*"))
                  if p.is_file() and p.name != "hashes.txt"]
        self.write("hashes.txt", "\n".join(hashes) + "\n")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--lane", required=True, help="the lane's name: docs/evidence/lanes/<lane>/ holds its record")
    parser.add_argument("--base-tree", required=True, help="a clean checkout of the base commit")
    parser.add_argument("--after-tree", required=True, help="a clean checkout of the after commit")
    parser.add_argument("--octane", required=True, help="a checkout of chromium/octane at the pinned revision")
    parser.add_argument("--scratch", required=True)
    parser.add_argument("--out", required=True, help="the bundle folder, docs/evidence/sjl-14-001")
    parser.add_argument("--repetitions", type=int, default=9, help="launches per workload and arm")
    parser.add_argument("--inner", type=int, default=5, help="the harness's in-process repetitions per launch")
    parser.add_argument("--folder", help="the sub-bundle's folder under measurement/, the lane's name by default")
    args = parser.parse_args(argv)
    print(f"== part I on {args.lane} {utc()}", flush=True)
    PartICollector(args).collect()
    print(f"== done {utc()}", flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
