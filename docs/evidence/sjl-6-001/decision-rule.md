# Bundle SJL-6-001's decision rule: the Date area's test262 parity per path, in UTC and in a zone with daylight-saving time

**Written:** 2026-10-15, at milestone SJL-6 (work package SJL-6.1) of the shared JavaScript libraries
programme, with the area verdict this repository's `docs/roadmap/SharedLibraries.md` records, before any
shared member, adapter or cut-over of the Date area exists, and with the `time_zone` input of
`.github/workflows/test262-evidence.yml`, which part B dispatches with, committed in the same change.
**Milestone judged:** SJL-6, `Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-6-date.md`: its
Broiler.JS clauses 3 and 4. **Owner:** the Broiler.JS maintainer. **Reviewer:** none. Every role this rule
names is held by one person (the programme records this as Broiler.VM's exception EX-30), and this rule
does not claim that anyone independent has read it.

**What this file is.** Two predeclared decisions, one per part, each in template T2 of the programme's
validation document, `Broiler.VM/docs/javascript-shared-libraries/validation.md` section 4. **Each part
decides alone**, and neither reads the other's runs. **There is no measurement part**: SJL-6's exit gate
has no measurement clause. The library-level reference-arm rule is [`reference-arms.md`](reference-arms.md)
beside this file; Broiler.VM commits its own rule for its own engine. Neither is read here. **It holds no
figure.**

**Why two parts.** This engine's local time is the host's (decision SJL-D-19 keeps it), and its test suite
and every earlier bundle ran in UTC, where an offset of zero hides most of what local time does. The
milestone asks for the area's evidence to run under a zone with daylight-saving time as well (SJL-6.6),
and its clause 4 for that run to meet its rule. **Part A** is the canonical configuration, in the runner's
own zone, UTC; **part B** is the same population and configuration with the host's zone set to
`America/New_York`, whose offset is never zero, which keeps daylight-saving time, and whose rules changed
within the tzdata's history (2007), so that an offset taken from the wrong instant shows. Clause 3,
test262 for `Date` and its `Temporal` interop per path, is read from part A over the whole suite: those
paths are in it, and a regression on any other path fails part A too.

**The base is taken afresh, at the commit that carries this rule**, in both configurations, by the
workflow. `git diff --name-only 1ebde70` against this commit under `Broiler.JS/` and `Broiler.ECMAScript/`
names nothing, so bundle `sjl-5-001`'s after run could stand for part A's base; it is not used, so that
both parts' bases come from one commit and one version of the workflow. No run in a zone other than UTC
exists before this rule. A run names its tested commit in its own `run.txt`, so a base is the base
whenever it is dispatched; a base taken at any other commit is void.

## Part A - test262 parity per path, in UTC (template T2)

```rule
evidence class: conformance parity (not a measurement)
population: the whole pinned suite - test262 at revision ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e, the revision .github/workflows/test262.yml defaults to and Broiler.VM's src/tests/conformance/pins/test262.pin pins - every path the runner's --all-script-host-verifiable selection admits, in both variants the canonical configuration runs, original and terser
configuration: scripts/compliance/run_test262.py through the shell BroilerJS built in Release, by .github/workflows/test262-evidence.yml as this file's commit carries it, dispatched with the tested commit's forty-digit identifier, suite_ref at the revision above, max_workers 4, test_timeout_seconds 30, rerun_timeouts true and time_zone empty: eight shards merged by scripts/compliance/merge_test262_shards.py, --minifier terser at the version its lock file pins, no memory limit, negative-metadata tests not included, a --self-check before each shard, every shard running the same shell, shown by the hashes of its binaries, and every shard in the runner's own zone, shown by its host-zone record; both runs' host-zone records read alike, line for line: TZ unset, UTC +0000 at both instants, and one tzdata release, since the host's zone data can reach a test's answer whatever the host's own zone; the base run is taken at the commit that carries this file, and the after run at the commit that lands SJL-6.5's cut-over, named in bundle sjl-6-001, on the same RID; each run's artifact test262-evidence is imported unchanged, with the digest of the zip as downloaded
comparison: per path and variant, joined on both, the verdict at the base against the verdict after, through scripts/compliance/compare_test262_paths.py as committed at SJL-0.9, with the classification file retained beside its report
timeouts: a row that timed out in either run is the one the workflow ran again alone, with one worker, at that run's commit; the script takes the re-run through --base-rerun or --after-rerun, it replaces the row, and both the run and the re-run are retained
admitted difference classes:
  C1 correction - a row failing at the base passes after, because the shared implementation (correspondence rows T4, G2, P1, P9, R3), or this engine's own correction the area record names (the time value as Date's only storage, rows D1 to D5; the setters' argument order, T3; the provider's offsets, L2 to L4), follows the specification where this engine did not; each one listed with its path, its variant and the vector, the specification clause or the row of the correspondence (Broiler.VM's docs/evidence/sjl-6-001/correspondence.md) that names the correction, and listed in the changelog (decision SJL-D-16)
  C3 implementation-defined - the verdict changes on a surface the specification leaves to the implementation (a string outside the format, rows P4 to P7, including one this engine's own fallback keeps or drops at SJL-6.5; the time zone's name, R4), and the area's vectors classify it; each one listed with its vector
  C5 data policy by decision - the verdict changes because a dated data decision moved a pin or a policy; each one listed with the decision
every other difference is a regression, including a row that passes at the base and fails, times out or is skipped after, which no class admits, and a row whose verdict moves between failed, timed out and skipped without a class
MET if and only if both runs are complete (every expected shard reported, no configuration failure, one suite revision, one shell, every shard in the zone), the two runs' host-zone records read alike, the script finds them comparable, no row passing at the base fails, times out or is skipped after, and every changed verdict is in C1, C3 or C5 and is listed.
NOT MET otherwise; and if the two runs' host-zone records differ, because the runners' tzdata moved between them, the part is not judged until the base is taken again at the same commit in the after run's zone record.
```

## Part B - test262 parity per path, in a zone with daylight-saving time (template T2)

```rule
evidence class: conformance parity (not a measurement)
population: part A's
configuration: part A's, with time_zone America/New_York in both dispatches, so that the workflow sets TZ to it for every shard and for the re-run; both runs' host-zone records read alike, line for line: TZ America/New_York, EST -0500 at 2021-01-15T12:00:00Z, EDT -0400 at 2021-07-15T12:00:00Z, and one tzdata release; the base run is taken at the commit that carries this file, and the after run at the same commit as part A's
comparison: part A's
timeouts: part A's
admitted difference classes: part A's
every other difference is a regression, as in part A
MET if and only if both runs are complete as part A says, every shard of each in the zone, the two runs' host-zone records read alike, the script finds them comparable, no row passing at the base fails, times out or is skipped after, and every changed verdict is in C1, C3 or C5 and is listed.
NOT MET otherwise; and if the two runs' host-zone records differ, because the runners' tzdata moved between them, the part is not judged until the base is taken again at the same commit in the after run's zone record.
```

**How it is read.** As bundle `sjl-5-001`'s rule reads its part A, once per part: the bundle retains each
run, each comparison's JSON report, the classification file it read, and a table naming each C1, C3 and C5
row with its reason. The script decides no class; a classification of a row that passed at the base is
reported as refused. Totals do not decide either part, and **the two parts' runs are never compared with
each other**: a path that passes in UTC and fails in New York is a fact about this engine's local time at
the base, which part B carries into its comparison with the after run, not a difference either part
judges. A correction that part B shows and part A cannot, such as a date-only string read as local time
(correspondence row P1) or a time near a transition (L2, L3), is listed in part B's table and nowhere else.

**What SJL-5 taught this rule.** Bundle `sjl-5-001`'s first after run failed part A on a path that had
passed at the base only because of a defect the cut-over corrected elsewhere, and the owner fixed the
defect and took the run again rather than revise the rule. This rule names this engine's own corrections
in C1 before any run, and its part B exists so that a defect of local time is found by a run the rule
reads, not by a later one.

## Revisions

A rule revised after any run it judges has been seen is a new dated file in this directory, quoting
this one whole and saying it was written afterwards; its decision is then weaker evidence.

## What this file does not decide

- **The library-level comparison**, which [`reference-arms.md`](reference-arms.md) decides.
- **Broiler.VM's conformance and fuel accounting**, which Broiler.VM's own rule decides.
- **The deletion list, Date's storage and the corrections list** (clauses 1, 2 and 5), each judged by
  its own mechanism.
- **A time-zone dataset.** The host's tzdata is the data both parts run on; SJL-8 chooses the shared one.
- **A speed claim of any kind.** SJL-6 measures nothing.
