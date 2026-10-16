#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Collect Broiler.JS's evidence for milestone SJL-6 (work package SJL-6.6) into bundle sjl-6-001.

    python3 docs/evidence/sjl-6-001/collect.py --tree <a checkout of the after commit> \
        [--part A|B --artifact-zip <that part's after run's test262-evidence artifact> \
         --run-url <its workflow run> --artifact-id <its id> --run-times <when it was dispatched and completed>] \
        --out docs/evidence/sjl-6-001 --only STEP [STEP ...]

THE RULE IS decision-rule.md BESIDE THIS FILE, committed at SJL-6.1 before any code of SJL-6. Its two parts
take the whole pinned suite through .github/workflows/test262-evidence.yml: part A in the runner's own zone,
UTC, and part B with time_zone America/New_York. Both bases were taken at the commit that carries the rule
and imported by hand into test262/base/ and test262/base-new-york/, each with its imported.txt; this
collector imports the after runs the same way, into test262/after/ and test262/after-new-york/, and compares
each part's after run with its own base through scripts/compliance/compare_test262_paths.py as the rule fixes
it. The two parts' runs are never compared with each other.

THE AFTER ARM IS A CHECKOUT, NOT THIS TREE. Every local build and run is taken at `--tree`, a clean checkout of
a commit whose product sources (Broiler.JS/ and Broiler.ECMAScript/) are those of the commit that lands
SJL-6.5's cut-over; the identity step lists every file there that differs.

IT REUSES BUNDLE SJL-5-001'S COLLECTOR, and through it sjl-4-001's, sjl-3-001's and sjl-0-001's, for the home's
checks, the pristine consumer and the import, at this milestone's version of the shared set. What it adds is
SJL-6's: the two parts, the engine-level vectors in both zones, the Date area's deletion list and its
duplicate search.

THE COLLECTOR JUDGES NOTHING. Each step writes what it ran and what it printed, failures included; the README
reads them under the rule.

Steps, each with `--only`:

  identity        the after commit and its cleanliness, the cut-over commit and the product files that differ
                  from it, the rule commit, its ancestry and the files it fixes unchanged since, the SDK, Node
                  and the machine, into identity.txt;
  home            the shared library at the after commit, as sjl-3-001's step takes it: the shared filter's
                  tests, the corpus generators' tests, the Native AOT probe over the corpus against the JIT, and
                  the shared set packed at 0.1.0-preview.4 with package validation and restored and run by a
                  pristine consumer, into home/;
  vectors         the engine-level vectors, once under TZ=UTC and once under TZ=America/New_York, into
                  engine/after-revision-<n>-utc.* and engine/after-revision-<n>-new-york.*;
  test262         with --part, that part's after run imported into test262/after/ (A) or test262/after-new-york/
                  (B), the two runs' host-zone records set side by side, the comparison with the part's base,
                  and the script's checks, into test262/comparison-utc/ or test262/comparison-new-york/, which
                  holds the part's classification file when it exists;
  consumer        the engine set packed and restored by a pristine consumer, into engine/;
  simplification  the Date area's Broiler.JS deletion list (Broiler.VM docs/javascript-shared-libraries/
                  milestones/sjl-6-date.md, SJL-6.5, as JSD-0072's X6 extends it), each entry searched in the
                  file or folder that held it, and the search for a surviving duplicate of the area in the
                  engine's product sources, into simplification/;
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
import re
import shutil
import subprocess
import sys
import zipfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
TEXT_BUNDLE = HERE.parent / "sjl-5-001"

_spec = importlib.util.spec_from_file_location("sjl_5_001_collect", TEXT_BUNDLE / "collect.py")
text = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(text)
prior = text.prior
base = text.base

RULE = "docs/evidence/sjl-6-001/decision-rule.md"
# What the rule fixes with itself: it, the comparison script and the workflow both parts dispatch.
RULE_FILES = (RULE, "scripts/compliance/compare_test262_paths.py", ".github/workflows/test262-evidence.yml")
# The rule's base: the commit that carries the rule, run before any code of SJL-6 landed, in both zones.
BASE_COMMIT = "5fef1165f001c226b28f9bf0ae32c6dbe56abcc8"
# The commit that lands SJL-6.5's cut-over, whose product sources the after arm must carry.
CUT_OVER = "31c04809337e9b60e0480782a068438415cf8e07"
SHARED_VERSION = "0.1.0-preview.4"
VECTORS_TESTS = "Broiler.JS/Broiler.JavaScript.Vectors.Tests"
STEPS = ("identity", "home", "vectors", "test262", "consumer", "simplification", "manifest")
# Each part: its base folder, its after folder and its comparison folder under test262/, and its zone.
PARTS = {
    "A": ("base", "after", "comparison-utc", "the runner's own zone (time_zone empty: TZ unset, UTC)"),
    "B": ("base-new-york", "after-new-york", "comparison-new-york", "time_zone America/New_York"),
}
ZONES = (("utc", "UTC"), ("new-york", "America/New_York"))

# The area's Broiler.JS deletion list (Broiler.VM docs/javascript-shared-libraries/milestones/sjl-6-date.md,
# SJL-6.5, as JSD-0072's X6 extends it per sub-area), entry by entry: what was deleted, the file or folder that
# held it, and what is searched there. An adapter or a call that keeps a deleted member's name shows its line.
JS = "Broiler.JS/Broiler.JavaScript.BuiltIns/"
DATE = JS + "Date"
TEMPORAL = JS + "Temporal"
DELETED = (
    ("JSDateMath's arithmetic, its unused DaysInYear and TimeFromYear included, and Intl's dozen reads of it",
     ["Broiler.JS"], r"\bJSDateMath\b"),
    ("the inline MakeFullYear copies, and the dead one", [DATE],
     r"\+ ?= ?1900|\+ 1900|< 100 \?|<= 99\b"),
    ("JSDate.ToDateTime, invalidDate and the unused IsValid(JSValue, ...)", [DATE],
     r"\bToDateTime\(|\binvalidDate\b|\bIsValid\("),
    ("JSDate.DaysInMonth", [DATE], r"\bDaysInMonth\b"),
    ("the Temporal civil copies: the leap year, the month length, DaysFromCivil and CivilFromDays, IsoDayOfWeek, "
     "IsoWeek, WeeksInIsoYear, IsValidISODate, the inline days of the year, and the FloorDiv beside each that "
     "serves only them; EpochDaysFor and DateFromEpochDays, which become calls", [TEMPORAL],
     r"\b(IsLeapYear|DaysInMonth|DaysFromCivil|CivilFromDays|IsoDayOfWeek|IsoWeek|WeeksInIsoYear|IsValidISODate|"
     r"FloorDiv|EpochDaysFor|DateFromEpochDays)\b|146097|719468"),
    ("Intl's DayOfWeek", [JS + "Intl"], r"\bDayOfWeek\b"),
    ("TryParseExtendedIso with EcmaIsoDateTime, and DateParser's lists of the format (DefaultFormats and the "
     "\\dT lists)", ["Broiler.JS"],
     r"\b(TryParseExtendedIso|EcmaIsoDateTime|DefaultFormats|StrictIsoTUtcFormats|StrictIsoTLocalFormats|"
     r"IsoTSeparator)\b"),
    ("the renderings' DateString, ClockString, HumanYearString, ToIsoString and ToTimeZoneString's text", [DATE],
     r"\b(DateString|ClockString|HumanYearString|ToIsoString|ToTimeZoneString)\b"),
)
# The search for a surviving duplicate of the Date area: the civil-day and time constants, the civil-day
# helpers by name, the month and weekday names a rendering spells, the platform's parse of a date, its
# conversions between local and universal time, and its zones.
DUPLICATE_PATTERN = (r'146097|719468|\b86400000\b|8\.64e15|8640000000000000|DaysFromCivil|CivilFromDays|IsLeapYear'
                     r'|DaysInMonth|"(Sun|Mon|Tue|Wed|Thu|Fri|Sat)"|"(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)"'
                     r'|\bDateTime(Offset)?\.(Parse|TryParse|ParseExact|TryParseExact|DaysInMonth|IsLeapYear)\b'
                     r'|\.ToUniversalTime\(|\.ToLocalTime\(|\bTimeZoneInfo\b')


class Collector(text.Collector):
    def __init__(self, args: argparse.Namespace) -> None:
        super().__init__(args)
        self.base_commit = BASE_COMMIT
        self.part = args.part

    # -- identity --------------------------------------------------------------------------------

    def identity(self) -> None:
        after = self.git(self.tree, "rev-parse", "HEAD")
        rule = self.git(self.tree, "log", "--diff-filter=A", "--format=%H", "--", RULE).splitlines()[-1]
        lines = [
            "bundle: sjl-6-001 (Broiler.JS), its after arm",
            f"after commit: {after}",
            f"after tree clean: {self.git(self.tree, 'status', '--porcelain') == ''}",
            f"cut-over commit: {CUT_OVER} (SJL-6.5)",
            "files under Broiler.JS/ and Broiler.ECMAScript/ that differ between the cut-over commit and the "
            "after commit:",
        ]
        changed = self.git(self.tree, "diff", "--name-only", CUT_OVER, after, "--",
                           "Broiler.JS", "Broiler.ECMAScript").splitlines()
        lines += [f"  {p}" for p in changed] or ["  none"]
        lines += [
            f"bases: test262/base/ (part A) and test262/base-new-york/ (part B), both taken at {BASE_COMMIT}",
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

    # -- vectors ---------------------------------------------------------------------------------

    def vectors(self) -> None:
        # The engine-level runner, from one build, once in each zone part A and part B run in, as this bundle's
        # runs before the after commit were taken.
        log: list[str] = []
        self.run(["dotnet", "build", VECTORS_TESTS, "-c", "Release", "--nologo"], self.tree, log)
        self.log_to("engine/after-build.log", log)
        for name, zone in ZONES:
            report = self.scratch / f"vectors-{name}.tsv"
            report.unlink(missing_ok=True)
            log = []
            env, self.env = self.env, dict(self.env, TZ=zone, BROILER_VECTORS_REPORT=str(report))
            try:
                self.run(["dotnet", "test", VECTORS_TESTS, "-c", "Release", "--no-build", "--nologo",
                          "--logger", "console;verbosity=normal"], self.tree, log)
            finally:
                self.env = env
            revision = "unknown"
            if report.exists():
                found = re.search(r"^# corpus revision: (\d+)", report.read_text(encoding="utf-8"), re.M)
                revision = found.group(1) if found else revision
                self.write_gz(f"engine/after-revision-{revision}-{name}.tsv.gz", report.read_text(encoding="utf-8"))
            log.insert(0, f"# TZ={zone}\n")
            self.log_to(f"engine/after-revision-{revision}-{name}.log", log)

    # -- test262 ---------------------------------------------------------------------------------

    def test262(self) -> None:
        if self.part not in PARTS:
            raise SystemExit("test262 imports one part's after run: pass --part A or --part B")
        if self.artifact_zip is None or not self.run_url or not self.artifact_id or not self.run_times:
            raise SystemExit("test262 imports the after run: pass --artifact-zip, --run-url, --artifact-id "
                             "and --run-times")
        base_folder, after_folder, compared, zone = PARTS[self.part]
        root = self.out / "test262"
        after = root / after_folder
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
            f"tested commit: {tested}, whose product sources are the cut-over commit {CUT_OVER}'s but for the "
            "files identity.txt lists",
            f"configuration: part {self.part}'s, {zone}",
            f"artifact: test262-evidence, id {self.artifact_id}, {self.artifact_zip.stat().st_size:,} bytes, "
            f"sha256 {digest} (the zip as downloaded)",
            f"retained: every file of the artifact, unchanged, in this directory ({len(names)} entries); this "
            "file is the only one added",
        ]) + "\n", encoding="utf-8", newline="\n")

        # The two runs' host-zone records, line for line: the rule reads them alike or does not judge the part.
        zones = []
        records = {}
        for side, folder in (("base", base_folder), ("after", after_folder)):
            path = root / folder / "host-zone.log"
            records[side] = path.read_text(encoding="utf-8").splitlines() if path.exists() else []
            expected = [line for line in records[side] if line.startswith("expected: ")]
            zones.append(f"## {side}: test262/{folder}/host-zone.log")
            zones += expected + [line for line in records[side] if line.startswith("shards: ")]
        alike = ([l for l in records["base"] if l.startswith("expected: ")]
                 == [l for l in records["after"] if l.startswith("expected: ")])
        zones.append(f"# the two runs' expected zone lines read alike, line for line: {alike}")
        self.write(f"test262/{compared}/zone-records.log", "\n".join(zones) + "\n")

        merged = {}
        for side, folder in (("base", base_folder), ("after", after_folder)):
            merged[side] = self.scratch / f"{side}-{self.part}-test262-results.json"
            with gzip.open(root / folder / "test262-results.json.gz", "rt", encoding="utf-8") as handle:
                merged[side].write_text(handle.read(), encoding="utf-8")
        script = self.tree / "scripts" / "compliance" / "compare_test262_paths.py"
        output = self.scratch / f"comparison-{self.part}.json"
        output.unlink(missing_ok=True)
        command = [sys.executable, str(script), "--base", str(merged["base"]), "--after", str(merged["after"]),
                   "--output", str(output)]
        for side, folder in (("base", base_folder), ("after", after_folder)):
            rerun = root / folder / "rerun-timeouts.json"
            if rerun.exists():
                command += [f"--{side}-rerun", str(rerun)]
        classifications = root / compared / "classifications.json"
        if classifications.exists():
            command += ["--classifications", str(classifications)]
        log: list[str] = []
        self.run(command, self.tree, log)
        self.log_to(f"test262/{compared}/comparison.log", log)
        if output.exists():
            self.write(f"test262/{compared}/comparison.json", output.read_text(encoding="utf-8"))

        # The script on the after run: against itself (no difference), and against a copy with one passing
        # row turned to failed (one regression, exit 1).
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
        self.write(f"test262/{compared}/compare-checks.log", "".join(checks))

    # -- simplification --------------------------------------------------------------------------

    def simplification(self) -> None:
        after = self.git(self.tree, "rev-parse", "HEAD")
        lines = [f"# SJL-6.6 deletion check, Broiler.JS at {after}: every entry of the Date area's Broiler.JS",
                 "# deletion list (Broiler.VM docs/javascript-shared-libraries/milestones/sjl-6-date.md, SJL-6.5, as",
                 "# JSD-0072's X6 extends it), searched in the file or folder that held it with git grep -E, test",
                 "# projects included where the folder holds them. An entry with no line is absent; a line that",
                 "# remains is an adapter, a call to the library, a comment or another member of the same name,",
                 "# which the bundle's README reads.", ""]
        for entry, paths, pattern in DELETED:
            hits = self.grep("-E", pattern, paths=["--", *paths])
            lines += [f"## {entry}", f"# in {', '.join(paths)}: {pattern}"] + hits + [""]
        self.write("simplification/deletion-check.log", "\n".join(lines) + "\n")

        hits = self.grep("-E", DUPLICATE_PATTERN)
        search = [f"# SJL-6.6 search for a surviving intra-engine duplicate of the Date area, Broiler.JS at {after}:",
                  "# every construct that spells a civil-day or time constant (the 400-year cycle's days, the civil",
                  "# epoch's shift, the day's milliseconds, the time value's limit), names a civil-day helper, spells a",
                  "# month or weekday name, parses a date by the platform, converts between local and universal time",
                  "# by the platform, or reads the platform's zones, in the engine's product sources (Broiler.JS/, test",
                  "# projects, benchmarks and samples excluded).",
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
            "bundle": "sjl-6-001",
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
    parser.add_argument("--part", choices=sorted(PARTS), help="test262: the part whose after run is imported")
    parser.add_argument("--artifact-zip", help="the after run's test262-evidence artifact, as downloaded")
    parser.add_argument("--run-url", help="the after run's workflow run, with its attempt")
    parser.add_argument("--artifact-id", help="the artifact's id")
    parser.add_argument("--run-times", help="when the after run was dispatched and completed")
    parser.add_argument("--out", required=True)
    parser.add_argument("--scratch", default=os.path.join(os.environ.get("TMPDIR", "/tmp"), "sjl-6-001-js"))
    parser.add_argument("--only", nargs="+", choices=STEPS, required=True)
    args = parser.parse_args(argv)
    args.label = "after"
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
