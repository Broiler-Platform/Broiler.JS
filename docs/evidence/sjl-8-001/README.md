# Evidence bundle SJL-8-001 (Broiler.JS) - the time-zone and calendar area

**Milestone:** SJL-8 of the shared JavaScript libraries programme,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-8-time-zones-and-calendars.md`: its data-home clauses
1 to 6 and its Broiler.JS clauses 7 to 11.

**Rule:** [`decision-rule.md`](decision-rule.md), committed at SJL-8.1 in `15a1bd8f` before any code of the area:
- part A is test262 per path in UTC, with negative-metadata tests;
- part B is the same in `America/New_York`;
- part C is Broiler.VM's reference datasets `timezones.js` and `temporal.js` run in this engine.

The data home's clauses are judged by their own mechanisms, the tests SJL-8.2 commits. The rule names them and
does not decide them.

**Area verdict:** [`SharedLibraries.md`](../../roadmap/SharedLibraries.md), "Area verdicts", with Broiler.VM's
JSD-0076 and its correspondence from code, retained in Broiler.VM's bundle of the same name.

**Owner:** the Broiler.JS maintainer. One person holds every role (the programme's EX-30, recorded in Broiler.VM).
**Reviewer:** none, and nothing here has been read by anyone but its author.

**State, 2026-10-18, at SJL-8.7:**
- The data home is in place, with the shared decoder and algorithms over its tables (sections 1 and 2).
- This engine reads them since SJL-8.6's cut-over, and every vector of corpus revision 23 has the result its class
  expects in both zones (sections 4 and 14).
- The after arm is collected at `79a0506c`, the merge that lands row Z16's correction, the last change of SJL-8.6
  (sections 5 to 15). **Every clause of this engine's, 1 to 11, is met** as section 6 reads it:
  - the data home's clauses 1 to 6 by their own tests (section 10);
  - clause 7, the deletion list, as JSD-0076's X6 reads it, and clause 8 (section 12);
  - clause 9 under parts A and B of the rule as written: in UTC and in `America/New_York`, no row of the suite
    changed (sections 7 and 8);
  - clause 10 under part C as written: 458 of `timezones.js`'s 610 lines are as expected and the other 152 are
    class D1, and `temporal.js`'s 7 are class D3 (section 9);
  - clause 11, every correction listed (section 13).
- The first after runs, at `5f96cd40`, are observed and not judged, and the rule is not revised (section 5).

Nothing is accepted: review is deferred, and SJL-8 may not meet its gate before SJL-3 meets SJL-3's.

*(Revised 2026-10-18, at SJL-8.7. The state read "at SJL-8.4", with the items "No engine reads them yet. Broiler.VM's
cut-over is SJL-8.5 and this engine's is SJL-8.6." and "The bases of all three parts are taken at the rule's commit
`15a1bd8f` (section 3).", and the line after the note below read "**Nothing here is a verdict.**" Sections 5 to 15
hold the verdicts.)*

*(Revised 2026-10-17, at SJL-8.4. The state read "at SJL-8.3", and its last item "The bases of parts A and B are
dispatched at the rule's commit. Part C's base follows SJL-8.4, which copies the datasets.")*

## 1. The data home (SJL-8.2)

Everything below lives in [`Broiler.ECMAScript/data/time/`](../../../Broiler.ECMAScript/data/time/README.md), with
its generator `Broiler.ECMAScript.Generation`. Each data-home clause is held by a test or a script that the change
commits.

| Clause | What holds it | At this change |
|---|---|---|
| 1. The pinned archive and crates are present | `tzdb/`, `calendars/` and `cldr/` hold IANA tzdb 2026e's data tarball, ICU4X's `icu_calendar` 2.3.0 and `calendrical_calculations` 0.2.4 crates, and CLDR 48.2.0's `bcp47/timezone.json`, each with its pin; the generator's `PinTests` hold every archive and member to its pin, and each pin file to a digest of Broiler.VM's | Every pin, archive, CLDR file and licence is byte-identical to Broiler.VM's at `9508bd5` (compared file by file with `cmp`) |
| 2. Generation is byte-identical | Rule SL7 in the architecture tests regenerates `generated/` from the pins in memory and compares it byte for byte both ways; `BROILER_ECMASCRIPT_TIME_WRITE=1` writes it instead | The four tables equal the data of Broiler.VM's `JsTzdbTables.g.cs` and `JsCalendarTables.g.cs` at `9508bd5`, byte for byte ([`home/verification.txt`](home/verification.txt)) |
| 3. The `zic` comparison passes across the validated range | [`tzdb/check-zic.py`](../../../Broiler.ECMAScript/data/time/tzdb/check-zic.py), also a Linux step of `Shared libraries` | PASS: 593 of 597 identifiers agree with `zic -b fat` from 1800 to 2037 as an engine reads them. The 4 others read their CLDR primaries by design and differ only before 1966-10-31. All 597 of the table's own zones agree ([`home/zic-check.txt`](home/zic-check.txt)) |
| 4. Public versions are exposed | `TimeDataVersions`: `TzdbRelease`, `CldrVersion`, `IcuCalendarVersion`, `CalendricalCalculationsVersion` | Present, in the API baseline |
| 5. The provider implementation is in place | `ITimeZoneDatabase`, `ITimeZoneRules` and `ICalendarTables` in `Broiler.ECMAScript.Algorithms`, with `TimeZoneDatabase` and `CalendarTables` decoding the bytes an engine hands in (section 2) | Present |
| 6. The `SL` rules pass | The architecture tests, SL1 to SL10, with the generator registered | 32 of 32 pass |

**No package carries a table or a pinned input** (decision SJL-D-6 (c)). The generator's package holds its
assembly, its documentation, its README, the notices and the icon; its assembly was searched for table bytes and
holds none. The shared set packs, and a pristine consumer restores both packages from a local feed alone.

The generator keeps Broiler.VM's behaviour, with three deliberate differences that leave the generated bytes
unchanged:
- Broiler.VM's regular expressions are replaced by readers written by hand, because rule SL4 forbids regular
  expressions in a shared library. The generator's tests hold those readers to Broiler.VM's expressions, on the
  pinned sources and on deliberately broken ones.
- Case handling uses ASCII folding, and numbers are parsed with the invariant format.
- `CldrPin` is a one-file subset of Broiler.VM's.

Its source writers, given Broiler.VM's options, write Broiler.VM's two generated files byte for byte, so Broiler.VM
can regenerate them with this package at SJL-8.5.

## 2. The algorithms (SJL-8.3)

`Broiler.ECMAScript.Algorithms` gains the area under `Broiler.ECMAScript.Time`. The adapter guide is
[`adapter-guide-time-zones.md`](../../../Broiler.ECMAScript/Broiler.ECMAScript.Algorithms/docs/adapter-guide-time-zones.md).
- **The database and its decoder:**
  - `ITimeZoneDatabase` and `ITimeZoneRules`, the area design's section 3.2 shape, with `TimeZoneId`;
  - `TimeZoneDatabase`, over the three zone tables an engine hands in: lookup by ASCII case only, CLDR primaries,
    the primary identifiers and a region's zones;
  - each zone's offset, its daylight flag, and its next and previous transitions, continued by its recurring rules
    with no 2037 cliff.
- **Offset zones:** `OffsetTimeZones`, with ASCII digits only, the `Etc/GMT±n` zones, and
  `FixedOffsetTimeZoneRules`.
- **Wall time:** `ZonedTime`:
  - possible epoch nanoseconds, sampled at the wall time and a day either side;
  - disambiguation in all four modes;
  - start of day, which answers `NoStartOfDay` and never falls back to local midnight;
  - transitions, and zone equality.
- **Failures are data:** each one is a `TimeStatus`, and an unknown name is `false`.
- **Calendar years:** `CalendarTables` decodes the calendar table, and `CalendarYears.GetYear` answers a `chinese`,
  `dangi`, `islamic-umalqura` or `persian` year:
  - from the table inside its range;
  - outside it, from ICU4X's approximation, the tabular Islamic year or the 33-year rule.
- **Nothing in the area charges** (JSD-0076's X3), and the library embeds no table.

**The differential check against Broiler.VM's code.** A throwaway harness ran Broiler.VM's own code beside the
library, over Broiler.VM's tables.
- **What ran:**
  - `JsTimeZones`, `JsTemporal.Zones` without its two members that read engine values, and `JsCalendars`, each
    copied from Broiler.VM's `src/Broiler.VM.Profile.JavaScript/` at `431c20e` with only its namespace changed;
  - `JsRealm.DateTimeFormat`'s zone resolution and the Temporal members it calls, extracted.
- **The result:** **82,514,651 comparisons, 0 differences.** They cover:
  - every identifier's lookup and primary, in ten forms;
  - offsets and daylight flags from 1800 to 2200;
  - next and previous transitions from 1850 to 2150, and every transition walked both ways;
  - possible instants and every disambiguation mode around every transition from 1900 to 2100;
  - start of day either side of every transition;
  - offset identifiers and the formatter's resolution core;
  - zone equality for every pair;
  - every year of the four calendars from -300,000 to 300,000.

  The summary is [`algorithms/differential-summary.txt`](algorithms/differential-summary.txt). The harness's
  program and its extracted copies are in [`algorithms/harness/`](algorithms/harness/), retained as text. The
  verbatim copies are named above by path and commit.
- **The code compared** is the library at the commit the summary names. This branch's library differs from it only
  in its assurance annotations' `Origin`, which became `Ported`, the rubric's word for code adapted from another
  project's code.

**What the library's contract adds to Broiler.VM's.** Neither change is observable through either engine's
callers:
- `NextTransition` answers nothing past its limit. Broiler.VM's two callers dropped such an answer themselves.
- A wall time more than 2×10^8 days from the epoch answers `OutOfRange` before sampling. No caller reaches it.

**The tests.** The library's tests read the data home's generated tables from disk, through one helper that holds
each table's length and SHA-256. Against the data home's tables:

| Project | Tests |
|---|---|
| `Broiler.ECMAScript.Algorithms.Tests` | 547 (234 of them the area's) |
| `Broiler.ECMAScript.Architecture.Tests` (SL1 to SL10) | 32 |
| `Broiler.ECMAScript.Conformance.Tests` | 111 |
| `Broiler.ECMAScript.Generation.Tests` | 28 |

All pass, after a clean Release build with no warning. `assurance check` reports no violation.

A Native AOT image of a smoke program over every member of the area answered byte for byte as the JIT did. The
Native AOT probe is unchanged: the corpus has no vector of the area until SJL-8.4.

## 3. The bases (SJL-8.1's rule)

Parts A and B's bases were dispatched by `test262 evidence` at the rule's commit `15a1bd8f`, with
`include_negative` true:
- part A in the runner's zone, UTC: run 39038217145;
- part B in `America/New_York`: run 39038219059.

Both are imported unchanged, with the digest of each zip as downloaded, which is the digest GitHub records:
[`test262/base/`](test262/base/imported.txt) and [`test262/base-new-york/`](test262/base-new-york/imported.txt).
- **What ran.** Each run tested `15a1bd8f`, dispatched from `main` at `bfafa77e`. The workflow and
  `scripts/compliance/` there are byte-identical to the rule commit's.
- **Completeness.** Each run has:
  - all eight shards reported, with no configuration failure;
  - one suite revision, `ccaac100`;
  - one shell, shown by one distinct list of binaries, with the re-run's shell the same;
  - `includeNegative` true in `run.txt`, the merged results and the re-run.
- **The zone.**
  - Part A's record reads TZ unset and UTC +0000 at both instants.
  - Part B's reads TZ `America/New_York`, EST -0500 and EDT -0400.
  - Each run records one tzdata release, 2026c, and every shard is as expected.
- **Taken.** Part A ran from 13:50Z to 15:25Z on 2026-10-17, and part B from 13:50Z to 15:28Z.

| Run | Passed | Failed | Timed out | Not applicable | Of |
|---|---|---|---|---|---|
| Part A, UTC | 98,181 | 2,004 | 2 | 5,871 | 106,058 rows (53,029 paths, two variants) |
| Part B, `America/New_York` | 98,181 | 2,004 | 2 | 5,871 | 106,058 rows |

The totals are equal in both zones, and the parts compare per row, never with each other. Against bundle
`sjl-7-001`'s after run (98,146 passed), 35 more rows pass: the fixes merged since, #1041's and the move to
Broiler.Unicode `0.1.0-preview.5`.

**Part C's base** ([`datasets/`](datasets/README.md)) was taken at the rule's commit.
- **The population.** Broiler.VM's two programs, with their retained answers and divergences files, are copied
  byte for byte from Broiler.VM's `9508bd5` into
  [`conformance/datasets/time/`](../../../conformance/datasets/time/README.md). `datasets.pin` holds each file's
  length, SHA-256 and Git blob id, and the harness refuses a copy whose digest differs.
- **The harness** is [`scripts/conformance/run_reference_datasets.py`](../../../scripts/conformance/run_reference_datasets.py),
  with its tests.
  - It evaluates each program's text by indirect eval in the Release shell, with no `LANG` and `TZ=UTC`, and
    prints the completion value.
  - It builds the expected answer exactly as Broiler.VM's slice checks do: the same line split, divergence keys
    and replacement, as `IntlChecks.cs` at `9508bd5` has them.
  - It compares line by line, and it compares an after run with a base run.
  - It assigns no class.

  Its expected answer for `timezones.js` equals Broiler.VM's own recorded answer, from the correspondence's run,
  on all 610 lines.
- **The base** used the shell built in Release from a clean checkout of `15a1bd8f`.

  | Program | Lines | Equal to the expected answer | Differing |
  |---|---|---|---|
  | `timezones.js` | 610 | 51 | 559 |
  | `temporal.js` | 1,629 | 1,622 | 7 |

  Each negative control showed its changed line as a difference. Both programs completed with nothing on
  standard error. The `timezones.js` answer is byte-identical to the correspondence's run of the program in this
  engine. A check at the harness's own commit gave byte-identical answers: no file of the engine differs.
- **What the differing lines look like.** This is an observation, not a classification; the rule's classes are
  assigned after the cut-over. [`datasets/base/observations.txt`](datasets/base/observations.txt) has it.
  - **`timezones.js`:**
    - 404 lines differ by the offset's display without its seconds (Z16, SJL-9's);
    - 201 by the offset in 2150 (Z4, and the release, Z1);
    - 152 by the resolved identifier (Z8);
    - 110 by an offset before 1970 (Z3);
    - 7 by the region lists (Z12);
    - 1 by the list of identifiers (Z7, Z5).
  - **`temporal.js`:** the 7 lines are one `Duration.round` to months relative to `2020-01-31`, in 7 of 9 rounding
    modes. It reads no zone and no calendar table year, so it looks like class D3's. D3 admits it only if its answer
    after equals its answer at the base.

## 4. The vectors (SJL-8.4)

**Corpus revision 21** (`09fa9ac5`) adds the time-zone area: 3,969 vectors in seven files under
[`conformance/vectors/time-zones/`](../../../conformance/vectors/time-zones/), written by
[`scripts/conformance/generate_time_zone_vectors.py`](../../../scripts/conformance/generate_time_zone_vectors.py).
- **The reference.** Every expected value is [`scripts/conformance/tzref.py`](../../../scripts/conformance/tzref.py)'s
  (`ref:tzref@1`). It shares no code with the data home's generator or with `Broiler.ECMAScript.Algorithms`, and it
  reads none of the generated tables. It:
  - checks the pinned tarball and CLDR file against their pins;
  - compiles the nine default source files with the host's `zic -b fat`;
  - reads every compiled file with its own TZif reader;
  - evaluates each footer's POSIX TZ string itself after the last listed transition;
  - applies SJL-D-20's policy.

  Its tests check its reader against Python's `zoneinfo` at about 877,000 instants across all 597 zones.
- **What the vectors pin.** Instants on both sides of every policy difference and correction JSD-0076 lists:
  - the release;
  - backzone;
  - extrapolation after 2037;
  - the identifiers, `Factory` and the primaries;
  - lookup by ASCII case only;
  - ISO strings as a formatter's zone;
  - offset identifiers;
  - a sample of ordinary offsets, transitions, gaps, overlaps and starts of day.
- **Classes.** 3,800 vectors are `normative` and 169 `impl-defined`. The `impl-defined` ones are the formatter's
  resolved name (Z8) and a region's zones (Z12).

**The library answers every one.** The library-level runner `Broiler.ECMAScript/runners/AlgorithmsLibrary.TimeZones.cs`
answers every time-zone vector through `Broiler.ECMAScript.Time`, over the data home's tables, under the JIT and
under Native AOT. The probe's reports are byte-identical, and a mutated vector is caught.

**This engine's first runs** of `Broiler.JS/Broiler.JavaScript.Vectors.Tests` in Release ([`engine/`](engine/)) were
taken under `TZ=UTC` and under `TZ=America/New_York`, whose reports are byte-identical.

| Revision | Match | Classified divergences | Unclassified | Not implemented |
|---|---|---|---|---|
| 21 | 91,269 | 4 (the number area's, SJL-14's) | 1,161, all in the time-zone area | 0 |
| 22 | 91,269 | 1,165 | 0 | 0 |

Each run's negative control flagged exactly its mutated vector. **Revision 22** (`4ce20ac7`) classifies the 1,161
under the correspondence's rows. The reasons are in
[`scripts/conformance/time-zone-divergences.txt`](../../../scripts/conformance/time-zone-divergences.txt).

| Class | Rows | Vectors |
|---|---|---|
| Policy, SJL-D-20 | Z1, Z2 (the release) | 309 |
| | Z3 (backzone) | 463 |
| | Z4, A5, A7, A10 (the 2037 freeze) | 310 |
| | Z5 (`Factory`), Z7 (the primaries) | 31 |
| Correction, SJL-D-16 (a) | Z10 (lookup by invariant upper-casing) | 13 |
| | Z11 (ISO strings as a formatter's zone) | 4 |
| | Z12 (the region lists) | 10 |
| | A13 (non-ASCII offset digits: a .NET `FormatException` escapes, rendered `abort:`) | 21 |

**Every difference falls under a row the area verdict names, and the vectors falsify none of it.** Every vector of
the rows it calls agreement matches. SJL-8.6, this engine's cut-over, answers each one.

**Revision 23** (`2d496a55`) retires the 1,161 fields, which SJL-8.6's cut-over corrects
([`engine/retired-divergences-revision-23.tsv`](engine/retired-divergences-revision-23.tsv)). The seven files under
`conformance/vectors/time-zones/` are revision 21's byte for byte. The runner was built once in Release at
`2d496a55`, from a clean tree, and run in both zones together, from 21:33Z to 22:10Z, with no `LANG`:

| Run | Result | Files |
|---|---|---|
| Revision 23, UTC | 92,434 vectors: 92,430 match, 4 classified divergences, 0 unclassified differences | [`engine/run-revision-23-utc.log`](engine/run-revision-23-utc.log), [`engine/run-revision-23-utc.tsv.gz`](engine/run-revision-23-utc.tsv.gz) |
| Revision 23, New York | 92,434 vectors: 92,430 match, 4 classified divergences, 0 unclassified differences | [`engine/run-revision-23-new-york.log`](engine/run-revision-23-new-york.log), [`engine/run-revision-23-new-york.tsv.gz`](engine/run-revision-23-new-york.tsv.gz) |

**Every vector of revision 23 has the result its class expects, in both zones.**
- The 4 are the number area's, SJL-14's, and each still holds. No vector is reported as not implemented.
- The two reports are byte for byte the same.
- Each run's negative control flagged exactly its mutated vector, `numbers/to-fixed/0001`
  ([`engine/negative-control-revision-23-utc.tsv`](engine/negative-control-revision-23-utc.tsv),
  [`engine/negative-control-revision-23-new-york.tsv`](engine/negative-control-revision-23-new-york.tsv)).

**Broiler.VM's runs** (observed, its bundle of the same name) match every vector at revision 21 and at revision 22,
on a pin moved to revision 22. Its run on revision 23, on the pin moved there, matches every vector too.

## 5. The after arm (SJL-8.7)

| Field | Value |
|---|---|
| Evidence bundle | SJL-8-001 (Broiler.JS's) |
| Bases | Section 3: the workflow's runs at `15a1bd8f`, the commit that carries the rule, before any code of the area, part A's in UTC and part B's in `America/New_York`, each with its re-run of the rows that timed out; and part C's run at the same commit |
| After | `79a0506c` (`79a0506c870302cefa6d7105146c65feab898879`), the merge into the default branch (Broiler-Platform/Broiler.JS#1048) that lands row Z16's correction (`4d558ee1`), the last change of SJL-8.6. It follows `5f96cd40` (#1047), the merge that landed SJL-8.6's cut-over (`f58f9b49` to `eb2ab599`) and corpus revision 23 (`2d496a55`). The owner brought Z16 into SJL-8.6 on 2026-10-18, so this is the commit "that lands SJL-8.6's cut-over" in the rule's words, as the plan records (below). **Its product sources are `4d558ee1`'s**, and they differ from `eb2ab599`'s in six files ([`identity.txt`](identity.txt), which lists every commit and every file): Z16's two, the formatter `JSIntlDateTimeFormatEngine.cs` and its test `Issue786Tests.cs`, and four that are not code, the two changelogs, the data home's README and `Broiler.ECMAScript/Directory.Build.props`, whose version suffix moved to `preview.5` when `0.1.0-preview.5` was published. Every local run of this bundle is taken from a clean checkout of it |
| Rule commit | `15a1bd8f`, an ancestor of the after commit. The rule, the comparison script, the workflow and the two scripts it runs and merges with are unchanged between the two. Part C's harness, its driver and the copies of the datasets are unchanged since SJL-8.4 committed them (`584caa2f`), and the harness's SHA-256 is the base run's ([`identity.txt`](identity.txt)) |
| Shared library | `Broiler.ECMAScript.Algorithms` at the after commit, whose code is the code of the package nuget.org serves as `0.1.0-preview.5`, published from `a04cd447` with `2d37a38a`'s library code: no file of the library or the generator differs between `2d37a38a` and `79a0506c` (section 10) |
| Shared vectors | `conformance/vectors` at corpus revision 23, which retires this engine's 1,161 divergence fields of the area ([`engine/retired-divergences-revision-23.tsv`](engine/retired-divergences-revision-23.tsv)) |
| Collector | [`collect.py`](collect.py) as this change commits it, which reuses bundle `sjl-7-001`'s collector, and through it `sjl-6-001`'s, `sjl-5-001`'s, `sjl-4-001`'s, `sjl-3-001`'s and `sjl-0-001`'s, run against clean checkouts of the after commit and, for the consumer's control, of the rule's commit. Part C was taken by its own harness, as its base was ([`datasets/README.md`](datasets/README.md)). Its `manifest` step wrote [`manifest.json`](manifest.json), the SHA-256 of every other file the bundle retains, this README's included |
| Taken | 2026-10-18. Parts A's and B's after runs by the workflow, dispatched together at 07:16Z and completed at 08:50Z and 08:52Z; locally, part C from 07:20Z, `vectors` from 07:21Z, `home` and `simplification` from 07:23Z, `area` from 07:25Z, `probe` from 07:25Z, `consumer` from 07:26Z and `identity` at 07:28Z; `test262` and `compare` at 09:07Z. The machine is section 3's; the vectors and the other steps ran beside each other, and nothing local here is timed |
| RID | `linux-x64`: the workflow's runners for test262; locally, one machine with four logical processors |

**The first after runs, at `5f96cd40`: observed, not judged.** All three parts were first taken at `5f96cd40`, the
merge of the cut-over and corpus revision 23, before Z16 came into SJL-8.6.
- **Parts A and B:** runs 39077770126 and 39077771789 of `test262 evidence`, dispatched together at 23:07Z on
  2026-10-17 and completed at 00:41Z and 00:34Z on 2026-10-18. Neither is imported here, and neither is compared
  with anything.
- **Part C** is retained in [`datasets/observed-5f96cd40/`](datasets/observed-5f96cd40/). `timezones.js` answered
  84 of its 610 lines as Broiler.VM expects. Of the 526 others, 24 differed only by the resolved identifier (Z8),
  374 only by the `longOffset` name's dropped seconds (Z16), and **128 by both**
  ([`both-kinds.tsv`](datasets/observed-5f96cd40/both-kinds.tsv) lists them). `temporal.js` answered as at the
  base. Part C admits a line whose difference is *only* the resolved identifier (D1) or *only* a zone's display
  text (D2), so the 128 were in no class, and part C was not met as written there.
- **The owner's direction.** Offered a dated revision of part C that would admit those lines (weaker evidence),
  leaving clause 10 open until SJL-9, or making the correction now, the owner chose the correction: an offset's
  GMT name keeps its seconds (`4d558ee1`, #1048). [`SharedLibraries.md`](../../roadmap/SharedLibraries.md) records
  it under "2026-10-18: row Z16 comes into SJL-8.6, at the owner's direction".
- **The rule is not revised.** [`decision-rule.md`](decision-rule.md) is the file `15a1bd8f` committed, byte for
  byte. D2 stays in it and admits what it admitted, and no class is read differently here.
- **Nothing of `5f96cd40` is carried forward.** Every part, and every local step, is taken again at `79a0506c`.

## 6. The clauses, and what holds each

| Clause | What it asks | Where | State |
|---|---|---|---|
| 1 | The pinned archive and crates are present | section 10 | **Met** at `79a0506c`: the generator's `PinTests` pass (12 tests: each archive, crate and CLDR file to its pin, each pin to Broiler.VM's, and each changed byte, release or malformed pin refused), and each pin and input is byte-identical to Broiler.VM's at its head `638e0f7e` |
| 2 | Generation is byte-identical | section 10 | **Met**: rule SL7 regenerates `generated/` from the pins in memory and compares it byte for byte both ways; no generated file changed since section 1 |
| 3 | The `zic` comparison passes across the validated range | section 10, [`home/after/zic-check.log`](home/after/zic-check.log) | **Met**: PASS, 593 of 597 identifiers agree with `zic -b fat` from 1800 to 2037 as an engine reads them, the 4 others by design, and all 597 of the table's own zones agree |
| 4 | Public versions are exposed | section 10 | **Met**: `TimeDataVersions`' four constants, in the API baseline, which SL6 holds in both directions |
| 5 | The provider implementation is in place | sections 2 and 10 | **Met**: `TimeZoneDatabase` and `CalendarTables` over the bytes an engine hands in, in the package nuget.org serves, and this engine reads its tables through them (section 12) |
| 6 | The `SL` rules pass | section 10 | **Met**: SL1 to SL10, 32 of 32, and the assurance check reports no violation |
| 7 | The deletion list is absent | section 12 | **Met** at `79a0506c`, as JSD-0076's X6 reads the list: every entry is absent from the file or folder that held it. What remains is calls to the library, adapters, members of the same name, comments, the tests that assert the absence, and the changelog. `TemporalArithmeticCalendar` and `TemporalCalendarMath` remain without their Umm al-Qura and .NET dispatch, holding the formula calendars, which are SJL-10's. That reading of the entry is the verdict's, not this bundle's |
| 8 | No host zone or .NET calendar is used by `Temporal`; `Date` follows the provider decision | section 12 | **Met**: `Temporal` names no .NET calendar and no CLDR package calendar, and reads every offset, rule and transition from the shared database. What it reads from the host is the name of the host's zone, `TimeZoneInfo.Local`'s IANA identifier, which `Temporal.Now` must report (SystemTimeZoneIdentifier) and which `TemporalTimeZones.Host()` resolves through the shared database, or `UTC` (A16). `Date`'s local time reads the same zone's offsets from the shared database (SJL-D-19 as SJL-8.6 applies it); its display names and the CLR views `JSDate.Value` and `DateTime` stay the host's, as `docs/public-api.md` records |
| 9 | test262 for Temporal and Intl time zones meets the area rule | sections 7 and 8, [`test262/`](test262/) | **Met** under parts A and B of the rule as written, at `79a0506c`: in each zone both runs are complete and comparable, every shard in the zone, the host-zone records alike (tzdata 2026c in all four runs), and of 106,058 rows none changed, so none regressed and classes C1 and C5 list no row |
| 10 | The reference datasets meet their rule | section 9, [`datasets/after/`](datasets/after/) | **Met** under part C of the rule as written, at `79a0506c`: both programs complete with every line. `timezones.js` answers 458 of its 610 lines as expected, and each of the other 152 differs only by the resolved identifier (D1); `temporal.js`'s 7 differing lines are D3's, its answer the base's byte for byte. Every one is listed |
| 11 | Every correction is listed | section 13, [`Broiler.JS/CHANGELOG.md`](../../../Broiler.JS/CHANGELOG.md) | **Met**: every correction the correspondence names for this engine, Z10, Z11, Z12, A13, A16, K2, K7 and K12, with Z16, brought into SJL-8.6 at the owner's direction, and the two deletions of unreached code, A6 and A11; every policy change, Z1 to Z5, Z7, Z15, A5, A7, A10, K4 to K6, K11 and K14; and what stays this engine's, Z8. Sections 7 and 8 have no row in class C1 or C5 |

Clauses 12 to 17 are Broiler.VM's, judged in its bundle of the same name. None is accepted: review is deferred,
and SJL-8 may not meet its gate before SJL-3 meets SJL-3's.

## 7. Test262 per path, in UTC (rule part A)

**MET under part A of the rule as written, at the after commit `79a0506c`: no row changed.**
[`test262/comparison-utc/comparison.json`](test262/comparison-utc/comparison.json) and
[`test262/comparison-utc/comparison.log`](test262/comparison-utc/comparison.log): the after run against part A's
base of section 3, through `scripts/compliance/compare_test262_paths.py` as the rule committed it at SJL-0.9,
with each run's timed-out rows replaced by their re-runs. **106,058 rows (53,029 paths, each in both variants): 0
changed, 0 regressions**, so no classification file is needed, and none is retained.

| Part A's condition | This run |
|---|---|
| Both runs complete | eight of eight expected shards reported in each, none incomplete or retried, no configuration failure, one suite revision, `ccaac100` ([`test262/comparison-utc/completeness.log`](test262/comparison-utc/completeness.log), read from each run's own files; [`test262/after/merge.log`](test262/after/merge.log)) |
| One shell in each | the hashes of the shell's binaries are the same in every shard and in the re-run, in each run ([`test262/after/shell-hashes.log`](test262/after/shell-hashes.log)). The two runs' shells differ, as they must: the after run's is built from the cut-over and Z16's correction ([`test262/after/shell-binaries.sha256`](test262/after/shell-binaries.sha256)) |
| Every shard in the zone, and the two records alike | `TZ` unset, UTC +0000 at both instants, tzdata 2026c, in every shard of each run, and the records' lines the same ([`test262/comparison-utc/zone-records.log`](test262/comparison-utc/zone-records.log)). The runners' tzdata did not move between the base and the after run |
| `includeNegative` true in both | in `run.txt`, in the merged results and in the re-run, in each run |
| The script finds them comparable | yes ([`test262/comparison-utc/compare-checks.log`](test262/comparison-utc/compare-checks.log): the after run against itself changes nothing, and a planted regression is reported, exit 1); the fields it records and does not compare are the same in both: .NET SDK 10.0.402, four workers, eight shards, Linux, X64 |
| No row passing at the base fails, times out or is skipped after | none |
| Every changed verdict in C1 or C5, and listed | **met**: no verdict changed, and C1 and C5 list no row |

**The after run.** Taken by `.github/workflows/test262-evidence.yml` at `79a0506c`, in the rule's configuration
([`test262/after/run.txt`](test262/after/run.txt)): run
[39107043317](https://github.com/Broiler-Platform/Broiler.JS/actions/runs/39107043317), attempt 1, dispatched
2026-10-18 07:16Z from `main` at `79a0506c` with `time_zone` empty and `include_negative` true, and completed
08:50Z. It ran `--all-script-host-verifiable` over eight shards, with terser 5.50.0, 30 seconds per test, no memory
limit, four workers and the .NET SDK 10.0.402, on runner image ubuntu24 20261012.334.1, the base's. It is imported
into [`test262/after/`](test262/after/) unchanged, every file of the workflow's artifact, with
[`imported.txt`](test262/after/imported.txt), which records the digest of the zip as downloaded, `9daad340`, the
digest GitHub records.
- **Timeouts.** As at the base, one path timed out in both variants,
  `test/staging/sm/regress/regress-1507322-deep-weakmap.js`. The workflow ran it again alone with one worker at the
  same commit, and it timed out again in both ([`test262/after/rerun-timeouts.log`](test262/after/rerun-timeouts.log)),
  so the rows compare timed out against timed out.
- **The merged totals**, which do not decide part A, are the base's: 98,181 variant cases passed, 2,004 failed,
  2 timed out and 5,871 skipped.

**What it says.** In UTC, SJL-8.6 moved no verdict of the suite.
- Every row whose path names `Temporal` passes in both runs, and so does every row of
  `intl402/Intl/supportedValuesOf`.
- `built-ins/Date`, `intl402/DateTimeFormat` and `intl402/Locale` pass in both runs but for five
  `proto-from-ctor-realm` paths, which fail in both: they read a constructor's prototype from another realm, not a
  zone or a calendar.
- So the area's corrections and policy changes are cases test262 at this revision does not hold, or holds where
  the base already passed. The vectors (section 14), part C (section 9) and the engine's tests (section 10) hold
  them. SJL-8.6's local subset of the area's paths found the same (`SharedLibraries.md`).

## 8. Test262 per path, in `America/New_York` (rule part B)

**MET under part B of the rule as written, at the after commit `79a0506c`: no row changed.**
[`test262/comparison-new-york/comparison.json`](test262/comparison-new-york/comparison.json) and
[`test262/comparison-new-york/comparison.log`](test262/comparison-new-york/comparison.log): the after run against
part B's base of section 3, through the same script, with each run's timed-out rows replaced by their re-runs.
**106,058 rows: 0 changed, 0 regressions**, so no classification file is needed, and none is retained. This is the
area's run under a host zone with daylight-saving time, where `Date`'s local time now reads the host zone's offsets
from the shared data (A15).

| Part B's condition | This run |
|---|---|
| Both runs complete | eight of eight expected shards reported in each, none incomplete or retried, no configuration failure, one suite revision, `ccaac100` ([`test262/comparison-new-york/completeness.log`](test262/comparison-new-york/completeness.log); [`test262/after-new-york/merge.log`](test262/after-new-york/merge.log)) |
| One shell in each | the hashes of the shell's binaries are the same in every shard and in the re-run, in each run ([`test262/after-new-york/shell-hashes.log`](test262/after-new-york/shell-hashes.log)); the after run's shell is part A's after run's, by its hashes |
| Every shard in the zone, and the two records alike | every shard of each with `TZ` `America/New_York`, and the records' lines the same: EST -0500 at 2021-01-15T12:00:00Z, EDT -0400 at 2021-07-15T12:00:00Z, tzdata 2026c ([`test262/comparison-new-york/zone-records.log`](test262/comparison-new-york/zone-records.log)). The runners' tzdata did not move |
| `includeNegative` true in both | in `run.txt`, in the merged results and in the re-run, in each run |
| The script finds them comparable | yes ([`test262/comparison-new-york/compare-checks.log`](test262/comparison-new-york/compare-checks.log): the after run against itself changes nothing, and a planted regression is reported, exit 1); the fields it records and does not compare are the same in both: .NET SDK 10.0.402, four workers, eight shards, Linux, X64 |
| No row passing at the base fails, times out or is skipped after | none |
| Every changed verdict in C1 or C5, and listed | **met**: no verdict changed, and C1 and C5, A15's rows among them, list no row |

**The after run.** Taken by `.github/workflows/test262-evidence.yml` at `79a0506c`, in part B's configuration
([`test262/after-new-york/run.txt`](test262/after-new-york/run.txt)): run
[39107045326](https://github.com/Broiler-Platform/Broiler.JS/actions/runs/39107045326), attempt 1, dispatched
2026-10-18 07:16Z from `main` at `79a0506c` with `time_zone` `America/New_York` and `include_negative` true, and
completed 08:52Z, in the configuration of part A's run and on the same runner image. It is imported into
[`test262/after-new-york/`](test262/after-new-york/) unchanged, every file of the workflow's artifact, with
[`imported.txt`](test262/after-new-york/imported.txt), which records the digest of the zip as downloaded,
`a096fac8`, the digest GitHub records.
- **Timeouts.** As at part B's base, one path timed out in both variants,
  `test/staging/sm/regress/regress-1507322-deep-weakmap.js`, and timed out again alone with one worker
  ([`test262/after-new-york/rerun-timeouts.log`](test262/after-new-york/rerun-timeouts.log)).
- **The merged totals**, which do not decide part B, are part B's base's: 98,181 variant cases passed, 2,004
  failed, 2 timed out and 5,871 skipped. The two parts' runs are not compared with each other, as the rule says.

**What it says.** Under `America/New_York`, SJL-8.6 moved no verdict either. `Date`'s offsets for the host zone are
the shared database's since the cut-over (tzdb 2026e, where the base read the runner's tzdata, 2026c), and no row's
verdict moved with them. As in part A, the rows of the area that do not pass are the
five `proto-from-ctor-realm` paths, in both runs.

## 9. The reference datasets (rule part C)

**MET under part C of the rule as written, at the after commit `79a0506c`.** Both programs complete with every
line, and every line that differs from its expected answer is in D1 or D3 and is listed.

The after run ([`datasets/after/`](datasets/after/)) was taken with the shell built in Release from a clean
checkout of `79a0506c`, by the harness as SJL-8.4 committed it, with `TZ=UTC` and no `LANG`, as the base was.
`compare` joined it with the base line by line ([`datasets/after/compare-with-base/`](datasets/after/compare-with-base/)).

| Program | Lines | Equal to the expected answer | Differing | Against the base |
|---|---|---|---|---|
| `timezones.js` | 610 | 458 | 152 | 407 lines that differed are equal; 133 differing lines moved; none that was equal differs |
| `temporal.js` | 1,629 | 1,622 | 7 | the same answer, byte for byte |

Both programs completed with nothing on standard error. Each negative control showed its changed line, and no
other, as a difference. The counts are those the plan's record of 2026-10-18 gives for the correction.

**How each differing line differs.** [`classify.py`](datasets/after/classify.py) splits each line of
`timezones.js` into its two parts, the identifier `resolvedOptions().timeZone` reports and the eight `longOffset`
names, and reports which part differs and how. It assigns no class.

| What differs | Lines | Class under the rule |
|---|---|---|
| `timezones.js`: the resolved identifier only (Z8) | 152 | **D1**. For example `Etc/GMT`, `GMT`, `Zulu`, `US/Eastern`, `Asia/Calcutta`, `America/Buenos_Aires` |
| `temporal.js`: `Duration.round` to months relative to `2020-01-31` | 7 | **D3**: the relative date is a plain ISO date, so the operation reads no zone and no calendar table year, and the answer after equals the base's |

- **The 152 lines, class D1.** Each names a zone that `Intl.supportedValuesOf('timeZone')` does not list, a
  non-primary name. The shell reports the name as given, and Broiler.VM's answer is its primary, which the shell
  lists. `Temporal` finds the two one zone (`ZonedDateTime.equals`). The eight `longOffset` names of every one of
  them equal the expected ones. Each line is listed with these checks in
  [`classification.tsv`](datasets/after/classification.tsv), and the probe that made them is
  [`probe.js`](datasets/after/probe.js), with its answer [`probe.answer.tsv`](datasets/after/probe.answer.tsv).
  The resolved identifier of each is what the base answered.
- **The 7 lines, class D3.** `P1Y2M3W4DT5H6M7.008009010S` rounded to months relative to `2020-01-31` in the modes
  `ceil`, `expand`, `halfCeil`, `halfFloor`, `halfExpand`, `halfTrunc` and `halfEven`. Broiler.VM expects `P1Y3M`;
  this engine answers `P1Y2M30D`, at the base and after. That is duration rounding, SJL-10's.
- **No line differs by a `longOffset` name**, so D2 lists no line.

**What moved since the base**, against the base's observations ([`datasets/base/observations.txt`](datasets/base/observations.txt)).
- **The 407 lines now equal** are the base's differing lines that did not differ by the resolved identifier. Each
  differed by the parts the cut-over and Z16 corrected: the dropped seconds (Z16), the offset in 2150 (Z4, and Z1
  for the names the release changed), an offset in 1850, 1900 or 1940 (Z3), or, for the 8 list lines, the region
  lists (Z12) and the listed identifiers (Z5, Z7).
- **The 133 differing lines that moved** moved only in their `longOffset` names, which now equal the expected
  ones. The other 19 differed by the resolved identifier alone at the base, and still do.
- No line differs any more by the release, backzone, the 2037 cliff, `Factory`, the primaries, the regions or an
  offset's seconds.

**Against the observed run at `5f96cd40`** (section 5). The 502 lines whose `longOffset` names dropped seconds
there now write them, and nothing else changed on any line: the 374 that differed by the seconds alone are equal,
and the 128 that differed by both now differ by the resolved identifier alone. The other 108 lines' answers are
the same, byte for byte, and so is `temporal.js`'s.

## 10. The home at the after commit (data-home clauses 1 to 6)

The home is this repository's: [`Broiler.ECMAScript/data/time/`](../../../Broiler.ECMAScript/data/time/README.md),
its generator and the library. From a clean checkout of `79a0506c`, into [`home/after/`](home/after/):

| Check | Result | Where |
|---|---|---|
| The home's files | Since section 1 read the clauses (`19e74588`), only the home's README changed (three lines naming this engine's cut-over). No generated file changed, and the four tables have the SHA-256 section 1 recorded | [`home/after/data-home.log`](home/after/data-home.log) |
| The pins and inputs against Broiler.VM's | `tzdb.pin`, the tz archive, `calendars.pin`, both crates and CLDR's `timezone.json` are each byte-identical to Broiler.VM's at its head `638e0f7e` | [`home/after/data-home.log`](home/after/data-home.log) |
| The shared folder, built in Release | 0 warnings, 0 errors | [`home/after/build.log`](home/after/build.log) |
| Its tests | **719 passed, none failed**: `Broiler.ECMAScript.Generation.Tests` 28 (the 12 `PinTests` among them), `Broiler.ECMAScript.Conformance.Tests` 112, `Broiler.ECMAScript.Algorithms.Tests` 547 and `Broiler.ECMAScript.Architecture.Tests` 32 (SL1 to SL10, with SL6's API baseline in both directions and SL7's regeneration of the generated files, byte for byte both ways) | [`home/after/tests.log`](home/after/tests.log) |
| The `zic` comparison | PASS, as at section 1, word for word: 593 of 597 identifiers agree with `zic -b fat` from 1800 to 2037 as an engine reads them; `CST6CDT`, `EST5EDT`, `MST7MDT` and `PST8PDT` read their CLDR primaries by design and differ only before 1966-10-31; all 597 of the table's own zones agree | [`home/after/zic-check.log`](home/after/zic-check.log) |
| The assurance check | `Broiler.ECMAScript: no violations.`, by Broiler.Code's CLI at `afd4ffb2` | [`home/after/assurance.log`](home/after/assurance.log) |
| The public versions | `TimeDataVersions`: `TzdbRelease` `2026e`, `CldrVersion` `48.2.0`, `IcuCalendarVersion` `2.3.0`, `CalendricalCalculationsVersion` `0.2.4`, in the generator's API baseline | [`home/after/data-home.log`](home/after/data-home.log) |
| The packages as nuget.org serves them | `Broiler.ECMAScript.Algorithms` and `Broiler.ECMAScript.Generation` `0.1.0-preview.5` each carry nuget.org's repository signature, which `dotnet nuget verify` accepts, and name this repository at `a04cd447`; neither declares a dependency. **No entry of either holds 32 bytes taken from the start, the middle or the end of any generated table or pinned archive** (decision SJL-D-6 (c)) | [`home/after/packages.log`](home/after/packages.log) |
| The Native AOT probe, `linux-x64` | published with no warning, and the publish directory holds no managed assembly; the image ran corpus revision 23, **92,434 vectors in 43 files, no failure**: 54,477 match, every one of the area's 3,969 among them, 0 differ, and 37,957 name operations the library-level runner does not implement (37,738 of them the Unicode area's, which is Broiler.Unicode's). Its report is byte-identical to the same probe's under the JIT | [`home/after/probe-publish-linux-x64.log`](home/after/probe-publish-linux-x64.log), [`home/after/probe-run-linux-x64.log`](home/after/probe-run-linux-x64.log), [`home/after/probe-report-aot.tsv.gz`](home/after/probe-report-aot.tsv.gz) |
| The shared set's pristine consumer | packed at `0.1.0-preview.5` from the after commit, restored from a local feed alone into an isolated cache, and run | [`home/after/pristine-consumer-shared.log`](home/after/pristine-consumer-shared.log) |
| The tables this engine embeds, and the area's tests in it | built in Release and run under `TZ=UTC`: **387 tests, none failed**. In `Broiler.JavaScript.BuiltIns.Tests`, 84: `SharedTimeZoneTests` (each embedded file held to the home's bytes and to the manifest's digests, the deleted types and resource absent, and every time-zone row), `SharedTimeZoneHostTests` (A15, A16) and `SharedCalendarTableTests` (the calendar rows). In `Broiler.JavaScript.Integration.Tests`, 303: `Issue773Tests`, `Issue775Tests`, `Issue783Tests` and `Issue794Tests`, which the cut-over changed, and `Issue786Tests`, which Z16's correction changed, its offset names with seconds among them. The builds had no error; the 6 warnings are xUnit's analyzers on integration tests outside the area | [`home/after/engine-build.log`](home/after/engine-build.log), [`home/after/engine-tests.log`](home/after/engine-tests.log) |

The provider implementation (clause 5) is section 2's, unchanged: no file of the library differs between
`2d37a38a`, whose code nuget.org's `0.1.0-preview.5` holds, and the after commit. Every result above is the one
the same steps gave at `5f96cd40`, but for Broiler.VM's head and the `Issue786Tests` added to the area's tests:
#1048 changed no file of the home, the library or the generator.

## 11. The pristine consumer

**The consumer runs, and so does the area through it.** [`engine/pristine-consumer.log`](engine/pristine-consumer.log):
the runtime graph of `Broiler.JavaScript.All` packed from `79a0506c` into 18 local packages, restored into a clean
consumer from that feed and nuget.org into an isolated package cache, and run: it printed `42`.

[`engine/pristine-consumer-time.log`](engine/pristine-consumer-time.log): the same consumer, from the same isolated
cache and offline, under `TZ=America/New_York` and no `LANG`, then evaluated fifteen expressions, each a correction
or a policy change of SJL-8.6, and printed **`ok 15`**:
- the release, backzone and the 2037 cliff: Vancouver in 2027, Amsterdam in 1937, New York in 2040 and its next
  transition after 2037-12-01 (Z1, Z3, Z4, A10);
- `Factory` refused, and the 445 primaries, `Europe/Bratislava` no longer `Europe/Prague` (Z5, Z7);
- `America/Loſ_Angeles` refused, an ISO string refused as the formatter's zone, Germany's two zones, and an offset
  in Arabic-Indic digits refused (Z10, Z11, Z12, A13);
- `chinese` 2057's ninth month, a `chinese` date at the lower limit, and the first two days of `persian` year 1
  (K2, K7, K12);
- the host's zone: `Temporal.Now.timeZoneId()`, and `Date`'s offsets in 1850, to the second, and in 2040 (A15);
- `Africa/Abidjan`'s `longOffset` name in 1900, `GMT-00:16:08` (Z16). This check is new since the run at
  `5f96cd40`, which had the other fourteen.

**Where the library came from.** The engine's packages depend on `Broiler.ECMAScript.Algorithms` by project
reference, so the engine set packs it too, at `0.1.0-preview.5`, and the consumer restored that package from the
local feed. It carries no signature, and its assembly is the after commit's build, not the bytes of nuget.org's
package of the same version; the code of the two is the same (section 10). `Broiler.ECMAScript.Generation` is not in
the consumer's closure. A consumer that restores the engine from nuget.org would take nuget.org's build.

**A control of the checks**, [`engine/pristine-consumer-checks-control.txt`](engine/pristine-consumer-checks-control.txt):
the same fifteen expressions, as one program, in the shell built at the rule's commit `15a1bd8f` and at
`79a0506c`, under `TZ=America/New_York`. At the base all fifteen fail.
`getTimeZoneTransition` answers `null` after 2037, and K7's date throws the CLDR package's .NET
`IndexOutOfRangeException`. After, `ok 15`. So the consumer's `ok 15` is SJL-8.6's, not a property of any
build.

## 12. The simplification check (clauses 7 and 8)

**The deletion check.** [`simplification/deletion-check.log`](simplification/deletion-check.log) searches each
entry of the area's Broiler.JS deletion list in the file or folder that held it, with a pattern naming the members
the entry held at the rule's commit. The list is the milestone's, SJL-8.6, as JSD-0076's X6 reads and extends it.
**Every entry is absent.** No file remains under `Temporal/Tz/` or `scripts/tzdata/`, and no `tzdata.bin` anywhere.

| Entry | What remains | Reading |
|---|---|---|
| `Temporal/Tz/IanaTimeZone`, `IanaTimeZoneDatabase` and `TzifReader` | `SharedTimeZoneTests`' assertion that none of the three is in `BuiltIns`, and the changelog | absent |
| The embedded `tzdata.bin` and its `EmbeddedResource` item | the same test's assertion that no such resource is embedded, and the changelog | absent |
| `scripts/tzdata/generate_tzdata.py` | the workflow's and part C's harness's records of the host's tzdata release, read from `/usr/share/zoneinfo/tzdata.zi` (the host-zone record parts A and B compare), and the changelog | absent |
| `JSTemporalZonedDateTime`'s offset, candidate, disambiguation, start-of-day and transition helpers, with the zero offset and the local-midnight fallback | `GetOffsetNanosecondsFor`, now one line calling the library's `ZonedTime.GetOffsetNanosecondsFor` through `TemporalTimeZones`; `TryMatchZoneOffset`, InterpretISODateTimeOffset's matching of an explicit offset against the library's possible instants (Temporal's own, SJL-10's); comments naming the specification's operations; and `TemporalTimeZones`' adapters, each a call to `ZonedTime` | the helpers are absent |
| `JSIntl`'s offset-zone normalisation and its region table for `getTimeZones` | a comment on week data, "CLDR's full per-region data is not bundled" | absent |
| The formatting engine's zone helpers, with their host-zone fallback for an unknown name | calls to the shared database (`TemporalTimeZones.Database`, `Rules`), and the host's zone for a formatter created with none, which `Date`'s provider reads from the shared data. An unknown name is refused when the formatter is created | absent |
| `TemporalLunisolarCalendar` and `TemporalSolarHijriCalendar` | the test's list of the deleted types, the changelog, and `IsLunisolar`, a predicate on a calendar's identifier in `TemporalTableCalendar` and `TemporalNonIso` | absent |
| The Umm al-Qura and .NET dispatch of `TemporalArithmeticCalendar` and `TemporalCalendarMath` | comments, and the dispatch to `TemporalTableCalendar`, whose years are the library's `CalendarYears.GetYear` over the embedded table. Neither file names Umm al-Qura's arithmetic, a .NET calendar or the CLDR package | absent, as X6 reads the entry (below) |
| This engine's uses of the CLDR package's `CldrChineseCalendar` and `CldrUmmAlQura` | the test's list of the deleted types, and the changelog | absent |

**How the list is read.** The milestone's entry names "the calendar classes that use .NET calendars or
computations: `TemporalLunisolarCalendar`, `TemporalSolarHijriCalendar`, `TemporalArithmeticCalendar` and
`TemporalCalendarMath`". JSD-0076's X6, the area verdict, reads it as the first two classes and "the Umm al-Qura and
.NET dispatch" of the other two, and this plan's area record says the same. Clause 7 is read that way here, and the
reading is the verdict's, not this bundle's. `TemporalArithmeticCalendar` and `TemporalCalendarMath` remain by name.
They hold the formula calendars (`coptic`, `ethiopic`, `ethioaa`, `islamic`, `islamic-civil`, `islamic-tbla`,
`hebrew`, `indian`), which are SJL-10's and which the library does not have yet, and the dispatch between them and
the table-backed calendars.

**The host search (clause 8).** [`simplification/host-search.log`](simplification/host-search.log) searches
`Temporal` and then the rest of the engine's product sources for the host's zone and for a .NET or CLDR calendar.
- **In `Temporal`, 7 lines, all in `TemporalTimeZones.Host()`.** It reads `TimeZoneInfo.Local` (or `Utc` if the host
  has none) to learn the host zone's IANA name, converting a Windows identifier if it must, and resolves that name
  through the shared database to its primary identifier, or `UTC` where the database does not hold it (A16). The
  zone's offsets, rules and transitions are the database's. The record keeps the .NET zone only for `Date`'s
  display names. This is the one input SystemTimeZoneIdentifier asks of the host, and decision SJL-D-19 leaves the
  host zone to each engine.
- **No .NET calendar** (`ChineseLunisolarCalendar`, `KoreanLunisolarCalendar`, `PersianCalendar`, `UmAlQuraCalendar`
  and the others, a constructed `Calendar`, or a culture's or format's `Calendar`) and **no CLDR package calendar**
  is named anywhere in `Temporal`. Its `System.Globalization` imports name `CultureInfo` and `NumberStyles` only:
  the invariant culture and number styles for reading and writing digits.
- **Outside `Temporal`**: `HostLocalTimeProvider`'s comments; `JSDate`'s CLR view, which converts a time value to a
  `DateTimeOffset` in `TimeZoneInfo.Local` for `JSDate.Value` and `DateTime` (kept by decision, since a
  `DateTimeOffset` holds whole minutes; `docs/public-api.md`); and the changelog. `Date`'s local time reads the
  shared database through `HostLocalTimeProvider` (SJL-D-19 as SJL-8.6 applies it).

**The duplicate search.** [`simplification/duplicate-search.log`](simplification/duplicate-search.log) searches
the engine's product sources for a surviving copy of the area outside the library, in three parts.

| Part | Hits | What they are |
|---|---|---|
| A zone's data or its reader: TZif, zoneinfo, tzdata, a POSIX TZ string, a Rule or Zone line | 6 | the changelog's text |
| The zone algorithms by name | 15 | adapters, each a call to the library through `TemporalTimeZones`: `DisambiguateEpochNs`, `StartOfDayFor`, `ValidStartOfDayEpochNs`, `GetTimeZoneTransition`, `GetOffsetNanosecondsFor`, `CanonicalizeTimeZone` (offset identifiers by `OffsetTimeZones`, names by the database), and `TemporalTimeZones`' own `PossibleEpochNanoseconds`, `Disambiguate`, `StartOfDay`, `NextTransition`, `PreviousTransition` and `ToDisambiguation`; and two readers of the `disambiguation` option |
| The calendars' years outside the tables: new moons, solar terms, astronomy, the 33-year rule, ICU4X's approximation, the table calendars' epochs | 21 | comments; the formula calendars' epochs (`islamic-civil`, SJL-10's); `TemporalTableCalendar`'s two epochs, from which it estimates where to start searching for the year a day falls in, the year itself being the library's; and the changelog |

**No hit is a copy of the area.** No clause reads the duplicate search. Against the same searches at `5f96cd40`,
every entry and every part has the same hits; only line numbers moved, in the changelog, which gained Z16's entry,
and in the formatter, whose `GmtOffset` grew by four lines.

## 13. Every correction listed (clause 11)

The correspondence's rows against [`Broiler.JS/CHANGELOG.md`](../../../Broiler.JS/CHANGELOG.md), "The time-zone and
calendar area moves to the data home's tables (2026-10-17, milestone SJL-8.6)" and "An offset's GMT name keeps its
seconds (2026-10-18, milestone SJL-8.6)":

| Rows | Class in the correspondence | In the changelog |
|---|---|---|
| Z10, Z11, Z12, A13 | correction | "Corrections the corpus names", each with its vectors and reason |
| A16, K2, K7, K12 | correction | "Corrections with no vector", each with the test that holds it |
| Z16 | zone display, SJL-9's in the correspondence; a correction of SJL-8.6 since 2026-10-18, at the owner's direction | "An offset's GMT name keeps its seconds", with its test, `Issue786Tests.DateTimeFormat_RendersTimeZoneName`; the 2026-10-17 entry's "Not this milestone's" quotes what it said before |
| A6, A11 | code no input reaches | "Code no input reached, deleted" |
| Z1, Z2, Z15 (release), Z3 (backzone), Z4, A5, A7, A10 (the 2037 cliff), Z5 (`Factory`), Z7 (primaries) | policy, SJL-D-20 | "Data policy by decision", each with its vectors |
| K4, K5, K6, K11, K14 | policy, SJL-D-21 | "Data policy by decision", "The calendar years that change source" |
| A15 | implementation-defined (the host zone) | "The host's zone has one dataset" |
| Z8 | policy that stays this engine's | "Not this milestone's" |
| K8, K9, K10 | agreement | named as unchanged |
| Z6, Z9, Z13, Z14, A1 to A4, A8, A9, A12, A14, K1, K3, K13, K15, K16 | agreement, or nothing observable | not corrections; nothing to list |
| K17 | SJL-10's | not this milestone's |

**Every correction and every policy change the correspondence names for this engine is listed**, with its row, and
so is Z16. The two regressions SJL-8.6's local subset found were fixed before the cut-over landed and are not
corrections of the base. One reading the cut-over made is listed with its row: a property bag's `offset` read by
the `UTCOffset` grammar, under A13. Sections 7 and 8 have no row in class C1 or C5, so test262 adds nothing to list.

## 14. Engine-level vectors at the after commit (corpus revision 23)

**Every vector of corpus revision 23 has the result its class expects, in both zones, at the after commit.** The
engine-level runner, `Broiler.JS/Broiler.JavaScript.Vectors.Tests` in Release, built once from a clean checkout of
`79a0506c` and run under `TZ=UTC` and `TZ=America/New_York` at once, from 07:22Z to 07:59Z on 2026-10-18. The machine
ran this bundle's other steps beside it; nothing in the run is timed. The build's log is
[`engine/after-build.log`](engine/after-build.log).

| Run | Result | Files |
|---|---|---|
| Revision 23, UTC | 92,434 vectors: 92,430 match, 4 classified divergences, 0 unclassified differences, 0 not implemented, and no divergence that no longer holds; the test's four cases pass | [`engine/after-revision-23-utc.log`](engine/after-revision-23-utc.log), [`engine/after-revision-23-utc.tsv.gz`](engine/after-revision-23-utc.tsv.gz) |
| Revision 23, New York | the same | [`engine/after-revision-23-new-york.log`](engine/after-revision-23-new-york.log), [`engine/after-revision-23-new-york.tsv.gz`](engine/after-revision-23-new-york.tsv.gz) |
| Their negative controls | one vector's expected value changed in a copy of the corpus, `numbers/to-fixed/0001`, reported as the one unclassified difference | [`engine/negative-control-after-revision-23-utc.tsv`](engine/negative-control-after-revision-23-utc.tsv), [`engine/negative-control-after-revision-23-new-york.tsv`](engine/negative-control-after-revision-23-new-york.tsv) |

The 4 are the number area's `numbers/literal/0082`, `0084`, `0088` and `0090`, which stay with SJL-14. **The two
reports hold the same rows, and each holds the same rows as section 4's run of revision 23 in its zone**, taken at
`2d496a55`, line for line; the negative controls are section 4's byte for byte. No vector's result moved with
Z16's correction.

## 15. What this bundle does not claim

- **Nothing about Broiler.VM.** Its clauses, 12 to 17, its own test262 and its run of the reference datasets are
  judged in its bundle of the same name, by its own rule, and this bundle reads none of its runs.
- **A verdict on the runs at `5f96cd40`.** They are observed, and section 5 names them; no part is judged there.
- **A revised rule.** Part C is met under the rule `15a1bd8f` committed, and no reading of a class is new.
- **Zone display names other than an offset's GMT name, and the formatter's resolved identifier.** The rest of
  zone display is SJL-9's, and the resolved identifier (Z8) stays this engine's until the specification edition
  both engines pin settles it.
- **The calendars' arithmetic and the formula calendars**, which are SJL-10's, with the 7 `temporal.js` lines.
- **A tzdata release other than 2026e** for this engine's area; the runners' tzdata, 2026c in every shard of all four runs, only as the
  host data parts A and B's records name, which after the cut-over gives the host zone's name and `Date`'s display
  names, and no offset.
- **A speed claim of any kind.** SJL-8 measures nothing.
- **Review**: the reviewer field says none and means it.
