# Bundle SJL-17-001's decision rule: collation, judged by test262 per path in UTC, by the orderings dataset, and under invariant globalization

**Written:** 2026-11-01, at milestone SJL-17 (work package SJL-17.1) of the shared JavaScript libraries programme,
with the area verdict this repository's `docs/roadmap/SharedLibraries.md` records ("Collation (SJL-17.1)") and
Broiler.VM's JSD-0084. It was written before any shared collation member or cut-over exists. **Milestone judged:**
SJL-17, `Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-17-collation.md`:
- its Broiler.JS clause 4 ("test262 `intl402/Collator` and `localeCompare` per path meet the area rule");
- clause 3's reading under invariant globalization ("host collation remains only behind the optional provider");
- SJL-17.6's "the `orderings` reference dataset in both engines" for this engine.

**Owner:** the Broiler.JS maintainer. **Reviewer:** none (EX-30); this rule does not claim that anyone independent has
read it.

**What this file is.** Three predeclared decisions, one per part.
- **Part A** is in template T2. **Part B** is a vector agreement in T3's sense. **Part C** is a robustness check of the
  providers.
- **There is no part in a zone with daylight-saving time.** Collation reads no zone (JSD-0084 decision E10).
- **There is no measurement part.**
- **Other rules.** The home clauses (1 and 2) are judged by the tests SJL-17.3 commits. Clauses 3 and 5 are read from the
  tree and the changelog.
- **It holds no figure.**

**The correspondence** is Broiler.VM's `docs/evidence/sjl-17-001/correspondence.md`, rows O, classified by JSD-0084.

**The commits.**
- **The rule commit** carries this file.
- **The after commit** is the default-branch merge that lands SJL-17.5's cut-over.
- **The base commit** is its first parent. It must meet three conditions:
  - it descends from the rule commit;
  - it descends from the after commit bundle `sjl-9-001` names in this repository;
  - the after commit is its only first-parent successor that changes a path under `Broiler.JS/Broiler.JavaScript.*/`,
    under `Broiler.ECMAScript/` outside `Broiler.ECMAScript/docs/`, or `Directory.Packages.props`.

## Part A - test262 parity per path, in UTC, with negative tests (template T2)

```rule
evidence class: conformance parity (not a measurement)
population: the whole pinned suite - test262 at revision ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e - every path the runner's --all-script-host-verifiable selection admits with --include-negative, in both variants the canonical configuration runs, original and terser; test/intl402/Collator and test/built-ins/String/prototype/localeCompare are in it, and so is test/intl402/Locale/prototype/getCollations/output-array-values.js, which keeps its base verdict (JSD-0082 decision D33 (c)); a regression on any other path fails this part too
configuration: scripts/compliance/run_test262.py through the shell BroilerJS built in Release, by .github/workflows/test262-evidence.yml as the base commit carries it and unchanged at the after commit, dispatched with the tested commit's forty-digit identifier, suite_ref at the revision above, max_workers 4, test_timeout_seconds 30, rerun_timeouts true, time_zone empty and include_negative true: eight shards merged by scripts/compliance/merge_test262_shards.py, --minifier terser at the version its lock file pins, no memory limit, negative-metadata tests included and their expected errors verified in every shard and in the re-run, a --self-check before each shard, every shard running the same shell, shown by the hashes of its binaries, and every shard in the runner's own zone, UTC, shown by its host-zone record; both runs' host-zone records read alike, line for line: TZ unset, UTC +0000 at both instants, and one tzdata release; the base run at the base commit and the after run at the after commit, on the same RID; each run's artifact test262-evidence imported unchanged, with the digest of the zip as downloaded
comparison: per path and variant, joined on both, the verdict at the base against the verdict after, through scripts/compliance/compare_test262_paths.py as committed at SJL-0.9, with the classification file retained beside its report; the script refuses two runs whose includeNegative differs
timeouts: a row that timed out in either run is the one the workflow ran again alone, with one worker, at that run's commit; the script takes the re-run through --base-rerun or --after-rerun, it replaces the row, and both the run and the re-run are retained
admitted difference classes:
  C1 correction - a row failing at the base passes after, because the shared collator follows the pinned text where this engine did not: each row JSD-0084 puts in its column "Correction of Broiler.JS" (numeric and caseFirst applied, the bound compare's identity, and ordering that no longer depends on the host's ICU for the covered locales), each listed with its path, its variant and its row, and in the changelog
  C5 data policy by decision - a row failing at the base passes after, because CLDR 48.2.0's root collation and the tailorings of JSD-0084 decision E3's list replace the host's ICU for the covered locales: each listed with its row and the decision
every other difference is a regression, including a row that passes at the base and fails, times out or is skipped after, whatever its class
MET if and only if both runs are complete (every expected shard reported, no configuration failure, one suite revision, one shell, every shard in the zone, includeNegative true in both), the two runs' host-zone records read alike, the script finds them comparable, no row passing at the base fails, times out or is skipped after, and every changed verdict is in C1 or C5 and is listed.
NOT MET otherwise; and if the two runs' host-zone records differ, the part is not judged until the base is taken again at the same commit in the after run's zone record.
```

## Part B - the orderings dataset (vector agreement, T3's sense)

```rule
evidence class: conformance parity (not a measurement)
population: Broiler.VM's src/tests/cldr/orderings/ program with its retained answer, and its divergences file if that commit holds one (none at Broiler.VM 83290f9, whose src/tests/cldr/orderings/ the commit that carries Broiler.VM's own bundle sjl-17-001 rule leaves unchanged; JSD-0084 finding F3), byte for byte as Broiler.VM's commit that carries its own bundle sjl-17-001 rule holds them, which SJL-17.2 copies into conformance/datasets/intl/ under a new source-commit block of datasets.pin; a copy whose digest differs is not the population
configuration: scripts/conformance/run_reference_datasets.py --bundle sjl-17-001 in the shell built in Release; no LANG and TZ=UTC; at the base commit (retained and read) and at the after commit (judged); the expected answer of each line is Broiler.VM's retained answer with its divergences applied; and once, at the after commit's time, the program run in the reference bundle sjl-9-001's reference.txt names, its output retained
comparison: per line, the line after against its expected answer, and against the reference's line where it differs
admitted difference classes:
  V Broiler.VM's coverage - a line whose expected answer comes from a divergence in a group this bundle's divergence-groups.tsv marks coverage (a tailoring Broiler.VM's data lacks), where this engine's answer after equals the retained answer; each listed
  R the release - a line of V's kind whose answer after equals neither, and equals the reference's line; each listed with the three lines
every other difference is a failure
MET if and only if the program completes after with every line, and every line that differs from its expected answer is in V or R and is listed.
NOT MET otherwise.
```

## Part C - collation under invariant globalization

```rule
evidence class: robustness and conformance (not a comparison of runs)
population: docs/evidence/sjl-17-001/invariant-probe.js, committed with this rule: comparisons and sorts in the covered locales and in locales the host lacks, each line's expected answer in invariant-probe.expected.txt, taken from the reference bundle sjl-9-001's reference.txt names, for the locale this engine resolves under its unchanged available-locale policy (JSD-0082 decision D7)
configuration: the probe evaluated in the shell built in Release, with DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1, no LANG and TZ=UTC, at the base commit (retained and read) and at the after commit (judged)
MET if and only if the probe completes after with every line, no line is an abort: line, and every line equals its expected answer, so that no comparison falls back to code-unit order (JSD-0084 decision E4).
NOT MET otherwise.
```

## Revisions

A rule revised after any run it judges has been seen is a new dated file in this directory. It quotes this one whole
and says it was written afterwards, and its decision is then weaker evidence.

## What this file does not decide

- **The home clauses**: the collator, its vectors and the UCA test files at library level; the SL rules and the probe.
  Tests SJL-17.3 commits hold them.
- **This engine's deletion list and changelog.** They are read from the tree.
- **Broiler.VM's rule.**
- **A speed claim of any kind.**
