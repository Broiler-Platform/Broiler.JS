#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Collect Broiler.JS's evidence for milestone SJL-8 (work package SJL-8.7) into bundle sjl-8-001.

    python3 docs/evidence/sjl-8-001/collect.py --tree <a clean checkout of the after commit> \
        [--vm-tree <a checkout of Broiler.VM>] [--code-tree <a checkout of Broiler.Code with its CLI built>] \
        [--base-tree <a clean checkout of the rule's commit>] \
        [--part A|B --artifact-zip <that part's after run's test262-evidence artifact> --run-url <its workflow run> \
         --artifact-id <its id> --run-times <when it was dispatched and completed>] \
        --out docs/evidence/sjl-8-001 --only STEP [STEP ...]

THE RULE IS decision-rule.md BESIDE THIS FILE, committed at SJL-8.1 in 15a1bd8f before any code of SJL-8. Parts A
and B take the whole pinned suite, negative-metadata tests included, through .github/workflows/test262-evidence.yml,
part A in the runner's own zone, UTC, and part B with time_zone America/New_York. Both bases were taken at the
commit that carries the rule and imported by hand into test262/base/ and test262/base-new-york/, each with its
imported.txt; this collector imports the after runs the same way, into test262/after/ and test262/after-new-york/,
and compares each part's after run with its own base through scripts/compliance/compare_test262_paths.py as the
rule fixes it. The two parts' runs are never compared with each other. Part C, the reference datasets, is taken
by its own harness, scripts/conformance/run_reference_datasets.py, as the base was (datasets/README.md).

THE AFTER ARM IS A CHECKOUT, NOT THIS TREE. Every local build and run is taken at `--tree`, a clean checkout of
the after commit. That is the merge that lands SJL-8.6's last correction, row Z16 (4d558ee1, Broiler.JS PR #1048),
which the owner brought into SJL-8.6 on 2026-10-18 after part C's run at 5f96cd40, the merge of the cut-over and
corpus revision 23, was observed; the runs at 5f96cd40 are observed and not judged. The identity step lists every
product file at the after commit that differs from the cut-over's last commit, from the Z16 correction and from
5f96cd40.

IT REUSES BUNDLE SJL-7-001'S COLLECTOR, and through it sjl-6-001's, sjl-5-001's, sjl-4-001's, sjl-3-001's and
sjl-0-001's, for its helpers, the import of a workflow's run, the comparison and its checks, the completeness
record, the package record against nuget.org and the manifest. What it adds is SJL-8's: the data home's clauses
at the after commit, the engine-level vectors in both zones, a pristine consumer of the time-zone and calendar
area with its control at the base, the area's Broiler.JS deletion list, and the searches for a host zone, a .NET
calendar and a surviving duplicate of the area.

THE COLLECTOR JUDGES NOTHING. Each step writes what it ran and what it printed, failures included; the README
reads them under the rule.

Steps, each with `--only`:

  identity        the after commit and its cleanliness, the cut-over's commits and the product files that differ
                  between its last commit and the after commit, the rule commit, its ancestry and the files it
                  fixes unchanged since, part C's harness and copies, the library code against the code
                  0.1.0-preview.5 was published from, the SDK, Node and the machine, into identity.txt;
  home            the data home's clauses 1 to 6 at the after commit: the shared folder built in Release and its
                  tests run (the generator's PinTests, SL1 to SL10 with SL6's API baseline and SL7's
                  regeneration), the zic comparison, the assurance check, the home's files against SJL-8.2's and
                  its pins against Broiler.VM's, the public versions, the packages nuget.org serves at
                  0.1.0-preview.5 with a search of every entry for the tables' bytes, and the shared set's
                  pristine consumer, into home/after/;
  area            the area's tests in this engine, the changelog's SJL-8.6 tests, built in Release and run under
                  TZ=UTC, into home/after/engine-*.log;
  probe           the Native AOT probe over the corpus, published for linux-x64 and run, its report against the
                  JIT's, into home/after/probe-*;
  vectors         the engine-level vectors from one Release build, under TZ=UTC and TZ=America/New_York at
                  once, into engine/after-revision-<n>-utc.* and engine/after-revision-<n>-new-york.*;
  test262         with --part, that part's after run imported into test262/after/ (A) or test262/after-new-york/
                  (B), unchanged, with imported.txt;
  compare         with --part, the part's two runs' completeness and host-zone records, the comparison with the
                  part's base, with the part's classifications.json when it exists, and the script's checks,
                  into test262/comparison-utc/ or test262/comparison-new-york/; it may be run again, the import
                  may not;
  consumer        the engine set packed and restored by a pristine consumer, which then runs the area's checks
                  from the same isolated cache under TZ=America/New_York; and the same checks in the shell built
                  at the rule's commit and at the after commit, the control, into engine/;
  simplification  the area's Broiler.JS deletion list entry by entry, the search for a host zone and a .NET
                  calendar in Temporal and beyond it, and the search for a surviving duplicate of the area, into
                  simplification/;
  manifest        manifest.json: the SHA-256 of every file the bundle retains.

Paths in retained logs are written as <tree>, <base>, <vm>, <code>, <repo>, <run>, <scratch> and <tmp>.
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
import urllib.request
import zipfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
UNICODE_BUNDLE = HERE.parent / "sjl-7-001"

_spec = importlib.util.spec_from_file_location("sjl_7_001_collect", UNICODE_BUNDLE / "collect.py")
unicode = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(unicode)
date = unicode.date  # sjl-6-001's collector: its parts and zones
prior = unicode.prior  # sjl-3-001's: utc() and the product paths
base = unicode.base  # sjl-0-001's: the suite revision

RULE = "docs/evidence/sjl-8-001/decision-rule.md"
# What the rule fixes with itself: it, the comparison script, the workflow parts A and B dispatch, and the scripts
# the workflow runs and merges with.
RULE_FILES = (RULE, "scripts/compliance/compare_test262_paths.py", ".github/workflows/test262-evidence.yml",
              "scripts/compliance/run_test262.py", "scripts/compliance/merge_test262_shards.py")
# Part C's harness and population, committed at SJL-8.4 after the rule, as the rule asks.
DATASET_FILES = ("scripts/conformance/run_reference_datasets.py", "scripts/conformance/reference_dataset_driver.js",
                 "conformance/datasets/time")
DATASETS_COMMIT = "584caa2f32f0a2f903ebb794a512db9588ebfc17"
# The rule's commit, which carries the three parts' bases.
BASE_COMMIT = "15a1bd8f329f12238b093def5d1ddcd3e1dffe3d"
# The merge that lands SJL-8.6's last correction, row Z16, which both after runs name.
AFTER_COMMIT = "79a0506c870302cefa6d7105146c65feab898879"
# SJL-8.6's cut-over, its first and last commits that change product sources; and row Z16's correction, which the
# owner brought into SJL-8.6 on 2026-10-18.
CUT_OVER_FIRST = "f58f9b491542bee1b373e4db40de9681cd666a59"
CUT_OVER = "eb2ab599d9d98f62ddce0fa8ecdd58b0125ea31d"
Z16_COMMIT = "4d558ee1d8af2cdd45a66ec416854f8881781ef3"
# The merge of the cut-over and corpus revision 23, where the first after runs were taken: observed, not judged.
OBSERVED_COMMIT = "5f96cd4090a08565044ce29503733a0cc051fc87"
PRODUCT_PATHS = ("Broiler.JS", "Broiler.ECMAScript", "Directory.Packages.props", "Directory.Build.props",
                 "Directory.Build.targets", "NuGet.config", "global.json")
# The shared stream's version this engine depends on, published to nuget.org from PUBLISHED_FROM, whose library
# code is LIBRARY_CODE's.
SHARED_VERSION = "0.1.0-preview.5"
PUBLISHED_FROM = "a04cd447af3424951eb767524812b5b310d71da3"
LIBRARY_CODE = "2d37a38a2560b076ebf2ca29989bfe20db64ed64"
SHARED_PACKAGES = (("Broiler.ECMAScript.Algorithms", SHARED_VERSION), ("Broiler.ECMAScript.Generation", SHARED_VERSION))
LIBRARY_PATHS = ("Broiler.ECMAScript/Broiler.ECMAScript.Algorithms", "Broiler.ECMAScript/Broiler.ECMAScript.Generation")
# The data home, and the commit at which section 1 read its clauses.
HOME = "Broiler.ECMAScript/data/time"
HOME_READ_AT = "19e7458853626b541766790f48a2813eda09eb2e"
# The home's pins and inputs, against Broiler.VM's copies of them.
VM_PINS = {
    "tzdb/tzdb.pin": "src/tests/tzdb/pins/tzdb.pin",
    "tzdb/tzdata2026e.tar.gz": "src/tests/tzdb/pins/tzdata2026e.tar.gz",
    "calendars/calendars.pin": "src/tests/calendars/pins/calendars.pin",
    "calendars/icu_calendar-2.3.0.crate": "src/tests/calendars/pins/icu_calendar-2.3.0.crate",
    "calendars/calendrical_calculations-0.2.4.crate": "src/tests/calendars/pins/calendrical_calculations-0.2.4.crate",
    "cldr/cldr-48.2.0/json/cldr-bcp47/bcp47/timezone.json":
        "src/tests/cldr/pins/cldr-48.2.0/json/cldr-bcp47/bcp47/timezone.json",
}
VECTORS_TESTS = "Broiler.JS/Broiler.JavaScript.Vectors.Tests"
SHELL_PROJECT = "Broiler.JS/Broiler.JavaScript/Broiler.JavaScript.csproj"
SHELL_DLL = "Broiler.JS/Broiler.JavaScript/bin/Release/net10.0/BroilerJS.dll"
ZONES = (("utc", "UTC"), ("new-york", "America/New_York"))
PARTS = {
    "A": ("base", "after", "comparison-utc", "part A's, in the runner's own zone (time_zone empty: TZ unset, UTC)"),
    "B": ("base-new-york", "after-new-york", "comparison-new-york",
          "part B's, with time_zone America/New_York (TZ set for every shard and the re-run)"),
}
STEPS = ("identity", "home", "area", "probe", "vectors", "test262", "compare", "consumer", "simplification",
         "manifest")
PROBE = "Broiler.ECMAScript/Broiler.ECMAScript.Probe"
# The area's tests in this engine, by project, as the changelog's SJL-8.6 sections name them: the embedded tables
# held to the home's bytes and the manifest's digests, every time-zone row, the host zone (A15, A16) and the
# calendar rows, the integration tests the cut-over changed, and those row Z16's correction changed.
AREA_TESTS = {
    "Broiler.JS/Broiler.JavaScript.BuiltIns.Tests": ("SharedTimeZoneTests", "SharedTimeZoneHostTests",
                                                     "SharedCalendarTableTests"),
    "Broiler.JS/Broiler.JavaScript.Integration.Tests": ("Issue773Tests", "Issue775Tests", "Issue783Tests",
                                                        "Issue794Tests", "Issue786Tests"),
}

# The area's Broiler.JS deletion list: Broiler.VM docs/javascript-shared-libraries/milestones/
# sjl-8-time-zones-and-calendars.md, SJL-8.6, as JSD-0076's X6 reads and extends it, entry by entry: what was
# deleted, the file or folder that held it, and what is searched there. A call to the library, an adapter or a
# comment shows its line. Each member's name is the one it had at the rule's commit.
JS = "Broiler.JS/Broiler.JavaScript.BuiltIns/"
TEMPORAL = JS + "Temporal"
DELETED = (
    ("Temporal/Tz/IanaTimeZone, IanaTimeZoneDatabase and TzifReader", ["Broiler.JS"],
     r"\bIanaTimeZone\b|\bIanaTimeZoneDatabase\b|\bTzifReader\b|Temporal\.Tz\b|\bTz\.|namespace [A-Za-z.]*\.Tz\b|TZif"),
    ("the embedded tzdata.bin and its EmbeddedResource item", ["Broiler.JS"], r"tzdata\.bin|tzdata"),
    ("scripts/tzdata/generate_tzdata.py", ["scripts", ".github", "Broiler.JS", "eng"],
     r"generate_tzdata|scripts/tzdata|zoneinfo/tzdata\.zi"),
    ("JSTemporalZonedDateTime's offset, candidate, disambiguation, start-of-day and transition helpers, with the "
     "zero offset and the local-midnight fallback", [TEMPORAL],
     r"\b(FindTransition|GetOffsetNanosecondsFor|OffsetNanosecondsForInstant|GetOffsetForLocal|"
     r"CandidateOffsetsForLocal|TryMatchZoneOffset|StartOfDayEpochNs|TryFixedOffset|OffsetIdPattern|ResolveNamedZone|"
     r"TryCanonicalizeTimeZoneIdentifier|OffsetTimeZoneIdentifierPattern|TryOffsetTimeZoneIdentifier|"
     r"GetPossibleEpochNanoseconds|DisambiguatePossibleEpochNanoseconds)\b"),
    ("JSIntl's offset-zone normalisation and its region table for getTimeZones (Z12)", [JS + "Intl"],
     r"\b(OffsetTimeZonePattern|TryNormalizeOffsetTimeZone|RegionPrimaryTimeZone)\b|Region.IANA|not bundled"),
    ("the formatting engine's zone helpers, with their host-zone fallback for an unknown name",
     [JS + "Intl/JSIntlDateTimeFormatEngine.cs"],
     r"\bTryParseOffset\b|IanaTimeZoneDatabase|EcmaLocalTime\.LocalTime\(clippedMs, JSDate\.Zone\)|"
     r"TemporalTimeZones\.(Rules|Database|Host)"),
    ("TemporalLunisolarCalendar and TemporalSolarHijriCalendar", ["Broiler.JS"],
     r"\bTemporalLunisolarCalendar\b|\bTemporalSolarHijriCalendar\b|\bIsLunisolar\b|\bIsSolarHijri\b"),
    ("the Umm al-Qura and .NET dispatch of TemporalArithmeticCalendar and TemporalCalendarMath",
     [TEMPORAL + "/TemporalArithmeticCalendar.cs", TEMPORAL + "/TemporalCalendarMath.cs"],
     r"umalqura|UmmAlQura|UmAlQura|Lunisolar|SolarHijri|Persian|System\.Globalization|UnicodeCldr|"
     r"TemporalTableCalendar"),
    ("this engine's uses of the CLDR package's CldrChineseCalendar and CldrUmmAlQura", ["Broiler.JS"],
     r"\bCldrChineseCalendar\b|\bCldrUmmAlQura\b|\bChineseYear\b"),
)
# Clause 8: a host zone or a .NET calendar in Temporal, and where either remains in the rest of the engine.
HOST_SEARCHES = (
    ("Temporal: the host's zone, .NET's calendars and the CLDR package's calendars", [TEMPORAL],
     r"TimeZoneInfo|\bTimeZone\.CurrentTimeZone\b|DateTimeOffset\.Now|DateTime\.Now|ToLocalTime\(|ToUniversalTime\(|"
     r"\bnew [A-Za-z.]*Calendar *\(|System\.Globalization\.[A-Za-z]*Calendar|"
     r"\b(Chinese|Korean|Japanese|Taiwan)LunisolarCalendar\b|"
     r"\b(Persian|UmAlQura|Hijri|Hebrew|Gregorian|Julian|Japanese|Taiwan|ThaiBuddhist|Korean)Calendar\b|"
     r"(Culture|DateTimeFormat|Format)[A-Za-z]*\.Calendar\b|\.OptionalCalendars\b|CldrChineseCalendar|CldrUmmAlQura|"
     r"UnicodeCldr"),
    ("Temporal: the System.Globalization members each file names (a summary per file, not every line)", [TEMPORAL],
     r"\b(CultureInfo|NumberStyles|NumberFormatInfo|DateTimeFormatInfo|DateTimeStyles|CompareInfo|TextInfo|"
     r"CharUnicodeInfo|UnicodeCategory|StringInfo|RegionInfo|IdnMapping|CalendarWeekRule)\b"),
    ("the rest of the engine: the host's zone and .NET's calendars outside Temporal", ["Broiler.JS"],
     r"TimeZoneInfo|\b(Chinese|Korean|Japanese|Taiwan)LunisolarCalendar\b|"
     r"\b(Persian|UmAlQura|Hijri|Hebrew|Gregorian|Julian|Japanese|Taiwan|ThaiBuddhist|Korean)Calendar\b|"
     r"CldrChineseCalendar|CldrUmmAlQura|\.ToLocalTime\(|DateTimeOffset\.Now\b|DateTime\.Now\b"),
)
# The search for a surviving duplicate of the area outside the shared library: a zone's data or its reader, the
# zone algorithms by name, a host's compiled zoneinfo, lunisolar or Persian astronomy, and the table calendars'
# year data spelled in code.
DUPLICATE_SEARCHES = (
    ("a zone's data or its reader: TZif, zoneinfo, tzdata, a Rule or Zone line, a POSIX TZ string",
     r"TZif|zoneinfo|tzdata|\bposixrules\b|\bM3\.2\.0\b|\bTZ string\b|\bRuleLine\b|\bZoneLine\b"),
    ("the zone algorithms by name: offset at an instant, possible instants, disambiguation, start of day, "
     "transitions, primaries, lookup",
     r"(static|private|internal|public|protected)[^=;(]*\b(\w*Offset\w*For\w*|\w*PossibleEpoch\w*|"
     r"\w*Disambiguat\w*|\w*StartOfDay\w*|\w*(Zone|Next|Previous)Transition\w*|\w*Primary\w*Zone\w*|"
     r"\w*Canonicali[sz]e\w*Zone\w*|"
     r"\w*NamedTimeZone\w*)\s*[(<]"),
    ("the calendars' years outside the tables: new moons, solar terms, astronomy, the 33-year rule, ICU4X's "
     "approximation, and the table calendars' epochs",
     r"(?i)new ?moon|solar ?term|\blongitude\b|\bastronom|33-year|\b(227015|226896|1948320|1948439)\b|"
     r"mean.?motion|\bICU4X\b"),
)
# The pristine consumer's checks of the area: each a correction or a policy change of SJL-8.6, its cut-over and
# row Z16's correction, run from the packages restored into the consumer's isolated cache, with
# TZ=America/New_York. Each is a JavaScript expression that must be true.
TIME_CHECKS = (
    ("Z1", "Temporal.Instant.from('2027-01-15T12:00Z').toZonedDateTimeISO('America/Vancouver').offset === '-07:00'"),
    ("Z3", "Temporal.Instant.from('1937-07-01T12:00Z').toZonedDateTimeISO('Europe/Amsterdam').offset === '+01:00'"),
    ("Z4", "Temporal.Instant.from('2040-07-01T12:00Z').toZonedDateTimeISO('America/New_York').offset === '-04:00'"),
    ("A10", "Temporal.ZonedDateTime.from('2037-12-01T00:00[America/New_York]').getTimeZoneTransition('next')"
            ".toString() === '2038-03-14T03:00:00-04:00[America/New_York]'"),
    ("Z5", "(() => { try { new Temporal.ZonedDateTime(0n, 'Factory'); return false; } "
           "catch (e) { return e instanceof RangeError; } })()"),
    ("Z7", "Intl.supportedValuesOf('timeZone').length === 445 && !new Temporal.ZonedDateTime(0n, "
           "'Europe/Bratislava').equals(new Temporal.ZonedDateTime(0n, 'Europe/Prague'))"),
    ("Z10", "(() => { try { new Temporal.ZonedDateTime(0n, 'America/Lo\\u017F_Angeles'); return false; } "
            "catch (e) { return e instanceof RangeError; } })()"),
    ("Z11", "(() => { try { new Intl.DateTimeFormat('en', { timeZone: '2020-01-01T00:00[Europe/Berlin]' }); "
            "return false; } catch (e) { return e instanceof RangeError; } })()"),
    ("Z12", "new Intl.Locale('en-DE').getTimeZones().join(',') === 'Europe/Berlin,Europe/Busingen'"),
    ("A13", "(() => { try { new Temporal.ZonedDateTime(0n, '+\\u0660\\u0665:\\u0663\\u0660'); return false; } "
            "catch (e) { return e instanceof RangeError; } })()"),
    ("K2", "Temporal.PlainDate.from({ year: 2057, monthCode: 'M09', day: 1, calendar: 'chinese' })"
           ".withCalendar('iso8601').toString() === '2057-09-28'"),
    ("K7", "(() => { const d = Temporal.PlainDate.from('-271821-04-19').withCalendar('chinese'); "
           "return d.monthCode === 'M03' && d.day === 17; })()"),
    ("K12", "Temporal.PlainDate.from('0622-03-21').withCalendar('persian').day === 1 && "
            "Temporal.PlainDate.from('0622-03-22').withCalendar('persian').day === 2"),
    ("A15", "Temporal.Now.timeZoneId() === 'America/New_York' && new Date(1850, 0, 1).getTimezoneOffset() === "
            "296.03333333333336 && new Date(2040, 6, 1, 12).getTimezoneOffset() === 240"),
    ("Z16", "new Intl.DateTimeFormat('en-US', { timeZone: 'Africa/Abidjan', timeZoneName: 'longOffset' })"
            ".format(Date.UTC(1900, 0, 1)) === '12/31/1899 GMT-00:16:08'"),
)
CHECKS_ZONE = "America/New_York"


def checks_script() -> str:
    """The checks as one program whose completion value is `ok <n>` or `failed <rows>`."""
    checks = ",\n".join(f"  [{json.dumps(row)}, () => {expression}]" for row, expression in TIME_CHECKS)
    return ("(() => {\n const checks = [\n" + checks + "\n ];\n"
            " const failed = [];\n"
            " for (const c of checks) { let r; try { r = c[1](); } catch (e) { r = e.name + ': ' + e.message; }"
            " if (r !== true) failed.push(c[0] + ' (' + String(r) + ')'); }\n"
            " return failed.length === 0 ? 'ok ' + checks.length : 'failed ' + failed.join(', ');\n})()")


class Collector(unicode.Collector):
    def __init__(self, args: argparse.Namespace) -> None:
        super().__init__(args)
        self.base_commit = BASE_COMMIT
        self.part = args.part
        self.vm = Path(args.vm_tree).resolve() if args.vm_tree else None
        self.code = Path(args.code_tree).resolve() if args.code_tree else None
        self.base_tree = Path(args.base_tree).resolve() if args.base_tree else None

    def hide(self, text: str) -> str:
        for path, name in ((self.base_tree, "<base>"), (self.vm, "<vm>"), (self.code, "<code>")):
            if path is not None:
                text = text.replace(str(path), name)
        return super().hide(text)

    def rule_commit(self) -> str:
        return self.git(self.tree, "log", "--diff-filter=A", "--format=%H", "--", RULE).splitlines()[-1]

    # -- identity --------------------------------------------------------------------------------

    def identity(self) -> None:
        after = self.after()
        rule = self.rule_commit()
        lines = [
            "bundle: sjl-8-001 (Broiler.JS), its after arm",
            f"after commit: {after}",
            f"after commit is the one parts A's and B's after runs name ({AFTER_COMMIT}): {after == AFTER_COMMIT}",
            f"after tree clean: {self.git(self.tree, 'status', '--porcelain', '--untracked-files=all') == ''}",
            f"cut-over: SJL-8.6, {CUT_OVER_FIRST[:8]} to {CUT_OVER[:8]}; its commits that change product sources:",
        ]
        for line in self.git(self.tree, "log", "--reverse", "--format=%H %s", f"{CUT_OVER_FIRST}^..{CUT_OVER}", "--",
                             *PRODUCT_PATHS).splitlines():
            lines.append(f"  {line}")
        lines.append(f"commits between the cut-over's last ({CUT_OVER[:8]}) and the after commit, with the files each "
                     "changes (merges by subject only):")
        for line in self.git(self.tree, "log", "--reverse", "--no-merges", "--format=%H %s",
                             f"{CUT_OVER}..{after}").splitlines():
            commit = line.split(" ", 1)[0]
            lines.append(f"  {line}")
            files = self.git(self.tree, "diff-tree", "--no-commit-id", "--name-only", "-r", commit).splitlines()
            lines += [f"    {f}" for f in files]
        for line in self.git(self.tree, "log", "--reverse", "--merges", "--format=%H %s",
                             f"{CUT_OVER}..{after}").splitlines():
            lines.append(f"  merge: {line}")
        lines.append(f"files under {', '.join(PRODUCT_PATHS)} that differ between the cut-over's last commit and "
                     "the after commit:")
        changed = self.git(self.tree, "diff", "--name-only", CUT_OVER, after, "--", *PRODUCT_PATHS).splitlines()
        lines += [f"  {p}" for p in changed] or ["  none"]
        lines.append(f"row Z16's correction, brought into SJL-8.6 at the owner's direction: {Z16_COMMIT}, an ancestor "
                     f"of the after commit: {self.is_ancestor(Z16_COMMIT, after)}")
        lines.append("files that differ between it and the after commit: ")
        changed = self.git(self.tree, "diff", "--name-only", Z16_COMMIT, after).splitlines()
        lines[-1] += ", ".join(changed) if changed else "none"
        lines.append(f"files that differ between {OBSERVED_COMMIT[:8]} (the first after runs, observed and not judged) "
                     "and the after commit:")
        changed = self.git(self.tree, "diff", "--name-only", OBSERVED_COMMIT, after).splitlines()
        lines += [f"  {p}" for p in changed] or ["  none"]
        lines += [
            f"bases: test262/base/ (part A) and test262/base-new-york/ (part B), and datasets/base/ (part C), all "
            f"taken at {BASE_COMMIT}",
            f"rule commit: {rule} (the commit that added {RULE})",
            f"rule commit is the base commit: {rule == BASE_COMMIT}",
            f"rule commit an ancestor of the after commit: {self.is_ancestor(rule, after)}",
            "files the rule fixes, changed between the rule commit and the after commit:",
        ]
        fixed = self.git(self.tree, "diff", "--name-only", rule, after, "--", *RULE_FILES).splitlines()
        lines += [f"  {p}" for p in fixed] or ["  none"]
        lines.append(f"part C's harness and copies, changed between their commit ({DATASETS_COMMIT[:8]}) and the after "
                     "commit:")
        moved = self.git(self.tree, "diff", "--name-only", DATASETS_COMMIT, after, "--", *DATASET_FILES).splitlines()
        lines += [f"  {p}" for p in moved] or ["  none"]
        for path in DATASET_FILES[:2]:
            lines.append(f"  {self.sha256(self.tree / path)}  {path}")
        lines.append(f"the shared library's code, changed between {LIBRARY_CODE[:8]} (the code {SHARED_VERSION} was "
                     f"published from, at {PUBLISHED_FROM[:8]}) and the after commit:")
        library = self.git(self.tree, "diff", "--name-only", LIBRARY_CODE, after, "--", *LIBRARY_PATHS).splitlines()
        lines += [f"  {p}" for p in library] or ["  none"]
        lines.append(f"collector: docs/evidence/sjl-8-001/collect.py, sha256 {self.sha256(Path(__file__))}, run from "
                     f"<repo> at {self.git(HERE, 'rev-parse', 'HEAD')}, committed with this bundle")
        log: list[str] = []
        for command in (["dotnet", "--version"], ["dotnet", "--list-runtimes"], ["node", "--version"]):
            lines.append(f"{' '.join(command)}: {self.run(command, self.tree, log).stdout.strip()}")
        lines += self.machine()
        lines.append(f"suite revision: {base.SUITE_REVISION}")
        lines.append(f"collected: {prior.utc()}")
        self.write("identity.txt", "\n".join(lines) + "\n")

    # -- home ------------------------------------------------------------------------------------

    def home(self) -> None:
        after = self.after()
        out = "home/after"
        log: list[str] = []
        self.run(["dotnet", "build", "Broiler.ECMAScript.slnf", "-c", "Release", "--nologo"], self.tree, log)
        self.log_to(f"{out}/build.log", log)
        log = []
        self.run(["dotnet", "test", "Broiler.ECMAScript.slnf", "-c", "Release", "--no-build", "--nologo",
                  "--logger", "console;verbosity=normal"], self.tree, log)
        self.log_to(f"{out}/tests.log", log)
        log = []
        self.run([sys.executable, "-I", f"{HOME}/tzdb/check-zic.py"], self.tree, log)
        self.log_to(f"{out}/zic-check.log", log)
        if self.code is not None:
            log = []
            cli = self.code / "src" / "Broiler.Code.Review.Cli" / "bin" / "Release" / "net10.0" / "Broiler.Code.Review.Cli.dll"
            log.append(f"# Broiler.Code at {self.git(self.code, 'rev-parse', 'HEAD')}, its CLI as built there\n")
            self.run(["dotnet", str(cli), "assurance", "check", "--root", str(self.tree / "Broiler.ECMAScript")],
                     self.tree, log)
            self.log_to(f"{out}/assurance.log", log)

        # The home's files: what changed since section 1 read the clauses, each file's blob, the generated
        # tables against their manifest, the pins against Broiler.VM's, and the public versions.
        lines = [f"# SJL-8.7: the data home ({HOME}/) at {after}", "",
                 f"## files changed between {HOME_READ_AT[:8]} (section 1's reading) and the after commit"]
        changed = self.git(self.tree, "diff", "--stat", HOME_READ_AT, after, "--", HOME).splitlines()
        lines += [f"  {l}" for l in changed] or ["  none"]
        lines += ["", "## every file, by git blob"]
        lines += [f"  {l.split()[2]}  {l.split(chr(9), 1)[1]}" for l in
                  self.git(self.tree, "ls-tree", "-r", after, "--", HOME + "/").splitlines()]
        generated = self.tree / HOME / "generated"
        lines += ["", "## the generated files, SHA-256 and length"]
        for path in sorted(generated.iterdir()):
            if path.is_file():
                lines.append(f"  {self.sha256(path)}  {path.stat().st_size:>8}  {path.name}")
        if self.vm is not None:
            lines += ["", f"## the pins and inputs against Broiler.VM's at {self.git(self.vm, 'rev-parse', 'HEAD')}"]
            for ours, theirs in VM_PINS.items():
                here, there = self.tree / HOME / ours, self.vm / theirs
                if not there.exists():
                    lines.append(f"  absent in Broiler.VM  {theirs}")
                    continue
                same = here.read_bytes() == there.read_bytes()
                lines.append(f"  {'identical' if same else 'DIFFERENT'}  {HOME}/{ours}  {self.sha256(here)}")
        lines += ["", "## the public versions in the generator's API baseline (PublicAPI.txt)"]
        lines += [f"  {l}" for l in self.grep("-E", "TimeDataVersions",
                                              paths=["--", "Broiler.ECMAScript/Broiler.ECMAScript.Generation/PublicAPI.txt"])]
        self.write(f"{out}/data-home.log", "\n".join(lines) + "\n")

        # The packages nuget.org serves at the shared stream's version, each with a search of its entries for
        # the bytes of the generated tables and of the pinned archives (no package carries either, SJL-D-6 (c)).
        log = [f"# SJL-8.7: Broiler.ECMAScript's packages at {SHARED_VERSION} as nuget.org serves them, published from "
               f"{PUBLISHED_FROM}; the library code there is {LIBRARY_CODE}'s, and identity.txt lists any file of it "
               "that differs at the after commit\n\n"]
        probes = {}
        for path in sorted(generated.iterdir()):
            if path.is_file() and path.stat().st_size >= 64:
                data = path.read_bytes()
                probes[f"generated/{path.name}"] = [data[i:i + 32] for i in (0, len(data) // 2, len(data) - 32)]
        for pinned in ("tzdb/tzdata2026e.tar.gz", "calendars/icu_calendar-2.3.0.crate",
                       "calendars/calendrical_calculations-0.2.4.crate",
                       "cldr/cldr-48.2.0/json/cldr-bcp47/bcp47/timezone.json"):
            data = (self.tree / HOME / pinned).read_bytes()
            probes[pinned] = [data[i:i + 32] for i in (0, len(data) // 2, len(data) - 32)]
        for package_id, version in SHARED_PACKAGES:
            lowered = package_id.lower()
            served = self.scratch / "nuget-org" / f"{lowered}.{version}.nupkg"
            served.parent.mkdir(parents=True, exist_ok=True)
            url = f"https://api.nuget.org/v3-flatcontainer/{lowered}/{version}/{served.name}"
            log.append(f"## {package_id} {version}\n")
            try:
                with urllib.request.urlopen(url, timeout=60) as response:
                    served.write_bytes(response.read())
            except OSError as error:
                log.append(f"nuget.org: not fetched ({error})\n\n")
                continue
            log.append(f"nuget.org serves {url}: {served.stat().st_size:,} bytes; sha512 "
                       f"{hashlib.sha512(served.read_bytes()).hexdigest()}\n")
            self.run(["dotnet", "nuget", "verify", "--all", str(served)], self.tree, log)
            with zipfile.ZipFile(served) as archive:
                names = sorted(archive.namelist())
                nuspec = next(n for n in names if n.endswith(".nuspec"))
                text = archive.read(nuspec).decode("utf-8")
                repository = re.search(r"<repository [^>]*>", text)
                log.append(f"nuspec repository: {repository.group(0) if repository else 'none'}\n")
                log.append("nuspec dependencies: " + (", ".join(
                    f"{m.group(1)} {m.group(2)}" for m in re.finditer(r'<dependency id="([^"]+)" version="([^"]+)"', text))
                    or "none") + "\n")
                log.append(f"entries ({len(names)}):\n")
                found = []
                for name in names:
                    data = archive.read(name)
                    log.append(f"  {hashlib.sha256(data).hexdigest()}  {len(data):>9}  {name}\n")
                    for what, pieces in probes.items():
                        if any(piece in data for piece in pieces):
                            found.append(f"{name} holds bytes of {what}")
                log.append("entries holding 32 bytes taken from the start, the middle or the end of a generated table "
                           "or a pinned archive: " + ("; ".join(found) if found else "none") + "\n\n")
        self.log_to(f"{out}/packages.log", log)

        log = []
        self.run([sys.executable, "scripts/packaging/test_pristine_consumer.py", "--set", "shared",
                  "--version", SHARED_VERSION], self.tree, log)
        self.log_to(f"{out}/pristine-consumer-shared.log", log)

    # -- area ------------------------------------------------------------------------------------

    def area(self) -> None:
        # The area's tests in this engine, built in Release and run under TZ=UTC, as this repository's tests run.
        log: list[str] = []
        for project in AREA_TESTS:
            self.run(["dotnet", "build", project, "-c", "Release", "--nologo"], self.tree, log)
        self.log_to("home/after/engine-build.log", log)
        log = []
        env, self.env = self.env, dict(self.env, TZ="UTC")
        self.env.pop("LANG", None)
        try:
            for project, classes in AREA_TESTS.items():
                selection = "|".join(f"FullyQualifiedName~.{name}." for name in classes)
                self.run(["dotnet", "test", project, "-c", "Release", "--no-build", "--nologo", "--filter", selection,
                          "--logger", "console;verbosity=normal"], self.tree, log)
        finally:
            self.env = env
        self.log_to("home/after/engine-tests.log", log)

    # -- probe -----------------------------------------------------------------------------------

    def probe(self) -> None:
        # The Native AOT probe, as sjl-3-001's home step takes it: published, the publish directory listed, the
        # corpus run, and the report compared with the same probe's under the JIT.
        rid = "linux-x64"
        out = "home/after"
        log: list[str] = []
        result = self.run(["dotnet", "publish", PROBE, "-c", "Release", "-r", rid, "--nologo"], self.tree, log)
        warnings = sorted({line.strip() for line in result.stdout.splitlines() if ": warning " in line})
        log.append(f"# publish exit {result.returncode}; distinct warning lines: {len(warnings)}\n")
        log.extend(line + "\n" for line in warnings)
        self.log_to(f"{out}/probe-publish-{rid}.log", log)
        published = self.tree / PROBE / "bin" / "Release" / "net10.0" / rid / "publish"
        log = []
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
        self.log_to(f"{out}/probe-run-{rid}.log", log)
        if aot.exists():
            self.write_gz(f"{out}/probe-report-aot.tsv.gz", aot.read_text(encoding="utf-8"))

    # -- vectors ---------------------------------------------------------------------------------

    def vectors(self) -> None:
        # One Release build, then the runner once in each zone parts A and B run in, both at once.
        log: list[str] = []
        self.run(["dotnet", "build", VECTORS_TESTS, "-c", "Release", "--nologo"], self.tree, log, check=True)
        self.log_to("engine/after-build.log", log)
        running = []
        for name, zone in ZONES:
            report = self.scratch / f"vectors-{name}.tsv"
            report.unlink(missing_ok=True)
            env = dict(self.env, TZ=zone, BROILER_VECTORS_REPORT=str(report))
            env.pop("LANG", None)
            started = prior.utc()
            process = subprocess.Popen(["dotnet", "test", VECTORS_TESTS, "-c", "Release", "--no-build", "--nologo",
                                        "--logger", "console;verbosity=normal"], cwd=self.tree, env=env,
                                       stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
            running.append((name, zone, report, started, process))
        for name, zone, report, started, process in running:
            stdout, stderr = process.communicate()
            revision = "unknown"
            if report.exists():
                found = re.search(r"^# corpus revision: (\d+)", report.read_text(encoding="utf-8"), re.M)
                revision = found.group(1) if found else revision
                self.write_gz(f"engine/after-revision-{revision}-{name}.tsv.gz", report.read_text(encoding="utf-8"))
            control = report.with_suffix("").with_name(report.stem + ".negative-control.tsv")
            if control.exists():
                self.write(f"engine/negative-control-after-revision-{revision}-{name}.tsv",
                           control.read_text(encoding="utf-8"))
            lines = [f"== start {started} TZ={zone} at {self.after()} (a clean checkout; built once beforehand in "
                     f"Release, run with --no-build; the two zones run at once); exit {process.returncode}\n", stdout]
            if stderr:
                lines.append("# stderr\n" + stderr)
            lines.append(f"== end {prior.utc()}\n")
            self.log_to(f"engine/after-revision-{revision}-{name}.log", lines)

    # -- test262 ---------------------------------------------------------------------------------

    def test262(self) -> None:
        if self.part not in PARTS:
            raise SystemExit("test262 imports one part's after run: pass --part A or --part B")
        if self.artifact_zip is None or not self.run_url or not self.artifact_id or not self.run_times:
            raise SystemExit("test262 imports the after run: pass --artifact-zip, --run-url, --artifact-id "
                             "and --run-times")
        _, after_folder, _, configuration = PARTS[self.part]
        after = self.out / "test262" / after_folder
        if after.exists():
            raise SystemExit(f"{after} exists; the import writes it once")
        after.mkdir(parents=True)
        with zipfile.ZipFile(self.artifact_zip) as archive:
            names = sorted(archive.namelist())
            archive.extractall(after)
        digest = hashlib.sha256(self.artifact_zip.read_bytes()).hexdigest()
        run = (after / "run.txt").read_text(encoding="utf-8") if (after / "run.txt").exists() else ""
        fields = dict(line.split(": ", 1) for line in run.splitlines() if ": " in line)
        tested = fields.get("tested commit", "(run.txt names none)")
        (after / "imported.txt").write_text("\n".join([
            "# imported: the after run was taken by .github/workflows/test262-evidence.yml, not locally",
            f"workflow run: {self.run_url}, {self.run_times}",
            f"tested commit: {tested} (the merge that lands row Z16's correction, the last of SJL-8.6, after its "
            f"cut-over and corpus revision 23), whose product sources are {Z16_COMMIT[:8]}'s; identity.txt lists "
            "every file",
            f"configuration: {configuration}, with negative-metadata tests (include_negative true); includeNegative "
            f"{fields.get('includeNegative', '(not in run.txt)')} in run.txt",
            f"artifact: test262-evidence, id {self.artifact_id}, {self.artifact_zip.stat().st_size:,} bytes, "
            f"sha256 {digest} (the zip as downloaded)",
            f"retained: every file of the artifact, unchanged, in this directory ({len(names)} entries); this "
            "file is the only one added",
        ]) + "\n", encoding="utf-8", newline="\n")

    # -- compare ---------------------------------------------------------------------------------

    def compare(self) -> None:
        if self.part not in PARTS:
            raise SystemExit("compare compares one part's runs: pass --part A or --part B")
        base_folder, after_folder, compared, _ = PARTS[self.part]
        root = self.out / "test262"
        folders = {"base": base_folder, "after": after_folder}
        merged, loaded = {}, {}
        for side, folder in folders.items():
            merged[side] = self.scratch / f"{side}-{self.part}-test262-results.json"
            with gzip.open(root / folder / "test262-results.json.gz", "rt", encoding="utf-8") as handle:
                text = handle.read()
            merged[side].write_text(text, encoding="utf-8")
            loaded[side] = json.loads(text)

        lines = [f"# Part {self.part}'s completeness conditions for each run, read from the files the workflow wrote:",
                 "# every expected shard reported, no configuration failure, one suite revision, one shell, every",
                 "# shard in the zone, includeNegative true.", ""]
        for side, folder in folders.items():
            lines.append(f"## {side}: test262/{folder}/")
            lines += self.completeness(root / folder, loaded[side])
            lines.append("")
        self.write(f"test262/{compared}/completeness.log", "\n".join(lines))

        zones, records = [], {}
        for side, folder in folders.items():
            path = root / folder / "host-zone.log"
            records[side] = path.read_text(encoding="utf-8").splitlines() if path.exists() else []
            zones.append(f"## {side}: test262/{folder}/host-zone.log")
            zones += [l for l in records[side] if l.startswith("expected: ") or l.startswith("shards: ")]
        alike = ([l for l in records["base"] if l.startswith("expected: ")]
                 == [l for l in records["after"] if l.startswith("expected: ")])
        zones.append(f"# the two runs' expected zone lines read alike, line for line: {alike}")
        self.write(f"test262/{compared}/zone-records.log", "\n".join(zones) + "\n")

        script = self.tree / "scripts" / "compliance" / "compare_test262_paths.py"
        output = self.scratch / f"comparison-{self.part}.json"
        output.unlink(missing_ok=True)
        command = [sys.executable, str(script), "--base", str(merged["base"]), "--after", str(merged["after"]),
                   "--output", str(output)]
        for side, folder in folders.items():
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
        mutated = loaded["after"]
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
        for path in merged.values():
            path.unlink(missing_ok=True)

    # -- consumer --------------------------------------------------------------------------------

    def consumer(self) -> None:
        # The engine set, packed from the after commit, restored by a clean consumer from a local feed and
        # nuget.org into an isolated cache, and run (it prints 42); the consumer is kept for the next part.
        log: list[str] = []
        result = self.run([sys.executable, "scripts/packaging/test_pristine_consumer.py", "--set", "engine", "--keep"],
                          self.tree, log)
        self.log_to("engine/pristine-consumer.log", log)
        kept = re.search(r"^Retained: (.+)$", result.stdout, re.M)
        script = checks_script()
        if result.returncode == 0 and kept:
            temporary = Path(kept.group(1).strip())
            consumer = temporary / "consumer"
            cache = temporary / "packages"
            try:
                (consumer / "time-checks.js").write_text(script, encoding="utf-8")
                (consumer / "Program.cs").write_text(
                    """using System;
using System.IO;
using Broiler.JavaScript.Engine;

using var context = new JSContext();
var result = context.Eval(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "time-checks.js"))).ToString();
Console.WriteLine($"pristine-consumer: the time-zone and calendar area's checks: {result}");
if (!result.StartsWith("ok ", StringComparison.Ordinal))
    throw new InvalidOperationException(result);
""", encoding="utf-8")
                project = (consumer / "Consumer.csproj").read_text(encoding="utf-8")
                project = project.replace("</Project>", """  <ItemGroup>
    <None Include="time-checks.js" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>""")
                (consumer / "Consumer.csproj").write_text(project, encoding="utf-8")
                log = [f"# the pristine consumer above, kept, with its Program.cs replaced by a run of these checks under "
                       f"TZ={CHECKS_ZONE} and no LANG, offline from the same isolated cache; each is a JavaScript "
                       "expression that must be true:\n"]
                log += [f"#   {row}: {expression}\n" for row, expression in TIME_CHECKS]
                env, self.env = self.env, dict(self.env, NUGET_PACKAGES=str(cache), TZ=CHECKS_ZONE)
                self.env.pop("LANG", None)
                try:
                    self.run(["dotnet", "run", "-c", "Release", "--no-restore"], consumer, log)
                finally:
                    self.env = env
                log.append("\n# the shared packages in the consumer's isolated cache: where each was restored from, "
                           "whether it carries nuget.org's signature, its content hash, and the content hash of the "
                           "copy nuget.org serves (the hash of the package without its signature):\n")
                for package_id, _ in SHARED_PACKAGES:
                    lowered = package_id.lower()
                    found = sorted((cache / lowered).glob("*/.nupkg.metadata")) if (cache / lowered).exists() else []
                    if not found:
                        log.append(f"{package_id}: not in the consumer's closure\n")
                        continue
                    for metadata in found:
                        version = metadata.parent.name
                        record = json.loads(metadata.read_text(encoding="utf-8"))
                        restored = metadata.parent / f"{lowered}.{version}.nupkg"
                        with zipfile.ZipFile(restored) as archive:
                            signed = ".signature.p7s" in archive.namelist()
                            ours = {n: hashlib.sha256(archive.read(n)).hexdigest() for n in archive.namelist()
                                    if n.startswith("lib/") and n.endswith(".dll")}
                        served = self.scratch / "nuget-org" / f"{lowered}.{version}.nupkg"
                        theirs = {}
                        if not served.exists():
                            url = f"https://api.nuget.org/v3-flatcontainer/{lowered}/{version}/{served.name}"
                            served.parent.mkdir(parents=True, exist_ok=True)
                            try:
                                with urllib.request.urlopen(url, timeout=60) as response:
                                    served.write_bytes(response.read())
                            except OSError as error:
                                log.append(f"  nuget.org: not fetched ({error})\n")
                        if served.exists():
                            verified = self.run(["dotnet", "nuget", "verify", "--all", str(served)], self.tree, [])
                            with zipfile.ZipFile(served) as archive:
                                theirs = {n: hashlib.sha256(archive.read(n)).hexdigest() for n in archive.namelist()
                                          if n.startswith("lib/") and n.endswith(".dll")}
                            served_hash = self.content_hash(verified.stdout)
                        else:
                            served_hash = "(not fetched)"
                        log.append(f"{package_id} {version}: from {record.get('source')}; signed: {signed}; content hash "
                                   f"{record.get('contentHash')}; nuget.org's copy's content hash {served_hash}\n")
                        for name in sorted(set(ours) | set(theirs)):
                            log.append(f"  {name}: restored {ours.get(name, 'absent')}; nuget.org's "
                                       f"{theirs.get(name, 'absent')}; equal: {ours.get(name) == theirs.get(name)}\n")
                self.log_to("engine/pristine-consumer-time.log", log)
            finally:
                shutil.rmtree(temporary, ignore_errors=True)

        # The control: the same checks in the shell built at the rule's commit and at the after commit, as a
        # script-host program that prints its completion value, with TZ=America/New_York and no LANG.
        program = self.scratch / "time-checks-control.js"
        program.write_text("print(" + script + ");\n", encoding="utf-8")
        lines = [f"# the pristine consumer's checks, run as one program (print(<the checks>)) in each shell with "
                 f"TZ={CHECKS_ZONE} and no LANG; at the rule's commit and at the after commit", ""]
        for label, tree in (("rule's commit", self.base_tree), ("after commit", self.tree)):
            if tree is None:
                lines.append(f"## {label}: no tree given")
                continue
            build: list[str] = []
            dll = tree / SHELL_DLL
            if not dll.exists():
                self.run(["dotnet", "build", SHELL_PROJECT, "-c", "Release", "--nologo"], tree, build)
            env, self.env = self.env, dict(self.env, TZ=CHECKS_ZONE)
            self.env.pop("LANG", None)
            try:
                result = self.run(["dotnet", str(dll), "--script-host", str(program)], self.scratch, [])
            finally:
                self.env = env
            lines += [f"## {label}: {self.git(tree, 'rev-parse', 'HEAD')}, shell BroilerJS.dll sha256 "
                      f"{self.sha256(dll) if dll.exists() else '(not built)'}; exit {result.returncode}",
                      result.stdout.strip(), (result.stderr.strip() and "# stderr\n" + result.stderr.strip()) or "", ""]
        self.write("engine/pristine-consumer-checks-control.txt", "\n".join(lines) + "\n")

    # -- simplification --------------------------------------------------------------------------

    def simplification(self) -> None:
        after = self.after()
        lines = [f"# SJL-8.7 deletion check, Broiler.JS at {after}: every entry of the time-zone and calendar area's",
                 "# Broiler.JS deletion list (Broiler.VM docs/javascript-shared-libraries/milestones/",
                 "# sjl-8-time-zones-and-calendars.md, SJL-8.6, as JSD-0076's X6 reads and extends it), searched in the",
                 "# file or folder that held it with git grep -E, test projects included where the folder holds them,",
                 "# each pattern naming the members the entry held at the rule's commit. An entry with no line is",
                 "# absent; a line that remains is a call to the library, an adapter, a comment or another member of",
                 "# the same name, which the bundle's README reads. The files that held whole entries are listed too.",
                 ""]
        lines.append("## files at the after commit under the paths that held whole entries")
        for path in ("Broiler.JS/Broiler.JavaScript.BuiltIns/Temporal/Tz", "scripts/tzdata",
                     TEMPORAL + "/TemporalLunisolarCalendar.cs", TEMPORAL + "/TemporalSolarHijriCalendar.cs"):
            listed = self.git(self.tree, "ls-files", "--", path).splitlines()
            lines.append(f"# {path}: {', '.join(listed) if listed else 'no file'}")
        tzdata = self.git(self.tree, "ls-files", "--", "*tzdata.bin").splitlines()
        lines += [f"# any tzdata.bin in the tree: {', '.join(tzdata) if tzdata else 'none'}", ""]
        for entry, paths, pattern in DELETED:
            hits = self.grep("-E", pattern, paths=["--", *paths])
            lines += [f"## {entry}", f"# in {', '.join(paths)}: {pattern}", f"# hits: {len(hits)}"]
            lines += [h[:240] for h in hits] + [""]
        self.write("simplification/deletion-check.log", "\n".join(lines) + "\n")

        search = [f"# SJL-8.7 search for a host zone or a .NET calendar (clause 8), Broiler.JS at {after}: Temporal",
                  "# (Broiler.JS/Broiler.JavaScript.BuiltIns/Temporal), then the rest of the engine's product sources",
                  "# (Broiler.JS/; test projects, benchmarks and samples excluded), with git grep -E. Each hit is",
                  "# reviewed in the bundle's README.", ""]
        for title, paths, pattern in HOST_SEARCHES:
            hits = self.grep("-E", pattern, paths=[*unicode.PRODUCT[:1], *paths, *unicode.PRODUCT[2:]]
                             if paths != ["Broiler.JS"] else None)
            search += [f"## {title}", f"# in {', '.join(paths)}: {pattern}", f"# hits: {len(hits)}"]
            if "a summary per file" in title:
                members: dict[str, set[str]] = {}
                for hit in hits:
                    members.setdefault(hit.split(":", 1)[0], set()).update(
                        m.group(0) for m in re.finditer(pattern, hit.split(":", 2)[2]))
                search += [f"{file}: {', '.join(sorted(names))}" for file, names in sorted(members.items())] + [""]
                continue
            search += [h[:240] for h in hits] + [""]
        self.write("simplification/host-search.log", "\n".join(search) + "\n")

        search = [f"# SJL-8.7 search for a surviving duplicate of the time-zone and calendar area outside the shared",
                  f"# library, Broiler.JS at {after}: the engine's product sources (Broiler.JS/; test projects,",
                  "# benchmarks and samples excluded), with git grep -E, in three parts. Each hit is reviewed in the",
                  "# bundle's README.", ""]
        for title, pattern in DUPLICATE_SEARCHES:
            hits = self.grep("-E", pattern) if not pattern.startswith("(?i)") else \
                self.grep("-i", "-E", pattern[4:])
            search += [f"## {title}", f"# pattern: {pattern}", f"# hits: {len(hits)}"]
            search += [h[:240] for h in hits] + [""]
        self.write("simplification/duplicate-search.log", "\n".join(search) + "\n")

    # -- manifest --------------------------------------------------------------------------------

    def manifest(self) -> None:
        files = {}
        for path in sorted(self.out.rglob("*")):
            if path.is_file() and path.name != "manifest.json" and "__pycache__" not in path.parts:
                files[str(path.relative_to(self.out)).replace(os.sep, "/")] = self.sha256(path)
        manifest = {
            "bundle": "sjl-8-001",
            "repository": "Broiler.JS",
            "baseCommit": BASE_COMMIT,
            "afterCommit": self.after(),
            "cutOverCommit": CUT_OVER,
            "z16Commit": Z16_COMMIT,
            "observedCommit": OBSERVED_COMMIT,
            "ruleCommit": self.rule_commit(),
            "sharedVersion": SHARED_VERSION,
            "sharedPublishedFrom": PUBLISHED_FROM,
            "suiteRevision": base.SUITE_REVISION,
            "files": files,
        }
        (self.out / "manifest.json").write_text(json.dumps(manifest, indent=1, sort_keys=True) + "\n",
                                                encoding="utf-8", newline="\n")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--tree", required=True, help="a clean checkout of the after commit")
    parser.add_argument("--vm-tree", help="home: a checkout of Broiler.VM, whose pins the home's are compared with")
    parser.add_argument("--code-tree", help="home: a checkout of Broiler.Code with its review CLI built in Release")
    parser.add_argument("--base-tree", help="consumer: a clean checkout of the rule's commit, for the control")
    parser.add_argument("--part", choices=sorted(PARTS), help="test262 and compare: the part")
    parser.add_argument("--artifact-zip", help="test262: the part's after run's test262-evidence artifact, as downloaded")
    parser.add_argument("--run-url", help="test262: the after run's workflow run, with its attempt")
    parser.add_argument("--artifact-id", help="test262: the artifact's id")
    parser.add_argument("--run-times", help="test262: when the after run was dispatched and completed")
    parser.add_argument("--out", required=True)
    parser.add_argument("--scratch", default=os.path.join(os.environ.get("TMPDIR", "/tmp"), "sjl-8-001-js"))
    parser.add_argument("--only", nargs="+", choices=STEPS, required=True)
    args = parser.parse_args(argv)
    args.label = "after"
    args.owner_tree = None
    args.bench_tree = None
    args.lane = None
    args.lane_record = None
    args.dry_run = False
    args.suite_archive = None
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
