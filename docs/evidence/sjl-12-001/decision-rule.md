# Bundle SJL-12-001's decision rule: the parser's in-place preparation, judged by test262 per path, by the architecture test's witness, by the parse corpus's acceptance and by determinism

**Written:** 2026-10-19, at milestone SJL-12 (work package SJL-12.1) of the shared JavaScript libraries programme,
with the contract this repository's `docs/roadmap/SharedLibraries.md` records ("The frontend contract (SJL-12)") and
Broiler.VM's JSD-0080,
`Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0080-the-shared-frontend-contract-and-the-in-place-preparation.md`.
It was written before any change it judges exists: no explicit options, no per-parse counter, no per-scanner token,
no removed reference, no compiler pass and no architecture test. **Milestone judged:** SJL-12,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-12-frontend-contract.md`: its Broiler.JS clauses 1
("The parser reads no ambient state, holds no mutable static, references no `Runtime` type, and does not desugar. A
Broiler.JS architecture test enforces this, with a witness.") and 2 ("test262 per path meets the predeclared
rule."), and this repository's half of shared clause 8. **Owner:** the Broiler.JS maintainer. **Reviewer:** none.
Every role this rule names is held by one person (the programme records this as Broiler.VM's exception EX-30), and
this rule does not claim that anyone independent has read it.

**What this file is.** Four predeclared decisions, one per part.
- **The templates.** Part A is in template T2 of the programme's validation document,
  `Broiler.VM/docs/javascript-shared-libraries/validation.md` section 4. Part B is an architecture test with its
  witness, part C an acceptance comparison over the parse corpus, and part D layer L13's determinism. None of these
  three has a template letter.
- **Each part decides alone.** Part D's population is the inputs part C's after run accepted at entry point P, and
  no part reads another's verdict.
- **There is no measurement part.** SJL-12 measures nothing.
- **There is no part in a zone with daylight-saving time.** Nothing SJL-12.5 changes reads the host's zone.
- **Other rules.** Clause 3 (the contract record in this repository's plan) is read from the plan. Broiler.VM commits
  its own rule for its own engine. Neither is read here.
- **It holds no figure.**

**SJL-12 is not an area verdict.** The extraction gate is applied at SJL-13.1 (JSD-0080, decision D1).

**The corrections this rule classifies by** are JSD-0080's decision D21, listed by name in `Broiler.JS/CHANGELOG.md`
at SJL-12.5. The names J1 and J2 are SJL-12's, not the text area's correspondence rows of the same names:
- **J1:** code compiled while `EvalWithTopLevelAwaitAsync` runs, but not as its own program, is compiled as the
  specification's goal without top-level `await`, as under `Eval`. This covers direct and indirect eval,
  `ShadowRealm.prototype.evaluate`, the Function constructor and the debugger's evaluate.
- **J2:** the code cache keys a compilation on its top-level-`await` permission, so one context no longer answers a
  compilation from the other mode's entry.

**Why negative tests are included.** Bundles `sjl-7-001`, `sjl-8-001` and `sjl-10-001` took their runs with them,
and a parser change shows first in negative tests.

**The base is taken afresh.** It is not taken from SJL-0's base, which predates every cut-over since SJL-3, those of
SJL-3 to SJL-8 and SJL-10, and excludes negative tests (JSD-0080 decision D7). The milestone's SJL-12.5 done-when,
which names SJL-0's base, is revised by a dated note in Broiler.VM's milestone file, filed with JSD-0080. The
commits:
- **The rule commit** is the commit that carries this file. Part A's base run is taken there, and part B's base
  build.
- **The corpus commit** is the default-branch commit that adds SJL-12.4's parse corpus under `conformance/parse/`, the
  tool `docs/evidence/sjl-12-001/tools/parse-corpus/` and `scripts/compliance/compare_parse_corpus.py`. Parts C and
  D's base runs are taken there. It must meet three conditions:
  - it descends from the rule commit;
  - `git diff --name-only` between the two names only paths under `conformance/parse/`, `scripts/`, `docs/` and
    `Broiler.ECMAScript/docs/`, where SJL-12.3's early-error catalogue lives (JSD-0080 decision D31);
  - it is an ancestor of every commit after the rule commit that changes a path under `Broiler.JS/Broiler.JavaScript.*/`,
    or under `Broiler.ECMAScript/` outside `Broiler.ECMAScript/docs/`.
- **The after commit** is the default-branch merge that lands the last of SJL-12.5's items, which is item 5, the
  compiler's for-loop pass, in JSD-0080's order (decision D26). It is named in bundle `sjl-12-001`.

A part whose base was taken at any other commit, or whose corpus commit fails those conditions, is void.

## Part A - test262 parity per path, in UTC, with negative tests (template T2)

```rule
evidence class: conformance parity (not a measurement)
population: the whole pinned suite - test262 at revision ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e, the revision .github/workflows/test262.yml defaults to and Broiler.VM's src/tests/conformance/pins/test262.pin pins - every path the runner's --all-script-host-verifiable selection admits with --include-negative, in both variants the canonical configuration runs, original and terser
configuration: scripts/compliance/run_test262.py through the shell BroilerJS built in Release, by .github/workflows/test262-evidence.yml as this file's commit carries it, dispatched with the tested commit's forty-digit identifier, suite_ref at the revision above, max_workers 4, test_timeout_seconds 30, rerun_timeouts true, time_zone empty and include_negative true: eight shards merged by scripts/compliance/merge_test262_shards.py, --minifier terser at the version its lock file pins, no memory limit, negative-metadata tests included and their expected errors verified in every shard and in the re-run, a --self-check before each shard, every shard running the same shell, shown by the hashes of its binaries, and every shard in the runner's own zone, UTC, shown by its host-zone record; both runs' host-zone records read alike, line for line: TZ unset, UTC +0000 at both instants, and one tzdata release; the base run is taken at the rule commit and the after run at the after commit, on the same RID; each run's artifact test262-evidence is imported unchanged, with the digest of the zip as downloaded
comparison: per path and variant, joined on both, the verdict at the base against the verdict after, through scripts/compliance/compare_test262_paths.py as committed at SJL-0.9, with the classification file retained beside its report; the script refuses two runs whose includeNegative differs
timeouts: a row that timed out in either run is the one the workflow ran again alone, with one worker, at that run's commit; the script takes the re-run through --base-rerun or --after-rerun, it replaces the row, and both the run and the re-run are retained
admitted difference classes:
  C1 correction - a row failing at the base that passes after, because of correction J1 or J2 above; each one listed with its path, its variant and its correction, and listed in the changelog (decision SJL-D-16); a C1 row that neither explains is recorded in the bundle as a finding against JSD-0080 and is a regression
no C3 and no C5: SJL-12 moves no pin, data or policy, and changes no language surface beyond J1 and J2
every other difference is a regression, including a row that passes at the base and fails, times out or is skipped after, and a row whose verdict moves between failed, timed out and skipped without a class
MET if and only if both runs are complete (every expected shard reported, no configuration failure, one suite revision, one shell, every shard in the zone, includeNegative true in both), the two runs' host-zone records read alike, the script finds them comparable, no row passing at the base fails, times out or is skipped after, and every changed verdict is in C1 and is listed.
NOT MET otherwise; and if the two runs' host-zone records differ, because the runners' tzdata moved between them, the part is not judged until the base is taken again at the same commit in the after run's zone record.
```

## Part B - the parser's architecture test and its witness

```rule
evidence class: architecture (not a comparison of runs)
population: the Broiler.JavaScript.Parser assembly built in Release at the rule commit and at the after commit; the test class ParserArchitectureTests of Broiler.JS/Broiler.JavaScript.Architecture.Tests, which SJL-12.5 adds, and the witness source it compiles
the test's clauses, read from the assembly's metadata: no AssemblyRef to Broiler.JavaScript.Runtime, Broiler.JavaScript.Engine, Broiler.JavaScript.Compiler or Broiler.JavaScript.BuiltIns; no TypeRef to AsyncLocal`1 or ThreadLocal`1, and no ThreadStaticAttribute; outside compiler-generated types, no static field that is neither InitOnly nor Literal, no static field of an array type or of a mutable collection type (rule SL2's list), no settable static property, and no InitOnly static field whose type declares a mutable instance field, except primitives, enums, string, readonly structs and FastKeywordMap; no TypeRef to AstIdentifierReplacer, IdentifierNameCollector or ClosureDetector; and its behavioural fixtures - a for (let ...) loop parses to one for-statement whose initialiser is the declaration as written, two parses of one source give equal tree dumps, and concurrent parses of one source on four threads never end on another parse's token; non-vacuity: at least one accepted InitOnly static field is read
configuration: dotnet test of that project in Release at the after commit, three times: over the after commit's Parser assembly; over the rule commit's Parser assembly, named by the environment variable BROILER_ARCH_PARSER_ASSEMBLY that the test reads; and over the after commit with control K applied; each log retained
expected at the base: the test fails, naming at least the static field FastParser.TempVarID, the InitOnly static fields FastScanner.EOF and FastScanner.EmptyToken of the mutable type FastToken, the AssemblyRef to Broiler.JavaScript.Runtime, and a TypeRef to each of the three walkers
witness: the witness source, compiled in memory, declares one member per shape - a mutable static counter, a static readonly field of a mutable class, a static readonly array, a static readonly field of a mutable collection type, a [ThreadStatic] field, an AsyncLocal, a ThreadLocal, a settable static property, a use of a Runtime type and a use of a desugaring walker - and the test refuses each shape
control K: a static counter added to FastParser in a scratch copy of the after commit; the test fails naming it, and passes after the revert, the revert checked
MET if and only if the test passes over the after commit's assembly, fails over the rule commit's naming each violation listed above, refuses every witness shape, and control K fails and then passes as stated.
NOT MET otherwise.
```

## Part C - the parse corpus's acceptance

```rule
evidence class: conformance parity (not a measurement)
population: every input of conformance/parse/corpus.manifest as the corpus commit holds it, under its goal and each of its variants; the test262 inputs read from test262 at ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e and each checked against the manifest's SHA-256, the others from conformance/parse/inputs/
configuration: the tool docs/evidence/sjl-12-001/tools/parse-corpus, as the corpus commit holds it and unchanged at the after commit, built in Release against each commit's own projects, on one thread with a stack of 256 MB, no LANG and TZ=UTC; per input and variant, at two entry points - P, the parser alone (a FastParser over a FastTokenStream and ParseProgram, or FastParser.ParseModule for the module goal), and K, the whole compile (CoreScript.Compile with the module flag and no code cache) - each answering accepted, refused (the exception's type and message) or crashed (the exception's type); at the corpus commit (the base) and at the after commit
completeness: every input and variant reported at both entry points at both commits, the manifest's digest equal to the corpus commit's
controls: a copy of the base report with one outcome flipped and one message changed, which the comparison must report as two differences; retained with its output
comparison: scripts/compliance/compare_parse_corpus.py, as the corpus commit holds it, per input, variant and entry point: the outcome, the exception type and the message
admitted difference classes:
  M message - a refusal at the same entry point with the same exception type whose message differs, admitted only if Broiler.JS/CHANGELOG.md at the after commit lists the change by its old and new text
  X a crash resolved - an input that crashed at entry point K at the base and is accepted, or refused with a SyntaxError, after; each listed with both outcomes, as a correction the changelog names
  D a desugaring refusal moved out of the parser - an input refused at entry point P at the base by the parser's for-loop desugaring itself (AssignTempNames in Broiler.JS/Broiler.JavaScript.Parser/FastParser.ForStatement.cs at the rule commit, whose message is "Unknown token"), accepted at P after, and refused at K after with the exception type and message it had at K at the base; each listed with its outcomes at both entry points
no other class: SJL-12.5 changes no input's acceptance - the for-loop desugaring moves to a pass that runs before the validation that read its output, and corrections J1 and J2 reach no corpus input, which is compiled once, outside any context with top-level await
every other difference is a regression, including an input that moves between accepted, refused and crashed at either entry point outside X and D
MET if and only if both runs are complete, the control is reported, and every difference is in M, X or D and is listed.
NOT MET otherwise.
```

## Part D - determinism of the parser and its compiler pass (layer L13)

```rule
evidence class: determinism (not a measurement)
population: every input and variant accepted at entry point P at the after commit
configuration: the tool's --dump mode at the after commit, which writes for each input the SHA-256 of a canonical dump of the tree the parser returns (each node's type, span, names, flags and children, in tree order) and of the tree the compiler's for-loop pass returns for it; in three processes - run 1 in the manifest's order, run 2 in the reverse order, and run 3 on two threads, each input parsed and prepared twice concurrently, once on each
controls: two patches, each to a scratch copy of the after commit, never merged, retained with their output: K1 a process-wide counter in the pass's temporary names - run 2 must differ from run 1; K2 a list in the dumped tree ordered by each name's string.GetHashCode(), which the runtime randomizes per process - run 1 against run 2 must differ; each reverted, and the revert checked
comparison: per input and variant, the four hashes of each dump
admitted difference classes: none
MET if and only if every input gives one hash per dump in all four, and both controls differ as stated and agree again after their revert.
NOT MET otherwise.
```

**How the parts are read.**
- **Retained:** each run, each comparison's report, the classification file for part A, and a table of every C1, M,
  X and D row.
- **The base runs of parts C and D** at the corpus commit are retained and read. The corpus commit has no for-loop
  pass, so part D's base dumps the parser's tree alone; it shows the counter's dependence on parse order. Part B's
  base run is part of its decision.
- **What decides:** the script decides no class. Totals decide nothing.

## Revisions

A rule revised after any run it judges has been seen is a new dated file in this directory. It quotes this one whole
and says it was written afterwards, and its decision is then weaker evidence.

## What this file does not decide

- **Clause 3**, the contract record in the plan.
- **The public-API records:** `docs/public-api.md`, the changelog and SJL-D-25's dispositions, which are read from the
  tree at SJL-12.5.
- **Broiler.VM's clauses.**
- **The catalogue.**
- **The downstream suites:** Broiler.JSeal and Broiler.HtmlBridge, which this environment cannot run.
- **A speed claim of any kind.** The provider seam's cost is not measured, and nothing here may be read as a measure of
  it.
