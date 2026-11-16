#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Collect Broiler.JS's evidence for milestone SJL-17 (work package SJL-17.6) into bundle sjl-17-001.

    python3 -I docs/evidence/sjl-17-001/collect.py --out docs/evidence/sjl-17-001 --only STEP [STEP ...] [options]

THE RULE IS decision-rule.md BESIDE THIS FILE, committed at SJL-17.1. This collector changes no file it names. It is
bundle sjl-9-001's collect.py, cut down to this rule's three parts.

PART A is the two hosted runs of .github/workflows/test262-evidence.yml, the base at the after commit's first parent
and the after run at the after commit, in the runner's own zone, UTC, with negative-metadata tests. The `test262` step
imports one run's artifact test262-evidence, unchanged, with the digest of the zip as downloaded; the `compare` step
reads the two runs' completeness and host-zone records and compares them through
scripts/compliance/compare_test262_paths.py as committed at SJL-0.9, with the classification file retained beside its
report (test262/comparison-utc/), and checks the script with the after run against itself and a negative control.

PART B runs the harness the rule names, scripts/conformance/run_reference_datasets.py --bundle sjl-17-001, of the
scratch tree that `git archive` wrote from the commit (--tree), over that tree's shell built in Release, with TZ=UTC and
no LANG (the harness sets the child's environment). The `reference` step runs the orderings program once in the
reference that bundle sjl-9-001's reference.txt names, its tarball checked against the digest that file records.

PART C runs invariant-probe.js in the same shell with DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1, TZ=UTC and no LANG,
LANGUAGE or LC_ variable, and compares its output line by line with invariant-probe.expected.txt. The environment the
shell is given is written into the log.

THE COLLECTOR JUDGES NOTHING. Each step writes what it ran and what it printed, failures included; the README reads
them under the rule.

Steps, each with `--only`:

  identity   the base and after commits, the rule's conditions on the base, the files the rule fixes, by blob at the
             rule commit's candidates, the base and the after commit, into identity.txt;
  test262    with --side, one hosted run imported into test262/<base|after>/, with imported.txt; it writes the folder
             once;
  compare    the two runs' completeness and host-zone records and the comparison, with
             test262/comparison-utc/classifications.json, and the script's checks; and the rows of clause 4's
             directories and of getCollations/output-array-values.js, with their verdicts at both commits;
  datasets   with --side and --tree, part B's run into datasets/<side>/ (and, for after, the join with the base);
  invariant  with --side and --tree, part C's run into invariant/<side>/;
  reference  with --node-tarball, the orderings program once in the reference, into datasets/reference/;
  hashes     hashes.txt: the SHA-256 of every file the bundle retains but README.md and hashes.txt, for sha256sum -c.

Paths in retained logs are written as <repo>, <tree> and <scratch>.
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
BUNDLE = "docs/evidence/sjl-17-001"
RULE = f"{BUNDLE}/decision-rule.md"
# What the rule fixes with itself: it, the comparison script, the workflow part A dispatches, and the scripts the
# workflow runs and merges with; part B's harness, its driver, the copies and the divergence groups; part C's probe and
# its expected answers.
RULE_FILES = (RULE, "scripts/compliance/compare_test262_paths.py", ".github/workflows/test262-evidence.yml",
              ".github/actions/run-test262-shard/action.yml", "scripts/compliance/run_test262.py",
              "scripts/compliance/merge_test262_shards.py")
DATASET_FILES = ("scripts/conformance/run_reference_datasets.py", "scripts/conformance/reference_dataset_driver.js",
                 "conformance/datasets/intl/orderings.js", "conformance/datasets/intl/orderings.icu-77.1.txt",
                 "conformance/datasets/intl/datasets.pin", f"{BUNDLE}/divergence-groups.tsv",
                 f"{BUNDLE}/invariant-probe.js", f"{BUNDLE}/invariant-probe.expected.txt",
                 "docs/evidence/sjl-9-001/reference.txt")
# The commits the rule names before they exist: the after commit is the default-branch merge that lands SJL-17.5's
# cut-over (Broiler-Platform/Broiler.JS#1118), and the base is its first parent.
AFTER_COMMIT = "2f012739cfc4d99e91b22d28a09941c49d8bd646"
BASE_COMMIT = "5669f2bd29899ac93e0d7af130b6a7adc5791d6b"
# The rule commit, as the README's reading 1 takes it, the other candidate, and the merge that brought both to the
# default branch (Broiler-Platform/Broiler.JS#1087).
RULE_COMMIT = "096793ae0f3aaef7f18829c4b5efd923f3dbef94"
RULE_COMMIT_OTHER = "59380ef98e6a4b91655f04dc832786aaea3c2aaa"
RULE_MERGE = "d1e8f5beec824438f0fc42c37bd941ad7421ce52"
# Bundle sjl-9-001's after commit, which the base descends from.
SJL9_AFTER = "a5f2692e88a9aafeaa850bebe9c3399f6263353b"
# The rule's product paths: a first-parent successor of the base that changes one is SJL-17.5's.
PRODUCT_PATHSPECS = (":(glob)Broiler.JS/Broiler.JavaScript.*/**", "Broiler.ECMAScript", ":(exclude)Broiler.ECMAScript/docs",
                     "Directory.Packages.props")
SHELL_DLL = "Broiler.JS/Broiler.JavaScript/bin/Release/net10.0/BroilerJS.dll"
INVARIANT = "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"
PROBE = f"{BUNDLE}/invariant-probe.js"
EXPECTED = f"{BUNDLE}/invariant-probe.expected.txt"
# Clause 4's directories (test262 `intl402/Collator` and `localeCompare` per path) and the file part A names.
COLLATION_PATHS = ("test/intl402/Collator/", "test/intl402/String/prototype/localeCompare/",
                   "test/built-ins/String/prototype/localeCompare/")
GET_COLLATIONS = "test/intl402/Locale/prototype/getCollations/output-array-values.js"


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
        for name in [n for n in self.env if n in ("LANG", "LANGUAGE", "TZ", INVARIANT) or n.startswith("LC_")]:
            self.env.pop(name, None)

    # -- helpers ---------------------------------------------------------------------------------

    def hide(self, text: str) -> str:
        names = [(self.scratch, "<scratch>"), (REPO, "<repo>")]
        if self.tree is not None:
            names.append((self.tree, "<tree>"))
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
        successors = self.git("rev-list", "--first-parent", f"{BASE_COMMIT}..{AFTER_COMMIT}").split()
        changing = {c: self.git("diff", "--name-only", f"{c}^1", c, "--", *PRODUCT_PATHSPECS).splitlines()
                    for c in successors}
        rule_changes = self.git("rev-list", f"{RULE_COMMIT}..{AFTER_COMMIT}", "--", RULE).split()
        lines = [
            "bundle: sjl-17-001 (Broiler.JS), its parts A to C",
            f"collected: {utc()}, by {BUNDLE}/collect.py at {self.git('rev-parse', 'HEAD')}",
            f"after commit: {AFTER_COMMIT} ({self.git('log', '-1', '--format=%s', AFTER_COMMIT)})",
            f"after commit's parents: {' '.join(parents[1:])}",
            f"base commit: {BASE_COMMIT}, the after commit's first parent: {parents[1:2] == [BASE_COMMIT]}",
            f"rule commit (reading 1): {RULE_COMMIT} ({self.git('log', '-1', '--format=%s', RULE_COMMIT)})",
            f"the other candidate: {RULE_COMMIT_OTHER} ({self.git('log', '-1', '--format=%s', RULE_COMMIT_OTHER)})",
            f"their default-branch merge: {RULE_MERGE} ({self.git('log', '-1', '--format=%s', RULE_MERGE)})",
            f"condition 1, the base descends from the rule commit: {self.is_ancestor(RULE_COMMIT, BASE_COMMIT)}; "
            f"from the other candidate: {self.is_ancestor(RULE_COMMIT_OTHER, BASE_COMMIT)}; "
            f"from their merge: {self.is_ancestor(RULE_MERGE, BASE_COMMIT)}",
            f"commits after the rule commit, up to the after commit, that change the rule file: {' '.join(rule_changes) or 'none'}",
            f"condition 2, the base descends from bundle sjl-9-001's after commit {SJL9_AFTER}: "
            f"{self.is_ancestor(SJL9_AFTER, BASE_COMMIT)}; the base is that commit: {BASE_COMMIT == SJL9_AFTER}",
            f"condition 3 (reading 4), first-parent successors of the base up to the after commit: {' '.join(successors)}",
        ]
        for commit, paths in changing.items():
            lines.append(f"  {commit} changes {len(paths)} product paths the rule names (Broiler.JS/Broiler.JavaScript.*/, "
                         "Broiler.ECMAScript/ outside docs/, Directory.Packages.props):")
            lines += [f"    {p}" for p in paths]
        lines += ["", "# the files the rule fixes, by blob at the rule commit, the base and the after commit"]
        for path in RULE_FILES + DATASET_FILES:
            blobs = [self.git("rev-parse", "--verify", "-q", f"{c}:{path}") or "(absent)" for c in (RULE_COMMIT, BASE_COMMIT, AFTER_COMMIT)]
            lines.append(f"{path}: rule {blobs[0][:12]}, base {blobs[1][:12]}, after {blobs[2][:12]}; "
                         f"base and after the same: {blobs[1] == blobs[2]}")
        lines += ["", "# the rule, by blob at each candidate and in this checkout"]
        for commit in (RULE_COMMIT_OTHER, RULE_COMMIT, RULE_MERGE, BASE_COMMIT, AFTER_COMMIT):
            lines.append(f"{RULE} at {commit[:12]}: {self.git('rev-parse', f'{commit}:{RULE}')}")
        lines.append(f"{RULE} here: {self.git('hash-object', str(REPO / RULE))}")
        self.write("identity.txt", "\n".join(lines) + "\n")

    # -- part A ----------------------------------------------------------------------------------

    def test262(self) -> None:
        self.need("side", "artifact-zip", "run-url", "artifact-id", "run-times")
        side = self.args.side
        folder = self.out / "test262" / side
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
        role = ("the after commit, the merge that lands SJL-17.5's cut-over (Broiler-Platform/Broiler.JS#1118)"
                if side == "after" else "the base commit, the after commit's first parent")
        (folder / "imported.txt").write_text("\n".join([
            "# imported: the run was taken by .github/workflows/test262-evidence.yml, not locally",
            f"workflow run: {self.args.run_url}, {self.args.run_times}",
            f"tested commit: {fields.get('tested commit', '(run.txt names none)')} ({role}); the commit the rule "
            f"names: {fields.get('tested commit') == expected}",
            "configuration: part A's, in the runner's own zone (time_zone empty: TZ unset, UTC), with negative-metadata "
            f"tests (include_negative true); includeNegative {fields.get('includeNegative', '(not in run.txt)')} in run.txt",
            f"runner: {fields.get('runner', '(not in run.txt)')}; dotnetVersion {fields.get('dotnetVersion', '?')}",
            f"runner image (the run's job log, as the workflow run reports it): {self.args.runner_image or '(not given)'}",
            f"artifact: test262-evidence, id {self.args.artifact_id}, {zip_path.stat().st_size:,} bytes, sha256 "
            f"{sha256(zip_path)} (the zip as downloaded)",
            f"retained: every file of the artifact, unchanged, in this directory ({len(names)} entries); this file is "
            "the only one added",
        ]) + "\n", encoding="utf-8", newline="\n")

    @staticmethod
    def completeness(folder: Path, merged: dict) -> list[str]:
        """The rule's completeness conditions for one run, as its own files state them."""
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
        root = self.out / "test262"
        target = "test262/comparison-utc"
        classifications = root / "comparison-utc" / "classifications.json"
        merged, loaded = {}, {}
        for side in ("base", "after"):
            merged[side] = self.scratch / f"{side}-test262-results.json"
            with gzip.open(root / side / "test262-results.json.gz", "rt", encoding="utf-8") as handle:
                text = handle.read()
            merged[side].write_text(text, encoding="utf-8")
            loaded[side] = json.loads(text)
        lines = ["# Part A's completeness conditions for each run, read from the files the workflow wrote:",
                 "# every expected shard reported, no configuration failure, one suite revision, one shell, every",
                 "# shard in the zone, includeNegative true.", ""]
        for side in ("base", "after"):
            lines += [f"## {side}: test262/{side}/"] + self.completeness(root / side, loaded[side]) + [""]
        self.write(f"{target}/completeness.log", "\n".join(lines))
        zones, records = [], {}
        for side in ("base", "after"):
            path = root / side / "host-zone.log"
            records[side] = path.read_text(encoding="utf-8").splitlines() if path.exists() else []
            zones.append(f"## {side}: test262/{side}/host-zone.log")
            zones += [l for l in records[side] if l.startswith("expected: ") or l.startswith("shards: ")]
        alike = ([l for l in records["base"] if l.startswith("expected: ")]
                 == [l for l in records["after"] if l.startswith("expected: ")])
        zones.append(f"# the two runs' expected zone lines read alike, line for line: {alike}")
        zones.append(f"# the two host-zone.log files are byte for byte the same: {records['base'] == records['after']}")
        self.write(f"{target}/zone-records.log", "\n".join(zones) + "\n")

        script = REPO / "scripts" / "compliance" / "compare_test262_paths.py"
        output = self.scratch / "comparison-A.json"
        output.unlink(missing_ok=True)
        command = [sys.executable, "-I", str(script), "--base", str(merged["base"]), "--after", str(merged["after"]),
                   "--output", str(output)]
        for side in ("base", "after"):
            rerun = root / side / "rerun-timeouts.json"
            if rerun.exists():
                command += [f"--{side}-rerun", str(rerun)]
        if classifications.exists():
            command += ["--classifications", str(classifications)]
        log = [f"# compare_test262_paths.py blob {self.git('hash-object', str(script))}; at the base "
               f"{self.git('rev-parse', f'{BASE_COMMIT}:scripts/compliance/compare_test262_paths.py')}, at the after commit "
               f"{self.git('rev-parse', f'{AFTER_COMMIT}:scripts/compliance/compare_test262_paths.py')}; classification file "
               f"{classifications.relative_to(self.out) if classifications.exists() else '(none)'}\n"]
        self.run(command, REPO, log)
        self.write(f"{target}/comparison.log", "".join(log))
        if output.exists():
            self.write(f"{target}/comparison.json", output.read_text(encoding="utf-8"))
        # Clause 4's directories and the file part A names, row by row: each row's verdict at the base and after.
        verdicts: dict[tuple[str, str], dict[str, str]] = {}
        for side in ("base", "after"):
            for row in loaded[side]["results"]:
                path = str(row["path"])
                if path.startswith(COLLATION_PATHS) or path == GET_COLLATIONS:
                    verdicts.setdefault((path, str(row.get("variant", "original"))), {})[side] = row["status"]
        rows = [f"{p}\t{v}\t{s.get('base', '(absent)')}\t{s.get('after', '(absent)')}\t"
                f"{'same' if s.get('base') == s.get('after') else 'changed'}" for (p, v), s in sorted(verdicts.items())]
        self.write(f"{target}/collation-rows.tsv",
                   "# the rows of test/intl402/Collator/, both localeCompare directories and getCollations/output-array-"
                   "values.js, from each run's merged results\npath\tvariant\tbase\tafter\tverdict\n" + "\n".join(rows) + "\n")
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
        output.unlink(missing_ok=True)

    # -- parts B and C ---------------------------------------------------------------------------

    def tree_record(self, log: list[str]) -> None:
        commit = self.args.archive_commit or "(not given)"
        log.append(f"# shell tree: <tree>, written by git archive of {commit}"
                   f"{f'; {self.args.archive_note}' if self.args.archive_note else ''}\n")
        dll = self.tree / SHELL_DLL
        log.append(f"# shell: {SHELL_DLL}, sha256 {sha256(dll) if dll.exists() else '(not built)'}\n")

    def datasets(self) -> None:
        self.need("side", "tree")
        side = self.args.side
        log: list[str] = []
        self.tree_record(log)
        harness = self.tree / "scripts/conformance/run_reference_datasets.py"
        self.run([sys.executable, "-I", str(harness), "verify"], self.tree, log)
        self.run([sys.executable, "-I", str(harness), "run", "--bundle", "sjl-17-001", "--tree", str(self.tree),
                  "--out", str(self.out / "datasets" / side), "--label", side], self.tree, log, dict(self.env, TZ="UTC"))
        self.write(f"datasets/{side}/harness.log", "".join(log))
        if side == "after" and (self.out / "datasets/base/summary.json").exists():
            log = []
            self.run([sys.executable, "-I", str(harness), "compare", "--base", str(self.out / "datasets/base"),
                      "--after", str(self.out / "datasets/after"),
                      "--out", str(self.out / "datasets/after/compare-with-base")], self.tree, log)
            self.write("datasets/after/compare-with-base/compare.log", "".join(log))

    def invariant(self) -> None:
        self.need("side", "tree")
        side = self.args.side
        dll = self.tree / SHELL_DLL
        probe = self.tree / PROBE
        expected_path = self.tree / EXPECTED
        env = dict(self.env, TZ="UTC")
        env[INVARIANT] = "1"
        log: list[str] = []
        self.tree_record(log)
        shown = {k: env[k] for k in sorted(env) if k in ("TZ", INVARIANT) or k.startswith(("LANG", "LC_", "DOTNET_"))}
        log.append(f"# the shell's environment: this collector's, less LANG, LANGUAGE, TZ, {INVARIANT} and every LC_ "
                   f"variable, with {json.dumps(shown, sort_keys=True)}\n")
        log.append(f"# probe: {PROBE}, sha256 {sha256(probe)}; expected: {EXPECTED}, sha256 {sha256(expected_path)}\n")
        started = utc()
        result = subprocess.run(["dotnet", str(dll), "--script-host", str(probe)], cwd=self.tree, env=env,
                                capture_output=True)
        log.append(f"$ dotnet {SHELL_DLL} --script-host {PROBE}\n# started {started}; exit {result.returncode}; "
                   f"stdout {len(result.stdout):,} bytes; stderr {len(result.stderr):,} bytes\n")
        folder = self.out / "invariant" / side
        folder.mkdir(parents=True, exist_ok=True)
        (folder / "invariant-probe.out.txt").write_bytes(result.stdout)
        (folder / "invariant-probe.err.txt").write_bytes(result.stderr)
        expected = expected_path.read_bytes().decode("utf-8").splitlines()
        actual = result.stdout.decode("utf-8", errors="replace").splitlines()
        rows = []
        for number in range(1, max(len(expected), len(actual)) + 1):
            e = expected[number - 1] if number <= len(expected) else None
            a = actual[number - 1] if number <= len(actual) else None
            rows.append((number, e is not None and e == a, e, a))
        aborts = [n for n, _, _, a in rows if a is not None and " | abort: " in a]
        equal = sum(1 for _, same, _, _ in rows if same)
        report = [
            f"# part C at {side}: the probe's output against {EXPECTED}, line by line",
            f"shell exit: {result.returncode}",
            f"expected lines: {len(expected)}; printed lines: {len(actual)}",
            f"abort lines: {len(aborts)}{(' (' + ', '.join(map(str, aborts)) + ')') if aborts else ''}",
            f"lines equal to the expected answer: {equal}; differing or missing: {len(rows) - equal}",
            "",
            "# line\tequal\texpected\tprinted",
        ]
        report += [f"{n}\t{'yes' if same else 'no'}\t{e if e is not None else '(none)'}\t{a if a is not None else '(none)'}"
                   for n, same, e, a in rows]
        self.write(f"invariant/{side}/compare.txt", "\n".join(report) + "\n")
        self.write(f"invariant/{side}/probe.log", "".join(log))

    # -- the reference ---------------------------------------------------------------------------

    def reference(self) -> None:
        self.need("node-tarball")
        tarball = Path(self.args.node_tarball).resolve()
        text = (REPO / "docs/evidence/sjl-9-001/reference.txt").read_text(encoding="utf-8")
        pinned = re.search(r"^tarball-sha256 ([0-9a-f]{64})$", text, re.M).group(1)
        digest = sha256(tarball)
        log = [f"# the reference bundle sjl-9-001's reference.txt names: "
               f"{re.search(r'^release (.+)$', text, re.M).group(1)}; tarball {tarball.name}, "
               f"{tarball.stat().st_size:,} bytes, sha256 {digest}; reference.txt's: {pinned}; equal: {digest == pinned}\n",
               f"# taken {utc()}, after the after commit {AFTER_COMMIT}\n"]
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
        env = dict(self.env, TZ="UTC")
        source = REPO / "conformance/datasets/intl/orderings.js"
        script = ("const fs=require('fs');process.stdout.write((0,eval)(fs.readFileSync(process.argv[1],'utf8'))"
                  "+'\\n')")
        result = subprocess.run([str(node), "-e", script, str(source)], env=env, capture_output=True)
        answer = self.out / "datasets/reference/orderings.answer.txt"
        answer.parent.mkdir(parents=True, exist_ok=True)
        answer.write_bytes(result.stdout)
        log.append(f"$ TZ=UTC node -e \"{script}\" {source.relative_to(REPO)}\n# no LANG, LANGUAGE or LC_ variable; "
                   f"exit {result.returncode}; {len(result.stdout):,} bytes, sha256 "
                   f"{hashlib.sha256(result.stdout).hexdigest()}; lines "
                   f"{result.stdout.decode('utf-8', 'replace').rstrip(chr(10)).count(chr(10)) + 1}\n")
        if result.stderr:
            log.append("# stderr\n" + result.stderr.decode("utf-8", "replace"))
        shutil.rmtree(unpacked, ignore_errors=True)
        self.write("datasets/reference/reference.log", "".join(log))

    # -- hashes ----------------------------------------------------------------------------------

    def hashes(self) -> None:
        lines = []
        for path in sorted(self.out.rglob("*")):
            relative = str(path.relative_to(self.out)).replace(os.sep, "/")
            if path.is_file() and relative not in ("README.md", "hashes.txt") and "__pycache__" not in path.parts:
                lines.append(f"{sha256(path)}  {relative}")
        (self.out / "hashes.txt").write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")


STEPS = ("identity", "test262", "compare", "datasets", "invariant", "reference", "hashes")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--out", required=True)
    parser.add_argument("--only", nargs="+", choices=STEPS, required=True)
    parser.add_argument("--scratch", default=os.path.join(os.environ.get("TMPDIR", "/tmp"), "sjl-17-001-js"))
    parser.add_argument("--side", choices=("base", "after"))
    parser.add_argument("--artifact-zip", help="test262: the run's test262-evidence artifact, as downloaded")
    parser.add_argument("--run-url", help="test262: the workflow run, with its attempt")
    parser.add_argument("--artifact-id", help="test262: the artifact's id")
    parser.add_argument("--run-times", help="test262: when the run was dispatched and completed")
    parser.add_argument("--runner-image", help="test262: the runner image the run's job log names")
    parser.add_argument("--tree", help="datasets, invariant: the scratch tree git archive wrote, its shell built")
    parser.add_argument("--archive-commit", help="datasets, invariant: the commit the archive was written from")
    parser.add_argument("--archive-note", help="datasets, invariant: how the archive and the shell were made")
    parser.add_argument("--node-tarball", help="reference: the reference's tarball, as downloaded")
    args = parser.parse_args(argv)
    collector = Collector(args)
    for step in args.only:
        print(f"== {step} {utc()}", flush=True)
        getattr(collector, step)()
    return 0


if __name__ == "__main__":
    sys.exit(main())
