# Bundle SJL-3-001's decision rule: the pilot's test262 parity per path, and Broiler.JS's measurement

**Written:** 2026-10-12, at milestone SJL-0 (work package SJL-0.9) of the shared JavaScript libraries
programme, before any shared library, adapter or cut-over of the pilot exists, and with the
comparison script of part A and the workloads of part B committed in the same change. **Milestone
judged:** SJL-3, `Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-3-number-conversion-pilot.md`:
its Broiler.JS clauses 5 and 7 (the measurement half of clause 7). **Owner:** the Broiler.JS
maintainer. **Reviewer:** none. Every role this rule names is held by one person (the programme
records this as Broiler.VM's exception EX-30), and this rule does not claim that anyone independent
has read it.

**What this file is.** Two predeclared decisions, one per part, each of one evidence class, in the
templates T2 and T5 of the programme's validation document,
`Broiler.VM/docs/javascript-shared-libraries/validation.md` section 4. **Each part decides alone**,
and neither part's runs are read by the other. Part B is the only measurement, and SJL-3 retains its
runs in a sub-bundle of their own, `sjl-3-001/measurement/`, with its own README and manifest,
because [`Measurement.md`](../../roadmap/Measurement.md) asks every result bundle to declare exactly
one evidence class. Broiler.VM commits its own rule for its own engine in its own repository, and the
library-level reference-arm rule is [`reference-arms.md`](reference-arms.md) beside this file;
neither is read here. **It holds no figure.** Where a count or an allowance matters, it names the
file that fixes it, or states the count as a parameter of the protocol, not as a result.

**The base.** Every part compares an after arm with the base that bundle
[`sjl-0-001`](../sjl-0-001/README.md) names: its commit, `44a214dc`, taken before any engine changed.
A part whose base is not retained in that bundle, or cannot be rebuilt from that commit, is void and
is not judged.

## Part A - test262 parity per path (template T2)

```rule
evidence class: conformance parity (not a measurement)
population: the whole pinned suite - test262 at revision ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e, the revision .github/workflows/test262.yml defaults to and Broiler.VM's src/tests/conformance/pins/test262.pin pins - every path the runner's --all-script-host-verifiable selection admits, in both variants the canonical configuration runs, original and terser
configuration: scripts/compliance/run_test262.py through the shell BroilerJS built in Release, in the configuration of the test262 workflow's full phase at the base commit: --all-script-host-verifiable over eight shards merged by scripts/compliance/merge_test262_shards.py; --minifier terser at the version its lock file pins; the per-test timeout, the minifier timeout and the memory limit the workflow's inputs default to; negative-metadata tests not included, as the workflow's default; a --self-check before the shards; the worker count may differ from the workflow's and is recorded; the base run is bundle sjl-0-001's, at its base commit; the after run is taken at the commit that lands SJL-3.5's cut-over, named in bundle sjl-3-001, on the same RID
comparison: per path and variant, joined on both, the verdict at the base against the verdict after, through scripts/compliance/compare_test262_paths.py as committed with this rule, with the classification file retained beside its report
timeouts: a row that timed out in either run is run again alone, with one worker, at that run's commit; the script takes the re-run through --base-rerun or --after-rerun, it replaces the row, and both the run and the re-run are retained
admitted difference classes:
  C1 correction - a row failing at the base passes after, because the shared implementation follows the specification where this engine did not; each one listed with its path, its variant and the vector or specification clause that names the correction, and listed in the changelog (decision SJL-D-16)
  C3 implementation-defined - the verdict changes on a surface the specification leaves to the implementation, and the area's vectors classify it; each one listed with its vector
  C5 data policy by decision - the verdict changes because a dated data decision moved a pin or a policy; each one listed with the decision
every other difference is a regression, including a row that passes at the base and fails, times out or is skipped after, which no class admits, and a row whose verdict moves between failed, timed out and skipped without a class
MET if and only if both runs are complete (every expected shard reported, no configuration failure, one suite revision), the script finds them comparable, no row passing at the base fails, times out or is skipped after, and every changed verdict is in C1, C3 or C5 and is listed.
NOT MET otherwise.
```

**How it is read.** The bundle retains both merged reports, the comparison's JSON report, the
classification file it read, and a table naming each C1, C3 and C5 row with its reason. The script
decides no class: it joins, finds every changed row, and marks a row admitted only when the
classification file names an admitted class and a reason; a classification of a row that passed at
the base is reported as refused. Totals do not decide part A: a row the comparison cannot admit is a
regression even when the totals improve. The two engines' test262 totals are not compared with each
other anywhere.

## Part B - Broiler.JS's measurement (template T5)

```rule
evidence class: measurement; in Measurement.md's classes, an acceptance comparison when the lane is a controlled lane as its section "Controlled lanes and effective configuration" requires, and smoke otherwise; a smoke run is void for this part and is not judged
population: the six workloads of NumberConversionBenchmarks (Broiler.JS/benchmarks/Broiler.JavaScript.Engine.Benchmarks/NumberConversionBenchmarks.cs) as committed with this rule - ToString (number to string), ToNumber (string to number) and Literals (the compiler's literal values), each with its control, ToStringControl, ToNumberControl and LiteralsControl; the benchmark project built in Release under JIT with its own runtime settings; one machine and one RID, both named in the sub-bundle's manifest
arms: B the base, a worktree at this rule's commit, whose engine projects are byte-identical to bundle sjl-0-001's base commit (the bundle shows it: git diff between the two commits touches nothing under Broiler.JS/ but the benchmark project); A the commit that lands SJL-3.5's cut-over, with NumberConversionBenchmarks.cs and the benchmark Program.cs as this rule's commit has them; A-again the A arm measured a second time under another lane name, the A/A lane; each arm's benchmark and engine assemblies named by hash in the manifest, which is written and made read-only before any lane runs
configuration: in every repetition, B, A and A-again each run once, in an order that rotates by repetition, so the arms are balanced and process-isolated as Measurement.md's acceptance comparison asks; one run is one launch of the benchmark executable with --filter on this class only, BROILER_BENCHMARK_PROFILE=baseline and a build timeout ample for the lane, which runs all six workloads, each in BenchmarkDotNet's own child process, under the baseline job BenchmarkConfiguration.cs fixes at this rule's commit (its launch, warm-up and iteration counts); seven repetitions per arm, every one retained
condition: before and after every run, the arm's --number-conversion-answers prints every workload's answer; each answer equals the same workload's answer at arm B's first check; a run whose arm's answers differ fails the condition
configuration check: the configuration object --number-conversion-answers prints (framework, RID, architecture, GC mode, tiering, PGO and ReadyToRun settings, vector acceleration, processor count), and in each run's full JSON report BenchmarkDotNet's host environment record (runtime and SDK versions, architecture, processor, JIT) with each workload's hardware-intrinsics record, are identical across arms, lanes and repetitions
quantity: Q(workload, arm) = the median, over the arm's seven runs, of BenchmarkDotNet's reported median time per operation of that workload in that run
noise floor: N(workload) = |Q(workload, A) - Q(workload, A-again)|
comparison: D(workload) = Q(workload, A) - Q(workload, B)      (positive: the after arm slower), against N(workload), for ToString, ToNumber and Literals; one engine's after arm against the same engine's base arm, never against another engine
attribution, retained beside D and deciding nothing: E(workload, arm) = Q(workload, arm) - Q(control, arm)
guardrails, reported for every workload and arm and deciding nothing: BenchmarkDotNet's allocated bytes per operation and its collection counts
admitted difference classes: none; a measurement admits no class, and a difference within the A/A floor is below resolution rather than admitted
MET if and only if all of:
  1. the lane is a controlled lane, recorded as Measurement.md requires;
  2. every run's condition passes, before and after, for every arm;
  3. every configuration check is identical across arms, lanes and repetitions;
  4. every run completed every workload, and BenchmarkDotNet reported no workload with issues;
  5. for ToString, ToNumber and Literals, D <= N.
NOT MET otherwise.
readings:
  - a workload whose |D| <= N is below resolution, and meets clause 5;
  - clause 5 has no tolerance but the A/A lane: this rule states no percentage, so the after arm is held to the base within this lane's own noise on that workload;
  - a failed condition is a NOT MET, not a re-run; an interrupted collection may be run again from the start, with the stopped run retained beside it and named as stopped;
  - this part returns MET or NOT MET, not one of Measurement.md's candidate decisions, because it makes no performance claim; a NOT MET under clauses 1 to 4 is what Measurement.md calls invalid-run, and under clause 5 is a regression SJL-3 answers before it closes.
```

**Why the answers count conversions.** The cut-over corrects results this engine gets wrong today,
among them radix digits beyond the range of `long`, the whitespace set and long radix strings. An
answer built from the results would change for a correction, and the condition would refuse the very
change the pilot exists to make. Each workload's answer therefore counts the conversions it
completed: the same at the base and after, and still failed by a lane that throws or stops early.
The results themselves are judged by the vectors and by part A, not here.

**The base reading at SJL-0.** Bundle `sjl-0-001` measures arm B alone, under this part's harness:
B and an A/A lane B-again, rotated, seven runs each. **Its one decision is not this part's**: each
workload's attribution E(B) is recorded as the base's figure when |E(B)| exceeds
|Q(B) - Q(B-again)|, and as below resolution otherwise. It exists so the workloads, the harness and
the base's noise floor exist before any code moves. It decides nothing about SJL-3, which rebuilds B
and runs it rotated with A; and when its lane is not a controlled lane, it is smoke evidence in
Measurement.md's classes, and says so.

**What part B does not decide.** A speed claim of any kind, in either direction. A comparison with
Broiler.VM, whose own measurement is judged by its own rule in its own repository. Anything about a
RID or a machine other than the one named.

## Revisions

A rule revised after any run it judges has been seen is a new dated file in this directory, quoting
this one whole and saying it was written afterwards; its decision is then weaker evidence. **SJL-3.2
confirms this file, the comparison script and the workloads unchanged before any code of SJL-3.3 to
SJL-3.5 exists**, and a later bundle shows the ancestry with `git merge-base --is-ancestor`.

## What this file does not decide

- **The library-level reference-arm comparison.** Its rule is [`reference-arms.md`](reference-arms.md).
- **Engine-level vectors** (clause 6). They are read at SJL-3's corpus revision in the way
  [bundle `sjl-1-001`'s rule](../sjl-1-001/decision-rule.md) classifies them, and the changelog lists
  each correction.
- **Broiler.VM's test262 parity, fuel-parity twins or measurement.** Each is judged by Broiler.VM's
  own rule.
- **The deletion list, the public-type dispositions, the pristine consumer and the package checks**
  (clauses 4 and 7). Each is a clause of SJL-3 judged by its own mechanism, not by a comparison this
  file states.
