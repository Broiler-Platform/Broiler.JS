#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Collect Broiler.JS's evidence for milestone SJL-5 (work package SJL-5.6) as bundle sjl-5-001.

    python3 docs/evidence/sjl-5-001/collect.py --tree <a checkout of the after commit> \
        [--artifact-zip <the after run's test262-evidence artifact> --run-url <its workflow run> \
         --artifact-id <its id> --run-times <when it was dispatched and completed>] \
        --out docs/evidence/sjl-5-001 --only STEP [STEP ...]

THE AFTER ARM IS A CHECKOUT, NOT THIS TREE. Every local build and run is taken at `--tree`, a clean
checkout of a commit whose product sources (Broiler.JS/ and Broiler.ECMAScript/) are those of the
commit that lands SJL-5.5's cut-over; the identity step lists every file there that differs.

THE TEST262 RUNS ARE THE WORKFLOW'S. decision-rule.md lets each run be taken locally or by
.github/workflows/test262-evidence.yml, and both were taken by the workflow: the base at the rule's
commit, imported into test262/base/ unchanged with imported.txt beside it, and the after run at the
after commit, which the test262 step imports into test262/after/ the same way. The step then compares
the two through scripts/compliance/compare_test262_paths.py as the rule fixes it, with each run's
re-run of its timed-out rows and test262/classifications.json when it exists, and checks the script on
the after run: against itself, and against a copy with one passing row planted as a regression.

IT REUSES BUNDLE SJL-4-001'S COLLECTOR, and through it sjl-3-001's and sjl-0-001's, for the home's
checks, the engine-level vectors and the pristine consumer, at this milestone's version of the shared
set. What it adds is SJL-5's: the import of a workflow's run, the text area's deletion list and its
duplicate search.

THE COLLECTOR JUDGES NOTHING. Each step writes what it ran and what it printed, failures included; the
README reads them under the rule.

Steps, each with `--only`:

  identity        the after commit and its cleanliness, the cut-over commit and the product files that
                  differ from it, the rule commit, its ancestry and the files it fixes unchanged since,
                  the SDK, Node and the machine, into identity.txt;
  home            the shared library at the after commit, as sjl-3-001's step takes it: the shared
                  filter's tests, the corpus generators' tests, the Native AOT probe over the corpus
                  against the JIT, and the shared set packed at 0.1.0-preview.3 with package validation
                  and restored and run by a pristine consumer, into home/;
  vectors         the engine-level vectors, into engine/;
  test262         the after run's artifact imported into test262/after/, the comparison with the base,
                  and the script's checks, into test262/;
  consumer        the engine set packed and restored by a pristine consumer, into engine/;
  simplification  the text area's Broiler.JS deletion list (Broiler.VM
                  docs/javascript-shared-libraries/milestones/sjl-5-text-algorithms.md, SJL-5.5), each entry
                  searched in the file or folder that held it, and the search for a surviving duplicate
                  of the area in the engine's product sources, into simplification/;
  manifest        manifest.json: the SHA-256 of every file the bundle retains.

Paths in retained logs are written as <tree>, <repo>, <suite>, <run> and <scratch>.
"""

from __future__ import annotations

import argparse
import gzip
import hashlib
import importlib.util
import json
import os
import shutil
import subprocess
import sys
import zipfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
NUMERICS_BUNDLE = HERE.parent / "sjl-4-001"

_spec = importlib.util.spec_from_file_location("sjl_4_001_collect", NUMERICS_BUNDLE / "collect.py")
numerics = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(numerics)
prior = numerics.prior
base = numerics.base

RULE = "docs/evidence/sjl-5-001/decision-rule.md"
# What the rule fixes with itself: it and the comparison script (decision-rule.md, "Revisions").
RULE_FILES = (RULE, "scripts/compliance/compare_test262_paths.py")
# The rule's base: the commit that carries the rule, run before any code of SJL-5 landed (test262/base/).
BASE_COMMIT = "3309b15afe16c7d886fe7bc397997db923987b3a"
# The commit that lands SJL-5.5's cut-over, whose product sources the after arm must carry.
CUT_OVER = "37ece760c4fc170b3b3c0002e4d23f38c4d0d29e"
SHARED_VERSION = "0.1.0-preview.3"
STEPS = ("identity", "home", "vectors", "test262", "consumer", "simplification", "manifest")

# The area's Broiler.JS deletion list (Broiler.VM docs/javascript-shared-libraries/milestones/
# sjl-5-text-algorithms.md, SJL-5.5), entry by entry: what was deleted, the file or folder that held it,
# and what is searched there. An adapter that keeps a deleted member's name shows its line.
JS = "Broiler.JS/Broiler.JavaScript.BuiltIns/"
DELETED = (
    ("IsWellFormedUtf16 and ToWellFormedUtf16", ["Broiler.JS"], r"\b(IsWellFormedUtf16|ToWellFormedUtf16)\b"),
    ("StringPad's builder", [JS + "String/JSStringPrototype.Transform.cs"],
     r"\bStringPad\b|\.Pad(Left|Right)\(|StringBuilder|\.Append\("),
    ("JSString.GetSubstitution, and the initializer's local GetSubstitution", ["Broiler.JS"],
     r"\bstring GetSubstitution\(|JSString\.GetSubstitution\b|\bGetSubstitution\("),
    ("the UriHelper class", ["Broiler.JS"],
     r"\bUriHelper\b|\b(CreateCharacterSetLookupTable|CreateUnescapedLookupTable|ParseHexNumber|IsEscapeUnescaped|"
     r"Utf8Encode)\b"),
    ("the base64 and hex cores of JSUInt8Array", [JS + "Array/Typed/JSUInt8Array.cs"],
     r"\b(DecodeBase64|DecodeBase64Chunk|Base64DecodeResult|SkipAsciiWhitespace|Base64Value|GetBase64Alphabet|"
     r"TryParseHexByte|HexDigitValue)\b|Convert\.(To|From|TryFrom)(Base64|Hex)"),
    ("JSJSON.QuoteString", [JS + "Json"], r"\bQuoteString\b"),
    ("JSJsonParser's use of System.Text.Json for guest JSON, with its exception mapping", [JS + "Json"],
     r"System\.Text\.Json|Utf8JsonReader|JsonSerializer|JsonConverter|JsonReaderException|JsonException"),
)
# The search for a surviving duplicate of the text area: UTF-16 read or written by the platform or by
# the surrogate constants, padding by the platform, base64, hex and URI by the platform, JSON by
# System.Text.Json, a \uXXXX escape written by format string, and the line terminators and the byte
# order mark the whitespace set holds, spelled as code. Broiler.VM's bundle of the same name searches
# its own sources with the same pattern.
DUPLICATE_PATTERN = (r'char\.Is(High|Low)?Surrogate\(|char\.ConvertToUtf32\(|char\.ConvertFromUtf32\('
                     r'|0xD800|0xDC00|0xDBFF|0xDFFF|\.PadLeft\(|\.PadRight\('
                     r'|Convert\.(To|From|TryFrom)(Base64|Hex)|Uri\.(Escape|Unescape)DataString|WebUtility|HttpUtility'
                     r'|System\.Text\.Json|Utf8JsonReader|JsonDocument|JsonSerializer'
                     r'|"[xX]4"|0x2028|0x2029|0xFEFF|0x180E')


class Collector(numerics.Collector):
    def __init__(self, args: argparse.Namespace) -> None:
        super().__init__(args)
        self.base_commit = BASE_COMMIT
        self.artifact_zip = Path(args.artifact_zip).resolve() if args.artifact_zip else None
        self.run_url = args.run_url
        self.artifact_id = args.artifact_id
        self.run_times = args.run_times

    # -- identity --------------------------------------------------------------------------------

    def identity(self) -> None:
        after = self.git(self.tree, "rev-parse", "HEAD")
        rule = self.git(self.tree, "log", "--diff-filter=A", "--format=%H", "--", RULE).splitlines()[-1]
        lines = [
            "bundle: sjl-5-001 (Broiler.JS)",
            f"after commit: {after}",
            f"after tree clean: {self.git(self.tree, 'status', '--porcelain') == ''}",
            f"cut-over commit: {CUT_OVER} (SJL-5.5)",
            "files under Broiler.JS/ and Broiler.ECMAScript/ that differ between the cut-over commit and the "
            "after commit:",
        ]
        changed = self.git(self.tree, "diff", "--name-only", CUT_OVER, after, "--",
                           "Broiler.JS", "Broiler.ECMAScript").splitlines()
        lines += [f"  {p}" for p in changed] or ["  none"]
        lines += [
            f"base: test262/base/, taken at {BASE_COMMIT} (test262/base/imported.txt)",
            f"rule commit: {rule} (the commit that added {RULE})",
            f"rule commit an ancestor of the after commit: "
            f"{subprocess.run(['git', '-C', str(self.tree), 'merge-base', '--is-ancestor', rule, after]).returncode == 0}",
            "files the rule fixes, changed between the rule commit and the after commit:",
        ]
        fixed = self.git(self.tree, "diff", "--name-only", rule, after, "--", *RULE_FILES).splitlines()
        lines += [f"  {p}" for p in fixed] or ["  none"]
        lines.append(f"collector commit: {self.git(HERE, 'rev-parse', 'HEAD')}")
        log: list[str] = []
        for command in (["dotnet", "--version"], ["dotnet", "--list-runtimes"], ["node", "--version"]):
            lines.append(f"{' '.join(command)}: {self.run(command, self.tree, log).stdout.strip()}")
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
            prior.Collector.home(self)
        finally:
            prior.SHARED_VERSION = version

    # -- test262 ---------------------------------------------------------------------------------

    def test262(self) -> None:
        if self.artifact_zip is None or not self.run_url or not self.artifact_id or not self.run_times:
            raise SystemExit("test262 imports the after run: pass --artifact-zip, --run-url, --artifact-id "
                             "and --run-times")
        after = self.out / "test262" / "after"
        if after.exists():
            raise SystemExit(f"{after} exists; the import writes it once")
        after.mkdir(parents=True)
        with zipfile.ZipFile(self.artifact_zip) as archive:
            names = sorted(archive.namelist())
            archive.extractall(after)
        digest = hashlib.sha256(self.artifact_zip.read_bytes()).hexdigest()
        run = (after / "run.txt").read_text(encoding="utf-8") if (after / "run.txt").exists() else ""
        tested = next((line.split(": ", 1)[1] for line in run.splitlines() if line.startswith("tested commit: ")),
                      "(run.txt names none)")
        (after / "imported.txt").write_text("\n".join([
            "# imported: the after run was taken by .github/workflows/test262-evidence.yml, not locally",
            f"workflow run: {self.run_url}, {self.run_times}",
            f"tested commit: {tested}, whose product sources are the cut-over commit {CUT_OVER}'s but for "
            "the files identity.txt lists",
            f"artifact: test262-evidence, id {self.artifact_id}, {self.artifact_zip.stat().st_size:,} bytes, "
            f"sha256 {digest} (the zip as downloaded)",
            f"retained: every file of the artifact, unchanged, in this directory ({len(names)} entries); this "
            "file is the only one added",
        ]) + "\n", encoding="utf-8", newline="\n")

        merged = {}
        for side in ("base", "after"):
            merged[side] = self.scratch / f"{side}-test262-results.json"
            with gzip.open(self.out / "test262" / side / "test262-results.json.gz", "rt", encoding="utf-8") as handle:
                merged[side].write_text(handle.read(), encoding="utf-8")
        script = self.tree / "scripts" / "compliance" / "compare_test262_paths.py"
        output = self.scratch / "comparison.json"
        output.unlink(missing_ok=True)
        command = [sys.executable, str(script), "--base", str(merged["base"]), "--after", str(merged["after"]),
                   "--output", str(output)]
        for side in ("base", "after"):
            rerun = self.out / "test262" / side / "rerun-timeouts.json"
            if rerun.exists():
                command += [f"--{side}-rerun", str(rerun)]
        classifications = self.out / "test262" / "classifications.json"
        if classifications.exists():
            command += ["--classifications", str(classifications)]
        log: list[str] = []
        self.run(command, self.tree, log)
        self.log_to("test262/comparison.log", log)
        if output.exists():
            self.write("test262/comparison.json", output.read_text(encoding="utf-8"))

        # The script on the after run: against itself (no difference), and against a copy with one
        # passing row turned to failed (one regression, exit 1).
        checks: list[str] = []
        self.run([sys.executable, str(script), "--base", str(merged["after"]), "--after", str(merged["after"])],
                 self.tree, checks)
        mutated = json.loads(merged["after"].read_text(encoding="utf-8"))
        victim = next(r for r in sorted(mutated["results"], key=lambda r: (str(r["path"]), str(r.get("variant", ""))))
                      if r["status"] == "passed")
        victim["status"] = "failed"
        copy = self.scratch / "test262-negative-control.json"
        copy.write_text(json.dumps(mutated), encoding="utf-8")
        checks.append(f"# negative control: {victim['path']} [{victim.get('variant', 'original')}] turned from "
                      "passed to failed in a copy\n")
        self.run([sys.executable, str(script), "--base", str(merged["after"]), "--after", str(copy)], self.tree, checks)
        copy.unlink()
        self.write("test262/compare-checks.log", "".join(checks))

    # -- simplification --------------------------------------------------------------------------

    def simplification(self) -> None:
        after = self.git(self.tree, "rev-parse", "HEAD")
        lines = [f"# SJL-5.6 deletion check, Broiler.JS at {after}: every entry of the text area's Broiler.JS",
                 "# deletion list (Broiler.VM docs/javascript-shared-libraries/milestones/sjl-5-text-algorithms.md,",
                 "# SJL-5.5), searched in the file or folder that held it with git grep -E, test projects included",
                 "# where the folder holds them. An entry with no line is absent; a line that remains is an adapter,",
                 "# a comment or the library's own name, which the bundle's README reads.", ""]
        for entry, paths, pattern in DELETED:
            hits = self.grep("-E", pattern, paths=["--", *paths])
            lines += [f"## {entry}", f"# in {', '.join(paths)}: {pattern}"] + hits + [""]
        self.write("simplification/deletion-check.log", "\n".join(lines) + "\n")

        hits = self.grep("-E", DUPLICATE_PATTERN)
        search = [f"# SJL-5.6 search for a surviving intra-engine duplicate of the text area, Broiler.JS at {after}:",
                  "# every construct that reads or writes UTF-16 by the platform or by the surrogate constants, pads",
                  "# by the platform, converts base64, hex or a URI by the platform, reads JSON by System.Text.Json,",
                  "# writes a \\uXXXX escape by format string, or spells a line terminator or the byte order mark, in",
                  "# the engine's product sources (Broiler.JS/, test projects, benchmarks and samples excluded).",
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
            "bundle": "sjl-5-001",
            "repository": "Broiler.JS",
            "baseCommit": BASE_COMMIT,
            "afterCommit": self.git(self.tree, "rev-parse", "HEAD"),
            "cutOverCommit": CUT_OVER,
            "ruleCommit": self.git(self.tree, "log", "--diff-filter=A", "--format=%H", "--", RULE).splitlines()[-1],
            "suiteRevision": base.SUITE_REVISION,
            "files": files,
        }
        (self.out / "manifest.json").write_text(json.dumps(manifest, indent=1, sort_keys=True) + "\n",
                                                encoding="utf-8", newline="\n")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--tree", required=True, help="a checkout of the after commit")
    parser.add_argument("--artifact-zip", help="the after run's test262-evidence artifact, as downloaded")
    parser.add_argument("--run-url", help="the after run's workflow run, with its attempt")
    parser.add_argument("--artifact-id", help="the artifact's id")
    parser.add_argument("--run-times", help="when the after run was dispatched and completed")
    parser.add_argument("--out", required=True)
    parser.add_argument("--scratch", default=os.path.join(os.environ.get("TMPDIR", "/tmp"), "sjl-5-001-js"))
    parser.add_argument("--only", nargs="+", choices=STEPS, required=True)
    args = parser.parse_args(argv)
    args.suite_archive = None
    args.bench_tree = None
    args.terser_module = None
    args.test262_from = None
    args.max_workers = 4
    collector = Collector(args)
    for step in args.only:
        print(f"== {step} {prior.utc()}", flush=True)
        getattr(collector, step)()
    shutil.rmtree(collector.scratch, ignore_errors=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
