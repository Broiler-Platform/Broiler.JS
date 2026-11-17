# Bundle SJL-15-001's decision rule (Broiler.JS), revised on 2026-11-17: a model commit's conditions on Broiler.VM's merges, what part J decides, the commit that carries the class tables and the column, and part D's input

**Written:** 2026-11-17, at milestone SJL-15 (work package SJL-15.1, Broiler.JS's half), **before any run this rule
judges** and before any change it judges exists: no semantic model beyond the declarations walk SJL-13.2 began, no model
merge, no cut-over of this engine's compiler and no deletion. The `semantics` vectors' runs on corpus revisions 42 and 43
had been taken and seen (bundle `sjl-15-001`'s `engine/`); none of them is a run of this rule, which takes no run before
the base it names exists. It is filed on the branch `sjl-15-1-records`, with the rule it revises, before that branch
merges. This file revises [`decision-rule.md`](decision-rule.md) as that file's section "Revisions" says a revision
written before any run it judges is made: a new dated file in this directory, which says so, and which, as a revision
written afterwards does, quotes that file whole (at the end of this file). `decision-rule.md` is not edited. In this
file, "the rule" is `decision-rule.md` and "this revision" is this file. It is the rule's first revision.

**Whose choice it is.** A review of the branch before any run found the matters below. No owner's decision of its own
is behind this revision, and it takes none of the owner's questions. Where a matter needed a choice, the choice is taken
as recommended under the owner's direction of 2026-10-18 ("Please do all recommended decisions and skip waiting for
review"), with the alternative not taken, and the owner may rule otherwise. **Owner:** the Broiler.JS maintainer.
**Reviewer:** none; one person holds every role (Broiler.VM's EX-30).

**What does not change.** The milestone and the clauses judged; the classes E, T, C1 and M and their tables, but as
[`corrections.md`](corrections.md)'s dated note records for C1's row of probe `V046-EA0006`; the product paths; the
library commit, the base commit and the after commit; and parts A to J's rule blocks as written. Matter 1 revises the
definition of a model commit, matter 2 the paragraph "What part J decides", matter 3 records how "Committed in the same
change" is read and which commit is the rule commit, and matter 4 records a correction of part D's input that does not
change its population.

## 1. A model commit's conditions

The rule reads, under "The commits, named before they exist": "**A model commit** is a default-branch merge of SJL-15.2
that changes `Broiler.ECMAScript/Broiler.ECMAScript.Frontend*/`. It descends from the rule commit, from Broiler.VM's
SJL-15.1 rule (merged in Broiler-Platform/Broiler.VM#214) and from the corpus revision that adds the `semantics`
vectors." A merge in this repository cannot descend from a commit of Broiler.VM's, so read with the rule's "A part whose
base was taken at any other commit, or whose commits fail those conditions, is void", the sentence would void part B, and, through the library
commit, which is a model commit, and the base commit, which descends from it, every part judged at either or after. And it leaves out decision D-x2's condition, which this
repository's records quote ("Until it is filed no row has a class across both engines, and no SJL-15.2 code merges",
`docs/roadmap/SharedLibraries.md`, "Shared static semantics (SJL-15)").

**Revised.** A model commit is a default-branch merge of SJL-15.2 that changes
`Broiler.ECMAScript/Broiler.ECMAScript.Frontend*/`. It descends from the rule commit and from the corpus revision that
adds the `semantics` vectors, and **it lands after** Broiler.VM's SJL-15.1 rule has merged (Broiler-Platform/Broiler.VM#214,
`0ddc146f`, observed) **and after** the Broiler.VM change that carries this engine's column and JSD-0088's dated note
completing X2 and X6 has merged (decision D-x2), each merge observed and named, with its commit, in this bundle's README
before any run at the model commit is taken. A merge of SJL-15.2 that lands before either is a commit that fails the
conditions. The rest of the definition (W2, the dark windows of bundle `sjl-14-001`, SJL-11.5's hold) is unchanged.
*Not taken:* striking Broiler.VM from the condition, which would leave decision D-x2's order to the records alone, and a
rule that judges by it would then not check it.

## 2. What part J decides

The paragraph "What part J decides" reads: "The part is a rule part, as bundle `sjl-14-001`'s part I is, and on a lane
that is not controlled it is void." Bundle `sjl-14-001`'s part I is that rule's part for its milestone's gate clause 6
("## Part I - Broiler.JS's parse measurement (clause 6; template T5)"), and that rule reads the clause from it, so the
comparison holds only for its being void.

**Revised.** Part J is void on a lane that is not controlled, as bundle `sjl-14-001`'s part I is; it is a rule part and
no gate clause, as SJL-13 read its own measurement (JSD-0083's decision D24, the reading JSD-0088's decision D-measure
takes). Part J's rule block is unchanged.

## 3. "Committed in the same change", and the rule commit

The rule reads: "Committed in the same change: [`corrections.md`](corrections.md) with its tables (the classes E and T
and the corrections, each by row, before any run), [`inputs/closure-suites.txt`](inputs/closure-suites.txt) (part D's
suites), and this engine's column of the correspondence, [`correspondence-broiler-js.md`](correspondence-broiler-js.md),
with its probes." The commit that first carried the rule, `31ad81404efe580380ba795cba5e615ecd5e7114`, carried
`inputs/closure-suites.txt` with it and none of the rest; `corrections.md`, its tables, the column and the probes were
added in `750630fb3d2d6e5a9d2c6533d5cf5a38542e3bf7`, after the records (`02f67b8b`) and corpus revision 42 (`1cb27c7b`).
The rule's opening also cites sections of `docs/roadmap/SharedLibraries.md` that exist only from `02f67b8b`. `750630fb`
was committed while Broiler.JS's first run on revision 42 was being taken, and before any result of any vector run had
been seen; its tables are written by `tools/write_lists.py` from the stage table and the probes alone, so no vector run
could move them, and no run of this rule exists.

**Read so.** "The same change" is this branch's change, which reaches the default branch by one merge; nothing of it is
filed before that merge. **The rule commit**, as this bundle's README names it, is from this revision the last commit
on this branch that changes `decision-rule.md` or this file, with the merge that brings it into the default branch: it
carries the rule, this revision, the class tables, the column and the probes together, and every section the rule
cites. The tables' revisions after `750630fb` are recorded in `corrections.md`'s dated note. *Not taken:* a revision of
the rule's sentence to name `750630fb`, which would change no outcome and leave the rule commit, as the README first
defined it, without the tables.

## 4. Part D's input

`inputs/closure-suites.txt` said that the search of test file names gives the lines tagged `search`; at `6fda6ca4`
the search does not give `FreeNameScanTests`, which was tagged so: its file name holds none of the searched words, and
the draft of the list had put it under the search. It enters by reading, for row FC1's fact, and its tag is now `read`,
by a dated note in that file that says so. Part D's population, every test class the file names, is unchanged.

## The rule, quoted whole as it stood when this revision was written

````markdown
# Bundle SJL-15-001's decision rule: Broiler.JS on the shared semantic model, judged by test262 per path, by the model merges' identity, by the parse corpus across the cut-over, by the closure-sensitive suites, by the `semantics` vectors, by the home's three clauses, by fuzzing, by the deletion search and by this engine's compile measurement

**Written:** 2026-11-17, at milestone SJL-15 (work package SJL-15.1, Broiler.JS's half) of the shared JavaScript
libraries programme, with this repository's `docs/roadmap/SharedLibraries.md` sections "Static semantics (SJL-15.1)" and
"Shared static semantics (SJL-15)" and Broiler.VM's JSD-0088, before any change it judges exists: no semantic model
beyond the declarations walk SJL-13.2 began, no model merge, no cut-over of this engine's compiler and no deletion.
Committed in the same change: [`corrections.md`](corrections.md) with its tables (the classes E and T and the corrections,
each by row, before any run), [`inputs/closure-suites.txt`](inputs/closure-suites.txt) (part D's suites), and this
engine's column of the correspondence, [`correspondence-broiler-js.md`](correspondence-broiler-js.md), with its probes.
**Milestone judged:** SJL-15, `Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-15-static-semantics.md`: its
home clauses 1 ("The semantic model is implemented under the contract."), 2 ("Every catalogue row is covered.") and 3
("Rule SL9 and the Native AOT probe pass."), and its Broiler.JS clauses 10 ("Its scope analysis and its scattered early
errors are gone, and the compiler reads no bridged tree.", as revised at SJL-14.1 by decision D41) and 11 ("test262 per
path meets the rule. Every correction is listed."); and SJL-15.6's items for this engine: its closure-sensitive suites,
fuzzing and its own measurement. **Owner:** the Broiler.JS maintainer. **Reviewer:** none. Every role this rule names
is held by one person (the programme records this as Broiler.VM's exception EX-30), and this rule does not claim that
anyone independent has read it.

**What this file is.** Ten predeclared decisions, one per part, each of one evidence class. Part A is template T2, part E
template T3 and part J template T5; part B is an identity, part C the parse corpus across the cut-over with bundle
`sjl-12-001`'s tool, part D the closure-sensitive suites, part F the home's clauses 1 and 2, part G the home's clause 3,
part H fuzzing (layer L9) and part I the deletion search. Each part decides alone, and no part reads another's verdict.
It holds no figure: where a count matters, it names the file that fixes it. The bundle has a `manifest.json` at its
evidence step (decision D56).

**SJL-13's and SJL-14's rules, applied to the semantic half.** The milestone's SJL-15.1 says "The rules are those of
SJL-13 and SJL-14, applied to the semantic half". This rule reads that so: bundle `sjl-14-001`'s parts A (test262 per
path), B (identity), D (the parse corpus across the cut-over), J (fuzzing), K (the deletion search) and I (the
measurement) are applied to SJL-15.5's cut-over, each named below where it applies, with its classes; bundle
`sjl-13-001`'s parts B (the vectors at library level), C (determinism), D (the Native AOT probe), E (the rules) and H
(the contract's checklist) are applied to the model at the library commit for the home's clauses. **Decision D52 binds
SJL-15.2's model merges**: before this engine's SJL-14 cut-over (window W2 of bundle `sjl-14-001`), each is a dark
window of SJL-14, judged by part B of bundle `sjl-14-001`'s rule, not by this one.

**The correspondence this rule classifies by** is Broiler.VM's `docs/evidence/sjl-15-001/correspondence.md`, its rows
named by their letters (SC, DE, AB, FF, RS, LB, PN, MR, NE, FC, EE, SL), with this engine's column as
[`correspondence-broiler-js.md`](correspondence-broiler-js.md) gives it (decision D-js-column).

**The classes** (decisions D45 and D64, its second half, as decision D-js-timing lists them), named before any run with
their rows in [`corrections.md`](corrections.md):
- **E, early-error stage** - an input this engine accepts at entry point P (the parse alone, through the bridge or, after
  SJL-15.5, the library) at the base and refuses at P after, refusing it at K (the whole compile) at both, with a
  SyntaxError whose shared code is a `semantics` row of the stage table that this engine checks in a compiler stage
  ([`corrections/class-e.tsv`](corrections/class-e.tsv)). The model reports at the parse what this engine checked only
  in its compiler.
- **T, timing** - an input or test262 row whose SyntaxError, of a row of
  [`corrections/class-t.tsv`](corrections/class-t.tsv), this engine raised at the base after other code had run, or not
  at all, and raises after before any of the program runs: the rows whose site is `FastCompiler.EarlyErrors`, a `Visit*`
  method or `DirectEvalSupport`, which run as a function body is compiled or when eval runs, and the rows this bundle's
  probes show raised late or not at all.
- **C1, correction** - a difference where the model follows the pinned text and this engine did not, by a row of
  [`corrections/corrections.tsv`](corrections/corrections.tsv): the `semantics` rows this engine checks nowhere, the
  rows its probes show answered with another error than a SyntaxError, and the rows of this engine's column whose class
  is a correction of Broiler.JS; each listed with its row and in `Broiler.JS/CHANGELOG.md` at the cut-over (decision
  SJL-D-16 (a)). A C1 difference that no row explains is a finding against the correspondence and a regression.
- **M, message** - a refusal with the same exception type at the same entry point whose message differs, each listed in
  a generated message table with both texts (decision D46).

**The product paths** (decision D42): `Broiler.JS/` except `Broiler.JS/benchmarks/` and `Broiler.JS/CHANGELOG.md`,
`Broiler.ECMAScript/` outside `Broiler.ECMAScript/docs/`, and the root's `Directory.Packages.props`,
`Directory.Build.props`, `Directory.Build.targets`, `global.json` and `NuGet.config`.

**The commits, named before they exist.**
- **The rule commit** carries this file.
- **A model commit** is a default-branch merge of SJL-15.2 that changes `Broiler.ECMAScript/Broiler.ECMAScript.Frontend*/`.
  It descends from the rule commit, from Broiler.VM's SJL-15.1 rule (merged in Broiler-Platform/Broiler.VM#214) and from
  the corpus revision that adds the `semantics` vectors. Before W2 it is a dark commit of bundle `sjl-14-001`, judged
  there; after W2 it is judged by part B here, against its first parent. None lands while SJL-11.5's bases are held
  (bundle `sjl-11-001`'s README, decision D16).
- **The library commit** is the last model commit before the base commit, at which the home's clauses are judged
  (parts F and G), as bundle `sjl-13-001` judged the syntax half at its library commit.
- **The after commit** is the default-branch merge that lands SJL-15.5, one merge: the compiler on the shared tree and
  the model, and the deletion list of [`dispositions.md`](dispositions.md) deleted. **The base commit** is its first
  parent, which descends from the rule commit, from W2 of bundle `sjl-14-001` and from the library commit; the after
  commit is the only first-parent commit that carries any part of SJL-15.5.
- If SJL-15.5 had to land in more than one merge, each is named in this bundle, its base is the first one's first
  parent, and its after runs are taken at the last one.
- Hosted runs are dispatched at any time after their commits exist; local runs are taken from a clean `git archive` of
  each commit they name.

A part whose base was taken at any other commit, or whose commits fail those conditions, is void. **No run is taken
before the base it names exists**: an output taken at another commit, a "before" output at the rule commit among them,
is not a run of this rule and decides nothing.

## Part A - test262 parity per path, in UTC, with negative tests (template T2; clause 11)

```rule
evidence class: conformance parity (not a measurement)
population, configuration, comparison and timeouts: bundle sjl-14-001's part A's, at the base commit and the after commit of this rule
admitted difference classes:
  C1 correction - a row failing at the base that passes after, explained by a row of corrections/corrections.tsv; listed with its path, its variant and its row, and in the changelog; a C1 row that none explains is a finding against the correspondence and is a regression
  T timing (decision D64, its second half) - a negative row failing at the base that passes after because the SyntaxError of a row of corrections/class-t.tsv, which this engine raised late or not at all, is raised before evaluation; listed with its path, its variant and its row
no C3 and no C5: no limit moves at SJL-15.5, and no data, pin or policy
every other difference is a regression, including a row that passes at the base and fails, times out or is skipped after
MET if and only if both runs are complete (every expected shard reported, one suite revision, one shell, every shard in the zone, includeNegative true in both), the two runs' host-zone records read alike, the script finds them comparable, and every changed verdict is in C1 or T and is listed.
NOT MET otherwise; and if the two runs' host-zone records differ, the part is not judged until the base is taken again at the same commit in the after run's zone record.
```

## Part B - a model commit's identity after W2

```rule
evidence class: identity (not a measurement)
population: every input and variant of conformance/parse/corpus.manifest
configuration: bundle sjl-12-001's tool, byte-unchanged and built as bundle sjl-14-001's rule says, in Release against each commit's projects, on one thread with a 256 MB stack, no LANG and TZ=UTC: entry points P and K, and the --dump mode, at each model commit after W2 and at its first parent; and every test project of Broiler.JS.slnx at the model commit by its CI run
comparison: per input, variant and entry point, the outcome, the exception type and the message; per input, the dumps' hashes
admitted difference classes: none
MET if and only if, at every model commit after W2, both runs are complete and identical in every field, and the commit's CI run of every test project passed.
NOT MET otherwise.
```

**How it is read.** Until SJL-15.5 this engine's compiler reads none of the model, so a model merge after W2 that moves
any of its outcomes has moved the parse it reads, and is not a model merge but a change of the syntax half.

## Part C - the parse corpus across the cut-over

```rule
evidence class: conformance parity (not a measurement)
population: part B's
configuration: bundle sjl-12-001's tool, as part B builds it, at the base commit and at the after commit, entry points P and K, and the --dump mode
completeness: every input and variant at both entry points at both commits; the manifest's digest equal
controls: a copy of the base report with one outcome flipped and one message changed, which the comparison must report as two differences; retained
comparison: scripts/compliance/compare_parse_corpus.py, per input, variant and entry point: outcome, exception type and message; per input, the dump hashes
admitted difference classes: C1, E, T and M, each by the rows corrections.md lists
every other difference is a regression
MET if and only if both runs are complete, the control is reported, and every difference is in C1, E, T or M and is listed.
NOT MET otherwise.
```

## Part D - the closure-sensitive suites (SJL-15.1's "Broiler.JS's closure-sensitive suites")

```rule
evidence class: conformance (not a measurement)
population: every test of the test classes inputs/closure-suites.txt names, as the base commit holds them
configuration: dotnet test of each named test project in Release, filtered to the named classes, from a clean git archive of the base commit and of the after commit, TZ=UTC with no LANG
comparison: per test, its result at the base against the after commit
admitted difference classes: a test the after merge revises is listed with its diff, and must assert at the after commit what it asserted at the base, but where its assertion is of an outcome a row of corrections.md moves (C1, E, T or M), each such test listed with the class and the row
every other difference is a regression, a test that fails or is skipped at the after commit among them
MET if and only if every test passes at both commits, every revised test is listed and asserts what it did but for a listed class, and no test of the named classes is deleted.
NOT MET otherwise.
```

**How it is read.** The milestone names "Broiler.JS's closure-sensitive suites" without listing them. `inputs/closure-suites.txt`
names them, from a search of the test file names at `6fda6ca4` for "closure", "captur" and "scope", completed by reading
each test class whose subject is a fact the model moves into the compiler: `arguments`, a separate variable environment,
direct `eval`, `with`, labels and private names. A test class added later is not in the population unless a dated
revision of this rule, written before the base commit exists, adds it.

## Part E - the `semantics` vectors at engine level (template T3, decision D61)

```rule
evidence class: conformance (not a measurement)
population: every vector of the semantics area at the corpus revision the base commit holds
configuration: Broiler.JS/Broiler.JavaScript.Vectors.Tests's engine-level run, TZ=UTC with no LANG, at the base commit and the after commit, from a clean git archive of each
controls: a copy of one semantics vector's expected outcome changed to this engine's opposite answer; the run must report it as a difference; retained
comparison: per vector, the verdict at the base against the verdict after
admitted difference classes: C1 - a vector whose verdict is classified-divergence at the base and match after, listed with its row of the correspondence; a divergence field that no longer holds is retired by the next corpus revision (decision D61, the route the syntax vectors took)
every other change is a regression, including a vector that matches at the base and does not after
MET if and only if every vector's verdict after is match or classified-divergence, every change is in C1 and is listed, and the control is reported.
NOT MET otherwise.
```

## Part F - the model under the contract, and every catalogue row covered (home clauses 1 and 2)

```rule
evidence class: code fact and library-level conformance (not a measurement)
population: the model's public surface at the library commit; every row of Broiler.ECMAScript/docs/frontend/stages.tsv whose stage is semantics; the semantics vectors at the corpus revision the library commit holds
configuration: at the library commit, from a clean git archive: the contract's checklist as bundle sjl-13-001's part H reads it, applied to the facts the milestone lists (scopes and their kinds, declarations and conflicts, Annex B.3.3 aliases, per-function flags, reference resolution with dynamic markers, labels and jump targets, private-name environments, module records, named-evaluation names, free names and captures, and every early error); a listing, per semantics row, of the library's site that reports it, read from the library's code and its stage table; and Broiler.ECMAScript.Conformance.Tests' library-level run, whose adapter answers a semantics vector's refusal or acceptance where the model decides it
controls: a scratch copy of the library commit in which the model reports no diagnostic for one semantics row the vectors refuse; the library-level run must report that row's vectors as differences; reverted
comparison: per fact, the public member that answers it; per row, its site; per vector the adapter answers, its verdict
MET if and only if every fact has a public member and no public type names an engine type, every semantics row has a site in the library or is named, with its reason, as no row the model can report (the stage table's rows that only a run reaches, EA0006 to EA0008), every vector the adapter answers matches, and the control is reported.
NOT MET otherwise.
```

## Part G - rule SL9 and the Native AOT probe (home clause 3)

```rule
evidence class: rule runs and publish (not a measurement)
population and configuration: bundle sjl-13-001's parts C (determinism, by rule SL9 and three runs of the parse corpus in two orders and on two threads) and D (the Native AOT probe) at the library commit, the model's side tables included in what the determinism runs compare
MET if and only if both are MET as bundle sjl-13-001's rule defines them, read at the library commit.
NOT MET otherwise.
```

## Part H - fuzzing (layer L9)

```rule
evidence class: robustness (not a measurement)
population: the seeds of bundle sjl-14-001's inputs/fuzz-seeds.txt, each with its goal, its count of mutants and the time bound per compile, and the semantics vectors' scripts as seeds of their own, with the same count and bound
configuration: bundle sjl-14-001's part J's session, at the after commit in Release
reading: each mutant is accepted, or refused with a SyntaxError; a finding is any other exception type, a lost process or a compile past the bound
controls: a scratch copy in which the compiler throws on one model fact the seeds reach; the session must report a finding; reverted
MET if and only if no finding is reported and the control is reported.
NOT MET otherwise.
```

## Part I - the deletion search (clause 10)

```rule
evidence class: code fact (not a comparison)
population: SJL-15.5's deletion list member by member as dispositions.md lists it, and "the compiler reads no bridged tree": no reference from Broiler.JS/Broiler.JavaScript.Compiler/ to the bridge (Broiler.JavaScript.Parser/Bridge/) or to a type the bridge builds
configuration: the deletion search script, committed to the bundle with the after commit's change, as bundle sjl-14-001's part K runs its search: at the after commit, over Broiler.JS/ and Broiler.ECMAScript/ outside docs/, each listed member's declarations and references, and the compiler project's references to the bridge, every hit listed with its file and line
controls: a scratch copy of the after commit in which one deleted member is declared again and one compiler file calls the bridge; the search must report both; reverted, and the revert checked
MET if and only if no listed implementation is found, every hit is a member dispositions.md keeps as an obsolete facade over the model, the compiler reads no bridged tree, and both controls are reported.
NOT MET otherwise.
```

## Part J - this engine's compile measurement (template T5; decision D-measure)

```rule
evidence class: measurement
population: the workloads of bundle sjl-14-001's inputs/workloads, as committed with that rule; quantity: the time from source text to the compiled delegate, measured on the calling thread
arms: B the base commit's build; A the after commit's; A-again, the A/A lane
configuration: the harness Broiler.JS/docs/roadmap/Measurement.md names, on the lane the bundle names with its effective configuration; interleaved arms; repetitions and warm-up as Measurement.md fixes them; each lane's condition, that every workload compiles to the outcome the base gives, checked before and after
quantity, noise floor and comparison: Q the median per workload and arm; N = |Q(A) - Q(A-again)|; D = Q(A) - Q(B) against N
lane: the part is judged only on a controlled lane as Measurement.md requires; on a lane that is not controlled the part is void - neither MET nor NOT MET - and its runs are retained and recorded as void
MET if and only if the lane is controlled, every condition passes, and for every workload D <= N.
NOT MET if and only if the lane is controlled and a condition fails or some workload has D > N.
```

**What part J decides.** No gate clause: SJL-15's exit gate has no measurement clause and none is added (decision
D-measure). The part is a rule part, as bundle `sjl-14-001`'s part I is, and on a lane that is not controlled it is void.
No speed claim.

## Revisions

A rule revised after any run it judges has been seen is a new dated file in this directory. It quotes this one whole
and says it was written afterwards, and its decision is then weaker evidence. A revision written before any run it
judges, as the population of part D may need, is a new dated file of the same kind, which says so.

## What this file does not decide

- **Clause 10's code fact beyond part I**, read from the tree at the after commit, which the bundle retains.
- **Broiler.VM's clauses 4 to 9**, which Broiler.VM's bundle `sjl-15-001` judges.
- **The model's design in detail**, SJL-15.2's under JSD-0080's contract.
- **Each publication of the shared stream**, the owner's (JSD-0083's decision D30).
- **A speed claim of any kind.**
````
