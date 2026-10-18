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
