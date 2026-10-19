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

**State, 2026-10-19, at SJL-10.5:**
- The bases of all three parts are taken at the rule's commit `1d27064d` (section 1).
- The library holds the area, and the library-level runner answers its vectors (section 3).
- This engine is cut over to the library, and the cut-over's own checks hold: the local Temporal selection of test262,
  both datasets, the correspondence's probes, and the vectors at corpus revisions 25 and 26, which retires the area's
  divergence fields (section 4).
- The after runs of the rule's three parts follow, at the commit that lands the cut-over (SJL-10.6).

**Nothing here is a verdict.**

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
