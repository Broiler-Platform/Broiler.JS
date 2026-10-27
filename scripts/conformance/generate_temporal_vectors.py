#!/usr/bin/env python3
"""Generate the Temporal area of the shared conformance corpus.

    python3 -I scripts/conformance/generate_temporal_vectors.py --out conformance/vectors/temporal \\
        [--polyfill scripts/conformance/temporalref] [--node node]

**The reference (decision D10 of bundle sjl-10-001, JSD-0078).** Every expected value is the answer of the Temporal
proposal's own polyfill at the revision both engines follow, `polyfill/lib` of tc39/proposal-temporal at
e8cc03fc970a65a3359e8870e3b35e687ac94e55 (2026-07-27), run under Node 22.22.0 (V8 12.4, ICU4C 77.1, CLDR 47.0, tzdb
2025b) by `temporalref.mjs`, which this generator writes the scripts for and reads the answers of: `ref:temporalref@1`.
It is neither engine's code. The polyfill's three runtime dependencies and theirs were installed from the npm registry
at the versions NPM_PACKAGES lists, which `temporalref/package-lock.json` pins: big-integer 1.6.52, call-bind 1.0.9 and
es-abstract 1.24.2, and 89 packages under them. Before a script runs, the generator checks every file of the polyfill's
`lib/` against the git blob id POLYFILL_LIB records for it at that revision, every installed package against its
version, and the versions Node reports, and refuses the run on any difference.

To set the reference up from a clone of tc39/proposal-temporal:

    git -C <proposal-temporal> archive e8cc03fc970a65a3359e8870e3b35e687ac94e55 polyfill/lib \\
        | tar -x --strip-components=1 -C scripts/conformance/temporalref
    (cd scripts/conformance/temporalref && npm ci)

**Where the pinned text and its polyfill disagree, the text rules (decision D7).** Each such vector takes the text's
answer, cites the clauses it rests on, and records the polyfill's answer in its provenance as
`overruled:temporalref@1=<outcome>`; each is listed here by its operation and inputs, with both answers.
- D7_OVERRIDES, 28 vectors: row D8 of the correspondence (Broiler.VM's `docs/evidence/sjl-10-001/correspondence.md`),
  NudgeToCalendarUnit's nudged instant after a shifted window, the case decision D7 names. The generator runs every
  script a second time with that one statement of the polyfill replaced by the text's (`temporalref.mjs
  --text-nudge`): every vector whose two answers differ must be listed, and every listed one must differ.
- D7_READ_OVERRIDES, 45 vectors: where the polyfill's own parser and era table answer otherwise than the pinned texts,
  found at SJL-10.2 and beyond what the correspondence ran under the polyfill. The grammar: a second space for the
  date-time separator, an offset's decimal separator with no digit, the month-days 02-30 and 04-31, calendar strings
  that are only a year-month or month-day with a calendar, and time zone strings that are a time, year-month or
  month-day with an offset or annotation; and the era proposal: the era aliases its 2025-11-21 change removed, and a
  regnal era read leniently. Each group's reason, read in the text, is in D7_READ_REASONS. The generator refuses the run
  if the polyfill's answer is not the one listed.

**Where the pinned text does not do what it means.** Broiler.VM's JSD-0054 section 4 names two places where the text
at the pinned revision is defective and the polyfill, Broiler.VM and Broiler.JS agree: ISODateSurpasses returns before
it reaches weeks and days, so the text's CalendarDateUntil never ends for weeks (the text gives no answer), and
ComputeNudgeWindow takes the origin as the window's start when r1 is zero even when the start duration is not. Decision
D7 names row D8 as its case and these two are not text answers the specification means, so the vectors take the
polyfill's answer there. The generator runs every script a third time with ComputeNudgeWindow's test replaced by the
text's literal one (`--text-window`) and records in each vector it changes what the literal text answers. That D7's
general wording reaches these two places is reported at SJL-10.2 as a finding against the area record.

**Classes.** Every vector is `normative`: the proposal at the pinned revision, with the Intl era and month code
proposal at 5833eae6c9079ffbb06a9ab7cc5128ebbf6e9ac7 for the calendars, determines each outcome. The calendars'
implementation-defined approximations (Table 1: `chinese` outside 1900 to 2100, `dangi` outside 1900 to 2050,
`persian` outside 1206 to 1498 AP) are not vectored: the polyfill reckons them with ICU4C, which neither engine's plan
commits it to (SJL-D-21: ICU4X's), so it is no reference for them. Nor are, where the polyfill's ICU4C or its use of
JavaScript's Date is not the specification (HEBREW_FIRST, HEBREW_UNRELIABLE, LIMIT_DATES): the Hebrew years before
AM 1 and 5806 to 5807, which ICU4C 77.1 reckons otherwise than the calendar's arithmetic (Broiler.VM's
`src/tests/temporal/calendars/divergences.txt`, group 3, and Calendrical Calculations' formulas); a non-ISO calendar's
fields at the ISO limits, whose year runs past Date's range; and the five Chinese months of that file's group 2, where
ICU4C's astronomy disagrees with the observatories' published calendar (no Chinese vector's answer falls in them).

**Populations.** Milestone SJL-10.2's: the grammar, every accept and refuse case of the area design's table and of the
rows G1 to G24 its JSD-0078 revision added; duration rounding in every mode and every increment of every unit;
balancing, regulation and addition at the limits (rows R1 to R8, B1 to B9); the calendars at their table edges (rows C1
to C18); and the durations' and differences' rows D1 to D12 and F1 to F8. The inputs are the correspondence's probes'
(`docs/evidence/sjl-10-001/correspondence-runs/*/probes/` in Broiler.VM), so that every row with a behavioural
difference is reached, with the rows' edges added; none of the probes' answers is used. Each vector's note names its
rows.

Every script is one ASCII line that declares no global and completes with a primitive; the reference evaluates all of
them in the one realm the polyfill installs itself in, and checks the global object after each.

The output is deterministic: the same inputs give the same bytes. Vector identifiers are stable and are never reused;
a later revision appends.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import struct
import subprocess
import sys
import tempfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPOSITORY = HERE.parents[1]
CORPUS = REPOSITORY / "conformance" / "vectors"

# The corpus revision this generator writes, and the revision its vectors were added in. Revision 25 classifies every
# difference of both engines' first runs on revision 24 (temporal-divergences.txt): Broiler.JS's, under UTC and
# America/New_York, and Broiler.VM's, taken locally; no expected value changes. Revision 26 retires every one of them,
# which the two engines' cut-overs, milestones SJL-10.5 (Broiler.JS's) and SJL-10.4 (Broiler.VM's), correct.
CORPUS_REVISION = 30
FIRST_REVISION = 24
GENERATOR = "ref:temporalref@1"
SPEC = "spec:es2026@0248456c"
TEMPORAL = "proposal:temporal@e8cc03fc"
ERA = "proposal:intl-era-monthcode@5833eae"
TEMPORAL_REVISION = "e8cc03fc970a65a3359e8870e3b35e687ac94e55"
ERA_REVISION = "5833eae6c9079ffbb06a9ab7cc5128ebbf6e9ac7"
ENGINES = ("broiler-js", "broiler-vm")
DIVERGENCES = HERE / "temporal-divergences.txt"
DRIVER = HERE / "temporalref.mjs"
DEFAULT_POLYFILL = HERE / "temporalref"

# What Node must report: the reference's ICU4C answers the non-ISO calendars and its tzdb the named zones.
NODE_VERSIONS = {"node": "22.22.0", "v8": "12.4.254.21-node.33", "icu": "77.1", "cldr": "47.0", "tz": "2025b",
                 "unicode": "16.0"}

# polyfill/lib at the pinned revision: each file's git blob id (`git ls-tree e8cc03fc polyfill/lib/`).
POLYFILL_LIB = {
    "assert.mjs": "e444062cfc1e2e5b08f062e2f79f36fa8bdae0c9",
    "calendar.mjs": "25bac61fbd5a742cc1a4483b0c55b8b8b6d22d09",
    "duration.mjs": "1b247bac6d2d4c0da430c26b10b71451d0ef8560",
    "ecmascript.mjs": "86c7e20af3c101fa3e4480686d9a04d147a098b2",
    "init.js": "fb569dfc87584a43ecbb3e30075c30ac334daa97",
    "instant.mjs": "8198643a8e5bd670d3c329c27d8eaa027ad2a999",
    "intl.mjs": "a4bb2177c5fd4d1494aa3afb54f06705d2ccbbc1",
    "intrinsicclass.mjs": "1e24f817424e7bdb12ae5780c952762e46dc6414",
    "legacydate.mjs": "4a3232affe4f419d439c5e08ef7a86021cd596b6",
    "math.mjs": "54f02e8aba0adab7e6a579f3bdb31e4f4eb7673d",
    "monthcode.mjs": "b4db11f8f7d0f881e53affde74df9c02380b2be7",
    "now.mjs": "e36c568382088674f5413ed496fb49611c2a33a9",
    "plaindate.mjs": "9c4d0c57d4624b33c8fc6e06bcd068e4c14b9754",
    "plaindatetime.mjs": "a2f8e37f32bda1f3f0de9fccf0393ef9e947a3ff",
    "plainmonthday.mjs": "f563672b9d02f7099b8404c660a4b95050566b05",
    "plaintime.mjs": "2182c64ba5374388c24f4ad7730bceb0dfde556c",
    "plainyearmonth.mjs": "56da50f4769c4e82e4ed789522071e688c83c12c",
    "primordials.mjs": "16dc4503fd8aa8c20e7edce87b204573b95494a6",
    "regex.mjs": "c74d1137e9e11e964325c4198fd1b974218943e1",
    "shim.mjs": "92908f709c8d94b5b3b74625df0b930a626938aa",
    "slots.mjs": "0898c8bf0dc6dfaa1e292d9a5c2d6fa39d83e6a4",
    "temporal.mjs": "8af3f03b95ab59ff021f39a3378ab8cb41a38ae9",
    "timeduration.mjs": "184323eed18dc675504dea828901ab3a4f8cc033",
    "zoneddatetime.mjs": "dda981717fc62a3530db707b5f8352d014305f92",
}

# Every npm package the polyfill runs on, by its exact version (temporalref/package-lock.json; installed 2026-10-18 at
# SJL-10.1 from the registry at the versions then current, decision D10's recorded gap).
NPM_PACKAGES = {
    "array-buffer-byte-length": "1.0.2", "arraybuffer.prototype.slice": "1.0.4", "async-function": "1.0.0",
    "available-typed-arrays": "1.0.7", "big-integer": "1.6.52", "call-bind": "1.0.9",
    "call-bind-apply-helpers": "1.0.2", "call-bound": "1.0.4", "data-view-buffer": "1.0.2",
    "data-view-byte-length": "1.0.2", "data-view-byte-offset": "1.0.1", "define-data-property": "1.1.4",
    "define-properties": "1.2.1", "dunder-proto": "1.0.1", "es-abstract": "1.24.2", "es-abstract-get": "1.0.0",
    "es-define-property": "1.0.1", "es-errors": "1.3.0", "es-object-atoms": "1.1.2", "es-set-tostringtag": "2.1.0",
    "es-to-primitive": "1.3.4", "for-each": "0.3.5", "function-bind": "1.1.2", "function.prototype.name": "1.2.0",
    "functions-have-names": "1.2.3", "generator-function": "2.0.1", "get-intrinsic": "1.3.0", "get-proto": "1.0.1",
    "get-symbol-description": "1.1.0", "globalthis": "1.0.4", "gopd": "1.2.0", "has-bigints": "1.1.0",
    "has-property-descriptors": "1.0.2", "has-proto": "1.2.0", "has-symbols": "1.1.0", "has-tostringtag": "1.0.2",
    "hasown": "2.0.4", "internal-slot": "1.1.0", "is-array-buffer": "3.0.5", "is-async-function": "2.1.1",
    "is-bigint": "1.1.0", "is-boolean-object": "1.2.2", "is-callable": "1.2.7", "is-data-view": "1.0.2",
    "is-date-object": "1.1.0", "is-document.all": "1.0.0", "is-finalizationregistry": "1.1.1",
    "is-generator-function": "1.1.2", "is-map": "2.0.3", "is-negative-zero": "2.0.3", "is-number-object": "1.1.1",
    "is-regex": "1.2.1", "is-set": "2.0.3", "is-shared-array-buffer": "1.0.4", "is-string": "1.1.1",
    "is-symbol": "1.1.1", "is-typed-array": "1.1.15", "is-weakmap": "2.0.2", "is-weakref": "1.1.1",
    "is-weakset": "2.0.4", "isarray": "2.0.5", "math-intrinsics": "1.1.0", "object-inspect": "1.13.4",
    "object-keys": "1.1.1", "object.assign": "4.1.7", "own-keys": "1.0.2", "possible-typed-array-names": "1.1.0",
    "reflect.getprototypeof": "1.0.10", "regexp.prototype.flags": "1.5.4", "safe-array-concat": "1.1.4",
    "safe-push-apply": "1.0.0", "safe-regex-test": "1.1.0", "set-function-length": "1.2.2",
    "set-function-name": "2.0.2", "set-proto": "1.0.0", "side-channel": "1.1.1", "side-channel-list": "1.0.1",
    "side-channel-map": "1.0.1", "side-channel-weakmap": "1.0.2", "stop-iteration-iterator": "1.1.0",
    "string.prototype.trim": "1.2.11", "string.prototype.trimend": "1.0.10", "string.prototype.trimstart": "1.0.8",
    "typed-array-buffer": "1.0.3", "typed-array-byte-length": "1.0.3", "typed-array-byte-offset": "1.0.5",
    "typed-array-length": "1.0.8", "unbox-primitive": "1.1.0", "which-boxed-primitive": "1.1.1",
    "which-builtin-type": "1.2.1", "which-collection": "1.0.2", "which-typed-array": "1.1.24",
}

# Decision D7's overrides, each listed: the vector, by its operation and inputs, takes the pinned text's answer, and its
# provenance records the polyfill's. Every one is row D8's case (NudgeToCalendarUnit after a shifted window, the window
# rounded down): the text keeps the window's start as the nudged instant, so bubbling sees that a whole larger unit has
# not passed; the polyfill takes the window's end and bubbles. Filled from the --text-nudge comparison, which must find
# exactly these.
D7_CLAUSES = ("sec-temporal-nudgetocalendarunit", "sec-temporal-bubblerelativeduration")
D7_OVERRIDES: list[dict[str, str]] = [dict(zip(("op", "in", "text", "polyfill"), row)) for row in (
    ('Temporal.Duration.prototype.round, toString',
     'str:"months" int:10 str:"days" int:29 str:"hours" int:12 str:"month" str:"year" undefined str:"floor" int:2019 int:3 int:31',
     'value: str:"P11M"', 'value: str:"P1Y"'),
    ('Temporal.Duration.prototype.round, toString',
     'str:"months" int:10 str:"days" int:29 str:"hours" int:12 str:"month" str:"year" undefined str:"floor" str:"2019-03-31T00:00[UTC]"',
     'value: str:"P11M"', 'value: str:"P1Y"'),
    ('Temporal.Duration.prototype.round, toString',
     'str:"months" int:10 str:"days" int:29 str:"hours" int:12 str:"month" str:"year" undefined str:"trunc" int:2019 int:3 int:31',
     'value: str:"P11M"', 'value: str:"P1Y"'),
    ('Temporal.Duration.prototype.round, toString',
     'str:"months" int:10 str:"days" int:29 str:"hours" int:12 str:"month" str:"year" undefined str:"trunc" str:"2019-03-31T00:00[UTC]"',
     'value: str:"P11M"', 'value: str:"P1Y"'),
    ('Temporal.Duration.prototype.round, toString',
     'str:"months" int:10 str:"days" int:29 str:"hours" int:12 str:"month" str:"year" undefined str:"halfCeil" int:2019 int:3 int:31',
     'value: str:"P11M"', 'value: str:"P1Y"'),
    ('Temporal.Duration.prototype.round, toString',
     'str:"months" int:10 str:"days" int:29 str:"hours" int:12 str:"month" str:"year" undefined str:"halfCeil" str:"2019-03-31T00:00[UTC]"',
     'value: str:"P11M"', 'value: str:"P1Y"'),
    ('Temporal.Duration.prototype.round, toString',
     'str:"months" int:10 str:"days" int:29 str:"hours" int:12 str:"month" str:"year" undefined str:"halfFloor" int:2019 int:3 int:31',
     'value: str:"P11M"', 'value: str:"P1Y"'),
    ('Temporal.Duration.prototype.round, toString',
     'str:"months" int:10 str:"days" int:29 str:"hours" int:12 str:"month" str:"year" undefined str:"halfFloor" str:"2019-03-31T00:00[UTC]"',
     'value: str:"P11M"', 'value: str:"P1Y"'),
    ('Temporal.Duration.prototype.round, toString',
     'str:"months" int:10 str:"days" int:29 str:"hours" int:12 str:"month" str:"year" undefined str:"halfExpand" int:2019 int:3 int:31',
     'value: str:"P11M"', 'value: str:"P1Y"'),
    ('Temporal.Duration.prototype.round, toString',
     'str:"months" int:10 str:"days" int:29 str:"hours" int:12 str:"month" str:"year" undefined str:"halfExpand" str:"2019-03-31T00:00[UTC]"',
     'value: str:"P11M"', 'value: str:"P1Y"'),
    ('Temporal.Duration.prototype.round, toString',
     'str:"months" int:10 str:"days" int:29 str:"hours" int:12 str:"month" str:"year" undefined str:"halfTrunc" int:2019 int:3 int:31',
     'value: str:"P11M"', 'value: str:"P1Y"'),
    ('Temporal.Duration.prototype.round, toString',
     'str:"months" int:10 str:"days" int:29 str:"hours" int:12 str:"month" str:"year" undefined str:"halfTrunc" str:"2019-03-31T00:00[UTC]"',
     'value: str:"P11M"', 'value: str:"P1Y"'),
    ('Temporal.Duration.prototype.round, toString',
     'str:"months" int:10 str:"days" int:29 str:"hours" int:12 str:"month" str:"year" undefined str:"halfEven" int:2019 int:3 int:31',
     'value: str:"P11M"', 'value: str:"P1Y"'),
    ('Temporal.Duration.prototype.round, toString',
     'str:"months" int:10 str:"days" int:29 str:"hours" int:12 str:"month" str:"year" undefined str:"halfEven" str:"2019-03-31T00:00[UTC]"',
     'value: str:"P11M"', 'value: str:"P1Y"'),
    ('Temporal.PlainDateTime.prototype.until, toString',
     'str:"2019-03-31T00:00" str:"2020-02-29T12:00" str:"year" str:"month" undefined str:"floor"',
     'value: str:"P11M"', 'value: str:"P1Y"'),
    ('Temporal.ZonedDateTime.prototype.until, toString',
     'str:"2019-03-31T00:00[UTC]" str:"2020-02-29T12:00[UTC]" str:"year" str:"month" undefined str:"floor"',
     'value: str:"P11M"', 'value: str:"P1Y"'),
    ('Temporal.PlainDateTime.prototype.until, toString',
     'str:"2019-03-31T00:00" str:"2020-02-29T12:00" str:"year" str:"month" undefined str:"trunc"',
     'value: str:"P11M"', 'value: str:"P1Y"'),
    ('Temporal.ZonedDateTime.prototype.until, toString',
     'str:"2019-03-31T00:00[UTC]" str:"2020-02-29T12:00[UTC]" str:"year" str:"month" undefined str:"trunc"',
     'value: str:"P11M"', 'value: str:"P1Y"'),
    ('Temporal.PlainDateTime.prototype.until, toString',
     'str:"2019-03-31T00:00" str:"2020-02-29T12:00" str:"year" str:"month" undefined str:"halfCeil"',
     'value: str:"P11M"', 'value: str:"P1Y"'),
    ('Temporal.ZonedDateTime.prototype.until, toString',
     'str:"2019-03-31T00:00[UTC]" str:"2020-02-29T12:00[UTC]" str:"year" str:"month" undefined str:"halfCeil"',
     'value: str:"P11M"', 'value: str:"P1Y"'),
    ('Temporal.PlainDateTime.prototype.until, toString',
     'str:"2019-03-31T00:00" str:"2020-02-29T12:00" str:"year" str:"month" undefined str:"halfFloor"',
     'value: str:"P11M"', 'value: str:"P1Y"'),
    ('Temporal.ZonedDateTime.prototype.until, toString',
     'str:"2019-03-31T00:00[UTC]" str:"2020-02-29T12:00[UTC]" str:"year" str:"month" undefined str:"halfFloor"',
     'value: str:"P11M"', 'value: str:"P1Y"'),
    ('Temporal.PlainDateTime.prototype.until, toString',
     'str:"2019-03-31T00:00" str:"2020-02-29T12:00" str:"year" str:"month" undefined str:"halfExpand"',
     'value: str:"P11M"', 'value: str:"P1Y"'),
    ('Temporal.ZonedDateTime.prototype.until, toString',
     'str:"2019-03-31T00:00[UTC]" str:"2020-02-29T12:00[UTC]" str:"year" str:"month" undefined str:"halfExpand"',
     'value: str:"P11M"', 'value: str:"P1Y"'),
    ('Temporal.PlainDateTime.prototype.until, toString',
     'str:"2019-03-31T00:00" str:"2020-02-29T12:00" str:"year" str:"month" undefined str:"halfTrunc"',
     'value: str:"P11M"', 'value: str:"P1Y"'),
    ('Temporal.ZonedDateTime.prototype.until, toString',
     'str:"2019-03-31T00:00[UTC]" str:"2020-02-29T12:00[UTC]" str:"year" str:"month" undefined str:"halfTrunc"',
     'value: str:"P11M"', 'value: str:"P1Y"'),
    ('Temporal.PlainDateTime.prototype.until, toString',
     'str:"2019-03-31T00:00" str:"2020-02-29T12:00" str:"year" str:"month" undefined str:"halfEven"',
     'value: str:"P11M"', 'value: str:"P1Y"'),
    ('Temporal.ZonedDateTime.prototype.until, toString',
     'str:"2019-03-31T00:00[UTC]" str:"2020-02-29T12:00[UTC]" str:"year" str:"month" undefined str:"halfEven"',
     'value: str:"P11M"', 'value: str:"P1Y"'),
)]

# Decision D7's overrides found by reading the pinned texts: inputs on which the polyfill's own parser or era table
# answers otherwise than the text, where neither engine's code was consulted for the answer. Each group's reason and
# clauses are in D7_READ_REASONS; each entry is (group, op, in, the text's outcome, the polyfill's outcome). They were
# found where a draft of this area's run in Broiler.VM disagreed with the reference, and each was then read in the text.
D7_READ_REASONS = {
    "separator": (("temporal:sec-temporal-iso8601grammar", "temporal:sec-temporal-parseisodatetime"),
                  "the grammar's DateTimeSeparator is one space, T or t, and a Time begins with its Hour, so a second"
                  " space parses as nothing"),
    "fraction": (("temporal:sec-temporal-iso8601grammar", "spec:sec-time-zone-offset-strings"),
                 "UTCOffset's TemporalDecimalFraction (ECMA-262's) holds at least one digit after its separator, so a"
                 " trailing separator parses as nothing"),
    "monthday": (("temporal:sec-temporal-iso8601grammar-static-semantics-early-errors",
                  "temporal:sec-temporal-iso8601grammar-static-semantics-isvalidmonthday"),
                 "DateSpecMonthDay's early error: IsValidMonthDay is false for 02-30 and 04-31"),
    "calendar-string": (("temporal:sec-temporal-parsetemporalcalendarstring", "temporal:sec-temporal-parseisodatetime"),
                        "the string parses only as a year-month or month-day, for which ParseISODateTime throws with a"
                        " calendar other than iso8601, and it is no AnnotationValue, so ParseTemporalCalendarString"
                        " throws a RangeError"),
    "zone-string": (("temporal:sec-temporal-parsetemporaltimezonestring", "temporal:sec-temporal-parseisodatetime"),
                    "ParseTemporalTimeZoneString admits a TemporalTimeString, TemporalYearMonthString or"
                    " TemporalMonthDayString and answers its time zone annotation, else its UTC offset"),
    "era-alias": (("era:table-eras", "era:sec-temporal-canonicalizeeraincalendar", "era:sup-temporal-nonisoresolvefields"),
                  "Table 2 at the pinned era proposal lists no such era or alias for the calendar (the aliases mundi,"
                  " minguo and before-roc left it on 2025-11-21, and incar and minguo-qian with them), so"
                  " CanonicalizeEraInCalendar answers undefined and NonISOResolveFields throws a RangeError (decision"
                  " D4)"),
    "regnal": (("era:sup-temporal-nonisoresolvefields", "era:sec-temporal-calendardatearithmeticyearforerayear"),
               "NonISOResolveFields checks the year against the era's arithmetic year (reiwa 1 is 2019) and then erases"
               " the era, for a lenient reading of a regnal era's bounds, so 2019-01-01 is a date, in Heisei 31"),
}
D7_READ_OVERRIDES = (
    ('separator', 'Temporal.PlainDate.from (string), toString',
     'str:"2020-01-01  12:30"',
     'error: RangeError', 'value: str:"2020-01-01"'),
    ('separator', 'Temporal.PlainDateTime.from (string), toString',
     'str:"2020-01-01  12:30"',
     'error: RangeError', 'value: str:"2020-01-01T12:30:00"'),
    ('separator', 'Temporal.PlainTime.from (string), toString',
     'str:"2020-01-01  12:30"',
     'error: RangeError', 'value: str:"12:30:00"'),
    ('separator', 'Temporal.PlainYearMonth.from (string), toString',
     'str:"2020-01-01  12:30"',
     'error: RangeError', 'value: str:"2020-01"'),
    ('separator', 'Temporal.PlainMonthDay.from (string), toString',
     'str:"2020-01-01  12:30"',
     'error: RangeError', 'value: str:"01-01"'),
    ('separator', 'Temporal.PlainDate.from (string), toString',
     'str:"20200101  12:30"',
     'error: RangeError', 'value: str:"2020-01-01"'),
    ('separator', 'Temporal.PlainDateTime.from (string), toString',
     'str:"20200101  12:30"',
     'error: RangeError', 'value: str:"2020-01-01T12:30:00"'),
    ('separator', 'Temporal.PlainTime.from (string), toString',
     'str:"20200101  12:30"',
     'error: RangeError', 'value: str:"12:30:00"'),
    ('separator', 'Temporal.PlainYearMonth.from (string), toString',
     'str:"20200101  12:30"',
     'error: RangeError', 'value: str:"2020-01"'),
    ('separator', 'Temporal.PlainMonthDay.from (string), toString',
     'str:"20200101  12:30"',
     'error: RangeError', 'value: str:"01-01"'),
    ('separator', 'Temporal.ZonedDateTime.from (string), toString',
     'str:"2020-01-01  12:30[UTC]"',
     'error: RangeError', 'value: str:"2020-01-01T12:30:00+00:00[UTC]"'),
    ('separator', 'Temporal.Instant.from (string), toString',
     'str:"2020-01-01  12:30Z"',
     'error: RangeError', 'value: str:"2020-01-01T12:30:00Z"'),
    ('fraction', 'Temporal.PlainDate.from (string), toString',
     'str:"2020-01-01T12:00+01:00:00."',
     'error: RangeError', 'value: str:"2020-01-01"'),
    ('fraction', 'Temporal.PlainDateTime.from (string), toString',
     'str:"2020-01-01T12:00+01:00:00."',
     'error: RangeError', 'value: str:"2020-01-01T12:00:00"'),
    ('fraction', 'Temporal.PlainTime.from (string), toString',
     'str:"2020-01-01T12:00+01:00:00."',
     'error: RangeError', 'value: str:"12:00:00"'),
    ('fraction', 'Temporal.PlainYearMonth.from (string), toString',
     'str:"2020-01-01T12:00+01:00:00."',
     'error: RangeError', 'value: str:"2020-01"'),
    ('fraction', 'Temporal.PlainMonthDay.from (string), toString',
     'str:"2020-01-01T12:00+01:00:00."',
     'error: RangeError', 'value: str:"01-01"'),
    ('fraction', 'Temporal.PlainTime.from (string), toString',
     'str:"12:00+01:00:00."',
     'error: RangeError', 'value: str:"12:00:00"'),
    ('fraction', 'Temporal.ZonedDateTime.from (string, offset), toString',
     'str:"2020-01-01T12:00+01:00:00.[UTC]" str:"ignore"',
     'error: RangeError', 'value: str:"2020-01-01T12:00:00+00:00[UTC]"'),
    ('fraction', 'Temporal.Instant.from (string), toString',
     'str:"2020-01-01T12:00+01:00:00."',
     'error: RangeError', 'value: str:"2020-01-01T11:00:00Z"'),
    ('monthday', 'Temporal.PlainMonthDay.from (string), toString',
     'str:"02-30"',
     'error: RangeError', 'value: str:"02-30"'),
    ('monthday', 'Temporal.PlainMonthDay.from (string), toString',
     'str:"04-31"',
     'error: RangeError', 'value: str:"04-31"'),
    ('calendar-string', "ToTemporalCalendarIdentifier (string), a property bag's calendar",
     'str:"2020-01[u-ca=gregory]"',
     'error: RangeError', 'value: str:"gregory"'),
    ('calendar-string', 'ToTemporalCalendarIdentifier (string), withCalendar',
     'str:"2020-01[u-ca=gregory]"',
     'error: RangeError', 'value: str:"gregory"'),
    ('calendar-string', "ToTemporalCalendarIdentifier (string), a property bag's calendar",
     'str:"01-01[u-ca=gregory]"',
     'error: RangeError', 'value: str:"gregory"'),
    ('calendar-string', 'ToTemporalCalendarIdentifier (string), withCalendar',
     'str:"01-01[u-ca=gregory]"',
     'error: RangeError', 'value: str:"gregory"'),
    ('calendar-string', "ToTemporalCalendarIdentifier (string), a property bag's calendar",
     'str:"1214[u-ca=gregory]"',
     'error: RangeError', 'value: str:"gregory"'),
    ('calendar-string', 'ToTemporalCalendarIdentifier (string), withCalendar',
     'str:"1214[u-ca=gregory]"',
     'error: RangeError', 'value: str:"gregory"'),
    ('zone-string', 'ToTemporalTimeZoneIdentifier (string), toZonedDateTimeISO',
     'str:"10:00+01:00"',
     'value: str:"+01:00"', 'error: RangeError'),
    ('zone-string', "ToTemporalTimeZoneIdentifier (string), a property bag's timeZone",
     'str:"10:00+01:00"',
     'value: str:"+01:00"', 'error: RangeError'),
    ('zone-string', 'ToTemporalTimeZoneIdentifier (string), toZonedDateTimeISO',
     'str:"T10:00+01:00"',
     'value: str:"+01:00"', 'error: RangeError'),
    ('zone-string', "ToTemporalTimeZoneIdentifier (string), a property bag's timeZone",
     'str:"T10:00+01:00"',
     'value: str:"+01:00"', 'error: RangeError'),
    ('zone-string', 'ToTemporalTimeZoneIdentifier (string), toZonedDateTimeISO',
     'str:"10:00[Europe/Berlin]"',
     'value: str:"Europe/Berlin"', 'error: RangeError'),
    ('zone-string', "ToTemporalTimeZoneIdentifier (string), a property bag's timeZone",
     'str:"10:00[Europe/Berlin]"',
     'value: str:"Europe/Berlin"', 'error: RangeError'),
    ('zone-string', 'ToTemporalTimeZoneIdentifier (string), toZonedDateTimeISO',
     'str:"2020-01[Europe/Berlin]"',
     'value: str:"Europe/Berlin"', 'error: RangeError'),
    ('zone-string', "ToTemporalTimeZoneIdentifier (string), a property bag's timeZone",
     'str:"2020-01[Europe/Berlin]"',
     'value: str:"Europe/Berlin"', 'error: RangeError'),
    ('zone-string', 'ToTemporalTimeZoneIdentifier (string), toZonedDateTimeISO',
     'str:"01-01[Europe/Berlin]"',
     'value: str:"Europe/Berlin"', 'error: RangeError'),
    ('zone-string', "ToTemporalTimeZoneIdentifier (string), a property bag's timeZone",
     'str:"01-01[Europe/Berlin]"',
     'value: str:"Europe/Berlin"', 'error: RangeError'),
    ('era-alias', 'Temporal.PlainDate.from (property bag), era fields',
     'str:"calendar" str:"ethioaa" str:"era" str:"mundi" str:"eraYear" int:5 str:"monthCode" str:"M01" str:"day" int:1',
     'error: RangeError', 'value: str:"aa;5;5;-005488-07-17"'),
    ('era-alias', 'Temporal.PlainDate.from (property bag), era fields',
     'str:"calendar" str:"ethiopic" str:"era" str:"mundi" str:"eraYear" int:5 str:"monthCode" str:"M01" str:"day" int:1',
     'error: RangeError', 'value: str:"aa;5;-5495;-005488-07-17"'),
    ('era-alias', 'Temporal.PlainDate.from (property bag), era fields',
     'str:"calendar" str:"ethiopic" str:"era" str:"incar" str:"eraYear" int:5 str:"monthCode" str:"M01" str:"day" int:1',
     'error: RangeError', 'value: str:"am;5;5;0012-08-27"'),
    ('era-alias', 'Temporal.PlainDate.from (property bag), era fields',
     'str:"calendar" str:"roc" str:"era" str:"minguo" str:"eraYear" int:5 str:"monthCode" str:"M01" str:"day" int:1',
     'error: RangeError', 'value: str:"roc;5;5;1916-01-01"'),
    ('era-alias', 'Temporal.PlainDate.from (property bag), era fields',
     'str:"calendar" str:"roc" str:"era" str:"before-roc" str:"eraYear" int:5 str:"monthCode" str:"M01" str:"day" int:1',
     'error: RangeError', 'value: str:"broc;5;-4;1907-01-01"'),
    ('era-alias', 'Temporal.PlainDate.from (property bag), era fields',
     'str:"calendar" str:"roc" str:"era" str:"minguo-qian" str:"eraYear" int:5 str:"monthCode" str:"M01" str:"day" int:1',
     'error: RangeError', 'value: str:"broc;5;-4;1907-01-01"'),
    ('regnal', 'Temporal.PlainDate.from (property bag), era fields',
     'str:"calendar" str:"japanese" str:"year" int:2019 str:"era" str:"reiwa" str:"eraYear" int:1 str:"monthCode" str:"M01" str:"day" int:1',
     'value: str:"heisei;31;2019;2019-01-01"', 'error: RangeError'),
)

FILE_COMMENTS = {
    "grammar": [
        "# Rows G1 to G24 and the area design's table (section 3, with JSD-0078's rows): the ISO 8601 and RFC 9557",
        "# grammar through every goal's from(), duration strings, calendar, time zone and offset strings, and relativeTo",
        "# strings; years, separators, dates that are not dates, times, offsets, annotations and their order, digits other",
        "# than ASCII and U+2212. Each from() answers its value's toString().",
    ],
    "balancing": [
        "# Rows R1 to R8 and B1 to B9: the records' limits (dates, date-times, year-months, instants, times, durations),",
        "# integers beyond the engines' fields, narrowing inside addition, and balancing, regulation and addition at the",
        "# limits, across month ends and days, and on New York's 2026 wall clock.",
    ],
    "durations": [
        "# Rows D1 to D4: a duration's sign and shape, its default largest unit and the balance of a sum, addition with",
        "# calendar units (which the pinned proposal refuses, its add taking one parameter), and compare.",
    ],
    "rounding": [
        "# Rows D5 to D12: Duration.prototype.round in every rounding mode and every increment of every unit, with and",
        "# without relativeTo; nudging, bubbling and the shifted window (D8, where the pinned text rules: decision D7);",
        "# a zoned relativeTo across New York's 2026 transitions; total; and relativeTo in other calendars.",
    ],
    "differences": [
        "# Rows F1 to F8: until and since for every type: the difference settings, rounding in each calendar, the day a",
        "# date-time borrows, time and instant differences, New York's 2026 transitions, and year-months at the limits.",
    ],
    "calendars": [
        "# Rows C1 to C18: calendar identifiers and aliases, month codes in every calendar, eras and their names,",
        "# month-day reference dates, the formula calendars at their edges, the table calendars at their tables' edges",
        "# where Table 1 publishes them, the calendar an object lends, fields beyond 32 bits, and the conversions.",
    ],
}

MODES = ("ceil", "floor", "expand", "trunc", "halfCeil", "halfFloor", "halfExpand", "halfTrunc", "halfEven")


# --------------------------------------------------------------------------------------------------
# Values, in the schema's notation and as JavaScript source.

def utf16(s: str) -> list[int]:
    units = []
    for ch in s:
        code = ord(ch)
        if code > 0xFFFF:
            code -= 0x10000
            units += [0xD800 + (code >> 10), 0xDC00 + (code & 0x3FF)]
        else:
            units.append(code)
    return units


def body(s: str) -> str:
    """A string's code units as the schema writes them inside quotes, which is also an ASCII JavaScript literal's."""
    out = []
    for unit in utf16(s):
        if unit == 0x22:
            out.append('\\"')
        elif unit == 0x5C:
            out.append("\\\\")
        elif 0x20 <= unit <= 0x7E:
            out.append(chr(unit))
        else:
            out.append(f"\\u{unit:04X}")
    return "".join(out)


def lit(s: str) -> str:
    return f'"{body(s)}"'


def S(s: str) -> str:
    return f'str:"{body(s)}"'


U = "undefined"
NULL = "null"


def B(b: bool) -> str:
    return "bool:true" if b else "bool:false"


def bits(x: float) -> int:
    return struct.unpack(">Q", struct.pack(">d", x))[0]


def N(x: int | float) -> str:
    """A Number input: by its value where it is an integer a double holds exactly, else by its bit pattern."""
    if isinstance(x, bool):
        raise TypeError(x)
    if isinstance(x, int):
        if float(x) != x:
            raise ValueError(f"{x} is not a double")
        return f"int:{x}"
    if math.isnan(x):
        return "f64:0x7FF8000000000000"
    if x.is_integer() and abs(x) <= 2 ** 53 and not (x == 0 and math.copysign(1, x) < 0):
        return f"int:{int(x)}"
    return f"f64:0x{bits(x):016X}"


def js(v) -> str:
    """A JavaScript expression for an input value."""
    if v is None:
        return "null"
    if v is Undefined:
        return "undefined"
    if isinstance(v, bool):
        return "true" if v else "false"
    if isinstance(v, BigInt):
        return f"{v.value}n"
    if isinstance(v, Raw):
        return v.source
    if isinstance(v, int):
        return str(v)
    if isinstance(v, float):
        if math.isnan(v):
            return "NaN"
        if math.isinf(v):
            return "Infinity" if v > 0 else "-Infinity"
        if v == 0:
            return "-0" if math.copysign(1, v) < 0 else "0"
        if v.is_integer() and abs(v) < 1e16:
            return str(int(v))
        return repr(v).replace("e+", "e")
    if isinstance(v, str):
        return lit(v)
    if isinstance(v, dict):
        return "{" + ", ".join(f"{k}: {js(x)}" for k, x in v.items()) + "}"
    raise TypeError(v)


def typed(v) -> str:
    """An input value in the schema's notation; an object is its keys and values in order."""
    if v is None:
        return NULL
    if v is Undefined:
        return U
    if isinstance(v, bool):
        return B(v)
    if isinstance(v, BigInt):
        return f"bigint:{v.value}"
    if isinstance(v, Raw):
        return " ".join(v.inputs)
    if isinstance(v, (int, float)):
        return N(v)
    if isinstance(v, str):
        return S(v)
    if isinstance(v, dict):
        return " ".join(f"{S(k)} {typed(x)}" for k, x in v.items())
    raise TypeError(v)


class _Undefined:
    pass


Undefined = _Undefined()


class BigInt:
    def __init__(self, value: int) -> None:
        self.value = value


class Raw:
    """An input written as JavaScript source, with the typed inputs it stands for."""

    def __init__(self, source: str, *inputs: str) -> None:
        self.source = source
        self.inputs = inputs


def ins(*values) -> str:
    return " ".join(typed(v) for v in values)


# --------------------------------------------------------------------------------------------------
# The reference.

class PinError(Exception):
    pass


def git_blob(data: bytes) -> str:
    return hashlib.sha1(b"blob " + str(len(data)).encode() + b"\0" + data).hexdigest()


def check_polyfill(directory: Path) -> None:
    lib = directory / "lib"
    if not lib.is_dir():
        raise PinError(f"{lib} does not exist")
    present = sorted(p.name for p in lib.iterdir() if p.is_file())
    if present != sorted(POLYFILL_LIB):
        raise PinError(f"{lib} holds {present}, not the pinned revision's {sorted(POLYFILL_LIB)}")
    for name, blob in POLYFILL_LIB.items():
        actual = git_blob((lib / name).read_bytes())
        if actual != blob:
            raise PinError(f"lib/{name} is blob {actual}, not {blob} as at {TEMPORAL_REVISION}")
    modules = directory / "node_modules"
    installed = {}
    for package in modules.iterdir():
        if package.name.startswith("."):
            continue
        installed[package.name] = json.loads((package / "package.json").read_text(encoding="utf-8"))["version"]
    if installed != NPM_PACKAGES:
        extra = sorted(set(installed.items()) ^ set(NPM_PACKAGES.items()))
        raise PinError(f"node_modules differs from the pinned packages: {extra}")


def run_reference(polyfill: Path, node: str, requests: list[dict[str, str]], flag: str | None) -> dict[str, str]:
    with tempfile.TemporaryDirectory(prefix="temporalref-") as scratch:
        request_path = Path(scratch) / "requests.json"
        answer_path = Path(scratch) / "answers.json"
        request_path.write_text(json.dumps(requests), encoding="utf-8")
        command = [node, "--js-regexp-duplicate-named-groups", str(DRIVER), str(polyfill), str(CORPUS / "prelude.js"),
                   str(request_path), str(answer_path)] + ([flag] if flag else [])
        environment = {"TZ": "UTC", "PATH": "/usr/bin:/bin"}
        completed = subprocess.run(command, env=environment, capture_output=True, text=True)
        if completed.returncode != 0:
            raise PinError(f"temporalref.mjs {flag or ''} failed ({completed.returncode}): {completed.stderr.strip()}")
        result = json.loads(answer_path.read_text(encoding="utf-8"))
    if result["driver"] != "temporalref 1" or result["text"] != flag:
        raise PinError(f"the driver answered as {result['driver']!r} with {result['text']!r}")
    if result["versions"] != NODE_VERSIONS:
        raise PinError(f"Node reports {result['versions']}, not {NODE_VERSIONS}")
    return {answer["id"]: answer["outcome"] for answer in result["answers"]}


def compact(outcome: str) -> str:
    """An outcome inside a provenance entry: the space after its kind removed. It may hold no space."""
    kind, _, rest = outcome.partition(": ")
    text = f"{kind}:{rest}"
    if " " in text:
        raise PinError(f"the outcome {outcome!r} holds a space and cannot stand in a provenance entry")
    return text


# --------------------------------------------------------------------------------------------------
# Records.

class Corpus:
    def __init__(self) -> None:
        self.files: dict[str, list[dict[str, str]]] = {}
        self.counters: dict[str, int] = {}
        self.seen: set[tuple[str, str]] = set()
        self.meta: dict[str, dict] = {}

    def add(self, file: str, op: str, inputs: str, script: str, note: str,
            clauses: tuple[str, ...] = (), era: tuple[str, ...] = ()) -> None:
        # One vector per operation and input across the area: the first to ask for it keeps it.
        key = (op, inputs)
        if key in self.seen:
            return
        self.seen.add(key)
        assert file in FILE_COMMENTS, file
        self.counters[file] = self.counters.get(file, 0) + 1
        vector_id = f"temporal/{file}/{self.counters[file]:04d}"
        record = {"id": vector_id, "area": "temporal", "op": op, "in": inputs, "class": "normative",
                  "script": script, "note": note, "since": str(FIRST_REVISION)}
        for k, value in record.items():
            assert "\n" not in value and value == value.strip() and value, (k, value)
        assert all(0x20 <= ord(ch) < 0x7F for ch in script), script
        assert not script.startswith(("var ", "let ", "const ", "function ", "class ")), script
        self.files.setdefault(file, []).append(record)
        cited = [f"{TEMPORAL}#{c}" for c in clauses] + [f"{ERA}#{c}" for c in era]
        self.meta[vector_id] = {"clauses": list(dict.fromkeys(cited))}

    def records(self):
        for records in self.files.values():
            yield from records

    def resolve(self, polyfill: Path, node: str) -> dict[str, int]:
        """Runs the reference three times and writes every out and prov; returns counts for the summary."""
        requests = [{"id": r["id"], "script": r["script"]} for r in self.records()]
        pinned = run_reference(polyfill, node, requests, None)
        nudge = run_reference(polyfill, node, requests, "--text-nudge")
        window = run_reference(polyfill, node, requests, "--text-window")
        overrides = {(o["op"], o["in"]): o for o in D7_OVERRIDES}
        read = {(op, inputs): (group, text, polyfill) for group, op, inputs, text, polyfill in D7_READ_OVERRIDES}
        used: set[tuple[str, str]] = set()
        counts = {"d7": 0, "read": 0, "window": 0}
        problems = []
        for record in self.records():
            vector_id = record["id"]
            clauses = self.meta[vector_id]["clauses"]
            answer = pinned[vector_id]
            key = (record["op"], record["in"])
            if key in read:
                group, text, polyfill = read[key]
                if answer != polyfill or nudge[vector_id] != answer or window[vector_id] != answer:
                    problems.append(f"{vector_id}: D7_READ_OVERRIDES lists the polyfill's answer {polyfill!r}, and it "
                                    f"answers {answer!r}")
                    continue
                used.add(key)
                counts["read"] += 1
                cited, why = D7_READ_REASONS[group]
                entries = []
                for c in cited:
                    kind, _, clause = c.partition(":")
                    entries.append({"temporal": TEMPORAL, "era": ERA, "spec": SPEC}[kind] + "#" + clause)
                record["out"] = text
                record["prov"] = " ".join(dict.fromkeys(entries + clauses)) + f" overruled:{GENERATOR[4:]}={compact(answer)}"
                record["note"] += (f"; decision D7: the pinned text's answer, where its polyfill answers "
                                   f"{answer[len('value: '):] if answer.startswith('value: ') else answer}: {why}")
                continue
            if nudge[vector_id] != answer:
                override = overrides.get(key)
                if override is None:
                    problems.append(f"{vector_id} ({record['op']} {record['in']}): the text's nudge answers "
                                    f"{nudge[vector_id]!r}, the polyfill {answer!r}, and D7_OVERRIDES lists no override")
                    continue
                if override["polyfill"] != answer or override["text"] != nudge[vector_id]:
                    problems.append(f"{vector_id}: D7_OVERRIDES lists {override['text']!r} against "
                                    f"{override['polyfill']!r}, and the runs give {nudge[vector_id]!r} against {answer!r}")
                    continue
                used.add(key)
                counts["d7"] += 1
                record["out"] = override["text"]
                d7 = [f"{TEMPORAL}#{c}" for c in D7_CLAUSES]
                record["prov"] = " ".join(dict.fromkeys(d7 + clauses)) + f" overruled:{GENERATOR[4:]}={compact(answer)}"
                record["note"] += ("; decision D7: the pinned text's answer, where its polyfill answers "
                                   f"{answer[len('value: '):] if answer.startswith('value: ') else answer} (row D8)")
                continue
            record["out"] = answer
            record["prov"] = " ".join(clauses + [GENERATOR])
            if window[vector_id] != answer:
                counts["window"] += 1
                literal = window[vector_id]
                record["note"] += ("; ComputeNudgeWindow's r1 = 0 test, read literally, would answer "
                                   f"{literal[len('value: '):] if literal.startswith('value: ') else literal}: JSD-0054 "
                                   "section 4's defect of the text, which the polyfill and both engines do not follow")
        for key in list(overrides) + list(read):
            if key not in used:
                problems.append(f"{key} is listed as an override of decision D7 and no vector is it, or the text's"
                                " nudge does not change it")
        if problems:
            raise PinError("\n".join(problems))
        return counts

    def classify(self, path: Path) -> None:
        """Applies temporal-divergences.txt, as the other generators apply their own files; revision 24 had none,
        revision 25 adds it, and revision 26 retires every line of it."""
        if not path.exists():
            return
        reasons: dict[str, str] = {}
        by_id = {record["id"]: record for record in self.records()}
        for line_number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
            if not line or line.startswith("#"):
                continue
            if line.startswith("reason "):
                key, _, reason = line[len("reason "):].partition(": ")
                if not reason or key in reasons:
                    raise SystemExit(f"{path.name}:{line_number}: a malformed or repeated reason")
                reasons[key] = reason
                continue
            engine, key, vector_id = line.split(" ")
            record = by_id.get(vector_id)
            if engine not in ENGINES or key not in reasons or record is None:
                raise SystemExit(f"{path.name}:{line_number}: '{line}' names no engine, reason or vector")
            if f"divergence.{engine}" in record:
                raise SystemExit(f"{path.name}:{line_number}: {vector_id} cannot take this classification")
            if record["class"] == "normative":
                record["class"] = "divergence"
            record[f"divergence.{engine}"] = reasons[key]

    def write(self, out: Path) -> None:
        out.mkdir(parents=True, exist_ok=True)
        order = ["id", "area", "op", "in", "out", "class"]
        for name, records in sorted(self.files.items()):
            lines = [
                "# Generated by scripts/conformance/generate_temporal_vectors.py; do not edit by hand.",
                "# Expected values: the Temporal proposal's polyfill at e8cc03fc under Node 22.22.0 (ref:temporalref@1),",
                "# with the pinned text's answer where decision D7 rules (overruled:).",
                *FILE_COMMENTS[name],
                "schema: 1",
                "",
            ]
            for record in records:
                keys = order + sorted(k for k in record if k.startswith("divergence.")) + ["prov", "script", "note",
                                                                                           "since"]
                for key in keys:
                    if key in record:
                        lines.append(f"{key}: {record[key]}")
                lines.append("")
            (out / f"{name}.vectors").write_text("\n".join(lines), encoding="utf-8", newline="\n")


# --------------------------------------------------------------------------------------------------
# The grammar (rows G1 to G24 and the area design's table).

PLAIN = ("PlainDate", "PlainDateTime", "PlainTime", "PlainYearMonth", "PlainMonthDay")
ALL7 = PLAIN + ("ZonedDateTime", "Instant")
FROM_CLAUSE = {"PlainDate": "sec-temporal.plaindate.from", "PlainDateTime": "sec-temporal.plaindatetime.from",
               "PlainTime": "sec-temporal.plaintime.from", "PlainYearMonth": "sec-temporal.plainyearmonth.from",
               "PlainMonthDay": "sec-temporal.plainmonthday.from", "ZonedDateTime": "sec-temporal.zoneddatetime.from",
               "Instant": "sec-temporal.instant.from", "Duration": "sec-temporal.duration.from"}
GRAMMAR = ("sec-temporal-iso8601grammar", "sec-temporal-parseisodatetime")


def from_string(c: Corpus, goal: str, s: str, note: str, file: str = "grammar") -> None:
    clauses = (("sec-temporal-parsetemporaldurationstring",) if goal == "Duration" else GRAMMAR) + (FROM_CLAUSE[goal],)
    c.add(file, f"Temporal.{goal}.from (string), toString", ins(s), f"Temporal.{goal}.from({lit(s)}).toString()",
          note, clauses)


def each(c: Corpus, goals, s: str, note: str) -> None:
    for goal in goals:
        from_string(c, goal, s, note)


def grammar_note(base: str, s: str) -> str:
    if "−" in s:
        return base + "; U+2212 MINUS SIGN as a sign (row G10)"
    if any(0x660 <= ord(ch) <= 0x669 or 0xFF10 <= ord(ch) <= 0xFF19 for ch in s):
        return base + "; decimal digits other than ASCII (row G9)"
    return base


AI = lambda s: "".join(chr(0x660 + int(ch)) if ch.isdigit() else ch for ch in s)  # noqa: E731
FW = lambda s: "".join(chr(0xFF10 + int(ch)) if ch.isdigit() else ch for ch in s)  # noqa: E731


def grammar(c: Corpus) -> None:
    table = "the area design's table, section 3"
    from_string(c, "PlainDate", "2020-01-01[]", f"{table}: an empty annotation (row G11)")
    from_string(c, "PlainYearMonth", "2020-02-30", f"{table}: a year-month whose day is not a date (row G3)")
    from_string(c, "PlainTime", "2021-13-45T12:00", f"{table}: a time whose date is not a date (row G4)")

    dates = ["2020-01-01", "20200101", "2020-0101", "202001-01", "+002020-01-01", "+0020200101", "-002020-01-01",
             "-000000-01-01", "+000000-01-01", "-0000000101", "+0020-01-01", "02020-01-01", "020-01-01", "2020-1-01",
             "2020-01-1", "2020-00-01", "2020-13-01", "2020-01-00", "2020-01-32", "2020-02-29", "2021-02-29",
             "2020-02-30", "2020-04-31", "1900-02-29", "2000-02-29", "−002020-01-01", "+999999-12-31",
             "-999999-01-01", "-271821-04-19", "-271821-04-18", "+275760-09-13", "+275760-09-14", "0000-01-01",
             "-000001-12-31", " 2020-01-01", "2020-01-01 ", FW("2020") + "-01-01", AI("2020-01-01"), "2020-01-01\0", ""]
    for d in dates:
        note = grammar_note("rows G1 to G3: the date part (DateYear and its early error, DateSpec's separators and"
                            " early error), alone and through every goal", d)
        each(c, PLAIN, d, note)
        each(c, ("ZonedDateTime",), d + "[UTC]", note)
        each(c, ("Instant",), d + "T00:00Z", note)

    bare = ["12", "1230", "12:30", "123045", "12:30:45", "12:30:45.1", "12:30:45.123456789", "12:30:45.1234567890",
            "12:30:45,5", "12:30:60", "12:30:61", "24:00", "23:59:59.999999999", "12:3045", "1230:45", "12:30:45.",
            "12:30.5", "12.5", "T12:30", "t12:30", "T12", " 12:30", "TT12:30", "2021-12", "202112", "2021-13",
            "T2021-12", "1214", "0229", "0230", "1232", "--1214", "12:30Z", "12:30z", "T12:30Z", AI("12:30"),
            "12:30:45." + AI("1")]
    for s in bare:
        each(c, ("PlainTime",), s, grammar_note("rows G5 and G7: a time alone (TimeSpec, and the early errors of a"
                                                " designator-less time that is also a month-day or year-month)", s))
    with_date = ["T12", "T1230", "t12:30", " 12:30", "T", "TT12:30", "T12:30:45.123456789", "T12:30:45.1234567890",
                 "T12:30:60", "T24:00", "T23:59:60", "T12:3045", "T1230:45", "T12:30.5", "T12.5", "T12:30,5", "  12:30",
                 "T12:30:45.0"]
    for tail in with_date:
        note = "rows G5 and G6: a time after a date, in every goal (one form per time part)"
        each(c, PLAIN, "2020-01-01" + tail, note)
        each(c, PLAIN, "20200101" + tail, note)
        each(c, ("ZonedDateTime",), "2020-01-01" + tail + "[UTC]", note)
        each(c, ("Instant",), "2020-01-01" + tail + "Z", note)

    offsets = ["Z", "z", "+00:00", "-00:00", "-00", "-0000", "+01", "+0100", "+01:00", "+01:00:00", "+010000",
               "+01:00:00.5", "+01:00:00,5", "+010000.123456789", "+01:00:00.1234567890", "+01:0000", "+0100:00",
               "+24:00", "+23:60", "+23:59:60", "+23:59:59.999999999", "+1", "+001", "−01:00", "+" + AI("01:00"),
               "+01:00Z", "Z+01:00", "+01:00:00.", "+01:00.5"]
    for o in offsets:
        note = grammar_note("row G8: a UTC offset after a time in every goal (Z refused where the goal's"
                            " DateTimeUTCOffset is [~Z]; hour 00-23, minute and second 00-59)", o)
        each(c, PLAIN, "2020-01-01T12:00" + o, note)
        each(c, ("PlainTime",), "12:00" + o, note)
        s = "2020-01-01T12:00" + o + "[UTC]"
        c.add("grammar", "Temporal.ZonedDateTime.from (string, offset), toString", ins(s, "ignore"),
              f'Temporal.ZonedDateTime.from({lit(s)}, {{offset: "ignore"}}).toString()', note,
              GRAMMAR + ("sec-temporal.zoneddatetime.from",))
        each(c, ("Instant",), "2020-01-01T12:00" + o, note)
    for s in ("2020-01-01+01:00", "2020-01-01Z", "2020-01-01+01:00[UTC]"):
        each(c, ALL7, s, "row G8: an offset after a date with no time")

    annotations = [
        "[u-ca=iso8601]", "[!u-ca=iso8601]", "[u-ca=gregory][u-ca=japanese]", "[u-ca=gregory][!u-ca=japanese]",
        "[!u-ca=gregory][u-ca=japanese]", "[u-ca=iso8601][!u-ca=iso8601]", "[foo=bar]", "[!foo=bar]", "[U-CA=iso8601]",
        "[u-ca=ISO8601]", "[u-ca=iso_8601]", "[u-ca=]", "[=iso8601]", "[]", "[!]", "[u-ca=iso8601", "u-ca=iso8601]",
        "[u-ca=iso8601]]", "[[u-ca=iso8601]", "[UTC]", "[!UTC]", "[UTC][UTC]", "[UTC][u-ca=iso8601]",
        "[u-ca=iso8601][UTC]", "[foo=bar][UTC]", "[Europe/Berlin]", "[Mars/Olympus]", "[Mars Olympus]", "[+01:00]",
        "[+01:00:00]", "[-07:00:01]", "[+0100]", "[+01]", "[+24:00]", "[a=b]", "[_x=y]", "[x-y=z]", "[1a=b]",
        "[-a=b]", "[a=b-c]", "[a=b--c]", "[a=-b]", "[a=b-]", "[a=b_c]", "[aBc=d]", "[a=b=c]",
        "[foo=bar][u-ca=gregory]", "[u-ca=gregory][foo=bar][!u-ca=japanese]", "[.]", "[..]", "[a/./b]",
        "[Etc/GMT+5]", "[EST5EDT]", "[a..b]", "[/a]", "[a/]", "[a//b]", "[9a]", "[a-b+c_d.e]", "[ UTC]", "[UTC ]",
        "[u-ca=gregory] ", "[u-ca=" + AI("1") + "]", "[UTC][!u-ca=iso8601][u-ca=gregory]", "[!foo=bar][u-ca=iso8601]",
        "[u-ca=chinese]", "[u-ca=unknowncal]", "[u-ca=islamic]"]
    for a in annotations:
        note = grammar_note("rows G11 to G14: RFC 9557 annotations in every goal (a time zone annotation first and"
                            " once, critical flags, keys and values, empty and broken brackets, their order)", a)
        each(c, ("PlainDate", "PlainDateTime", "PlainYearMonth", "PlainMonthDay"), "2020-01-01T12:00" + a, note)
        each(c, ("PlainTime",), "12:00" + a, note)
        each(c, ("ZonedDateTime",), "2020-01-01T12:00[UTC]" + a, note)
        each(c, ("Instant",), "2020-01-01T12:00Z" + a, note)

    year_months = ["2020-01", "202001", "+00202001", "+002020-01", "-00202001", "2020-1", "2020-001", "2020-00",
                   "2020-13", "2020-01-01", "2020-01[u-ca=gregory]", "2020-01[u-ca=iso8601]", "2020-01[!u-ca=iso8601]",
                   "2020-01[u-ca=ISO8601]", "1976-11[u-ca=ISO8601]", "2020-01-01[u-ca=gregory]",
                   "2020-01-15[u-ca=hebrew]", "2020-01[UTC]", "2020-01[foo=bar]", "2020-01T12:00", "2020-01Z",
                   "202001[u-ca=gregory]", "2020-02-30[u-ca=gregory]", "+275760-09", "+275760-10", "-271821-04",
                   "-271821-03", "-271821-04-01[u-ca=gregory]", "2020-01[u-ca=chinese]"]
    for s in year_months:
        each(c, ("PlainYearMonth",), s, "rows G15, G3 and G16: the year-month goal (AnnotatedYearMonth, or a"
                                         " date-time whose date is a date)")
    month_days = ["01-01", "0101", "--01-01", "--0101", "-01-01", "---01-01", "--1-01", "02-29", "02-30", "04-31",
                  "00-01", "13-01", "01-00", "01-32", "--01-01[u-ca=iso8601]", "11-18[+01:00][u-ca=ISO8601]",
                  "01-01[u-ca=gregory]", "01-01[!u-ca=iso8601]", "2020-01-01[u-ca=gregory]", "2021-02-29", "2020-02-29",
                  "01-01T12:00", "--01-01T12:00", "01-01[UTC]", "01-01Z", "2020-01-01[u-ca=hebrew]",
                  "1972-02-29[u-ca=gregory]", "+275760-09-13[u-ca=gregory]", "-271821-04-19[u-ca=gregory]",
                  "-271821-04-19[u-ca=hebrew]", "+275760-09-13[u-ca=hebrew]"]
    for s in month_days:
        each(c, ("PlainMonthDay",), s, "row G15: the month-day goal (AnnotatedMonthDay, or a date-time)")
    zoned = ["2020-01-01[UTC]", "2020-01-01T00:00Z[UTC]", "2020-01-01T00:00z[UTC]", "2020-01-01T00:00+00:00[UTC]",
             "2020-01-01T00:00", "2020-01-01Z[UTC]", "2020-01-01T00:00Z", "2020-01-01T00:00[!UTC]",
             "2020-01-01T00:00[u-ca=iso8601][UTC]", "2020-01-01T00:00[foo=bar][UTC]", "2020-01-01T00:00[UTC][foo=bar]",
             "2020-01-01T00:00[UTC][!foo=bar]", "2020-01-01T00:00[UTC][u-ca=hebrew]", "2020-01-01T00:00[+01:00]",
             "2020-01-01T00:00[+0100]", "2020-01-01T00:00[+01]", "2020-01-01T00:00[-00:00]",
             "2020-01-01T00:00[+01:00:00]", "2020-01-01T00:00[Europe/Berlin]", "2020-01-01T00:00[europe/berlin]",
             "2020-01-01T00:00[]", "2020-01-01T00:00[!]", "2020-01-01T00:00[Mars Olympus]",
             "2020-01-01[u-ca=iso8601][UTC]", "2020-01[UTC]", "01-01[UTC]", "T00:00[UTC]",
             "2020-01-01T00:00+01:00[Europe/Berlin]", "2020-01-01T00:00+0100[Europe/Berlin]",
             "2020-01-01T00:00+01[Europe/Berlin]", "2020-01-01T00:00+01:00:00[Europe/Berlin]",
             "2020-01-01T00:00+01:00:00.000000001[Europe/Berlin]", "1970-01-01T00:00-00:44:30[Africa/Monrovia]",
             "1970-01-01T00:00-00:45[Africa/Monrovia]", "1970-01-01T00:00-00:44[Africa/Monrovia]",
             "1970-01-01T00:00-00:44:30.0[Africa/Monrovia]", "1970-01-01T00:00-0044[Africa/Monrovia]"]
    for s in zoned:
        each(c, ("ZonedDateTime",), s, "rows G15, G11 and G13: the zoned goal (the time zone annotation required and"
                                        " first; precise offsets that do and do not match)")
    instants = ["2020-01-01T00:00Z", "2020-01-01T00:00", "2020-01-01", "2020-01-01Z", "2020-01-01T00Z",
                "2020-01-01T00+01", "2020-01-01T00:00Z[UTC]", "2020-01-01T00:00Z[Mars/Olympus]",
                "2020-01-01T00:00Z[u-ca=hebrew]", "2020-01-01T00:00Z[!u-ca=hebrew]", "2020-01-01T00:00Z[u-ca=unknowncal]",
                "2020-01-01T00:00Z[!foo=bar]", "2020-01-01 00:00Z", "20200101T0000Z", "-271821-04-20T00:00Z",
                "-271821-04-19T23:59:59.999999999Z", "-271821-04-19T00:00Z", "+275760-09-13T00:00Z",
                "+275760-09-13T00:00:00.000000001Z", "-271821-04-19T23:00-01:00", "+275760-09-13T01:00+01:00",
                "+275760-09-14T00:00+01:00", "-271821-04-18T23:00-01:00"]
    for s in instants:
        each(c, ("Instant",), s, "row G15: the instant goal (a time and Z or an offset required, at and past both"
                                 " limits)")
    for s in ("2020-01-01T12:00", "2020-01-01", "2020-13-01T12:00", "2020-01-01T12:00[u-ca=hebrew]",
              "2020-01-01T12:00[u-ca=unknowncal]", "2020-01-01T12:00[!u-ca=hebrew]",
              "2020-01-01T12:00+01:00[Europe/Berlin]"):
        each(c, ("PlainTime",), s, "rows G15 and G4: the time goal through a date-time (the date checked)")
    for s in ("2020-01-01[u-ca=GREGORY]", "2020-01-01[u-ca=Gregory]", "2020-01-01[u-ca=ethiopic-amete-alem]",
              "2020-01-01[u-ca=islamicc]", "2020-01-01[u-ca=gregorian]", "2020-01-01[u-ca=minguo]",
              "2020-01-01[u-ca=islamic]", "2020-01-01[u-ca=islamic-rgsa]"):
        each(c, ("PlainDate",), s, "rows G15 and C2: the calendar of a date's annotation, canonicalised")

    durations = ["P1Y", "p1y", "P1Y2M3W4DT5H6M7S", "P1Y1M1W1D", "P1M1W", "P1W1D", "PT0S", "P0D", "-P1D", "+P1D",
                 "−P1D", "P", "PT", "P1YT", "P1DT", "P1Dt", "PT1H1.5M", "PT1.5H", "PT1.5H30M", "PT1.5M30S",
                 "PT1,5S", "PT0.123456789S", "PT0.1234567890S", "PT1.S", "PT.5S", "P1.5D", "P1.5Y", "P1.5W", "P1D1H",
                 "P1H", "PT1D", "P1M1Y", "P1W1M", "PT1S1M", "PT1M1H", "P 1D", "P1D ", " P1D", "PT46H66M71.50040904S",
                 "P4294967295Y", "P4294967296Y", "P4294967295M", "P4294967295W", "P104249991374D", "P104249991375D",
                 "PT9007199254740991S", "PT9007199254740991.999999999S", "PT9007199254740992S", "PT2501999792983H",
                 "PT2501999792984H", "PT150119987579016M", "PT2501999792983.999999999H", "P1Y1Y", "PT1H1H",
                 "P" + AI("1") + "D", "PT" + AI("1") + "H", "P1d", "pt1h", "PT1h", "P1Dt1H", "P1DT1H1M1.123456789S",
                 "PT0.000000001H", "PT0.000000001M", "PT1.999999999H", "-PT1.000000001M", "P0Y0M0W0DT0H0M0.000000000S",
                 "PT00000000000000000000001S", "P00000000000000000000001D", "PT1.5H0M", "PT1H0.5M", "-P1Y-1M", "P-1D",
                 "P1Y2M3W4DT5H6M7.008009010S"]
    for s in durations:
        from_string(c, "Duration", s, grammar_note("rows G21 to G23: duration strings (designators, part order, a"
                                                   " fraction on the last time part, a time designator with a part,"
                                                   " signs, each field at and past its limit)", s))
    for s in ("PT46H66M71.50040904S", "PT1.5H", "PT0.000000001H", "-PT1.000000001M", "PT2501999792983.999999999H"):
        c.add("grammar", "Temporal.Duration.from (string), time fields", ins(s),
              f"(function (d) {{ return [d.hours, d.minutes, d.seconds, d.milliseconds, d.microseconds, "
              f"d.nanoseconds].join(); }})(Temporal.Duration.from({lit(s)}))",
              "row G23: a duration string's time fields, each part exact and converted once",
              ("sec-temporal-parsetemporaldurationstring",))

    calendar_strings = [
        "iso8601", "ISO8601", "gregory", "2020-01-01", "2020-01-01[u-ca=gregory]", "2020-01-01T00:00Z[UTC][u-ca=hebrew]",
        "2020-01-01T00:00Z[u-ca=hebrew]", "T10:00Z", "10:00Z", "T10:00[u-ca=hebrew]", "10:00[u-ca=hebrew]",
        "2020-01[u-ca=gregory]", "2020-01[u-ca=iso8601]", "01-01[u-ca=gregory]", "--01-01", "2020-01-01[]",
        "2020-01-01[u-ca=gregory][UTC]", "2020-01-01[UTC][u-ca=gregory]", "2020-01-01[!foo=bar]",
        "2020-01-01[u-ca=gregory][!u-ca=japanese]", "gregory-x", "", "-", "a_b", "2020-13-01",
        "2020-01-01T25:00[u-ca=gregory]", "2020-01-01T12:00+25:00[u-ca=gregory]",
        "2020-01-01T12:00+01:00:00.5[u-ca=gregory]", "2020-01-01[Mars Olympus][u-ca=gregory]",
        "2020-02-30[u-ca=gregory]", "1214[u-ca=gregory]", "T1214[u-ca=gregory]", "2020-01-01T12:3045Z[u-ca=gregory]",
        "gregorian", "minguo", "islamic", "japanese ", " japanese", "JAPANESE", "ethiopic-amete-alem", "islamicc",
        "islamic-civil", "u-ca=gregory", "[u-ca=gregory]", "2020-01-01[u-ca=gregory][u-ca=japanese]",
        "2020-01-01[u-ca=japanese][u-ca=gregory]", "2020-01-01T00:00Z[UTC][u-ca=hebrew][u-ca=coptic]",
        "2020-01-01[u-ca=gregory][u-ca=bogus]", "2020-01-01[u-ca=bogus][u-ca=gregory]"]
    cal = ("sec-temporal-parsetemporalcalendarstring", "sec-temporal-totemporalcalendaridentifier")
    for s in calendar_strings:
        note = "rows G17 and C2: a calendar string (ParseTemporalCalendarString; of several u-ca annotations, the first)"
        c.add("grammar", "ToTemporalCalendarIdentifier (string), a property bag's calendar", ins(s),
              f'Temporal.PlainDate.from({{year: 2020, monthCode: "M01", day: 1, calendar: {lit(s)}}}).calendarId',
              note, cal)
        c.add("grammar", "ToTemporalCalendarIdentifier (string), withCalendar", ins(s),
              f"new Temporal.PlainDate(2020, 1, 1).withCalendar({lit(s)}).calendarId", note, cal)
        c.add("grammar", "CanonicalizeCalendar, the constructor's calendar", ins(s),
              f"new Temporal.PlainDate(2020, 1, 1, {lit(s)}).calendarId", note,
              ("sec-temporal-canonicalizecalendar", "sec-temporal.plaindate"))
    for s in ("2020-01-01[u-ca=gregory][u-ca=japanese]", "2020-01-01[u-ca=japanese][u-ca=gregory]",
              "2020-01-01T00:00Z[UTC][u-ca=hebrew][u-ca=coptic]", "2020-01-01[u-ca=gregory][u-ca=bogus]",
              "2020-01-01[u-ca=bogus][u-ca=gregory]"):
        c.add("grammar", "Temporal.PlainDate.from (string), calendarId", ins(s),
              f"Temporal.PlainDate.from({lit(s)}).calendarId",
              "rows G14 and G17: of several u-ca annotations, the first", GRAMMAR)

    zone_strings = [
        "UTC", "2020-01-01T00:00Z", "2020-01-01T00:00z", "2020-01-01T00:00+01:00", "2020-01-01T00:00+01:00:01",
        "2020-01-01T00:00+01:00:00", "2020-01-01[Europe/Berlin]", "2020-01-01T00:00Z[Europe/Berlin]", "T10:00Z",
        "10:00Z", "10:00+01:00", "T10:00+01:00", "10:00[Europe/Berlin]", "2020-01-01", "2020-01-01T00:00",
        "2020-01-01T00:00[u-ca=gregory]", "2020-01[Europe/Berlin]", "01-01[Europe/Berlin]", "2020-01-01[]",
        "2020-01-01T00:00+25:00", "2020-01-01T00:00+23:60", "2020-01-01[u-ca=gregory][Europe/Berlin]",
        "2020-01-01[foo=bar][Europe/Berlin]", "+01:00", "+0100", "+01", "-00:00", "+01:00:00", "+01:00:00.5", "+24:00",
        "2020-01-01T00:00+01:00[+02:00]", "2020-01-01T00:00Z[+02:00]", "2020-01-01T12:3045Z", "2021-12", "1214Z",
        "2020-01-01T00:00Z[!Europe/Berlin]", "2020-01-01T00:00Z[u-ca=gregory][!foo=bar]", "2020-01-01T00:00-00:00",
        "2020-01-01T00:00-00", "2020-01-01T00:00Z[Europe/Berlin][UTC]", "2020-01-01T00:00Z[u-ca=iso8601][Europe/Berlin]",
        "2020-01-01T00:00Z[Europe/Berlin][u-ca=iso8601]"]
    tz = ("sec-temporal-parsetemporaltimezonestring", "sec-temporal-totemporaltimezoneidentifier")
    for z in zone_strings:
        note = "row G18: a time zone string (ParseTemporalTimeZoneString: an identifier, else an ISO string's zone)"
        c.add("grammar", "ToTemporalTimeZoneIdentifier (string), toZonedDateTimeISO", ins(z),
              f'Temporal.Instant.from("2020-01-01T00:00Z").toZonedDateTimeISO({lit(z)}).timeZoneId', note, tz)
        c.add("grammar", "ToTemporalTimeZoneIdentifier (string), a property bag's timeZone", ins(z),
              f"Temporal.ZonedDateTime.from({{year: 2020, month: 1, day: 1, timeZone: {lit(z)}}}).timeZoneId", note, tz)
        c.add("grammar", "Temporal.ZonedDateTime (constructor), timeZoneId", ins(BigInt(0), z),
              f"new Temporal.ZonedDateTime(0n, {lit(z)}).timeZoneId", note,
              ("sec-temporal.zoneddatetime", "sec-temporal-parsetemporaltimezonestring"))

    for o in ["Z", "z", "+00:00", "-00:00", "+25:00", "+24:00", "+23:59", "+01:00:00.5", "+01:00:00.123456789",
              "+01:00:00.1234567890", "+0100", "+01", "-00", "+01:00:60", "+01:0000", "−01:00", "+" + AI("01:00"),
              "", "UTC", "+1", "+01:00Z"]:
        note = grammar_note("row G19 and the area design's table: an offset string (ToOffsetString:"
                            " UTCOffset[+SubMinutePrecision], no Z)", o)
        c.add("grammar", "ToOffsetString, ZonedDateTime.prototype.with", ins(o, "ignore"),
              f'Temporal.ZonedDateTime.from("2020-01-01T00:00[UTC]").with({{offset: {lit(o)}}}, {{offset: "ignore"}})'
              ".toString()", note, ("sec-temporal-tooffsetstring", "sec-temporal.zoneddatetime.prototype.with"))
        c.add("grammar", "ToOffsetString, a property bag's offset", ins(o, "use"),
              f'Temporal.ZonedDateTime.from({{year: 2020, month: 1, day: 1, offset: {lit(o)}, timeZone: "UTC"}}, '
              '{offset: "use"}).toString()', note, ("sec-temporal-tooffsetstring", "sec-temporal.zoneddatetime.from"))
    for v in (0, None, True, BigInt(1)):
        c.add("grammar", "ToOffsetString, ZonedDateTime.prototype.with, not a string", ins(v),
              f'Temporal.ZonedDateTime.from("2020-01-01T00:00[UTC]").with({{offset: {js(v)}}}).toString()',
              "row G19: an offset that is not a string", ("sec-temporal-tooffsetstring",))

    cases = [("PlainDate", "2020-01-01"), ("PlainDateTime", "2020-01-01T12:00"), ("PlainTime", "12:30"),
             ("PlainYearMonth", "2020-01"), ("PlainMonthDay", "01-01"), ("ZonedDateTime", "2020-01-01T12:00[UTC]"),
             ("Instant", "2020-01-01T12:00Z"), ("Duration", "P1D")]
    for goal, s in cases:
        from_string(c, goal, AI(s), "row G9: Arabic-Indic digits in every part")
        from_string(c, goal, FW(s), "row G9: fullwidth digits in every part")
    for goal, s in [("PlainDate", "2020-01-0" + AI("1")), ("PlainDateTime", "2020-01-01T12:0" + AI("0")),
                    ("PlainDateTime", "2020-01-01T12:00:00." + AI("1")), ("Instant", "2020-01-01T12:00+0" + AI("1") + ":00"),
                    ("ZonedDateTime", "2020-01-01T12:00+0" + AI("1") + ":00[UTC]"),
                    ("PlainDate", "2020-01-01[u-ca=" + AI("1") + "]"), ("PlainDate", "2020-01-01[" + AI("1") + "=a]"),
                    ("Duration", "PT1." + AI("5") + "S")]:
        from_string(c, goal, s, "row G9: a digit other than ASCII in one part only")
    M = "−"
    for goal, s in [("PlainDate", M + "002020-01-01"), ("PlainDateTime", M + "002020-01-01T00:00"),
                    ("PlainYearMonth", M + "002020-01"), ("PlainMonthDay", M + "002020-01-01"),
                    ("ZonedDateTime", M + "002020-01-01T00:00[UTC]"), ("Instant", M + "002020-01-01T00:00Z"),
                    ("PlainDate", "2020-01-01T00:00" + M + "01:00"), ("PlainDateTime", "2020-01-01T00:00" + M + "01:00"),
                    ("PlainTime", "00:00" + M + "01:00"), ("PlainYearMonth", "2020-01-01T00:00" + M + "01:00"),
                    ("PlainMonthDay", "2020-01-01T00:00" + M + "01:00"),
                    ("ZonedDateTime", "2020-01-01T00:00" + M + "01:00[UTC]"),
                    ("Instant", "2020-01-01T00:00" + M + "01:00"), ("ZonedDateTime", "2020-01-01T00:00[" + M + "01:00]"),
                    ("Duration", M + "P1D")]:
        from_string(c, goal, s, "row G10: U+2212 MINUS SIGN as a sign, in each parser")
    c.add("grammar", "Temporal.ZonedDateTime (constructor), timeZoneId", ins(BigInt(0), M + "01:00"),
          f"new Temporal.ZonedDateTime(0n, {lit(M + '01:00')}).timeZoneId", "row G10: U+2212 in an offset identifier",
          ("sec-temporal.zoneddatetime",))

    for s in ("2020-01T12:00", "202001T1200", "2020-01 12:00", "2020-01T12:00Z", "2020-01T12:00[u-ca=iso8601]",
              "2020-01T25:00"):
        each(c, ("PlainYearMonth",), s, "row G16: a time on a year-month")
    for s in ("01-01T12:00", "0101T12:00", "--01-01T12:00"):
        each(c, ("PlainMonthDay",), s, "row G16: a time on a month-day")
    for s in ("2020-02-30T12:00", "2021-02-29T12:00", "2020-00-01T12:00", "2020-01-32T12:00", "-000000-01-01T12:00",
              "2020-0101T12:00", "2021-13-45T12:00", "2020-13-01T12:00"):
        each(c, ("PlainTime",), s, "row G4: a time from a date-time whose date is not a date")
    for s in ("2020-02-30", "2020-01-00", "2020-01-32", "2021-02-29", "2020-04-31", "1900-02-29",
              "2020-02-30[u-ca=gregory]"):
        each(c, ("PlainYearMonth",), s, "row G3: a year-month string whose day is not a date")
    for s in ("2020-01-15[u-ca=hebrew]", "2020-03-01[u-ca=chinese]", "2023-03-22[u-ca=chinese]",
              "2020-01-15[u-ca=persian]", "2019-09-11[u-ca=coptic]"):
        each(c, ("PlainMonthDay", "PlainYearMonth"), s, "row G15: a month-day or year-month string in another"
                                                         " calendar, the reference date its date gives")

    relative = ["2020-01-01", "2020-01-01T00:00", "2020-01-01T00:00[UTC]", "2020-01-01T00:00Z[UTC]", "2020-01-01T00:00Z",
                "2020-01-01T00:00+01:00", "2020-01-01T00:00+01:00[Europe/Berlin]", "2020-01-01T00:00+02:00[Europe/Berlin]",
                "2020-01-01[]", "2020-01-01[u-ca=iso8601][UTC]", "2020-01-01[foo=bar][UTC]", "2020-01-01[!foo=bar]",
                "2020-01-01[u-ca=hebrew]", "2020-01-01T00:00+25:00", "2020-01-01T00:00+01:0000", "2020-01", "01-01",
                "T00:00", "00:00", "2020-02-30", "2020-01-01T24:00", "2020-01-01[Mars Olympus]", "2020-01-01[UTC][UTC]",
                "2020-01-01T00:00" + M + "01:00", AI("2020-01-01"), "2020-01-01T12:3045",
                "2020-01-01T00:00+01:00:00.5[UTC]", "2020-01-01T00:00Z[u-ca=hebrew]",
                "2020-01-01T00:00[u-ca=gregory][!u-ca=japanese]"]
    rel = ("sec-temporal-gettemporalrelativetooption", "sec-temporal-parseisodatetime")
    for s in relative:
        note = grammar_note("row G20: a relativeTo string (TemporalDateTimeString[+Zoned] and [~Zoned])", s)
        c.add("grammar", "GetTemporalRelativeToOption (string), Duration.prototype.round", ins("P1M", "day", s),
              f'Temporal.Duration.from("P1M").round({{smallestUnit: "day", relativeTo: {lit(s)}}}).toString()', note,
              rel)
        c.add("grammar", "GetTemporalRelativeToOption (string), Duration.prototype.total", ins("P1M", "day", s),
              f'Temporal.Duration.from("P1M").total({{unit: "day", relativeTo: {lit(s)}}})', note, rel)


# --------------------------------------------------------------------------------------------------
# Shapes of vector shared by the files below.

def ctor(c: Corpus, file: str, kind: str, args, note: str, clauses=(), render: str = ".toString()") -> None:
    c.add(file, f"Temporal.{kind} (constructor){', ' + render.strip('.()') if render else ''}", ins(*args),
          f"new Temporal.{kind}({', '.join(js(a) for a in args)}){render}", note,
          (f"sec-temporal.{kind.lower()}",) + tuple(clauses))


def from_bag(c: Corpus, file: str, kind: str, bag: dict, note: str, options: dict | None = None, clauses=(),
             render: str = ".toString()") -> None:
    args = [bag] + ([options] if options is not None else [])
    c.add(file, f"Temporal.{kind}.from (property bag{', options' if options is not None else ''})"
                f"{', ' + render.strip('.()') if render else ''}",
          ins(*args), f"Temporal.{kind}.from({', '.join(js(a) for a in args)}){render}", note,
          (FROM_CLAUSE[kind],) + tuple(clauses))


def receiver(kind: str, value) -> Raw:
    if isinstance(value, Raw):
        return value
    return Raw(f"Temporal.{kind}.from({js(value)})", typed(value))


def method(c: Corpus, file: str, kind: str, value, name: str, args, note: str, clauses=(),
           render: str = ".toString()") -> None:
    """Temporal.<kind>.from(value).<name>(args)<render>; value is a string, a property bag or Raw."""
    r = receiver(kind, value)
    rendered = render.strip(".()") if render else "value"
    c.add(file, f"Temporal.{kind}.prototype.{name}, {rendered}",
          " ".join([typed(r)] + [typed(a) for a in args]), f"{r.source}.{name}({', '.join(js(a) for a in args)}){render}",
          note, (f"sec-temporal.{kind.lower()}.prototype.{name.lower()}",) + tuple(clauses))


def pd(y: int, m: int, d: int) -> Raw:
    return Raw(f"new Temporal.PlainDate({y}, {m}, {d})", N(y), N(m), N(d))


def zdt(s: str) -> Raw:
    return Raw(f"Temporal.ZonedDateTime.from({lit(s)})", S(s))


ROUND_KEYS = ("smallestUnit", "largestUnit", "roundingIncrement", "roundingMode", "relativeTo")


def duration(value) -> Raw:
    return Raw(f"Temporal.Duration.from({js(value)})", typed(value))


def round_duration(c: Corpus, file: str, value, options: dict, note: str, clauses=()) -> None:
    """Duration.prototype.round with an options bag: the duration, then the five options in a fixed order."""
    d = duration(value)
    slots = [options.get(k, Undefined) for k in ROUND_KEYS]
    for key in options:
        assert key in ROUND_KEYS, key
    c.add(file, "Temporal.Duration.prototype.round, toString", " ".join([typed(d)] + [typed(v) for v in slots]),
          f"{d.source}.round({js(options)}).toString()", note,
          ("sec-temporal.duration.prototype.round",) + tuple(clauses))


def total_duration(c: Corpus, file: str, value, unit: str, relative_to, note: str, clauses=()) -> None:
    d = duration(value)
    options = {"unit": unit} if relative_to is Undefined else {"unit": unit, "relativeTo": relative_to}
    c.add(file, "Temporal.Duration.prototype.total (options)", ins(d, unit, relative_to), f"{d.source}.total({js(options)})",
          note, ("sec-temporal.duration.prototype.total", "sec-temporal-totalrelativeduration") + tuple(clauses))


DIFF_KEYS = ("largestUnit", "smallestUnit", "roundingIncrement", "roundingMode")


def difference(c: Corpus, kind: str, one, two, name: str, options: dict | None, note: str, clauses=(),
               render: str = ".toString()") -> None:
    a, b = receiver(kind, one), receiver(kind, two)
    options = options or {}
    for key in options:
        assert key in DIFF_KEYS, key
    slots = [options.get(k, Undefined) for k in DIFF_KEYS]
    tail = f", {js(options)}" if options else ""
    rendered = render.strip(".()")
    c.add("differences", f"Temporal.{kind}.prototype.{name}, {rendered}",
          " ".join([typed(a), typed(b)] + [typed(v) for v in slots]), f"{a.source}.{name}({b.source}{tail}){render}",
          note, (f"sec-temporal.{kind.lower()}.prototype.{name}", f"sec-temporal-differencetemporal{kind.lower()}")
          + tuple(clauses))


# --------------------------------------------------------------------------------------------------
# Records, limits, balancing, regulation and addition (rows R1 to R8, B1 to B9).

MAX_NS = 8640000000000000000000


def balancing(c: Corpus) -> None:
    f = "balancing"
    r1 = "row R1: ISODateWithinLimits, at and one day beyond each limit"
    for args in ((-271821, 4, 19), (-271821, 4, 18), (275760, 9, 13), (275760, 9, 14)):
        ctor(c, f, "PlainDate", args, r1, ("sec-temporal-isodatewithinlimits",))
    for s in ("-271821-04-19", "-271821-04-18", "+275760-09-13", "+275760-09-14"):
        from_string(c, "PlainDate", s, r1, f)
    for bag in ({"year": -271821, "month": 4, "day": 18}, {"year": -271821, "month": 4, "day": 19},
                {"year": 275760, "month": 9, "day": 13}, {"year": 275760, "month": 9, "day": 14}):
        from_bag(c, f, "PlainDate", bag, r1, clauses=("sec-temporal-isodatewithinlimits",))
    for args in ((4, 18, Undefined, -271821), (4, 19, Undefined, -271821), (9, 13, Undefined, 275760),
                 (9, 14, Undefined, 275760)):
        ctor(c, f, "PlainMonthDay", args, r1 + " (a month-day's reference year)", ("sec-temporal-isodatewithinlimits",))

    r2 = "row R2: ISODateTimeWithinLimits's strict comparison, at and beyond each limit"
    for args in ((-271821, 4, 19), (-271821, 4, 19, 0, 0, 0, 0, 0, 1), (-271821, 4, 18, 23, 59, 59, 999, 999, 999),
                 (275760, 9, 13, 23, 59, 59, 999, 999, 999), (275760, 9, 14)):
        ctor(c, f, "PlainDateTime", args, r2, ("sec-temporal-isodatetimewithinlimits",))
    for s in ("-271821-04-19T00:00", "-271821-04-19T00:00:00.000000001", "+275760-09-13T23:59:59.999999999",
              "+275760-09-14T00:00"):
        from_string(c, "PlainDateTime", s, r2, f)
    method(c, f, "PlainDate", "-271821-04-19", "toPlainDateTime", [], r2)
    method(c, f, "PlainDate", "-271821-04-19", "toPlainDateTime", ["00:00:00.000000001"], r2)
    method(c, f, "PlainDate", "+275760-09-13", "toPlainDateTime", ["23:59:59.999999999"], r2)
    for bag in ({"year": -271821, "month": 4, "day": 19}, {"year": -271821, "month": 4, "day": 19, "nanosecond": 1}):
        from_bag(c, f, "PlainDateTime", bag, r2)

    r3 = "row R3: ISOYearMonthWithinLimits, April -271821 to September 275760"
    for args in ((-271821, 4), (-271821, 3), (275760, 9), (275760, 10), (-271821, 4, Undefined, 1),
                 (-271821, 4, Undefined, 19), (275760, 9, Undefined, 30)):
        ctor(c, f, "PlainYearMonth", args, r3, ("sec-temporal-isoyearmonthwithinlimits",))
    for s in ("-271821-04", "-271821-03", "+275760-09", "+275760-10"):
        from_string(c, "PlainYearMonth", s, r3, f)
    from_bag(c, f, "PlainYearMonth", {"year": 275760, "month": 10}, r3)
    from_bag(c, f, "PlainYearMonth", {"year": -271821, "month": 4}, r3)
    method(c, f, "PlainDate", "-271821-04-19", "toPlainYearMonth", [], r3)

    r4 = "row R4: IsValidEpochNanoseconds, |ns| <= 8.64e21, and the wall clock at the limits"
    for ns in (MAX_NS, MAX_NS + 1, -MAX_NS, -MAX_NS - 1):
        c.add(f, "Temporal.Instant.fromEpochNanoseconds, toString", ins(BigInt(ns)),
              f"Temporal.Instant.fromEpochNanoseconds({ns}n).toString()", r4, ("sec-temporal-isvalidepochnanoseconds",))
    for ms in (8.64e15, 8.64e15 + 1, -8.64e15, -8.64e15 - 1):
        c.add(f, "Temporal.Instant.fromEpochMilliseconds, toString", ins(ms),
              f"Temporal.Instant.fromEpochMilliseconds({js(ms)}).toString()", r4,
              ("sec-temporal-isvalidepochnanoseconds",))
    for s in ("-271821-04-20T00:00Z", "-271821-04-19T23:59:59.999999999Z", "+275760-09-13T00:00Z",
              "+275760-09-13T00:00:00.000000001Z", "+275760-09-13T01:00+01:00", "-271821-04-19T23:00-01:00"):
        from_string(c, "Instant", s, r4, f)
    for ns, zone, what in ((MAX_NS, "Etc/GMT-14", "toPlainDateTime"), (MAX_NS, "Etc/GMT-14", "toPlainDate"),
                           (-MAX_NS, "Etc/GMT+12", "toPlainDateTime"), (-MAX_NS, "-23:59", "toPlainDateTime"),
                           (-MAX_NS, "+23:59", "toPlainDateTime"), (MAX_NS, "-23:59", "toPlainDateTime")):
        c.add(f, f"Temporal.ZonedDateTime (constructor), {what}", ins(BigInt(ns), zone),
              f"new Temporal.ZonedDateTime({ns}n, {lit(zone)}).{what}().toString()", r4,
              ("sec-temporal.zoneddatetime", "sec-temporal-getisodatetimefor"))
    ctor(c, f, "ZonedDateTime", (BigInt(MAX_NS + 1), "UTC"), r4)
    ctor(c, f, "ZonedDateTime", (BigInt(-MAX_NS), "UTC"), r4)

    r5 = "row R5: IsValidTime and IsValidISODate, and ToIntegerWithTruncation of the fields"
    for args in ((23, 59, 59, 999, 999, 999), (24,), (0, 60), (0, 0, 60), (0, 0, 0, 1000), (0, 0, 0, 0, 1000),
                 (0, 0, 0, 0, 0, 1000), (-1,), (1.9, 2.9), (-0.5,), (math.nan,), (math.inf,), (Undefined, 5)):
        ctor(c, f, "PlainTime", args, r5, ("sec-temporal-isvalidtime",))
    ctor(c, f, "PlainDateTime", (2020, 1, 1, 24), r5)
    for args in ((2020, 2, 30), (2021, 2, 29), (2020, 2, 29), (2020, 13, 1), (2020, 0, 1), (2020, 1, 0),
                 (2020.7, 1.2, 1.9), (2020, 1, -0.5)):
        ctor(c, f, "PlainDate", args, r5, ("sec-temporal-isvalidisodate",))

    r6 = ("row R6: integers beyond the engines' fields, to the constructors and in property bags (Broiler.VM's"
          " constructors end the invocation above 2^53, its one correction)")
    for args in ((2.0 ** 31, 1, 1), (2.0 ** 32 + 2020, 1, 1), (2.0 ** 53, 1, 1), (2.0 ** 53 + 2, 1, 1), (1e20, 1, 1),
                 (-1e20, 1, 1), (2020, 2.0 ** 32 + 1, 1), (2020, 1, 2.0 ** 32 + 1)):
        ctor(c, f, "PlainDate", args, r6, ("sec-temporal-isodatewithinlimits",))
    ctor(c, f, "PlainDateTime", (2020, 1, 1, 2.0 ** 32), r6)
    ctor(c, f, "PlainDateTime", (1e20, 1, 1), r6)
    ctor(c, f, "PlainDateTime", (2.0 ** 53 + 2, 1, 1), r6)
    ctor(c, f, "PlainYearMonth", (1e20, 1), r6)
    ctor(c, f, "PlainMonthDay", (1, 1, Undefined, 2.0 ** 32 + 1972), r6)
    ctor(c, f, "PlainMonthDay", (1, 1, Undefined, 1e20), r6)
    ctor(c, f, "PlainTime", (2.0 ** 32,), r6)
    ctor(c, f, "PlainTime", (2.0 ** 32 + 1,), r6)
    for bag in ({"year": 2.0 ** 32 + 2020, "month": 1, "day": 1}, {"year": 1e20, "month": 1, "day": 1},
                {"year": 2020, "month": 2.0 ** 32 + 1, "day": 1}, {"year": 2020, "month": 1, "day": 2.0 ** 32 + 1}):
        from_bag(c, f, "PlainDate", bag, r6)
        from_bag(c, f, "PlainDate", bag, r6, {"overflow": "reject"})
    for bag in ({"hour": 2.0 ** 32 + 1}, {"hour": -(2.0 ** 32) + 1}):
        from_bag(c, f, "PlainTime", bag, r6)
        from_bag(c, f, "PlainTime", bag, r6, {"overflow": "reject"})
    from_bag(c, f, "PlainYearMonth", {"year": 2020, "month": 2.0 ** 32 + 1}, r6)
    method(c, f, "PlainDateTime", "2020-01-01T00:00", "with", [{"year": 2.0 ** 32 + 2020}], r6)
    method(c, f, "PlainDateTime", "2020-01-01T00:00", "with", [{"hour": 2.0 ** 32 + 5}], r6)
    method(c, f, "PlainDate", "2020-01-01", "with", [{"day": 1e20}], r6)

    r7 = "row R7: integer narrowing inside addition (the milestone's listed correction: it wraps or saturates)"
    big = 4294967295
    for kind, value in (("PlainDateTime", "2020-06-15T12:00"), ("PlainYearMonth", "2020-06"),
                        ("PlainDate", "2020-06-15"), ("ZonedDateTime", "2020-06-15T12:00[UTC]"),
                        ("PlainDate", "2020-06-15[u-ca=hebrew]"), ("PlainDate", "2020-06-15[u-ca=coptic]")):
        for name in ("add", "subtract"):
            method(c, f, kind, value, name, [{"years": big}], r7, ("sec-temporal-calendardateadd",))
    method(c, f, "PlainDateTime", "2020-06-15T12:00", "add", [{"years": big - 2020 - 1000}], r7)
    method(c, f, "PlainDateTime", "2020-06-15T12:00", "add", [{"months": big}], r7)
    method(c, f, "PlainDate", "2020-06-15", "add", [{"years": 273740}], r7 + " (the last representable year)")
    method(c, f, "PlainDate", "2020-06-15", "add", [{"years": 273741}], r7 + " (one past it)")
    for kind, value in (("PlainDate", "2020-01-01"), ("PlainDateTime", "2020-01-01T00:00"),
                        ("ZonedDateTime", "2020-01-01T00:00[UTC]")):
        for bag in ({"nanoseconds": 9e24}, {"microseconds": 9e21}, {"milliseconds": 9e18}):
            method(c, f, kind, value, "add", [bag], r7)
    round_duration(c, f, {"nanoseconds": 9e24}, {"smallestUnit": "second", "relativeTo": pd(2020, 1, 1)}, r7)
    round_duration(c, f, {"nanoseconds": 9e24}, {"largestUnit": "year", "relativeTo": pd(2020, 1, 1)}, r7)
    total_duration(c, f, {"nanoseconds": 9e24}, "day", pd(2020, 1, 1), r7)
    total_duration(c, f, {"nanoseconds": 9e24}, "day", Undefined, r7)

    r8 = "row R8: IsValidDuration (years, months, weeks below 2^32; the time with days below 2^53 seconds)"
    for args in ((2.0 ** 32 - 1,), (2.0 ** 32,), (-(2.0 ** 32) + 1,), (0, 2.0 ** 32), (0, 0, -(2.0 ** 32)),
                 (0, 0, 0, 104249991374), (0, 0, 0, 104249991375), (0, 0, 0, -104249991375),
                 (0, 0, 0, 0, 0, 0, 2.0 ** 53 - 1), (0, 0, 0, 0, 0, 0, 2.0 ** 53),
                 (0, 0, 0, 0, 0, 0, 2.0 ** 53 - 1, 999, 999, 999), (0, 0, 0, 0, 0, 0, 2.0 ** 53 - 1, 999, 999, 1000),
                 (0, 0, 0, 0, 0, 0, 0, 0, 0, 9e24), (0, 0, 0, 0, 0, 0, 0, 0, 0, 2.0 ** 53 * 1e9),
                 (0, 0, 0, 0, 2501999792983, 36), (0, 0, 0, 0, 2501999792983, 37), (1, -1),
                 (0, 0, 0, 0, 0, 0, 0, 0, 0, 1.5), (math.inf,), (0, 0, 0, 0, 0, 0, 0, 0, 0, -0.0)):
        ctor(c, f, "Duration", args, r8, ("sec-isvalidduration",))
    c.add(f, "Temporal.Duration (constructor), years is +0", ins(-0.0), "Object.is(new Temporal.Duration(-0).years, 0)",
          r8, ("sec-temporal.duration",))
    c.add(f, "Temporal.Duration.from (property bag), nanoseconds", ins({"nanoseconds": 2 ** 53 + 2}),
          "Temporal.Duration.from({nanoseconds: 9007199254740994}).nanoseconds", r8, ("sec-temporal.duration.from",))
    c.add(f, "Temporal.Duration.from (property bag), nanoseconds", ins({"nanoseconds": 9e24}),
          "Temporal.Duration.from({nanoseconds: 9e24}).nanoseconds", r8, ("sec-temporal.duration.from",))
    from_bag(c, f, "Duration", {"days": 104249991374, "seconds": 2 ** 53 - 1 - 104249991374 * 86400}, r8)
    from_bag(c, f, "Duration", {"days": 104249991374, "seconds": 2 ** 53 - 104249991374 * 86400}, r8)

    b1 = "row B1: BalanceTime and AddTime through PlainTime arithmetic (modulo a day, the date units ignored)"
    for value, name, arg in (("23:59:59.999999999", "add", {"nanoseconds": 1}),
                             ("00:00", "subtract", {"nanoseconds": 1}), ("12:00", "add", {"hours": 1e9}),
                             ("12:00", "subtract", {"hours": 2501999792983, "minutes": 36}),
                             ("12:00", "add", {"nanoseconds": 9e24}), ("12:00", "add", {"days": 3, "hours": 1}),
                             ("12:00", "add", {"years": 1, "months": 1, "weeks": 1}),
                             ("12:00", "add", {"minutes": 90, "seconds": 3600}), ("01:00", "subtract", {"minutes": 61}),
                             ("12:00", "add", "-PT36H")):
        method(c, f, "PlainTime", value, name, [arg], b1, ("sec-temporal-balancetime", "sec-temporal-addtime"))
    b2 = "row B2: BalanceISODateTime through PlainDateTime and Instant arithmetic"
    for value, name, arg in (("2020-12-31T23:00", "add", {"hours": 25}), ("2020-01-01T00:00", "subtract", {"minutes": 1}),
                             ("2020-01-01T12:00", "subtract", {"hours": 36}),
                             ("2020-02-28T12:00", "add", {"days": 1, "hours": 12}),
                             ("2020-01-31T23:00", "add", {"months": 1, "hours": 2}),
                             ("2020-01-01T00:00", "add", {"nanoseconds": 86400e9 * 366}),
                             ("2020-01-01T00:00", "add", {"seconds": 2 ** 53 - 1}),
                             ("+275760-09-13T00:00", "add", {"hours": 23, "minutes": 59}),
                             ("+275760-09-13T00:00", "add", {"days": 1}),
                             ("-271821-04-19T00:00:00.000000001", "subtract", {"nanoseconds": 1})):
        method(c, f, "PlainDateTime", value, name, [arg], b2, ("sec-temporal-balanceisodatetime",))
    for value, name, arg in (("2020-01-01T00:00Z", "add", {"hours": 48, "nanoseconds": 1}),
                             ("2020-01-01T00:00Z", "add", {"days": 1}),
                             ("2020-01-01T00:00Z", "subtract", {"hours": 2501999792983}),
                             ("+275760-09-13T00:00Z", "add", {"nanoseconds": 1}),
                             ("-271821-04-20T00:00Z", "subtract", {"nanoseconds": 1})):
        method(c, f, "Instant", value, name, [arg], b2, ("sec-temporal-adddurationtoinstant",))
    b3 = "row B3: AddISODate under both overflows (BalanceISOYearMonth, RegulateISODate, AddDaysToISODate)"
    reject = {"overflow": "reject"}
    for value, arg, options in (("2020-01-31", {"months": 1}, None), ("2020-01-31", {"months": 1}, reject),
                                ("2020-02-29", {"years": 1}, None), ("2020-02-29", {"years": 1}, reject),
                                ("2020-03-31", {"months": -1}, None), ("2020-01-31", {"months": 1, "days": 1}, None),
                                ("2019-01-31", {"years": 1, "months": 1}, None),
                                ("2020-01-01", {"weeks": 2, "days": 3}, None), ("2020-03-15", {"months": -15}, None),
                                ("2020-01-01", {"months": 1200000}, None), ("2020-01-01", {"days": 1e11}, None),
                                ("2020-01-01", {"years": 1e9 + 1}, None), ("2020-01-01", {"months": 1}, {"overflow": "bogus"})):
        method(c, f, "PlainDate", value, "add", [arg] + ([options] if options else []), b3,
               ("sec-temporal-calendardateadd", "sec-temporal-regulateisodate"))
    method(c, f, "PlainDate", "2020-01-01", "add", [{"months": 1}, None], b3 + " (options null)")
    method(c, f, "PlainDateTime", "2020-01-31T10:00", "add", [{"months": 1}, reject], b3)
    method(c, f, "PlainDateTime", "2020-01-31T10:00", "add", [{"months": 1}], b3)
    method(c, f, "ZonedDateTime", "2020-01-31T12:00[UTC]", "add", [{"months": 1}, reject], b3)
    b4 = "row B4: RegulateISODate and RegulateTime from property bags, constrain and reject"
    for kind, bag, options in (("PlainDate", {"year": 2021, "month": 2, "day": 31}, None),
                               ("PlainDate", {"year": 2021, "month": 13, "day": 1}, None),
                               ("PlainDate", {"year": 2021, "month": 2, "day": 31}, reject),
                               ("PlainDate", {"year": 2021, "month": 0, "day": 1}, None),
                               ("PlainDate", {"year": 2021, "month": 1, "day": 0}, None),
                               ("PlainTime", {"hour": 25, "minute": 61, "second": 61, "millisecond": 1000}, None),
                               ("PlainTime", {"hour": 25}, reject), ("PlainTime", {"hour": -1, "minute": -1}, None),
                               ("PlainTime", {"hour": -1}, reject),
                               ("PlainTime", {"microsecond": 1000, "nanosecond": 1000}, None),
                               ("PlainDateTime", {"year": 2020, "month": 1, "day": 1, "second": 60}, None),
                               ("PlainDateTime", {"year": 2020, "month": 1, "day": 1, "second": 60}, reject),
                               ("PlainYearMonth", {"year": 2020, "month": 13}, None),
                               ("PlainYearMonth", {"year": 2020, "month": 13}, reject)):
        from_bag(c, f, kind, bag, b4, options, ("sec-temporal-regulateisodate", "sec-temporal-regulatetime"))
    method(c, f, "PlainDateTime", "2020-02-29T12:00", "with", [{"year": 2021}], b4)
    method(c, f, "PlainDateTime", "2020-02-29T12:00", "with", [{"year": 2021}, reject], b4)
    method(c, f, "PlainTime", "12:00", "with", [{"minute": 75}], b4)
    b5 = "row B5: addition at the representable limits"
    for value, name, arg in (("+275760-09-13", "add", {"days": 1}), ("-271821-04-19", "subtract", {"days": 1}),
                             ("1970-01-01", "add", {"days": 100000000}), ("1970-01-01", "add", {"days": 100000001}),
                             ("-271821-04-19", "add", {"days": 0}), ("+275760-09-13", "add", {"months": 0})):
        method(c, f, "PlainDate", value, name, [arg], b5, ("sec-temporal-isodatewithinlimits",))
    for value, name, arg in (("+275760-09", "add", {"months": 1}), ("-271821-04", "add", {"months": 0}),
                             ("-271821-05", "subtract", {"months": 1}), ("+275760-08", "add", {"months": 1}),
                             ("-271821-06", "subtract", {"months": 2}), ("-271821-05", "add", {"months": -1}),
                             ("-271820-04", "subtract", {"years": 1}), ("-271820-04", "subtract", {"months": 12})):
        method(c, f, "PlainYearMonth", value, name, [arg], b5, ("sec-temporal-adddurationtoyearmonth",))
    for value, arg in ((Raw(f"new Temporal.ZonedDateTime({MAX_NS}n, \"UTC\")", f"bigint:{MAX_NS}", S("UTC")),
                        {"nanoseconds": 1}),
                       (Raw(f"new Temporal.ZonedDateTime({MAX_NS - 1}n, \"UTC\")", f"bigint:{MAX_NS - 1}", S("UTC")),
                        {"nanoseconds": 1}), ("+275760-09-12T12:00[UTC]", {"days": 1}),
                       ("+275760-09-12T00:00[UTC]", {"days": 1})):
        method(c, f, "ZonedDateTime", value, "add", [arg], b5)
    b6 = "row B6: AddZonedDateTime on New York's 2026 wall clock (the date part on the wall, the time on the timeline)"
    ny = "[America/New_York]"
    for value, name, arg, options in (("2026-03-07T02:30-05:00", "add", {"days": 1}, None),
                                      ("2026-03-07T02:30-05:00", "add", {"hours": 24}, None),
                                      ("2026-03-07T02:30-05:00", "add", {"days": 1}, reject),
                                      ("2026-11-01T00:30-04:00", "add", {"days": 1}, None),
                                      ("2026-11-01T00:30-04:00", "add", {"hours": 24}, None),
                                      ("2026-10-31T01:30-04:00", "add", {"days": 1}, None),
                                      ("2026-11-01T01:30-05:00", "subtract", {"days": 1}, None),
                                      ("2026-03-08T12:00-04:00", "subtract", {"days": 1, "hours": 12}, None),
                                      ("2026-01-31T02:30-05:00", "add", {"months": 1, "weeks": 1}, None)):
        method(c, f, "ZonedDateTime", value + ny, name, [arg] + ([options] if options else []), b6,
               ("sec-temporal-adddurationtozoneddatetime",))
    b7 = "row B7: a PlainDate adds the time part as whole days, truncated toward zero"
    for value, name, arg in (("2020-01-01", "add", {"hours": 47}), ("2020-01-01", "add", {"hours": 48}),
                             ("2020-01-01", "subtract", {"hours": 25}), ("2020-01-01", "subtract", {"hours": 23}),
                             ("2020-01-01", "add", {"nanoseconds": 86400e9 - 1}),
                             ("2020-01-01", "add", {"days": 1, "hours": 24}),
                             ("2020-01-31", "add", {"months": 1, "hours": 24}),
                             ("2020-01-01", "add", {"seconds": 2 ** 53 - 1}),
                             ("2020-01-01", "add", {"hours": 2501999792983})):
        method(c, f, "PlainDate", value, name, [arg], b7, ("sec-temporal-adddurationtodate",))
    b8 = "row B8: AddDurationToYearMonth, years and months from the first of the month"
    for name, arg, options in (("subtract", {"months": 1}, None), ("add", {"years": 1, "months": 13}, None),
                               ("add", {"days": 1}, None), ("add", {"weeks": 1}, None), ("add", {"hours": 1}, None),
                               ("add", {"days": 0, "months": 1}, None), ("add", {"months": 1}, reject),
                               ("add", {"months": -1200000}, None), ("add", {"days": -1}, None)):
        method(c, f, "PlainYearMonth", "2020-01", name, [arg] + ([options] if options else []), b8,
               ("sec-temporal-adddurationtoyearmonth",))
    b9 = "row B9: RoundTime, RoundISODateTime and RoundTemporalInstant, the rounding of points"
    for m in MODES:
        method(c, f, "PlainTime", "12:30", "round", [{"smallestUnit": "hour", "roundingMode": m}], b9)
        method(c, f, "Instant", Raw("Temporal.Instant.fromEpochNanoseconds(-1500000000n)", "bigint:-1500000000"), "round",
               [{"smallestUnit": "second", "roundingMode": m}], b9)
        method(c, f, "PlainDateTime", "2020-12-31T12:00", "round", [{"smallestUnit": "day", "roundingMode": m}], b9)
    for kind, value, options in (
            ("PlainTime", "23:59:59.5", {"smallestUnit": "second"}),
            ("PlainTime", "13:00", {"smallestUnit": "hour", "roundingIncrement": 12, "roundingMode": "ceil"}),
            ("PlainTime", "13:00", {"smallestUnit": "hour", "roundingIncrement": 5}),
            ("PlainDateTime", "2020-12-31T23:59:59.5", {"smallestUnit": "second"}),
            ("PlainDateTime", "2020-12-31T12:00", {"smallestUnit": "day", "roundingIncrement": 2}),
            ("PlainDateTime", "+275760-09-13T12:00", {"smallestUnit": "day"}),
            ("PlainDateTime", "+275760-09-13T11:59:59.999999999", {"smallestUnit": "day"}),
            ("PlainDateTime", "-271821-04-19T00:00:00.000000001", {"smallestUnit": "day", "roundingMode": "floor"}),
            ("PlainDateTime", "-271821-04-19T00:00:00.000000001", {"smallestUnit": "microsecond", "roundingMode": "floor"}),
            ("Instant", Raw(f"Temporal.Instant.fromEpochNanoseconds({MAX_NS}n)", f"bigint:{MAX_NS}"),
             {"smallestUnit": "hour", "roundingMode": "ceil"}),
            ("Instant", Raw(f"Temporal.Instant.fromEpochNanoseconds({MAX_NS - 1}n)", f"bigint:{MAX_NS - 1}"),
             {"smallestUnit": "hour", "roundingIncrement": 24}),
            ("Instant", "2020-01-01T12:00Z", {"smallestUnit": "minute", "roundingIncrement": 1440}),
            ("Instant", "2020-01-01T12:00Z", {"smallestUnit": "minute", "roundingIncrement": 1441}),
            ("ZonedDateTime", "2026-03-08T01:59:59.5-05:00" + ny, {"smallestUnit": "second"}),
            ("ZonedDateTime", "2026-11-01T01:59:59.5-04:00" + ny, {"smallestUnit": "second"}),
            ("ZonedDateTime", "2026-11-01T01:30-05:00" + ny, {"smallestUnit": "hour"}),
            ("ZonedDateTime", "+275760-09-13T00:00[UTC]", {"smallestUnit": "day"}),
            ("ZonedDateTime", "+275760-09-12T12:00[UTC]", {"smallestUnit": "day"})):
        method(c, f, kind, value, "round", [options], b9, ("sec-temporal-roundisodatetime",))


# --------------------------------------------------------------------------------------------------
# Durations: sign and shape, the largest unit, addition and compare (rows D1 to D4).

def durations(c: Corpus) -> None:
    f = "durations"
    d1 = "row D1: DurationSign, blank, negation (zeros stay +0), abs, with"
    c.add(f, "Temporal.Duration.prototype.sign", ins("-PT0.000000001S"),
          'Temporal.Duration.from("-PT0.000000001S").sign', d1, ("sec-durationsign",))
    c.add(f, "Temporal.Duration.prototype.sign", ins("PT0S"), 'Temporal.Duration.from("PT0S").sign', d1,
          ("sec-durationsign",))
    c.add(f, "Temporal.Duration.prototype.blank", ins("PT0S"), 'Temporal.Duration.from("PT0S").blank', d1,
          ("sec-durationsign",))
    method(c, f, "Duration", "P1D", "negated", [], d1)
    c.add(f, "Temporal.Duration.prototype.negated, hours is +0", ins("P1D"),
          'Object.is(Temporal.Duration.from("P1D").negated().hours, 0)', d1,
          ("sec-temporal-createnegatedtemporalduration",))
    c.add(f, "Temporal.Duration.prototype.negated, years is +0", ins("PT0S"),
          'Object.is(Temporal.Duration.from("PT0S").negated().years, 0)', d1,
          ("sec-temporal-createnegatedtemporalduration",))
    method(c, f, "Duration", "-P1DT1H", "abs", [], d1)
    method(c, f, "Duration", "P1D", "with", [{"hours": -1}], d1)
    method(c, f, "Duration", "P1D", "with", [{"days": -1}], d1, render=".sign")
    method(c, f, "Duration", "P1D", "with", [Raw("{}", 'str:"{}"')], d1 + " (an empty bag)")

    d2 = "row D2: DefaultTemporalLargestUnit and TemporalDurationFromInternal through addition"
    for one, two, name in (({"minutes": 30}, {"minutes": 90}, "add"), ({"hours": 1}, {"minutes": 90}, "add"),
                           ({"days": 1}, {"hours": 24}, "add"), ({"days": 1}, {"hours": 25}, "add"),
                           ({"seconds": 59}, {"milliseconds": 1001}, "add"),
                           ({"microseconds": 1}, {"nanoseconds": 2000}, "add"), ({"hours": 1}, {"hours": 2}, "subtract"),
                           ({"days": 1}, {"days": 1}, "subtract"),
                           ({"seconds": 2 ** 53 - 1}, {"nanoseconds": 999999999}, "add"),
                           ({"seconds": 2 ** 53 - 1}, {"seconds": 1}, "add"),
                           ({"days": 104249991374}, {"days": 1}, "add"), ({"days": 104249991374}, {"hours": 1}, "add")):
        method(c, f, "Duration", one, name, [two], d2, ("sec-temporal-adddurations",))
    for one, two, prop in (({"nanoseconds": 2 ** 53 - 1}, {"nanoseconds": 2}, "nanoseconds"),
                           ({"nanoseconds": 2 ** 53 - 1}, {"nanoseconds": 4}, "nanoseconds"),
                           ({"milliseconds": 2 ** 53 - 1}, {"milliseconds": 2 ** 53 - 1}, "milliseconds")):
        method(c, f, "Duration", one, "add", [two], d2, ("sec-temporal-adddurations",), render="." + prop)

    d3 = ("row D3: Duration.prototype.add and subtract with calendar units (the milestone's listed correction):"
          " AddDurations refuses a calendar unit, and add takes one parameter")
    for one, two, name in (({"years": 1}, {"months": 1}, "add"), ({"weeks": 1}, {"days": 1}, "add"),
                           ({"hours": 1}, {"months": 1}, "add"), ({"years": 1}, {"years": -1}, "add"),
                           ({"years": 1}, {"months": 1}, "subtract"), ({"months": 1}, {"months": 1}, "subtract")):
        method(c, f, "Duration", one, name, [two], d3, ("sec-temporal-adddurations",))
    for name in ("add", "subtract"):
        for second in ({"relativeTo": "2020-01-01"}, {"relativeTo": zdt("2020-01-01T00:00[UTC]")}):
            method(c, f, "Duration", {"years": 1}, name, [{"months": 1}, second], d3, ("sec-temporal-adddurations",))
        for second in (5, None, {"relativeTo": "2020-01-01"}):
            method(c, f, "Duration", {"hours": 1}, name, [{"hours": 1}, second], d3, ("sec-temporal-adddurations",))
    c.add(f, "Temporal.Duration.prototype.add, the second argument's reads", ins({"hours": 1}, {"hours": 1}),
          '(function (reads) { var r = Temporal.Duration.from({hours: 1}).add({hours: 1}, {get relativeTo() '
          '{ reads.push("relativeTo"); return undefined; }}); return r.toString() + ";" + reads.join(); })([])',
          d3 + " (a getter on a second argument runs not at all)", ("sec-temporal.duration.prototype.add",))
    for name in ("add", "subtract"):
        c.add(f, "Function.prototype.length", ins(f"Temporal.Duration.prototype.{name}"),
              f"Temporal.Duration.prototype.{name}.length", d3 + " (its length is 1)",
              (f"sec-temporal.duration.prototype.{name}",))

    d4 = "row D4: Temporal.Duration.compare, with DateDurationDays (no date-time limit is involved)"
    spring = zdt("2026-03-08T00:00-05:00[America/New_York]")

    def compare(one, two, relative_to=Undefined, note=d4):
        tail = "" if relative_to is Undefined else f", {{relativeTo: {js(relative_to)}}}"
        c.add(f, "Temporal.Duration.compare", ins(one, two, relative_to),
              f"Temporal.Duration.compare({js(one)}, {js(two)}{tail})", note, ("sec-temporal.duration.compare",))

    compare({"hours": 24}, {"days": 1})
    compare({"days": 1}, {"hours": 25})
    compare({"months": 1}, {"days": 30})
    compare({"months": 1}, {"days": 30}, pd(2020, 2, 1))
    compare({"months": 1}, {"days": 30}, pd(2020, 1, 1))
    compare({"days": 1}, {"hours": 24}, spring)
    compare({"days": 1}, {"hours": 23}, spring)
    compare({"months": 1}, {"months": 1})
    compare({"months": 1}, {"months": 1}, "bad")
    compare({"years": 1, "seconds": 9e15}, {"years": 1}, pd(2020, 1, 1))
    compare({"years": 1, "seconds": 9e15}, {"years": 1, "seconds": 9e15 - 1}, pd(2020, 1, 1))
    compare({"years": 1, "seconds": 9007199254740991 - 31622399}, {"years": 1}, pd(2020, 1, 1))
    compare({"years": 1, "seconds": 9007199254740991 - 31622400}, {"years": 1}, pd(2020, 1, 1))
    compare({"years": 1, "seconds": 2 ** 53 - 1}, {"years": 2}, pd(2020, 1, 1))
    compare({"years": 1, "hours": 1}, {"years": 1}, pd(275759, 1, 1))
    compare({"years": 1}, {"days": 366}, pd(275759, 9, 13))
    compare({"years": 1, "days": 1}, {"years": 1}, zdt("+275759-09-12T00:00[UTC]"))
    compare({"days": 200000000}, {"days": 1}, pd(2020, 1, 1))
    compare({"weeks": 1}, {"days": 7}, pd(2020, 1, 1))
    compare({"years": 1}, {"days": 365}, "2020-01-01[u-ca=hebrew]", d4 + "; a relativeTo in another calendar (row D11)")
    compare({"months": 1}, {"days": 29}, Raw('Temporal.PlainDate.from({year: 5780, monthCode: "M05", day: 1, '
                                             'calendar: "hebrew"})', S("hebrew"), N(5780), S("M05"), N(1)),
            d4 + "; a relativeTo in another calendar (row D11)")


# --------------------------------------------------------------------------------------------------
# Rounding: every mode and increment, nudging and bubbling, the shifted window, zoned relativeTo, totals and other
# calendars (rows D5 to D12).

UNIT_NS = {"day": 86400 * 10 ** 9, "hour": 3600 * 10 ** 9, "minute": 60 * 10 ** 9, "second": 10 ** 9,
           "millisecond": 10 ** 6, "microsecond": 10 ** 3, "nanosecond": 1}
SUBSECOND = (1, 2, 4, 5, 8, 10, 20, 25, 40, 50, 100, 125, 200, 250, 500)
INCREMENTS = {"day": (1, 2, 3, 5, 7, 10), "hour": (1, 2, 3, 4, 6, 8, 12), "minute": (1, 2, 3, 4, 5, 6, 10, 12, 15, 20, 30),
              "second": (1, 2, 3, 4, 5, 6, 10, 12, 15, 20, 30), "millisecond": SUBSECOND, "microsecond": SUBSECOND,
              "nanosecond": SUBSECOND}


def time_duration_string(ns: int) -> str:
    """An ISO duration string of days and time for a signed count of nanoseconds, days being 24 hours."""
    sign = "-" if ns < 0 else ""
    ns = abs(ns)
    days, ns = divmod(ns, UNIT_NS["day"])
    hours, ns = divmod(ns, UNIT_NS["hour"])
    minutes, ns = divmod(ns, UNIT_NS["minute"])
    seconds, fraction = divmod(ns, UNIT_NS["second"])
    out = f"{sign}P" + (f"{days}D" if days else "")
    time = (f"{hours}H" if hours else "") + (f"{minutes}M" if minutes else "")
    if seconds or fraction:
        time += f"{seconds}" + (f".{fraction:09d}".rstrip("0") if fraction else "") + "S"
    if time:
        out += "T" + time
    return out if out not in ("P", "-P") else "PT0S"


def rounding_values(q: int) -> list[int]:
    """Values in (3q, 4q): the tie, where q is even, and one past it; or 3q itself where nothing lies between."""
    values = []
    if q % 2 == 0:
        values.append(3 * q + q // 2)
    above = 3 * q + q // 2 + max(1, q // 4)
    if above < 4 * q:
        values.append(above)
    return values or [3 * q]


def rounding(c: Corpus) -> None:
    f = "rounding"
    for unit, increments in INCREMENTS.items():
        for increment in increments:
            for value in rounding_values(increment * UNIT_NS[unit]):
                for sign in (1, -1):
                    s = time_duration_string(sign * value)
                    tie = value - 3 * increment * UNIT_NS[unit] == increment * UNIT_NS[unit] // 2 and \
                        increment * UNIT_NS[unit] % 2 == 0
                    for mode in MODES:
                        round_duration(c, f, s, {"smallestUnit": unit, "roundingIncrement": increment,
                                                 "roundingMode": mode},
                                       f"rows D5 and B9's modes: {unit} increment {increment}, "
                                       f"{'a tie' if tie else 'off a tie'}, {mode}"
                                       + (" (days of 24 hours, no relativeTo)" if unit == "day" else ""),
                                       ("sec-temporal-roundtimedurationtoincrement",))

    calendar_units = {"year": ((1, 2, 5), (pd(2020, 1, 1), pd(2020, 2, 29))),
                      "month": ((1, 2, 3, 6, 12), (pd(2020, 1, 1), pd(2020, 1, 31))),
                      "week": ((1, 2, 4), (pd(2020, 1, 1),)),
                      "day": ((1, 2, 7), (pd(2020, 1, 1),))}
    for unit, (increments, origins) in calendar_units.items():
        for increment in increments:
            for origin in origins:
                for label, value in calendar_values(unit, increment):
                    for sign in (1, -1):
                        bag = {k: sign * v for k, v in value.items()}
                        for mode in MODES:
                            round_duration(c, f, bag, {"smallestUnit": unit, "largestUnit": unit,
                                                       "roundingIncrement": increment, "roundingMode": mode,
                                                       "relativeTo": origin},
                                           f"rows D6 and D12: {unit} increment {increment} relative to "
                                           f"{origin.source[len('new Temporal.PlainDate('):-1].replace(', ', '-')}, "
                                           f"{label}, {mode} (NudgeToCalendarUnit, NudgeToDayOrTime)",
                                           ("sec-temporal-roundrelativeduration", "sec-temporal-nudgetocalendarunit"))

    d5 = "row D5: round with no relativeTo"
    for m in MODES:
        for s in ("PT1H30M", "-PT1H30M", "PT2H30M"):
            round_duration(c, f, s, {"smallestUnit": "hour", "roundingMode": m}, d5)
        round_duration(c, f, "-PT1H20M", {"smallestUnit": "minute", "roundingIncrement": 30, "roundingMode": m}, d5)
    for value, options in (({"hours": 25, "minutes": 30}, {"largestUnit": "day"}),
                           ({"days": 3, "hours": 12}, {"smallestUnit": "day", "roundingIncrement": 2, "largestUnit": "day"}),
                           ({"minutes": 125}, {"smallestUnit": "minute", "roundingIncrement": 15}),
                           ({"hours": 47}, {"largestUnit": "day"}), ({"days": 2}, {"largestUnit": "hour"}),
                           ({"days": 2}, {"largestUnit": "auto", "smallestUnit": "hour"}), ({"hours": 1}, {}),
                           ({"hours": 1}, {"largestUnit": "hour", "smallestUnit": "day"}),
                           ({"hours": 1}, {"smallestUnit": "hour", "roundingIncrement": 5}),
                           ({"hours": 1}, {"smallestUnit": "hour", "roundingIncrement": 24}),
                           ({"minutes": 1}, {"smallestUnit": "minute", "roundingIncrement": 60}),
                           ({"seconds": 1}, {"smallestUnit": "second", "roundingIncrement": 7}),
                           ({"milliseconds": 1}, {"smallestUnit": "millisecond", "roundingIncrement": 1000}),
                           ({"nanoseconds": 1}, {"smallestUnit": "nanosecond", "roundingIncrement": 3}),
                           ({"hours": 1}, {"smallestUnit": "hour", "roundingIncrement": 0}),
                           ({"hours": 1}, {"smallestUnit": "hour", "roundingIncrement": 1e9 + 1}),
                           ({"hours": 1}, {"smallestUnit": "hour", "roundingIncrement": math.inf}),
                           ({"hours": 1}, {"smallestUnit": "hour", "roundingMode": "bogus"}),
                           ({"days": 1}, {"smallestUnit": "day", "roundingIncrement": 2, "largestUnit": "month",
                                          "relativeTo": pd(2020, 1, 1)}),
                           ({"days": 1}, {"smallestUnit": "month"}), ({"years": 1}, {"smallestUnit": "hour"}),
                           ({"weeks": 1}, {"largestUnit": "day"}), ({"days": 1}, {"largestUnit": "week"}),
                           ({"seconds": 2 ** 53 - 1}, {"smallestUnit": "hour", "roundingMode": "ceil"}),
                           ({"seconds": 2 ** 53 - 1}, {"smallestUnit": "day", "largestUnit": "day", "roundingMode": "floor"}),
                           ({"seconds": 2 ** 53 - 1}, {"smallestUnit": "day", "largestUnit": "day", "roundingMode": "ceil"}),
                           ({"days": 1e8}, {"smallestUnit": "day", "roundingIncrement": 1e9, "roundingMode": "ceil",
                                            "largestUnit": "day"}),
                           ({"days": 1e8 + 1}, {"smallestUnit": "day", "roundingIncrement": 2.5, "largestUnit": "day"})):
        round_duration(c, f, value, options, d5, ("sec-validatetemporalroundingincrement",))
    for arg in ("minutes", "hour"):
        c.add(f, "Temporal.Duration.prototype.round (string)", ins({"hours": 1, "seconds": 30}, arg),
              f'Temporal.Duration.from({{hours: 1, seconds: 30}}).round({lit(arg)}).toString()', d5,
              ("sec-temporal.duration.prototype.round",))
    c.add(f, "Temporal.Duration.prototype.round (no argument)", ins({"hours": 1}),
          "Temporal.Duration.from({hours: 1}).round().toString()", d5, ("sec-temporal.duration.prototype.round",))

    d6 = "row D6: round with a plain-date relativeTo (nudging, bubbling, weeks, increments, negative, the limits)"
    for value, options in (
            ({"days": 45}, {"largestUnit": "month", "relativeTo": pd(2020, 1, 31)}),
            ({"days": 15}, {"smallestUnit": "month", "relativeTo": pd(2020, 2, 1)}),
            ({"days": 15}, {"smallestUnit": "month", "relativeTo": pd(2020, 1, 1)}),
            ({"months": 11, "days": 30}, {"smallestUnit": "month", "largestUnit": "year", "relativeTo": pd(2020, 1, 1)}),
            ({"days": 40}, {"smallestUnit": "week", "largestUnit": "month", "relativeTo": pd(2020, 1, 1)}),
            ({"days": 10}, {"smallestUnit": "week", "relativeTo": pd(2020, 1, 1)}),
            ({"hours": 36}, {"largestUnit": "day", "relativeTo": pd(2020, 1, 1)}),
            ({"years": 1}, {"largestUnit": "day", "relativeTo": pd(2020, 1, 1)}),
            ({"years": 1}, {"largestUnit": "day", "relativeTo": pd(2021, 1, 1)}),
            ({"months": 1}, {"smallestUnit": "day", "relativeTo": pd(2020, 1, 31)}),
            ({"months": -1, "days": -15}, {"smallestUnit": "month", "relativeTo": pd(2020, 3, 31)}),
            ({"weeks": 3}, {"largestUnit": "month", "relativeTo": pd(2020, 2, 15)}),
            ({"days": 31}, {"smallestUnit": "month", "largestUnit": "month", "roundingIncrement": 2,
                            "relativeTo": pd(2020, 1, 1)}),
            ({"days": 30}, {"smallestUnit": "month", "largestUnit": "month", "roundingIncrement": 2,
                            "relativeTo": pd(2020, 1, 1)}),
            ({"months": 23, "days": 30, "hours": 23, "minutes": 59, "seconds": 59, "milliseconds": 999,
              "microseconds": 999, "nanoseconds": 999},
             {"smallestUnit": "microsecond", "largestUnit": "year", "relativeTo": pd(2020, 1, 1)}),
            ({"days": -1, "hours": -12}, {"smallestUnit": "day", "relativeTo": pd(2020, 1, 1)}),
            ({"years": 1, "months": 6}, {"smallestUnit": "year", "roundingMode": "halfEven", "relativeTo": pd(2020, 1, 1)}),
            ({"years": 2, "months": 6}, {"smallestUnit": "year", "roundingMode": "halfEven", "relativeTo": pd(2020, 1, 1)}),
            ({"days": 366}, {"largestUnit": "year", "relativeTo": Raw('Temporal.PlainDateTime.from("2020-01-01T12:00")',
                                                                         S("2020-01-01T12:00"))}),
            ({"days": 1}, {"largestUnit": "year", "relativeTo": pd(-271821, 4, 19)}),
            ({"days": 1}, {"largestUnit": "year", "relativeTo": pd(-271821, 4, 20)}),
            ("PT0S", {"largestUnit": "year", "relativeTo": pd(-271821, 4, 19)}),
            ({"days": 1}, {"largestUnit": "year", "relativeTo": pd(275760, 9, 12)}),
            ({"days": 1}, {"smallestUnit": "year", "roundingMode": "ceil", "relativeTo": pd(275760, 9, 12)}),
            ({"days": -1}, {"smallestUnit": "year", "roundingMode": "ceil", "relativeTo": pd(-271821, 4, 20)}),
            ({"days": -1}, {"smallestUnit": "year", "roundingMode": "floor", "relativeTo": pd(-271821, 4, 20)}),
            ({"years": 1}, {"smallestUnit": "month", "largestUnit": "year", "roundingMode": "halfExpand",
                            "relativeTo": pd(2020, 1, 1)}),
            ({"years": 1}, {"smallestUnit": "week", "largestUnit": "year", "roundingMode": "ceil",
                            "relativeTo": pd(2020, 1, 1)}),
            ({"years": 2}, {"smallestUnit": "week", "largestUnit": "year", "roundingMode": "ceil",
                            "relativeTo": pd(2020, 1, 1)}),
            ({"months": 1}, {"smallestUnit": "week", "largestUnit": "month", "roundingMode": "expand",
                             "relativeTo": pd(2020, 1, 1)}),
            ({"years": 1, "days": 1}, {"smallestUnit": "month", "largestUnit": "year", "roundingMode": "ceil",
                                       "relativeTo": pd(2020, 1, 1)})):
        round_duration(c, f, value, options, d6, ("sec-temporal-roundrelativeduration",))

    for m in MODES:
        for origin in (pd(2020, 1, 31), pd(2020, 1, 1)):
            round_duration(c, f, "P1Y2M3W4DT5H6M7.008009010S", {"smallestUnit": "month", "roundingMode": m,
                                                                 "relativeTo": origin},
                           f"row D6: the retained dataset's duration rounded to months, bubbled, {m} (from 2020-01-31 its"
                           " fifteen months end on a clamped day)", ("sec-temporal-roundrelativeduration",))

    d78 = ("rows D7 and D8: from 2019-03-31 the destination lies past the first month window and inside the shifted"
           " one; rounded to months")
    shifted = {"months": 10, "days": 29, "hours": 12}
    for m in MODES:
        for largest in ("year", "month"):
            round_duration(c, f, shifted, {"smallestUnit": "month", "largestUnit": largest, "roundingMode": m,
                                           "relativeTo": pd(2019, 3, 31)}, d78 + f", largest {largest}, {m}",
                           ("sec-temporal-roundrelativeduration",))
        round_duration(c, f, shifted, {"smallestUnit": "month", "largestUnit": "year", "roundingMode": m,
                                       "relativeTo": zdt("2019-03-31T00:00[UTC]")},
                       d78 + f", a zoned relativeTo, {m}", ("sec-temporal-roundrelativeduration",))
        round_duration(c, f, {k: -v for k, v in shifted.items()},
                       {"smallestUnit": "month", "largestUnit": "year", "roundingMode": m, "relativeTo": pd(2020, 2, 29)},
                       d78 + f", the negated duration back from 2020-02-29, {m}", ("sec-temporal-roundrelativeduration",))
        round_duration(c, f, {"months": 10, "days": 30}, {"smallestUnit": "month", "largestUnit": "year",
                                                          "roundingMode": m, "relativeTo": pd(2019, 3, 31)},
                       d78 + f", a whole day further, {m}", ("sec-temporal-roundrelativeduration",))
        round_duration(c, f, {"months": 10, "days": 29}, {"smallestUnit": "month", "largestUnit": "year",
                                                          "roundingMode": m, "relativeTo": pd(2019, 3, 31)},
                       d78 + f", the destination at the shifted window's start, {m}",
                       ("sec-temporal-roundrelativeduration",))
        round_duration(c, f, {"months": 22, "days": 29, "hours": 12},
                       {"smallestUnit": "year", "largestUnit": "year", "roundingMode": m, "relativeTo": pd(2019, 3, 31)},
                       d78 + f", rounded to years, {m}", ("sec-temporal-roundrelativeduration",))
    for unit in ("month", "year"):
        total_duration(c, f, shifted, unit, pd(2019, 3, 31), d78 + ", the total")
        total_duration(c, f, shifted, unit, zdt("2019-03-31T00:00[UTC]"), d78 + ", the total")
    method(c, f, "PlainDate", "2019-03-31", "add", [{"years": 1}], d78 + ": a year from the origin")

    d9 = "row D9: round with a zoned relativeTo across New York's 2026 transitions (real day lengths)"
    spring, fall = zdt("2026-03-08T00:00-05:00[America/New_York]"), zdt("2026-11-01T00:00-04:00[America/New_York]")
    for value, options in (
            ({"hours": 23}, {"smallestUnit": "day", "relativeTo": spring}),
            ({"hours": 24}, {"largestUnit": "day", "relativeTo": spring}),
            ({"days": 1}, {"largestUnit": "hour", "relativeTo": spring}),
            ({"hours": 25}, {"largestUnit": "day", "relativeTo": fall}),
            ({"hours": 24}, {"largestUnit": "day", "relativeTo": fall}),
            ({"days": 1}, {"largestUnit": "hour", "relativeTo": fall}),
            ({"hours": 11, "minutes": 45}, {"smallestUnit": "day", "relativeTo": zdt("2026-03-07T12:00-05:00[America/New_York]")}),
            ({"hours": 12, "minutes": 15}, {"smallestUnit": "day", "relativeTo": zdt("2026-10-31T12:00-04:00[America/New_York]")}),
            ({"days": 45}, {"largestUnit": "month", "relativeTo": zdt("2020-01-31T00:00[UTC]")}),
            ({"hours": 30}, {"smallestUnit": "hour", "largestUnit": "day", "relativeTo": spring}),
            ({"hours": 22, "minutes": 40}, {"smallestUnit": "hour", "roundingIncrement": 2, "largestUnit": "day",
                                            "relativeTo": spring}),
            ({"days": 1, "hours": 1}, {"smallestUnit": "day", "largestUnit": "year", "relativeTo": spring}),
            ({"days": 1}, {"smallestUnit": "hour", "relativeTo": zdt("+275760-09-12T00:00[UTC]")}),
            ({"hours": 1}, {"smallestUnit": "hour", "largestUnit": "day", "relativeTo": zdt("+275760-09-12T12:00[UTC]")}),
            ("PT0S", {"largestUnit": "year", "relativeTo": zdt("+275760-09-13T00:00[UTC]")}),
            ({"hours": 1}, {"smallestUnit": "hour", "largestUnit": "year", "relativeTo": zdt("+275760-09-12T12:00[UTC]")})):
        round_duration(c, f, value, options, d9, ("sec-temporal-nudgetozonedtime",))
    for m in MODES:
        for value, origin in (({"hours": 12}, spring), ({"hours": 12}, fall), ({"hours": 11, "minutes": 30}, spring),
                              ({"hours": 12, "minutes": 30}, fall)):
            round_duration(c, f, value, {"smallestUnit": "day", "roundingMode": m, "relativeTo": origin},
                           d9 + f": a day of 23 or 25 hours, {m}", ("sec-temporal-nudgetocalendarunit",))

    d10 = "row D10: total, exact ratios rounded once"
    for value, unit, origin in (
            ({"hours": 1, "minutes": 30}, "hour", Undefined), ({"days": 45}, "month", pd(2020, 1, 1)),
            ({"days": 45}, "month", zdt("2020-01-01T00:00[UTC]")),
            ({"nanoseconds": 2 ** 53 - 1}, "second", Undefined),
            ({"seconds": 2 ** 53 - 1, "nanoseconds": 999999999}, "nanosecond", Undefined),
            ({"seconds": 2 ** 53 - 1, "nanoseconds": 999999999}, "day", Undefined),
            ({"days": 1}, "hour", spring), ({"hours": 12}, "day", spring), ({"hours": 12}, "day", fall),
            ({"months": 1}, "day", pd(2020, 2, 1)), ({"years": 1}, "week", pd(2020, 1, 1)),
            ({"months": 1}, "day", Undefined), ({"days": 1}, "day", Undefined), ({"days": 1}, "month", Undefined),
            ({"days": -45}, "month", pd(2020, 3, 15)), ({"days": 1}, "day", zdt("+275760-09-12T00:00:01+00:00[UTC]")),
            ({"hours": 1}, "day", zdt("+275760-09-12T12:00[UTC]")), ({"days": 1}, "day", pd(275760, 9, 12)),
            ({"years": 1}, "day", pd(2020, 2, 29)), ({"years": 4}, "year", pd(2020, 2, 29)),
            ({"months": 13}, "year", pd(2020, 1, 31)), ({"weeks": 5}, "month", pd(2020, 1, 31)),
            ({"seconds": 1}, "year", pd(2020, 1, 1)), ({"nanoseconds": 1}, "day", Undefined),
            ({"days": 1, "nanoseconds": 1}, "hour", Undefined)):
        total_duration(c, f, value, unit, origin, d10)
    for unit in ("year", "month", "week", "day", "hour", "minute", "second", "millisecond", "microsecond", "nanosecond"):
        total_duration(c, f, "P1Y2M3W4DT5H6M7.008009010S", unit, pd(2020, 1, 31), d10 + f": every unit, {unit}")
        total_duration(c, f, "-P1Y2M3W4DT5H6M7.008009010S", unit, pd(2020, 1, 31), d10 + f": every unit, {unit}")

    d11 = "row D11: round, total and compare relative to a date in another calendar"
    hebrew = Raw('Temporal.PlainDate.from("2020-01-01[u-ca=hebrew]")', S("2020-01-01[u-ca=hebrew]"))
    for value, options in (({"months": 1}, {"smallestUnit": "day", "relativeTo": hebrew}),
                           ({"days": 45}, {"largestUnit": "month", "relativeTo": hebrew}),
                           ({"months": 13}, {"largestUnit": "year", "relativeTo": Raw(
                               'Temporal.PlainDate.from({year: 5784, monthCode: "M01", day: 1, calendar: "hebrew"})',
                               S("hebrew"), N(5784), S("M01"), N(1))}),
                           ({"days": 45}, {"largestUnit": "month", "relativeTo": zdt("2020-01-01T00:00[UTC][u-ca=coptic]")}),
                           ({"days": 45}, {"largestUnit": "month", "relativeTo": {"year": 5780, "month": 4, "day": 1,
                                                                                    "calendar": "hebrew"}}),
                           ({"days": 45}, {"largestUnit": "month", "relativeTo": Raw(
                               'Temporal.PlainDate.from("2020-01-01[u-ca=gregory]")', S("2020-01-01[u-ca=gregory]"))}),
                           ({"days": 45}, {"largestUnit": "month", "relativeTo": Raw(
                               'Temporal.PlainDate.from("2020-01-01[u-ca=japanese]")', S("2020-01-01[u-ca=japanese]"))}),
                           ({"days": 400}, {"largestUnit": "year", "relativeTo": Raw(
                               'Temporal.PlainDate.from("2020-01-01[u-ca=islamic-civil]")',
                               S("2020-01-01[u-ca=islamic-civil]"))}),
                           ({"months": 2}, {"smallestUnit": "month", "roundingMode": "halfExpand", "relativeTo": Raw(
                               'Temporal.PlainDate.from("2020-09-11[u-ca=coptic]")', S("2020-09-11[u-ca=coptic]"))}),
                           ({"months": 12, "days": 3}, {"smallestUnit": "year", "roundingMode": "ceil", "relativeTo": Raw(
                               'Temporal.PlainDate.from("2020-09-11[u-ca=coptic]")', S("2020-09-11[u-ca=coptic]"))})):
        round_duration(c, f, value, options, d11, ("sec-temporal-roundrelativeduration",))
    total_duration(c, f, {"days": 45}, "month", hebrew, d11)
    total_duration(c, f, {"days": 45}, "month", Raw('Temporal.PlainDate.from("2020-01-01").withCalendar("islamic-civil")',
                                                    S("2020-01-01"), S("islamic-civil")), d11)
    total_duration(c, f, {"months": 13}, "year", Raw(
        'Temporal.PlainDate.from({year: 5784, monthCode: "M01", day: 1, calendar: "hebrew"})',
        S("hebrew"), N(5784), S("M01"), N(1)), d11)


def calendar_values(unit: str, q: int) -> list[tuple[str, dict]]:
    """Durations around the fourth window of q units: below and above half of it, the tie where it is fixed, the
    window's start exactly, and a quarter of the first window (where r1 is zero)."""
    sub = {"year": ("months", 12), "month": ("days", 30), "week": ("days", 7), "day": ("hours", 24)}[unit]
    plural = unit + "s"
    out = [("below half", {plural: 3 * q, sub[0]: max(1, q * sub[1] // 4)}),
           ("above half", {plural: 3 * q, sub[0]: q * sub[1] * 3 // 4})]
    if unit in ("week", "day") and q * sub[1] % 2 == 0:
        out.append(("a tie", {plural: 3 * q, sub[0]: q * sub[1] // 2}))
    out.append(("exact", {plural: 3 * q}))
    out.append(("in the first window", {sub[0]: max(1, q * sub[1] // 4)}))
    return out


# --------------------------------------------------------------------------------------------------
# Differences: until and since (rows F1 to F8).

def differences(c: Corpus) -> None:
    f1 = "row F1: GetDifferenceSettings (defaults, unit groups, disallowed units, increments)"
    for kind, a, b, options in (
            ("PlainDate", "2020-01-01", "2021-03-15", None), ("PlainDateTime", "2020-01-01T00:00", "2020-01-03T12:30", None),
            ("PlainTime", "01:00", "23:30:15", None), ("Instant", "2020-01-01T00:00Z", "2020-01-02T01:00:00.5Z", None),
            ("ZonedDateTime", "2020-01-01T00:00[UTC]", "2020-01-02T01:00[UTC]", None),
            ("PlainYearMonth", "2019-05", "2021-03", None),
            ("PlainDate", "2020-01-01", "2020-01-02", {"largestUnit": "hour"}),
            ("PlainDate", "2020-01-01", "2020-01-02", {"smallestUnit": "hour"}),
            ("PlainTime", "01:00", "02:00", {"largestUnit": "day"}),
            ("Instant", "2020-01-01T00:00Z", "2020-01-02T00:00Z", {"smallestUnit": "day"}),
            ("Instant", "2020-01-01T00:00Z", "2020-01-02T00:00Z", {"largestUnit": "day"}),
            ("PlainYearMonth", "2019-05", "2021-03", {"smallestUnit": "week"}),
            ("PlainYearMonth", "2019-05", "2021-03", {"largestUnit": "day"}),
            ("PlainDate", "2020-01-01", "2021-03-15", {"smallestUnit": "year", "largestUnit": "month"}),
            ("PlainTime", "01:00", "02:00", {"smallestUnit": "minute", "roundingIncrement": 7}),
            ("PlainTime", "01:00", "02:00", {"smallestUnit": "minute", "roundingIncrement": 60}),
            ("Instant", "2020-01-01T00:00Z", "2020-01-03T00:00Z", {"smallestUnit": "hour", "roundingIncrement": 24}),
            ("Instant", "2020-01-01T00:00Z", "2020-01-03T05:00Z", {"smallestUnit": "hour", "roundingIncrement": 12,
                                                                   "largestUnit": "hour"}),
            ("PlainDate", "2020-01-01", "2020-01-02", {"smallestUnit": "day", "roundingIncrement": 1e9}),
            ("PlainDate", "2020-01-01", "2020-01-02", {"roundingIncrement": 0}),
            ("PlainDate", "2020-01-01", "2020-01-02", {"roundingIncrement": 1e9 + 1}),
            ("PlainDate", "2020-01-01", "2020-01-02", {"roundingMode": "bogus"}),
            ("PlainDate", "2020-01-01", "2021-03-15", {"largestUnit": "auto", "smallestUnit": "month"}),
            ("ZonedDateTime", "2020-01-01T00:00[UTC]", "2020-01-03T12:00[UTC]", {"smallestUnit": "day"}),
            ("Instant", "2020-01-01T00:00Z", "2020-01-01T02:00Z", {"smallestUnit": "minute"}),
            ("ZonedDateTime", "2020-01-01T00:00[UTC]", "2020-01-02T00:00+01:00[Europe/Berlin]", {"largestUnit": "day"}),
            ("ZonedDateTime", "2020-01-01T00:00[UTC]", "2020-01-02T00:00+01:00[Europe/Berlin]", {"largestUnit": "hour"})):
        difference(c, kind, a, b, "until", options, f1, ("sec-temporal-getdifferencesettings",))
    c.add("differences", "Temporal.PlainDate.prototype.until, options not an object", ins("2020-01-01", "2020-01-02", 5),
          'Temporal.PlainDate.from("2020-01-01").until("2020-01-02", 5).toString()', f1,
          ("sec-temporal-getdifferencesettings",))
    difference(c, "PlainDate", "2020-01-01", Raw('Temporal.PlainDate.from("2020-02-01").withCalendar("gregory")',
                                                 S("2020-02-01"), S("gregory")), "until", None, f1 + "; another calendar")

    f2 = "row F2: PlainDate differences in every largest unit, anchored on the receiver across month ends"
    for a, b, name, largest in (("2020-01-31", "2020-03-01", "until", "month"), ("2020-03-01", "2020-01-31", "since", "month"),
                                ("2020-01-31", "2020-03-01", "since", "month"), ("2020-03-01", "2020-01-31", "until", "month"),
                                ("2020-01-01", "2020-03-01", "until", "week"), ("2019-02-28", "2020-02-29", "until", "year"),
                                ("2020-02-29", "2021-02-28", "until", "year"), ("2021-02-28", "2020-02-29", "until", "year"),
                                ("2020-01-29", "2020-02-28", "until", "month"), ("2019-01-29", "2019-03-01", "until", "month"),
                                ("-271821-04-19", "+275760-09-13", "until", "year"),
                                ("-271821-04-19", "+275760-09-13", "until", "day"),
                                ("+275760-09-13", "-271821-04-19", "since", "week"),
                                ("2020-01-01", "2020-01-01", "until", "year")):
        difference(c, "PlainDate", a, b, name, {"largestUnit": largest}, f2, ("sec-temporal-calendardateuntil",))

    f3 = "row F3: PlainDate differences rounded (modes, increments, the negated mode of since, bubbling, the limits)"
    for a, b, name, options in (
            ("2020-01-01", "2020-07-20", "until", {"smallestUnit": "month"}),
            ("2020-01-01", "2020-07-02", "until", {"smallestUnit": "year", "roundingMode": "halfExpand"}),
            ("2020-01-01", "2020-07-02", "until", {"smallestUnit": "year", "roundingMode": "halfTrunc"}),
            ("2020-01-17", "2020-01-01", "since", {"smallestUnit": "week", "roundingMode": "floor"}),
            ("2020-01-01", "2020-01-17", "since", {"smallestUnit": "week", "roundingMode": "floor"}),
            ("2020-01-01", "2020-01-17", "until", {"smallestUnit": "week", "roundingMode": "ceil"}),
            ("2020-01-17", "2020-01-01", "until", {"smallestUnit": "week", "roundingMode": "ceil"}),
            ("2020-01-01", "2020-03-19", "until", {"largestUnit": "month", "smallestUnit": "day", "roundingIncrement": 5,
                                                   "roundingMode": "halfExpand"}),
            ("2020-01-01", "2020-03-30", "until", {"largestUnit": "month", "smallestUnit": "day", "roundingIncrement": 5,
                                                   "roundingMode": "halfExpand"}),
            ("2020-01-01", "2020-04-15", "until", {"smallestUnit": "month", "roundingIncrement": 2}),
            ("2020-01-01", "2020-04-15", "until", {"smallestUnit": "month", "roundingIncrement": 2,
                                                   "roundingMode": "halfExpand"}),
            ("2020-01-01", "2021-12-20", "until", {"smallestUnit": "month", "largestUnit": "year",
                                                   "roundingMode": "halfExpand"}),
            ("2020-01-01", "2020-01-29", "until", {"smallestUnit": "week", "largestUnit": "month",
                                                   "roundingMode": "halfExpand"}),
            ("2020-01-31", "2020-02-29", "until", {"smallestUnit": "month"}),
            ("2020-01-31", "2020-02-29", "until", {"smallestUnit": "month", "roundingMode": "halfExpand"}),
            ("2020-01-01", "2020-01-02", "until", {"smallestUnit": "day", "roundingIncrement": 1e9, "roundingMode": "ceil"}),
            ("+275760-09-12", "+275760-09-13", "until", {"smallestUnit": "month", "roundingMode": "ceil"}),
            ("-271821-04-20", "-271821-04-19", "since", {"smallestUnit": "year", "roundingMode": "ceil"})):
        difference(c, "PlainDate", a, b, name, options, f3, ("sec-temporal-roundrelativeduration",))
    for m in MODES:
        for name in ("until", "since"):
            difference(c, "PlainDate", "2020-01-31", "2020-07-15", name, {"smallestUnit": "month", "roundingMode": m},
                       f3 + f": every mode, {m}", ("sec-temporal-roundrelativeduration",))
            difference(c, "PlainDate", "2020-01-01", "2020-01-18", name, {"smallestUnit": "week", "roundingMode": m},
                       f3 + f": every mode, {m}", ("sec-temporal-roundrelativeduration",))

    f4 = ("row F4: differences in another calendar with rounding options (the milestone's listed correction; Broiler.JS"
          " ignores them, or measures a PlainDateTime's months in ISO)")
    for kind, a, b, name, options in (
            ("PlainDate", "2020-01-01[u-ca=hebrew]", "2020-07-20[u-ca=hebrew]", "until", {"smallestUnit": "month",
                                                                                         "roundingMode": "halfExpand"}),
            ("PlainDate", "2020-01-01[u-ca=hebrew]", "2020-07-20[u-ca=hebrew]", "until", {"largestUnit": "month"}),
            ("PlainDate", "2020-01-01[u-ca=hebrew]", "2021-10-20[u-ca=hebrew]", "until", {"smallestUnit": "year",
                                                                                         "roundingMode": "halfExpand"}),
            ("PlainDate", "2020-01-01[u-ca=hebrew]", "2020-07-20[u-ca=hebrew]", "since", {"smallestUnit": "month",
                                                                                         "roundingMode": "halfExpand"}),
            ("PlainDate", "2020-01-01[u-ca=coptic]", "2020-03-20[u-ca=coptic]", "until", {"smallestUnit": "week",
                                                                                         "roundingMode": "ceil"}),
            ("PlainDate", "2020-01-01[u-ca=islamic-civil]", "2020-03-20[u-ca=islamic-civil]", "until",
             {"smallestUnit": "day", "roundingIncrement": 7, "roundingMode": "halfExpand"}),
            ("PlainDate", "2020-01-01[u-ca=gregory]", "2020-07-20[u-ca=gregory]", "until", {"smallestUnit": "month",
                                                                                           "roundingMode": "halfExpand"}),
            ("PlainDate", "2020-01-01[u-ca=hebrew]", "2020-01-16[u-ca=hebrew]", "until", {"smallestUnit": "month",
                                                                                         "roundingMode": "halfExpand"}),
            ("PlainDateTime", "2020-01-01T00:00[u-ca=hebrew]", "2020-01-03T13:00[u-ca=hebrew]", "until",
             {"smallestUnit": "day", "roundingMode": "halfExpand"}),
            ("PlainDateTime", "2020-01-01T00:00[u-ca=hebrew]", "2020-07-20T00:00[u-ca=hebrew]", "until",
             {"smallestUnit": "month", "roundingMode": "halfExpand"}),
            ("PlainDateTime", "2020-01-01T00:00[u-ca=hebrew]", "2020-01-03T13:20[u-ca=hebrew]", "until",
             {"largestUnit": "day", "smallestUnit": "hour", "roundingMode": "halfExpand"}),
            ("PlainDateTime", "2020-01-01T00:00[u-ca=hebrew]", "2020-01-16T00:00[u-ca=hebrew]", "until",
             {"smallestUnit": "month", "roundingMode": "halfExpand"}),
            ("PlainDateTime", "2020-01-01T00:00[u-ca=hebrew]", "2020-01-16T00:00[u-ca=hebrew]", "until",
             {"smallestUnit": "month", "largestUnit": "year", "roundingMode": "halfExpand"}),
            ("ZonedDateTime", "2020-01-01T00:00[UTC][u-ca=hebrew]", "2020-01-03T13:00[UTC][u-ca=hebrew]", "until",
             {"smallestUnit": "day", "roundingMode": "halfExpand"}),
            ("ZonedDateTime", "2020-01-01T00:00[UTC][u-ca=hebrew]", "2020-07-20T00:00[UTC][u-ca=hebrew]", "until",
             {"largestUnit": "month", "smallestUnit": "month", "roundingMode": "halfExpand"}),
            ("ZonedDateTime", "2020-01-01T00:00[UTC][u-ca=hebrew]", "2020-01-03T13:20[UTC][u-ca=hebrew]", "until",
             {"largestUnit": "day", "smallestUnit": "hour", "roundingMode": "halfExpand"})):
        difference(c, kind, a, b, name, options, f4, ("sec-temporal-calendardateuntil",))
    for m in MODES:
        difference(c, "PlainDate", "2020-01-01[u-ca=hebrew]", "2020-07-20[u-ca=hebrew]", "until",
                   {"smallestUnit": "month", "roundingMode": m}, f4 + f": every mode, {m}")
    hym = lambda y, code: Raw(f'Temporal.PlainYearMonth.from({{year: {y}, monthCode: {lit(code)}, calendar: "hebrew"}})',  # noqa: E731
                              S("hebrew"), N(y), S(code))
    difference(c, "PlainYearMonth", hym(5780, "M04"), hym(5781, "M03"), "until",
               {"smallestUnit": "year", "roundingMode": "halfExpand"}, f4)
    difference(c, "PlainYearMonth", hym(5780, "M04"), hym(5781, "M03"), "until", None, f4)

    f5 = "rows F5 and D8: PlainDateTime differences (the borrowed day, a largest hour, rounding, month ends)"
    for a, b, name, options in (
            ("2020-01-01T12:00", "2020-01-03T06:00", "until", None), ("2020-01-03T06:00", "2020-01-01T12:00", "since", None),
            ("2020-01-03T06:00", "2020-01-01T12:00", "until", None),
            ("2020-01-01T12:00", "2020-01-03T06:00", "until", {"largestUnit": "hour"}),
            ("2020-01-01T12:00", "2020-01-03T06:00", "until", {"smallestUnit": "day", "roundingMode": "halfExpand"}),
            ("2020-01-31T12:00", "2020-03-01T06:00", "until", {"largestUnit": "month"}),
            ("2019-01-01T00:00", "2020-12-31T23:59:59.999999999", "until", {"smallestUnit": "microsecond",
                                                                            "largestUnit": "year",
                                                                            "roundingMode": "halfExpand"}),
            ("2020-01-01T00:00", "2020-01-01T23:59:59.5", "until", {"smallestUnit": "second", "largestUnit": "day",
                                                                    "roundingMode": "halfExpand"}),
            ("2020-01-01T00:00", "2020-01-01T23:30", "until", {"smallestUnit": "hour", "largestUnit": "day",
                                                               "roundingIncrement": 3, "roundingMode": "ceil"}),
            ("2020-01-01T00:00", "2020-01-08T12:00", "until", {"smallestUnit": "week", "roundingMode": "halfExpand"}),
            ("-271821-04-19T00:00:00.000000001", "+275760-09-13T23:59:59.999999999", "until", {"largestUnit": "nanosecond"}),
            ("-271821-04-19T00:00:00.000000001", "+275760-09-13T23:59:59.999999999", "until", {"largestUnit": "year"})):
        difference(c, "PlainDateTime", a, b, name, options, f5, ("sec-temporal-differenceisodatetime",))
    for m in MODES:
        for name, a, b in (("until", "2019-03-31T00:00", "2020-02-29T12:00"), ("since", "2020-02-29T12:00", "2019-03-31T00:00")):
            difference(c, "PlainDateTime", a, b, name, {"smallestUnit": "month", "largestUnit": "year", "roundingMode": m},
                       f5 + f": D8's shifted window, {m}", ("sec-temporal-nudgetocalendarunit",))
            difference(c, "ZonedDateTime", a + "[UTC]", b + "[UTC]", name,
                       {"smallestUnit": "month", "largestUnit": "year", "roundingMode": m},
                       "rows F7 and D8: ZonedDateTime, D8's shifted window, " + m, ("sec-temporal-nudgetocalendarunit",))
        difference(c, "PlainDate", "2019-03-31", "2020-02-29", "until",
                   {"smallestUnit": "month", "largestUnit": "year", "roundingMode": m},
                   "rows F3 and D8: the same dates with no time, which reach no shifted window, " + m)
    difference(c, "ZonedDateTime", "2019-03-31T00:00[UTC]", "2020-02-29T12:00[UTC]", "until", {"smallestUnit": "year"},
               "rows F7 and D8: rounded to years")

    f6 = "row F6: PlainTime and Instant differences (time units only)"
    for kind, a, b, name, options, render in (
            ("PlainTime", "10:00", "08:30", "until", None, ".toString()"),
            ("PlainTime", "10:00", "08:29:31", "since", {"smallestUnit": "minute", "roundingMode": "halfExpand"}, ".toString()"),
            ("PlainTime", "10:00", "08:29:31", "since", {"largestUnit": "minute"}, ".toString()"),
            ("PlainTime", "08:29:31", "10:00", "since", {"smallestUnit": "minute", "roundingMode": "floor"}, ".toString()"),
            ("PlainTime", "00:00", "15:00", "until", {"smallestUnit": "hour", "roundingIncrement": 6,
                                                      "roundingMode": "halfEven"}, ".toString()"),
            ("PlainTime", "00:00", "21:00", "until", {"smallestUnit": "hour", "roundingIncrement": 6,
                                                      "roundingMode": "halfEven"}, ".toString()"),
            ("PlainTime", "00:00", "23:59:59.999999999", "until", {"largestUnit": "nanosecond"}, ".toString()"),
            ("Instant", "-271821-04-20T00:00Z", "+275760-09-13T00:00Z", "until", {"largestUnit": "hour"}, ".toString()"),
            ("Instant", "-271821-04-20T00:00Z", "+275760-09-13T00:00Z", "until", {"largestUnit": "nanosecond"}, ".toString()"),
            ("Instant", "-271821-04-20T00:00Z", "+275760-09-13T00:00Z", "until", {"largestUnit": "microsecond"},
             ".microseconds"),
            ("Instant", "2020-01-01T00:00Z", "2020-01-01T01:29Z", "until", {"smallestUnit": "hour",
                                                                            "roundingMode": "halfExpand"}, ".toString()"),
            ("Instant", "2020-01-01T01:30Z", "2020-01-01T00:00Z", "since", {"smallestUnit": "hour",
                                                                            "roundingMode": "halfTrunc"}, ".toString()"),
            ("Instant", "2020-01-01T00:00Z", "2020-01-01T01:30Z", "since", {"smallestUnit": "hour",
                                                                            "roundingMode": "halfTrunc"}, ".toString()"),
            ("Instant", "2020-01-01T00:00Z", "2020-01-01T01:30Z", "since", {"smallestUnit": "hour",
                                                                            "roundingMode": "halfCeil"}, ".toString()"),
            ("Instant", "2020-01-01T00:00Z", "2020-01-01T03:29Z", "until", {"smallestUnit": "minute", "roundingIncrement": 30,
                                                                            "roundingMode": "expand", "largestUnit": "hour"},
             ".toString()")):
        difference(c, kind, a, b, name, options, f6, render=render)

    f7 = ("row F7: ZonedDateTime differences across New York's 2026 transitions (the milestone's listed DST"
          " correction: a calendar smallest unit on 24-hour days)")
    ny = "[America/New_York]"
    for a, b, name, options in (
            ("2026-03-08T00:00-05:00", "2026-03-09T00:00-04:00", "until", None),
            ("2026-03-08T00:00-05:00", "2026-03-09T00:00-04:00", "until", {"largestUnit": "day"}),
            ("2026-11-01T00:00-04:00", "2026-11-02T00:00-05:00", "until", {"largestUnit": "day"}),
            ("2026-03-07T12:00-05:00", "2026-03-07T23:45-05:00", "until", {"smallestUnit": "day", "roundingMode": "halfExpand"}),
            ("2026-03-07T23:45-05:00", "2026-03-07T12:00-05:00", "since", {"smallestUnit": "day", "roundingMode": "halfExpand"}),
            ("2026-10-31T12:00-04:00", "2026-11-01T00:15-04:00", "until", {"smallestUnit": "day", "roundingMode": "halfExpand"}),
            ("2026-03-07T00:00-05:00", "2026-03-08T11:45-04:00", "until", {"smallestUnit": "day", "roundingMode": "halfExpand"}),
            ("2026-03-07T12:00-05:00", "2026-03-07T23:45-05:00", "until", {"smallestUnit": "day", "largestUnit": "month",
                                                                           "roundingMode": "halfExpand"}),
            ("2026-03-07T12:00-05:00", "2026-03-08T12:30-04:00", "until", {"largestUnit": "day", "smallestUnit": "hour",
                                                                           "roundingMode": "halfExpand"}),
            ("2026-03-07T12:00-05:00", "2026-03-08T11:30-04:00", "until", {"largestUnit": "day", "smallestUnit": "hour",
                                                                           "roundingMode": "halfExpand"}),
            ("2026-02-07T12:00-05:00", "2026-03-14T23:45-04:00", "until", {"smallestUnit": "week", "largestUnit": "month",
                                                                           "roundingMode": "halfExpand"}),
            ("2026-03-08T01:30-05:00", "2026-03-08T03:30-04:00", "until", {"largestUnit": "day"}),
            ("2026-11-01T01:30-04:00", "2026-11-01T01:30-05:00", "until", {"largestUnit": "day"}),
            ("2026-11-01T01:45-04:00", "2026-11-02T01:15-05:00", "until", {"largestUnit": "day"}),
            ("2026-11-02T01:15-05:00", "2026-11-01T01:45-04:00", "since", {"largestUnit": "day"})):
        difference(c, "ZonedDateTime", a + ny, b + ny, name, options, f7, ("sec-temporal-differencezoneddatetime",))
    for m in MODES:
        for a, b in (("2026-03-07T12:00-05:00", "2026-03-07T23:45-05:00"),
                     ("2026-10-31T12:00-04:00", "2026-11-01T00:15-04:00")):
            difference(c, "ZonedDateTime", a + ny, b + ny, "until", {"smallestUnit": "day", "roundingMode": m},
                       f7 + f": a day of 23 or 25 hours, {m}", ("sec-temporal-nudgetocalendarunit",))
    for value in ("2026-03-08T12:00-04:00", "2026-03-08T11:29-04:00", "2026-11-01T12:29-05:00", "2026-11-01T12:30-05:00"):
        method(c, "differences", "ZonedDateTime", value + ny, "round", [{"smallestUnit": "day"}],
               f7 + ": round to a day (refuted for round, which measures real days)")
    for value in ("2026-03-08T12:00-04:00", "2026-11-01T12:00-05:00", "2026-07-01T12:00-04:00"):
        c.add("differences", "Temporal.ZonedDateTime.prototype.hoursInDay (string)", ins(value + ny),
              f"Temporal.ZonedDateTime.from({lit(value + ny)}).hoursInDay", f7 + ": hoursInDay",
              ("sec-get-temporal.zoneddatetime.prototype.hoursinday",))

    f8 = "row F8: PlainYearMonth differences (years and months only, both limits)"
    for a, b, name, options in (
            ("2019-05", "2021-03", "until", {"largestUnit": "month"}),
            ("2019-05", "2021-03", "until", {"smallestUnit": "year", "roundingMode": "halfExpand"}),
            ("2021-03", "2019-05", "since", {"smallestUnit": "year", "roundingMode": "halfExpand"}),
            ("2019-05", "2021-03", "since", {"smallestUnit": "year", "roundingMode": "floor"}),
            ("2019-05", "2021-03", "until", {"smallestUnit": "month", "roundingIncrement": 5, "roundingMode": "halfExpand"}),
            ("-271821-04", "-271821-05", "until", None), ("-271821-05", "-271821-04", "until", None),
            ("-271821-04", "-271821-04", "until", None), ("+275760-09", "+275760-08", "since", None),
            ("+275760-08", "+275760-09", "until", {"smallestUnit": "year", "roundingMode": "ceil"})):
        difference(c, "PlainYearMonth", a, b, name, options, f8, ("sec-temporal-differencetemporalplainyearmonth",))


# --------------------------------------------------------------------------------------------------
# The calendars (rows C1 to C18).

CALS = ("buddhist", "chinese", "coptic", "dangi", "ethioaa", "ethiopic", "gregory", "hebrew", "indian", "islamic-civil",
        "islamic-tbla", "islamic-umalqura", "japanese", "persian", "roc")
# A year of each calendar in the 2020s, a leap year where the calendar has one (Hebrew 5784 and Chinese and Korean
# 2023 have a leap month).
LEAPYEAR = {"buddhist": 2567, "chinese": 2023, "coptic": 1739, "dangi": 2023, "ethioaa": 7515, "ethiopic": 2015,
            "gregory": 2024, "hebrew": 5784, "indian": 1946, "islamic-civil": 1445, "islamic-tbla": 1445,
            "islamic-umalqura": 1445, "iso8601": 2024, "japanese": 2024, "persian": 1403, "roc": 113}
# The ISO dates within which Table 1 publishes a table calendar's years; outside them the calendar is an
# implementation-defined approximation, which the reference reckons with ICU4C and neither engine's plan does.
PUBLISHED = {"chinese": ("1900-01-31", "2100-12-31"), "dangi": ("1900-01-31", "2050-12-31"),
             "persian": ("1827-03-22", "2120-03-20")}
ERA_CLAUSES = ("sec-temporal-canonicalizeeraincalendar", "sec-temporal-calendardateera")
# Where the reference's calendars are not the specification's, and so no source: ICU4C 77.1 reckons the Hebrew years
# before AM 1 and the years 5806 and 5807 otherwise than the calendar's arithmetic (Reingold and Dershowitz's
# Calendrical Calculations: year 0 has 384 days and begins -003761-08-20, 5806 has 384 and 5807 begins 2046-10-01; the
# retained calendar dataset's divergences.txt, group 3, found 5806); and the reference reckons a non-ISO calendar's year
# through JavaScript's Date, whose range ends inside the years that hold the ISO limits, so it answers a RangeError for a
# field that needs that year's extent. Neither is vectored. (Found where Broiler.VM's run of a draft of this area
# disagreed with the reference, and each checked against the specification's text or the calendar's arithmetic.)
HEBREW_FIRST = "-003760-09-07"
HEBREW_UNRELIABLE = ("2045-09-12", "2047-09-20")
LIMIT_DATES = ("-271821-04-19", "-271821-04-20", "+275760-09-12", "+275760-09-13")


def iso_key(iso: str) -> tuple[int, int, int]:
    sign = -1 if iso.startswith("-") else 1
    year, month, day = iso.lstrip("+-").split("-")
    return sign * int(year), int(month), int(day)


def published(calendar: str, iso: str) -> bool:
    if calendar == "hebrew":
        first, last = HEBREW_UNRELIABLE
        return iso_key(HEBREW_FIRST) <= iso_key(iso) and not iso_key(first) <= iso_key(iso) <= iso_key(last)
    if calendar not in PUBLISHED:
        return True
    first, last = PUBLISHED[calendar]
    return iso_key(first) <= iso_key(iso) <= iso_key(last)


YMD = 'd.year + "/" + d.monthCode + "/" + d.day + ";" + d.month + ";" + d.toString({calendarName: "never"})'
ERA_FIELDS = 'd.era + ";" + d.eraYear + ";" + d.year + ";" + d.toString({calendarName: "never"})'
REF = 'md.monthCode + "/" + md.day + ";" + md.toString({calendarName: "never"})'
FIELDS = ('[d.era, d.eraYear, d.year, d.month, d.monthCode, d.day, d.dayOfYear, d.daysInMonth, d.daysInYear, '
          'd.monthsInYear, d.inLeapYear, d.weekOfYear, d.yearOfWeek].join(";")')


def calendar_date(c: Corpus, bag: dict, options: dict | None, note: str, show: str = YMD, what: str = "calendar fields",
                  clauses=(), era=()) -> None:
    args = [bag] + ([options] if options is not None else [])
    c.add("calendars", f"Temporal.PlainDate.from (property bag{', options' if options is not None else ''}), {what}",
          ins(*args), f"(function (d) {{ return {show}; }})(Temporal.PlainDate.from({', '.join(js(a) for a in args)}))",
          note, ("sec-temporal-calendardatefromfields",) + tuple(clauses), era)


def with_calendar(c: Corpus, iso: str, calendar: str, show: str, what: str, note: str, clauses=(), era=()) -> None:
    c.add("calendars", f"Temporal.PlainDate.prototype.withCalendar, {what}", ins(iso, calendar),
          f"(function (d) {{ return {show}; }})(Temporal.PlainDate.from({lit(iso)}).withCalendar({lit(calendar)}))",
          note, ("sec-temporal-calendarisotodate",) + tuple(clauses), era)


def month_day(c: Corpus, bag: dict, options: dict | None, note: str, clauses=(), era=()) -> None:
    args = [bag] + ([options] if options is not None else [])
    c.add("calendars", f"Temporal.PlainMonthDay.from (property bag{', options' if options is not None else ''}), "
                       "reference date", ins(*args),
          f"(function (md) {{ return {REF}; }})(Temporal.PlainMonthDay.from({', '.join(js(a) for a in args)}))",
          note, ("sec-temporal-calendarmonthdaytoisoreferencedate",) + tuple(clauses), era)


def calendars(c: Corpus) -> None:
    f = "calendars"
    c1 = "rows C1 to C3: calendar identifiers (Table 1's types, the aliases ethiopic-amete-alem and islamicc, ASCII case)"
    for cal in (("iso8601",) + CALS + (
            "ethiopic-amete-alem", "islamicc", "gregorian", "minguo", "islamic", "islamic-rgsa", "islamic-civil-x",
            "ISO8601", "Gregory", "HEBREW", "Islamic-Umalqura", "ETHIOPIC-AMETE-ALEM", "ISLAMICC", "GREGORIAN",
            "iso-8601", "iso8601 ", "chinese\0", "japanese-x", "gregoryİ", "İso8601", "japаnese",
            "persian-x", "ethiopic-amete", "islamic-tbla ", "tbla")):
        c.add(f, "CanonicalizeCalendar, the constructor's calendar", ins(cal),
              f"new Temporal.PlainDate(2020, 1, 1, {lit(cal)}).calendarId", c1,
              ("sec-temporal-canonicalizecalendar", "sec-temporal.plaindate"), ("sup-availablecalendars",))
        s = f"2020-01-01[u-ca={cal}]"
        c.add(f, "Temporal.PlainDate.from (string), calendarId", ins(s), f"Temporal.PlainDate.from({lit(s)}).calendarId",
              c1, ("sec-temporal-canonicalizecalendar",) + GRAMMAR, ("sup-availablecalendars",))

    c4 = ("rows C4 and C5: month codes in each calendar's leap year under both overflows (the grammar's MonthCode,"
          " then Table 3's validity: an invalid code is a RangeError under constrain too)")
    codes = ("M00", "M00L", "M01", "M01L", "M02L", "M05L", "M06L", "M12", "M12L", "M13", "M13L", "M14", "M99", "m01", "M1",
             "M001", "M01l", "L01M", "M01 ", "")
    for cal in ("iso8601",) + CALS:
        for code in codes:
            for overflow in ("constrain", "reject"):
                calendar_date(c, {"calendar": cal, "year": LEAPYEAR[cal], "monthCode": code, "day": 1},
                              {"overflow": overflow}, c4, clauses=("sec-temporal-parsemonthcode",),
                              era=("sec-temporal-isvalidmonthcodeforcalendar",))
    c7 = "row C7: a leap month code in a year without that month (Hebrew skips forward, the lunisolar back)"
    for cal, year, code in (("hebrew", 5783, "M05L"), ("chinese", 2024, "M02L"), ("dangi", 2024, "M02L"),
                            ("chinese", 2023, "M03L"), ("chinese", 2023, "M02L")):
        for overflow in ("constrain", "reject"):
            calendar_date(c, {"calendar": cal, "year": year, "monthCode": code, "day": 1}, {"overflow": overflow}, c7,
                          era=("sec-temporal-constrainmonthcode",))
            c.add(f, "Temporal.PlainYearMonth.from (property bag, options), calendar fields",
                  ins({"calendar": cal, "year": year, "monthCode": code}, {"overflow": overflow}),
                  f'(function (d) {{ return d.year + "/" + d.monthCode + ";" + d.toString(); }})(Temporal.PlainYearMonth'
                  f'.from({js({"calendar": cal, "year": year, "monthCode": code})}, {js({"overflow": overflow})}))',
                  c7, ("sec-temporal-calendaryearmonthfromfields",), ("sec-temporal-constrainmonthcode",))
    for cal, year, month, code in (("hebrew", 5784, 6, "M05L"), ("hebrew", 5784, 7, "M06"), ("hebrew", 5783, 6, "M06"),
                                   ("chinese", 2023, 3, "M02L"), ("chinese", 2023, 3, "M03"), ("coptic", 1739, 13, "M13"),
                                   ("iso8601", 2024, 1, "M02")):
        calendar_date(c, {"calendar": cal, "year": year, "month": month, "monthCode": code, "day": 1}, None,
                      "row C7: month and monthCode together", era=("sup-temporal-nonisoresolvefields",))
    for cal in CALS + ("iso8601",):
        for overflow in ("constrain", "reject"):
            calendar_date(c, {"calendar": cal, "year": LEAPYEAR[cal], "month": 14, "day": 1}, {"overflow": overflow},
                          "row C7: a month beyond the year's months")
    for cal in ("hebrew", "chinese", "dangi", "coptic", "ethiopic"):
        y = LEAPYEAR[cal]
        c.add(f, "CalendarISOToDate, the month codes of a year", ins(cal, y),
              f'(function (r) {{ var n = Temporal.PlainDate.from({{calendar: {lit(cal)}, year: {y}, month: 1, day: 1}})'
              f'.monthsInYear; for (var m = 1; m <= n; m++) r.push(Temporal.PlainDate.from({{calendar: {lit(cal)}, '
              f'year: {y}, month: m, day: 1}}).monthCode); return r.join(); }})([])',
              "row C7: the month codes of a leap year, month by month", ("sec-temporal-calendarisotodate",),
              ("table-additional-month-codes",))

    c8 = "rows C8 and C9: eras (Table 2's codes; the names of earlier drafts are refused) with eraYear 5"
    eras = ("ce", "bce", "ad", "bc", "CE", "Ce", "gregory", "gregory-inverse", "be", "roc", "broc", "minguo", "before-roc",
            "reiwa", "heisei", "showa", "taisho", "meiji", "am", "aa", "AM", "coptic", "hebrew", "incarnation", "mundi",
            "ethiopic", "ethioaa", "shaka", "saka", "ap", "persian", "ah", "bh", "AH", "islamic", "x", "",
            "incar", "minguo-qian", "roc-inverse", "coptic-inverse", "japanese", "japanese-inverse", "islamicc")
    for cal in ("iso8601",) + CALS:
        for era in eras:
            calendar_date(c, {"calendar": cal, "era": era, "eraYear": 5, "monthCode": "M01", "day": 1}, None, c8,
                          show=ERA_FIELDS, what="era fields", era=ERA_CLAUSES + ("table-eras",))
    edges = ("-271821-04-19", "-000001-12-31", "0000-01-01", "0000-12-31", "0001-01-01", "0284-08-29", "0284-08-30",
             "0008-08-27", "0008-08-28", "0622-07-15", "0622-07-16", "0622-07-19", "0079-03-21", "0079-03-22",
             "1867-12-31", "1868-01-01", "1872-12-31", "1873-01-01", "1911-12-31", "1912-01-01", "1912-07-29",
             "1912-07-30", "1926-12-24", "1926-12-25", "1989-01-07", "1989-01-08", "2019-04-30", "2019-05-01",
             "+275760-09-13")
    for iso in edges:
        for cal in CALS:
            if published(cal, iso):
                with_calendar(c, iso, cal, 'd.era + ";" + d.eraYear + ";" + d.year + ";" + d.monthCode + ";" + d.day',
                              "era fields", "row C8: the era and era year of a date at an era's edge", era=ERA_CLAUSES)
    for cal, era, year in (("gregory", "ce", 0), ("gregory", "bce", 0), ("gregory", "bce", -1), ("japanese", "reiwa", 0),
                           ("japanese", "reiwa", -1), ("japanese", "meiji", 1), ("japanese", "meiji", 6),
                           ("japanese", "heisei", 31), ("japanese", "heisei", 40), ("roc", "broc", 0), ("roc", "roc", 0),
                           ("ethiopic", "aa", 5500), ("ethiopic", "aa", 5501), ("ethiopic", "am", 0),
                           ("islamic-civil", "bh", 0), ("islamic-civil", "ah", 0), ("coptic", "am", -5),
                           ("hebrew", "am", 1), ("persian", "ap", -1), ("indian", "shaka", 0), ("buddhist", "be", 0)):
        calendar_date(c, {"calendar": cal, "era": era, "eraYear": year, "monthCode": "M01", "day": 1}, None,
                      "row C8: eraYear zero and negative", show=ERA_FIELDS, what="era fields",
                      era=("sec-temporal-calendardatearithmeticyearforerayear",))
    for cal, fields in (("gregory", {"year": 2020, "era": "ce", "eraYear": 2019}),
                        ("gregory", {"year": 2020, "era": "ce", "eraYear": 2020}), ("gregory", {"era": "ce"}),
                        ("gregory", {"eraYear": 2020}), ("gregory", {"year": 2020, "era": "ce"}),
                        ("gregory", {"year": 2020, "eraYear": 2020}), ("chinese", {"era": "ce", "eraYear": 2020}),
                        ("chinese", {"year": 2020, "era": "x", "eraYear": 1}), ("iso8601", {"era": "ce", "eraYear": 2020}),
                        ("iso8601", {"year": 2020, "era": "x", "eraYear": 1}),
                        ("japanese", {"year": 2019, "era": "heisei", "eraYear": 31}),
                        ("japanese", {"year": 2019, "era": "reiwa", "eraYear": 1})):
        bag = {"calendar": cal}
        bag.update(fields)
        bag.update({"monthCode": "M01", "day": 1})
        calendar_date(c, bag, None, "row C8: year against era, and an era or era year alone", show=ERA_FIELDS,
                      what="era fields", era=("sup-temporal-nonisoresolvefields",))

    c10 = ("row C10: month-day reference dates (Table 6 for chinese and dangi, else the latest date in 1900 to 1972 or"
           " the earliest in 1973 to 2035)")
    for cal in ("iso8601",) + CALS:
        if cal in ("chinese", "dangi"):
            month_codes = [f"M{m:02d}" for m in range(1, 13)] + [f"M{m:02d}L" for m in range(1, 13)]
        elif cal == "hebrew":
            month_codes = [f"M{m:02d}" for m in range(1, 13)] + ["M05L"]
        else:
            month_codes = ["M01", "M02", "M12", "M13"]
        for code in month_codes:
            for day in (1, 29, 30, 31):
                month_day(c, {"calendar": cal, "monthCode": code, "day": day}, {"overflow": "reject"}, c10,
                          era=("sup-temporal-nonisomonthdaytoisoreferencedate", "chinese-dangi-iso-reference-years"))
            for day in (30, 31, 32):
                month_day(c, {"calendar": cal, "monthCode": code, "day": day}, {"overflow": "constrain"}, c10,
                          era=("sup-temporal-nonisomonthdaytoisoreferencedate", "chinese-dangi-iso-reference-years"))
    for cal, code, day in (("chinese", "M11L", 10), ("chinese", "M11L", 11), ("dangi", "M11L", 10), ("dangi", "M11L", 11)):
        month_day(c, {"calendar": cal, "monthCode": code, "day": day}, {"overflow": "reject"},
                  c10 + "; Table 6's M11L split at day 10", era=("chinese-dangi-iso-reference-years",))
    for cal, year, code, day in (("hebrew", 5783, "M05L", 1), ("hebrew", 5784, "M05L", 30), ("chinese", 2023, "M02L", 30),
                                 ("chinese", 2024, "M02L", 1), ("coptic", 1739, "M13", 6), ("coptic", 1740, "M13", 6),
                                 ("islamic-civil", 1445, "M12", 30), ("islamic-civil", 1444, "M12", 30),
                                 ("persian", 1403, "M12", 30), ("persian", 1404, "M12", 30), ("indian", 1946, "M01", 31),
                                 ("gregory", 2021, "M02", 29), ("iso8601", 2021, "M02", 29)):
        for overflow in ("constrain", "reject"):
            month_day(c, {"calendar": cal, "year": year, "monthCode": code, "day": day}, {"overflow": overflow},
                      c10 + "; a year given with the month-day")
    for cal, fields in (("hebrew", {"month": 6, "day": 1}), ("gregory", {"month": 2, "day": 29}),
                        ("iso8601", {"month": 2, "day": 29}), ("chinese", {"month": 1, "day": 1}),
                        ("hebrew", {"year": 1e10, "monthCode": "M01", "day": 1}),
                        ("iso8601", {"year": 1e10, "monthCode": "M01", "day": 1}),
                        ("gregory", {"year": 1e10, "monthCode": "M01", "day": 1}),
                        ("chinese", {"year": 1e10, "monthCode": "M01", "day": 1}),
                        ("hebrew", {"year": 300000, "monthCode": "M01", "day": 1})):
        bag = {"calendar": cal}
        bag.update(fields)
        month_day(c, bag, None, c10 + "; a month with no year, and a year past the limits")

    c14 = ("row C14: the formula calendars at their edges (the ISO limits, years 0 and 1, epochs, new years, leap days,"
           " the Gregorian reform)")
    formula = ("gregory", "buddhist", "roc", "japanese", "coptic", "ethiopic", "ethioaa", "indian", "islamic-civil",
               "islamic-tbla", "hebrew")
    for iso in ("-271821-04-19", "-271821-04-20", "+275760-09-13", "+275760-09-12", "0000-01-01", "0000-12-31",
                "0001-01-01", "-000001-12-31", "0284-08-29", "0008-08-27", "-005492-07-17", "0622-07-16", "0622-07-15",
                "0079-03-22", "-003761-10-07", "2019-09-11", "2019-09-12", "2020-09-11", "2020-09-12", "2023-09-11",
                "2023-09-12", "2024-03-20", "2024-03-21", "2024-03-22", "2024-07-07", "2024-07-08", "2024-06-06",
                "2024-06-07", "2024-10-02", "2024-10-03", "2025-09-22", "2025-09-23", "2023-03-22", "2022-03-22",
                "2026-09-12", "2027-10-02", "2028-09-21", "2031-09-18", "2029-09-10", "2010-09-09", "1900-02-28",
                "1900-03-01", "2100-02-28", "2100-03-01", "1582-10-14", "1582-10-15"):
        for cal in formula:
            if published(cal, iso) and iso not in LIMIT_DATES:
                with_calendar(c, iso, cal, FIELDS, "every field", c14, era=("sec-temporal-calendardatearithmeticyear",))
    for cal in formula:
        for iso in ("-271821-04-19", "+275760-09-13"):
            step = 1 if iso.startswith("+") else -1
            c.add(f, "CalendarDateToISO, the round trip of a date's fields", ins(iso, cal),
                  f"(function (d) {{ return Temporal.PlainDate.from({{calendar: {lit(cal)}, year: d.year, monthCode: "
                  f"d.monthCode, day: d.day}}).toString(); }})(Temporal.PlainDate.from({lit(iso)}).withCalendar({lit(cal)}))",
                  c14 + ": the round trip at the ISO limits", ("sec-temporal-calendardatetoiso",))
            c.add(f, "CalendarDateToISO, a day past the ISO limit", ins(iso, cal),
                  f"(function (d) {{ return Temporal.PlainDate.from({{calendar: {lit(cal)}, year: d.year, monthCode: "
                  f"d.monthCode, day: d.day + ({step})}}, {{overflow: \"reject\"}}).toString(); }})(Temporal.PlainDate"
                  f".from({lit(iso)}).withCalendar({lit(cal)}))", c14 + ": a day past the ISO limits",
                  ("sec-temporal-calendardatetoiso", "sec-temporal-isodatewithinlimits"))
    for year in (5781, 5782, 5783, 5784, 5785, 5786, 5787, 5795):
        c.add(f, "CalendarISOToDate, a Hebrew year's months", ins("hebrew", year), year_lengths("hebrew", year),
              "row C14: Hebrew years of each of the six lengths (353 to 385 days), and each month's length",
              ("sec-temporal-calendarisotodate",))

    c18 = ("row C18: the table calendars at their tables' edges where Table 1 publishes them (chinese 1900 to 2100, dangi"
           " 1900 to 2050, Umm al-Qura 1300 to 1600 AH and islamic-civil outside them, persian 1206 to 1498 AP): new year,"
           " length, months and leap month")
    for cal, years in (("chinese", (1900, 1901, 2050, 2051, 2099)), ("dangi", (1900, 1901, 2049)),
                       ("islamic-umalqura", (1299, 1300, 1301, 1599, 1600, 1601)), ("persian", (1206, 1207, 1497, 1498))):
        for year in years:
            c.add(f, "CalendarISOToDate, a year's structure", ins(cal, year), year_info(cal, year), c18,
                  ("sec-temporal-calendarisotodate",), ("table-calendar-types",))
    for cal, iso, duration_bag in (("chinese", "1900-02-15", {"years": 1}), ("chinese", "2099-06-01", {"years": 1}),
                                   ("chinese", "2050-06-01", {"months": 13}), ("dangi", "2049-06-01", {"months": 6}),
                                   ("islamic-umalqura", "2173-12-01", {"months": 1}),
                                   ("islamic-umalqura", "2174-11-01", {"months": 1}),
                                   ("islamic-umalqura", "1882-11-01", {"months": 1}),
                                   ("islamic-umalqura", "1882-10-01", {"months": 2}),
                                   ("persian", "2119-03-01", {"years": 1}), ("persian", "1827-03-01", {"months": 1})):
        c.add(f, "Temporal.PlainDate.prototype.add and until across a table's seam", ins(iso, cal, duration_bag),
              f'(function (d) {{ var e = d.add({js(duration_bag)}); return e.year + "/" + e.monthCode + "/" + e.day + ";" + '
              f'e.toString({{calendarName: "never"}}) + " until months " + d.until(e, {{largestUnit: "months"}}).toString(); '
              f'}})(Temporal.PlainDate.from({lit(iso)}).withCalendar({lit(cal)}))',
              c18 + ": a year or month added, and until", ("sec-temporal-calendardateadd", "sec-temporal-calendardateuntil"))

    c13 = "row C13: the calendar an object lends as a calendar value (ToTemporalCalendarIdentifier reads its slot)"
    lenders = (("a PlainMonthDay in hebrew", 'Temporal.PlainMonthDay.from({calendar: "hebrew", monthCode: "M05L", day: 1})'),
               ("a PlainYearMonth in coptic", 'Temporal.PlainYearMonth.from({calendar: "coptic", year: 1740, monthCode: "M13"})'),
               ("a ZonedDateTime in japanese", 'Temporal.ZonedDateTime.from("2020-01-01T00:00[UTC][u-ca=japanese]")'),
               ("a PlainDateTime in indian", 'Temporal.PlainDateTime.from("2020-01-01T00:00[u-ca=indian]")'),
               ("a PlainMonthDay in gregory", 'Temporal.PlainMonthDay.from("01-01[u-ca=gregory]")'),
               ("a PlainTime", 'Temporal.PlainTime.from("12:00")'), ("a Duration", 'Temporal.Duration.from("P1D")'),
               ("an Instant", 'Temporal.Instant.from("2020-01-01T00:00Z")'))
    for label, source in lenders:
        value = Raw(source, S(label))
        c.add(f, "ToTemporalCalendarIdentifier (an object), a property bag's calendar", ins(value),
              f"Temporal.PlainDate.from({{year: 2020, month: 1, day: 1, calendar: {source}}}).calendarId", c13,
              ("sec-temporal-totemporalcalendaridentifier",))
        c.add(f, "ToTemporalCalendarIdentifier (an object), withCalendar", ins(value),
              f"new Temporal.PlainDate(2020, 1, 1).withCalendar({source}).calendarId", c13,
              ("sec-temporal-totemporalcalendaridentifier",))
        c.add(f, "ToTemporalCalendarIdentifier (an object), a month-day bag's calendar", ins(value),
              f'Temporal.PlainMonthDay.from({{monthCode: "M01", day: 1, calendar: {source}}}).calendarId', c13,
              ("sec-temporal-totemporalcalendaridentifier",))

    c11 = "row C11: calendar fields beyond 32 bits (no narrowing that wraps reaches a calendar field)"
    w = 2.0 ** 32
    for cal in ("iso8601", "gregory", "japanese", "hebrew", "chinese", "persian", "coptic", "islamic-civil"):
        y = LEAPYEAR[cal]
        calendar_date(c, {"calendar": cal, "year": w + y, "monthCode": "M01", "day": 1}, None, c11)
        calendar_date(c, {"calendar": cal, "year": y, "monthCode": "M01", "day": w + 1}, None, c11)
        calendar_date(c, {"calendar": cal, "year": y, "monthCode": "M01", "day": w + 1}, {"overflow": "reject"}, c11)
        calendar_date(c, {"calendar": cal, "year": y, "month": w + 1, "day": 1}, None, c11)
        c.add(f, "Temporal.PlainYearMonth.from (property bag), toString", ins({"calendar": cal, "year": w + y, "monthCode": "M01"}),
              f'Temporal.PlainYearMonth.from({js({"calendar": cal, "year": w + y, "monthCode": "M01"})}).toString()', c11,
              ("sec-temporal.plainyearmonth.from",))
        c.add(f, "Temporal.PlainMonthDay.from (property bag), toString",
              ins({"calendar": cal, "year": w + y, "monthCode": "M01", "day": 1}),
              f'Temporal.PlainMonthDay.from({js({"calendar": cal, "year": w + y, "monthCode": "M01", "day": 1})})'
              ".toString()", c11, ("sec-temporal.plainmonthday.from",))
    for cal, era in (("gregory", "ce"), ("japanese", "reiwa"), ("roc", "roc"), ("coptic", "am"), ("ethiopic", "am"),
                     ("islamic-civil", "ah"), ("hebrew", "am"), ("persian", "ap")):
        calendar_date(c, {"calendar": cal, "era": era, "eraYear": w + 5, "monthCode": "M01", "day": 1}, None, c11)

    c12 = ("row C12: an ISO month-day's year beyond 2^31, which decides only whether 29 February exists"
           " (CalendarMonthDayToISOReferenceDate regulates against the given year)")
    for year in (2147483652, 2147483653, 4294967296, 1e10, 1e10 + 1, 1e15, -2147483652, 2147483647, 2147483648, 2000,
                 2001):
        for overflow in ("reject", "constrain"):
            c.add(f, "Temporal.PlainMonthDay.from (property bag, options), toString",
                  ins({"year": year, "month": 2, "day": 29}, {"overflow": overflow}),
                  f'Temporal.PlainMonthDay.from({js({"year": year, "month": 2, "day": 29})}, '
                  f'{js({"overflow": overflow})}).toString()', c12, ("sec-temporal-calendarmonthdaytoisoreferencedate",))
    method(c, f, "PlainMonthDay", "02-28", "with", [{"year": 2147483652, "day": 29}, {"overflow": "reject"}], c12)

    c15 = ("rows C15 to C17: the conversions (toPlainYearMonth's first day of the calendar month, toPlainMonthDay's"
           " calendar and reference year, toPlainDate reading the year in the calendar)")
    for cal in ("iso8601",) + CALS:
        d = f'Temporal.PlainDate.from("2023-03-15").withCalendar({lit(cal)})'
        always = '{calendarName: "always"}'
        for what, source in (
                ("PlainDate.prototype.toPlainYearMonth", f"{d}.toPlainYearMonth().toString({always})"),
                ("PlainDate.prototype.toPlainMonthDay", f'(function (md) {{ return md.calendarId + ";" + md.monthCode + '
                                                        f'"/" + md.day + ";" + md.toString({always}); }})({d}.toPlainMonthDay())'),
                ("PlainDateTime.prototype.toPlainDate, toPlainYearMonth",
                 f'Temporal.PlainDateTime.from("2023-03-15T12:00").withCalendar({lit(cal)}).toPlainDate()'
                 f".toPlainYearMonth().toString({always})"),
                ("ZonedDateTime.prototype.toPlainDate, toPlainMonthDay",
                 f'Temporal.ZonedDateTime.from("2023-03-15T12:00[UTC]").withCalendar({lit(cal)}).toPlainDate()'
                 f".toPlainMonthDay().toString({always})"),
                ("PlainMonthDay.prototype.toPlainDate, the calendar year of 2001-06-01",
                 f'Temporal.PlainMonthDay.from({{calendar: {lit(cal)}, monthCode: "M02", day: 10}}).toPlainDate({{year: '
                 f'Temporal.PlainDate.from("2001-06-01").withCalendar({lit(cal)}).year}}).toString({always})'),
                ("PlainYearMonth.prototype.toPlainDate", f"{d}.toPlainYearMonth().toPlainDate({{day: 10}}).toString({always})"),
                ("PlainYearMonth.from (fields), the reference day",
                 f'(function (d) {{ return Temporal.PlainYearMonth.from({{calendar: {lit(cal)}, year: d.year, monthCode: '
                 f"d.monthCode}}).toString({always}); }})({d})"),
                ("PlainYearMonth.prototype.with", f'{d}.toPlainYearMonth().with({{monthCode: "M01"}}).toString({always})'),
                ("PlainMonthDay.prototype.with", f"{d}.toPlainMonthDay().with({{day: 1}}).toString({always})")):
            c.add(f, f"Temporal.{what}", ins("2023-03-15", cal), source, c15,
                  ("sec-temporal.plaindate.prototype.toplainyearmonth", "sec-temporal.plaindate.prototype.toplainmonthday"))

    c6 = "row C6: a month code with decimal digits other than ASCII, and month codes of other shapes"
    for code in ("M٠١", "M0١", "M０１", "M٠٥L", "M01L", "M01ʟ", "M1L", "M001L",
                 "M01LL", "ML01", " M01"):
        for cal in ("iso8601", "gregory", "hebrew", "chinese", "coptic", "persian"):
            calendar_date(c, {"calendar": cal, "year": LEAPYEAR[cal], "monthCode": code, "day": 1}, None, c6,
                          clauses=("sec-temporal-parsemonthcode",))
        month_day(c, {"calendar": "hebrew", "monthCode": code, "day": 1}, None, c6, ("sec-temporal-parsemonthcode",))
        c.add(f, "Temporal.PlainYearMonth.from (property bag), toString", ins({"year": 2020, "monthCode": code}),
              f'Temporal.PlainYearMonth.from({js({"year": 2020, "monthCode": code})}).toString()', c6,
              ("sec-temporal-parsemonthcode",))


def year_lengths(cal: str, year: int) -> str:
    return (f'(function (first, r) {{ for (var m = 1; m <= first.monthsInYear; m++) r.push(Temporal.PlainDate.from('
            f'{{calendar: {lit(cal)}, year: {year}, month: m, day: 1}}).daysInMonth); return first.daysInYear + ";" + '
            f'first.toString({{calendarName: "never"}}) + ";" + r.join(); }})(Temporal.PlainDate.from({{calendar: '
            f'{lit(cal)}, year: {year}, month: 1, day: 1}}), [])')


def year_info(cal: str, year: int) -> str:
    return (f'(function (first, r, leap) {{ for (var m = 1; m <= first.monthsInYear; m++) {{ var d = Temporal.PlainDate'
            f'.from({{calendar: {lit(cal)}, year: {year}, month: m, day: 1}}); r.push(d.daysInMonth === 30 ? "l" : '
            f'd.daysInMonth === 29 ? "s" : String(d.daysInMonth)); if (d.monthCode.length === 4) leap.push(d.monthCode + '
            f'"@" + m); }} return first.toString({{calendarName: "never"}}) + ";" + first.daysInYear + ";" + '
            f'first.monthsInYear + ";" + r.join("") + ";" + leap.join(); }})(Temporal.PlainDate.from({{calendar: '
            f'{lit(cal)}, year: {year}, month: 1, day: 1}}), [], [])')


# --------------------------------------------------------------------------------------------------

def check_clauses(corpus: Corpus, spec: Path | None, era_spec: Path | None) -> None:
    """Every clause id the vectors cite, against the pinned proposals' sources where they are given."""
    import re
    cited_temporal, cited_era = set(), set()
    for meta in corpus.meta.values():
        for entry in meta["clauses"]:
            (cited_temporal if entry.startswith(TEMPORAL) else cited_era).add(entry.split("#", 1)[1])
    cited_temporal.update(D7_CLAUSES)
    for source, cited, name in ((spec, cited_temporal, "the Temporal proposal"), (era_spec, cited_era, "the era proposal")):
        if source is None:
            continue
        files = sorted(source.glob("*.html")) if source.is_dir() else [source]
        ids = set()
        for path in files:
            ids.update(re.findall(r'id="([^"]+)"', path.read_text(encoding="utf-8")))
        missing = sorted(cited - ids)
        if missing:
            raise PinError(f"{name} has no clause {missing}")


def build() -> Corpus:
    corpus = Corpus()
    grammar(corpus)
    balancing(corpus)
    durations(corpus)
    rounding(corpus)
    differences(corpus)
    calendars(corpus)
    return corpus


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--out", required=True, type=Path)
    parser.add_argument("--polyfill", type=Path, default=DEFAULT_POLYFILL,
                        help="the directory holding the polyfill's lib/ and node_modules/")
    parser.add_argument("--node", default="node", help="the Node 22.22.0 to run")
    parser.add_argument("--spec", type=Path, help="the pinned Temporal proposal's spec/ directory, to check clause ids")
    parser.add_argument("--era-spec", type=Path, help="the pinned era proposal's spec.emu, to check clause ids")
    arguments = parser.parse_args(argv)
    try:
        check_polyfill(arguments.polyfill)
        corpus = build()
        check_clauses(corpus, arguments.spec, arguments.era_spec)
        counts = corpus.resolve(arguments.polyfill.resolve(), arguments.node)
    except PinError as error:
        print(f"refused: {error}", file=sys.stderr)
        return 2
    corpus.classify(DIVERGENCES)
    corpus.write(arguments.out)
    for name, records in sorted(corpus.files.items()):
        classes: dict[str, int] = {}
        for record in records:
            classes[record["class"]] = classes.get(record["class"], 0) + 1
        print(f"{name}.vectors: {len(records)} vectors ({', '.join(f'{n} {c}' for c, n in sorted(classes.items()))})")
    total = sum(len(records) for records in corpus.files.values())
    print(f"{total} vectors in {len(corpus.files)} files; decision D7: {counts['d7']} take the pinned text's nudge"
          f" (row D8) and {counts['read']} the texts' answer read where the polyfill's parser or era table differs;"
          f" {counts['window']} note what ComputeNudgeWindow's literal text answers")
    return 0


if __name__ == "__main__":
    sys.exit(main())
