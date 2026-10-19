#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Collect Broiler.JS's evidence for milestone SJL-10 (work package SJL-10.6) into bundle sjl-10-001.

    python3 docs/evidence/sjl-10-001/collect.py --tree <a clean checkout of the after commit> \
        [--vm-tree <a checkout of Broiler.VM>] [--code-tree <a checkout of Broiler.Code with its CLI built>] \
        [--base-tree <a clean checkout of the rule's commit>] \
        [--part A|B --artifact-zip <that part's after run's test262-evidence artifact> --run-url <its workflow run> \
         --artifact-id <its id> --run-times <when it was dispatched and completed>] \
        --out docs/evidence/sjl-10-001 --only STEP [STEP ...]

THE RULE IS decision-rule.md BESIDE THIS FILE, committed at SJL-10.1 in 1d27064d before any code of SJL-10. Parts A
and B take the whole pinned suite, negative-metadata tests included, through .github/workflows/test262-evidence.yml,
part A in the runner's own zone, UTC, and part B with time_zone America/New_York. Both bases were taken at the
commit that carries the rule and imported by hand into test262/base/ and test262/base-new-york/, each with its
imported.txt; this collector imports the after runs the same way, into test262/after/ and test262/after-new-york/,
and compares each part's after run with its own base through scripts/compliance/compare_test262_paths.py as the
rule fixes it, with the part's classifications.json beside its report when one is written. The two parts' runs are
never compared with each other. Part C, the reference datasets, is taken by its own harness,
scripts/conformance/run_reference_datasets.py --bundle sjl-10-001, as its base was and as bundle sjl-8-001's after
run was, by hand (datasets/README.md): this collector has no step for it, as sjl-8-001's had none.

THE AFTER ARM IS A CHECKOUT, NOT THIS TREE. Every local build and run is taken at `--tree`, a clean checkout of
the after commit, 00b65dd0, the merge into the default branch (Broiler-Platform/Broiler.JS#1054) that lands
SJL-10.5's cut-over. Each project is built once there, in Release, by the step that first needs it, and every run
after its build is --no-build. The identity step lists every file under the product paths that differs between the
cut-over's last commit and the after commit.

IT REUSES BUNDLE SJL-8-001'S COLLECTOR, and through it sjl-7-001's, sjl-6-001's, sjl-5-001's, sjl-4-001's,
sjl-3-001's and sjl-0-001's, for its helpers, the engine-level vectors in both zones, the import of a workflow's
run, the comparison and its checks, the completeness record and the package record against nuget.org. What it adds
is SJL-10's: the home clauses at the after commit (every sub-area implemented, its vectors passing and the polyfill
dataset at library level, each held by the tests SJL-10.3 committed), the library's code against the code the
shared stream's 0.1.0-preview.6 was published from, a pristine consumer of the Temporal area's corrections with its
control at the rule's commit and a second run over the library as nuget.org publishes it, the deletion list of the
correspondence's section 11 entry by entry, and the name search for every deleted file and type with the lines each
C# file gained and lost.

THE COLLECTOR JUDGES NOTHING. Each step writes what it ran and what it printed, failures included; the README
reads them under the rule.

Steps, each with `--only`:

  identity        the after commit and its cleanliness, the cut-over's commits that change product sources and
                  the product files that differ between its last commit and the after commit, the rule commit, its
                  ancestry and the files it fixes unchanged since, part C's harness and copies, the library's code
                  against the code 0.1.0-preview.6 was published from, the SDK, Node and the machine, into
                  identity.txt;
  home            the home clauses at the after commit: the shared folder built in Release and its tests run, the
                  tests SJL-10.3 committed for each clause counted from that run, the library's Temporal files and
                  what differs from the published code, the assurance check, the packages nuget.org serves at
                  0.1.0-preview.6 with a search of every entry for the tables' bytes, and the shared set's pristine
                  consumer, into home/after/;
  area            the area's tests in this engine, the changelog's SJL-10.5 tests, built in Release and run under
                  TZ=UTC, into home/after/engine-*.log;
  probe           the Native AOT probe over the corpus, published for linux-x64 and run, its report against the
                  JIT's (the JIT's build being the home step's), into home/after/probe-*;
  vectors         the engine-level vectors from one Release build, under TZ=UTC and TZ=America/New_York at
                  once, into engine/after-revision-<n>-utc.* and engine/after-revision-<n>-new-york.*;
  test262         with --part, that part's after run imported into test262/after/ (A) or test262/after-new-york/
                  (B), unchanged, with imported.txt;
  compare         with --part, the part's two runs' completeness and host-zone records, the comparison with the
                  part's base, with the part's classifications.json when it exists, and the script's checks,
                  into test262/comparison-utc/ or test262/comparison-new-york/; it may be run again, the import
                  may not;
  consumer        the engine set packed and restored by a pristine consumer, which then runs the area's checks
                  from the same isolated cache under TZ=America/New_York; the same consumer restored again with
                  the shared stream's packages taken from nuget.org alone, as published, and the checks run; and
                  the same checks in the shell built at the rule's commit and at the after commit, the control,
                  into engine/;
  simplification  the correspondence's section 11 deletion list entry by entry, the name search for every
                  deleted file and type (and the cut-over's own name search repeated), and the lines each C# file
                  gained and lost since the rule's commit, into simplification/;
  manifest        manifest.json: the SHA-256 of every file the bundle retains.

Paths in retained logs are written as <tree>, <base>, <vm>, <code>, <repo>, <run>, <scratch> and <tmp>.
"""

from __future__ import annotations

import argparse
import hashlib
import html
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
from xml.etree import ElementTree

HERE = Path(__file__).resolve().parent
TIME_BUNDLE = HERE.parent / "sjl-8-001"

_spec = importlib.util.spec_from_file_location("sjl_8_001_collect", TIME_BUNDLE / "collect.py")
time = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(time)
unicode = time.unicode  # sjl-7-001's collector: the package record and the product paths
prior = time.prior  # sjl-3-001's: utc()
base = time.base  # sjl-0-001's: the suite revision

RULE = "docs/evidence/sjl-10-001/decision-rule.md"
# What the rule fixes with itself: it, the comparison script, the workflow parts A and B dispatch, and the scripts
# the workflow runs and merges with.
RULE_FILES = (RULE, "scripts/compliance/compare_test262_paths.py", ".github/workflows/test262-evidence.yml",
              "scripts/compliance/run_test262.py", "scripts/compliance/merge_test262_shards.py")
# Part C's harness and population, extended to the second program at SJL-10.2 after the rule, as the rule asks.
DATASET_FILES = ("scripts/conformance/run_reference_datasets.py", "scripts/conformance/reference_dataset_driver.js",
                 "conformance/datasets/time")
DATASETS_COMMIT = "80dff79f80a981e92175dd0cd2c0916b13ad606b"
# The rule's commit, which carries the three parts' bases.
BASE_COMMIT = "1d27064d0a1f57608a7af20e37eac91bd8887baa"
# The merge that lands SJL-10.5's cut-over (Broiler-Platform/Broiler.JS#1054), which both after runs name.
AFTER_COMMIT = "00b65dd055a277859d5325ead6338a040bcf2f30"
# SJL-10.5's cut-over, its first and last commits that change product sources, from main at CUT_OVER_FROM.
CUT_OVER_FROM = "1c411b68c7166e68c29ea90abd79b06dcfedcab3"
CUT_OVER_FIRST = "f42eac6b01eeee69023eba6ddb9ed0ef401b2c7b"
CUT_OVER = "6dbff8058713c5724ed82ee6173c19b53392e59b"
# Decision D12's library change, after the shared stream's 0.1.0-preview.6 was published.
D12_COMMIT = "163d43aabebfbee46bf700c3a294f5a131b1b8ce"
PRODUCT_PATHS = time.PRODUCT_PATHS
# The shared stream's version this engine depends on, published to nuget.org from PUBLISHED_FROM, whose library
# code is LIBRARY_CODE's.
SHARED_VERSION = "0.1.0-preview.6"
PUBLISHED_FROM = "fc829f8a507295185527e4175e70d74e15630fd1"
LIBRARY_CODE = "bf81d1b7b468d8235b672ed7dfc0327c26a4843c"
SHARED_PACKAGES = (("Broiler.ECMAScript.Algorithms", SHARED_VERSION), ("Broiler.ECMAScript.Generation", SHARED_VERSION))
LIBRARY_PATHS = time.LIBRARY_PATHS
LIBRARY_TEMPORAL = "Broiler.ECMAScript/Broiler.ECMAScript.Algorithms/Temporal"
# SJL-10.3's last commit, at which the bundle's section 3 read the library.
LIBRARY_READ_AT = "e33b53e6bee924b417c13857ab383cc192e2df6d"
HOME = time.HOME
VECTORS_TESTS = time.VECTORS_TESTS
SHELL_PROJECT = time.SHELL_PROJECT
SHELL_DLL = time.SHELL_DLL
PROBE = time.PROBE
PARTS = time.PARTS
STEPS = ("identity", "home", "area", "probe", "vectors", "test262", "compare", "consumer", "simplification",
         "manifest")
# The home clauses (the milestone's exit gate, and the rule's "What this file does not decide"), each with the tests
# SJL-10.3 committed that hold it, by project and class (or class and method).
ALGORITHMS_TESTS = "Broiler.ECMAScript.Algorithms.Tests."
CONFORMANCE_TESTS = "Broiler.ECMAScript.Conformance.Tests."
HOME_CLAUSES = (
    ("every sub-area implemented: records and limits", (ALGORITHMS_TESTS + "TemporalLimitsTests",)),
    ("every sub-area implemented: balancing", (ALGORITHMS_TESTS + "TemporalBalancingTests",)),
    ("every sub-area implemented: durations and rounding, and differences",
     (ALGORITHMS_TESTS + "DurationApiTests", ALGORITHMS_TESTS + "DurationCorrespondenceTests")),
    ("every sub-area implemented: the grammar", (ALGORITHMS_TESTS + "TemporalGrammarTests",)),
    ("every sub-area implemented: calendars",
     (ALGORITHMS_TESTS + "CalendarApiTests", ALGORITHMS_TESTS + "CalendarCorrespondenceTests")),
    ("every sub-area implemented: serialization", (ALGORITHMS_TESTS + "TemporalFormatTests",)),
    ("its vectors passing (the library-level runner over the Temporal area, and its control against the empty "
     "library)",
     (CONFORMANCE_TESTS + "LibraryRunTests.Every_Temporal_vector_passes_against_the_algorithms_library",
      CONFORMANCE_TESTS + "LibraryRunTests.Against_the_empty_library_every_vector_is_not_implemented")),
    ("the polyfill dataset at library level",
     (ALGORITHMS_TESTS + "TemporalDatasetTests", ALGORITHMS_TESTS + "CalendarDatasetTests")),
)
# The area's tests in this engine, by project, as the changelog's SJL-10.5 entry names them: the corrections of the
# durations and differences with decision D11, the adapter's fixes, and the era tests row C9 changed.
AREA_TESTS = {
    "Broiler.JS/Broiler.JavaScript.Integration.Tests": ("SharedTemporalDurationsTests", "SharedTemporalAdapterTests",
                                                        "Issue781Tests", "Issue777Tests"),
}

# The Broiler.JS deletion list: Broiler.VM docs/evidence/sjl-10-001/correspondence.md, section 11, which JSD-0078's
# X6 states, with decision D3's calendar copies, entry by entry: what was deleted, the folder that held it, and what
# is searched there. Each member's name is the one it had at the rule's commit, where the correspondence read the
# engine (d3464413, whose Broiler.JS sources are 1d27064d's). A call to the library, an adapter or a comment shows its
# line.
JS = "Broiler.JS/Broiler.JavaScript.BuiltIns/"
TEMPORAL = JS + "Temporal"
DELETED = (
    ("validity and limits: the per-type limit checks, DurationSign and RejectDuration, the default largest unit, "
     "HasCalendarUnits and the relative range checks", [TEMPORAL],
     r"\b(WithinLimits|IsWithinIsoDateLimits|IsoYearMonthWithinLimits|IsValidISODateTime|RejectDuration|"
     r"DurationSign|DefaultLargestUnitRank|DefaultLargestUnit|HasCalendarUnits|RejectRelativeDateTimeRange|"
     r"ValidateRelativeEndpoints)\b"),
    ("regulation: RegulateISODate, RegulateDateTime, RegulateTime and RegulateYearMonth (RegulateMonthDay the "
     "calendar half's)", [TEMPORAL],
     r"\b(RegulateISODate|RegulateDateTime|RegulateTime|RegulateYearMonth|RegulateMonthDay)\b"),
    ("addition: AddISODate and its copies, AddYearsMonths, each AddDuration's arithmetic, the time-duration "
     "nanoseconds, and AddSubtract with its \"not yet implemented\" Error", [TEMPORAL],
     r"\b(AddISODate|AddIsoDate|AddYearsMonths|AddCalendarYearsMonths|TimePlusDaysNanoseconds|TimeComponentsNs|"
     r"FromTimeNanoseconds|AddSubtract|AddToIso)\b|not yet implemented"),
    ("balancing: BalanceTimeOnly, BalanceFromNanoseconds, BuildDuration and the per-type balancing", [TEMPORAL],
     r"\b(BalanceTimeOnly|BalanceFromNanoseconds|BuildDuration|BalanceTime|BalanceISODateTime|BalanceDateTime)\b"),
    ("the small copies: CompareISODate, FloorDiv and the unit-length tables", [TEMPORAL],
     r"\b(CompareISODate|FloorDiv|FloorDivBig|UnitNanoseconds|TimeUnitNanoseconds|TimeUnitNs|NsPerDay|NsPerHour)\b"),
    ("the separate relative-rounding implementations (plain-date, zoned, the day-axis nudge) and compare's "
     "arithmetic", [TEMPORAL],
     r"\b(RoundImpl|TotalImpl|RoundDurationRelative|TotalDurationRelative|TotalRelative|RelativeEndpoints|"
     r"NudgeCalendarUnit|CalendarNudgeWindow|ComputeNudgeWindow|BubbleDateDuration|NudgeResult|NudgeWindow|"
     r"InternalDuration|RoundTimeDifference|RoundDateDifference|RoundDateTimeDateDifference|AddDurationEpochNs)\b"),
    ("JSTemporalRoundingOptions' arithmetic and rules: NudgeTotal, NudgeRoundsToEnd, the unit order, normalisation, "
     "maximum increment, increment validation and Pow10", [TEMPORAL],
     r"\b(NudgeTotal|NudgeRoundsToEnd|UnitOrder|UnitIndex|NormalizeUnit|NormalizeTimeUnit|NormalizeAnyUnit|MaxUnit|"
     r"MaximumRoundingIncrement|ValidateRoundingIncrement|Pow10)\b"),
    ("the differences and the settings' rules: each type's difference arithmetic, ReadDifferenceSettings and its "
     "copies", [TEMPORAL],
     r"\b(DifferenceISODate|DifferenceISODateRounded|DifferenceTimeOnly|DifferenceCalendar|DifferenceInternal|"
     r"NonIsoDateDifference|ReadDifferenceSettings|ReadZonedDifferenceSettings|DateDurationSign|AddDateGetDays|"
     r"ISODateSurpasses|CompareSurpasses|CalendarDateUntil)\b"),
    ("the dead code: NotImplementedArithmetic and ReadLargestUnit", [TEMPORAL],
     r"\b(NotImplementedArithmetic|ReadLargestUnit)\b"),
    ("the per-type regular-expression parsers with their patterns, and CanonicalizeTimeZone's string fallback",
     [TEMPORAL],
     r"\bRegex\b|RegularExpressions|\b(DatePattern|CalendarAnnotation|DateTimePattern|StrictTimeCore|"
     r"OffsetDesignator|DateTimeFormPattern|BareTimeFormPattern|TrailingAnnotationsPattern|NumericUtcOffsetPattern|"
     r"ValidateNumericUtcOffset|IsAmbiguousTimeString|YearMonthPattern|MonthDayPattern|FullDatePattern|"
     r"ResolveCalendarId|OffsetValuePattern|ZonedCorePattern|ZonedTrailingAnnotation|InstantPattern|DurationPattern|"
     r"FractionToNanoseconds)\b"),
    ("the per-type formatters, the toString bodies' precision arithmetic and the five ReadCalendarName copies",
     [TEMPORAL],
     r"\b(FormatCalendarAnnotation|FormatISOString|FormatTime|FormatOffset|FormatOffsetRounded|FormatToString|"
     r"FormatISO|AppendFraction|ReadCalendarName|GetFractionalSecondDigits)\b"),
    ("TemporalIsoString, whole: its helpers, its coercing members (adapter code) and the dead "
     "RejectIncompatibleStyle", ["Broiler.JS"],
     r"\bTemporalIsoString\b|\b(RejectTimeTailForCalendarOnly|RequireWellFormedMonthCode|"
     r"RejectMultipleCalendarAnnotations|RejectMalformedAnnotations|RejectInvalidAnnotations|"
     r"MinutePrecisionOffsetName|TryExtractCalendar|TryExtractTimeZone|TryParseRelative|IsStrictOffset|"
     r"StrictOffsetPattern|RejectIncompatibleStyle|RequireMonthCodeString|RequireOffsetString|RequireStringField)\b"),
    ("TemporalNonIso's calendar parts (its coercions and property reads adapter code)", ["Broiler.JS"],
     r"\bTemporalNonIso\b|\b(CalendarYmd|ToIsoFromBag|WithToIso|YearMonthToIso|MonthDaySearchYears|MonthDayOrdinal|"
     r"MonthDayExact|MonthDayMaxLength|MonthDayFromCode|MonthDayFromYearMonth|MonthDayFromBag|MonthDayCore|"
     r"MonthDayFields|MonthDayToPlainDate|ResolveYearMonth|RegulateToIso|ResolveMonthAfterYearShift|"
     r"AddCalendarMonths|ParseMonthCode)\b"),
    ("decision D3's calendar copies: TemporalArithmeticCalendar, TemporalTableCalendar's arithmetic, "
     "TemporalCalendarMath, the Gregorian family's identifiers, years and eras in JSTemporalCalendar.cs",
     ["Broiler.JS"],
     r"\b(TemporalArithmeticCalendar|TemporalTableCalendar|TemporalCalendarMath|JSTemporalCalendar|"
     r"TemporalCalendar|JapaneseEra|ToSlotValue|ResolveCalendarIdentifierArgument|TryCanonicalizeName|"
     r"CanonicalizeIdentifierOrIsoString)\b"),
    ("decision D3's per-type month-code copies: MonthFromCode, ValidateMonthCodeSyntax, MonthCodeSyntaxPattern, "
     "MonthFromCodeIso and ParseMonthCodeLeap", [TEMPORAL],
     r"\b(MonthFromCode|ValidateMonthCodeSyntax|MonthCodeSyntaxPattern|MonthFromCodeIso|ParseMonthCodeLeap)\b"),
)
# The searched trees of the name search: every tracked file of the engine and of the shared folder, tests and the
# changelog included.
NAME_SEARCH_PATHS = ["Broiler.JS", "Broiler.ECMAScript"]

# The pristine consumer's checks of the area: each a correction or a policy change of SJL-10.5, as the changelog
# writes it, run from the packages restored into the consumer's isolated cache, with TZ=America/New_York. Each is a
# JavaScript expression that must be true.
_A = "Temporal.ZonedDateTime.from('2011-12-31T16:15+14:00[Pacific/Apia]')"
_B = "Temporal.ZonedDateTime.from('2011-12-31T00:00+14:00[Pacific/Apia]')"


def _throws(expression: str, error: str = "RangeError") -> str:
    return f"(() => {{ try {{ {expression}; return false; }} catch (e) {{ return e instanceof {error}; }} }})()"


TEMPORAL_CHECKS = (
    ("R7", _throws("Temporal.PlainDateTime.from('2020-06-15T12:00').add({ years: 4294967295 })")),
    ("B5", _throws("Temporal.PlainYearMonth.from('-271821-05').subtract({ months: 1 })")),
    ("D3", "Temporal.Duration.from({ hours: 1 }).add({ hours: 1 }, 5).toString() === 'PT2H' && "
           + _throws("Temporal.Duration.from({ years: 1 }).add({ months: 1 }, { relativeTo: '2020-01-01' })")),
    ("D4", "Temporal.Duration.compare({ years: 1, seconds: 9e15 }, { years: 1 }, "
           "{ relativeTo: new Temporal.PlainDate(2020, 1, 1) }) === 1"),
    ("D6", "Temporal.Duration.from('P1Y2M3W4DT5H6M7.008009010S').round({ smallestUnit: 'month', roundingMode: "
           "'ceil', relativeTo: new Temporal.PlainDate(2020, 1, 31) }).toString() === 'P1Y3M'"),
    ("D7", "Temporal.Duration.from({ months: 10, days: 29 }).round({ smallestUnit: 'month', largestUnit: 'year', "
           "roundingMode: 'ceil', relativeTo: new Temporal.PlainDate(2019, 3, 31) }).toString() === 'P11M'"),
    ("D8", "Temporal.Duration.from({ months: 10, days: 29, hours: 12 }).round({ smallestUnit: 'month', largestUnit: "
           "'year', roundingMode: 'trunc', relativeTo: Temporal.ZonedDateTime.from('2019-03-31T00:00[UTC]') })"
           ".toString() === 'P11M'"),
    ("D11", "Temporal.Duration.from('P1M').round({ smallestUnit: 'day', relativeTo: '2020-01-01[u-ca=hebrew]' })"
            ".toString() === 'P1M'"),
    ("F4", "Temporal.PlainDate.from('2020-01-01[u-ca=hebrew]').until(Temporal.PlainDate.from('2020-07-20[u-ca=hebrew]'), "
           "{ smallestUnit: 'month', roundingMode: 'halfExpand' }).toString() === 'P7M'"),
    ("F7", "Temporal.ZonedDateTime.from('2026-03-07T12:00[America/New_York]').until("
           "Temporal.ZonedDateTime.from('2026-03-07T23:45[America/New_York]'), { smallestUnit: 'day', roundingMode: "
           "'halfExpand' }).toString() === 'P1D'"),
    ("G9", _throws("Temporal.PlainDate.from('\\uFF12\\uFF10\\uFF12\\uFF10-01-01')")),
    ("G22", _throws("Temporal.Duration.from('P1YT')")),
    ("C2", _throws("Temporal.PlainDate.from('2020-01-01[u-ca=gregorian]')")),
    ("C5", _throws("Temporal.PlainDate.from({ calendar: 'coptic', year: 1739, monthCode: 'M00', day: 1 }, "
                   "{ overflow: 'constrain' })")),
    ("C6", _throws("Temporal.PlainDate.from({ calendar: 'hebrew', year: 5784, monthCode: 'M\\u0660\\u0661', day: 1 })")),
    ("C9", _throws("Temporal.PlainDate.from({ calendar: 'coptic', era: 'coptic', eraYear: 5, monthCode: 'M01', "
                   "day: 1 })")),
    ("C12", "Temporal.PlainMonthDay.from({ year: 2147483652, month: 2, day: 29 }, { overflow: 'reject' })"
            ".toString() === '02-29'"),
    ("C13", "Temporal.PlainDate.from({ calendar: Temporal.PlainMonthDay.from({ calendar: 'hebrew', monthCode: 'M05', "
            "day: 1 }), year: 5784, monthCode: 'M05', day: 1 }).calendarId === 'hebrew'"),
    ("C15", "Temporal.PlainDate.from('2023-03-15').withCalendar('buddhist').toPlainYearMonth().toString() === "
            "'2023-03-01[u-ca=buddhist]'"),
    ("C16", "Temporal.PlainDate.from('2020-05-01').withCalendar('gregory').toPlainMonthDay().calendarId === 'gregory'"),
    ("C17", "Temporal.PlainMonthDay.from({ calendar: 'buddhist', monthCode: 'M02', day: 10 }).toPlainDate("
            "{ year: 2544 }).withCalendar('iso8601').toString() === '2001-02-10'"),
    ("points", "Temporal.PlainTime.from('01:10').round({ smallestUnit: 'minute', roundingIncrement: 20, "
               "roundingMode: 'halfEven' }).toString() === '01:00:00'"),
    ("decision D11", _throws(f"{_A}.since({_B}, {{ largestUnit: 'day', smallestUnit: 'day' }})")),
    ("decision D12", _throws(f"{_A}.since({_B}, {{ largestUnit: 'day', smallestUnit: 'hour' }})")),
)
CHECKS_ZONE = time.CHECKS_ZONE


def checks_script() -> str:
    """The checks as one program whose completion value is `ok <n>` or `failed <rows>`."""
    checks = ",\n".join(f"  [{json.dumps(row)}, () => {expression}]" for row, expression in TEMPORAL_CHECKS)
    return ("(() => {\n const checks = [\n" + checks + "\n ];\n"
            " const failed = [];\n"
            " for (const c of checks) { let r; try { r = c[1](); } catch (e) { r = e.name + ': ' + e.message; }"
            " if (r !== true) failed.push(c[0] + ' (' + String(r) + ')'); }\n"
            " return failed.length === 0 ? 'ok ' + checks.length : 'failed ' + failed.join(', ');\n})()")


CONSUMER_PROGRAM = """using System;
using System.IO;
using Broiler.JavaScript.Engine;

using var context = new JSContext();
var result = context.Eval(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "temporal-checks.js"))).ToString();
Console.WriteLine($"pristine-consumer: the Temporal area's checks: {result}");
if (!result.StartsWith("ok ", StringComparison.Ordinal))
    throw new InvalidOperationException(result);
"""


class Collector(time.Collector):
    def __init__(self, args: argparse.Namespace) -> None:
        super().__init__(args)
        self.base_commit = BASE_COMMIT

    def rule_commit(self) -> str:
        return self.git(self.tree, "log", "--diff-filter=A", "--format=%H", "--", RULE).splitlines()[-1]

    # -- identity --------------------------------------------------------------------------------

    def identity(self) -> None:
        after = self.after()
        rule = self.rule_commit()
        lines = [
            "bundle: sjl-10-001 (Broiler.JS), its after arm",
            f"after commit: {after}",
            f"after commit is the one parts A's and B's after runs name ({AFTER_COMMIT}): {after == AFTER_COMMIT}",
            f"after tree clean: {self.git(self.tree, 'status', '--porcelain', '--untracked-files=all') == ''}",
            f"cut-over: SJL-10.5, {CUT_OVER_FIRST[:8]} to {CUT_OVER[:8]}, on a branch from main at "
            f"{CUT_OVER_FROM[:8]}; its commits that change product sources:",
        ]
        for line in self.git(self.tree, "log", "--reverse", "--no-merges", "--format=%H %s",
                             f"{CUT_OVER_FROM}..{CUT_OVER}", "--", *PRODUCT_PATHS).splitlines():
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
        lines.append(f"files under Broiler.JS/ that differ between the rule commit and the cut-over's branch point "
                     f"({CUT_OVER_FROM[:8]}): ")
        moved = self.git(self.tree, "diff", "--name-only", BASE_COMMIT, CUT_OVER_FROM, "--", "Broiler.JS").splitlines()
        lines[-1] += ", ".join(moved) if moved else "none"
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
        library = self.git(self.tree, "diff", "--stat=200", LIBRARY_CODE, after, "--", *LIBRARY_PATHS).splitlines()
        lines += [f"  {p}" for p in library] or ["  none"]
        lines.append("  the commits that change them: " + ("; ".join(self.git(
            self.tree, "log", "--reverse", "--no-merges", "--format=%h %s", f"{LIBRARY_CODE}..{after}", "--",
            *LIBRARY_PATHS).splitlines()) or "none"))
        lines.append(f"collector: docs/evidence/sjl-10-001/collect.py, sha256 {self.sha256(Path(__file__))}, run from "
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
        trx = self.scratch / "trx"
        shutil.rmtree(trx, ignore_errors=True)
        tests = self.run(["dotnet", "test", "Broiler.ECMAScript.slnf", "-c", "Release", "--no-build", "--nologo",
                          "--logger", "console;verbosity=normal", "--logger", "trx;LogFilePrefix=home",
                          "--results-directory", str(trx)], self.tree, log)
        self.log_to(f"{out}/tests.log", log)
        if self.code is not None:
            log = []
            cli = self.code / "src" / "Broiler.Code.Review.Cli" / "bin" / "Release" / "net10.0" / "Broiler.Code.Review.Cli.dll"
            log.append(f"# Broiler.Code at {self.git(self.code, 'rev-parse', 'HEAD')}, its CLI as built there\n")
            self.run(["dotnet", str(cli), "assurance", "check", "--root", str(self.tree / "Broiler.ECMAScript")],
                     self.tree, log)
            self.log_to(f"{out}/assurance.log", log)

        # The home clauses, each with the tests SJL-10.3 committed that hold it, counted from the run above: from the
        # TRX file each test assembly wrote beside the console log, whose lines the four assemblies interleave.
        results: dict[str, dict[str, int]] = {}
        verdicts = {"Passed": "Passed", "Failed": "Failed", "NotExecuted": "Skipped"}
        reports = sorted(trx.glob("*.trx"))
        for report in reports:
            for element in ElementTree.parse(report).getroot().iter():
                if element.tag.endswith("}UnitTestResult"):
                    name = re.match(r"[\w.]+", element.get("testName", ""))
                    verdict = verdicts.get(element.get("outcome", ""), element.get("outcome", "?"))
                    if name:
                        results.setdefault(name.group(0), {}).setdefault(verdict, 0)
                        results[name.group(0)][verdict] += 1
        lines = [f"# SJL-10.6: the home clauses at {after}, each with the tests SJL-10.3 committed that hold it, counted",
                 f"# from the run in home/after/tests.log, by the {len(reports)} TRX reports it wrote (one per test assembly;",
                 "# the console interleaves their lines). A test with several data rows counts once per row.", ""]
        for clause, holders in HOME_CLAUSES:
            lines.append(f"## {clause}")
            for holder in holders:
                counts: dict[str, int] = {}
                for name, verdicts in results.items():
                    if name == holder or name.startswith(holder + "."):
                        for verdict, count in verdicts.items():
                            counts[verdict] = counts.get(verdict, 0) + count
                lines.append(f"  {holder}: passed {counts.get('Passed', 0)}, failed {counts.get('Failed', 0)}, "
                             f"skipped {counts.get('Skipped', 0)}")
            lines.append("")
        totals = {"Passed": 0, "Failed": 0, "Skipped": 0}
        for verdicts in results.values():
            for verdict, count in verdicts.items():
                totals[verdict] += count
        lines.append(f"## the whole run: passed {totals['Passed']}, failed {totals['Failed']}, skipped "
                     f"{totals['Skipped']}; exit {tests.returncode}")
        for project in ("Broiler.ECMAScript.Algorithms.Tests", "Broiler.ECMAScript.Architecture.Tests",
                        "Broiler.ECMAScript.Conformance.Tests", "Broiler.ECMAScript.Generation.Tests"):
            passed = sum(v.get("Passed", 0) for n, v in results.items() if n.startswith(project + "."))
            failed = sum(v.get("Failed", 0) for n, v in results.items() if n.startswith(project + "."))
            lines.append(f"  {project}: passed {passed}, failed {failed}")
        self.write(f"{out}/clauses.log", "\n".join(lines) + "\n")

        # The library's Temporal code: what changed since the bundle's section 3 read it, against the code the
        # shared stream's published version holds, and each file by git blob.
        lines = [f"# SJL-10.6: the library's Temporal core ({LIBRARY_TEMPORAL}/) at {after}", "",
                 f"## files of the library and the generator changed between {LIBRARY_READ_AT[:8]} (SJL-10.3's last "
                 "commit, where section 3 read the library) and the after commit"]
        changed = self.git(self.tree, "diff", "--stat=200", LIBRARY_READ_AT, after, "--", *LIBRARY_PATHS).splitlines()
        lines += [f"  {l}" for l in changed] or ["  none"]
        lines += ["", f"## files of the library and the generator changed between {LIBRARY_CODE[:8]} (the code "
                      f"{SHARED_VERSION} was published from, at {PUBLISHED_FROM[:8]}) and the after commit"]
        changed = self.git(self.tree, "diff", "--stat=200", LIBRARY_CODE, after, "--", *LIBRARY_PATHS).splitlines()
        lines += [f"  {l}" for l in changed] or ["  none"]
        lines += ["", "## the code of those files, as it differs (documentation left out)"]
        diff = self.git(self.tree, "diff", LIBRARY_CODE, after, "--", *(p + "/*.cs" for p in LIBRARY_PATHS),
                        *(p + "/**/*.cs" for p in LIBRARY_PATHS))
        lines += [f"  {l}" for l in diff.splitlines()] or ["  none"]
        lines += ["", "## every file of the Temporal core, by git blob"]
        lines += [f"  {l.split()[2]}  {l.split(chr(9), 1)[1]}" for l in
                  self.git(self.tree, "ls-tree", "-r", after, "--", LIBRARY_TEMPORAL + "/").splitlines()]
        lines += ["", "## the Temporal core's public API in the library's baseline (PublicAPI.txt), lines naming "
                      "Broiler.ECMAScript.Temporal"]
        api = self.grep("-c", "Broiler.ECMAScript.Temporal",
                        paths=["--", "Broiler.ECMAScript/Broiler.ECMAScript.Algorithms/PublicAPI.txt"])
        lines += [f"  {l}" for l in api] or ["  none"]
        self.write(f"{out}/library.log", "\n".join(lines) + "\n")

        # The packages nuget.org serves at the shared stream's version, each with a search of its entries for the
        # bytes of the generated tables and of the pinned archives (no package carries either, SJL-D-6 (c)).
        generated = self.tree / HOME / "generated"
        log = [f"# SJL-10.6: Broiler.ECMAScript's packages at {SHARED_VERSION} as nuget.org serves them, published "
               f"from {PUBLISHED_FROM}; the library code there is {LIBRARY_CODE}'s, and identity.txt and "
               "library.log list every file of it that differs at the after commit\n\n"]
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
            served = self.served(package_id, version, log)
            if served is None:
                continue
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

    def served(self, package_id: str, version: str, log: list[str]) -> Path | None:
        """nuget.org's copy of a package, fetched once into the scratch folder."""
        lowered = package_id.lower()
        served = self.scratch / "nuget-org" / f"{lowered}.{version}.nupkg"
        served.parent.mkdir(parents=True, exist_ok=True)
        url = f"https://api.nuget.org/v3-flatcontainer/{lowered}/{version}/{served.name}"
        log.append(f"## {package_id} {version}\n")
        if not served.exists():
            try:
                with urllib.request.urlopen(url, timeout=60) as response:
                    served.write_bytes(response.read())
            except OSError as error:
                log.append(f"nuget.org: not fetched ({error})\n\n")
                return None
        log.append(f"nuget.org serves {url}: {served.stat().st_size:,} bytes; sha512 "
                   f"{hashlib.sha512(served.read_bytes()).hexdigest()}\n")
        return served

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
        # The Native AOT probe, as sjl-8-001's probe step takes it, but the JIT's run takes the build the home step
        # made of the shared folder, with no second build.
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
        jit_dll = self.tree / PROBE / "bin" / "Release" / "net10.0" / "Broiler.ECMAScript.Probe.dll"
        log.append(f"# the JIT's probe: {jit_dll.relative_to(self.tree)}, the home step's Release build, sha256 "
                   f"{self.sha256(jit_dll) if jit_dll.exists() else '(not built)'}\n")
        self.run(["dotnet", str(jit_dll), "conformance/vectors", "--report", str(jit)], self.tree, log)
        same = aot.exists() and jit.exists() and aot.read_bytes() == jit.read_bytes()
        log.append(f"# the Native AOT report is byte-identical to the JIT's: {same}\n")
        self.log_to(f"{out}/probe-run-{rid}.log", log)
        if aot.exists():
            self.write_gz(f"{out}/probe-report-aot.tsv.gz", aot.read_text(encoding="utf-8"))

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
            f"tested commit: {tested} (the merge that lands SJL-10.5's cut-over, Broiler-Platform/Broiler.JS#1054, "
            f"with corpus revision 26), whose product sources are {CUT_OVER[:8]}'s but for the files identity.txt "
            "lists",
            f"configuration: {configuration}, with negative-metadata tests (include_negative true); includeNegative "
            f"{fields.get('includeNegative', '(not in run.txt)')} in run.txt",
            f"artifact: test262-evidence, id {self.artifact_id}, {self.artifact_zip.stat().st_size:,} bytes, "
            f"sha256 {digest} (the zip as downloaded)",
            f"retained: every file of the artifact, unchanged, in this directory ({len(names)} entries); this "
            "file is the only one added",
        ]) + "\n", encoding="utf-8", newline="\n")

    # -- consumer --------------------------------------------------------------------------------

    def consumer(self) -> None:
        # The engine set, packed from the after commit, restored by a clean consumer from a local feed and
        # nuget.org into an isolated cache, and run (it prints 42); the consumer is kept for the next parts.
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
                (consumer / "temporal-checks.js").write_text(script, encoding="utf-8")
                (consumer / "Program.cs").write_text(CONSUMER_PROGRAM, encoding="utf-8")
                project = (consumer / "Consumer.csproj").read_text(encoding="utf-8")
                project = project.replace("</Project>", """  <ItemGroup>
    <None Include="temporal-checks.js" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>""")
                (consumer / "Consumer.csproj").write_text(project, encoding="utf-8")
                log = [f"# the pristine consumer above, kept, with its Program.cs replaced by a run of these checks under "
                       f"TZ={CHECKS_ZONE} and no LANG, offline from the same isolated cache; each is a JavaScript "
                       "expression that must be true:\n"]
                log += [f"#   {row}: {expression}\n" for row, expression in TEMPORAL_CHECKS]
                env, self.env = self.env, dict(self.env, NUGET_PACKAGES=str(cache), TZ=CHECKS_ZONE)
                self.env.pop("LANG", None)
                try:
                    self.run(["dotnet", "run", "-c", "Release", "--no-restore"], consumer, log)
                finally:
                    self.env = env
                self.shared_record(cache, log)

                # The same consumer, its build outputs removed, restored into a second isolated cache with the
                # shared stream's packages mapped to nuget.org alone, so that the library is the one nuget.org
                # publishes at this version, and the checks run again.
                published = temporary / "packages-published"
                feed = temporary / "feed"
                local_ids = sorted({self.package_id(p) for p in feed.glob("*.nupkg")
                                    if not p.name.endswith(".symbols.nupkg")})
                mapped = [i for i in local_ids if not i.startswith("Broiler.ECMAScript.")]
                (temporary / "NuGet.published.Config").write_text(
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<configuration>\n  <packageSources>\n    <clear />\n"
                    f"    <add key=\"phase1-local\" value=\"{html.escape(str(feed))}\" />\n"
                    "    <add key=\"nuget.org\" value=\"https://api.nuget.org/v3/index.json\" />\n  </packageSources>\n"
                    "  <packageSourceMapping>\n    <packageSource key=\"phase1-local\">\n"
                    + "".join(f"      <package pattern=\"{html.escape(i)}\" />\n" for i in mapped)
                    + "    </packageSource>\n    <packageSource key=\"nuget.org\">\n"
                    "      <package pattern=\"Broiler.ECMAScript.*\" />\n      <package pattern=\"*\" />\n"
                    "    </packageSource>\n  </packageSourceMapping>\n</configuration>\n", encoding="utf-8")
                for folder in ("bin", "obj"):
                    shutil.rmtree(consumer / folder, ignore_errors=True)
                log.append(f"\n# the same consumer, its bin/ and obj/ removed, restored into a second isolated cache "
                           f"with the shared stream's packages (Broiler.ECMAScript.*) mapped to nuget.org alone and the "
                           f"engine's {len(mapped)} packages to the local feed alone, so that the library is the one "
                           f"nuget.org publishes as {SHARED_VERSION}; then the same checks, under TZ={CHECKS_ZONE}\n")
                log.append("# the local feed's packages: " + ", ".join(local_ids) + "\n")
                env, self.env = self.env, dict(self.env, NUGET_PACKAGES=str(published), TZ=CHECKS_ZONE)
                self.env.pop("LANG", None)
                try:
                    self.run(["dotnet", "restore", "--configfile", str(temporary / "NuGet.published.Config"),
                              "--no-http-cache"], consumer, log)
                    self.run(["dotnet", "run", "-c", "Release", "--no-restore"], consumer, log)
                finally:
                    self.env = env
                self.shared_record(published, log)
                self.log_to("engine/pristine-consumer-temporal.log", log)
            finally:
                shutil.rmtree(temporary, ignore_errors=True)

        # The control: the same checks in the shell built at the rule's commit and at the after commit, as a
        # script-host program that prints its completion value, with TZ=America/New_York and no LANG.
        program = self.scratch / "temporal-checks-control.js"
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
                      f"{self.sha256(dll) if dll.exists() else '(not built)'}; exit {result.returncode}"
                      + (f"; built here: {' '.join(['dotnet', 'build', SHELL_PROJECT, '-c', 'Release'])}" if build else
                         "; built before (the harness's build)"),
                      result.stdout.strip(), (result.stderr.strip() and "# stderr\n" + result.stderr.strip()) or "", ""]
        self.write("engine/pristine-consumer-checks-control.txt", "\n".join(lines) + "\n")

    @staticmethod
    def package_id(package: Path) -> str:
        with zipfile.ZipFile(package) as archive:
            nuspec = next(n for n in archive.namelist() if n.endswith(".nuspec"))
            found = re.search(r"<id>([^<]+)</id>", archive.read(nuspec).decode("utf-8"))
        return found.group(1) if found else package.stem

    def shared_record(self, cache: Path, log: list[str]) -> None:
        """Where each shared package in a consumer's isolated cache came from, against nuget.org's copy."""
        log.append("\n# the shared packages in the consumer's isolated cache: where each was restored from, whether it "
                   "carries nuget.org's signature, its content hash, and the content hash of the copy nuget.org serves "
                   "(the hash of the package without its signature):\n")
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
                served = self.served(package_id, version, [])
                theirs = {}
                if served is not None:
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

    # -- simplification --------------------------------------------------------------------------

    def simplification(self) -> None:
        after = self.after()
        rule = BASE_COMMIT
        lines = [f"# SJL-10.6 deletion check, Broiler.JS at {after}: every entry of the Temporal core's Broiler.JS",
                 "# deletion list (Broiler.VM docs/evidence/sjl-10-001/correspondence.md, section 11, which JSD-0078's X6",
                 "# states, with decision D3's calendar copies, as the milestone's SJL-10.5 list names them), searched in",
                 "# the folder that held it with git grep -E, comments included, test projects included where the folder",
                 "# holds them, each pattern naming the members the entry held at the rule's commit. An entry with no line",
                 "# is absent; a line that remains is a call to the library, an adapter, a comment or another member of",
                 "# the same name, which the bundle's README reads. The files that held whole entries are listed too.", ""]
        lines.append(f"## files of {TEMPORAL}/ at the rule's commit ({rule[:8]}) that are not at the after commit")
        before = set(self.git(self.tree, "ls-tree", "-r", "--name-only", rule, "--", TEMPORAL + "/").splitlines())
        now = set(self.git(self.tree, "ls-tree", "-r", "--name-only", after, "--", TEMPORAL + "/").splitlines())
        lines += [f"  {p}" for p in sorted(before - now)] or ["  none"]
        lines.append(f"## files there at the after commit that were not at the rule's commit")
        lines += [f"  {p}" for p in sorted(now - before)] or ["  none"]
        lines.append("")
        for entry, paths, pattern in DELETED:
            hits = self.grep("-E", pattern, paths=["--", *paths])
            # The same search at the rule's commit, the control: it shows that the pattern names what the entry held.
            held = subprocess.run(["git", "-C", str(self.tree), "grep", "-n", "-E", pattern, rule, "--", *paths],
                                  capture_output=True, text=True).stdout.splitlines()
            lines += [f"## {entry}", f"# in {', '.join(paths)}: {pattern}",
                      f"# hits at the rule's commit ({rule[:8]}), the control: {len(held)}", f"# hits: {len(hits)}"]
            lines += [h[:240] for h in hits] + [""]
        self.write("simplification/deletion-check.log", "\n".join(lines) + "\n")

        # The name search: every deleted file and every deleted type, word-bounded, in every tracked file of the
        # engine and the shared folder; then the cut-over's own search of 156 names, repeated at the after commit.
        types_pattern = (r"^\s*((public|internal|private|protected|file)\s+)*((static|sealed|abstract|partial|"
                         r"readonly|ref)\s+)*(class|struct|record|enum|interface)\s+\w+")

        def types(commit: str) -> dict[str, str]:
            listing = subprocess.run(["git", "-C", str(self.tree), "grep", "-n", "-E", types_pattern, commit, "--",
                                      TEMPORAL + "/"], capture_output=True, text=True).stdout.splitlines()
            found: dict[str, str] = {}
            for line in listing:
                _, path, number, content = line.split(":", 3)
                name = re.search(r"\b(?:record\s+(?:class\s+|struct\s+)?|class\s+|struct\s+|enum\s+|interface\s+)(\w+)",
                                 content)
                if name:
                    found.setdefault(name.group(1), f"{path}:{number}")
            return found

        before_types, after_types = types(rule), types(after)
        deleted_types = sorted(set(before_types) - set(after_types))
        deleted_files = sorted(Path(p).name[:-len(".cs")] for p in before - now if p.endswith(".cs"))
        search = [f"# SJL-10.6 name search, Broiler.JS at {after}: every file of {TEMPORAL}/ that the rule's commit",
                  f"# ({rule[:8]}) holds and the after commit does not, by its name without .cs, and every type declared",
                  "# there at the rule's commit and nowhere there at the after commit, each searched word-bounded with",
                  f"# git grep -w in every tracked file of {', '.join(NAME_SEARCH_PATHS)} (tests, project files, comments",
                  "# and the changelogs included). A name with no hit is absent; each hit is read in the bundle's README.",
                  f"# Then the cut-over's own name search (cutover/deletion-search.txt, at c97a9996), its 156 names",
                  "# searched again the same way in Broiler.JS/'s .cs files, and set against the cut-over's.", "",
                  f"## deleted files: {len(deleted_files)}",
                  *[f"#   {n}" for n in deleted_files],
                  f"## deleted types: {len(deleted_types)}",
                  *[f"#   {n} (declared at {rule[:8]} in {before_types[n]})" for n in deleted_types],
                  f"## types declared there at the after commit: {', '.join(sorted(after_types))}", ""]
        for name in sorted(set(deleted_files) | set(deleted_types)):
            kind = " and ".join(k for k, names in (("a deleted file", deleted_files), ("a deleted type", deleted_types))
                                if name in names)
            hits = self.grep("-n", "-w", "-F", name, paths=["--", *NAME_SEARCH_PATHS])
            where = "; ".join(f"in {p}/: {sum(h.startswith(p + '/') for h in hits)}" for p in NAME_SEARCH_PATHS)
            search += [f"## {name} ({kind})", f"# hits: {len(hits)} ({where})"] + [h[:240] for h in hits] + [""]
        recorded = (HERE / "cutover" / "deletion-search.txt").read_text(encoding="utf-8").splitlines()
        names: list[str] = []
        absent_block = False
        for line in recorded:
            if line.startswith("Absent (no hit):"):
                absent_block = True
                continue
            if absent_block and line.startswith("    "):
                names += [n.strip() for n in line.split(",") if n.strip()]
            elif absent_block:
                absent_block = False
            elif re.match(r"^[A-Za-z_]\w*: \d+ - ", line):
                names.append(line.split(":", 1)[0])
        recorded_hits = {line.split(":", 1)[0]: int(line.split(":", 2)[1].split(" - ")[0])
                         for line in recorded if re.match(r"^[A-Za-z_]\w*: \d+ - ", line)}
        search += [f"## the cut-over's name search repeated: {len(names)} names, in every tracked .cs file of Broiler.JS/",
                   "# name: hits now (hits at the cut-over)"]
        with_hits = 0
        for name in names:
            hits = self.grep("-n", "-w", "-F", name, paths=["--", "Broiler.JS/*.cs", "Broiler.JS/**/*.cs"])
            with_hits += bool(hits)
            search.append(f"{name}: {len(hits)} ({recorded_hits.get(name, 0)})")
            search += [f"    {h[:236]}" for h in hits]
        search += [f"# names with hits now: {with_hits}; at the cut-over: {len(recorded_hits)}", ""]
        self.write("simplification/name-search.log", "\n".join(search) + "\n")

        # The lines each C# file gained and lost between the rule's commit and the after commit, under Broiler.JS/.
        numstat = self.git(self.tree, "diff", "--numstat", "-M", rule, after, "--", "Broiler.JS/*.cs",
                           "Broiler.JS/**/*.cs").splitlines()
        rows, gained, lost = [], 0, 0
        for line in numstat:
            added, removed, path = line.split("\t", 2)
            gained += int(added)
            lost += int(removed)
            rows.append(f"{added:>6}  {removed:>6}  {path}")

        def folder(commit: str) -> tuple[int, int, int]:
            files = [p for p in self.git(self.tree, "ls-tree", "-r", "--name-only", commit, "--",
                                         TEMPORAL + "/").splitlines() if p.endswith(".cs")]
            count = sum(len(self.git(self.tree, "show", f"{commit}:{p}").splitlines()) for p in files)
            regex = subprocess.run(["git", "-C", str(self.tree), "grep", "-l", "-E", r"\bRegex\b|RegularExpressions",
                                    commit, "--", TEMPORAL + "/"], capture_output=True, text=True).stdout.splitlines()
            return len(files), count, len(regex)

        rule_folder, after_folder = folder(rule), folder(after)
        counts = [f"# SJL-10.6: the lines each C# file under Broiler.JS/ gained and lost between the rule's commit "
                  f"({rule[:8]}) and the after commit ({after[:8]}), by git diff --numstat -M (a renamed file is",
                  "# one row; a deleted file loses every line). Columns: lines added, lines removed, file.", "",
                  *rows, "",
                  f"# files: {len(rows)}; lines added {gained}; lines removed {lost}",
                  f"# {TEMPORAL}/: {rule_folder[0]} .cs files of {rule_folder[1]} lines at the rule's commit, "
                  f"{rule_folder[2]} of them naming Regex; {after_folder[0]} of {after_folder[1]} lines at the after "
                  f"commit, {after_folder[2]} naming Regex",
                  f"# the public JSTemporal* classes at the after commit (SJL-D-25 keeps them as shells):"]
        counts += [f"#   {h[:200]}" for h in self.grep("-E", r"public (sealed |partial |abstract )*class JSTemporal\w*",
                                                         paths=["--", TEMPORAL + "/"])]
        self.write("simplification/line-counts.log", "\n".join(counts) + "\n")

    # -- manifest --------------------------------------------------------------------------------

    def manifest(self) -> None:
        files = {}
        for path in sorted(self.out.rglob("*")):
            if path.is_file() and path.name != "manifest.json" and "__pycache__" not in path.parts:
                files[str(path.relative_to(self.out)).replace(os.sep, "/")] = self.sha256(path)
        manifest = {
            "bundle": "sjl-10-001",
            "repository": "Broiler.JS",
            "baseCommit": BASE_COMMIT,
            "afterCommit": AFTER_COMMIT,
            "afterTree": self.after(),
            "cutOverCommit": CUT_OVER,
            "d12Commit": D12_COMMIT,
            "ruleCommit": self.rule_commit(),
            "sharedVersion": SHARED_VERSION,
            "sharedPublishedFrom": PUBLISHED_FROM,
            "sharedLibraryCode": LIBRARY_CODE,
            "suiteRevision": base.SUITE_REVISION,
            "files": files,
        }
        (self.out / "manifest.json").write_text(json.dumps(manifest, indent=1, sort_keys=True) + "\n",
                                                encoding="utf-8", newline="\n")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--tree", required=True, help="a clean checkout of the after commit")
    parser.add_argument("--vm-tree", help="unused here; kept for the shared helpers")
    parser.add_argument("--code-tree", help="home: a checkout of Broiler.Code with its review CLI built in Release")
    parser.add_argument("--base-tree", help="consumer: a clean checkout of the rule's commit, for the control")
    parser.add_argument("--part", choices=sorted(PARTS), help="test262 and compare: the part")
    parser.add_argument("--artifact-zip", help="test262: the part's after run's test262-evidence artifact, as downloaded")
    parser.add_argument("--run-url", help="test262: the after run's workflow run, with its attempt")
    parser.add_argument("--artifact-id", help="test262: the artifact's id")
    parser.add_argument("--run-times", help="test262: when the after run was dispatched and completed")
    parser.add_argument("--out", required=True)
    parser.add_argument("--scratch", default=os.path.join(os.environ.get("TMPDIR", "/tmp"), "sjl-10-001-js"))
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
