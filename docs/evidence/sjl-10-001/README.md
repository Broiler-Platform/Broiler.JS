# Evidence bundle SJL-10-001 (Broiler.JS) - the Temporal core

**Milestone:** SJL-10 of the shared JavaScript libraries programme,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-10-temporal.md`: its home clauses and its Broiler.JS
clauses.

**Rule:** [`decision-rule.md`](decision-rule.md), committed at SJL-10.1 before any code of the area:
- part A is test262 per path in UTC, with negative-metadata tests;
- part B is the same in `America/New_York`;
- part C is Broiler.VM's reference datasets `temporal.js` and `temporal-calendars.js` run in this engine against
  Broiler.VM's answers. The first is in [`conformance/datasets/time/`](../../../conformance/datasets/time/README.md)
  since SJL-8.4; the second is copied there at SJL-10.2.

The home clauses are judged by their own mechanisms, the tests SJL-10.3 commits. The rule names them and does not
decide them.

**Area verdict:** [`SharedLibraries.md`](../../roadmap/SharedLibraries.md), "Area verdicts", with Broiler.VM's
JSD-0078 and its correspondence from code, retained in Broiler.VM's bundle of the same name.

**The pins.** This engine follows `tc39/proposal-temporal@e8cc03fc` from SJL-10.1 (JSD-0078's decision D1), and the
area's rules run test262 `ccaac100` (decision D2).

**Owner:** the Broiler.JS maintainer. One person holds every role (the programme's EX-30, recorded in Broiler.VM).
**Reviewer:** none, and nothing here has been read by anyone but its author.

**State, 2026-10-19, at SJL-10.6:**
- The bases of all three parts are taken at the rule's commit `1d27064d` (section 1).
- The library holds the area, and the library-level runner answers its vectors (section 3).
- This engine is cut over to the library, and the cut-over's own checks hold: the local Temporal selection of test262,
  both datasets, the correspondence's probes, and the vectors at corpus revisions 25 and 26, which retires the area's
  divergence fields (section 4).
- The after arm is collected at `00b65dd0`, the merge that lands the cut-over (sections 5 to 15). **The three home
  clauses and the four Broiler.JS clauses are met**, as section 6 reads them:
  - **parts A and B are met** under the rule as written: in UTC and in `America/New_York`, with negative-metadata
    tests, no row of the suite changed, so none regressed and class C1 lists no row (sections 7 and 8);
  - **part C is met** under the rule as written: every line of both datasets equals its expected answer (section 9);
  - the home clauses hold by the tests SJL-10.3 committed (section 10);
  - the deletion list is absent (section 12), and every correction is listed (section 13);
  - every vector of corpus revision 26 has the result its class expects, in both zones (section 14).
- With the shared library as nuget.org publishes it at `0.1.0-preview.6` in place of the after commit's, this engine
  answers decision D12's case with a duration (section 11). Decision D12 (`163d43aa`) is in `0.1.0-preview.7`, which
  `Publish shared libraries` published on 2026-10-19 (run 39312237448, from `5ddee404`, whose library code is the
  after commit's file for file). With that version restored from nuget.org, the pristine consumer's check of decision
  D12 holds, a `RangeError`, and so do the other 23 (section 11).

Nothing is accepted: review is deferred, and SJL-10 may not meet its gate before SJL-6 and SJL-8 meet theirs.

*(Revised 2026-10-19, when `0.1.0-preview.7` was published. The last item read "With the shared library as nuget.org
publishes it, `0.1.0-preview.6`, in place of the after commit's, this engine answers decision D12's case with a
duration (section 11): decision D12 (`163d43aa`) is under the shared changelog's Unreleased heading. The stream's next
version carries it, and publishing it is the owner's to approve.")*

*(Revised 2026-10-19, at SJL-10.6, when parts A and B were imported. The fourth item read "The after arm is collected
locally at `00b65dd0`, the merge that lands the cut-over (sections 5 to 15):", with no sub-item on parts A and B, and
the last item read "Parts A and B's after runs are taken by the workflow at the same commit, runs 39252310861 and
39252316070, and are judged when they are imported (sections 7 and 8). Until then, the clause "test262 Temporal per
path meets the area rule" is open.")*

*(Revised 2026-10-19, at SJL-10.6. The state read "at SJL-10.5", its last item "The after runs of the rule's three
parts follow, at the commit that lands the cut-over (SJL-10.6).", and the line after it "**Nothing here is a
verdict.**" Sections 5 to 15 hold the after arm.)*

*(Revised 2026-10-19, at SJL-10.5: the state read "at SJL-10.3", and its last item "The after runs follow SJL-10.5's
cut-over.")*

*(Revised 2026-10-18, at SJL-10.3: the state read "at SJL-10.2", and had no item on the library.)*

*(Revised 2026-10-18, at SJL-10.2, twice. The state read "at SJL-10.1: the rule only. Nothing here is a verdict,
and nothing has been taken.", and its first two items "The bases of parts A and B are taken at the rule's commit
by `test262 evidence`, with `include_negative` true." and "Part C's base follows SJL-10.2, which copies the
calendars dataset." When part C's base was taken it read "at SJL-10.2: the rule and part C's base", with the bases
of parts A and B "dispatched at the rule's commit by `test262 evidence`, with `include_negative` true, and ...
imported when they complete".)*

The owner's direction of 2026-10-18, "Please do all recommended decisions and skip waiting for review.", under which
JSD-0078 records its decisions, changes no rule: nothing is accepted without review, and SJL-10 may not meet its gate
before SJL-6 and SJL-8 meet theirs.

Broiler.VM judges its own half in its own bundle of the same name. This bundle reads none of its runs.

## 1. The bases (SJL-10.2)

**Parts A and B's bases** were dispatched by `test262 evidence` at the rule's commit `1d27064d`, named by all forty
digits, with negative-metadata tests:
- part A in the runner's zone, UTC: run 39123945598;
- part B in `America/New_York`: run 39123948554.

Both are imported unchanged, with the digest of each zip as downloaded, which is the digest GitHub records:
[`test262/base/`](test262/base/imported.txt) and [`test262/base-new-york/`](test262/base-new-york/imported.txt).
- **What ran.** Each run tested `1d27064d`, dispatched from `main` at `d3464413`, with suite `ccaac100`. The
  workflow and `scripts/compliance/` there are byte-identical to the rule commit's: `git diff --stat d3464413
  1d27064d -- .github scripts` prints nothing, and the two commits differ only in four documents.
- **Completeness.** Each run has:
  - all eight shards reported, with no configuration failure, no incomplete shard and no retried one;
  - one suite revision, `ccaac100`;
  - one shell, shown by one distinct list of binaries, with the re-run's shell the same. The list's digest,
    `faed2c5a…`, is the same in both runs;
  - `includeNegative` true in `run.txt`, the merged results and the re-run.
- **The zone.**
  - Part A's record reads TZ unset and UTC +0000 at both instants.
  - Part B's reads TZ `America/New_York`, EST -0500 at 2021-01-15T12:00:00Z and EDT -0400 at 2021-07-15T12:00:00Z.
  - Each run records one tzdata release, 2026c, and every shard is as expected.
- **Timeouts.** In each run one path timed out in both variants,
  `test/staging/sm/regress/regress-1507322-deep-weakmap.js`. The workflow ran it again alone, with one worker,
  and it timed out again; both the run and the re-run are retained, as the rule's timeouts clause asks.
- **Taken.** Part A ran from 11:37Z to 13:12Z on 2026-10-18, and part B from 11:37Z to 13:17Z, each on image
  `ubuntu24` 20261012.334.1.

| Run | Passed | Failed | Timed out | Not applicable | Of |
|---|---|---|---|---|---|
| Part A, UTC | 98,181 | 2,004 | 2 | 5,871 | 106,058 rows (53,029 paths, two variants) |
| Part B, `America/New_York` | 98,181 | 2,004 | 2 | 5,871 | 106,058 rows |

The totals are equal in both zones, and the parts compare per row, never with each other. Each part's base gives
every one of its 106,058 rows the verdict that bundle `sjl-8-001`'s after run in the same zone, at `79a0506c`, gave
it.

**Part C's base** ([`datasets/`](datasets/README.md)) was taken at the rule's commit.
- **The population.** SJL-8.4 copied `temporal.js`, with its answer and divergences, from Broiler.VM's `9508bd5`,
  whose three blobs are `14072b0`'s. SJL-10.2 copied `temporal-calendars.js`, with its polyfill answer and
  divergences, byte for byte from Broiler.VM's `14072b0`, into
  [`conformance/datasets/time/`](../../../conformance/datasets/time/README.md). `datasets.pin` gained the three
  files' lengths, SHA-256 and Git blob ids, in a block naming `14072b0`, and the harness refuses a copy whose digest
  differs.
- **The harness** is [`scripts/conformance/run_reference_datasets.py`](../../../scripts/conformance/run_reference_datasets.py),
  extended at SJL-10.2 to the second program.
  - `--bundle sjl-10-001` runs this rule's two programs and names this rule in the run's identity.
  - It builds the expected answer as Broiler.VM's check `temporal/t3/calendars-match-the-reference-polyfill` does,
    with the same `RetainedMatches` as the first program's check.
  - It assigns no class.

  Its expected answers equal Broiler.VM's own answers, from the correspondence's runs, on every line of both
  programs.
- **The base** used the shell built in Release from a clean detached checkout of `1d27064d`, with no `LANG` and
  `TZ=UTC`. A build of `d3464413`, whose product sources are the same, was at hand, but the rule takes "the shell
  built there".

  | Program | Lines | Equal to the expected answer | Differing |
  |---|---|---|---|
  | `temporal.js` | 1,629 | 1,622 | 7 |
  | `temporal-calendars.js` | 3,999 | 3,896 | 103 |

  Both programs completed with nothing on standard error, and each negative control showed its changed line as a
  difference. Both answers are byte-identical to the correspondence's runs of the programs in this engine.
- **Which lines differ, and under which rows.** This is an observation, not a classification; the rule admits no
  class, and reads the base without judging it. [`datasets/base/observations.txt`](datasets/base/observations.txt)
  has it, line by line.
  - **`temporal.js`:** the 7 lines are row D6: a `Duration.round` to months relative to `2020-01-31`, in 7 of 9
    rounding modes.
  - **`temporal-calendars.js`:** the 103 lines are C5 (5), C15 (15), C16 (4), C17 (2), F4 (55) and D11 (22). These
    are the rows the rule names, each with the count the correspondence's run found.
  - **No other line differs**, so nothing here is a finding against the rule's sentence that the correspondence
    classifies no other difference on these programs.

## 2. The vectors (SJL-10.2)

**Corpus revision 24** (`2890294f`) adds the Temporal area: 10,878 vectors in six files under
[`conformance/vectors/temporal/`](../../../conformance/vectors/temporal/), written by
[`scripts/conformance/generate_temporal_vectors.py`](../../../scripts/conformance/generate_temporal_vectors.py), all
`normative`.

| File | Vectors | What it pins |
|---|---|---|
| `grammar.vectors` | 1,904 | The ISO 8601 and RFC 9557 grammar through every type's `from()`; duration, calendar, time zone, offset and `relativeTo` strings; the area design's table and JSD-0078's G rows (G1 to G24) |
| `rounding.vectors` | 4,758 | `Duration.prototype.round` in all nine modes and every increment of every time unit; days of 24 hours; the calendar units with increments, relative to plain dates; nudging, bubbling and the shifted window; a zoned `relativeTo` across New York's 2026 transitions; totals; other calendars (D5 to D12) |
| `calendars.vectors` | 3,598 | Identifiers and aliases; month codes in every calendar under both overflows; eras; month-day reference dates with Table 6; the formula calendars at their edges; the table calendars at their tables' edges, where Table 1 publishes them; the calendar an object lends; fields past 32 bits; the conversions (C1 to C18) |
| `balancing.vectors` | 309 | The records' limits; integers beyond the engines' fields; narrowing in addition; balancing, regulation, addition and the rounding of points at the limits (R1 to R8, B1 to B9) |
| `differences.vectors` | 244 | `until` and `since` in every type: the settings, rounding in every calendar, New York's transitions and the limits (F1 to F8) |
| `durations.vectors` | 65 | Sign and shape, the largest unit, addition with calendar units, and `compare` (D1 to D4) |

- **The reference (decision D10).** Every expected value is the answer of the proposal's own polyfill at `e8cc03fc`
  (`ref:temporalref@1`).
  - The generator holds `polyfill/lib`'s 24 files to their git blob ids.
  - [`scripts/conformance/temporalref.mjs`](../../../scripts/conformance/temporalref.mjs) imports it under Node
    22.22.0, which carries ICU4C 77.1 and tzdb 2025b.
  - Its 92 npm packages are at the versions
    [`scripts/conformance/temporalref/package-lock.json`](../../../scripts/conformance/temporalref/package-lock.json)
    pins: big-integer 1.6.52, call-bind 1.0.9, es-abstract 1.24.2 and their own dependencies.

  Neither engine's code is consulted.
- **The inputs** are the correspondence's probes, with the edges of its rows:
  - every row with a behavioural difference (R6, R7, B5, D3, D4, D6 to D8, D11, F4, F7, G3, G4, G6, G8, G9, G11 to
    G13, G16 to G20, G22, C2, C5, C6, C9, C12, C13 and C15 to C17);
  - the rows of agreement beside them.

  Each vector's note names its rows.
- **Decision D7: where the pinned text and its polyfill disagree, the text rules.** 73 vectors take the text's answer
  and record the polyfill's as `overruled:temporalref@1=<outcome>`. The generator lists each one.

  | Where | Vectors | The text | The polyfill |
  |---|---|---|---|
  | Row D8: NudgeToCalendarUnit after a shifted window (seven modes; a plain and a zoned `relativeTo`, and `until`) | 28 | P11M | P1Y |
  | Two spaces as the date-time separator | 12 | `RangeError` | accepted |
  | An offset's decimal separator with no digit | 8 | `RangeError` | accepted |
  | The month-days `02-30` and `04-31` | 2 | `RangeError` (DateSpecMonthDay's early error) | accepted |
  | Calendar strings that are only a year-month or month-day with a calendar | 6 | `RangeError` | the calendar |
  | Time zone strings that are a time, year-month or month-day with an offset or annotation | 10 | the offset or zone | `RangeError` |
  | Era aliases the era proposal removed on 2025-11-21 (decision D4) | 6 | `RangeError` | accepted |
  | A regnal era read leniently (`reiwa` 1 on 2019-01-01, Heisei 31) | 1 | accepted | `RangeError` |

  How each was found:
  - **The first row**: every script is run again with the polyfill's statement replaced by the text's.
  - **The others**: by reading the texts, where the polyfill's own parser and era table answer otherwise. The
    correspondence did not run its grammar and calendar probes under the polyfill, so it could not see them.
- **Seven vectors keep the polyfill's answer** where ComputeNudgeWindow's text, read literally (its `r1 = 0` test),
  would not. Broiler.VM's JSD-0054 section 4 records that the text does not do what it means there. Both engines and
  the polyfill test the start duration instead. Each note says what the literal text answers.
- **Not vectored.** The polyfill reckons these through ICU4C or JavaScript's `Date`, so it is no source for them:
  - the table calendars' implementation-defined approximations outside Table 1's published years;
  - the Hebrew years before AM 1, and 5806 and 5807, where Calendrical Calculations' arithmetic and Broiler.VM agree
    against ICU4C;
  - a non-ISO calendar's fields at the ISO limits.
- **The schema** widens, dated in `SCHEMA.md` section 6:
  - `proposal:` cites a clause of a pinned proposal's text (`sources/temporal.pin`, `sources/intl-era-monthcode.pin`);
  - `overruled:` records an answer a vector does not take;
  - the validator refuses a vector with no source (`no-source`, with its negative control).
- **No library answers the area yet.** `Broiler.ECMAScript.Algorithms` holds no Temporal code until SJL-10.3, so
  there is no library-level runner for it: SJL-8.4's `942f8cd3` has no counterpart here yet. The Native AOT probe
  reports every Temporal vector as not implemented, as it should for an area the library does not hold. Its report
  equals the JIT's. *(SJL-10.3 adds the library and its runner: section 3.)*

**This engine's runs** of `Broiler.JS/Broiler.JavaScript.Vectors.Tests` are in [`engine/`](engine/). Each was taken
in Release, built once and run with `--no-build` from a clean tree, under `TZ=UTC` and under `TZ=America/New_York`,
with no `LANG`. The product code is `d3464413`'s. At each revision the two zones' reports are byte for byte the
same. Every value revision 25's runs render is the one revision 24's runs rendered.

| Revision | Vectors | Match | Classified divergences | Unclassified | Not implemented |
|---|---|---|---|---|---|
| 24 | 103,312 | 102,835 | 4 (the number area's, SJL-14's) | 473, all in the Temporal area | 0 |
| 25 | 103,312 | 102,835 | 477 (the area's 473 and the number area's 4) | 0 | 0 |

Each run's negative control changes one expected value of `numbers/to-fixed.vectors` in a copy of the corpus. It is
reported as the one unclassified difference, `numbers/to-fixed/0001`.

**One revision-24 attempt was discarded.** In the first attempt at revision 24, one vector of the text area in each
zone found no completion within the runner's 5 s, a different vector in each: `text/from-base64/4186` under UTC and
`3604` under New York. Each is a one-line `Uint8Array.fromBase64` that the other zone's run answered, on a machine
other work shared. The runs were taken again with nothing changed. The first attempt's logs are retained as
`discarded-run-revision-24-*.log`, and the lines its reports differ in as `discarded-run-revision-24.txt`.

**Revision 25** (`5af5d320`) classifies every difference under the correspondence's rows. The reasons are in
[`scripts/conformance/temporal-divergences.txt`](../../../scripts/conformance/temporal-divergences.txt). JSD-0078
lists every one as a correction. SJL-10.5, this engine's cut-over, answers this engine's, and SJL-10.4 Broiler.VM's.

| Engine | Sub-area | Rows (vectors) | Vectors |
|---|---|---|---|
| Broiler.JS | Records and limits, balancing | R7 (14), B5 (5) | 19 |
| | Durations and rounding | D3 (9), D4 (3), D6 (7), D7 (23), D8 (7), D11 (15) | 64 |
| | Differences | F4 (20), F7 (11) | 31 |
| | The grammar | G3 (7), G4 (6), G6 (2), G8 (12), G9 (45), G11 (13), G12 (48), G13 (15), G16 (4), G17 (32), G18 (16), G19 (4), G20 (6), G22 (3) | 213 |
| | Calendars | C2 (12), C5 (29), C6 (23), C9 (24), C12 (11), C13 (3), C15 (30), C16 (12), C17 (2) | 146 |
| | **All** | | **473** |
| Broiler.VM | Records and limits | R6 (6) | 6 |

- **G9 and C6.** In 41 of G9's vectors and all 23 of C6's, a .NET `FormatException` escapes the evaluation, rendered
  `abort:`.
- **D7 and D8.** Every one of their vectors expects the text's P11M. This engine answers P10M29D relative to a plain
  date (D7, 23) and P1Y relative to a zoned one (D8, 7). Its two `until`s answer P11M, as row D8 records.

**Broiler.VM** was run locally, from a scratch archive and pin of each revision, through its conformance root built
in Release at `14072b0` (`--vectors <archive> --expect <pin>`).
- At revision 24 it matches 103,306 of 103,312 vectors.
- The six others are row R6, its one correction, which SJL-10.4 answers. Revision 25 classifies them as
  `divergence.broiler-vm`.
- At revision 25 it matches every other vector and classifies those six.

The runs are retained in [`engine/`](engine/) (`broiler-vm-*`), with their scratch pins, as the basis of those
fields. They are not a reading of Broiler.VM's half: its own bundle judges that, and takes its runs when its pin
moves to a merged revision. Each negative control changes `temporal/grammar/0004` in memory, and is reported as one
more difference.

**What the vectors say of the area record.**
- **Every difference falls under a row the area verdict names.** Every vector of the rows it calls agreement matches
  in both engines. Every row it lists as a correction that changes an answer is confirmed by at least one vector. C10,
  which changes no answer, is not.
- **The record's "the one case found is row D8"** (decision D7) no longer holds: 45 vectors find the polyfill's own
  parser and era table at odds with the pinned texts. Broiler.VM answers as the texts on all 45, and this engine on
  35. Its other 10 answer as the polyfill does, and are corrections the verdict already lists: calendar strings (G17,
  6) and removed era aliases (C9, 4). So no class changes.
- **Decision D7's general wording reaches JSD-0054 section 4's two places.** There the pinned text does not do what
  it means, and Broiler.VM, by its record, follows the polyfill. The vectors read D7 as naming row D8, and keep the
  polyfill's answer at ComputeNudgeWindow (seven vectors). The owner's record should say which it means.
- **Row D11 is refined.** The row says a `relativeTo` in another calendar is refused with a plain `Error`. A Coptic
  `ZonedDateTime` `relativeTo` is not refused: it is rounded in ISO months, P1M14D where the answer is P1M15D. It
  stays a correction under the row.
- **Row R6 counts five faulting cases.** The vectors find a sixth of the same cause, `new PlainDateTime` with a year
  of 2^53 + 2.

## 3. The library (SJL-10.3)

`Broiler.ECMAScript.Algorithms` gains the Temporal core under `Broiler.ECMAScript.Temporal`, in three parts joined on
one branch. The adapter guide is
[`adapter-guide-temporal.md`](../../../Broiler.ECMAScript/Broiler.ECMAScript.Algorithms/docs/adapter-guide-temporal.md).
- **Part A: records, balancing, grammar and serialization.**
  - `TemporalLimits`, each limit a `TemporalStatus` and none throwing (decision D9);
  - `IsoArithmetic`: balancing, regulation under both overflows, ISO addition and the rounding of points;
  - `TemporalGrammar`, Broiler.VM's linear parser, with no regular expression, each refusal a `TemporalParseError`;
  - `TemporalFormat`, every string form; and `TemporalOptionRules`' rules of the `toString` options.
- **Part B: the calendars.** `CalendarId`, `MonthCode` and `CalendarFields`; `TemporalCalendars` over SJL-8's
  calendar years, with the formula calendars, the table calendars, Table 2's eras, Table 3's month codes and Table
  6's reference years; and `ICalendarArithmetic`, the calendar provider. The calendars' private limits were unified
  onto part A's when the parts were joined, with no answer changed.
- **Part C: durations, rounding and differences.**
  - `DurationMath`: sign, the default largest unit, the internal records, addition, `compare`, `round` and `total`
    with and without `relativeTo`, and `RoundRelativeDuration` and `TotalRelativeDuration` with the nudges and
    bubbling, over exact rationals;
  - `TemporalDifferences`: `until` and `since` of every type;
  - `ITimeZoneArithmetic`, the zone provider, one zone per instance, with `TemporalTimeZone` over SJL-8's
    `ZonedTime`; `RelativeTo`; and `TemporalOptionRules`' rounding and difference rules;
  - decision D7 at row D8: the pinned text's nudge after a shifted window. JSD-0054 section 4's named places stand.
- **Nothing in the area charges** (JSD-0078's X3), and the library embeds no table.

**The differential check against Broiler.VM's code.** A harness ran Broiler.VM's own code beside the library, over the
data home's tables.
- **What ran:** `JsTemporalCore`, `JsTemporalParser`, `JsTemporal.Conversions`, `JsTemporal.Durations`,
  `JsTemporal.Options`, `JsTemporal.Calendar`, `JsTemporal.CalendarNonIso`, `JsTemporal.Zones` and `JsCalendars`,
  each copied from Broiler.VM's `src/Broiler.VM.Profile.JavaScript/` at `14072b0` with its namespace changed, the
  members that read engine values removed and `DifferenceSettings`' reads made parameters; and the realm files'
  duration and difference methods after their reads, extracted.
- **The result:** **76,814,791 comparisons, 0 differences**, at seed 20261018 and scale 2. They cover:
  - the grammar over 2,616,164 generated strings, in every goal;
  - the records' limits, balancing, regulation, ISO addition, the rounding of points and every string form;
  - durations' sign, shape, internal records, addition and `compare`;
  - `round` and `total` with no `relativeTo`, relative to a plain date and relative to a zoned instant, in
    every calendar, and from a month's end, where the shifted window lives;
  - the option rules and `until` and `since` of every type.
- **Row D12, one case counted apart.** Pacific/Apia skipped 2011-12-30. A day nudge window back from 2011-12-31 ends
  on that day, which `compatible` moves onto the window's start, so the window has no length and cannot hold the
  destination, which the specification asserts:
  - Broiler.VM's code ends the invocation with a .NET `ArgumentOutOfRangeException`;
  - the reference polyfill fails its own assertion, with a plain `Error`;
  - this engine's shell answers `PT0S`, from its own day arithmetic;
  - the library answers `OutOfRange`, a `RangeError` in both engines.

  The correspondence read row D12 as code no input reaches, and its open point on the row let the library assert there
  or answer a status. JSD-0078 records the status as the owner's decision D11 of 2026-10-18. The input reaches it, so the library answers a status (`cc3e3076`). It is the one place the library does
  not answer as Broiler.VM's code does.
- **The negative control:** with the polyfill's nudge after a shifted window in place of decision D7's, the
  durations sections find 94 differences.

  The summary is [`algorithms/differential-summary.txt`](algorithms/differential-summary.txt). The harness and its
  extracted code are in [`algorithms/harness/`](algorithms/harness/), retained as text. The verbatim copies are
  named there by their git blobs at `14072b0`, with every edit
  ([`Vm-copies.txt`](algorithms/harness/Vm-copies.txt)).

**Broiler.VM's recorded answers, at library level.** The library's tests compute, through the runner's Temporal
adapter, the lines of Broiler.VM's answer files for the correspondence's probes, copied from Broiler.VM with their
digests into `Temporal/Correspondence/`:
- the calendar rows C1 to C18: 4,416 lines; the 21 lines of row C13 that lend an object's slot are the engine's;
- the duration rows D1 to D11 and difference rows F1 to F8, with R7 and B5: 366 lines; one, a function's `length`,
  is the engine's.

Every computed line answers as Broiler.VM does.

**The retained datasets, at library level.** `temporal.js` answers all 1,629 lines as its expected answer, the
polyfill's, from which Broiler.VM diverges on none. `temporal-calendars.js` answers all 3,999 lines as its expected
answer, the polyfill's with Broiler.VM's 70 divergences.

**The vectors, at library level.** `runners/AlgorithmsLibrary.Temporal.cs` answers the Temporal area of corpus
revision 25, as SJL-8.4's `942f8cd3` answered the time-zone area: each operation's inputs are read back into the
call, and the call goes through the adapter to the library. The calendar table is read from the data home and
held to its manifest.

| File | Vectors | Answered, all matching | Engine-level |
|---|---|---|---|
| `balancing.vectors` | 309 | 309 | 0 |
| `calendars.vectors` | 3,598 | 3,574 | 24 |
| `differences.vectors` | 244 | 244 | 0 |
| `durations.vectors` | 65 | 62 | 3 |
| `grammar.vectors` | 1,904 | 1,904 | 0 |
| `rounding.vectors` | 4,758 | 4,758 | 0 |
| **All** | **10,878** | **10,851** | **27** |

- **Engine-level** by operation, as the other areas name theirs:
  - `Function.prototype.length`, a built-in's own property (2);
  - the properties `Duration.prototype.add`'s second argument is read for, which only a script observes (1);
  - the calendar an object lends from its internal slot, the object given by a description (row C13, 24).
- **The negative control:** with the polyfill's nudge in place of decision D7's, 35 vectors differ.
- **The Native AOT probe** expects every answered Temporal vector to match. It publishes with no warning, and runs
  over the whole corpus with 0 failures: 65,328 of 103,312 vectors match and 37,984 are not implemented. Its report
  is byte for byte the JIT's (SHA-256 `372306119101e2a85372f50213cea18e4f82c263710af386c0ba4784fb016dda`).

**The tests.** Against the data home's tables, after a clean Release build with no warning:

| Project | Tests |
|---|---|
| `Broiler.ECMAScript.Algorithms.Tests` | 1,294 (747 of them the Temporal core's, 69 part C's) |
| `Broiler.ECMAScript.Architecture.Tests` (SL1 to SL10) | 32 |
| `Broiler.ECMAScript.Conformance.Tests` | 133 |
| `Broiler.ECMAScript.Generation.Tests` | 28 |

All pass. `assurance check` reports no violation.

**What the library's contract adds to Broiler.VM's.**
- Every failure is a status, and an option value off its rule is a `false`. Each engine raises its own error, at
  the specification's moment, so no caller observes the change.
- Row R6: a limit check answers `OutOfRange` for a year past 2^53, where Broiler.VM's constructors ended the
  invocation (decision D9). It is Broiler.VM's one correction, made at SJL-10.4.
- Row D12: a day the zone skips answers `OutOfRange`, where Broiler.VM's code ended the invocation with a .NET
  exception (above).

*(Note, 2026-10-19, at SJL-10.5. "It is the one place the library does not answer as Broiler.VM's code does", above,
held of the library as SJL-10.3 wrote it, and this list named every such place. Decision D12 makes a second, in
NudgeToZonedTime: where the day span's sign is not the duration's, as back from the same Pacific/Apia instant when a
time unit is rounded under a date largest unit, the library now answers `OutOfRange`, where Broiler.VM's code, and
the library at `0.1.0-preview.6`, answer durations from outside the text's domain. The differential check above
ran before the decision, so it found no difference there. Section 4 has the decision and its lines. The text above
is left as written.)*

**Nothing is cut over yet.** Broiler.VM's cut-over (SJL-10.4) follows on the shared stream's next version, which it
sets, as SJL-8.5's `2d37a38a` did, and this engine's (SJL-10.5) after it. *(SJL-10.5 cuts this engine over: section
4.)*

## 4. The cut-over (SJL-10.5)

This engine's Temporal core is the library's `Broiler.ECMAScript.Temporal` (section 3). `BuiltIns` takes it by
project reference: the shared stream's `0.1.0-preview.6`, published on 2026-10-18 after Broiler.VM's cut-over set
it, with decision D12 (below), which the library makes after that version.
- **The built-ins keep** their `JSTemporal*` objects, the reads of options and fields in specification order,
  coercion, `relativeTo` resolution, their errors and `Temporal.Now`'s clock and host zone.
- **The adapter** is `TemporalAdapter`, in `Broiler.JS/Broiler.JavaScript.BuiltIns/Temporal/`, one file by concern:
  records, grammar, serialization, calendars and durations. Beside it, `TemporalTimeZones.Arithmetic` is the
  library's zone provider over SJL-8's rules.
- **Every correction** is in [`Broiler.JS/CHANGELOG.md`](../../../Broiler.JS/CHANGELOG.md), 2026-10-19.

**The commits**, on branch `sjl-10-5-js` from `main` at `1c411b68`, each a part of the cut-over:

| Part | Commits | What it moved, or did |
|---|---|---|
| A: records, limits, balancing, the grammar and serialization | `f42eac6b`, `3503cab5`, `808d7838` | `TemporalLimits` and `IsoArithmetic` for every type's limits, regulation, addition and balancing; `TemporalGrammar` for every string; `TemporalFormat` and `TemporalOptionRules` for every string form |
| B: calendars | `9242c342`, `b34f8c30` | `TemporalCalendars` for calendar identifiers, the calendar a Temporal object lends, and every calendar operation |
| C: durations, rounding and differences | `8df9227e`, `b37c7465`, `2dfdf92d`, `dd3ade8c`, `41eaa90f` | `DurationMath` for `Duration`'s addition, `compare`, `round` and `total` with `relativeTo`; `TemporalDifferences` for every `until` and `since`; the rounding of points, a zone's day lengths and the zoned addition; the options' rules; the tests of the corrections |
| The review's fixes | `3dd4626e`, `fd6dab95`, `25a474ec`, `88bbe199`, `70770e35`, `4bdfb232`, `58549f77`, `b72aefdd` | two bridges with no caller deleted; one start-of-day adapter, one copy of InterpretISODateTimeOffset's matching and one `overflow` read; stale comments; the defects code-1 and code-2; their tests |
| Decision D12 | `163d43aa`, `6dbff805` | the library's NudgeToZonedTime answers `OutOfRange` where the day span's sign is not the duration's; its tests, the shared changelog and the adapter guide |
| `main` | `e2b2a785` (merge) | `0.1.0-preview.6` published: `Broiler.ECMAScript/CHANGELOG.md` and the version suffix in `Broiler.ECMAScript/Directory.Build.props`, `preview.5` to `preview.6`, only. No code |
| Corpus revision 26 | `feee5b7a`, `c97a9996` | retires the area's 479 `divergence` fields; this engine's runs on revision 25 after the cut-over and on revision 26 |
| The records | the commits that add this section | the changelog, the public API, the notices, the plan pair and this section; the wording of revision 26's row in `REVISIONS.md` and of its retired fields' header, no vector |

**What was deleted.** Every member the deletion list names (JSD-0078's X6, the correspondence's section 11), the
formula and table calendar copies among them (decision D3). The commits name each.
- **Nine files, whole:** `TemporalIsoString.cs`, `TemporalNonIso.cs`, `TemporalArithmeticCalendar.cs`,
  `TemporalTableCalendar.cs`, `TemporalCalendarMath.cs`, `JSTemporalCalendar.cs`, `JSTemporalDuration.Arithmetic.cs`,
  `JSTemporalZonedDateTime.Rounding.cs` and `JSTemporalStubs.cs`, which held only comments.
- **In each type:** the limit, regulation, addition, balancing, difference, parsing and formatting copies, the
  month-code copies, and the copies of the `overflow` and `disambiguation` reads.
- **In `JSTemporalRoundingOptions`:** the unit order, the maximum increments and their validation.

`BuiltIns/Temporal/` held 21 files of 11,521 lines at the base, and holds 17 of 4,761. None of them uses a regular
expression, where ten did. The public API listings of `Broiler.JavaScript.BuiltIns` and `Broiler.JavaScript.Runtime`
are byte for byte the base's ([`cutover/public-api-check.txt`](cutover/public-api-check.txt)), so the public
`JSTemporal*` types stay as shells (SJL-D-25).

**The name search** ([`cutover/deletion-search.txt`](cutover/deletion-search.txt)) runs over every tracked `.cs` file
of `Broiler.JS/` at `c97a9996`, whose code is `6dbff805`'s, comments included. It looks for 156 names, every type
and member the commits name as deleted. 127 have no hit. The other 29 are found only as:
- thin adapters named after the specification's operations, each a call into the library (`RegulateDateTime`,
  `LocalAt`, `LocalNanoseconds`, the six `Parse…String` members, `TryParseOffsetString`, `CanonicalizeCalendar`);
- comments naming those operations;
- tests' comments on the engine before the cut-over;
- a library record's member (`TimeNanoseconds`), and Intl's own `ResolveCalendar`.

**The checks at the cut-over's final commit, `6dbff805`**, taken on 2026-10-19 from 06:12Z to 09:04Z from a clean
tree, on a machine nothing else was using. The branch's later commits change no code (the table above).

| Check | Result | Retained |
|---|---|---|
| Build | `Broiler.JS.slnx` in Release, every project compiled: 0 errors, 76 warnings, the 58 distinct warnings of parts B's and C's builds, none new | [`cutover/build-and-tests.txt`](cutover/build-and-tests.txt) |
| Unit tests, Release, `--no-build`, `TZ=UTC` | `Broiler.JavaScript.BuiltIns.Tests` 3,524, `Broiler.JavaScript.Integration.Tests` 5,339 (20 of them `SharedTemporalAdapterTests`' and 23 `SharedTemporalDurationsTests`') and `Broiler.ECMAScript.Algorithms.Tests` 1,296, every one passing, none skipped | the same |
| test262, the local Temporal selection, UTC and `America/New_York` | Each path's verdict is the base's, in each zone: no path changed, and none timed out | [`cutover/test262-compare-utc.txt`](cutover/test262-compare-utc.txt), [`-new-york.txt`](cutover/test262-compare-new-york.txt), [`cutover/test262-run-utc.log`](cutover/test262-run-utc.log), [`-new-york.log`](cutover/test262-run-new-york.log) |
| The reference datasets, against part C's base | Every line of `temporal.js` and of `temporal-calendars.js` equals its expected answer; 7 and 103 moved there since the base, and none the other way | [`cutover/datasets-report.txt`](cutover/datasets-report.txt), [`cutover/datasets-compare.txt`](cutover/datasets-compare.txt) |
| The correspondence's probes, against Broiler.VM's recorded answers | Every line is Broiler.VM's answer, by error type, but the five of row R6, whose recorded answers are Broiler.VM's from before its cut-over corrected the row | [`cutover/probes-check.txt`](cutover/probes-check.txt), [`cutover/probes-vs-vm.txt`](cutover/probes-vs-vm.txt) |
| The engine-level vectors, revision 25 | In each zone, the 473 classified divergences no longer hold and everything else is as at the base | [`engine/cut-over-revision-25-*`](engine/) |

- **test262.** The selection is `built-ins/Temporal`, `intl402/Temporal`, `staging/Temporal`,
  `staging/Intl402/Temporal`, `intl402/DateTimeFormat`, `intl402/DurationFormat` and `built-ins/Date`. It was run at
  suite `ccaac100` by `scripts/compliance/run_test262.py` with `--all-script-host-verifiable --include-negative`,
  three workers and a 60 s timeout. Each zone's run is compared, path by path, with the one local base, taken the
  same way at `1c411b68`. The paths that fail at the base, Date's three `proto-from-ctor-realm` tests and
  `DateTimeFormat`'s, fail after. These are not the rule's runs: parts A and B judge hosted runs, at the commit that
  lands the cut-over (SJL-10.6).
- **The datasets** were run by the rule's harness, `--bundle sjl-10-001`, with the shell of `6dbff805`, and compared
  with [`datasets/base/`](datasets/base/). The Broiler.VM copies are the same SHA-256 in both runs.
- **The probes.** All 73 of the correspondence's probes were run in `TZ=UTC`. 70 have a Broiler.VM answer and 65
  equal it on every line. The other five differ by one line each: R6's `new PlainDate(1e20, 1, 1)` and its kin, where
  Broiler.VM's recorded answer ended the invocation and this engine answers a `RangeError`. In exact text, 264
  `RangeError` and `TypeError` lines carry another message than Broiler.VM's; each engine keeps its own (JSD-0078).
- **The vectors.** Built once in Release at `6dbff805` from a clean tree and run in both zones together, with no
  `LANG`, from 08:02Z to 09:03Z. In each zone: 103,312 vectors, 103,308 matching, 4 classified divergences (the
  number area's) and, among the matches, 473 whose divergence no longer holds. Nothing is unclassified, and nothing
  is not implemented. The two reports are byte for byte the same. Against the base's runs (section 2) the vectors
  are the same, in the same order. Only those 473 changed status, and no other vector's rendered value moved. Each
  negative control reports `numbers/to-fixed/0001` as its one difference.
  - Logs and reports: [`engine/cut-over-revision-25-utc.log`](engine/cut-over-revision-25-utc.log) and `.tsv.gz`,
    [`engine/cut-over-revision-25-new-york.log`](engine/cut-over-revision-25-new-york.log) and `.tsv.gz`.
  - Negative controls: `negative-control-cut-over-revision-25-utc.tsv` and `-new-york.tsv`.

**Corpus revision 26** (`feee5b7a`) retires the area's 479 `divergence` fields: this engine's 473, and Broiler.VM's
6 (row R6), which its cut-over corrects and its bundle's section 4 records
([`engine/retired-divergences-revision-26.tsv`](engine/retired-divergences-revision-26.tsv)). The six files under
`conformance/vectors/temporal/` are revision 24's byte for byte.

This engine's runs on revision 26 were built once in Release at `feee5b7a`, from a clean tree, and run in both zones
together from 09:37Z to 10:37Z, with no `LANG`. In each zone:
- 103,312 vectors: 103,308 match, and 4 are classified divergences, the number area's;
- no difference is unclassified, none is not implemented, and no divergence no longer holds;
- the two reports are byte for byte the same, and so are the negative controls, each of which reports
  `numbers/to-fixed/0001` as its one difference.

Against the runs on revision 25 after the cut-over, every expected and rendered value is the same, and only the
479 retired vectors changed class. The files are [`engine/run-revision-26-utc.log`](engine/run-revision-26-utc.log)
and `.tsv.gz`, [`engine/run-revision-26-new-york.log`](engine/run-revision-26-new-york.log) and `.tsv.gz`, and
`negative-control-revision-26-utc.tsv` and `-new-york.tsv`.

**Decision D11**, the owner's of 2026-10-18 (JSD-0078): where the pinned text asserts that a week or day nudge window
holds its destination and an input breaks it, the answer is a `RangeError`. Pacific/Apia skipped 2011-12-30. With `a`
2011-12-31T16:15+14:00[Pacific/Apia] and `b` 2011-12-31T00:00+14:00[Pacific/Apia], exactly these moved. Part C's
probe [`cutover/d12-ext.js`](cutover/d12-ext.js) runs `trunc`, `floor`, `ceil`, `expand` and `halfExpand`
([`.base.txt`](cutover/d12-ext.base.txt) at `1c411b68`, [`.after.txt`](cutover/d12-ext.after.txt) at `6dbff805`);
these records' [`cutover/d12-half-modes.js`](cutover/d12-half-modes.js) runs the other four, `halfCeil`,
`halfFloor`, `halfTrunc` and `halfEven`, in the same two shells ([`.base.txt`](cutover/d12-half-modes.base.txt),
[`.after.txt`](cutover/d12-half-modes.after.txt)), in the polyfill and in Broiler.VM on `0.1.0-preview.6`.

| Operation, in each of the nine rounding modes | Base | After |
|---|---|---|
| `a.since(b, { largestUnit: "day", smallestUnit: "day" })` | `PT0S` (`trunc`, `floor`), `P1D` (the other seven) | `RangeError` |
| `a.until(b, { largestUnit: "day", smallestUnit: "day" })` | `PT0S` (`trunc`, `ceil`), `-P1D` (the other seven) | `RangeError` |
| `a.since(b, { largestUnit: "month", smallestUnit: "day" })` | `PT0S` (`trunc`, `floor`), `P1D` (the other seven) | `RangeError` |
| `Temporal.Duration.from("-PT16H15M").round({ smallestUnit: "day", relativeTo: a })`, and `.total({ unit: "day", relativeTo: a })`, which takes no mode | a plain `Error` carrying a .NET `ArgumentOutOfRangeException` | `RangeError` |

The message is "result is outside the representable range". Nothing else in the probes moved but the lines of
decision D12 (below), 30 in `d12-ext.js` and 12 in `d12-half-modes.js`, whose `RangeError` changes its message
only:
- `b.until(a)` and `b.since(a)` in days;
- `a.since(b)` and `-PT16H15M` relative to `a` rounded to weeks;
- `a.since(b)` in hours;
- anything relative to `b`, and `a.since(c)` from 2011-12-29;
- `compare`, `a.round("day")`, `hoursInDay` and `a.subtract("P1D")`.

So section 3's "this engine's shell answers `PT0S`" holds of `trunc`, the default, and of some other modes: the
others answered a day, and `round` and `total` a plain `Error`. The plan's SJL-10.3 record gains a dated note that
says so.

**Decision D12**, the owner's of 2026-10-19, under the same direction: decision D11's rule reaches
NudgeToZonedTime's assertion on the day span's sign (step 8), where an input breaks it. The same `a` and `b` break it
whenever a time unit is rounded with a date largest unit back from `a`. In 36 probe lines, the answers are:
- **the base:** a `RangeError`, "time zone returned inconsistent Instants", from this engine's own zoned code;
- **this change:** a `RangeError`, "result is outside the representable range";
- **the polyfill at `e8cc03fc`:** a `RangeError`;
- **Broiler.VM**, before its cut-over and on `0.1.0-preview.6`: durations from outside the text's domain, `P1DT16H`
  and the like;
- **part C's shell, at `41eaa90f`**, the cut-over before the decision: the same durations as Broiler.VM, line for
  line.

The lines are `a.since(b)` and `a.until(b)` with `{ largestUnit: "day", smallestUnit: "hour" }`, with a largest
month, to 30 minutes and to 2 nanoseconds, and `-PT16H15M` rounded the same way relative to `a`. The 9 other lines,
forward from `b`, unrounded, or in hours, are equal in all six. `d12-half-modes.js` finds the same in the four half
modes, for `a.since(b)` and `a.until(b)` to hours and `-PT16H15M` rounded to hours: 12 more lines, the base's and
the polyfill's a `RangeError`, this change's a `RangeError` with its own message, and Broiler.VM's durations.

[`cutover/d12-facts.md`](cutover/d12-facts.md) has every line in every engine but part C's shell, from the
decision's own check, with its probe [`cutover/d12z.js`](cutover/d12z.js) and the answers beside it:
`d12z.base.txt`, `d12z.after.txt`, `d12z.ref.txt`, `d12z.vm-now.txt`, and for Broiler.VM before its cut-over
`d12z.vm-before.txt` with its transcript `d12z.vm-before.txt.raw` (decision D11's line ends the invocation) and
`d12z-nod11.js` with `d12z-nod11.vm-before.txt`, the probe without that line. Its "this build" was `163d43aa`,
whose library code is `6dbff805`'s; at `6dbff805` the probe gives the same answers. Its file names are those of the
library step's scratch folder; its `probes/` files are these. These records ran `d12z.js` in part C's shell
([`cutover/d12z.part-c.txt`](cutover/d12z.part-c.txt)), and retain part C's own run of `d12-ext.js` there
([`cutover/d12-ext.part-c.txt`](cutover/d12-ext.part-c.txt)), which d12-facts.md cites: its 30 lines of decision
D12 are durations.

**The changes no row lists**, kept under the owner's direction of 2026-10-18:
- part A's two, part B's seven and five of part C's six (its fifth, that `add` and `subtract` read no second
  argument, is row D3's);
- the review's U1 to U8;
- the two defects at the base that the review fixed, code-1 and code-2;
- one these records found: the rounding of points, `01:10` to 20 minutes under `halfEven` being `01:00` where it was
  `01:20`.

[`cutover/kept-changes.js`](cutover/kept-changes.js) probes each, in `TZ=UTC`, in this engine at `1c411b68`
([`.base.txt`](cutover/kept-changes.base.txt)) and at `6dbff805` ([`.after.txt`](cutover/kept-changes.after.txt)),
in Broiler.VM at `83fef1a` with `0.1.0-preview.6` ([`.vm.txt`](cutover/kept-changes.vm.txt)), and in the polyfill
at `e8cc03fc` ([`.ref.txt`](cutover/kept-changes.ref.txt)).
[`cutover/kept-changes.compare.tsv`](cutover/kept-changes.compare.tsv) sets them side by side:
- 64 of its 69 lines changed since the base, and on every one this engine now answers as Broiler.VM and the
  polyfill do;
- three are controls that did not change;
- two did not change and still differ from both. A string outside the limits is refused before the `overflow`
  option is read, a divergence from the text that this cut-over leaves as it was.

**What SJL-10.6 judges.** The rule's three parts at the commit that lands this change, as the rule asks:
- part A, the whole suite per path in UTC with negative-metadata tests, by `test262 evidence`, against section 1's
  base;
- part B, the same in `America/New_York`;
- part C, both datasets at that commit, against [`datasets/base/`](datasets/base/).

Nothing above is a verdict on them. The local selection, the datasets run and the probes are the cut-over's own
checks. The bundle has no manifest yet: its collector writes one when the after runs are imported, as bundle
`sjl-8-001`'s did at SJL-8.7.

*(Note, 2026-10-19, at SJL-10.6: the collector is committed with the after arm's local steps, and its manifest lists
what the bundle holds before parts A and B are imported; their import extends it (section 5). The sentence above is
left as written.)*

**The files in [`cutover/`](cutover/)**, each small and text:

| File | What it holds |
|---|---|
| `build-and-tests.txt` | The build's summary and its 58 distinct warnings; the three test projects' results |
| `test262-compare-utc.txt`, `test262-compare-new-york.txt` | The per-path comparison with the local base, in each zone |
| `test262-run-utc.log`, `test262-run-new-york.log` | The two runs' logs, with the shell's commit |
| `datasets-report.txt`, `datasets-compare.txt` | The datasets run's report and its comparison with part C's base |
| `probes-check.txt`, `probes-vs-vm.txt` | The correspondence's probes against Broiler.VM's recorded answers, by probe and by line |
| `d12-ext.js`, `.base.txt`, `.after.txt`, `.part-c.txt` | Part C's probe of row D12's input: decision D11's operations, and decision D12's, in five rounding modes; at the base, after, and in part C's shell |
| `d12-half-modes.js`, `.{base,after,ref,vm}.txt` | The same operations in the other four rounding modes, in four engines |
| `d12-facts.md`, `d12z.js`, `d12z.{base,after,ref,vm-now,vm-before,part-c}.txt`, `d12z.vm-before.txt.raw`, `d12z-nod11.js`, `d12z-nod11.vm-before.txt` | Decision D12's facts, its probe and the answers of each engine |
| `kept-changes.js`, `kept-changes.{base,after,vm,ref}.txt`, `kept-changes.compare.tsv` | The changes no row lists, in four engines |
| `deletion-search.txt` | The name search for the deletion list |
| `public-api-check.txt` | The public API listings before and after |

Paths under this repository's checkout and the checks' scratch folder are shortened to `<tree>` and `<scratch>`.
Three kinds are left as written: the test262 suite root, `/home/user/tc39/test262`, in the two test262 logs; the
.NET runtime's folder in `datasets-report.txt`; and `d12-facts.md`'s own names, which name Broiler.VM's build before
its cut-over by its folder, `/home/user/vm-at-14072b0`, and the library step's scratch files by their names there.

## 5. The after arm (SJL-10.6)

| Field | Value |
|---|---|
| Evidence bundle | SJL-10-001 (Broiler.JS's) |
| Bases | Section 1: the workflow's runs at `1d27064d`, the commit that carries the rule, before any code of the area, part A's in UTC and part B's in `America/New_York`, each with its re-run of the rows that timed out; and part C's run at the same commit |
| After | `00b65dd0` (`00b65dd055a277859d5325ead6338a040bcf2f30`), the merge into the default branch (Broiler-Platform/Broiler.JS#1054) that lands SJL-10.5's cut-over (`f42eac6b` to `6dbff805`) and corpus revision 26 (`feee5b7a`). **Its product sources are `6dbff805`'s**, the cut-over's last commit, but for three files that are not code: the two changelogs and `Broiler.ECMAScript/Directory.Build.props`, whose version suffix moved to `preview.6` when `0.1.0-preview.6` was published (`bf81d1b7`, merged in `e2b2a785`). [`identity.txt`](identity.txt) lists every commit and every file. Every local run of this bundle is taken from a clean checkout of it |
| Rule commit | `1d27064d`, an ancestor of the after commit. The rule, the comparison script, the workflow and the two scripts it runs and merges with are unchanged between the two. Part C's harness, its driver and the copies are unchanged since SJL-10.2 committed them (`2b5b6daf`), and the harness's SHA-256 is the base run's ([`identity.txt`](identity.txt)). The base run names `80dff79f` ([`datasets/base/identity.txt`](datasets/base/identity.txt)): the same commit before its branch was rebased, which no branch holds, with the same tree as `2b5b6daf` *(corrected 2026-10-19, at SJL-10.6, after review; read "unchanged since SJL-10.2 committed them (`80dff79f`)", the id the collector and `identity.txt` used too)*. No file under `Broiler.JS/` differs between the rule commit and `1c411b68`, the cut-over's branch point |
| Shared library | `Broiler.ECMAScript.Algorithms` at the after commit, by project reference. **Its code is not byte for byte the published version's**: nuget.org serves `0.1.0-preview.6`, published from `fc829f8a` with `bf81d1b7`'s library code, and decision D12 (`163d43aa`) changes two of its files after that, `DurationMath.Relative.cs` and `DurationMath.cs`, in the shared changelog's `0.1.0-preview.7` entry (section 10). That version, published on 2026-10-19 from `5ddee404`, holds the after commit's library code file for file (section 11) *(revised 2026-10-19, when `0.1.0-preview.7` was published; read "under the shared changelog's Unreleased heading (section 10).")*. Bundle `sjl-8-001`'s after commit held its published version's code exactly |
| Shared vectors | `conformance/vectors` at corpus revision 26, which retires the area's 479 divergence fields, this engine's 473 and Broiler.VM's 6 ([`engine/retired-divergences-revision-26.tsv`](engine/retired-divergences-revision-26.tsv)) |
| Collector | [`collect.py`](collect.py) as this change commits it, which reuses bundle `sjl-8-001`'s collector, and through it `sjl-7-001`'s, `sjl-6-001`'s, `sjl-5-001`'s, `sjl-4-001`'s, `sjl-3-001`'s and `sjl-0-001`'s, run against clean checkouts of the after commit and, for the consumer's control, of the rule's commit. Part C was taken by its own harness, by hand, as its base was and as bundle `sjl-8-001`'s after run was ([`datasets/README.md`](datasets/README.md)); the collector has no step for it, as `sjl-8-001`'s had none. Its `test262` step imported parts A's and B's after runs, and its `compare` step compared each with its base (sections 7 and 8). Its `manifest` step wrote [`manifest.json`](manifest.json), the SHA-256 of every other file the bundle retains, this README's included *(revised 2026-10-19, at SJL-10.6, when parts A and B were imported; the last two sentences read "Its `test262` and `compare` steps are written for parts A and B and not yet run. Its `manifest` step wrote [`manifest.json`](manifest.json), the SHA-256 of every other file the bundle retains, this README's included, as it stands before parts A and B are imported")* |
| Taken | 2026-10-19. Parts A's and B's after runs by the workflow, dispatched together at 12:28Z and completed at 13:54Z and 14:00Z: run 39252310861 (part A) and run 39252316070 (part B). Locally, part C from 13:45Z, `simplification` at 13:53Z, `vectors` from 13:53Z, `home` from 15:02Z and again at 15:07Z (section 10), `area` from 15:04Z, `probe` from 15:05Z, `consumer` from 15:05Z and `identity` at 15:09Z; `test262` and `compare` at 15:18Z, once for each part, and `manifest` after them; after review, from a second clean checkout of the after commit, `identity` and `simplification` again at 17:24Z with the collector corrected (the Rule commit row and section 12), decision D12's answer with the published library by hand at 17:28Z (section 11), and `manifest` again last *(corrected 2026-10-19, at SJL-10.6, after review; the local steps ended "`test262` and `compare` at 15:18Z, once for each part, and `manifest` last")*. After `0.1.0-preview.7` was published, decision D12's check of the pristine consumer with that version restored from nuget.org, by hand from a clean clone at `5ddee404`, from 20:00Z (section 11), and `manifest` again last *(added 2026-10-19, when `0.1.0-preview.7` was published)*. The machine is section 1's; other work shared it, and nothing local here is timed *(revised 2026-10-19, at SJL-10.6, when parts A and B were imported; read "Parts A's and B's after runs by the workflow, dispatched at 12:28Z: run 39252310861 (part A) and run 39252316070 (part B), not imported here.", and the local steps ended "`identity` at 15:09Z and `manifest` last")* |
| RID | `linux-x64`: the workflow's runners for test262; locally, one machine with four logical processors |

**Each project was built once.** The after checkout was created clean (`git status --porcelain --untracked-files=all`
empty, and so at every step: build outputs are ignored). Part C's harness built the shell there in Release first. Each
later step built in Release only the projects it needs that were not yet built: the vectors' runner, the shared folder
and the area's test project, each build compiling nothing the shell's had. Every run is `--no-build`. The packaging
script's build of `Broiler.JavaScript.All` for the pristine consumer, and the home step's second build (section 10),
found every project up to date. The Native AOT probe was published for `linux-x64` once, as bundle `sjl-8-001`'s was,
and its JIT twin is the shared folder's build, with no second build (bundle `sjl-8-001`'s probe step built it again).
The rule's checkout for the consumer's control was built once, its shell only. These checkouts are git worktrees of
this repository in the scratch folder, as bundle `sjl-8-001`'s after tree was. After review, a second clean checkout of
the after commit, also a worktree there, ran the `identity` and `simplification` steps again, which build nothing, and
its shell was built in Release once for section 11's record of decision D12 *(added 2026-10-19, at SJL-10.6, after
review)*. After `0.1.0-preview.7` was published, a clean clone of this repository at `5ddee404`, in the scratch
folder, built `Broiler.JavaScript.All` in Release once, for the packaging script, for section 11's record of that
version, restoring from nuget.org into a package cache of its own *(added 2026-10-19, when `0.1.0-preview.7` was
published)*.

**Parts A and B** judge hosted runs, which the collector's `test262` and `compare` steps imported and compared, each
from the zip as downloaded (sections 7 and 8).

*(Revised 2026-10-19, at SJL-10.6, when parts A and B were imported. The paragraph read "**What is not here yet.**
Parts A and B judge hosted runs, which the collector's `test262` and `compare` steps import and compare. Sections 7
and 8 are written when they are imported, and clause "test262 Temporal per path meets the area rule" is not judged
before then.")*

## 6. The clauses, and what holds each

The milestone's exit gate names three home clauses and four Broiler.JS clauses. The rule decides two of the
Broiler.JS clauses, the test262 clause by parts A and B and the dataset clause by part C; the home clauses are held by
the tests SJL-10.3 committed, and the deletion list and the corrections are read from the tree and the changelog (the
rule's "What this file does not decide").

| Clause | What it asks | Where | State |
|---|---|---|---|
| Home: every sub-area not refused is implemented | Records and limits, balancing, durations and rounding, differences, the grammar, calendars and serialization, in the library | section 10, [`home/after/clauses.log`](home/after/clauses.log) | **Met** at `00b65dd0`: each sub-area's tests that SJL-10.3 committed pass, records and limits 52, balancing 41, durations and rounding with differences 70, the grammar 357, calendars 76 and serialization 151, in a run of the shared folder's 1,489 tests with none failed |
| Home: its vectors pass | The library-level runner answers the Temporal area | section 10 | **Met**: `LibraryRunTests`' Temporal test passes on corpus revision 26, every answered Temporal vector matching, and so does its control against the empty library; the Native AOT probe runs the corpus with no failure, its report byte for byte the JIT's |
| Home: the polyfill dataset passes at library level wherever it can run there | Both retained datasets, at library level | section 10 | **Met**: `TemporalDatasetTests` (all 1,629 lines of `temporal.js`) and `CalendarDatasetTests` (all 3,999 lines of `temporal-calendars.js`) pass |
| Broiler.JS: the deletion list is absent | The correspondence's section 11, JSD-0078's X6, with decision D3's calendar copies | section 12 | **Met** at `00b65dd0`: every entry is absent from the folder that held it, and nine files are gone whole. What remains is adapters that call the library, the coercing members the list makes adapter code, comments, the library's own types of two of the names, and the changelog |
| Broiler.JS: test262 Temporal per path meets the area rule | Parts A and B of the rule | sections 7 and 8, [`test262/`](test262/) | **Met** under parts A and B of the rule as written, at `00b65dd0`: in each zone both runs are complete and comparable, every shard in the zone, the host-zone records alike (tzdata 2026c in all four runs), and no row of the suite changed, so none regressed and class C1 lists no row *(revised 2026-10-19, at SJL-10.6, when parts A and B were imported; read "**Not judged yet.** The after runs, 39252310861 and 39252316070, are imported and compared with section 1's bases when they are complete", and the third column "sections 7 and 8")* |
| Broiler.JS: the polyfill dataset meets its rule | Part C of the rule | section 9, [`datasets/after/`](datasets/after/) | **Met** under part C of the rule as written, at `00b65dd0`: both programs complete with every line, and every line of both equals its expected answer |
| Broiler.JS: every correction is listed | The changelog against the correspondence's rows and the vectors | section 13, [`Broiler.JS/CHANGELOG.md`](../../../Broiler.JS/CHANGELOG.md) | **Met** for every correction the correspondence names for this engine, with the 473 vectors and the datasets' 110 lines by row; for the code no input reaches; for every policy change; and for the changes no row lists. Sections 7 and 8 have no row in class C1, so test262 adds none to list *(revised 2026-10-19, at SJL-10.6, when parts A and B were imported; the last sentence read "Whether test262 adds a row to list is read when parts A and B are imported")* |

The Broiler.VM clauses are judged in its bundle of the same name. None is accepted: review is deferred, and SJL-10
may not meet its gate before SJL-6 and SJL-8 meet theirs, and so not before SJL-3 meets SJL-3's.

## 7. Test262 per path, in UTC (rule part A)

**MET under part A of the rule as written, at the after commit `00b65dd0`: no row changed.**
[`test262/comparison-utc/comparison.json`](test262/comparison-utc/comparison.json) and
[`test262/comparison-utc/comparison.log`](test262/comparison-utc/comparison.log): the after run against part A's
base of section 1, through `scripts/compliance/compare_test262_paths.py` as SJL-0.9 committed it (`1410079e`, the
only commit that touches it; the after checkout it ran from holds the same blob), with each run's timed-out rows
replaced by their re-runs, and with the classification file the rule asks for beside the report,
[`test262/comparison-utc/classifications.json`](test262/comparison-utc/classifications.json). **Every row, each path
in both variants, gives its base's verdict: 0 changed, 0 regressions, 0 unclassified.** The classification file is
empty, `{}`, because no verdict changed, and the script reports no classification unused.

| Part A's condition | This run |
|---|---|
| Both runs complete: every expected shard reported | eight of eight expected shards reported in each run, none incomplete and none retried; every shard's status file reads phase `full`, its exit code 1 for failing tests, as at the base ([`test262/comparison-utc/completeness.log`](test262/comparison-utc/completeness.log), read from each run's own files; [`test262/after/merge.log`](test262/after/merge.log)) |
| No configuration failure | none in either run |
| One suite revision | `ccaac100` in each run, the revision the rule pins |
| One shell | in each run, the hashes of the shell's binaries are the same in every shard and in the re-run: one distinct list ([`test262/after/shell-hashes.log`](test262/after/shell-hashes.log)). The two runs' shells differ, as they must: the after run's is built from the cut-over ([`test262/after/shell-binaries.sha256`](test262/after/shell-binaries.sha256)) |
| Every shard in the zone | in each run, all eight shards in the runner's zone: `TZ` unset, UTC +0000 at 2021-01-15T12:00:00Z and at 2021-07-15T12:00:00Z ([`test262/after/host-zone.log`](test262/after/host-zone.log)) |
| `includeNegative` true in both | in `run.txt`, in the merged results and in the re-run, in each run |
| The two runs' host-zone records read alike, line for line | yes: every expected line is the same in both, `TZ` unset, UTC +0000 at both instants and one tzdata release, 2026c ([`test262/comparison-utc/zone-records.log`](test262/comparison-utc/zone-records.log)); the after run's `host-zone.log` is the base's byte for byte. The runners' tzdata did not move between the base and the after run, so the part is judged |
| The script finds them comparable | yes, exit 0. Its checks ([`test262/comparison-utc/compare-checks.log`](test262/comparison-utc/compare-checks.log)): the after run against itself changes nothing, and a copy with one passing row turned to failed is reported as one regression, exit 1. The fields it records and does not compare are the same in both: .NET SDK 10.0.402, four workers, eight shards, Linux, X64 |
| No row passing at the base fails, times out or is skipped after | none |
| Every changed verdict in C1, and listed | **met**: no verdict changed, so C1 lists no row (the table below) |

**The after run.** Taken by `.github/workflows/test262-evidence.yml` at `00b65dd0`, in the rule's configuration
([`test262/after/run.txt`](test262/after/run.txt)): run
[39252310861](https://github.com/Broiler-Platform/Broiler.JS/actions/runs/39252310861), attempt 1, dispatched
2026-10-19 12:28Z from `main` at `00b65dd0`, with the tested commit named by all forty digits, `time_zone` empty and
`include_negative` true, and completed 13:54Z with the conclusion success. It ran `--all-script-host-verifiable` over
eight shards at suite `ccaac100`, with terser 5.50.0, 30 seconds per test, no memory limit, four workers,
`rerun_timeouts` true and the .NET SDK 10.0.402, on runner image ubuntu24 20261012.334.1, the base's. The workflow, the
two scripts it runs and merges with, and the comparison script are the rule commit's: `git diff 1d27064d 00b65dd0`
over `.github` and `scripts/compliance` prints nothing.

It is imported into [`test262/after/`](test262/after/) unchanged, every file of the workflow's artifact
`test262-evidence` (id 12192515568), with [`imported.txt`](test262/after/imported.txt), which records the digest of
the zip as downloaded, `1fcc203a…`, the digest GitHub records for the artifact. The run uploaded 18 artifacts:
`test262-evidence`; `test262-rerun-shard-0`, the re-run's (id 12192029219); and for each of the eight shards
`test262-full-shard-N` and `test262-evidence-shell-shard-N`. The bundle keeps `test262-evidence` alone, as it did of
the bases. The re-run's artifact holds three files: its report and log, which are `rerun-timeouts.json` and
`rerun-timeouts.log` here byte for byte, and its status file, `test262-rerun-shard-0-status.json` (phase `rerun`,
exit code 1), which is not kept *(corrected 2026-10-19, at SJL-10.6, after review; read "The bundle keeps that
artifact alone, as the bases did: the run's second artifact, `test262-rerun-shard-0`, holds the re-run's report and
log, and they are `rerun-timeouts.json` and `rerun-timeouts.log` here byte for byte.")*.

**Timeouts and their re-runs.** As at the base, one path timed out in both variants,
`test/staging/sm/regress/regress-1507322-deep-weakmap.js` ([`test262/after/timed-out-paths.txt`](test262/after/timed-out-paths.txt),
the base's byte for byte). The workflow ran it again alone, with one worker, at the same commit and with the same
shell, and it timed out again in both variants
([`test262/after/rerun-timeouts.log`](test262/after/rerun-timeouts.log),
[`rerun-timeouts.json`](test262/after/rerun-timeouts.json)). The script took each run's re-run, with `--base-rerun`
and `--after-rerun`, and replaced the two rows on each side, so they compare timed out against timed out (the report's
`replacedByRerun`). Both the runs and the re-runs are retained.

**The C1 table.** Each row that changed under class C1, with its path, its variant and the row of the correspondence
that names the correction:

| Path | Variant | Base | After | Row of the correspondence |
|---|---|---|---|---|

It has no row: no verdict changed.

**What it says.** In UTC, SJL-10.5's cut-over moved no verdict of the suite.
- Every row whose path names `Temporal` passes in both runs, `built-ins/Temporal`'s and `intl402/Temporal`'s among
  them.
- `built-ins/Date`, `intl402/DateTimeFormat` and `intl402/DurationFormat` pass in both runs but for four
  `proto-from-ctor-realm` paths, `Date`'s three and `DateTimeFormat`'s one, which fail in both: they read a
  constructor's prototype from another realm, not a Temporal value.
- So no test262 case at this revision shows one of the area's corrections: each is a case the suite does not hold, or
  holds where the base already passed. The vectors (section 14), part C (section 9) and the area's tests (section 10)
  hold them. The cut-over's local selection found the same (section 4).

The retained logs and report name this repository's checkout, the after checkout and the step's scratch folder as
`<repo>`, `<tree>` and `<scratch>`.

*(Noted 2026-10-19, at SJL-10.6, after review: before part A's after run was imported, this section read "(To be
written when the hosted after runs are imported.)")*

## 8. Test262 per path, in `America/New_York` (rule part B)

**MET under part B of the rule as written, at the after commit `00b65dd0`: no row changed.**
[`test262/comparison-new-york/comparison.json`](test262/comparison-new-york/comparison.json) and
[`test262/comparison-new-york/comparison.log`](test262/comparison-new-york/comparison.log): the after run against
part B's base of section 1, through the same script, with each run's timed-out rows replaced by their re-runs and with
the classification file beside the report,
[`test262/comparison-new-york/classifications.json`](test262/comparison-new-york/classifications.json). **Every row
gives its base's verdict: 0 changed, 0 regressions, 0 unclassified.** The classification file is empty, `{}`, because
no verdict changed. This is the area's run under a host zone with daylight-saving time, the zone `Temporal.Now` reads,
which stays this engine's (decision SJL-D-19).

| Part B's condition | This run |
|---|---|
| Both runs complete: every expected shard reported | eight of eight expected shards reported in each run, none incomplete and none retried; every shard's status file reads phase `full`, its exit code 1 for failing tests, as at the base ([`test262/comparison-new-york/completeness.log`](test262/comparison-new-york/completeness.log); [`test262/after-new-york/merge.log`](test262/after-new-york/merge.log)) |
| No configuration failure | none in either run |
| One suite revision | `ccaac100` in each run |
| One shell | in each run, the hashes of the shell's binaries are the same in every shard and in the re-run ([`test262/after-new-york/shell-hashes.log`](test262/after-new-york/shell-hashes.log)); the after run's shell is part A's after run's, by its hashes |
| Every shard in the zone | in each run, all eight shards with `TZ` `America/New_York`: EST -0500 at 2021-01-15T12:00:00Z and EDT -0400 at 2021-07-15T12:00:00Z ([`test262/after-new-york/host-zone.log`](test262/after-new-york/host-zone.log)) |
| `includeNegative` true in both | in `run.txt`, in the merged results and in the re-run, in each run |
| The two runs' host-zone records read alike, line for line | yes: `TZ` `America/New_York`, EST -0500 and EDT -0400 at the two instants, and one tzdata release, 2026c, in both ([`test262/comparison-new-york/zone-records.log`](test262/comparison-new-york/zone-records.log)); the after run's `host-zone.log` is the base's byte for byte. The runners' tzdata did not move, so the part is judged |
| The script finds them comparable | yes, exit 0. Its checks ([`test262/comparison-new-york/compare-checks.log`](test262/comparison-new-york/compare-checks.log)): the after run against itself changes nothing, and a planted regression is reported, exit 1. The fields it records and does not compare are the same in both: .NET SDK 10.0.402, four workers, eight shards, Linux, X64 |
| No row passing at the base fails, times out or is skipped after | none |
| Every changed verdict in C1, and listed | **met**: no verdict changed, so C1 lists no row (the table below); the rule admits nothing here that part A does not |

**The after run.** Taken by `.github/workflows/test262-evidence.yml` at `00b65dd0`, in part B's configuration
([`test262/after-new-york/run.txt`](test262/after-new-york/run.txt)): run
[39252316070](https://github.com/Broiler-Platform/Broiler.JS/actions/runs/39252316070), attempt 1, dispatched
2026-10-19 12:28Z from `main` at `00b65dd0` with `time_zone` `America/New_York` and `include_negative` true, and
completed 14:00Z with the conclusion success, in the configuration of part A's run and on the same runner image. It is
imported into [`test262/after-new-york/`](test262/after-new-york/) unchanged, every file of the workflow's artifact
`test262-evidence` (id 12193110753), with [`imported.txt`](test262/after-new-york/imported.txt), which records the
digest of the zip as downloaded, `fccbee20…`, the digest GitHub records. As in part A, the run uploaded 18 artifacts
and the bundle keeps `test262-evidence` alone: the re-run's artifact, `test262-rerun-shard-0` (id 12193010971), holds
its report and log, which are `rerun-timeouts.json` and `rerun-timeouts.log` here byte for byte, and its status file
(phase `rerun`, exit code 1), which is not kept *(corrected 2026-10-19, at SJL-10.6, after review; read "As in part A,
the run's second artifact, `test262-rerun-shard-0`, is the re-run's report and log byte for byte, and is not kept
apart.")*.

**Timeouts and their re-runs.** As at part B's base, one path timed out in both variants,
`test/staging/sm/regress/regress-1507322-deep-weakmap.js`, and timed out again alone with one worker, under the same
zone, at the same commit ([`test262/after-new-york/rerun-timeouts.log`](test262/after-new-york/rerun-timeouts.log));
the script replaced the two rows on each side with their re-runs, so they compare timed out against timed out.

**The C1 table.**

| Path | Variant | Base | After | Row of the correspondence |
|---|---|---|---|---|

It has no row: no verdict changed.

**What it says.** Under `America/New_York`, the cut-over moved no verdict either. Every row whose path names
`Temporal` passes in both runs, and the rows of `built-ins/Date`, `intl402/DateTimeFormat` and
`intl402/DurationFormat` that do not pass are part A's four `proto-from-ctor-realm` paths, in both runs. The two parts'
runs are not compared with each other, as the rule says.

*(Noted 2026-10-19, at SJL-10.6, after review: before part B's after run was imported, this section read "(To be
written when the hosted after runs are imported.)")*

## 9. The reference datasets (rule part C)

**MET under part C of the rule as written, at the after commit `00b65dd0`.** Both programs complete with every line,
and every line of both equals its expected answer.

The after run ([`datasets/after/`](datasets/after/)) was taken with the shell built in Release from a clean checkout of
`00b65dd0`, by the harness as SJL-10.2 committed it, with `--bundle sjl-10-001`, `TZ=UTC` and no `LANG`, as the base
was. The harness's `compare` joined it with the base line by line
([`datasets/after/compare-with-base/`](datasets/after/compare-with-base/)).

| Program | Lines | Equal to the expected answer | Differing | Against the base |
|---|---|---|---|---|
| `temporal.js` | 1,629 | 1,629 | 0 | the 7 lines that differed at the base are equal; none that was equal differs |
| `temporal-calendars.js` | 3,999 | 3,999 | 0 | the 103 lines that differed at the base are equal; none that was equal differs |

- **The rule's conditions.** Both programs completed, exit 0, with nothing on standard error, and every line was
  answered and compared: 1,629 and 3,999. Every line equals its expected answer. No line still differs, so the rule's
  second comparison, of a differing line with the same line at the base, has no line to read; the join with the base
  is retained all the same.
- **The expected answers** are Broiler.VM's, built as at the base: the retained polyfill answer with Broiler.VM's
  divergences applied. `temporal.js`'s divergences file has no entry; all 70 of `temporal-calendars.js`'s are used.
  The copies are the base run's, the same SHA-256 in both runs, and `datasets.pin` holds them.
- **The negative controls.** Each program's control changed line 1 of the expected answer in memory, an equal line,
  and the comparison showed it, and no other line, as a difference.
- **What moved since the base** is exactly what the base's observations name
  ([`datasets/base/observations.txt`](datasets/base/observations.txt)): in `temporal.js` row D6's 7 lines, and in
  `temporal-calendars.js` the 103 lines of rows C5 (5), C15 (15), C16 (4), C17 (2), F4 (55) and D11 (22). No line moved
  the other way. The after run's `temporal.js` answer is the retained polyfill answer,
  `temporal.polyfill-e8cc03fc.txt`, byte for byte.
- **Against the cut-over's own check** (section 4, at `6dbff805`), the counts are the same; the after commit changes
  no code since.

The rule admits no class, and none is needed: no line differs.

## 10. The home at the after commit (the home clauses)

The home is this repository's: the library's Temporal core, `Broiler.ECMAScript.Temporal`
(`Broiler.ECMAScript/Broiler.ECMAScript.Algorithms/Temporal/`), with its tests, its runner and its probe, over the
data home's tables. From a clean checkout of `00b65dd0`, into [`home/after/`](home/after/):

| Check | Result | Where |
|---|---|---|
| The library's code | Since SJL-10.3's last commit (`e33b53e6`), where section 3 read it, only decision D12 (`163d43aa`) changed it: `DurationMath.Relative.cs` and `DurationMath.cs`, with the adapter guide. The same files are all that differ from `bf81d1b7`, the code nuget.org's `0.1.0-preview.6` holds; the public API baseline is unchanged | [`home/after/library.log`](home/after/library.log) |
| The shared folder, built in Release | 0 warnings, 0 errors | [`home/after/build.log`](home/after/build.log) |
| Its tests | **1,489 passed, none failed**: `Broiler.ECMAScript.Algorithms.Tests` 1,296, `Broiler.ECMAScript.Architecture.Tests` 32 (SL1 to SL10), `Broiler.ECMAScript.Conformance.Tests` 133 and `Broiler.ECMAScript.Generation.Tests` 28 | [`home/after/tests.log`](home/after/tests.log) |
| The home clauses' tests, from that run | **Every sub-area:** `TemporalLimitsTests` 52 (records and limits), `TemporalBalancingTests` 41 (balancing), `DurationApiTests` 50 and `DurationCorrespondenceTests` 20 (durations and rounding, and differences), `TemporalGrammarTests` 357 (the grammar), `CalendarApiTests` 65 and `CalendarCorrespondenceTests` 11 (calendars), `TemporalFormatTests` 151 (serialization). **The vectors:** `LibraryRunTests.Every_Temporal_vector_passes_against_the_algorithms_library`, with its control `Against_the_empty_library_every_vector_is_not_implemented`. **The datasets:** `TemporalDatasetTests` and `CalendarDatasetTests`. Every one passes | [`home/after/clauses.log`](home/after/clauses.log) |
| The assurance check | `Broiler.ECMAScript: no violations.`, by Broiler.Code's CLI at `afd4ffb2` | [`home/after/assurance.log`](home/after/assurance.log) |
| The packages as nuget.org serves them | `Broiler.ECMAScript.Algorithms` and `Broiler.ECMAScript.Generation` `0.1.0-preview.6` each carry nuget.org's repository signature, which `dotnet nuget verify` accepts, and name this repository at `fc829f8a`; neither declares a dependency. **No entry of either holds 32 bytes taken from the start, the middle or the end of any generated table or pinned archive** (decision SJL-D-6 (c)) | [`home/after/packages.log`](home/after/packages.log) |
| The Native AOT probe, `linux-x64` | published with no warning, and the publish directory holds no managed assembly; the image ran corpus revision 26, **103,312 vectors in 49 files, no failure**: 65,328 match, the Temporal area's 10,851 answered vectors among them, 0 differ, and 37,984 name operations the library-level runner does not implement, the Temporal area's 27 engine-level vectors among them. Its report is byte-identical to the same probe's under the JIT, and its counts are section 3's at revision 25 | [`home/after/probe-publish-linux-x64.log`](home/after/probe-publish-linux-x64.log), [`home/after/probe-run-linux-x64.log`](home/after/probe-run-linux-x64.log), [`home/after/probe-report-aot.tsv.gz`](home/after/probe-report-aot.tsv.gz) |
| The shared set's pristine consumer | packed at `0.1.0-preview.6` from the after commit, restored from a local feed alone into an isolated cache, and run | [`home/after/pristine-consumer-shared.log`](home/after/pristine-consumer-shared.log) |
| The area's tests in this engine | built in Release and run under `TZ=UTC`: **250 tests, none failed**, in `Broiler.JavaScript.Integration.Tests`, the classes the changelog's SJL-10.5 entry names: `SharedTemporalDurationsTests` 23 (D3, D4, D6, D7, D8, D11, F4, F7 and decision D11), `SharedTemporalAdapterTests` 20 (code-1, code-2 and the offset's range check), `Issue781Tests` 138 and `Issue777Tests` 69 (row C9's era tests among them). The build had no error; its 6 warnings are xUnit's analyzers on integration tests outside the area | [`home/after/engine-build.log`](home/after/engine-build.log), [`home/after/engine-tests.log`](home/after/engine-tests.log) |

**The library at the after commit is not the published version's code**, as section 5 says. The tests above are of
the after commit's library, with decision D12. The packages nuget.org serves at `0.1.0-preview.6` hold `bf81d1b7`'s
code, and section 11 shows the one answer of this engine's that this changes. Those it serves at `0.1.0-preview.7`,
published on 2026-10-19 from `5ddee404`, hold the after commit's library code file for file, and with them this engine
answers there as the after commit does (section 11). *(Revised 2026-10-19, when `0.1.0-preview.7` was published; the
third sentence read "The packages nuget.org serves hold `bf81d1b7`'s code, and section 11 shows the one answer of
this engine's that this changes.")*

**The home step was run twice.** Its first run, at 15:02Z, counted the clauses' tests from the console, whose lines the
four test assemblies interleave, and missed two of the 1,489 results, neither of them a clause's test. The collector was
changed to count from the TRX report each assembly writes, and the step was run again at 15:07Z; these files are the
second run's. Its build found every project up to date and rewrote no assembly, and every result is the first run's:
1,489 passed, none failed.

## 11. The pristine consumer

**The consumer runs, and so does the area through it.** [`engine/pristine-consumer.log`](engine/pristine-consumer.log):
the runtime graph of `Broiler.JavaScript.All` packed from `00b65dd0` into 18 local packages, restored into a clean
consumer from that feed and nuget.org into an isolated package cache, and run: it printed `42`.

[`engine/pristine-consumer-temporal.log`](engine/pristine-consumer-temporal.log): the same consumer, from the same
isolated cache and offline, under `TZ=America/New_York` and no `LANG`, then evaluated 24 expressions, each a correction or
a policy change of SJL-10.5 as the changelog writes it, and printed **`ok 24`**:
- records and limits: R7 and B5;
- durations and rounding: D3, D4, D6, D7, D8 and D11;
- differences: F4, and F7 on the day before New York's 2026 spring transition;
- the grammar: G9 and G22;
- calendars: C2, C5, C6, C9, C12, C13, C15, C16 and C17;
- the rounding of points, `01:10` to 20 minutes under `halfEven`, which no row lists;
- decisions D11 and D12, back from Pacific/Apia's skipped day: `since` to days, and `since` to hours with a largest unit
  of a day.

**Where the library came from.** The engine's packages depend on `Broiler.ECMAScript.Algorithms` by project reference,
so the engine set packs it too, at `0.1.0-preview.6`, and the consumer restored that package from the local feed. It
carries no signature, and its assembly is the after commit's build, with decision D12, not nuget.org's package of the
same version, whose assembly differs. `Broiler.ECMAScript.Generation` is not in the consumer's closure.

**With the shared stream as nuget.org publishes it.** The same consumer, its build outputs removed, was restored again
into a second isolated cache, with `Broiler.ECMAScript.*` mapped to nuget.org alone and the engine's 17 other packages to
the local feed alone. It restored nuget.org's `0.1.0-preview.6`, signed, its assembly nuget.org's byte for byte, and the
checks printed **`failed decision D12 (false)`**: the other 23 hold, and decision D12's check is false. The log records
no more than that: the check is false when `since` returns, and also when it throws an error that is not a
`RangeError`. **What `since` answers there** is recorded by hand,
[`engine/pristine-consumer-d12-published.txt`](engine/pristine-consumer-d12-published.txt): in the shell built in
Release from a clean checkout of the after commit, under `TZ=America/New_York`, the check is true and `since` throws a
`RangeError` with the after commit's library, and with nuget.org's assembly of `0.1.0-preview.6` in its place (the one
the consumer restored, by its SHA-256, `48488a70…`) and every other file the same, the check is false and `since`
answers the duration **`P1DT16H`**. Part C's shell at `41eaa90f`, whose library code is `bf81d1b7`'s, the code that version holds,
answers the same ([`cutover/d12z.part-c.txt`](cutover/d12z.part-c.txt), the line `a.since(b) day/hour trunc`, `trunc`
being the default), as Broiler.VM does on that version ([`cutover/d12z.vm-now.txt`](cutover/d12z.vm-now.txt)). So
decision D12's `since` answers a duration where this engine answers a `RangeError` *(corrected 2026-10-19, at
SJL-10.6, after review; read "the other 23 hold, and decision D12's `since` answers a duration where this engine
answers a `RangeError`.", which the log alone does not show)*. That is the shared changelog's `0.1.0-preview.7` entry:
with the published `0.1.0-preview.6`, this engine answers there as Broiler.VM does on that version. The consumer then
stops with the exception it is written to throw, and the log records it. *(Revised 2026-10-19, when
`0.1.0-preview.7` was published; the last two sentences read "That is the shared changelog's Unreleased entry: with
the published `0.1.0-preview.6`, this engine answers there as Broiler.VM does on that version, until the stream's next
version is published. The consumer then stops with the exception it is written to throw, and the log records it.")*

**With `0.1.0-preview.7` as nuget.org publishes it** *(added 2026-10-19, when that version was published)*.
`Publish shared libraries`, run 39312237448, published `Broiler.ECMAScript.Algorithms` and
`Broiler.ECMAScript.Generation` `0.1.0-preview.7` from `5ddee404`, and tagged that commit
`ecmascript-v0.1.0-preview.7`. Its library code is the after commit's file for file: between `00b65dd0` and
`5ddee404`, no file of either project changes; under `Broiler.ECMAScript/` only the changelog and the version suffix
change, and under `Broiler.JS/` only the vectors' test project, which the engine set does not pack. Decision D12's
check was taken again by hand, as the collector's consumer step takes the published run,
[`engine/pristine-consumer-d12-published-preview.7.txt`](engine/pristine-consumer-d12-published-preview.7.txt):
- the engine set packed from a clean clone at `5ddee404` by `scripts/packaging/test_pristine_consumer.py --set engine
  --keep`, the library at `0.1.0-preview.7` among its 18 packages, and `BuiltIns` and `Runtime` each depending on that
  version; the consumer printed `42`;
- the kept consumer, its `bin/` and `obj/` removed, restored into a second isolated cache with `Broiler.ECMAScript.*`
  mapped to nuget.org alone and the engine's 17 other packages to the local feed alone, and run under
  `TZ=America/New_York` and no `LANG`. Its program is the collector's, with one line added that prints decision D12's
  check and its expression's value before the 24 checks.

It restored nuget.org's `0.1.0-preview.7`, signed, which `dotnet nuget verify` accepts, the package and its assembly
nuget.org's byte for byte (SHA-256 `2e553d33…` for the assembly), and printed decision D12's check **true**, `since`
throwing **`RangeError: Temporal.ZonedDateTime: result is outside the representable range`**, and the area's checks
**`ok 24`**. With the shared library as nuget.org publishes it at `0.1.0-preview.7`, this engine's packages answer
decision D12's case as the after commit does.

**A control of the checks**, [`engine/pristine-consumer-checks-control.txt`](engine/pristine-consumer-checks-control.txt):
the same 24 expressions, as one program, in the shell built at the rule's commit `1d27064d` and at `00b65dd0`, under
`TZ=America/New_York`. At the base 23 fail. The one that holds is decision D12's: this engine's own zoned code answered a
`RangeError` there before the cut-over, with another message (section 4). After, `ok 24`. So the consumer's `ok 24` is
SJL-10.5's, not a property of any build; decision D12's check tells the after commit's library from the published one,
not this engine from its base.

## 12. The simplification check (the deletion list)

**The deletion check.** [`simplification/deletion-check.log`](simplification/deletion-check.log) searches each entry of
the area's Broiler.JS deletion list in the folder that held it, with a pattern naming the members the entry held at
the rule's commit. The list is the correspondence's section 11 (Broiler.VM's
`docs/evidence/sjl-10-001/correspondence.md`), which JSD-0078's X6 states, with decision D3's calendar copies, as the
milestone's SJL-10.5 list names them. The same search at the rule's commit is its control: each pattern finds what
its entry held there. **Every entry is absent.**
- **Nine files are gone from `BuiltIns/Temporal/`, whole:** `JSTemporalCalendar.cs`, `JSTemporalDuration.Arithmetic.cs`,
  `JSTemporalStubs.cs`, `JSTemporalZonedDateTime.Rounding.cs`, `TemporalArithmeticCalendar.cs`, `TemporalCalendarMath.cs`,
  `TemporalIsoString.cs`, `TemporalNonIso.cs` and `TemporalTableCalendar.cs`. Five are new, the adapter's
  `TemporalAdapter.Calendars.cs`, `.Durations.cs`, `.Grammar.cs`, `.Records.cs` and `.Serialization.cs`.

| Entry | Hits at the rule's commit | What remains at the after commit | Reading |
|---|---|---|---|
| Validity and limits: the per-type limit checks, `DurationSign`, `RejectDuration`, the default largest unit, `HasCalendarUnits`, the relative range checks | 49 | `JSTemporalDuration`'s private `DurationSign()`, one line calling the library's `DurationMath.Sign`, and its two callers, `sign` and `blank` | absent |
| Regulation: `RegulateISODate`, `RegulateDateTime`, `RegulateTime`, `RegulateYearMonth` | 22 | `TemporalAdapter.RegulateTime`, which calls the library's `IsoArithmetic.RegulateTime` and raises this engine's error; `JSTemporalPlainDateTime`'s and `JSTemporalPlainTime`'s private adapters of the specification's names, which call it and the library's `ISODateTimeWithinLimits`; and comments naming the operation | absent |
| Addition: `AddISODate` and its copies, `AddYearsMonths`, each `AddDuration`'s arithmetic, the time-duration nanoseconds, `AddSubtract` with its "not yet implemented" `Error` | 46 | nothing | absent |
| Balancing: `BalanceTimeOnly`, `BalanceFromNanoseconds`, `BuildDuration`, the per-type balancing | 12 | nothing | absent |
| The small copies: `CompareISODate`, `FloorDiv`, the unit-length tables | 77 | nothing | absent |
| The three relative-rounding implementations and `compare`'s arithmetic | 69 | nothing | absent |
| `JSTemporalRoundingOptions`' arithmetic and rules | 105 | nothing | absent |
| The differences and the settings' rules | 59 | nothing | absent |
| The dead code: `NotImplementedArithmetic`, `ReadLargestUnit` | 2 | nothing | absent |
| The eight regular-expression parsers with their patterns, and `CanonicalizeTimeZone`'s string fallback | 137 | no pattern: no file of `BuiltIns/Temporal/` names `Regex`, and `CanonicalizeTimeZone` is gone. The eight parsers' names remain, `ParseTemporalDateString`, `ParseTemporalDateTimeString`, `ParseTemporalTimeString`, `ParseTemporalYearMonthString`, `ParseTemporalMonthDayString`, `ParseZonedDateTimeString`, `ParseTemporalInstant` and `ParseTemporalDurationString`, each a private adapter that parses with the library's grammar, `TemporalGrammar`, through `TemporalAdapter`, then takes its type's own creation steps; and `TemporalAdapter.TryParseOffsetString`, one line calling the library's `TemporalGrammar.TryParseOffsetString`. The other lines are their callers and comments naming the proposal's operations *(corrected 2026-10-19, at SJL-10.6, after review, when the entry's pattern was given the parsers' names, `TryParseOffsetString`, the two ambiguity patterns, `Year`, `YearField` and `CanonicalizeTimeZone`; the row read "94" and "nothing: no file of `BuiltIns/Temporal/` names `Regex`")* | absent |
| The per-type formatters, the precision arithmetic and the five `ReadCalendarName` copies | 43 | nothing | absent |
| `TemporalIsoString`, whole | 75 | its coercing members, `RequireMonthCodeString`, `RequireOffsetString` and `RequireStringField`, now in `TemporalAdapter.Grammar.cs`, which the list makes adapter code; a test's comment on the engine before the cut-over; the changelog | absent |
| `TemporalNonIso`'s calendar parts | 171 | a comment naming the library's `ParseMonthCode`; tests' comments on the engine before the cut-over; the changelog | absent |
| Decision D3's calendar copies: `TemporalArithmeticCalendar`, `TemporalTableCalendar`, `TemporalCalendarMath`, the Gregorian family's identifiers, years and eras in `JSTemporalCalendar.cs` | 201 | tests' comments on the engine before the cut-over; the changelog | absent |
| Decision D3's five per-type month-code copies | 31 | nothing | absent |

**The name search** ([`simplification/name-search.log`](simplification/name-search.log)) looks for every deleted file
and type, word-bounded, in every tracked file of `Broiler.JS/` and `Broiler.ECMAScript/`, tests, project files and
changelogs included. The deleted types are the ten that `BuiltIns/Temporal/` declares at the rule's commit and does
not at the after commit: `InternalDuration`, `NudgeResult`, `NudgeWindow` and `Parsed`, which were nested, and
`TemporalArithmeticCalendar`, `TemporalCalendar`, `TemporalCalendarMath`, `TemporalIsoString`, `TemporalNonIso` and
`TemporalTableCalendar`.

| Name | Hits in `Broiler.JS/` | Hits in `Broiler.ECMAScript/` | What they are |
|---|---|---|---|
| `NudgeResult`, `TemporalCalendar` | 0 | 0 | absent |
| `JSTemporalCalendar`, `JSTemporalStubs`, `TemporalTableCalendar` | 1 each | 0 | the changelog |
| `TemporalCalendarMath` | 3 | 0 | the changelog (twice) and a test's comment on the engine before the cut-over |
| `JSTemporalDuration.Arithmetic`, `JSTemporalZonedDateTime.Rounding` | 1 and 2 | 1 each | the changelog, a test's comment, and the provenance comment of SJL-4.3's reference arm J, which holds a copy of this engine's rounding at the commit it names, in the shared folder's tests only |
| `TemporalArithmeticCalendar`, `TemporalIsoString`, `TemporalNonIso` | 3, 1 and 5 | 6, 5 and 5 | the changelog, tests' comments, and SJL-6.3's reference arm J, which holds copies of this engine's civil-day helpers at the blobs it names, in the shared folder's tests only |
| `Parsed` | 1 | 0 | the word in a comment of `JSModuleContext.cs`, not the type |
| `InternalDuration`, `NudgeWindow` | 0 | 101 and 6 | the library's own types of the same names, the specification's Internal Duration Record and its nudge window, with its API baseline, assurance manifest, adapter guide, tests and changelog |

No hit is a copy of the area in this engine. The same file repeats the cut-over's own search of 156 names
([`cutover/deletion-search.txt`](cutover/deletion-search.txt), at `c97a9996`) at the after commit: every name has the
same number of hits as at the cut-over, and the same 29 names have any.

**The lines each C# file gained and lost** ([`simplification/line-counts.log`](simplification/line-counts.log)), between
the rule's commit and the after commit, under `Broiler.JS/`, by `git diff --numstat`:
- 31 files: 2,261 lines added and 8,812 removed;
- `BuiltIns/Temporal/` held 21 `.cs` files of 11,521 lines, ten of them naming `Regex`, and holds 17 of 4,761, none
  naming it;
- the nine deleted files lose 3,247 lines; the eight `JSTemporal*` type files keep their public classes as shells
  (decision SJL-D-25), gaining 922 lines and losing 5,403; `JSTemporalRoundingOptions.cs` gains 34 and loses 133; the
  adapter's five files are 1,059 lines;
- outside `Temporal/`: `JSIntlDateTimeFormatEngine.cs` (28 added, 16 removed), whose projection of a date into a
  calendar is the library's, and `BuiltInsAssemblyInitializer.cs` (3 removed); and four test files, two new.

The public API listings are the cut-over's check ([`cutover/public-api-check.txt`](cutover/public-api-check.txt)); the
after commit changes no code since `6dbff805`, so it stands.

## 13. Every correction listed

The correspondence's rows against [`Broiler.JS/CHANGELOG.md`](../../../Broiler.JS/CHANGELOG.md), "The Temporal core
moves to the shared library (2026-10-19, milestone SJL-10.5)":

| Rows | Class in the correspondence | In the changelog |
|---|---|---|
| R7, B5 | correction in Broiler.JS | "Corrections the corpus names", *Records and limits*: R7 14 vectors, B5 5 |
| D3, D4, D6, D7, D11 | correction in Broiler.JS | the same, *Durations and rounding*: D3 9, D4 3, D6 7, D7 23, D11 15 |
| D8 (with F5's case of it) | needs a decision; decision D7 makes it a correction | the same: D8 7; and "Policy, by decision": decision D7 |
| F4, F7 | correction in Broiler.JS | the same, *Differences*: F4 20, F7 11 |
| G3, G4, G6, G8, G9, G11, G12, G13, G16, G17, G18, G19, G20, G22 | correction of Broiler.JS | the same, *The grammar*: 7, 6, 2, 12, 45, 13, 48, 15, 4, 32, 16, 4, 6 and 3 |
| C2, C5, C6, C9, C12, C13, C15, C16, C17 | correction of Broiler.JS | the same, *Calendars*: 12, 29, 23, 24, 11, 3, 30, 12 and 2; C2 and C9 also under decisions D5 and D4 |
| D6; C5, C15, C16, C17, F4, D11 | the same rows | "Corrections the reference datasets name": `temporal.js`'s 7 lines and `temporal-calendars.js`'s 103, by row |
| G10, C3, C10, S8, D12 | code no input reaches (G10, C10 and S8 also "same") | "Code no input reached, deleted"; D12's "inconsistent Instants" branch, which is reached, under decision D12 |
| D12's day window and zoned branch | reached (decisions D11 and D12) | "Policy, by decision": decisions D11 and D12, with the probes that name every moved line |
| — | changes no row lists | "Corrections the correspondence did not find": parts A to C's, the review's U1 to U8, code-1, code-2 and the rounding of points, each kept under the owner's direction |
| R6 | correction in Broiler.VM | not this engine's: Broiler.VM's bundle |
| every other row of R, B, D, F, G, C and S | same | not corrections; nothing to list |

**The 473 vectors.** Corpus revision 26 retires 473 `divergence.broiler-js` fields
([`engine/retired-divergences-revision-26.tsv`](engine/retired-divergences-revision-26.tsv)), under 33 reasons, one per
row. Each reason's count equals the count the changelog gives its row, and the 33 rows are the correction rows above:
R7 14, B5 5, D3 9, D4 3, D6 7, D7 23, D8 7, D11 15, F4 20, F7 11, the fourteen G rows 213, and the nine C rows 146.
Every correction the correspondence names for this engine, and every policy change, is listed with its row.

Sections 7 and 8 have no row in class C1, so test262 adds nothing to list: no verdict of the suite changed in either
zone, as the cut-over's local selection found no path that changed (section 4).

*(Revised 2026-10-19, at SJL-10.6, when parts A and B were imported. The paragraph read "Sections 7 and 8 will say
whether test262 adds anything to list; the cut-over's local selection found no path that changed (section 4).")*

## 14. Engine-level vectors at the after commit (corpus revision 26)

**Every vector of corpus revision 26 has the result its class expects, in both zones, at the after commit.** The
engine-level runner, `Broiler.JS/Broiler.JavaScript.Vectors.Tests` in Release, built once from a clean checkout of
`00b65dd0` (its build compiled nothing the shell's build had: the runner and the projects only it needs,
`Broiler.JavaScript.All`, `Broiler.JavaScript.Debugger`, `Broiler.JavaScript.ModuleExtensions` and
`Broiler.ECMAScript.Conformance`, [`engine/after-build.log`](engine/after-build.log); *corrected 2026-10-19, at
SJL-10.6, after review; read "its build compiled the runner alone: the engine was the shell's build"*)
and run under `TZ=UTC` and `TZ=America/New_York` at once, with no `LANG`, from 13:53Z to 15:02Z on 2026-10-19. Other
work shared the machine; nothing in the run is timed.

| Run | Result | Files |
|---|---|---|
| Revision 26, UTC | 103,312 vectors: 103,308 match, 4 classified divergences, 0 unclassified differences, 0 not implemented, and no divergence that no longer holds; the test's four cases pass | [`engine/after-revision-26-utc.log`](engine/after-revision-26-utc.log), [`engine/after-revision-26-utc.tsv.gz`](engine/after-revision-26-utc.tsv.gz) |
| Revision 26, New York | the same | [`engine/after-revision-26-new-york.log`](engine/after-revision-26-new-york.log), [`engine/after-revision-26-new-york.tsv.gz`](engine/after-revision-26-new-york.tsv.gz) |
| Their negative controls | one vector's expected value changed in a copy of the corpus, `numbers/to-fixed/0001`, reported as the one unclassified difference | [`engine/negative-control-after-revision-26-utc.tsv`](engine/negative-control-after-revision-26-utc.tsv), [`engine/negative-control-after-revision-26-new-york.tsv`](engine/negative-control-after-revision-26-new-york.tsv) |

The 4 are the number area's `numbers/literal/0082`, `0084`, `0088` and `0090`, which stay with SJL-14. **The two
reports are byte for byte the same, and each is byte for byte section 4's run of revision 26 in its zone**, taken at
`feee5b7a` (committed in `c97a9996`), whose code is `6dbff805`'s; so are the negative controls *(corrected 2026-10-19,
at SJL-10.6, after review; read "taken at `c97a9996`", the commit that only added those runs' files; the message of
`f65fa03b` repeats the slip)*. Every Temporal vector, the 473 whose
`divergence.broiler-js` field revision 26 retired among them, matches in both zones.

## 15. What this bundle does not claim

- **That test262 shows the area's corrections.** Parts A and B are met with no verdict changed: every row whose path
  names `Temporal` passed at the base already, so no case of the suite at `ccaac100` tells this engine before the
  cut-over from after it. The corrections are held by the vectors, part C and the area's tests (sections 9, 10 and 14).
  *(Revised 2026-10-19, at SJL-10.6, when parts A and B were imported. The item read "**A verdict on parts A and B.**
  Their after runs are taken by the workflow at the after commit and judged when they are imported (sections 7 and
  8). Until then, clause "test262 Temporal per path meets the area rule" is open.")*
- **Nothing about Broiler.VM.** Its clauses, its own test262, its run of the reference datasets and its records are
  judged in its bundle of the same name, by its own rule, and this bundle reads none of its runs.
- **A revised rule.** Part C is met under the rule `1d27064d` committed, and no reading of it is new.
- **That a consumer of nuget.org's `0.1.0-preview.6` answers as this engine does** where decision D12 applies: the
  published library answers durations there (section 11). This engine's packages need `0.1.0-preview.7`, published
  on 2026-10-19, with which the pristine consumer answers as this engine does (section 11). *(Revised 2026-10-19,
  when `0.1.0-preview.7` was published; the last sentence read "This engine's packages need the stream's next
  version.")*
- **Intl's formatting of Temporal objects**, which is SJL-9's, and `Temporal.Now`'s clock and host zone, which stay this
  engine's (SJL-D-19).
- **A speed claim of any kind.** SJL-10 measures nothing.
- **Review**: the reviewer field says none and means it.
