# Bundle SJL-4-001's decision rule, revised after part A's run was seen: a correction the cut-over had to make

**Written:** 2026-10-14, at milestone SJL-4.6, **after** the after run of part A at `bc2d663`, its
comparison with the base, the stopped run at `2e0f885` and the bundle's report that part A is NOT MET
had all been seen, so the decision it gives is **weaker evidence** than the one
[`decision-rule.md`](decision-rule.md) gives, as that rule's own "Revisions" paragraph says. It was
written at the owner's direction, given the same day after the bundle had reported part A NOT MET
under the original rule. **Owner:** the Broiler.JS maintainer. **Reviewer:** none; every role is one
person (the programme's EX-30), and this revision does not claim that anyone independent has read it.
The bundle's README states part A's decision under both rules.

## What changed, and why

1. **Class C1 gains (b), a correction by code of this engine that the cut-over had to change.** The
   original C1 admits a row that fails at the base and passes after "because the shared
   implementation follows the specification where this engine did not". The run at `bc2d663` has one
   path that changed, in both variants: `test/staging/sm/TypedArray/slice-bitwise-same.js` fails at
   the base and passes after. It passes because `%TypedArray%.prototype.slice` now copies bytes when
   the source and the result have the same element type, as the specification's step 14.g asks, so a
   binary32 signalling NaN keeps its bits where the base quieted it. That change is this engine's,
   not the shared library's, but the cut-over had to make it: the library's binary16 read gives the
   platform's NaN, and `slice`, copying through the value, then lost a binary16 NaN's sign and payload,
   which `test/built-ins/TypedArray/prototype/slice/bit-precision.js`, passing at the base, showed in
   the stopped run at `2e0f885`. The original rule has no class for that, and counts every other
   difference as a regression, an improvement included. **Class (b) admits a row that fails at the
   base and passes after because code of this engine that the cut-over had to change now follows the
   specification where the base did not.** The code must have been changed in a commit between the
   rule commit and the after commit, and had to be changed because, without the change, the move of
   one of the area's operations to the shared library would have broken a behaviour the base had.
   The commit names the operation and the row or test that showed it. A correction in code the
   cut-over did not have to change is not admitted, whatever clause it follows, and a row that
   passes at the base and fails, times out or is skipped after stays a regression, whatever its
   cause.
2. **The classification is a file of its own, and the comparison is run again.** A (b) row is
   classified in `test262/revised-2026-10-14/classifications.json`, written after the run, beside the
   original's classification file, which stays empty, as its comparison stays as it was. The
   collector's `revised` step runs `scripts/compliance/compare_test262_paths.py` again, unchanged, over
   the runs the `test262` step retained (both merged reports and both re-runs of the timed-out rows),
   reading that file, into `test262/revised-2026-10-14/`. It takes no run of its own.
3. **How the script carries the class.** The script admits the labels C1, C3 and C5 only, and this
   revision changes no file the original rule fixes. A (b) row is labelled C1 in the revision's file,
   and its reason begins "C1 (b)"; the README reads which kind each C1 row is.

**Nothing else changed:** part A's population, configuration, comparison, timeouts, classes C3 and C5,
the regression line and the MET line are the original's. Part A's block, restated with class C1 as
revised; its configuration names the base run by the original rule's header, quoted below, and its
comparison names the revision's classification file:

```rule
evidence class: conformance parity (not a measurement)
population: the whole pinned suite - test262 at revision ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e, the revision .github/workflows/test262.yml defaults to and Broiler.VM's src/tests/conformance/pins/test262.pin pins - every path the runner's --all-script-host-verifiable selection admits, in both variants the canonical configuration runs, original and terser
configuration: scripts/compliance/run_test262.py through the shell BroilerJS built in Release, in the configuration of the test262 workflow's full phase, as bundle sjl-3-001's after run took it: --all-script-host-verifiable over eight shards merged by scripts/compliance/merge_test262_shards.py; --minifier terser at the version its lock file pins; the per-test timeout, the minifier timeout and the memory limit the workflow's inputs default to; negative-metadata tests not included; a --self-check before the shards; the worker count may differ from the workflow's and is recorded; the base run is the one the original rule's header names; the after run is taken at the commit that lands SJL-4.5's cut-over, named in bundle sjl-4-001, on the same RID
comparison: per path and variant, joined on both, the verdict at the base against the verdict after, through scripts/compliance/compare_test262_paths.py as committed at SJL-0.9, with the classification file retained beside its report; under this revision, the file test262/revised-2026-10-14/classifications.json, and the report beside it
timeouts: a row that timed out in either run is run again alone, with one worker, at that run's commit; the script takes the re-run through --base-rerun or --after-rerun, it replaces the row, and both the run and the re-run are retained
admitted difference classes:
  C1 correction - a row failing at the base passes after, either (a) because the shared implementation follows the specification where this engine did not, or (b) because code of this engine that SJL-4.5's cut-over had to change follows the specification where the base did not: code changed in a commit between the rule commit and the after commit because, without the change, the move of one of the area's operations to the shared library would have broken a behaviour the base had, the commit naming the operation and the row or test that showed it; each one listed with its path, its variant and the vector, the specification clause or the correction of the area record that names it, and listed in the changelog (decision SJL-D-16); a (b) row also listed with the commit that changed the code, why the cut-over had to change it, and the engine's test that holds the case; a correction in code the cut-over did not have to change is not in C1
  C3 implementation-defined - the verdict changes on a surface the specification leaves to the implementation (an implementation-approximated Math function, a NaN's encoding), and the area's vectors classify it; each one listed with its vector
  C5 data policy by decision - the verdict changes because a dated data decision moved a pin or a policy; each one listed with the decision
every other difference is a regression, including a row that passes at the base and fails, times out or is skipped after, which no class admits, and a row whose verdict moves between failed, timed out and skipped without a class
MET if and only if both runs are complete (every expected shard reported, no configuration failure, one suite revision), the script finds them comparable, no row passing at the base fails, times out or is skipped after, and every changed verdict is in C1, C3 or C5 and is listed.
NOT MET otherwise.
```

**How it is read.** As the original's part A: the bundle retains the revised comparison's report and
the classification file it read, and a table naming each C1 row with its kind, (a) or (b), and its
reason. The script decides no class; a classification of a row that passed at the base is reported as
refused.

## The rule it revises, quoted whole

> # Bundle SJL-4-001's decision rule: the numerics area's test262 parity per path
>
> **Written:** 2026-10-13, at milestone SJL-4 (work package SJL-4.1) of the shared JavaScript libraries
> programme, with the area verdict this repository's `docs/roadmap/SharedLibraries.md` records, and
> before any shared kernel, adapter or cut-over of the numerics area exists. **Milestone judged:** SJL-4,
> `Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-4-numerics.md`: its Broiler.JS clause 5.
> **Owner:** the Broiler.JS maintainer. **Reviewer:** none. Every role this rule names is held by one
> person (the programme records this as Broiler.VM's exception EX-30), and this rule does not claim
> that anyone independent has read it.
>
> **What this file is.** One predeclared decision, of one evidence class, in template T2 of the
> programme's validation document, `Broiler.VM/docs/javascript-shared-libraries/validation.md` section 4.
> **There is no measurement part**: SJL-4's exit gate has no measurement clause, unlike SJL-3's. The
> library-level reference-arm rule is [`reference-arms.md`](reference-arms.md) beside this file;
> Broiler.VM commits its own rule for its own engine. Neither is read here. **It holds no figure.**
>
> **The base.** SJL-4's base is this engine after SJL-3. Its whole-suite run is **bundle
> [`sjl-3-001`](../sjl-3-001/README.md)'s after run**, taken at `b598028`, whose shell that bundle shows
> `ef67693` builds identically but for the commit each assembly names. This rule takes that run as its
> base on one condition, which the bundle shows before it is read: `git diff --name-only ef67693` to the
> commit that carries this rule, under `Broiler.JS/` and `Broiler.ECMAScript/`, names no file of a
> project the shell's build reads, only test projects and documents. If it names one, the base is
> taken again at the commit that carries this rule, in the same configuration, and retained in
> `sjl-4-001/test262/base/`; a base taken after any code of SJL-4 landed is void.
>
> ## Part A - test262 parity per path (template T2)
>
> ```rule
> evidence class: conformance parity (not a measurement)
> population: the whole pinned suite - test262 at revision ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e, the revision .github/workflows/test262.yml defaults to and Broiler.VM's src/tests/conformance/pins/test262.pin pins - every path the runner's --all-script-host-verifiable selection admits, in both variants the canonical configuration runs, original and terser
> configuration: scripts/compliance/run_test262.py through the shell BroilerJS built in Release, in the configuration of the test262 workflow's full phase, as bundle sjl-3-001's after run took it: --all-script-host-verifiable over eight shards merged by scripts/compliance/merge_test262_shards.py; --minifier terser at the version its lock file pins; the per-test timeout, the minifier timeout and the memory limit the workflow's inputs default to; negative-metadata tests not included; a --self-check before the shards; the worker count may differ from the workflow's and is recorded; the base run is the one this file's header names; the after run is taken at the commit that lands SJL-4.5's cut-over, named in bundle sjl-4-001, on the same RID
> comparison: per path and variant, joined on both, the verdict at the base against the verdict after, through scripts/compliance/compare_test262_paths.py as committed at SJL-0.9, with the classification file retained beside its report
> timeouts: a row that timed out in either run is run again alone, with one worker, at that run's commit; the script takes the re-run through --base-rerun or --after-rerun, it replaces the row, and both the run and the re-run are retained
> admitted difference classes:
>   C1 correction - a row failing at the base passes after, because the shared implementation follows the specification where this engine did not; each one listed with its path, its variant and the vector, the specification clause or the correction of the area record that names it, and listed in the changelog (decision SJL-D-16)
>   C3 implementation-defined - the verdict changes on a surface the specification leaves to the implementation (an implementation-approximated Math function, a NaN's encoding), and the area's vectors classify it; each one listed with its vector
>   C5 data policy by decision - the verdict changes because a dated data decision moved a pin or a policy; each one listed with the decision
> every other difference is a regression, including a row that passes at the base and fails, times out or is skipped after, which no class admits, and a row whose verdict moves between failed, timed out and skipped without a class
> MET if and only if both runs are complete (every expected shard reported, no configuration failure, one suite revision), the script finds them comparable, no row passing at the base fails, times out or is skipped after, and every changed verdict is in C1, C3 or C5 and is listed.
> NOT MET otherwise.
> ```
>
> **How it is read.** As bundle `sjl-3-001`'s rule reads its own part A: the bundle retains the after
> run, the comparison's JSON report, the classification file it read, and a table naming each C1, C3
> and C5 row with its reason. The script decides no class; a classification of a row that passed at the
> base is reported as refused. Totals do not decide part A.
>
> ## Revisions
>
> A rule revised after any run it judges has been seen is a new dated file in this directory, quoting
> this one whole and saying it was written afterwards; its decision is then weaker evidence.
>
> ## What this file does not decide
>
> - **The library-level comparison**, which [`reference-arms.md`](reference-arms.md) decides.
> - **Broiler.VM's conformance or fuel accounting**, which Broiler.VM's own rule decides.
> - **The vectors, the deletion list and the corrections list** (clauses 4, 6 and 7), each judged by its
>   own mechanism.
> - **A speed claim of any kind.** SJL-4 measures nothing; the risk the area design names for this
>   engine's hot paths is answered, if at all, by a later measurement under its own rule.
