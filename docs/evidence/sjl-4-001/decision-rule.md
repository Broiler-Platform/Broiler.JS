# Bundle SJL-4-001's decision rule: the numerics area's test262 parity per path

**Written:** 2026-10-13, at milestone SJL-4 (work package SJL-4.1) of the shared JavaScript libraries
programme, with the area verdict this repository's `docs/roadmap/SharedLibraries.md` records, and
before any shared kernel, adapter or cut-over of the numerics area exists. **Milestone judged:** SJL-4,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-4-numerics.md`: its Broiler.JS clause 5.
**Owner:** the Broiler.JS maintainer. **Reviewer:** none. Every role this rule names is held by one
person (the programme records this as Broiler.VM's exception EX-30), and this rule does not claim
that anyone independent has read it.

**What this file is.** One predeclared decision, of one evidence class, in template T2 of the
programme's validation document, `Broiler.VM/docs/javascript-shared-libraries/validation.md` section 4.
**There is no measurement part**: SJL-4's exit gate has no measurement clause, unlike SJL-3's. The
library-level reference-arm rule is [`reference-arms.md`](reference-arms.md) beside this file;
Broiler.VM commits its own rule for its own engine. Neither is read here. **It holds no figure.**

**The base.** SJL-4's base is this engine after SJL-3. Its whole-suite run is **bundle
[`sjl-3-001`](../sjl-3-001/README.md)'s after run**, taken at `b598028`, whose shell that bundle shows
`ef67693` builds identically but for the commit each assembly names. This rule takes that run as its
base on one condition, which the bundle shows before it is read: `git diff --name-only ef67693` to the
commit that carries this rule, under `Broiler.JS/` and `Broiler.ECMAScript/`, names no file of a
project the shell's build reads, only test projects and documents. If it names one, the base is
taken again at the commit that carries this rule, in the same configuration, and retained in
`sjl-4-001/test262/base/`; a base taken after any code of SJL-4 landed is void.

## Part A - test262 parity per path (template T2)

```rule
evidence class: conformance parity (not a measurement)
population: the whole pinned suite - test262 at revision ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e, the revision .github/workflows/test262.yml defaults to and Broiler.VM's src/tests/conformance/pins/test262.pin pins - every path the runner's --all-script-host-verifiable selection admits, in both variants the canonical configuration runs, original and terser
configuration: scripts/compliance/run_test262.py through the shell BroilerJS built in Release, in the configuration of the test262 workflow's full phase, as bundle sjl-3-001's after run took it: --all-script-host-verifiable over eight shards merged by scripts/compliance/merge_test262_shards.py; --minifier terser at the version its lock file pins; the per-test timeout, the minifier timeout and the memory limit the workflow's inputs default to; negative-metadata tests not included; a --self-check before the shards; the worker count may differ from the workflow's and is recorded; the base run is the one this file's header names; the after run is taken at the commit that lands SJL-4.5's cut-over, named in bundle sjl-4-001, on the same RID
comparison: per path and variant, joined on both, the verdict at the base against the verdict after, through scripts/compliance/compare_test262_paths.py as committed at SJL-0.9, with the classification file retained beside its report
timeouts: a row that timed out in either run is run again alone, with one worker, at that run's commit; the script takes the re-run through --base-rerun or --after-rerun, it replaces the row, and both the run and the re-run are retained
admitted difference classes:
  C1 correction - a row failing at the base passes after, because the shared implementation follows the specification where this engine did not; each one listed with its path, its variant and the vector, the specification clause or the correction of the area record that names it, and listed in the changelog (decision SJL-D-16)
  C3 implementation-defined - the verdict changes on a surface the specification leaves to the implementation (an implementation-approximated Math function, a NaN's encoding), and the area's vectors classify it; each one listed with its vector
  C5 data policy by decision - the verdict changes because a dated data decision moved a pin or a policy; each one listed with the decision
every other difference is a regression, including a row that passes at the base and fails, times out or is skipped after, which no class admits, and a row whose verdict moves between failed, timed out and skipped without a class
MET if and only if both runs are complete (every expected shard reported, no configuration failure, one suite revision), the script finds them comparable, no row passing at the base fails, times out or is skipped after, and every changed verdict is in C1, C3 or C5 and is listed.
NOT MET otherwise.
```

**How it is read.** As bundle `sjl-3-001`'s rule reads its own part A: the bundle retains the after
run, the comparison's JSON report, the classification file it read, and a table naming each C1, C3
and C5 row with its reason. The script decides no class; a classification of a row that passed at the
base is reported as refused. Totals do not decide part A.

## Revisions

A rule revised after any run it judges has been seen is a new dated file in this directory, quoting
this one whole and saying it was written afterwards; its decision is then weaker evidence.

## What this file does not decide

- **The library-level comparison**, which [`reference-arms.md`](reference-arms.md) decides.
- **Broiler.VM's conformance or fuel accounting**, which Broiler.VM's own rule decides.
- **The vectors, the deletion list and the corrections list** (clauses 4, 6 and 7), each judged by its
  own mechanism.
- **A speed claim of any kind.** SJL-4 measures nothing; the risk the area design names for this
  engine's hot paths is answered, if at all, by a later measurement under its own rule.
