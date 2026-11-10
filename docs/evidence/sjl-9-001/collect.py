#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Collect Broiler.JS's evidence for milestone SJL-9 (work package SJL-9.6) into bundle sjl-9-001.

    python3 -I docs/evidence/sjl-9-001/collect.py --out docs/evidence/sjl-9-001 --only STEP [STEP ...] [options]

THE RULE IS decision-rule.md BESIDE THIS FILE, committed at SJL-9.1, read beside its two dated revisions,
decision-rule-revised-2026-11-09.md (parts A and B: class L, test/intl402/DateTimeFormat/prototype/format/
numbering-system.js, with its probe) and decision-rule-revised-2026-11-10.md (part C: class L19, locales:128).
Neither revision rewrites the rule, and this collector changes no file either names.

PARTS A AND B are the four hosted runs of .github/workflows/test262-evidence.yml, the base at the after commit's first
parent and the after run at the after commit, each part's pair in its own zone. The `test262` step imports one run's
artifact test262-evidence, unchanged, with the digest of the zip as downloaded; the `compare` step reads each part's
two runs (completeness, host-zone records) and compares them through scripts/compliance/compare_test262_paths.py as
committed at SJL-0.9, first with the rule's own classification file beside the report (test262/comparison-utc/ and
test262/comparison-new-york/), then, with --revised, again over the same imported runs with the revision's file
test262/revised-2026-11-09/classifications.json, into test262/revised-2026-11-09/comparison-*/, as that revision's
item 3 says. The two parts' runs are never compared with each other.

THE CLASS-L PROBE (`probe`) runs test262/revised-2026-11-09/class-l-probe.js in a shell given with --shell, in the
part's zone, and records the shell's binaries beside the after run's own record of the binaries it ran
(shell-binaries.sha256), so the file says whether it is that shell.

PARTS C AND D run the harness the rule names, scripts/conformance/run_reference_datasets.py --bundle sjl-9-001, of this
checkout, over a shell built in Release in a scratch tree that `git archive` wrote from the commit (--tree); part D
adds --invariant-globalization and runs scripts/compliance/run_test262.py over test/intl402/default-locale-is-
supported.js with DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1, in both variants (original and terser). The `reference`
step runs each of part C's programs once in the reference reference.txt names, its tarball checked against the
digest that file records.

THE COLLECTOR JUDGES NOTHING. Each step writes what it ran and what it printed, failures included; the README reads
them under the rule and its revisions.

Steps, each with `--only`:

  identity   the base and after commits, the rule's conditions on the base, the files the rule fixes unchanged
             between the two, the rule files' blobs, part C's harness and copies, into identity.txt;
  test262    with --part and --side, one hosted run imported into test262/<base|after>[-new-york]/, with
             imported.txt; it writes the folder once;
  compare    with --part, the part's two runs' completeness and host-zone records and the comparison, with the
             part's classifications.json, and the script's checks; with --revised, the same over the revision's
             classification file, into test262/revised-2026-11-09/;
  probe      with --part, --shell and --after-run, test262/revised-2026-11-09/class-l-probe.<part>.txt;
  datasets   with --side and --tree, part C's run into datasets/<side>/ (and, for after, the join with the base);
  invariant  with --side, --tree, --suite-root and --terser-module, part D's runs into invariant/<side>/;
  reference  with --node-tarball, each program once in the reference, into datasets/reference/;
  manifest   manifest.json: the SHA-256 of every other file the bundle retains.

Paths in retained logs are written as <repo>, <tree>, <suite>, <scratch> and <tmp>.
"""

from __future__ import annotations

import argparse
import datetime
import gzip
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
import tarfile
import zipfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[2]
BUNDLE = "docs/evidence/sjl-9-001"
RULE = f"{BUNDLE}/decision-rule.md"
REVISIONS = (f"{BUNDLE}/decision-rule-revised-2026-11-09.md", f"{BUNDLE}/decision-rule-revised-2026-11-10.md")
# What the rule fixes with itself: it, the comparison script, the workflow parts A and B dispatch, and the scripts
# the workflow runs and merges with; and part C's harness, its driver and the copies.
RULE_FILES = (RULE, "scripts/compliance/compare_test262_paths.py", ".github/workflows/test262-evidence.yml",
              ".github/actions/run-test262-shard/action.yml", "scripts/compliance/run_test262.py",
              "scripts/compliance/merge_test262_shards.py")
DATASET_FILES = ("scripts/conformance/run_reference_datasets.py", "scripts/conformance/reference_dataset_driver.js",
                 "conformance/datasets/intl", "conformance/datasets/time", f"{BUNDLE}/divergence-groups.tsv",
                 f"{BUNDLE}/reference.txt")
# The commits the rule names before they exist (JSD-0082 decision D4): the after commit is the default-branch merge
# that lands SJL-9.5's cut-over (Broiler-Platform/Broiler.JS#1110), and the base is its first parent.
AFTER_COMMIT = "a5f2692e88a9aafeaa850bebe9c3399f6263353b"
BASE_COMMIT = "8841799642ff61499faa01dc894b6fec768c8af6"
# The rule commit (Broiler-Platform/Broiler.JS#1077) and bundle sjl-12-001's after commit, which the base descends from.
RULE_COMMIT = "50978882"
SJL12_AFTER = "c29d653f"
# The rule's product paths: a first-parent successor of the base that changes one is SJL-9.5's.
PRODUCT_PATHSPECS = (":(glob)Broiler.JS/Broiler.JavaScript.*/**", "Broiler.ECMAScript", ":(exclude)Broiler.ECMAScript/docs",
                     "Directory.Packages.props")
PARTS = {
    "A": {"base": "base", "after": "after", "compared": "comparison-utc", "zone": "UTC", "tz": None,
          "configuration": "part A's, in the runner's own zone (time_zone empty: TZ unset, UTC)"},
    "B": {"base": "base-new-york", "after": "after-new-york", "compared": "comparison-new-york",
          "zone": "America/New_York", "tz": "America/New_York",
          "configuration": "part B's, with time_zone America/New_York (TZ set for every shard and the re-run)"},
}
REVISED = "test262/revised-2026-11-09"
SHELL_DLL = "Broiler.JS/Broiler.JavaScript/bin/Release/net10.0/BroilerJS.dll"
SHELL_PROJECT = "Broiler.JS/Broiler.JavaScript/Broiler.JavaScript.csproj"
DEFAULT_LOCALE_TEST = "test/intl402/default-locale-is-supported.js"
INVARIANT = "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"


def utc() -> str:
    return datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with open(path, "rb") as handle:
        for block in iter(lambda: handle.read(1 << 20), b""):
            digest.update(block)
    return digest.hexdigest()


class Collector:
    def __init__(self, args: argparse.Namespace) -> None:
        self.args = args
        self.out = Path(args.out).resolve()
        self.scratch = Path(args.scratch).resolve()
        self.tree = Path(args.tree).resolve() if args.tree else None
        self.scratch.mkdir(parents=True, exist_ok=True)
        self.env = dict(os.environ, DOTNET_CLI_TELEMETRY_OPTOUT="1", DOTNET_NOLOGO="1")
        for name in ("LANG", "LC_ALL", "LANGUAGE"):
            self.env.pop(name, None)

    # -- helpers ---------------------------------------------------------------------------------

    def hide(self, text: str) -> str:
        names = [(self.scratch, "<scratch>"), (REPO, "<repo>")]
        if self.tree is not None:
            names.append((self.tree, "<tree>"))
        if getattr(self.args, "suite_root", None):
            names.append((Path(self.args.suite_root).resolve(), "<suite>"))
        for path, name in sorted(names, key=lambda n: -len(str(n[0]))):
            text = text.replace(str(path), name)
        return text

    def write(self, relative: str, text: str) -> Path:
        path = self.out / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(self.hide(text), encoding="utf-8", newline="\n")
        return path

    def run(self, command: list[str], cwd: Path, log: list[str], env: dict | None = None) -> subprocess.CompletedProcess:
        started = utc()
        result = subprocess.run(command, cwd=cwd, env=env or self.env, capture_output=True, text=True)
        log.append(f"$ {' '.join(command)}\n# cwd {cwd}; started {started}; exit {result.returncode}\n")
        log.append(result.stdout)
        if result.stderr:
            log.append("# stderr\n" + result.stderr)
        return result

    @staticmethod
    def git(*args: str, tree: Path = REPO) -> str:
        return subprocess.run(["git", "-C", str(tree), *args], capture_output=True, text=True).stdout.strip()

    def is_ancestor(self, older: str, newer: str) -> bool:
        return subprocess.run(["git", "-C", str(REPO), "merge-base", "--is-ancestor", older, newer]).returncode == 0

    def need(self, *names: str) -> None:
        missing = [n for n in names if not getattr(self.args, n.replace("-", "_"), None)]
        if missing:
            raise SystemExit("this step needs " + ", ".join("--" + n for n in missing))

    # -- identity --------------------------------------------------------------------------------

    def identity(self) -> None:
        parents = self.git("rev-list", "--parents", "-n", "1", AFTER_COMMIT).split()
        successors = self.git("rev-list", "--first-parent", "--ancestry-path", f"{BASE_COMMIT}..{AFTER_COMMIT}").split()
        changing = [c for c in successors
                    if self.git("diff", "--name-only", f"{c}^1", c, "--", *PRODUCT_PATHSPECS)]
        lines = [
            "bundle: sjl-9-001 (Broiler.JS), its parts A to D",
            f"collected: {utc()}, by {BUNDLE}/collect.py at {self.git('rev-parse', 'HEAD')}",
            f"after commit: {AFTER_COMMIT} ({self.git('log', '-1', '--format=%s', AFTER_COMMIT)})",
            f"after commit's parents: {' '.join(parents[1:])}",
            f"base commit: {BASE_COMMIT}, the after commit's first parent: {parents[1:2] == [BASE_COMMIT]}",
            f"rule commit: {self.git('rev-parse', RULE_COMMIT)} (the merge of Broiler-Platform/Broiler.JS#1077)",
            f"the base descends from the rule commit: {self.is_ancestor(RULE_COMMIT, BASE_COMMIT)}",
            f"the base descends from bundle sjl-12-001's after commit {self.git('rev-parse', SJL12_AFTER)}: "
            f"{self.is_ancestor(SJL12_AFTER, BASE_COMMIT)}",
            f"first-parent successors of the base up to the after commit: {' '.join(successors)}",
            f"of those, the ones that change a product path the rule names (Broiler.JS/Broiler.JavaScript.*/, "
            f"Broiler.ECMAScript/ outside docs/, Directory.Packages.props): {' '.join(changing) or 'none'}",
            "",
            "# the files the rule fixes, by blob at the rule commit, the base and the after commit",
        ]
        for path in RULE_FILES:
            blobs = [self.git("rev-parse", f"{c}:{path}") or "(absent)" for c in (RULE_COMMIT, BASE_COMMIT, AFTER_COMMIT)]
            lines.append(f"{path}: rule {blobs[0][:12]}, base {blobs[1][:12]}, after {blobs[2][:12]}; "
                         f"base and after the same: {blobs[1] == blobs[2]}")
        lines += ["", "# the rule and its dated revisions, by blob at the after commit and in this checkout"]
        for path in (RULE,) + REVISIONS:
            lines.append(f"{path}: after {self.git('rev-parse', f'{AFTER_COMMIT}:{path}')}; here "
                         f"{self.git('hash-object', str(REPO / path))}")
        lines += ["", "# part C's harness, driver, copies and tables, by tree or blob at the base and the after commit"]
        for path in DATASET_FILES:
            blobs = [self.git("rev-parse", f"{c}:{path}") for c in (BASE_COMMIT, AFTER_COMMIT)]
            lines.append(f"{path}: base {blobs[0][:12]}, after {blobs[1][:12]}; the same: {blobs[0] == blobs[1]}")
        self.write("identity.txt", "\n".join(lines) + "\n")

    # -- test262 ---------------------------------------------------------------------------------

    def test262(self) -> None:
        self.need("part", "side", "artifact-zip", "run-url", "artifact-id", "run-times")
        part = PARTS[self.args.part]
        side = self.args.side
        folder = self.out / "test262" / part[side]
        if folder.exists():
            raise SystemExit(f"{folder} exists; the import writes it once")
        zip_path = Path(self.args.artifact_zip).resolve()
        folder.mkdir(parents=True)
        with zipfile.ZipFile(zip_path) as archive:
            names = sorted(archive.namelist())
            archive.extractall(folder)
        run = (folder / "run.txt").read_text(encoding="utf-8") if (folder / "run.txt").exists() else ""
        fields = dict(line.split(": ", 1) for line in run.splitlines() if ": " in line)
        expected = AFTER_COMMIT if side == "after" else BASE_COMMIT
        role = ("the after commit, the merge that lands SJL-9.5's cut-over (Broiler-Platform/Broiler.JS#1110)"
                if side == "after" else "the base commit, the after commit's first parent")
        (folder / "imported.txt").write_text("\n".join([
            "# imported: the run was taken by .github/workflows/test262-evidence.yml, not locally",
            f"workflow run: {self.args.run_url}, {self.args.run_times}",
            f"tested commit: {fields.get('tested commit', '(run.txt names none)')} ({role}); the commit the rule "
            f"names: {fields.get('tested commit') == expected}",
            f"configuration: {part['configuration']}, with negative-metadata tests (include_negative true); "
            f"includeNegative {fields.get('includeNegative', '(not in run.txt)')} in run.txt",
            f"runner: {fields.get('runner', '(not in run.txt)')}; dotnetVersion {fields.get('dotnetVersion', '?')}",
            f"artifact: test262-evidence, id {self.args.artifact_id}, {zip_path.stat().st_size:,} bytes, sha256 "
            f"{sha256(zip_path)} (the zip as downloaded)",
            f"retained: every file of the artifact, unchanged, in this directory ({len(names)} entries); this file is "
            "the only one added",
        ]) + "\n", encoding="utf-8", newline="\n")

    # -- compare ---------------------------------------------------------------------------------

    @staticmethod
    def completeness(folder: Path, merged: dict) -> list[str]:
        """The rule's completeness conditions for one run, as its own files state them (sjl-7-001's reading)."""
        run = (folder / "run.txt").read_text(encoding="utf-8") if (folder / "run.txt").exists() else ""
        fields = dict(line.split(": ", 1) for line in run.splitlines() if ": " in line)
        configuration = merged.get("runConfiguration") or {}
        rerun = folder / "rerun-timeouts.json"
        rerun_negative = json.loads(rerun.read_text(encoding="utf-8")).get("includeNegative") if rerun.exists() else None
        shells = (folder / "shell-hashes.log").read_text(encoding="utf-8").splitlines() if (folder / "shell-hashes.log").exists() else []
        zone = (folder / "host-zone.log").read_text(encoding="utf-8").splitlines() if (folder / "host-zone.log").exists() else []
        statuses = sorted((folder / "shards").glob("*-status.json"))
        phases = sorted({json.loads(p.read_text(encoding="utf-8")).get("phase", "?") for p in statuses})
        return [
            f"tested commit (run.txt): {fields.get('tested commit', '?')}",
            f"workflow run (run.txt): {fields.get('workflow run', '?')}",
            f"runner (run.txt): {fields.get('runner', '?')}; dotnetVersion {fields.get('dotnetVersion', '?')}",
            f"expected shards: {merged.get('expectedShards')}; reported: {merged.get('reportedShards')}; "
            f"incomplete: {merged.get('incompleteShards')}; retried: {merged.get('retriedShards')}; "
            f"status files: {len(statuses)}, phases {phases}",
            f"configuration failures: {merged.get('configurationFailures')}",
            f"suite revisions (suiteRefs): {merged.get('suiteRefs')}",
            f"includeNegative: run.txt {fields.get('includeNegative', '?')}; merged results "
            f"{configuration.get('includeNegative')}; re-run {rerun_negative}",
            f"selection: {configuration.get('selectionMode')}; workers {configuration.get('maxWorkers')}; "
            f"timeout {configuration.get('testTimeoutSeconds')} s; memory limit {configuration.get('memoryLimitMb')}; "
            f"minifier {configuration.get('minifier')} {configuration.get('minifierVersion')}",
            "shell: " + "; ".join(l for l in shells if l.startswith("shards:") or l.startswith("the re-run")),
            "zone: " + "; ".join(l for l in zone if l.startswith("expected:") or l.startswith("shards:")),
        ]

    def compare(self) -> None:
        self.need("part")
        part = PARTS[self.args.part]
        root = self.out / "test262"
        folders = {"base": part["base"], "after": part["after"]}
        target = f"{REVISED}/{part['compared']}" if self.args.revised else f"test262/{part['compared']}"
        classifications = (self.out / REVISED / "classifications.json" if self.args.revised
                           else root / part["compared"] / "classifications.json")
        merged, loaded = {}, {}
        for side, folder in folders.items():
            merged[side] = self.scratch / f"{side}-{self.args.part}-test262-results.json"
            with gzip.open(root / folder / "test262-results.json.gz", "rt", encoding="utf-8") as handle:
                text = handle.read()
            merged[side].write_text(text, encoding="utf-8")
            loaded[side] = json.loads(text)
        if not self.args.revised:
            lines = [f"# Part {self.args.part}'s completeness conditions for each run, read from the files the workflow "
                     "wrote:", "# every expected shard reported, no configuration failure, one suite revision, one shell, "
                     "every", "# shard in the zone, includeNegative true.", ""]
            for side, folder in folders.items():
                lines += [f"## {side}: test262/{folder}/"] + self.completeness(root / folder, loaded[side]) + [""]
            self.write(f"{target}/completeness.log", "\n".join(lines))
            zones, records = [], {}
            for side, folder in folders.items():
                path = root / folder / "host-zone.log"
                records[side] = path.read_text(encoding="utf-8").splitlines() if path.exists() else []
                zones.append(f"## {side}: test262/{folder}/host-zone.log")
                zones += [l for l in records[side] if l.startswith("expected: ") or l.startswith("shards: ")]
            alike = ([l for l in records["base"] if l.startswith("expected: ")]
                     == [l for l in records["after"] if l.startswith("expected: ")])
            identical = records["base"] == records["after"]
            zones.append(f"# the two runs' expected zone lines read alike, line for line: {alike}")
            zones.append(f"# the two host-zone.log files are byte for byte the same: {identical}")
            self.write(f"{target}/zone-records.log", "\n".join(zones) + "\n")

        script = REPO / "scripts" / "compliance" / "compare_test262_paths.py"
        output = self.scratch / f"comparison-{self.args.part}.json"
        output.unlink(missing_ok=True)
        command = [sys.executable, "-I", str(script), "--base", str(merged["base"]), "--after", str(merged["after"]),
                   "--output", str(output)]
        for side, folder in folders.items():
            rerun = root / folder / "rerun-timeouts.json"
            if rerun.exists():
                command += [f"--{side}-rerun", str(rerun)]
        if classifications.exists():
            command += ["--classifications", str(classifications)]
        log = [f"# compare_test262_paths.py blob {self.git('hash-object', str(script))}; classification file "
               f"{classifications.relative_to(self.out) if classifications.exists() else '(none)'}\n"]
        self.run(command, REPO, log)
        self.write(f"{target}/comparison.log", "".join(log))
        if output.exists():
            self.write(f"{target}/comparison.json", output.read_text(encoding="utf-8"))
        if not self.args.revised:
            checks: list[str] = []
            self.run([sys.executable, "-I", str(script), "--base", str(merged["after"]), "--after", str(merged["after"])],
                     REPO, checks)
            mutated = loaded["after"]
            victim = next(r for r in sorted(mutated["results"], key=lambda r: (str(r["path"]), str(r.get("variant", ""))))
                          if r["status"] == "passed")
            victim["status"] = "failed"
            copy = self.scratch / "test262-negative-control.json"
            copy.write_text(json.dumps(mutated), encoding="utf-8")
            checks.append(f"# negative control: {victim['path']} [{victim.get('variant', 'original')}] turned from "
                          "passed to failed in a copy\n")
            self.run([sys.executable, "-I", str(script), "--base", str(merged["after"]), "--after", str(copy)], REPO, checks)
            copy.unlink()
            self.write(f"{target}/compare-checks.log", "".join(checks))
        for path in merged.values():
            path.unlink(missing_ok=True)

    # -- the class-L probe -----------------------------------------------------------------------

    def probe(self) -> None:
        self.need("part", "shell", "after-run")
        part = PARTS[self.args.part]
        shell = Path(self.args.shell).resolve()
        recorded = Path(self.args.after_run).resolve() / "shell-binaries.sha256"
        program = self.out / REVISED / "class-l-probe.js"
        env = dict(self.env)
        env.pop("TZ", None)
        if part["tz"]:
            env["TZ"] = part["tz"]
        env.pop(INVARIANT, None)
        log: list[str] = []
        result = self.run(["dotnet", str(shell), str(program)], self.scratch, log, env)
        ours = {p.name: sha256(p) for p in sorted(shell.parent.glob("*.dll"))}
        theirs = {}
        for line in recorded.read_text(encoding="utf-8").splitlines():
            digest, _, name = line.partition("  ")
            theirs[name.strip()] = digest
        same = ours == theirs
        differing = sorted(n for n in set(ours) | set(theirs) if ours.get(n) != theirs.get(n))
        zone = subprocess.run(["date", "-d", "2021-01-15T12:00:00Z", "+%Z %z"], env=env, capture_output=True,
                              text=True).stdout.strip()
        zone_summer = subprocess.run(["date", "-d", "2021-07-15T12:00:00Z", "+%Z %z"], env=env, capture_output=True,
                                     text=True).stdout.strip()
        lines = [
            f"# The class-L probe of decision-rule-revised-2026-11-09.md, part {self.args.part}, taken {utc()}",
            f"# program: {REVISED}/class-l-probe.js (sha256 {sha256(program)})",
            f"# zone: TZ {env.get('TZ', 'unset')}; 2021-01-15T12:00:00Z {zone}; 2021-07-15T12:00:00Z {zone_summer}; "
            f"no LANG, LC_ALL or LANGUAGE; {INVARIANT} unset",
            f"# shell: {shell}; BroilerJS.dll sha256 {sha256(shell)}",
            f"# the after run's record of the binaries it ran: {recorded} (sha256 {sha256(recorded)})",
            f"# every *.dll beside the shell has the digest that record gives it, and no other: {same}",
            f"# binaries that differ from the record: {', '.join(differing) or 'none'}",
            f"# command: dotnet <shell> <program>; exit {result.returncode}",
            "",
            result.stdout.rstrip("\n"),
        ]
        if result.stderr.strip():
            lines += ["", "# stderr", result.stderr.rstrip("\n")]
        self.write(f"{REVISED}/class-l-probe.{self.args.part}.txt", "\n".join(lines) + "\n")

    # -- part C ----------------------------------------------------------------------------------

    def tree_record(self, log: list[str]) -> None:
        commit = ""
        archive = Path(self.args.archive).resolve() if self.args.archive else None
        if archive is not None and archive.exists():
            commit = subprocess.run(["git", "get-tar-commit-id"], stdin=open(archive, "rb"), capture_output=True,
                                    text=True).stdout.strip()
        log.append(f"# shell tree: {self.tree}, written by git archive of {commit or '(the archive was not given)'}"
                   f"{f' ({archive.name}, sha256 {sha256(archive)})' if archive is not None and archive.exists() else ''}\n")
        dll = self.tree / SHELL_DLL
        log.append(f"# shell: {SHELL_DLL}, sha256 {sha256(dll) if dll.exists() else '(not built)'}\n")

    def harness(self, side: str, invariant: bool, out: str) -> None:
        log: list[str] = []
        self.tree_record(log)
        env = dict(self.env, TZ="UTC")
        command = [sys.executable, "-I", str(REPO / "scripts/conformance/run_reference_datasets.py"), "run",
                   "--bundle", "sjl-9-001", "--tree", str(self.tree), "--out", str(self.out / out), "--label", side]
        if invariant:
            command.append("--invariant-globalization")
        self.run(command, REPO, log, env)
        self.write(f"{out}/harness.log", "".join(log))

    def datasets(self) -> None:
        self.need("side", "tree")
        self.harness(self.args.side, False, f"datasets/{self.args.side}")
        if self.args.side == "after" and (self.out / "datasets/base/summary.json").exists():
            log: list[str] = []
            self.run([sys.executable, "-I", str(REPO / "scripts/conformance/run_reference_datasets.py"), "compare",
                      "--base", str(self.out / "datasets/base"), "--after", str(self.out / "datasets/after"),
                      "--out", str(self.out / "datasets/after/compare-with-base")], REPO, log)
            self.write("datasets/after/compare-with-base/compare.log", "".join(log))

    # -- part D ----------------------------------------------------------------------------------

    def invariant(self) -> None:
        self.need("side", "tree", "suite-root", "terser-module")
        side = self.args.side
        self.harness(side, True, f"invariant/{side}/datasets")
        if side == "after" and (self.out / "invariant/base/datasets/summary.json").exists():
            log: list[str] = []
            self.run([sys.executable, "-I", str(REPO / "scripts/conformance/run_reference_datasets.py"), "compare",
                      "--base", str(self.out / "invariant/base/datasets"), "--after",
                      str(self.out / "invariant/after/datasets"),
                      "--out", str(self.out / "invariant/after/datasets/compare-with-base")], REPO, log)
            self.write("invariant/after/datasets/compare-with-base/compare.log", "".join(log))
        suite = Path(self.args.suite_root).resolve()
        log = []
        self.tree_record(log)
        log.append(f"# suite: <suite> at {self.git('rev-parse', 'HEAD', tree=suite)}; "
                   f"clean: {self.git('status', '--porcelain', tree=suite) == ''}\n")
        env = dict(self.env, TZ="UTC")
        env[INVARIANT] = "1"
        output = self.scratch / f"default-locale-{side}.json"
        output.unlink(missing_ok=True)
        terser = Path(self.args.terser_module).resolve()
        version = json.loads((terser / "package.json").read_text(encoding="utf-8")).get("version")
        log.append(f"# terser: {version}, installed by npm ci from .github/actions/run-test262-shard/terser/"
                   "package-lock.json\n")
        self.run([sys.executable, "-I", str(self.tree / "scripts/compliance/run_test262.py"), "--suite-root", str(suite),
                  "--broiler-dll", str(self.tree / SHELL_DLL), "--include-negative", "--test-timeout-seconds", "30",
                  "--minifier", "terser", "--terser-module", str(terser), "--memory-limit-mb", "0",
                  "--output", str(output), "--no-summary-stdout", DEFAULT_LOCALE_TEST], self.tree, log, env)
        if output.exists():
            report = json.loads(output.read_text(encoding="utf-8"))
            for row in report.get("results", []):
                log.append(f"# row: {row['path']} [{row.get('variant', 'original')}] {row['status']}"
                           f"{(': ' + row['stderr'].strip().splitlines()[0]) if row.get('stderr') else ''}\n")
            self.write(f"invariant/{side}/default-locale-is-supported.json", json.dumps(report, indent=1, sort_keys=True) + "\n")
            output.unlink()
        self.write(f"invariant/{side}/default-locale-is-supported.log", "".join(log))

    # -- the reference ---------------------------------------------------------------------------

    def reference(self) -> None:
        self.need("node-tarball")
        tarball = Path(self.args.node_tarball).resolve()
        text = (HERE / "reference.txt").read_text(encoding="utf-8")
        pinned = re.search(r"^tarball-sha256 ([0-9a-f]{64})$", text, re.M).group(1)
        digest = sha256(tarball)
        log = [f"# the reference reference.txt names: {re.search(r'^release (.+)$', text, re.M).group(1)}; tarball "
               f"{tarball.name}, {tarball.stat().st_size:,} bytes, sha256 {digest}; reference.txt's: {pinned}; "
               f"equal: {digest == pinned}\n"]
        if digest != pinned:
            self.write("datasets/reference/reference.log", "".join(log))
            raise SystemExit("the tarball's digest is not reference.txt's")
        unpacked = self.scratch / "node"
        shutil.rmtree(unpacked, ignore_errors=True)
        unpacked.mkdir()
        with tarfile.open(tarball) as archive:
            archive.extractall(unpacked, filter="data")
        node = next(unpacked.glob("node-*/bin/node"))
        versions = subprocess.run([str(node), "-p", "JSON.stringify(process.versions)"], capture_output=True, text=True)
        log.append(f"# process.versions: {versions.stdout.strip()}\n")
        temporal = subprocess.run([str(node), "-p", "typeof Temporal"], capture_output=True, text=True).stdout.strip()
        log.append(f"# typeof Temporal in this Node, with no flag: {temporal}\n")
        env = dict(self.env, TZ="UTC")
        sources = {"timezones": REPO / "conformance/datasets/time"}
        for program in ("dates", "displaynames", "durations", "lists", "locales", "numbers", "plurals",
                        "relativetimes", "segments", "temporal-intl", "calendar-formats", "zone-names", "timezones"):
            folder = sources.get(program, REPO / "conformance/datasets/intl")
            source = folder / f"{program}.js"
            script = ("const fs=require('fs');process.stdout.write((0,eval)(fs.readFileSync(process.argv[1],'utf8'))"
                      "+'\\n')")
            result = subprocess.run([str(node), "-e", script, str(source)], env=env, capture_output=True)
            answer = self.out / "datasets/reference" / f"{program}.answer.txt"
            answer.parent.mkdir(parents=True, exist_ok=True)
            answer.write_bytes(result.stdout)
            log.append(f"$ TZ=UTC node -e \"{script}\" {source.relative_to(REPO)}\n# exit {result.returncode}; "
                       f"{len(result.stdout):,} bytes, sha256 {hashlib.sha256(result.stdout).hexdigest()}; "
                       f"lines {result.stdout.decode('utf-8', 'replace').rstrip(chr(10)).count(chr(10)) + 1}\n")
            if result.stderr:
                log.append("# stderr\n" + result.stderr.decode("utf-8", "replace"))
        shutil.rmtree(unpacked, ignore_errors=True)
        self.write("datasets/reference/reference.log", "".join(log))

    # -- manifest --------------------------------------------------------------------------------

    def manifest(self) -> None:
        files = {}
        for path in sorted(self.out.rglob("*")):
            if path.is_file() and path.name != "manifest.json" and "__pycache__" not in path.parts:
                files[str(path.relative_to(self.out)).replace(os.sep, "/")] = sha256(path)
        manifest = {"bundle": "sjl-9-001", "repository": "Broiler.JS", "baseCommit": BASE_COMMIT,
                    "afterCommit": AFTER_COMMIT, "ruleCommit": self.git("rev-parse", RULE_COMMIT),
                    "suiteRevision": "ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e", "files": files}
        (self.out / "manifest.json").write_text(json.dumps(manifest, indent=1, sort_keys=True) + "\n",
                                                encoding="utf-8", newline="\n")


STEPS = ("identity", "test262", "compare", "probe", "datasets", "invariant", "reference", "manifest")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--out", required=True)
    parser.add_argument("--only", nargs="+", choices=STEPS, required=True)
    parser.add_argument("--scratch", default=os.path.join(os.environ.get("TMPDIR", "/tmp"), "sjl-9-001-js"))
    parser.add_argument("--part", choices=sorted(PARTS))
    parser.add_argument("--side", choices=("base", "after"))
    parser.add_argument("--revised", action="store_true", help="compare: over the revision's classification file")
    parser.add_argument("--artifact-zip", help="test262: the run's test262-evidence artifact, as downloaded")
    parser.add_argument("--run-url", help="test262: the workflow run, with its attempt")
    parser.add_argument("--artifact-id", help="test262: the artifact's id")
    parser.add_argument("--run-times", help="test262: when the run was dispatched and completed")
    parser.add_argument("--shell", help="probe: the BroilerJS.dll to probe")
    parser.add_argument("--after-run", help="probe: the part's imported after run (its folder)")
    parser.add_argument("--tree", help="datasets, invariant: the scratch tree git archive wrote, its shell built")
    parser.add_argument("--archive", help="datasets, invariant: the archive that tree was written from")
    parser.add_argument("--suite-root", help="invariant: a checkout of test262 at ccaac100")
    parser.add_argument("--terser-module", help="invariant: the installed terser package directory")
    parser.add_argument("--node-tarball", help="reference: the reference's tarball, as downloaded")
    args = parser.parse_args(argv)
    collector = Collector(args)
    for step in args.only:
        print(f"== {step} {utc()}", flush=True)
        getattr(collector, step)()
    return 0


if __name__ == "__main__":
    sys.exit(main())
