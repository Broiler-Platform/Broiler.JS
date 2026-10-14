# Bundle SJL-5-001's decision rule: the text area's test262 parity per path

**Written:** 2026-10-14, at milestone SJL-5 (work package SJL-5.1) of the shared JavaScript libraries
programme, with the area verdict this repository's `docs/roadmap/SharedLibraries.md` records, and
before any shared member, adapter or cut-over of the text area exists. **Milestone judged:** SJL-5,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-5-text-algorithms.md`: its Broiler.JS clause 4.
**Owner:** the Broiler.JS maintainer. **Reviewer:** none. Every role this rule names is held by one
person (the programme records this as Broiler.VM's exception EX-30), and this rule does not claim
that anyone independent has read it.

**What this file is.** One predeclared decision, of one evidence class, in template T2 of the
programme's validation document, `Broiler.VM/docs/javascript-shared-libraries/validation.md` section 4.
**There is no measurement part**: SJL-5's exit gate has no measurement clause. The library-level
reference-arm rule is [`reference-arms.md`](reference-arms.md) beside this file; Broiler.VM commits its
own rule for its own engine. Neither is read here. **It holds no figure.**

**The base is taken afresh.** SJL-5's base is this engine after SJL-4. Bundle `sjl-4-001`'s after run,
taken at `bc2d663`, cannot stand for it: `git diff --name-only bc2d663 62c42e3` under `Broiler.JS/` and
`Broiler.ECMAScript/` names `Broiler.ECMAScript/Directory.Build.props`, which every shared project's build
reads, because the shared stream's version moved to `0.1.0-preview.2` after that run. So the base is
**the commit that carries this rule**, run in the configuration below **before any code of SJL-5 lands**,
and retained in `sjl-5-001/test262/base/`. A base taken after any code of SJL-5 landed is void.

## Part A - test262 parity per path (template T2)

```rule
evidence class: conformance parity (not a measurement)
population: the whole pinned suite - test262 at revision ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e, the revision .github/workflows/test262.yml defaults to and Broiler.VM's src/tests/conformance/pins/test262.pin pins - every path the runner's --all-script-host-verifiable selection admits, in both variants the canonical configuration runs, original and terser
configuration: scripts/compliance/run_test262.py through the shell BroilerJS built in Release, in the configuration bundle sjl-4-001's after run took: --all-script-host-verifiable over eight shards merged by scripts/compliance/merge_test262_shards.py; --minifier terser at the version its lock file pins; the per-test timeout, the minifier timeout and the memory limit that run used; negative-metadata tests not included; a --self-check before the shards; every shard running the same shell, shown by the hashes of its binaries; the worker count may differ from the workflow's and is recorded; each run taken either locally, as bundle sjl-4-001's was, or by .github/workflows/test262-evidence.yml, which runs this configuration and writes nothing, and the bundle records which; the base run is the one this file's header names; the after run is taken at the commit that lands SJL-5.5's cut-over, named in bundle sjl-5-001, on the same RID
comparison: per path and variant, joined on both, the verdict at the base against the verdict after, through scripts/compliance/compare_test262_paths.py as committed at SJL-0.9, with the classification file retained beside its report
timeouts: a row that timed out in either run is run again alone, with one worker, at that run's commit; the script takes the re-run through --base-rerun or --after-rerun, it replaces the row, and both the run and the re-run are retained
admitted difference classes:
  C1 correction - a row failing at the base passes after, because the shared implementation, or this engine's own correction the area record names (split's limit, the ordinal search, the raw-JSON brand), follows the specification where this engine did not; each one listed with its path, its variant and the vector, the specification clause or the row of the correspondence (Broiler.VM's docs/evidence/sjl-5-001/correspondence.md) that names the correction, and listed in the changelog (decision SJL-D-16)
  C3 implementation-defined - the verdict changes on a surface the specification leaves to the implementation (a length or depth limit within the specification's latitude), and the area's vectors classify it; each one listed with its vector
  C5 data policy by decision - the verdict changes because a dated data decision moved a pin or a policy; each one listed with the decision
every other difference is a regression, including a row that passes at the base and fails, times out or is skipped after, which no class admits, and a row whose verdict moves between failed, timed out and skipped without a class
MET if and only if both runs are complete (every expected shard reported, no configuration failure, one suite revision, one shell), the script finds them comparable, no row passing at the base fails, times out or is skipped after, and every changed verdict is in C1, C3 or C5 and is listed.
NOT MET otherwise.
```

**How it is read.** As bundle `sjl-4-001`'s rule reads its own part A: the bundle retains the after
run, the comparison's JSON report, the classification file it read, and a table naming each C1, C3
and C5 row with its reason. The script decides no class; a classification of a row that passed at the
base is reported as refused. Totals do not decide part A.

**What SJL-4 taught this rule.** Bundle `sjl-4-001`'s part A met a row that improved through this
engine's own `slice`, a correction the cut-over had to make, which its rule as written admitted in no
class, and the rule was revised after the run. This rule names the engine-side corrections the area
record lists in C1 before any run, so such a row is classified under the rule as written.

## Revisions

A rule revised after any run it judges has been seen is a new dated file in this directory, quoting
this one whole and saying it was written afterwards; its decision is then weaker evidence.

## What this file does not decide

- **The library-level comparison**, which [`reference-arms.md`](reference-arms.md) decides.
- **Broiler.VM's conformance, fuel accounting or JSON modules**, which Broiler.VM's own rule decides.
- **The vectors, the deletion list and the corrections list** (clauses 1, 3 and 5), each judged by its
  own mechanism.
- **A speed claim of any kind.** SJL-5 measures nothing; the risk the area design names for this
  engine's JSON parsing speed when it leaves System.Text.Json is answered, if at all, by a later
  measurement under its own rule.
