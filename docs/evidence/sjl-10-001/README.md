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

**State, 2026-10-18, at SJL-10.2:**
- The bases of all three parts are taken at the rule's commit `1d27064d` (section 1).
- The after runs follow SJL-10.5's cut-over.

**Nothing here is a verdict.**

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
  equals the JIT's.

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
