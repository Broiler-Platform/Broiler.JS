# Evidence bundle SJL-6-001 (Broiler.JS) - the Date area

**Milestone:** SJL-6 of the shared JavaScript libraries programme,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-6-date.md`: its home clauses and its
Broiler.JS clauses. **Rules:** [`decision-rule.md`](decision-rule.md) (test262 per path, part A in UTC and
part B in `America/New_York`) and [`reference-arms.md`](reference-arms.md) (the library-level comparison),
both committed at SJL-6.1 before any code they judge. **Area verdict:**
[`SharedLibraries.md`](../../roadmap/SharedLibraries.md), "Area verdicts", with Broiler.VM's JSD-0072 and its
correspondence from code, retained in Broiler.VM's bundle of the same name. **Owner:** the Broiler.JS
maintainer; one person holds every role (the programme's EX-30, recorded in Broiler.VM). **Reviewer:** none,
and nothing here has been read by anyone but its author.

**State:** collected, 2026-10-16, at SJL-6.6, from clean checkouts of the after commit `4d70821e`, the merge
that landed SJL-6.5's cut-over. **The clauses this repository holds are met**: the home clauses and
Broiler.JS clauses 1 to 5 (section 6). **Clause 3 is met under part A of
[`decision-rule.md`](decision-rule.md) as written**, with no revision: in UTC no test262 row changed
(section 9). **Clause 4 is met under part B as written**, with no revision: in `America/New_York`, 12 rows
changed, six paths in both variants that failed at the base and pass after, each a correction of class C1
the changelog lists, and none regressed (section 10). Nothing is accepted: review is deferred, and SJL-6 may
not meet its gate before SJL-3 meets SJL-3's. Broiler.VM keeps its own bundle of the same name for its own
half, judged by its own rule; neither bundle reads the other's runs. *(Revised 2026-10-16, at SJL-6.6. The
paragraph read: "**State:** collection in progress. Nothing here is a verdict until its section says so.
**Taken:** both test262 bases (section 1), the engine-level runs on the Date area, at corpus revisions 14
and 15 (section 2), the reference-arm comparison, with this engine's runs on corpus revision 16, which holds
its class (b) kinds (section 3), and the cut-over's runs (section 4). The after runs and the rest of SJL-6.6
are added when they are taken. Broiler.VM keeps its own bundle of the same name for its own half, judged by
its own rule; neither bundle reads the other's runs.", with the note of its revision of 2026-10-15, at
SJL-6.3, which quoted its earlier sentences: "**Taken:** both test262 bases (section 1) and the engine-level
runs on the Date area, at corpus revisions 14 and 15 (section 2). The reference-arm comparison, the after
runs and the rest of SJL-6.6 are added when they are taken.", and said its fourth sentence ended at "which
holds its class (b) kinds (section 3)." until SJL-6.5.)*

## 1. The test262 bases (parts A and B)

| | Part A, UTC | Part B, `America/New_York` |
|---|---|---|
| Commit | `5fef116`, the commit that carries the rule, before any code of SJL-6 landed | the same |
| Taken | 2026-10-15, 14:33Z to 16:00Z, by `.github/workflows/test262-evidence.yml` (run 38752508689), dispatched from `main` at `e8b801f` with `time_zone` empty | 2026-10-15, 14:33Z to 16:04Z, by the same workflow (run 38752514354), dispatched from `main` at `e8b801f` with `time_zone` `America/New_York` |
| Configuration | the rule's: eight shards over `--all-script-host-verifiable`, terser at its pinned version, the per-test timeout of 30 seconds, no memory limit, four workers, the suite at `ccaac100` | the same |
| Retained | [`test262/base/`](test262/base/): the workflow's artifact `test262-evidence`, every file unchanged, with [`imported.txt`](test262/base/imported.txt), which records its digest | [`test262/base-new-york/`](test262/base-new-york/), likewise, with [`imported.txt`](test262/base-new-york/imported.txt) |
| Zone | every shard in the expected zone: TZ unset, UTC +0000 at both instants, tzdata 2026c ([`host-zone.log`](test262/base/host-zone.log)) | every shard in the expected zone: TZ `America/New_York`, EST -0500 on 2021-01-15 and EDT -0400 on 2021-07-15, tzdata 2026c ([`host-zone.log`](test262/base-new-york/host-zone.log)) |
| Shell | one digest of the shell's binaries across the eight shards and the re-run ([`shell-hashes.log`](test262/base/shell-hashes.log)) | one digest, the same as part A's ([`shell-hashes.log`](test262/base-new-york/shell-hashes.log)) |
| Timed out | one path, `test/staging/sm/regress/regress-1507322-deep-weakmap.js`, in both variants; run again alone with one worker, it timed out again, and the re-run replaces its rows ([`rerun-timeouts.log`](test262/base/rerun-timeouts.log)) | the same path, with the same re-run ([`rerun-timeouts.log`](test262/base-new-york/rerun-timeouts.log)) |

The two parts' runs are never compared with each other: the rule says so. Each is compared with its own
after run, at the commit that lands SJL-6.5's cut-over, in the same zone.

## 2. Engine-level vectors: the first runs on the Date area (SJL-6.2)

Corpus revision 14 (`ff6cd24`) adds the Date area: 7,772 vectors in seven files under
`conformance/vectors/date/`, from `scripts/conformance/dateref.py`. None of them depends on the time zone,
so the engine-level runner, `Broiler.JS/Broiler.JavaScript.Vectors.Tests` in Release, was run on it twice,
from one build, once under `TZ=UTC`, as this repository's tests run, and once under
`TZ=America/New_York`, as part B runs:

| Run | Result | Files |
|---|---|---|
| Revision 14, UTC | 50,715 vectors: 50,646 match, 4 classified divergences, 65 unclassified differences | [`engine/run-revision-14-utc.log`](engine/run-revision-14-utc.log), [`engine/run-revision-14-utc.tsv.gz`](engine/run-revision-14-utc.tsv.gz) |
| Revision 14, New York | 50,715 vectors: 49,695 match, 4 classified divergences, 1,016 unclassified differences | [`engine/run-revision-14-new-york.log`](engine/run-revision-14-new-york.log), [`engine/run-revision-14-new-york.tsv.gz`](engine/run-revision-14-new-york.tsv.gz) |

The 4 are the number area's `strict-non-octal-decimal` fields, which stay with SJL-14. Every one of the 65
also differs in New York. Corpus revision 15 (`a3452d9`) classifies the 1,016, each under the row of
bundle `sjl-6-001`'s correspondence from code (Broiler.VM's `docs/evidence/sjl-6-001/correspondence.md`)
that it confirms; SJL-6.5 answers each one:

| Row | Vectors | Zones | What this engine does |
|---|---|---|---|
| L2 | 440 `date/offset`, 440 `date/to-string`, 10 `date/local-string`, `date/local/0035`, `date/date-time/0003` | New York | local time takes the zone's base offset where a date is made from local fields and read back by the local getters, and the offset in force at the instant where `getTimezoneOffset`, `toString` and some of `Date.parse`'s forms read it. Each of the 880 relation vectors that differs is an instant in daylight-saving time, and each one that matches is not; 8 of the `local-string` vectors are local dates in the years 1 to 999, before the zone's standard time, where its offset in the tzdata is local mean time |
| P1 | 8 `date/date-only`, 51 `date/utc` | New York | a date-only form for a year from 0001 to 9999 is read as local time |
| G2 | 22 `date/iso-week` | both | the ISO week of a date near the start or end of a negative year is counted with a truncating division |
| P9 | 16 `date/round-trip` | both | `Date.parse` reads none of the engine's own `toUTCString` renderings of a year below 1 or above 9999 |
| T3 | 14 `date/setter-order` | both | `setUTCMinutes`, `setUTCSeconds`, `setUTCMonth`, `setHours`, `setMinutes`, `setSeconds` and `setMonth` make the date invalid before converting their later arguments when the first is `undefined` or NaN |
| D1 | `date/utc-setter/0007`, `0010`, `0011`, `date/local-setter/0001`, `0002` | both | a setter whose valid result lies outside the years 0001 to 9999 answers NaN |
| D2 | `date/utc-setter/0008`, `0012`, `0021`, `date/local-setter/0004`, `0009` | both | a setter on a date stored outside those years answers NaN and leaves the date unchanged |
| D3 | `date/utc-setter/0019`, `date/round-trip/0021` | both | the instant 0001-01-01T00:00Z is the engine's invalid-date sentinel |
| T4 | `date/make-day/0863` | both | a month of 1e19 saturates as a 64-bit integer |

On revision 15, with the same build:

| Run | Result | Files |
|---|---|---|
| Revision 15, New York | 50,715 vectors: 49,695 match, 1,020 classified divergences, 0 unclassified differences | [`engine/run-revision-15-new-york.log`](engine/run-revision-15-new-york.log), [`engine/run-revision-15-new-york.tsv.gz`](engine/run-revision-15-new-york.tsv.gz) |
| Revision 15, UTC | 50,715 vectors: 50,646 match, 69 classified divergences, 0 unclassified differences; the 951 fields of rows L2 and P1 match there and are reported as no longer holding | [`engine/run-revision-15-utc.log`](engine/run-revision-15-utc.log), [`engine/run-revision-15-utc.tsv.gz`](engine/run-revision-15-utc.tsv.gz) |

**Every vector of revision 15 has a classified result in this engine, in both zones.** The 951 are
correct under UTC: their difference needs an offset other than zero. A field that no longer holds under
UTC is retired only when its correction is listed and the run in New York shows it gone. The negative
control ([`engine/negative-control-revision-15.log`](engine/negative-control-revision-15.log),
[`engine/negative-control-revision-15.tsv`](engine/negative-control-revision-15.tsv)) runs the 280 vectors
of `numbers/to-fixed.vectors` from a copy of the corpus in which `numbers/to-fixed/0001`'s expected value is
changed; that vector is the one verdict that moves, and it moves to a difference.

**What the runs say of the area verdict.** Every difference is a row the verdict classified, so its
falsifier does not fire, and no new record is needed. The vectors confirm rows T3, T4, D1 to D3, G2, P1, P9
and L2 for this engine, and refine two. Row T3 was worked from the code; the vectors show it for seven
setters, and `setUTCHours`, `setUTCFullYear` and `setFullYear` convert as the specification says. Row L2's
mechanism also shows before the zone's standard time, where the tzdata's offset is local mean time. Rows L3,
L4, L5, D4, D5, R3 and R4, and the fallback rows P4 to P7 beyond the numeric slash dates, have no vector:
their outcome depends on the zone, on the culture or on the host, or lies outside ECMA-262, and the
reference arms and part B's run carry them. The 22 numeric slash dates, `impl-defined`, match this engine's
expectation, its slash-date heuristic, which decision SJL-D-17 keeps.

## 3. The reference-arm comparison (SJL-6.3)

**MET**, with one judgement it states, under [`reference-arms.md`](reference-arms.md) as revised before any
run ([`reference-arms-revised-2026-10-15.md`](reference-arms-revised-2026-10-15.md)) and after the first
judged run was seen
([`reference-arms-revised-2026-10-15-after-first-run.md`](reference-arms-revised-2026-10-15-after-first-run.md),
weaker evidence on the two points it decides). The run, its first run, the three culture runs and the
negative controls are in [`reference-arms-run/`](reference-arms-run/README.md), whose README reads the MET
line clause by clause. In short: the Date area of `Broiler.ECMAScript.Algorithms` (`c69b70c`, with two
defects the first run showed fixed at `b75d12e9`) against arm J, this engine's code at `bc3597f` with its
eight other civil copies, and arm V, Broiler.VM's at `9c3d809`, over 13 population files; every difference
is a correction of an arm (class (a)) or a string outside the format the engines read differently (class
(b)), and none is unclassified. The corrections confirm rows G2, L3, L4, P8 and P9 of the correspondence for
this engine, and under the `fa-IR` and `ar-SA` cultures show a second way row P8's read-back fails, through
the culture's calendar.

**The class (b) kinds in vectors.** Corpus revision 16 (`6f29deb`) appends 12 `impl-defined` vectors,
`date/outside-format/0001` to `0012`, one or two for each of the comparison's 12 class (b) kinds. This
engine's expectation is its reading before its cut-over; Broiler.VM's is its dialect's reading as the library
ports it. The engine-level runner was run on revision 16 from one build of `6f29deb`, in both zones:

| Run | Result | Files |
|---|---|---|
| Revision 16, UTC | 50,727 vectors: 50,658 match, 69 classified divergences, 0 unclassified differences; the 951 fields of rows L2 and P1 are again reported as no longer holding | [`engine/run-revision-16-utc.log`](engine/run-revision-16-utc.log), [`engine/run-revision-16-utc.tsv.gz`](engine/run-revision-16-utc.tsv.gz) |
| Revision 16, New York | 50,727 vectors: 49,707 match, 1,020 classified divergences, 0 unclassified differences | [`engine/run-revision-16-new-york.log`](engine/run-revision-16-new-york.log), [`engine/run-revision-16-new-york.tsv.gz`](engine/run-revision-16-new-york.tsv.gz) |

The 12 new vectors match in both zones, the two read through the local fields included; every other verdict
is revision 15's. The runs were taken together, from 19:08Z to 19:31Z.

## 4. This engine's cut-over (SJL-6.5)

On corpus revision 16, with the product sources of the change that lands SJL-6.5's cut-over, from one
build, once under `TZ=UTC` and once under `TZ=America/New_York`:

| Run | Result | Files |
|---|---|---|
| Revision 16, UTC | 50,727 vectors: 50,721 match, 4 classified divergences, 2 unclassified differences | [`engine/cut-over-revision-16-utc.log`](engine/cut-over-revision-16-utc.log), [`engine/cut-over-revision-16-utc.tsv.gz`](engine/cut-over-revision-16-utc.tsv.gz) |
| Revision 16, New York | 50,727 vectors: 50,721 match, 4 classified divergences, 2 unclassified differences | [`engine/cut-over-revision-16-new-york.log`](engine/cut-over-revision-16-new-york.log), [`engine/cut-over-revision-16-new-york.tsv.gz`](engine/cut-over-revision-16-new-york.tsv.gz) |

**Each of the 1,016 fields of section 2 reports "divergence no longer holds" in both zones**, the 951 of
rows L2 and P1 in New York, where they held before the cut-over, as well as in UTC; every correction is
listed in [`Broiler.JS/CHANGELOG.md`](../../../Broiler.JS/CHANGELOG.md). The 4 are the number area's, as
before. **The 2 are `date/outside-format/0001` and `0010`**, `impl-defined` vectors whose expectation for
this engine is its reading before the cut-over (section 3): this engine now reads both in Broiler.VM's
dialect, after its own grammar refuses them, as JSD-0072's order of grammars has it, `"0000-02-30T00:00Z"`
as March 1 of year 0, -62162035200000, and `"Thu Jan 01 -5 00:00:00 GMT+0000 (Coordinated Universal Time)"`
as the year -5, -62324985600000. The runner fails on them, as it must until the corpus changes the
expectation; the plan, `SharedLibraries.md`'s record of SJL-6.5, states the reading, and the next corpus
revision takes it from there, not from this run. The runs were taken together, from 08:41Z to 09:03Z.

**Every vector of corpus revision 17 has the result its class expects, in both zones.** At `520ecf9`,
which carries revision 17, retires the 1,016 fields with Broiler.VM's 16
([`engine/retired-divergences-revision-17.tsv`](engine/retired-divergences-revision-17.tsv)) and states this
engine's reading of the two strings, with the same product sources, from a clean checkout:

| Run | Result | Files |
|---|---|---|
| Revision 17, UTC | 50,727 vectors: 50,723 match, 4 classified divergences, 0 unclassified differences | [`engine/run-revision-17-utc.log`](engine/run-revision-17-utc.log), [`engine/run-revision-17-utc.tsv.gz`](engine/run-revision-17-utc.tsv.gz) |
| Revision 17, New York | 50,727 vectors: 50,723 match, 4 classified divergences, 0 unclassified differences | [`engine/run-revision-17-new-york.log`](engine/run-revision-17-new-york.log), [`engine/run-revision-17-new-york.tsv.gz`](engine/run-revision-17-new-york.tsv.gz) |

The 4 are the number area's. The runner's test passes in both zones. The runs were taken together, from
09:08Z to 09:29Z.

## 5. The after arm (SJL-6.6)

| Field | Value |
|---|---|
| Evidence bundle | SJL-6-001 (Broiler.JS's) |
| Bases | Section 1: the workflow's runs at `5fef116`, the commit that carries the rule, before any code of SJL-6 landed, part A's in UTC and part B's in `America/New_York`, each with its re-run of the rows that timed out |
| After | `4d70821e`: SJL-6.5's cut-over (`31c0480`) and corpus revision 17 (`520ecf9`), as merged into the default branch in `4d70821e` (Broiler-Platform/Broiler.JS#1035), the merge that landed them, as bundle `sjl-5-001` took its first after run at the merge that landed its cut-over. Its product sources differ from the cut-over's in three files, which [`identity.txt`](identity.txt) lists: the runners' Date adapter (`Broiler.ECMAScript/runners/AlgorithmsLibrary.Date.cs`, revision 17's reading of a string outside the format), a test (`SharedDateAlgorithmTests.cs`, its zone collection set on Windows too) and the changelog; none is product code. Every local run of this bundle is taken from a clean checkout of it |
| Rule commit | `5fef116`, an ancestor of the after commit; the rule, the comparison script and the workflow are unchanged between the two ([`identity.txt`](identity.txt)) |
| Shared library | `Broiler.ECMAScript.Algorithms` at the after commit, whose code is that of the package published to nuget.org as `0.1.0-preview.4`, from `d1e4b11`, whose library code is `e04874d`'s: `git diff e04874d 4d70821e -- Broiler.ECMAScript/Broiler.ECMAScript.Algorithms` is empty. Section 7's pack is of that code under the same version, into an isolated feed |
| Shared vectors | `conformance/vectors` at corpus revision 17 ([`REVISIONS.md`](../../../conformance/vectors/REVISIONS.md)) |
| Collector | [`collect.py`](collect.py) as this change commits it, which reuses bundle `sjl-5-001`'s collector, and through it `sjl-4-001`'s, `sjl-3-001`'s and `sjl-0-001`'s, run against the clean checkout |
| Taken | 2026-10-16. Both after runs by the workflow from 10:36Z, dispatched together with Broiler.VM's after run, part A's completing at 12:01Z and part B's at 12:08Z; locally, `identity` and `simplification` at 10:43Z, then, after Broiler.VM's local steps, `home` from 11:31Z, `vectors` from 11:33Z to 12:17Z, `test262 --part A` at 12:08Z and `--part B` at 12:11Z as each run completed, and `consumer` at 12:17Z, on the machine Broiler.VM's bundle names |
| RID | `linux-x64`: the workflow's runners for test262, and for the local runs one machine with four logical processors |

## 6. The clauses, and what holds each

| Clause | What it asks | Where | State |
|---|---|---|---|
| Home | Every sub-area that was not refused is implemented under the conventions, and its vectors and reference arms pass under the area rule | [`SharedLibraries.md`](../../roadmap/SharedLibraries.md), "Area verdicts"; sections 3 and 7 | **Met**: the four sub-areas, all accepted at SJL-6.1, are in `Broiler.ECMAScript.Algorithms` at `4d70821e`; every vector of corpus revision 17 passes at library level, under the JIT and Native AOT alike; the reference arms MET (section 3) |
| Home | The `SL` rules, the API baseline and package validation pass, and the Native AOT probe runs | section 7 | **Met** at `4d70821e` |
| 1 | The deletion list is absent | section 12 | **Met**: every entry absent at `4d70821e`; what remains is calls to the library, adapters, other members of the same name and comments |
| 2 | Date storage is migrated | [`docs/public-api.md`](../../public-api.md), "Public API changes, 2026-10-16"; sections 4 and 8 | **Met**: a `Date`'s storage is its time value alone since `31c0480`, and the vectors outside the years 1 to 9999 pass in both zones |
| 3 | test262 for `Date` and its `Temporal` interop meets the area rule, per path | section 9, [`test262/`](test262/) | **Met** under part A of the rule as written: no row of the whole suite changed, the `Date` and `Temporal` paths among them |
| 4 | The non-UTC host run meets its rule | section 10 | **Met** under part B of the rule as written: 12 rows changed, all C1, none regressed |
| 5 | Every correction is listed | [`Broiler.JS/CHANGELOG.md`](../../../Broiler.JS/CHANGELOG.md) | **Met**: every correction the area record names for this engine, each under its row of the correspondence, T3, T4, G2, P1, P8, P9, R3, D1 to D5 and L2 to L4, with the readings decided at SJL-6.5 (P4 to P6, R5) and the `Temporal.PlainMonthDay` bound check the record did not name |

Clauses 6 to 9 are Broiler.VM's, judged in its bundle of the same name. None is accepted: review is deferred,
and SJL-6 may not meet its gate before SJL-3 meets SJL-3's.

## 7. The home: the library at the after commit

From a clean checkout of `4d70821e`, into [`home/`](home/):

| Check | Result | Where |
|---|---|---|
| The shared filter's tests, built in Release | **456 passed, none failed** (111, 313 and 32 in the three projects): SL1 to SL10 each in both directions and the register holding them `Active`, the API baseline in both directions and package validation on for every packed shared library (SL6), the generated data byte-identical to its generator (SL7), the area's own tests, and `Every_date_vector_passes_against_the_algorithms_library` with the number, numerics and text areas' | [`home/build.log`](home/build.log), [`home/tests.log`](home/tests.log) |
| The corpus generators' own tests | 121, OK | [`home/generator-tests.log`](home/generator-tests.log) |
| The Native AOT probe, `linux-x64` | published with no warning, and the publish directory holds no managed assembly; the image ran corpus revision 17, **50,727 vectors, no failure**, and its report is byte-identical to the same probe's under the JIT | [`home/probe-publish-linux-x64.log`](home/probe-publish-linux-x64.log), [`home/probe-run-linux-x64.log`](home/probe-run-linux-x64.log), [`home/probe-report-aot.tsv.gz`](home/probe-report-aot.tsv.gz) |
| The shared set at `0.1.0-preview.4` | packed from the after commit with package validation, restored from a local feed alone into an isolated cache, and its consumer run | [`home/pristine-consumer-shared.log`](home/pristine-consumer-shared.log) |

## 8. Engine-level vectors at the after commit

**No difference, in either zone.** `Broiler.JavaScript.Vectors.Tests` at `4d70821e`, built once and run once
under `TZ=UTC` and once under `TZ=America/New_York`, over corpus revision 17:

| Run | Result | Files |
|---|---|---|
| UTC | **50,727 vectors: 50,723 match, 4 classified divergences, 0 differences**; the test's four cases pass, its negative control among them | [`engine/after-revision-17-utc.log`](engine/after-revision-17-utc.log), [`engine/after-revision-17-utc.tsv.gz`](engine/after-revision-17-utc.tsv.gz) |
| New York | **50,727 vectors: 50,723 match, 4 classified divergences, 0 differences**; the test's four cases pass, its negative control among them | [`engine/after-revision-17-new-york.log`](engine/after-revision-17-new-york.log), [`engine/after-revision-17-new-york.tsv.gz`](engine/after-revision-17-new-york.tsv.gz) |

The four are `numbers/literal/0082`, `0084`, `0088` and `0090`, a `NonOctalDecimalIntegerLiteral` accepted in
strict mode code, which the changelog lists as not corrected here and SJL-14 answers, as at SJL-3 to SJL-5.
Each report holds the same rows as section 4's run of revision 17 in its zone, taken at `520ecf9`, line for
line (the compressed files differ in their gzip headers alone), and the two zones' reports hold the same rows.
The build's log is [`engine/after-build.log`](engine/after-build.log).

## 9. Test262 per path, in UTC (rule part A)

**MET under the rule as written, at the after commit `4d70821e`: no row changed.**
[`test262/comparison-utc/comparison.json`](test262/comparison-utc/comparison.json) and
[`test262/comparison-utc/comparison.log`](test262/comparison-utc/comparison.log): the after run against part A's
base of section 1, through `scripts/compliance/compare_test262_paths.py` as the rule committed it at SJL-0.9,
with each run's timed-out rows replaced by their re-runs. **96,720 rows (48,360 paths, each in both variants): 0
changed, 0 regressions**, so no classification file is needed, and none is retained.

| Part A's condition | This run |
|---|---|
| Both runs complete | eight of eight shards reported in each, no incomplete shard, no configuration failure, one suite revision, `ccaac100`, and one shell in each, its binaries' hashes the same in every shard and in the re-run ([`test262/after/merge.log`](test262/after/merge.log), [`test262/after/shell-hashes.log`](test262/after/shell-hashes.log); the base's in [`test262/base/`](test262/base/)) |
| Every shard in the zone, and the two runs' host-zone records alike | every shard of each in the runner's own zone, and the records' lines the same: `TZ` unset, UTC +0000 at both instants, tzdata 2026c ([`test262/comparison-utc/zone-records.log`](test262/comparison-utc/zone-records.log), from each run's `host-zone.log`) |
| The script finds them comparable | yes ([`test262/comparison-utc/compare-checks.log`](test262/comparison-utc/compare-checks.log): the after run against itself changes nothing, and a planted regression is reported, exit 1); the fields it records and does not compare are the same in both: .NET SDK 10.0.402, four workers, eight shards, Linux, X64 |
| No row passing at the base fails, times out or is skipped after | none |
| Every changed verdict in C1, C3 or C5, and listed | **met**: no verdict changed |

**The after run.** Taken by `.github/workflows/test262-evidence.yml` at `4d70821e`, in the rule's configuration
([`test262/after/run.txt`](test262/after/run.txt)): run
[38888323117](https://github.com/Broiler-Platform/Broiler.JS/actions/runs/38888323117), attempt 1, dispatched
2026-10-16 10:36Z from `main` at `4d70821e` with `time_zone` empty, and completed 12:01Z,
`--all-script-host-verifiable` over eight shards, terser 5.50.0, 30 seconds per test, no memory limit, four
workers, the .NET SDK 10.0.402, on runner image ubuntu24 20261012.334.1, the base's. It is imported into
[`test262/after/`](test262/after/) unchanged, every file of the workflow's artifact, with
[`imported.txt`](test262/after/imported.txt), which records the digest of the zip as downloaded, the digest
GitHub records. **Timeouts.** As at the base, one path timed out in both variants,
`test/staging/sm/regress/regress-1507322-deep-weakmap.js`; the workflow ran it again alone with one worker at the
same commit, and it timed out again in both ([`test262/after/rerun-timeouts.log`](test262/after/rerun-timeouts.log)),
so the rows compare timed out against timed out. The merged totals, which do not decide part A, are the base's:
94,245 variant cases passed, 1,244 failed, 2 timed out and 1,229 skipped.

**What it says.** In UTC, the cut-over moved no verdict of the suite: every `Date`, `Temporal` and
`DateTimeFormat` path gives the base's verdict, as SJL-6.5's local run of those directories found (a check its
pull request records and no bundle retains). The corrections whose effect needs an offset other than zero are
part B's to show; the others, the storage outside the years 1 to 9999 among them, are cases test262 at this revision does not hold, and the
vectors and `SharedDateAlgorithmTests` do.


## 10. Test262 per path, in `America/New_York` (rule part B)

**MET under the rule as written, at the after commit `4d70821e`; no row regressed.**
[`test262/comparison-new-york/comparison.json`](test262/comparison-new-york/comparison.json) and
[`test262/comparison-new-york/comparison.log`](test262/comparison-new-york/comparison.log): the after run against
part B's base of section 1, through the same script, with each run's timed-out rows replaced by their re-runs
and [`test262/comparison-new-york/classifications.json`](test262/comparison-new-york/classifications.json) read.
**96,720 rows: 12 changed, every one admitted as C1, 0 unclassified, and no row passing at the base fails, times
out or is skipped after.** This is milestone clause 4's run: the area's evidence under a host zone with
daylight-saving time, where this engine's provider is exercised.

| Part B's condition | This run |
|---|---|
| Both runs complete | eight of eight shards reported in each, no incomplete shard, no configuration failure, one suite revision, `ccaac100`, and one shell in each, its binaries' hashes the same in every shard and in the re-run ([`test262/after-new-york/merge.log`](test262/after-new-york/merge.log), [`test262/after-new-york/shell-hashes.log`](test262/after-new-york/shell-hashes.log)); the after run's shell is part A's after run's, by its hashes |
| Every shard in the zone, and the two runs' host-zone records alike | every shard of each with `TZ` `America/New_York`, and the records' lines the same: EST -0500 on 2021-01-15, EDT -0400 on 2021-07-15, tzdata 2026c ([`test262/comparison-new-york/zone-records.log`](test262/comparison-new-york/zone-records.log)) |
| The script finds them comparable | yes ([`test262/comparison-new-york/compare-checks.log`](test262/comparison-new-york/compare-checks.log): the after run against itself changes nothing, and a planted regression is reported, exit 1); the fields it records and does not compare are the same in both: .NET SDK 10.0.402, four workers, eight shards, Linux, X64 |
| No row passing at the base fails, times out or is skipped after | none |
| Every changed verdict in C1, C3 or C5, and listed | **met**: the 12 rows below, all C1, each with its reason in the classification file, which names no row the comparison did not change |

**The changed rows.** Each failed at the base and passes after, in both variants; the base's message is the
base run's own (`test262/base-new-york/test262-results.json.gz`).

| Path | Variants | Base | After | Class | Why |
|---|---|---|---|---|---|
| `test/built-ins/Date/15.9.1.15-1.js` | original, terser | failed | passed | C1 | correction P1, made by the shared strict dialect: `new Date("1970")` read the date-only form as local midnight, so its `toISOString()` was `"1970-01-01T05:00:00.000Z"`; ECMA-262 21.4.3.2 and 21.4.1.32 read a date-only form as UTC. Vectors: `date/date-only` and `date/utc`, reason `js-date-only-local`. The changelog: "A date-only form is UTC" |
| `test/built-ins/Date/parse/without-utc-offset.js` | original, terser | failed | passed | C1 | the same: `Date.parse('1970-01-01')` answered 18000000, local midnight, where the specification asks for 0 |
| `test/built-ins/Date/prototype/setFullYear/this-value-invalid-date.js` | original, terser | failed | passed | C1 | correction L2, the provider's offsets: `new Date(2016, 6)` took the zone's base offset, EST, and was 05:00Z, where `setFullYear(2016, 6)` on an invalid date took the offset in force, EDT, and answered 04:00Z; ECMA-262 21.4.1.25 LocalTime and 21.4.1.26 UTC read the offset in force at the instant. Vectors: `date/offset`, `date/to-string`, `date/local-string`, `date/local` and `date/date-time`, reason `js-offset-mixed`. The changelog: "One offset per instant, the one in force at it" |
| `test/built-ins/Date/prototype/setFullYear/arg-year-to-number.js` | original, terser | failed | passed | C1 | the same row: `setFullYear(2)` on a date of 2016-07-07 answered a time value 180,000 ms from that of `new Date(-1, 42, 7, ...)`, the same local time, because the two paths took different offsets for it; in the year 2 the zone's offset in the tzdata is its local mean time, which is not its base offset |
| `test/built-ins/Date/prototype/toISOString/15.9.5.43-0-4.js` | original, terser | failed | passed | C1 | the same row: `new Date(1999, 9, 10, 10, 10, 10, 10)` took EST while `getTimezoneOffset()` read EDT, 240, so the test's local date was an hour late, `"1999-10-10T11:10:10.010Z"` |
| `test/harness/assertRelativeDateMs.js` | original, terser | failed | passed | C1 | the same row, in the harness's own test: `assertRelativeDateMs` subtracts `getTimezoneOffset()` from the time value, and `new Date(2016, 3, 12, 13, 21, 23, 24)` took EST while `getTimezoneOffset()` read EDT |

The classification file names each row with its reason in full. **Classes C3 and C5** list no row. The five
`Date` paths are the five that SJL-6.5's local run of the `Date`, `Temporal` and `DateTimeFormat` directories
found failing at the base and passing after, a check that change's pull request (Broiler-Platform/Broiler.JS#1035)
records and no bundle retains; the harness's own test is outside those directories and shows the same
correction. No row of the corrections that need no offset other than zero changed here, as none changed in
part A.

**The after run.** Taken by `.github/workflows/test262-evidence.yml` at `4d70821e`, in part B's configuration
([`test262/after-new-york/run.txt`](test262/after-new-york/run.txt)): run
[38888327712](https://github.com/Broiler-Platform/Broiler.JS/actions/runs/38888327712), attempt 1, dispatched
2026-10-16 10:36Z from `main` at `4d70821e` with `time_zone` `America/New_York`, and completed 12:08Z, in the
configuration of part A's run and on the same runner image. It is imported into
[`test262/after-new-york/`](test262/after-new-york/) unchanged, every file of the workflow's artifact, with
[`imported.txt`](test262/after-new-york/imported.txt), which records the digest of the zip as downloaded, the
digest GitHub records. **Timeouts.** As at part B's base, one path timed out in both variants,
`test/staging/sm/regress/regress-1507322-deep-weakmap.js`, and timed out again alone with one worker
([`test262/after-new-york/rerun-timeouts.log`](test262/after-new-york/rerun-timeouts.log)). The merged totals,
which do not decide part B: 94,245 variant cases passed, 1,244 failed, 2 timed out and 1,229 skipped, where
part B's base's were 94,233, 1,256, 2 and 1,229. The two parts' runs are not compared with each other, as the
rule says.

## 11. The pristine consumer

**The consumer runs.** [`engine/pristine-consumer.log`](engine/pristine-consumer.log): the runtime graph of
`Broiler.JavaScript.All` packed from `4d70821e` into 18 local packages, the shared
`Broiler.ECMAScript.Algorithms` `0.1.0-preview.4` among them, restored into a clean consumer from that feed
alone and run: it printed `42`. The shared set's own consumer is section 7's.

## 12. The simplification check

**The deletion check.** [`simplification/deletion-check.log`](simplification/deletion-check.log) searches
each entry of the area's Broiler.JS deletion list (Broiler.VM `docs/javascript-shared-libraries/milestones/
sjl-6-date.md`, SJL-6.5, as JSD-0072's X6 extends it per sub-area) in the file or folder that held it, with a
pattern for what the entry deleted. **Every entry is absent.** The lines that remain are calls to the
library, adapters, other members of the same name and comments:

| Entry | What remains | Reading |
|---|---|---|
| `JSDateMath`'s arithmetic, its unused `DaysInYear` and `TimeFromYear`, and Intl's dozen reads of it | a comment in an integration test, `Issue838Tests`, that names the old path | absent |
| The inline MakeFullYear copies, and the dead one | nothing | absent: `getYear` subtracts 1900 from `YearFromTime`, which the pattern does not name, and `EcmaTime.MakeFullYear` is the library's |
| `JSDate.ToDateTime`, `invalidDate` and the unused `IsValid(JSValue, ...)` | nothing | absent |
| `JSDate.DaysInMonth` | nothing | absent |
| The Temporal civil copies (the leap year, the month length, `DaysFromCivil` and `CivilFromDays`, `IsoDayOfWeek`, `IsoWeek`, `WeeksInIsoYear`, `IsValidISODate`, the inline days of the year, and the `FloorDiv` beside each that serves only them) | calls to `IsoCalendarMath` (`DaysInMonth`, `IsLeapYear`, `EpochDays`, `FromEpochDays`) and the Temporal types' own `daysInMonth` getters; `JSTemporalPlainDate.EpochDaysFor` and `DateFromEpochDays`, now one-line calls to the library, and their callers in `JSTemporalDuration`; the `FloorDiv`s that balance a year and month (`FloorDiv(total, 12)`) or split nanoseconds into seconds and days, which the area record leaves (`BalanceYearMonth` has no target in `IsoCalendarMath`), each followed by the library's `FromEpochDays` where it splits days; and the non-ISO calendars' own month lengths and leap years (`TemporalArithmeticCalendar`, `TemporalCalendarMath`, `TemporalLunisolarCalendar`, `TemporalNonIso`, `TemporalSolarHijriCalendar`), which stay with SJL-8 and SJL-10 | the copies are absent; what remains holds no ISO civil-day arithmetic |
| Intl's `DayOfWeek` | nothing | absent: the weekday is `EcmaTime.WeekDay` inline |
| `TryParseExtendedIso` with `EcmaIsoDateTime`, and `DateParser`'s lists of the format (`DefaultFormats` and the `\dT` lists) | `DateParser.IsoTSeparator`, which now routes a `T`-joined string to `ReadFormatLeniencies`, this engine's reading of the format's leniencies it keeps (P4, P6), and no longer to a list | the lists are absent; the separator test is the fallback's |
| The renderings' `DateString`, `ClockString`, `HumanYearString`, `ToIsoString` and `ToTimeZoneString`'s text | `toDateString`'s comment and its call to the library's `EcmaDateFormatter.DateString` | absent |

**The public dispositions** are recorded in [`docs/public-api.md`](../../public-api.md), "Public API changes,
2026-10-16": `JSDate(DateTimeOffset)`, `Value`, `DateTime`, `ConvertTo` and `ToDetailString` kept, as views of
the time value or with their bodies replaced, `DateParser` kept as its type (SJL-D-25); no public member is
removed.

**The duplicate search.** [`simplification/duplicate-search.log`](simplification/duplicate-search.log) searches
the engine's product sources (`Broiler.JS/`, test projects, benchmarks and samples excluded) for the constructs
that would mean a surviving copy of the area: the 400-year cycle's days, the civil epoch's shift, the day's
milliseconds and the time value's limit spelled as code, the civil-day helpers by name, the month and weekday
names a rendering spells, the platform's parse of a date, its conversions between local and universal time,
and its zones. **78 hits at `4d70821e`; none is a copy the area's deletion list or the correspondence names**:

| Hits | Where | What they are |
|---|---|---|
| 23 | `JSTemporalPlainDate` (7), `JSTemporalPlainDateTime` (5), `JSTemporalZonedDateTime` (5), `JSTemporalPlainYearMonth` (4), `JSTemporalPlainMonthDay` (2) | calls to the library's `IsoCalendarMath` and the types' own `daysInMonth` and `inLeapYear` getters, which call it |
| 24 | `TemporalArithmeticCalendar` (7), `TemporalNonIso` (7), `TemporalLunisolarCalendar` (5), `TemporalCalendarMath` (3), `TemporalSolarHijriCalendar` (2) | the non-ISO calendars' own month lengths and leap years, which stay with SJL-8 and SJL-10, as JSD-0072 says; the Indian calendar's leap year calls `IsoCalendarMath` |
| 8 | `HostLocalTimeProvider` (6), `JSDate` (2) | this engine's provider of the host's `TimeZoneInfo` (SJL-D-19) with its comments, whose 400-year cycle reads an instant past year 9999 in the zone's repeating rules and computes no calendar; `JSDate`'s `DateTimeOffset` view of the time value (`Value`) and its annotation |
| 6 | `JSIntlDateTimeFormatEngine` | a comment on the time value's range, its two clamps to that range, and the English month and weekday names of Intl's own rendering (SJL-9) |
| 1 | `JSIntl` | `formatToParts`'s case for a minute and second without an hour, which reads them in the host's local time through the platform; a finding for SJL-9, below |
| 4 | `JSTemporalInstant` (1), `JSTemporalZonedDateTime` (2), `Tz/IanaTimeZone` (1) | Temporal's own limit of 8.64e21 nanoseconds (SJL-10), and two comments on the zone data |
| 2 | `JSTemporalNow` | `Temporal.Now`'s identifier of the host's zone, which Temporal reads from its bundled tzdata (SJL-8) |
| 5 | `DateParser` | this engine's own fallback, which decision SJL-D-17 keeps in it: the slash-date heuristic's month length, and its culture parses through the platform |
| 4 | `JSValueToClrConverter` | the CLR binding's conversion of a script value to a `DateTime` or `DateTimeOffset` parameter, which parses a string by the platform for a host method and is not `Date.parse` |
| 1 | `Broiler.JS/CHANGELOG.md` | the corrections' text |

No clause of SJL-6 reads the duplicate search. **A finding for a later milestone.** The `JSIntl` hit is
`Intl.DateTimeFormat.prototype.formatToParts`'s special case for a format with a minute and a second and no
hour, which reads them through `DateTimeOffset.ToLocalTime()`, the host's zone, where every other path of the
formatter reads its fields in the format's zone through this engine's provider or the zone's data; where the
host's offset and the format's zone differ by part of an hour, the minute it writes is the host's. It is
Intl's code, outside this area, and this bundle names it for SJL-9, which owns it.

## 13. What this bundle does not claim

- **Nothing about Broiler.VM**, whose half is its own bundle, judged by its own rule.
- **A time-zone dataset.** Both parts ran on the runners' tzdata, 2026c; SJL-8 chooses the shared one, and until
  then this engine's `Date` reads the host's zone and its `Temporal` its bundled tzdata (JSD-0072).
- **A speed claim of any kind.** SJL-6 measures nothing.
- **Review**: the reviewer field says none and means it.

*(2026-10-16, at SJL-6.6: this bundle ended with a section "What this bundle does not hold yet", which listed
"**The after runs** of parts A and B, at the commit that lands SJL-6.5's cut-over, and their comparisons" and
"**The deletion list, Date's storage and the corrections list** (clauses 1, 2 and 5), at SJL-6.5 and SJL-6.6",
followed by the note that, until SJL-6.3, the list began with the reference-arm comparison. Sections 5 to 12 hold
them, and this section replaces it.)*
