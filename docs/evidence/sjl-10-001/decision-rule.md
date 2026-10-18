# Bundle SJL-10-001's decision rule: the Temporal core's test262 parity per path, in UTC and in a zone with daylight-saving time, and its reference datasets

**Written:** 2026-10-18, at milestone SJL-10 (work package SJL-10.1) of the shared JavaScript libraries programme,
with the area verdict this repository's `docs/roadmap/SharedLibraries.md` records. It was written before any code of
the area exists: no shared Temporal member, no cut-over. **Milestone judged:** SJL-10,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-10-temporal.md`: its Broiler.JS clauses "test262 Temporal
per path meets the area rule" and "The polyfill dataset meets its rule". **Owner:** the Broiler.JS maintainer.
**Reviewer:** none. Every role this rule names is held by one person (the programme records this as Broiler.VM's
exception EX-30), and this rule does not claim that anyone independent has read it.

**What this file is.** Three predeclared decisions, one per part.
- **The templates.** Parts A and B are in template T2 of the programme's validation document,
  `Broiler.VM/docs/javascript-shared-libraries/validation.md` section 4. Part C is a vector agreement in T3's sense,
  over the reference datasets Broiler.VM retains.
- **Each part decides alone**, and none reads another's runs.
- **There is no measurement part.** SJL-10's exit gate has no measurement clause.
- **Other rules.** The home clauses (every sub-area implemented, its vectors passing, and the polyfill dataset passing
  at library level wherever it can run there) are judged by their own mechanisms, the tests SJL-10.3 commits, not by a
  comparison this file states. This engine's other clauses, the deletion list absent and every correction listed, are
  read from the tree and the changelog. Broiler.VM commits its own rule for its own engine. None is read here.
- **It holds no figure.**

**The correspondence this rule classifies by** is Broiler.VM's `docs/evidence/sjl-10-001/correspondence.md`. It was
read from code in both engines at the revisions it names, and every behavioural cell its rows rest on was run in this
engine's shell. Its rows are named here by their letters: R for records and limits, B for balancing, D for durations,
F for differences, G for the grammar, C for the calendars and S for serialization. The decisions are Broiler.VM's
JSD-0078's D1 to D9: a decision is "decision Dn", and a row "row Dn".

**The pins.** The proposal revision this engine follows is `tc39/proposal-temporal@e8cc03fc` (decision D1). The suite
is test262 `ccaac100` (decision D2). Where the pinned text and its polyfill disagree, the text rules (decision D7).

**Why two zones.** This engine's `Temporal.Now` reads the host's zone, which stays its own (decision SJL-D-19, and the
milestone's "does not do"). The cut-over moves the arithmetic under every zoned value, those `Temporal.Now` makes in
the host's zone among them.
- **Part A** is the canonical configuration, in the runner's own zone, UTC.
- **Part B** is the same with the host's zone set to `America/New_York`, whose offset is never zero and has
  daylight-saving time, as bundles `sjl-6-001`'s and `sjl-8-001`'s part B were.

The clause "test262 Temporal per path meets the area rule" is read from part A over the whole suite:
`built-ins/Temporal` and `intl402/Temporal` are in it, and a regression on any other path fails part A too.

**Why negative tests are included.** Bundles `sjl-7-001` and `sjl-8-001` took their runs with them, so the base here
is taken with them too, and every run of this bundle is comparable with those bundles' after runs.

**The base is taken afresh, at the commit that carries this rule**, in both configurations, by the workflow. A run
names its tested commit in its own `run.txt`, so a base is the base whenever it is dispatched. A base taken at any
other commit is void.

## Part A - test262 parity per path, in UTC, with negative tests (template T2)

```rule
evidence class: conformance parity (not a measurement)
population: the whole pinned suite - test262 at revision ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e, the revision .github/workflows/test262.yml defaults to, Broiler.VM's src/tests/conformance/pins/test262.pin pins, and JSD-0078's decision D2 keeps for the area - every path the runner's --all-script-host-verifiable selection admits with --include-negative, in both variants the canonical configuration runs, original and terser
configuration: scripts/compliance/run_test262.py through the shell BroilerJS built in Release, by .github/workflows/test262-evidence.yml as this file's commit carries it, dispatched with the tested commit's forty-digit identifier, suite_ref at the revision above, max_workers 4, test_timeout_seconds 30, rerun_timeouts true, time_zone empty and include_negative true: eight shards merged by scripts/compliance/merge_test262_shards.py, --minifier terser at the version its lock file pins, no memory limit, negative-metadata tests included and their expected errors verified in every shard and in the re-run, a --self-check before each shard, every shard running the same shell, shown by the hashes of its binaries, and every shard in the runner's own zone, UTC, shown by its host-zone record; both runs' host-zone records read alike, line for line: TZ unset, UTC +0000 at both instants, and one tzdata release; the base run is taken at the commit that carries this file, and the after run at the commit that lands SJL-10.5's cut-over, named in bundle sjl-10-001, on the same RID; each run's artifact test262-evidence is imported unchanged, with the digest of the zip as downloaded
comparison: per path and variant, joined on both, the verdict at the base against the verdict after, through scripts/compliance/compare_test262_paths.py as committed at SJL-0.9, with the classification file retained beside its report; the script refuses two runs whose includeNegative differs
timeouts: a row that timed out in either run is the one the workflow ran again alone, with one worker, at that run's commit; the script takes the re-run through --base-rerun or --after-rerun, it replaces the row, and both the run and the re-run are retained
admitted difference classes:
  C1 correction - a row failing at the base passes after, because the shared Temporal core follows the pinned specification text where this engine did not: the correspondence rows R7 (narrowing in arithmetic), B5 (a year-month at the lower limit), D3 (Duration.prototype.add), D4 (compare's range check), D6 and D7 (rounding to months through a clamped difference), D8 with F5's case of it (the nudge after a shifted window, where decision D7 has the text rule), D11 (a relativeTo in another calendar), F4 (rounding options in non-ISO differences), F7 (day rounding on 24-hour days in until and since), G3, G4, G6, G8, G9, G11, G12, G13, G16, G17, G18, G19, G20 and G22 (the grammar's lenient acceptances), C2 (the aliases gregorian and minguo, decision D5), C5 (invalid month codes constrained), C6 (month codes with digits other than ASCII), C9 (era names of earlier drafts, decision D4), C12 (an ISO month-day's year past 2^31), C13 (the calendar a PlainMonthDay lends), and C15, C16 and C17 (the three conversions); each one listed with its path, its variant and the row of the correspondence that names the correction, and listed in the changelog (decision SJL-D-16)
no C3 and no C5: SJL-10 moves no data pin or data policy, and adopting the proposal revision (decision D1) changes no answer that rows C9 and D3 do not already name; a verdict that moves for any other reason of revision, implementation choice or data is a regression
every other difference is a regression, including a row that passes at the base and fails, times out or is skipped after, which no class admits, and a row whose verdict moves between failed, timed out and skipped without a class
MET if and only if both runs are complete (every expected shard reported, no configuration failure, one suite revision, one shell, every shard in the zone, includeNegative true in both), the two runs' host-zone records read alike, the script finds them comparable, no row passing at the base fails, times out or is skipped after, and every changed verdict is in C1 and is listed.
NOT MET otherwise; and if the two runs' host-zone records differ, because the runners' tzdata moved between them, the part is not judged until the base is taken again at the same commit in the after run's zone record.
```

## Part B - test262 parity per path, in a zone with daylight-saving time (template T2)

```rule
evidence class: conformance parity (not a measurement)
population: part A's
configuration: part A's, with time_zone America/New_York in both dispatches, so that the workflow sets TZ to it for every shard and for the re-run; both runs' host-zone records read alike, line for line: TZ America/New_York, EST -0500 at 2021-01-15T12:00:00Z, EDT -0400 at 2021-07-15T12:00:00Z, and one tzdata release; the base run is taken at the commit that carries this file, and the after run at the same commit as part A's
comparison: part A's
timeouts: part A's
admitted difference classes: part A's; no row of the correspondence depends on the host's zone, so part B admits nothing that part A does not
every other difference is a regression, as in part A
MET if and only if both runs are complete as part A says, every shard of each in the zone, the two runs' host-zone records read alike, the script finds them comparable, no row passing at the base fails, times out or is skipped after, and every changed verdict is in C1 and is listed.
NOT MET otherwise; and if the two runs' host-zone records differ, the part is not judged until the base is taken again at the same commit in the after run's zone record.
```

## Part C - the reference datasets (vector agreement, T3's sense)

```rule
evidence class: conformance parity (not a measurement)
population: Broiler.VM's src/tests/temporal/temporal.js and src/tests/temporal/calendars/temporal-calendars.js, byte for byte as Broiler.VM's commit 14072b0 holds them, each with Broiler.VM's retained polyfill answer and divergences file; the first, with its two files, is in conformance/datasets/time/ since SJL-8.4, copied from Broiler.VM's 9508bd5, whose three blobs are 14072b0's, and held by its datasets.pin; the second, with its temporal-calendars.polyfill-e8cc03fc.txt and its divergences.txt (named temporal-calendars.divergences.txt here, as SJL-8.4 named the first's), is copied there at SJL-10.2 with its digests, and datasets.pin gains its lines; a copy whose digest differs is not the population. Broiler.VM's three other programs under src/tests/temporal/ (temporal-intl.js, calendar-formats.js and zone-names.js) format Temporal objects through Intl, which the milestone leaves to SJL-9, and are not this part's
configuration: each program's text evaluated by indirect eval in the shell BroilerJS built in Release, through scripts/conformance/run_reference_datasets.py and its driver, which print the program's completion value, the harness extended at SJL-10.2 to the second program; no LANG and TZ=UTC; at the commit that carries this file, the shell built there running the copies (the base, retained and read), and at the commit that lands SJL-10.5's cut-over (the after run, judged); the expected answer of each line is Broiler.VM's: the retained answer with Broiler.VM's divergences applied, which Broiler.VM's slice checks temporal/t1/temporal-matches-the-reference-polyfill and temporal/t3/calendars-match-the-reference-polyfill hold its own answer equal to
comparison: per program and line, the line after against its expected answer; and per line that still differs, the line after against the same line at the base
admitted difference classes: none; every line this engine answered otherwise when the correspondence ran the two programs is a row SJL-10.5 corrects - in temporal.js row D6, and in temporal-calendars.js rows C5, C15, C16 and C17, F4 and D11 - and the correspondence classifies no other difference on them, so a line that differs after is a failure
MET if and only if both programs complete after with every line, and every line equals its expected answer.
NOT MET otherwise.
```

**How the parts are read.** As bundle `sjl-8-001`'s rule reads its parts A to C, once per part. The bundle retains:
- each run;
- each comparison's JSON report;
- the classification file it read;
- a table naming each C1 row with its row of the correspondence.

The script decides no class. A classification of a row that passed at the base is reported as refused. Totals do not
decide either test262 part, and **the two parts' runs are never compared with each other**. Part C's base run is
retained and read, but not judged: on the base, the programs differ wherever this engine's Temporal does, on the
lines the correspondence names.

## Revisions

A rule revised after any run it judges has been seen is a new dated file in this directory. It quotes this one whole
and says it was written afterwards, and its decision is then weaker evidence.

## What this file does not decide

- **The home clauses**: every sub-area implemented, its vectors passing, and the polyfill dataset at library level.
  Each is held by a test SJL-10.3 commits, with its witness.
- **This engine's deletion list and its changelog.** They are read from the tree and from `Broiler.JS/CHANGELOG.md`
  at SJL-10.6.
- **Broiler.VM's test262, reference datasets, closures and rules.** Broiler.VM's rule judges them.
- **The vectors.** SJL-10.2 adds them, and they may falsify the area record.
- **A speed claim of any kind.** SJL-10 measures nothing.
