#!/usr/bin/env python3
"""Collect Broiler.JS's half of milestone SJL-0's baselines (work package SJL-0.8) as bundle sjl-0-001.

    python3 docs/evidence/sjl-0-001/collect.py --tree <a checkout at the base commit> \
        --suite-archive <the pinned test262 archive> --terser-module <terser's node module> \
        --bench-tree <a checkout at the decision rule's commit> \
        --out docs/evidence/sjl-0-001 [--only STEP ...]

THE BASE IS A CHECKOUT, NOT THIS TREE. The shared JavaScript libraries programme compares every later
cut-over against a base taken before any engine changes, at a named commit. This collector is written
after that commit, so `--tree` names a separate checkout of the base, and every build, run and hash of
the base is taken there. The one exception is the measurement: its workloads were committed with
their rule, after the base, so `--bench-tree` names a checkout of the rule's commit, and the
`measure` step first shows that its engine projects are the base's.

THE COLLECTOR JUDGES NOTHING. Each step writes what it ran and what it printed, failures included.
The bundle's README interprets; no figure leaves the bundle.

Steps, each with `--only` to take it alone:

  identity   the base commit and its cleanliness, the SDK, Node, Terser, the machine, and the suite
             archive's SHA-256 against the pin Broiler.VM holds for the same revision;
  test262    the whole pinned suite in the canonical configuration of .github/workflows/test262.yml's
             full phase (all script-host-verifiable paths, eight shards, the terser variant, the
             workflow's default timeouts and memory limit, a self-check first), through the base's own
             scripts/compliance/run_test262.py, merged by its merge_test262_shards.py; the per-path
             table, the merged report (compressed), every shard's log and status, and two checks of
             scripts/compliance/compare_test262_paths.py on the real report: against itself, and
             against a copy with one passing row turned to failed. `--test262-from` imports a run
             already taken with the same commands instead of taking it again, and says so;
  packages   every project of Broiler.JS.slnx evaluated for IsPackable, each packable one packed in
             Release from the base, and of each package its nuspec, its contents with sizes and
             SHA-256, and the checks eng/pack.ps1 makes, re-implemented here because pack.ps1 needs
             PowerShell;
  api        an interim public API listing of every packable assembly: a throwaway console project,
             built outside the base tree, references every packable project of the base, and lists
             each assembly's public surface with PublicApiGenerator. It is interim: SJL-2 adopts the
             tool the API baselines use from then on;
  measure    the base arm of part B of docs/evidence/sjl-3-001/decision-rule.md, measured alone: the
             six workloads of NumberConversionBenchmarks in arm B and in an A/A lane B-again, rotated
             by repetition, seven runs each, every run retained, the answers and the effective
             configuration checked before and after every run. It writes measurement/ as a
             sub-bundle of its own, because a measurement is its own evidence class;
  manifest   manifest.json: the SHA-256 of every file the bundle retains.

Paths in retained logs are written as <tree>, <bench>, <repo> (this collector's checkout), <suite>,
<run> (an imported test262 run) and <scratch>.
"""

from __future__ import annotations

import argparse
import datetime
import gzip
import hashlib
import json
import os
import platform
import shutil
import statistics
import subprocess
import sys
import tarfile
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path

SUITE_REVISION = "ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e"
# Broiler.VM/src/tests/conformance/pins/test262.pin: archive-sha256 of the same revision.
SUITE_ARCHIVE_SHA256 = "f58ce79141529c9fa33592e22ff3ff0d83b83830ac8e372ecd32e1623db1c040"
SHARDS = 8
# The workflow's input defaults (.github/workflows/test262.yml): per-test timeout, minifier timeout,
# memory limit. The worker count is this machine's and is recorded.
TEST_TIMEOUT = 30
MINIFIER_TIMEOUT = 30
MEMORY_LIMIT = 0
SHELL_DLL = "Broiler.JS/Broiler.JavaScript/bin/Release/net10.0/BroilerJS.dll"
BENCH_PROJECT = "Broiler.JS/benchmarks/Broiler.JavaScript.Engine.Benchmarks"
BENCH_DLL = BENCH_PROJECT + "/bin/Release/net10.0/Broiler.JavaScript.Engine.Benchmarks.dll"
WORKLOADS = ("ToString", "ToStringControl", "ToNumber", "ToNumberControl", "Literals", "LiteralsControl")
MEASURED = ("ToString", "ToNumber", "Literals")
REPETITIONS = 7
PUBLIC_API_GENERATOR = "11.4.6"

STEPS = ("identity", "test262", "packages", "api", "measure", "manifest")


class Collector:
    def __init__(self, args: argparse.Namespace) -> None:
        self.tree = Path(args.tree).resolve()
        self.out = Path(args.out).resolve()
        self.scratch = Path(args.scratch).resolve()
        self.bench = Path(args.bench_tree).resolve() if args.bench_tree else None
        self.suite_archive = Path(args.suite_archive).resolve() if args.suite_archive else None
        self.terser = Path(args.terser_module).resolve() if args.terser_module else None
        self.test262_from = Path(args.test262_from).resolve() if args.test262_from else None
        self.workers = args.max_workers
        self.env = dict(os.environ, DOTNET_CLI_TELEMETRY_OPTOUT="1", DOTNET_NOLOGO="1")
        self.out.mkdir(parents=True, exist_ok=True)
        self.scratch.mkdir(parents=True, exist_ok=True)

    # -- helpers ---------------------------------------------------------------------------------

    def hide(self, text: str) -> str:
        names = [(self.scratch / "suite", "<suite>"), (self.scratch, "<scratch>"), (self.tree, "<tree>"),
                 (self.bench, "<bench>"), (Path(__file__).resolve().parents[3], "<repo>")]
        if self.test262_from is not None:
            names += [(self.test262_from.parent / "suite", "<suite>"), (self.test262_from, "<run>"),
                      (self.test262_from.parent, "<scratch>")]
        # Longest first, so a path inside another is named by the narrower name.
        for path, name in sorted((n for n in names if n[0] is not None), key=lambda n: -len(str(n[0]))):
            text = text.replace(str(path), name)
        return text

    def write(self, relative: str, text: str) -> Path:
        path = self.out / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(self.hide(text), encoding="utf-8", newline="\n")
        return path

    def write_gz(self, relative: str, text: str) -> Path:
        path = self.out / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        with gzip.GzipFile(path, "wb", mtime=0) as handle:
            handle.write(self.hide(text).encode("utf-8"))
        return path

    def run(self, command: list[str], cwd: Path, log: list[str], check: bool = False) -> subprocess.CompletedProcess:
        started = datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
        result = subprocess.run(command, cwd=cwd, env=self.env, capture_output=True, text=True)
        log.append(f"$ {' '.join(command)}\n# cwd {cwd}; started {started}; exit {result.returncode}\n")
        log.append(result.stdout)
        if result.stderr:
            log.append("# stderr\n" + result.stderr)
        if check and result.returncode != 0:
            raise SystemExit(self.hide(f"failed: {' '.join(command)}\n{result.stdout}\n{result.stderr}"))
        return result

    def git(self, tree: Path, *args: str) -> str:
        return subprocess.run(["git", "-C", str(tree), *args], capture_output=True, text=True, check=True).stdout.strip()

    @staticmethod
    def sha256(path: Path) -> str:
        digest = hashlib.sha256()
        with open(path, "rb") as handle:
            for block in iter(lambda: handle.read(1 << 20), b""):
                digest.update(block)
        return digest.hexdigest()

    # -- identity --------------------------------------------------------------------------------

    def identity(self) -> None:
        lines = [
            f"base commit: {self.git(self.tree, 'rev-parse', 'HEAD')}",
            f"base tree clean: {self.git(self.tree, 'status', '--porcelain') == ''}",
            f"collector commit: {self.git(Path(__file__).resolve().parent, 'rev-parse', 'HEAD')}",
        ]
        if self.bench is not None:
            lines.append(f"rule commit (bench tree): {self.git(self.bench, 'rev-parse', 'HEAD')}")
            lines.append(f"rule tree clean: {self.git(self.bench, 'status', '--porcelain') == ''}")
        log: list[str] = []
        for command in (["dotnet", "--version"], ["dotnet", "--list-runtimes"], ["node", "--version"]):
            result = self.run(command, self.tree, log)
            lines.append(f"{' '.join(command)}: {result.stdout.strip()}")
        if self.terser is not None:
            version = json.loads((self.terser / "package.json").read_text(encoding="utf-8"))["version"]
            lines.append(f"terser: {version}")
        lines.append(f"machine: {platform.machine()}; {platform.platform()}; logical processors {os.cpu_count()}")
        try:
            cpu = [l for l in Path("/proc/cpuinfo").read_text().splitlines() if l.startswith("model name")]
            lines.append(f"cpu: {cpu[0].split(':', 1)[1].strip() if cpu else 'unknown'}")
            memory = [l for l in Path("/proc/meminfo").read_text().splitlines() if l.startswith("MemTotal")]
            lines.append(f"memory: {memory[0].split(':', 1)[1].strip() if memory else 'unknown'}")
        except OSError:
            pass
        lines.append(f"suite revision: {SUITE_REVISION}")
        if self.suite_archive is not None:
            actual = self.sha256(self.suite_archive)
            lines.append(f"suite archive sha256: {actual}")
            lines.append(f"suite archive matches Broiler.VM's pin: {actual == SUITE_ARCHIVE_SHA256}")
        lines.append(f"collected: {datetime.datetime.now(datetime.timezone.utc).strftime('%Y-%m-%dT%H:%M:%SZ')}")
        self.write("identity.txt", "\n".join(lines) + "\n")

    # -- test262 ---------------------------------------------------------------------------------

    def suite_root(self) -> Path:
        root = self.scratch / "suite"
        extracted = root / f"test262-{SUITE_REVISION}"
        if not extracted.is_dir():
            if self.suite_archive is None or self.sha256(self.suite_archive) != SUITE_ARCHIVE_SHA256:
                raise SystemExit("the suite archive is missing or does not match the pin")
            root.mkdir(parents=True, exist_ok=True)
            with tarfile.open(self.suite_archive) as archive:
                archive.extractall(root, filter="data")
        return extracted

    def test262(self) -> None:
        shard_dir = self.scratch / "test262-shards"
        merged = self.scratch / "test262-results.json"
        log: list[str] = []
        if self.test262_from is not None:
            shard_dir = self.test262_from / "shard-out"
            merged = self.test262_from / "test262-results.json"
            log.append(f"# imported: the run was taken by {self.test262_from / 'drive.sh'}, retained as test262/drive.sh\n")
            # The driver, its log, and what the run left beside them: the resume script and the
            # interrupted shard's log when a restart cut a shard short, the hashes of the shell's
            # binaries, and the re-run of every timed-out row alone with one worker.
            for name in ("drive.sh", "drive.log", "drive-resume.sh", "interrupted-shard-6.log",
                         "shell-binaries.sha256", "rerun-timeouts.json", "rerun-timeouts.log"):
                if (self.test262_from / name).exists():
                    self.write(f"test262/{name}", (self.test262_from / name).read_text(encoding="utf-8"))
        else:
            suite = self.suite_root()
            shard_dir.mkdir(parents=True, exist_ok=True)
            self.run(["dotnet", "build", "-c", "Release", "-nologo", "Broiler.JS/Broiler.JavaScript/Broiler.JavaScript.csproj"], self.tree, log, check=True)
            common = ["python3", "scripts/compliance/run_test262.py", "--suite-ref", SUITE_REVISION,
                      "--suite-root", str(suite), "--broiler-dll", SHELL_DLL, "--no-summary-stdout"]
            result = self.run(common + ["--self-check"], self.tree, log)
            (shard_dir / "self-check.log").write_text(result.stdout + result.stderr, encoding="utf-8")
            for index in range(SHARDS):
                output = shard_dir / f"test262-full-shard-{index}.json"
                result = self.run(common + [
                    "--shard-count", str(SHARDS), "--shard-index", str(index),
                    "--test-timeout-seconds", str(TEST_TIMEOUT), "--minifier", "terser",
                    "--terser-module", str(self.terser), "--minifier-timeout-seconds", str(MINIFIER_TIMEOUT),
                    "--memory-limit-mb", str(MEMORY_LIMIT), "--max-workers", str(self.workers),
                    "--feature-match", "any", "--selection-label", "all", "--runner-os", "Linux",
                    "--runner-arch", "X64", "--all-script-host-verifiable", "--output", str(output)], self.tree, [])
                (shard_dir / f"test262-full-shard-{index}.log").write_text(result.stderr, encoding="utf-8")
                (shard_dir / f"test262-full-shard-{index}-status.json").write_text(
                    json.dumps({"phase": "full", "shardIndex": index, "exitCode": result.returncode}) + "\n", encoding="utf-8")
            self.run(["python3", "scripts/compliance/merge_test262_shards.py", "--shard-dir", str(shard_dir),
                      "--phase", "full", "--expected-shards", ",".join(str(i) for i in range(SHARDS)),
                      "--merged-json", str(merged)], self.tree, log)

        for entry in sorted(shard_dir.iterdir()):
            if entry.suffix in (".log", ".json") and (entry.name.startswith("test262-full-shard-") or entry.name == "self-check.log"):
                if entry.name.endswith("-status.json") or entry.name == "self-check.log":
                    self.write(f"test262/{entry.name}", entry.read_text(encoding="utf-8"))
                elif entry.suffix == ".log":
                    self.write_gz(f"test262/{entry.name}.gz", entry.read_text(encoding="utf-8"))
        if (merged.parent / "merge.log").exists():
            self.write("test262/merge.log", (merged.parent / "merge.log").read_text(encoding="utf-8"))
        report = json.loads(merged.read_text(encoding="utf-8"))
        self.write_gz("test262/test262-results.json.gz", json.dumps(report, indent=1, sort_keys=True) + "\n")
        rows = sorted((str(r["path"]), str(r.get("variant", "original")), str(r.get("hostMode", "script")), str(r["status"]))
                      for r in report["results"])
        self.write_gz("test262/paths.tsv.gz", "path\tvariant\thostMode\tstatus\n" + "".join("\t".join(r) + "\n" for r in rows))

        # The comparison script on the real report: against itself (no difference), and against a copy
        # with one passing row turned to failed (one regression, exit 1).
        script = Path(__file__).resolve().parents[3] / "scripts" / "compliance" / "compare_test262_paths.py"
        checks: list[str] = []
        self.run([sys.executable, str(script), "--base", str(merged), "--after", str(merged)], self.tree, checks)
        mutated = json.loads(merged.read_text(encoding="utf-8"))
        victim = next(r for r in sorted(mutated["results"], key=lambda r: (str(r["path"]), str(r.get("variant", ""))))
                      if r["status"] == "passed")
        victim["status"] = "failed"
        copy = self.scratch / "test262-negative-control.json"
        copy.write_text(json.dumps(mutated), encoding="utf-8")
        checks.append(f"# negative control: {victim['path']} [{victim.get('variant', 'original')}] turned from passed to failed in a copy\n")
        self.run([sys.executable, str(script), "--base", str(merged), "--after", str(copy)], self.tree, checks)
        copy.unlink()
        self.write("test262/compare-checks.log", "".join(checks))
        self.write("test262/collect.log", "".join(log))

    # -- packages --------------------------------------------------------------------------------

    def projects(self) -> list[str]:
        root = ET.parse(self.tree / "Broiler.JS.slnx").getroot()
        return sorted(node.attrib["Path"].replace("\\", "/") for node in root.iter("Project"))

    def packables(self, log: list[str]) -> list[dict[str, str]]:
        cache = self.scratch / "packables.json"
        if cache.exists():
            return json.loads(cache.read_text(encoding="utf-8"))
        found = []
        for project in self.projects():
            result = self.run(["dotnet", "msbuild", project, "-nologo", "-p:Configuration=Release",
                               "-getProperty:IsPackable,PackageId,PackageVersion,IncludeBuildOutput,IncludeSymbols,AssemblyName,TargetFramework"],
                              self.tree, log)
            properties = json.loads(result.stdout)["Properties"]
            if properties.get("IsPackable") == "true":
                found.append({"Project": project, **properties})
        cache.write_text(json.dumps(found, indent=1), encoding="utf-8")
        return found

    def packages(self) -> None:
        log: list[str] = []
        packables = self.packables(log)
        output = self.scratch / "packages"
        if output.exists():
            shutil.rmtree(output)
        output.mkdir()
        for package in packables:
            self.run(["dotnet", "pack", package["Project"], "-c", "Release", "--nologo", "-o", str(output)], self.tree, log)
        checks = []
        expected = {p["PackageId"]: p["PackageVersion"] for p in packables}
        summary = ["package\tversion\tproject\tnupkg sha256\tentries"]
        for package in packables:
            identity = f"{package['PackageId']}.{package['PackageVersion']}"
            path = output / f"{identity}.nupkg"
            if not path.exists():
                checks.append(f"FAIL {identity}: no package was produced")
                continue
            with zipfile.ZipFile(path) as archive:
                entries = sorted(archive.infolist(), key=lambda e: e.filename)
                listing = [f"{e.filename}\t{e.file_size}\t{hashlib.sha256(archive.read(e)).hexdigest()}"
                           for e in entries if not e.filename.endswith("/")]
                self.write(f"packages/contents/{identity}.txt", "path\tbytes\tsha256\n" + "\n".join(listing) + "\n")
                nuspecs = [e for e in entries if e.filename.endswith(".nuspec")]
                problems = []
                if len(nuspecs) != 1:
                    problems.append("expected one nuspec")
                else:
                    text = archive.read(nuspecs[0]).decode("utf-8-sig")
                    self.write(f"packages/nuspecs/{identity}.nuspec", text)
                    spec = ET.fromstring(text)
                    def find(name: str) -> str:
                        node = next((n for n in spec.iter() if n.tag.rsplit("}", 1)[-1] == name), None)
                        return (node.text or "").strip() if node is not None else ""
                    if find("id") != package["PackageId"] or find("version") != package["PackageVersion"]:
                        problems.append("incorrect identity")
                    if find("description") in ("", "Package Description"):
                        problems.append("no description")
                    for dependency in (n for n in spec.iter() if n.tag.rsplit("}", 1)[-1] == "dependency"):
                        name, version = dependency.attrib.get("id"), dependency.attrib.get("version")
                        if name in expected and version != expected[name]:
                            problems.append(f"internal dependency {name} {version}")
                names = {e.filename for e in entries}
                for asset in ("README.md", "icon.png"):
                    if asset not in names:
                        problems.append(f"missing {asset}")
                if package.get("IncludeBuildOutput") != "false":
                    if not any(n.startswith("lib/") and n.endswith(".dll") for n in names):
                        problems.append("missing assembly")
                    if not any(n.startswith("lib/") and n.endswith(".xml") for n in names):
                        problems.append("missing API documentation")
                    if package.get("IncludeSymbols") == "true" and not (output / f"{identity}.snupkg").exists():
                        problems.append("missing symbols")
                checks.append(f"{'FAIL' if problems else 'ok  '} {identity}" + (f": {'; '.join(problems)}" if problems else ""))
                summary.append(f"{package['PackageId']}\t{package['PackageVersion']}\t{package['Project']}\t{self.sha256(path)}\t{len(listing)}")
        versions = {p["PackageVersion"] for p in packables}
        checks.append(f"{'ok  ' if len(versions) == 1 else 'FAIL'} one package version across packages: {sorted(versions)}")
        ids = [p["PackageId"] for p in packables]
        checks.append(f"{'ok  ' if len(ids) == len(set(ids)) else 'FAIL'} package ids are unique")
        archives = sorted(p.name for p in output.glob("*.nupkg"))
        checks.append(f"{'ok  ' if len(archives) == len(packables) else 'FAIL'} one package per packable project ({len(archives)} archives)")
        self.write("packages/packables.tsv", "\n".join(summary) + "\n")
        self.write("packages/checks.txt", "\n".join(checks) + "\n")
        self.write_gz("packages/pack.log.gz", "".join(log))

    # -- api -------------------------------------------------------------------------------------

    def api(self) -> None:
        log: list[str] = []
        packables = [p for p in self.packables(log) if p.get("IncludeBuildOutput") != "false"]
        tool = self.scratch / "api-lister"
        if tool.exists():
            shutil.rmtree(tool)
        tool.mkdir()
        references = "\n".join(f'    <ProjectReference Include="{self.tree / p["Project"]}" />' for p in packables)
        (tool / "api-lister.csproj").write_text(f"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="PublicApiGenerator" Version="{PUBLIC_API_GENERATOR}" />
{references}
  </ItemGroup>
</Project>
""", encoding="utf-8")
        (tool / "Program.cs").write_text("""using System.Reflection;
using PublicApiGenerator;

// args: <output directory> <assembly name>...
var output = args[0];
foreach (var name in args.Skip(1))
{
    var path = Path.Combine(AppContext.BaseDirectory, name + ".dll");
    var text = Assembly.LoadFrom(path).GeneratePublicApi(new ApiGeneratorOptions { IncludeAssemblyAttributes = false });
    File.WriteAllText(Path.Combine(output, name + ".txt"), text.ReplaceLineEndings("\\n") + "\\n");
}
""", encoding="utf-8")
        self.run(["dotnet", "build", "-c", "Release", "-nologo", str(tool / "api-lister.csproj")], tool, log, check=True)
        listing = self.scratch / "api-listing"
        if listing.exists():
            shutil.rmtree(listing)
        listing.mkdir()
        names = [p["AssemblyName"] for p in packables]
        self.run(["dotnet", str(tool / "bin" / "Release" / "net10.0" / "api-lister.dll"), str(listing), *names], tool, log, check=True)
        for name in names:
            self.write(f"api/{name}.txt", (listing / f"{name}.txt").read_text(encoding="utf-8"))
        self.write("api/api-lister.csproj", self.hide((tool / "api-lister.csproj").read_text(encoding="utf-8")))
        self.write("api/Program.cs", (tool / "Program.cs").read_text(encoding="utf-8"))
        self.write_gz("api/build.log.gz", "".join(log))

    # -- measure ---------------------------------------------------------------------------------

    def answers(self, log: list[str]) -> dict:
        result = self.run(["dotnet", str(self.bench / BENCH_DLL), "--number-conversion-answers"], self.bench, log, check=True)
        return json.loads(result.stdout.strip().splitlines()[-1])

    def measure(self) -> None:
        if self.bench is None:
            raise SystemExit("measure needs --bench-tree")
        out = "measurement"
        log: list[str] = []
        if (self.out / out).exists():
            # A collection is taken whole: a second one replaces the first, read-only manifest included.
            for path in (self.out / out).rglob("*"):
                path.chmod(0o644 if path.is_file() else 0o755)
            shutil.rmtree(self.out / out)
        base = self.git(self.tree, "rev-parse", "HEAD")
        rule = self.git(self.bench, "rev-parse", "HEAD")
        changed = self.git(self.bench, "diff", "--name-only", base, rule, "--", "Broiler.JS").splitlines()
        outside = [p for p in changed if not p.startswith(BENCH_PROJECT + "/")]
        identity = [f"base commit: {base}", f"rule commit: {rule}",
                    f"rule tree clean: {self.git(self.bench, 'status', '--porcelain') == ''}",
                    "files under Broiler.JS/ that differ between the base and the rule commit:"]
        identity += [f"  {p}" for p in changed] or ["  none"]
        identity.append(f"engine projects identical to the base: {not outside}")
        if outside:
            self.write(f"{out}/identity.txt", "\n".join(identity) + "\n")
            raise SystemExit("the rule tree's engine projects differ from the base")
        self.run(["dotnet", "build", "-c", "Release", "-nologo", BENCH_PROJECT], self.bench, log, check=True)
        binaries = sorted(Path(self.bench, BENCH_DLL).parent.glob("*.dll"))
        identity.append("arm binaries (B and B-again are the same build):")
        identity += [f"  {self.sha256(p)}  {p.name}" for p in binaries]
        identity.append(f"profile: baseline; repetitions per lane: {REPETITIONS}; lanes: B, B-again, rotated")
        manifest = self.write(f"{out}/manifest.txt", "\n".join(identity) + "\n")
        manifest.chmod(0o444)

        reference = None
        conditions = ["repetition\tlane\twhen\tanswers equal B's first\tconfiguration equal B's first"]
        runs = ["repetition\tlane\tworkload\tmedian_ns\tmean_ns\tallocated_bytes\tgen0\tgen1\tgen2"]
        environments = []
        for repetition in range(REPETITIONS):
            lanes = ("B", "B-again") if repetition % 2 == 0 else ("B-again", "B")
            for lane in lanes:
                artifacts = self.scratch / "bdn" / f"r{repetition}-{lane}"
                if artifacts.exists():
                    shutil.rmtree(artifacts)
                before = self.answers(log)
                reference = reference or before
                conditions.append(f"{repetition}\t{lane}\tbefore\t{before['answers'] == reference['answers']}\t{before['configuration'] == reference['configuration']}")
                env = dict(self.env, BROILER_BENCHMARK_PROFILE="baseline", BROILER_BENCHMARK_ARTIFACTS=str(artifacts))
                started = datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
                result = subprocess.run(["dotnet", str(self.bench / BENCH_DLL), "--filter", "*NumberConversionBenchmarks*", "--buildTimeout", "1800"],
                                        cwd=Path(self.bench, BENCH_DLL).parent, env=env, capture_output=True, text=True)
                log.append(f"# run r{repetition} {lane}: started {started}, exit {result.returncode}\n")
                after = self.answers(log)
                conditions.append(f"{repetition}\t{lane}\tafter\t{after['answers'] == reference['answers']}\t{after['configuration'] == reference['configuration']}")
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
        self.write(f"{out}/conditions.tsv", "\n".join(conditions) + "\n")
        self.write(f"{out}/runs.tsv", "\n".join(runs) + "\n")
        self.write(f"{out}/answers.json", json.dumps(reference, indent=1, sort_keys=True) + "\n")
        self.write(f"{out}/environments.txt", f"distinct BenchmarkDotNet host environment records: {len(set(environments))}\n"
                   + "\n".join(sorted(set(environments))) + "\n")
        self.write_gz(f"{out}/collect.log.gz", "".join(log))

        # The base reading the rule describes: Q per lane, the A/A floor, and E(B) against it.
        medians: dict[tuple[str, str], list[float]] = {}
        for line in runs[1:]:
            fields = line.split("\t")
            if fields[3]:
                medians.setdefault((fields[1], fields[2]), []).append(float(fields[3]))
        q = {key: statistics.median(values) for key, values in medians.items()}
        summary = ["workload\truns B\truns B-again\tQ(B) ns\tQ(B-again) ns\t|Q(B)-Q(B-again)| ns\tE(B) ns\treading"]
        for workload in MEASURED:
            control = workload + "Control"
            try:
                qb, qa, qc = q[("B", workload)], q[("B-again", workload)], q[("B", control)]
            except KeyError:
                summary.append(f"{workload}\t\t\t\t\t\t\tincomplete")
                continue
            floor, attribution = abs(qb - qa), qb - qc
            reading = "recorded" if abs(attribution) > floor else "below resolution"
            summary.append(f"{workload}\t{len(medians[('B', workload)])}\t{len(medians[('B-again', workload)])}\t"
                           f"{qb:.1f}\t{qa:.1f}\t{floor:.1f}\t{attribution:.1f}\t{reading}")
        self.write(f"{out}/summary.tsv", "\n".join(summary) + "\n")
        hashes = [f"{self.sha256(p)}  {p.relative_to(self.out / out)}" for p in sorted((self.out / out).rglob("*"))
                  if p.is_file() and p.name != "hashes.txt"]
        self.write(f"{out}/hashes.txt", "\n".join(hashes) + "\n")

    # -- manifest --------------------------------------------------------------------------------

    def manifest(self) -> None:
        files = {}
        for path in sorted(self.out.rglob("*")):
            if path.is_file() and path.name != "manifest.json":
                files[str(path.relative_to(self.out)).replace(os.sep, "/")] = self.sha256(path)
        manifest = {
            "bundle": "sjl-0-001",
            "repository": "Broiler.JS",
            "baseCommit": self.git(self.tree, "rev-parse", "HEAD"),
            "ruleCommit": self.git(self.bench, "rev-parse", "HEAD") if self.bench else None,
            "suiteRevision": SUITE_REVISION,
            "files": files,
        }
        (self.out / "manifest.json").write_text(json.dumps(manifest, indent=1, sort_keys=True) + "\n", encoding="utf-8", newline="\n")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--tree", required=True, help="a checkout of the base commit")
    parser.add_argument("--bench-tree", help="a checkout of the decision rule's commit (measure)")
    parser.add_argument("--suite-archive", help="the pinned test262 archive")
    parser.add_argument("--terser-module", help="terser's node module directory")
    parser.add_argument("--test262-from", help="import a run taken with the same commands")
    parser.add_argument("--max-workers", type=int, default=1)
    parser.add_argument("--out", required=True)
    parser.add_argument("--scratch", default=os.path.join(os.environ.get("TMPDIR", "/tmp"), "sjl-0-001-js"))
    parser.add_argument("--only", nargs="+", choices=STEPS)
    args = parser.parse_args(argv)
    collector = Collector(args)
    for step in args.only or STEPS:
        print(f"== {step}", flush=True)
        getattr(collector, step)()
    return 0


if __name__ == "__main__":
    sys.exit(main())
