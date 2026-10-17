# Bundle SJL-8-001's decision rule: the time-zone and calendar area's test262 parity per path, in UTC and in a zone with daylight-saving time, and its reference datasets

**Written:** 2026-10-17, at milestone SJL-8 (work package SJL-8.1) of the shared JavaScript libraries programme,
with the area verdict this repository's `docs/roadmap/SharedLibraries.md` records. It was written before any code
of the area exists: no generator in this repository, no shared member, no cut-over. **Milestone judged:** SJL-8,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-8-time-zones-and-calendars.md`: its Broiler.JS clauses
9 and 10. **Owner:** the Broiler.JS maintainer. **Reviewer:** none. Every role this rule names is held by one person
(the programme records this as Broiler.VM's exception EX-30), and this rule does not claim that anyone independent
has read it.

**What this file is.** Three predeclared decisions, one per part.
- **The templates.** Parts A and B are in template T2 of the programme's validation document,
  `Broiler.VM/docs/javascript-shared-libraries/validation.md` section 4. Part C is a vector agreement in T3's
  sense, over the reference datasets Broiler.VM retains.
- **Each part decides alone**, and none reads another's runs.
- **There is no measurement part.** SJL-8's exit gate has no measurement clause.
- **Other rules.** The data home's own clauses (the pinned archive and crates, byte-for-byte generation, the `zic`
  comparison, the public versions, the provider and the `SL` rules) are judged by their own mechanisms, the tests
  SJL-8.2 commits, not by a comparison this file states. Broiler.VM commits its own rule for its own engine.
  Neither is read here.
- **It holds no figure.**

**The correspondence this rule classifies by** is Broiler.VM's `docs/evidence/sjl-8-001/correspondence.md`. It was
read from code in both engines at the revisions it names, and every behavioural cell its rows rest on was run in
this engine's shell. Its rows are named here by their letters: Z for the tz database, A for the zone algorithms and
K for the calendar tables.

**Why two zones.** This engine's `Date`, its default formatter zone and `Temporal.Now` read the host's zone
(correspondence row A15). SJL-8.6 lets the local-time provider read the shared data (decision SJL-D-19), so the
area reaches `Date` through the host's zone.
- **Part A** is the canonical configuration, in the runner's own zone, UTC.
- **Part B** is the same with the host's zone set to `America/New_York`, whose offset is never zero and whose rules
  changed within the data's history, as bundle `sjl-6-001`'s part B was.

Clause 9, test262 for Temporal and Intl time zones, is read from part A over the whole suite: those paths are in
it, and a regression on any other path fails part A too.

**Why negative tests are included.** Bundle `sjl-7-001`'s rule took both its runs with them, so the base here is
taken with them too, and every run of this bundle is comparable with that bundle's after run.

**The base is taken afresh, at the commit that carries this rule**, in both configurations, by the workflow. A
run names its tested commit in its own `run.txt`, so a base is the base whenever it is dispatched. A base taken at
any other commit is void.

## Part A - test262 parity per path, in UTC, with negative tests (template T2)

```rule
evidence class: conformance parity (not a measurement)
population: the whole pinned suite - test262 at revision ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e, the revision .github/workflows/test262.yml defaults to and Broiler.VM's src/tests/conformance/pins/test262.pin pins - every path the runner's --all-script-host-verifiable selection admits with --include-negative, in both variants the canonical configuration runs, original and terser
configuration: scripts/compliance/run_test262.py through the shell BroilerJS built in Release, by .github/workflows/test262-evidence.yml as this file's commit carries it, dispatched with the tested commit's forty-digit identifier, suite_ref at the revision above, max_workers 4, test_timeout_seconds 30, rerun_timeouts true, time_zone empty and include_negative true: eight shards merged by scripts/compliance/merge_test262_shards.py, --minifier terser at the version its lock file pins, no memory limit, negative-metadata tests included and their expected errors verified in every shard and in the re-run, a --self-check before each shard, every shard running the same shell, shown by the hashes of its binaries, and every shard in the runner's own zone, UTC, shown by its host-zone record; both runs' host-zone records read alike, line for line: TZ unset, UTC +0000 at both instants, and one tzdata release; the base run is taken at the commit that carries this file, and the after run at the commit that lands SJL-8.6's cut-over, named in bundle sjl-8-001, on the same RID; each run's artifact test262-evidence is imported unchanged, with the digest of the zip as downloaded
comparison: per path and variant, joined on both, the verdict at the base against the verdict after, through scripts/compliance/compare_test262_paths.py as committed at SJL-0.9, with the classification file retained beside its report; the script refuses two runs whose includeNegative differs
timeouts: a row that timed out in either run is the one the workflow ran again alone, with one worker, at that run's commit; the script takes the re-run through --base-rerun or --after-rerun, it replaces the row, and both the run and the re-run are retained
admitted difference classes:
  C1 correction - a row failing at the base passes after, because the shared data or algorithms follow the specification where this engine did not: the correspondence rows Z10 (lookup by ASCII case only), Z11 (no ISO string as the formatter's zone), Z12 (the region lists), A13 (non-ASCII offset digits), A16 (an unvalidated host zone), K2 (chinese 2057), K7 (far lunisolar dates) and K12 (the Persian seams at .NET's bounds); each one listed with its path, its variant and the row of the correspondence or the specification clause that names the correction, and listed in the changelog (decision SJL-D-16)
  C5 data policy by decision - the verdict changes because the shared dataset replaces this engine's under decision SJL-D-20 or SJL-D-21, as Broiler.VM's JSD-0076 applies them: the release, tzdb 2026e for 2025b (rows Z1, Z2, Z15); backzone excluded (Z3); extrapolation by recurring rules after the last listed transition (Z4, A5, A7, A10); no Factory (Z5); CLDR's primary identifiers (Z7); and ICU4X's calendar sources for the years outside Table 1's published ranges and the runtime's classes (K4, K5, K6, K11, K14); each one listed with its row and the decision
no C3: no implementation-defined surface of the area moves at SJL-8; the formatter's resolved timeZone (Z8) and zone display names stay this engine's, and a verdict that moves on either is a regression
every other difference is a regression, including a row that passes at the base and fails, times out or is skipped after, which no class admits, and a row whose verdict moves between failed, timed out and skipped without a class
MET if and only if both runs are complete (every expected shard reported, no configuration failure, one suite revision, one shell, every shard in the zone, includeNegative true in both), the two runs' host-zone records read alike, the script finds them comparable, no row passing at the base fails, times out or is skipped after, and every changed verdict is in C1 or C5 and is listed.
NOT MET otherwise; and if the two runs' host-zone records differ, because the runners' tzdata moved between them, the part is not judged until the base is taken again at the same commit in the after run's zone record.
```

## Part B - test262 parity per path, in a zone with daylight-saving time (template T2)

```rule
evidence class: conformance parity (not a measurement)
population: part A's
configuration: part A's, with time_zone America/New_York in both dispatches, so that the workflow sets TZ to it for every shard and for the re-run; both runs' host-zone records read alike, line for line: TZ America/New_York, EST -0500 at 2021-01-15T12:00:00Z, EDT -0400 at 2021-07-15T12:00:00Z, and one tzdata release; the base run is taken at the commit that carries this file, and the after run at the same commit as part A's
comparison: part A's
timeouts: part A's
admitted difference classes: part A's, and in C5 also a row whose verdict moves because the local-time provider reads the host zone's offsets from the shared data rather than the host's tzdata (row A15, decision SJL-D-19 as SJL-8.6 applies it), each one listed with the instant and the two offsets
every other difference is a regression, as in part A
MET if and only if both runs are complete as part A says, every shard of each in the zone, the two runs' host-zone records read alike, the script finds them comparable, no row passing at the base fails, times out or is skipped after, and every changed verdict is in C1 or C5 and is listed.
NOT MET otherwise; and if the two runs' host-zone records differ, the part is not judged until the base is taken again at the same commit in the after run's zone record.
```

## Part C - the reference datasets (vector agreement, T3's sense)

```rule
evidence class: conformance parity (not a measurement)
population: Broiler.VM's src/tests/cldr/timezones/timezones.js and src/tests/temporal/temporal.js, byte for byte as Broiler.VM's commit 9508bd5 holds them, with Broiler.VM's retained answer and divergences file for each; SJL-8.4 copies them into this repository with their digests, and a copy whose digest differs is not the population
configuration: each program's text evaluated by indirect eval in the shell BroilerJS built in Release, through the harness SJL-8.4 commits beside the copies, which prints the program's completion value; no LANG and TZ=UTC; at the commit that carries this file (the base, retained and read) and at the commit that lands SJL-8.6's cut-over (the after run, judged); the expected answer of each line is Broiler.VM's: the retained answer with Broiler.VM's divergences applied, which Broiler.VM's slice checks hold its own answer equal to
comparison: per program and line, the line after against its expected answer; and per line that still differs, the line after against the same line at the base
admitted difference classes:
  D1 the formatter's resolved identifier - a line whose difference is only the timeZone that Intl.DateTimeFormat's resolvedOptions reports for a non-primary name (row Z8), which JSD-0076 leaves each engine's until the specification edition both pin settles it; each one listed
  D2 zone display - a line whose difference is only a zone's display text, such as the longOffset name's seconds (row Z16), which is SJL-9's; each one listed
  D3 outside the area - a line whose operation reads neither a zone nor a calendar table year (duration and calendar arithmetic, rounding: SJL-10's), whose answer after equals its answer at the base; each one listed with its operation
every other difference is a failure, including a line whose operation reads a zone or a calendar year and differs from the expected answer for any other reason, and a line of D3's kind whose answer moved between the base and the after run
MET if and only if both programs complete after with every line, and every line that differs from its expected answer is in D1, D2 or D3 and is listed.
NOT MET otherwise.
```

**How the parts are read.** As bundle `sjl-6-001`'s rule reads its parts A and B, once per part. The bundle retains:
- each run;
- each comparison's JSON report;
- the classification file it read;
- a table naming each C1 and C5 row with its reason.

The script decides no class. A classification of a row that passed at the base is reported as refused. Totals do
not decide either part, and **the two parts' runs are never compared with each other**. Part C's base run is
retained and read, but not judged: on the base, the datasets differ wherever this engine's data does. The
correspondence counts 51 of `timezones.js`'s 610 lines equal to Broiler.VM's.

## Revisions

A rule revised after any run it judges has been seen is a new dated file in this directory. It quotes this one
whole and says it was written afterwards, and its decision is then weaker evidence.

## What this file does not decide

- **The data home's clauses**: the pinned archive and crates, byte-for-byte generation, the `zic` comparison across
  the validated range, the public versions, the provider implementation and the `SL` rules. Each is held by a test
  SJL-8.2 commits, with its witness.
- **Broiler.VM's test262, reference datasets, closures and rules.** Broiler.VM's rule judges them.
- **The vectors.** SJL-8.4 adds them, and they may falsify the area record.
- **A speed claim of any kind.** SJL-8 measures nothing.
