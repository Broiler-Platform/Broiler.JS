#!/usr/bin/env python3
"""Collect Broiler.JS's evidence for milestone SJL-3 (work packages SJL-3.3 to SJL-3.7) as bundle sjl-3-001.

    python3 docs/evidence/sjl-3-001/collect.py --tree <a checkout at the after commit> \
        [--bench-tree <a checkout at the decision rule's commit>] [--test262-from <the after run>] \
        [--terser-module <terser's node module>] --out docs/evidence/sjl-3-001 --only STEP [STEP ...]

THE AFTER ARM IS A CHECKOUT, NOT THIS TREE. Every build and run of the after arm is taken at `--tree`,
a clean checkout of the commit that completes SJL-3.5's cut-over; the base is bundle sjl-0-001's,
read from its retained files, except the measurement's arm B, which part B of decision-rule.md
rebuilds from `--bench-tree`, the rule's own commit.

IT REUSES BUNDLE SJL-0-001'S COLLECTOR for every procedure the rule takes from the base - the test262
import, the measurement harness's runs - so the two arms run the same code, and adds what the base
did not have: the comparison of the two test262 runs, the after arm and its A/A lane, and the checks
of the area's home and its simplification.

THE COLLECTOR JUDGES NOTHING. Each step writes what it ran and what it printed, failures included; the
README reads them under the rule.

Steps, each with `--only`:

  identity        the after commit and its cleanliness, the base and the rule commits, the rule's
                  ancestry and the files it fixes unchanged since, the SDK, Node and the machine;
  home            the shared library at the after commit: the shared filter's tests (SL1 to SL10, the
                  library-level vectors and the API baseline among them), the corpus generator's own
                  tests, the Native AOT probe published, run over the corpus and compared with the same
                  probe under the JIT, and the shared set packed at 0.1.0-preview.1 with package
                  validation and restored and run by a pristine consumer, into home/;
  vectors         the engine-level vectors (Broiler.JavaScript.Vectors.Tests with
                  BROILER_VECTORS_REPORT), into engine/;
  test262         the after run, imported with `--test262-from` through the base's procedure, the
                  re-run of every timed-out row alone with one worker, and the comparison with the
                  base through scripts/compliance/compare_test262_paths.py, with test262/
                  classifications.json when it exists, into test262/;
  consumer        the engine set packed and restored by a pristine consumer
                  (scripts/packaging/test_pristine_consumer.py), into engine/;
  simplification  SJL-3.7: the deletion check against the area's Broiler.JS deletion list and the
                  search for surviving intra-engine duplicates, into simplification/;
  measure         part B: arms B, A and A-again rotated by repetition, seven runs each, into
                  measurement/, a sub-bundle of its own;
  manifest        manifest.json: the SHA-256 of every file the bundle retains.

Paths in retained logs are written as <tree>, <bench>, <repo>, <suite>, <run> and <scratch>.
"""

from __future__ import annotations

import argparse
import datetime
import gzip
import importlib.util
import json
import os
import platform
import re
import shutil
import statistics
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
BASE_BUNDLE = HERE.parent / "sjl-0-001"

_spec = importlib.util.spec_from_file_location("sjl_0_001_collect", BASE_BUNDLE / "collect.py")
base = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(base)

RULE = "docs/evidence/sjl-3-001/decision-rule.md"
# What the rule fixes with itself: it, the comparison script and the workloads (decision-rule.md,
# "Revisions" and part B's arm A).
RULE_FILES = (RULE, "scripts/compliance/compare_test262_paths.py",
              base.BENCH_PROJECT + "/NumberConversionBenchmarks.cs", base.BENCH_PROJECT + "/Program.cs",
              base.BENCH_PROJECT + "/BenchmarkConfiguration.cs")
SHARED_VERSION = "0.1.0-preview.1"
PROBE = "Broiler.ECMAScript/Broiler.ECMAScript.Probe"
VECTORS_TESTS = "Broiler.JS/Broiler.JavaScript.Vectors.Tests"
STEPS = ("identity", "home", "vectors", "test262", "consumer", "simplification", "measure", "manifest")
LANES = ("B", "A", "A-again")

# The area's Broiler.JS deletion list (Broiler.VM docs/javascript-shared-libraries/areas/numbers.md
# section 7), member by member. Each is searched as a whole word in the engine's product sources.
DELETED = ("NumberCoercion", "ParseCore", "ParseCoreStatus", "integerPowersOfTen", "ParseHex", "ParseOctal",
           "ParseBinary", "IsWhiteSpaceOrLineTerminator", "RefineEstimate", "AddUlps", "ScaleToInteger",
           "ToECMAString", "RoundTrippable", "NumberToECMAString", "RoundScaled", "ToExactFraction",
           "ExactSignificantDigits", "Compare10Pow", "ShortestSignificantDigits", "FormatExponential",
           "BigIntLiteralToValue", "ParseBigIntRadix", "TryParseBigIntLiteral", "trimCharacters")
# Kept by decision SJL-D-25 as forwarders, or as obsolete forwarders, and the one wrapper pair left.
KEPT = ("CoerceToNumber", "TryCoerceToUInt32", "TryGetArrayIndex", "DecimalToBase", "DecimalToArbitrarySystem", "JSTrim")
PRODUCT = ["--", "Broiler.JS", ":!Broiler.JS/*Tests*", ":!Broiler.JS/OtherTests", ":!Broiler.JS/benchmarks",
           ":!Broiler.JS/samples"]
# The surviving-duplicate search: every construct that converts between numbers and text, rounds
# by powers of ten, or classifies white space with a table other than the library's.
DUPLICATE_PATTERN = (r'double\.(Try)?Parse\(|decimal\.(Try)?Parse\(|\.ToString\("(R|G17|E[0-9]*|F[0-9]*|N[0-9]*)"'
                     r'|0123456789abcdefghijklmnopqrstuvwxyz|Math\.Pow\(10|char\.IsWhiteSpace|string\.IsNullOrWhiteSpace'
                     r'|\.Trim\(\)|BigInteger\.(Try)?Parse\(|NumberStyles\.(Float|AllowExponent|HexNumber)')


def utc() -> str:
    return datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


class Collector(base.Collector):
    def __init__(self, args: argparse.Namespace) -> None:
        super().__init__(args)
        self.base_commit = next(line.split(": ", 1)[1] for line in
                                (BASE_BUNDLE / "identity.txt").read_text(encoding="utf-8").splitlines()
                                if line.startswith("base commit: "))

    def log_to(self, relative: str, log: list[str]) -> None:
        self.write(relative, "".join(log))

    # -- identity --------------------------------------------------------------------------------

    def identity(self) -> None:
        after = self.git(self.tree, "rev-parse", "HEAD")
        rule = self.git(self.tree, "log", "-1", "--format=%H", "--", RULE)
        lines = [
            "bundle: sjl-3-001 (Broiler.JS)",
            f"after commit: {after}",
            f"after tree clean: {self.git(self.tree, 'status', '--porcelain') == ''}",
            f"base commit: {self.base_commit} (bundle sjl-0-001)",
            f"rule commit: {rule} (the last commit to touch {RULE})",
            f"rule commit an ancestor of the after commit: "
            f"{subprocess.run(['git', '-C', str(self.tree), 'merge-base', '--is-ancestor', rule, after]).returncode == 0}",
            "files the rule fixes, changed between the rule commit and the after commit:",
        ]
        changed = self.git(self.tree, "diff", "--name-only", rule, after, "--", *RULE_FILES).splitlines()
        lines += [f"  {p}" for p in changed] or ["  none"]
        lines.append(f"collector commit: {self.git(HERE, 'rev-parse', 'HEAD')}")
        if self.bench is not None:
            lines.append(f"bench tree (arm B): {self.git(self.bench, 'rev-parse', 'HEAD')}, "
                         f"clean: {self.git(self.bench, 'status', '--porcelain') == ''}")
        log: list[str] = []
        for command in (["dotnet", "--version"], ["dotnet", "--list-runtimes"], ["node", "--version"]):
            lines.append(f"{' '.join(command)}: {self.run(command, self.tree, log).stdout.strip()}")
        if self.terser is not None:
            lines.append(f"terser: {json.loads((self.terser / 'package.json').read_text(encoding='utf-8'))['version']}")
        lines.append(f"machine: {platform.machine()}; {platform.platform()}; logical processors {os.cpu_count()}")
        cpu = [l for l in Path("/proc/cpuinfo").read_text().splitlines() if l.startswith("model name")]
        memory = [l for l in Path("/proc/meminfo").read_text().splitlines() if l.startswith("MemTotal")]
        lines.append(f"cpu: {cpu[0].split(':', 1)[1].strip() if cpu else 'unknown'}")
        lines.append(f"memory: {memory[0].split(':', 1)[1].strip() if memory else 'unknown'}")
        lines.append(f"suite revision: {base.SUITE_REVISION}")
        lines.append(f"collected: {utc()}")
        self.write("identity.txt", "\n".join(lines) + "\n")

    # -- home ------------------------------------------------------------------------------------

    def home(self) -> None:
        log: list[str] = []
        self.run(["dotnet", "build", "Broiler.ECMAScript.slnf", "-c", "Release", "--nologo"], self.tree, log)
        self.log_to("home/build.log", log)
        log = []
        self.run(["dotnet", "test", "Broiler.ECMAScript.slnf", "-c", "Release", "--no-build", "--nologo",
                  "--logger", "console;verbosity=normal"], self.tree, log)
        self.log_to("home/tests.log", log)
        log = []
        self.run([sys.executable, "-m", "unittest", "discover", "-s", "scripts/conformance/tests", "-v"], self.tree, log)
        self.log_to("home/generator-tests.log", log)

        # The Native AOT probe: published, the publish directory listed, the corpus run, and the
        # report compared with the same probe's under the JIT.
        rid = "linux-x64"
        log = []
        result = self.run(["dotnet", "publish", PROBE, "-c", "Release", "-r", rid, "--nologo"], self.tree, log)
        warnings = sorted({line.strip() for line in result.stdout.splitlines() if ": warning " in line})
        log.append(f"# publish exit {result.returncode}; distinct warning lines: {len(warnings)}\n")
        log.extend(line + "\n" for line in warnings)
        self.log_to(f"home/probe-publish-{rid}.log", log)
        published = self.tree / PROBE / "bin" / "Release" / "net10.0" / rid / "publish"
        log = []
        self.run(["ls", "-l", str(published)], self.tree, log)
        managed = sorted(p.name for p in published.glob("*.dll"))
        log.append(f"# managed assemblies in the publish directory: {managed or 'none'}\n")
        aot = self.scratch / "probe-aot.tsv"
        jit = self.scratch / "probe-jit.tsv"
        self.run([str(published / "Broiler.ECMAScript.Probe"), "conformance/vectors", "--report", str(aot)], self.tree, log)
        self.run(["dotnet", "build", PROBE, "-c", "Release", "--nologo"], self.tree, log)
        self.run(["dotnet", str(self.tree / PROBE / "bin" / "Release" / "net10.0" / "Broiler.ECMAScript.Probe.dll"),
                  "conformance/vectors", "--report", str(jit)], self.tree, log)
        same = aot.exists() and jit.exists() and aot.read_bytes() == jit.read_bytes()
        log.append(f"# the Native AOT report is byte-identical to the JIT's: {same}\n")
        self.log_to(f"home/probe-run-{rid}.log", log)
        if aot.exists():
            self.write_gz("home/probe-report-aot.tsv.gz", aot.read_text(encoding="utf-8"))

        # The shared set at the preview version: packed with package validation, restored from a
        # local feed alone into an isolated cache, and run.
        log = []
        self.run([sys.executable, "scripts/packaging/test_pristine_consumer.py", "--set", "shared",
                  "--version", SHARED_VERSION], self.tree, log)
        self.log_to("home/pristine-consumer-shared.log", log)

    # -- vectors ---------------------------------------------------------------------------------

    def vectors(self) -> None:
        report = self.scratch / "vectors-report.tsv"
        report.unlink(missing_ok=True)
        log: list[str] = []
        env, self.env = self.env, dict(self.env, BROILER_VECTORS_REPORT=str(report))
        try:
            self.run(["dotnet", "test", VECTORS_TESTS, "-c", "Release", "--nologo",
                      "--logger", "console;verbosity=normal"], self.tree, log)
        finally:
            self.env = env
        self.log_to("engine/vectors-tests.log", log)
        if report.exists():
            text = report.read_text(encoding="utf-8")
            revision = re.search(r"^# corpus revision: (\d+)", text, re.M)
            self.write_gz(f"engine/vectors-revision-{revision.group(1) if revision else 'unknown'}.tsv.gz", text)

    # -- test262 ---------------------------------------------------------------------------------

    def test262(self) -> None:
        if self.test262_from is None:
            raise SystemExit("test262 imports the after run: pass --test262-from")
        merged = self.test262_from / "test262-results.json"
        # The re-run of every timed-out row alone, with one worker, at the after commit, as part A asks.
        rerun = self.test262_from / "rerun-timeouts.json"
        report = json.loads(merged.read_text(encoding="utf-8"))
        timed_out = sorted({str(r["path"]) for r in report["results"] if r["status"] == "timedOut"})
        if timed_out and not rerun.exists():
            result = subprocess.run(
                ["python3", "scripts/compliance/run_test262.py", "--suite-ref", base.SUITE_REVISION,
                 "--suite-root", str(self.test262_from.parent / "suite" / f"test262-{base.SUITE_REVISION}"),
                 "--broiler-dll", base.SHELL_DLL, "--test-timeout-seconds", str(base.TEST_TIMEOUT),
                 "--minifier", "terser", "--terser-module", str(self.terser),
                 "--minifier-timeout-seconds", str(base.MINIFIER_TIMEOUT), "--memory-limit-mb", str(base.MEMORY_LIMIT),
                 "--max-workers", "1", "--feature-match", "any", "--runner-os", "Linux", "--runner-arch", "X64",
                 "--dotnet-version", self.run(["dotnet", "--version"], self.tree, []).stdout.strip(),
                 "--output", str(rerun), "--no-summary-stdout", *timed_out],
                cwd=self.tree, env=self.env, capture_output=True, text=True)
            (self.test262_from / "rerun-timeouts.log").write_text(result.stderr, encoding="utf-8")
        super().test262()
        # What the resume after a restart left beside the run, which the base's import does not name.
        for entry in sorted(self.test262_from.iterdir()):
            if entry.name.startswith("interrupted-shard-") or entry.name == "shell-binaries.before-resume.sha256":
                self.write(f"test262/{entry.name}", entry.read_text(encoding="utf-8"))

        # The comparison with the base, through the script the rule committed.
        base_merged = self.scratch / "base-test262-results.json"
        with gzip.open(BASE_BUNDLE / "test262" / "test262-results.json.gz", "rt", encoding="utf-8") as handle:
            base_merged.write_text(handle.read(), encoding="utf-8")
        command = [sys.executable, "scripts/compliance/compare_test262_paths.py", "--base", str(base_merged),
                   "--after", str(merged), "--output", str(self.scratch / "comparison.json")]
        if (BASE_BUNDLE / "test262" / "rerun-timeouts.json").exists():
            command += ["--base-rerun", str(BASE_BUNDLE / "test262" / "rerun-timeouts.json")]
        if rerun.exists():
            command += ["--after-rerun", str(rerun)]
        classifications = self.out / "test262" / "classifications.json"
        if classifications.exists():
            command += ["--classifications", str(classifications)]
        log: list[str] = []
        self.run(command, self.tree, log)
        self.log_to("test262/comparison.log", log)
        comparison = self.scratch / "comparison.json"
        if comparison.exists():
            self.write("test262/comparison.json", comparison.read_text(encoding="utf-8"))

    # -- consumer --------------------------------------------------------------------------------

    def consumer(self) -> None:
        log: list[str] = []
        self.run([sys.executable, "scripts/packaging/test_pristine_consumer.py", "--set", "engine"], self.tree, log)
        self.log_to("engine/pristine-consumer.log", log)

    # -- simplification --------------------------------------------------------------------------

    def grep(self, *args: str, paths: list[str] | None = None) -> list[str]:
        result = subprocess.run(["git", "-C", str(self.tree), "grep", "-n", *args, *(paths or PRODUCT)],
                                capture_output=True, text=True)
        return result.stdout.splitlines()

    def simplification(self) -> None:
        after = self.git(self.tree, "rev-parse", "HEAD")
        lines = [f"# SJL-3.7 deletion check, Broiler.JS at {after}: every member of the area's deletion list",
                 "# (Broiler.VM docs/javascript-shared-libraries/areas/numbers.md section 7), searched as a whole word",
                 "# in the engine's product sources (Broiler.JS/, test projects, benchmarks and samples excluded),",
                 "# with git grep -w. A member listed with no line is absent.", ""]
        absent = 0
        for member in DELETED:
            hits = self.grep("-w", member)
            absent += not hits
            lines += [f"## {member}"] + hits + [""]
        lines.append(f"# deleted members absent: {absent} of {len(DELETED)}")
        lines += ["", "## NumberParser's declarations: the private ParseInt and ParseFloat bodies are on the list"]
        lines += self.grep("-E", r"(public|private|internal|protected).*\(",
                           paths=["--", "Broiler.JS/Broiler.JavaScript.Runtime/NumberParser.cs"])
        lines += ["", "## The kept public members (forwarders, SJL-D-25): " + ", ".join(KEPT)]
        for member in KEPT:
            lines += self.grep("-E", rf"public static .*\b{member}\(")
        lines += ["", "## The parseInt/parseFloat wrapper pairs: JSExport declarations of parseInt and parseFloat"]
        lines += self.grep("-E", r'JSExport\("parse(Int|Float)"')
        self.write("simplification/deletion-check.log", "\n".join(lines) + "\n")

        hits = self.grep("-E", DUPLICATE_PATTERN)
        search = [f"# SJL-3.7 search for a surviving intra-engine duplicate of the number area, Broiler.JS at {after}:",
                  "# every construct that converts between numbers and text, rounds by powers of ten, or classifies",
                  "# white space with a table other than the library's, in the engine's product sources (as above).",
                  f"# pattern: {DUPLICATE_PATTERN}", f"# hits: {len(hits)}; each is reviewed in the bundle's README", ""]
        search += [h[:220] for h in hits]
        self.write("simplification/duplicate-search.log", "\n".join(search) + "\n")

    # -- measure ---------------------------------------------------------------------------------

    def measure(self) -> None:
        if self.bench is None:
            raise SystemExit("measure needs --bench-tree")
        out = "measurement"
        if (self.out / out).exists():
            for path in (self.out / out).rglob("*"):
                path.chmod(0o644 if path.is_file() else 0o755)
            shutil.rmtree(self.out / out)
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
        identity.append("lane: smoke - this machine is not a controlled lane as Measurement.md's section "
                        "\"Controlled lanes and effective configuration\" requires; part B is void on it")
        manifest = self.write(f"{out}/manifest.txt", "\n".join(identity) + "\n")
        manifest.chmod(0o444)

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
                started = utc()
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
        self.write(f"{out}/conditions.tsv", "\n".join(conditions) + "\n")
        self.write(f"{out}/runs.tsv", "\n".join(runs) + "\n")
        self.write(f"{out}/answers.json", json.dumps(reference, indent=1, sort_keys=True) + "\n")
        self.write(f"{out}/environments.txt", f"distinct BenchmarkDotNet host environment records: {len(set(environments))}\n"
                   + "\n".join(sorted(set(environments))) + "\n")
        self.write_gz(f"{out}/collect.log.gz", "".join(log))

        # The rule's quantities. They are computed whatever the lane; on a smoke lane they decide nothing.
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
        hashes = [f"{self.sha256(p)}  {p.relative_to(self.out / out)}" for p in sorted((self.out / out).rglob("*"))
                  if p.is_file() and p.name != "hashes.txt"]
        self.write(f"{out}/hashes.txt", "\n".join(hashes) + "\n")

    # -- manifest --------------------------------------------------------------------------------

    def manifest(self) -> None:
        files = {}
        for path in sorted(self.out.rglob("*")):
            if path.is_file() and path.name != "manifest.json" and "__pycache__" not in path.parts:
                files[str(path.relative_to(self.out)).replace(os.sep, "/")] = self.sha256(path)
        manifest = {
            "bundle": "sjl-3-001",
            "repository": "Broiler.JS",
            "baseCommit": self.base_commit,
            "afterCommit": self.git(self.tree, "rev-parse", "HEAD"),
            "ruleCommit": self.git(self.tree, "log", "-1", "--format=%H", "--", RULE),
            "suiteRevision": base.SUITE_REVISION,
            "files": files,
        }
        (self.out / "manifest.json").write_text(json.dumps(manifest, indent=1, sort_keys=True) + "\n", encoding="utf-8", newline="\n")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--tree", required=True, help="a checkout of the after commit")
    parser.add_argument("--bench-tree", help="a checkout of the decision rule's commit (measure, arm B)")
    parser.add_argument("--terser-module", help="terser's node module directory")
    parser.add_argument("--test262-from", help="the after run's directory, taken with the base's commands")
    parser.add_argument("--max-workers", type=int, default=4)
    parser.add_argument("--out", required=True)
    parser.add_argument("--scratch", default=os.path.join(os.environ.get("TMPDIR", "/tmp"), "sjl-3-001-js"))
    parser.add_argument("--only", nargs="+", choices=STEPS, required=True)
    args = parser.parse_args(argv)
    args.suite_archive = None
    collector = Collector(args)
    for step in args.only:
        print(f"== {step} {utc()}", flush=True)
        getattr(collector, step)()
    return 0


if __name__ == "__main__":
    sys.exit(main())
