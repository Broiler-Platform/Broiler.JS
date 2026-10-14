#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Collect Broiler.JS's evidence for milestone SJL-4 (work package SJL-4.6) as bundle sjl-4-001.

    python3 docs/evidence/sjl-4-001/collect.py --tree <a checkout at the after commit> \
        [--test262-from <the after run>] [--terser-module <terser's node module>] \
        --out docs/evidence/sjl-4-001 --only STEP [STEP ...]

THE AFTER ARM IS A CHECKOUT, NOT THIS TREE. Every build and run of the after arm is taken at `--tree`,
a clean checkout of the commit that completes SJL-4.5's cut-over. THE BASE IS BUNDLE SJL-3-001'S
AFTER RUN, as decision-rule.md's header takes it, read from that bundle's retained files: its
test262 run (taken at b598028, whose shell ef67693 builds identically but for the commit each
assembly names) and its re-run of the rows that timed out.

IT REUSES BUNDLE SJL-3-001'S COLLECTOR, and through it sjl-0-001's, for every procedure the rule takes
from SJL-3's after run - the test262 import, the re-run of timed-out rows, the comparison - so the two
runs are imported by the same code. What it adds is SJL-4's: the rule's base condition, the shared
library at its new version, the numerics area's deletion list and its duplicate search.

THE COLLECTOR JUDGES NOTHING. Each step writes what it ran and what it printed, failures included; the
README reads them under the rule.

Steps, each with `--only`:

  identity        the after commit and its cleanliness, the rule commit, its ancestry and the files it
                  fixes unchanged since, the rule's base condition (the files under Broiler.JS/ and
                  Broiler.ECMAScript/ that differ between ef67693 and the rule commit), the SDK, Node
                  and the machine;
  home            the shared library at the after commit, as sjl-3-001's step takes it: the shared
                  filter's tests, the corpus generator's tests, the Native AOT probe over the corpus
                  against the JIT, and the shared set packed at 0.2.0-preview.1 with package validation
                  and restored and run by a pristine consumer, into home/;
  vectors         the engine-level vectors, into engine/;
  test262         the after run, imported with `--test262-from`, the re-run of every timed-out row alone
                  with one worker, and the comparison with bundle sjl-3-001's after run through
                  scripts/compliance/compare_test262_paths.py, with test262/classifications.json when
                  it exists, into test262/;
  consumer        the engine set packed and restored by a pristine consumer, into engine/;
  simplification  the numerics area's Broiler.JS deletion list (Broiler.VM
                  docs/javascript-shared-libraries/milestones/sjl-4-numerics.md, SJL-4.5), each member
                  searched in the file that held it, and the search for a surviving duplicate of the
                  area in the engine's product sources, into simplification/;
  manifest        manifest.json: the SHA-256 of every file the bundle retains.

Paths in retained logs are written as <tree>, <repo>, <suite>, <run> and <scratch>.
"""

from __future__ import annotations

import argparse
import gzip
import importlib.util
import json
import os
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
PRIOR_BUNDLE = HERE.parent / "sjl-3-001"

_spec = importlib.util.spec_from_file_location("sjl_3_001_collect", PRIOR_BUNDLE / "collect.py")
prior = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(prior)
base = prior.base

RULE = "docs/evidence/sjl-4-001/decision-rule.md"
# What the rule fixes with itself: it and the comparison script (decision-rule.md, "Revisions").
RULE_FILES = (RULE, "scripts/compliance/compare_test262_paths.py")
# The commit whose shell bundle sjl-3-001 shows its run's binaries are, and so the start of the
# rule's base condition.
BASE_SHELL_COMMIT = "ef67693"
SHARED_VERSION = "0.2.0-preview.1"
STEPS = ("identity", "home", "vectors", "test262", "consumer", "simplification", "manifest")

# The area's Broiler.JS deletion list (Broiler.VM docs/javascript-shared-libraries/milestones/
# sjl-4-numerics.md, SJL-4.5), entry by entry: what was deleted, the file or files that held it, and
# what is searched there. A body that stays as a forwarder shows its forwarding line.
JS = "Broiler.JS/Broiler.JavaScript.BuiltIns/"
DELETED = (
    ("JSValue.ToUint32(double)'s body", ["Broiler.JS/Broiler.JavaScript.Runtime/JSValue.cs"],
     r"ToUint32\(double"),
    ("the inline clamp in JSUint8ClampedArray", [JS + "Array/Typed/JSUint8ClampedArray.cs"],
     r"Math\.(Round|Clamp|Max|Min|Floor|Ceiling)\(|MidpointRounding"),
    ("the inline (Half) casts: JSMath.F16round, DataView, JSFloat16Array",
     [JS + "Objects/JSMath.cs", JS + "DataView/DataView.cs", JS + "Array/Typed/JSFloat16Array.cs"],
     r"\(Half\)|\bHalf\.|BitConverter\.(HalfToUInt16Bits|UInt16BitsToHalf)"),
    ("JSMath's round, cbrt, expm1, log1p, hypot, pow and sumPrecise kernels", [JS + "Objects/JSMath.cs"],
     r"\b(CorrectlyRoundedCbrt|TwoSum|SameNonFinite|Two1023|Two53|MaxUlp)\b|Math\.(Pow|Cbrt|Round)\("),
    ("the special-case-less JSValue.Power and the direct Math.Pow emission for **",
     ["Broiler.JS/Broiler.JavaScript.Runtime/JSValue.cs", "Broiler.JS/Broiler.JavaScript.Expressions",
      "Broiler.JS/Broiler.JavaScript.Compiler", "Broiler.JS/Broiler.JavaScript.LinqExpressions"],
     r"Math\.Pow\b|nameof\(Math\.Pow\)"),
    ("the rounding copies: JSTemporalRoundingOptions, JSTemporalZonedDateTime.Rounding, "
     "JSTemporalDuration.Arithmetic.RoundToIncrement, JSIntl's unsigned rounding",
     [JS + "Temporal", JS + "Intl"],
     r"\b(NormalizeRoundingMode|ApplyRoundingPicksEnd|RoundToIncrement|RoundToIncrementAsIfPositive|"
     r"RatioToDouble|NearestDouble|RoundOffsetToMinute|RoundRatio)\b|\"half(Expand|Even|Trunc|Ceil|Floor)\" *=>|"
     r"MidpointRounding|enum UnsignedRoundingMode"),
    ("JSBigInt's ToNumber, TryStringToBigInt, TryParseBigIntString, TryParsePrefixedDigits, "
     "BigIntegerToRadixString, Shift, and the bodies of AsIntN and AsUintN", [JS + "BigInt/JSBigInt.cs"],
     r"\b(ToNumber|TryStringToBigInt|TryParseBigIntString|TryParsePrefixedDigits|BigIntegerToRadixString|"
     r"Shift|AsIntN|AsUintN)\b"),
    ("JSBigIntExtensions.CompareToNumber", ["Broiler.JS"], r"\bCompareToNumber\b"),
)
# The search for a surviving duplicate of the numerics area: the platform's own approximations and
# conversions the shared library replaces, the rounding modes spelled as strings, binary16 by the
# platform, BigInt arithmetic by BigInteger's own parse and power, and the 32-bit wrap written out.
DUPLICATE_PATTERN = (r'Math\.(Pow|Cbrt|Round|Truncate)\(|double\.(ExpM1|LogP1)|\(Half\)|\bHalf\.'
                     r'|BitConverter\.(HalfToUInt16Bits|UInt16BitsToHalf)|MidpointRounding'
                     r'|"half(Expand|Even|Trunc|Ceil|Floor)"|BigInteger\.(Pow|Parse|TryParse)\('
                     r'|4294967296|unchecked\(\(int\)\(uint\)')


class Collector(prior.Collector):
    def __init__(self, args: argparse.Namespace) -> None:
        super().__init__(args)
        identity = (PRIOR_BUNDLE / "identity.txt").read_text(encoding="utf-8").splitlines()
        self.base_commit = next(line.split(": ", 1)[1] for line in identity if line.startswith("after commit: "))

    # -- identity --------------------------------------------------------------------------------

    def identity(self) -> None:
        after = self.git(self.tree, "rev-parse", "HEAD")
        rule = self.git(self.tree, "log", "--diff-filter=A", "--format=%H", "--", RULE).splitlines()[-1]
        lines = [
            "bundle: sjl-4-001 (Broiler.JS)",
            f"after commit: {after}",
            f"after tree clean: {self.git(self.tree, 'status', '--porcelain') == ''}",
            f"base: bundle sjl-3-001's after run, its after commit {self.base_commit}",
            f"rule commit: {rule} (the commit that added {RULE})",
            f"rule commit an ancestor of the after commit: "
            f"{subprocess.run(['git', '-C', str(self.tree), 'merge-base', '--is-ancestor', rule, after]).returncode == 0}",
            "files the rule fixes, changed between the rule commit and the after commit:",
        ]
        changed = self.git(self.tree, "diff", "--name-only", rule, after, "--", *RULE_FILES).splitlines()
        lines += [f"  {p}" for p in changed] or ["  none"]
        lines.append(f"the rule's base condition: files under Broiler.JS/ and Broiler.ECMAScript/ that differ "
                     f"between {BASE_SHELL_COMMIT} and the rule commit:")
        condition = self.git(self.tree, "diff", "--name-only", BASE_SHELL_COMMIT, rule, "--",
                             "Broiler.JS", "Broiler.ECMAScript").splitlines()
        lines += [f"  {p}" for p in condition] or ["  none"]
        lines.append(f"collector commit: {self.git(HERE, 'rev-parse', 'HEAD')}")
        log: list[str] = []
        for command in (["dotnet", "--version"], ["dotnet", "--list-runtimes"], ["node", "--version"]):
            lines.append(f"{' '.join(command)}: {self.run(command, self.tree, log).stdout.strip()}")
        if self.terser is not None:
            lines.append(f"terser: {json.loads((self.terser / 'package.json').read_text(encoding='utf-8'))['version']}")
        lines.append(f"machine: {prior.platform.machine()}; {prior.platform.platform()}; "
                     f"logical processors {os.cpu_count()}")
        cpu = [l for l in Path("/proc/cpuinfo").read_text().splitlines() if l.startswith("model name")]
        memory = [l for l in Path("/proc/meminfo").read_text().splitlines() if l.startswith("MemTotal")]
        lines.append(f"cpu: {cpu[0].split(':', 1)[1].strip() if cpu else 'unknown'}")
        lines.append(f"memory: {memory[0].split(':', 1)[1].strip() if memory else 'unknown'}")
        lines.append(f"suite revision: {base.SUITE_REVISION}")
        lines.append(f"collected: {prior.utc()}")
        self.write("identity.txt", "\n".join(lines) + "\n")

    # -- home ------------------------------------------------------------------------------------

    def home(self) -> None:
        # sjl-3-001's step, at this milestone's version of the shared set.
        version, prior.SHARED_VERSION = prior.SHARED_VERSION, SHARED_VERSION
        try:
            super().home()
        finally:
            prior.SHARED_VERSION = version

    # -- test262 ---------------------------------------------------------------------------------

    def test262(self) -> None:
        if self.test262_from is None:
            raise SystemExit("test262 imports the after run: pass --test262-from")
        merged = self.test262_from / "test262-results.json"
        rerun = self.test262_from / "rerun-timeouts.json"
        report = json.loads(merged.read_text(encoding="utf-8"))
        timed_out = sorted({str(r["path"]) for r in report["results"] if r["status"] == "timedOut"})
        # The re-run of every timed-out row alone, with one worker, at the after commit, as part A asks.
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
        base.Collector.test262(self)
        for entry in sorted(self.test262_from.iterdir()):
            if entry.name.startswith("interrupted-shard-") or entry.name == "shell-binaries.before-resume.sha256":
                self.write(f"test262/{entry.name}", entry.read_text(encoding="utf-8"))

        # The comparison with bundle sjl-3-001's after run, through the script the rule committed.
        base_merged = self.scratch / "base-test262-results.json"
        with gzip.open(PRIOR_BUNDLE / "test262" / "test262-results.json.gz", "rt", encoding="utf-8") as handle:
            base_merged.write_text(handle.read(), encoding="utf-8")
        command = [sys.executable, "scripts/compliance/compare_test262_paths.py", "--base", str(base_merged),
                   "--after", str(merged), "--output", str(self.scratch / "comparison.json"),
                   "--base-rerun", str(PRIOR_BUNDLE / "test262" / "rerun-timeouts.json")]
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

    # -- simplification --------------------------------------------------------------------------

    def simplification(self) -> None:
        after = self.git(self.tree, "rev-parse", "HEAD")
        lines = [f"# SJL-4.6 deletion check, Broiler.JS at {after}: every entry of the numerics area's Broiler.JS",
                 "# deletion list (Broiler.VM docs/javascript-shared-libraries/milestones/sjl-4-numerics.md, SJL-4.5),",
                 "# searched in the file or folder that held it with git grep -E. An entry with no line is absent;",
                 "# a line that remains is a forwarder or the library's own name, which the bundle's README reads.", ""]
        for entry, paths, pattern in DELETED:
            hits = self.grep("-E", pattern, paths=["--", *paths])
            lines += [f"## {entry}", f"# in {', '.join(paths)}: {pattern}"] + hits + [""]
        self.write("simplification/deletion-check.log", "\n".join(lines) + "\n")

        hits = self.grep("-E", DUPLICATE_PATTERN)
        search = [f"# SJL-4.6 search for a surviving intra-engine duplicate of the numerics area, Broiler.JS at {after}:",
                  "# every construct that approximates a Math function the shared library computes, converts binary16",
                  "# or a BigInt by the platform, rounds by a mode spelled as a string or by the platform's midpoint",
                  "# rule, or writes the 32-bit wrap out, in the engine's product sources (Broiler.JS/, test projects,",
                  "# benchmarks and samples excluded).",
                  f"# pattern: {DUPLICATE_PATTERN}", f"# hits: {len(hits)}; each is reviewed in the bundle's README", ""]
        search += [h[:220] for h in hits]
        self.write("simplification/duplicate-search.log", "\n".join(search) + "\n")

    # -- manifest --------------------------------------------------------------------------------

    def manifest(self) -> None:
        files = {}
        for path in sorted(self.out.rglob("*")):
            if path.is_file() and path.name != "manifest.json" and "__pycache__" not in path.parts:
                files[str(path.relative_to(self.out)).replace(os.sep, "/")] = self.sha256(path)
        manifest = {
            "bundle": "sjl-4-001",
            "repository": "Broiler.JS",
            "baseCommit": self.base_commit,
            "afterCommit": self.git(self.tree, "rev-parse", "HEAD"),
            "ruleCommit": self.git(self.tree, "log", "--diff-filter=A", "--format=%H", "--", RULE).splitlines()[-1],
            "suiteRevision": base.SUITE_REVISION,
            "files": files,
        }
        (self.out / "manifest.json").write_text(json.dumps(manifest, indent=1, sort_keys=True) + "\n",
                                                encoding="utf-8", newline="\n")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--tree", required=True, help="a checkout of the after commit")
    parser.add_argument("--terser-module", help="terser's node module directory")
    parser.add_argument("--test262-from", help="the after run's directory, taken with the base's commands")
    parser.add_argument("--max-workers", type=int, default=4)
    parser.add_argument("--out", required=True)
    parser.add_argument("--scratch", default=os.path.join(os.environ.get("TMPDIR", "/tmp"), "sjl-4-001-js"))
    parser.add_argument("--only", nargs="+", choices=STEPS, required=True)
    args = parser.parse_args(argv)
    args.suite_archive = None
    args.bench_tree = None
    collector = Collector(args)
    for step in args.only:
        print(f"== {step} {prior.utc()}", flush=True)
        getattr(collector, step)()
    return 0


if __name__ == "__main__":
    sys.exit(main())
