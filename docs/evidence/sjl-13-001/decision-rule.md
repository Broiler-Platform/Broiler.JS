# Bundle SJL-13-001's decision rule: the shared syntax library in its home, judged at library level by the parse corpus and test262's syntax tests as classified, by the syntax vectors, by determinism, by the Native AOT probe, by the shared libraries' rules, by its limits, by fuzzing and by the contract's checklist

**Written:** 2026-10-26, at milestone SJL-13 (work package SJL-13.1) of the shared JavaScript libraries programme,
with the frontend's area verdict this repository's `docs/roadmap/SharedLibraries.md` records ("Area verdicts") and
Broiler.VM's JSD-0083, before any code of the library exists: no `Broiler.ECMAScript.Frontend` project, no syntax
vector and no runner adapter. Committed in the same change: the classification
([`classification/`](classification/README.md)), built by `scripts/frontend/classify_parse_corpus.py` from the stage
table `Broiler.ECMAScript/docs/frontend/stages.tsv`, and part F's seeds and part G's deep-nesting corpus
([`inputs/`](inputs/README.md)). **Milestone judged:** SJL-13,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-13-shared-syntax-and-vm.md`: its home clauses 1 ("The
frontend's syntax half is implemented under the contract and the conventions."), 2 ("The parse corpus and test262's
syntax tests pass at library level, as classified.") and 3 ("Rule SL9 (determinism) and the Native AOT probe pass.").
**Owner:** the Broiler.JS maintainer. **Reviewer:** none. Every role this rule names is held by one person (the
programme records this as Broiler.VM's exception EX-30), and this rule does not claim that anyone independent has read
it.

**What this file is.** Eight predeclared decisions, one per part. Part B is template T3. Part A is an acceptance
comparison against a classification fixed before the library exists; part C is layer L13 and rule SL9; part D the
Native AOT probe (layer L10); part E the rule runs of SL1 to SL10; part F layer L9; part G the contract's limits; part H
a record-consistency check of the library against the contract. Each part decides alone.

**Which part reads which clause.** Clause 1 ("implemented under the contract and the conventions") is read from part H
(the contract) and part E (the conventions, through SL1 to SL10), with part G for the contract's limits. Clause 2 is
read from parts A and B. Clause 3 is read from parts C (SL9) and D (the probe). A clause is met when every part it is
read from is MET. **There is no base**: the library is new, and every part is judged at one commit. **There is no
measurement part**: the home clauses name none. It holds no figure.

**The syntax half** (decision D10) is the grammar and every early error either engine's parser reports at parse time
at the heads the correspondence names, the engine-only rows of kind Grammar and the contract's two limits among them;
the stage table assigns every catalogue row to it (`syntax`), to a switch of the request the library applies
(`request`: top-level `await`, which the library applies by reading a top level whose switch is off as a non-async
function's body, so that the row has no diagnostic of its own), to the engine that keeps it (`engine`: every other
engine-only Policy or Limit row, an import attribute the host cannot honour among them, which the library never
reports) or to SJL-15's semantic model
(`semantics`). **"As classified"** (clause 2, and SJL-13.2's "as both engines' baselines classify it")
is the classification: for every input of the parse corpus, under each of its variants and each configuration below,
the expected outcome at the syntax half, the catalogue rows whose shared codes may come first when it is refused, and
the reason - the test262 file's metadata, a `semantics` row that the syntax half leaves to the model, an engine
baseline, or a correspondence row that corrects one engine.

**The two configurations** (decision D26). **V**: every proposal flag off, no lexer extension, Annex B on for scripts,
HTML-like comments by goal, the nesting parameter at Broiler.VM's default, top-level `await` on for the Module goal
as Broiler.VM's module options set it and never set for the Script goal - Broiler.VM's request. **J**: the proposal
flags of the forms Broiler.JS parses today on, one flag per form (decorators, auto-accessors and the import phases'
call forms `import.source(…)` and `import.defer(…)`; the import phases' declaration forms off: decision D17), the
`m` extension, the nesting parameter at the library's supported maximum, top-level `await` on for the Module goal and
off for the Script goal, as Broiler.JS's module and script options set them (the switch applies to whichever goal a
request sets it for: decision D10) - Broiler.JS's request. In both, the regular-expression provider answers as that
configuration's engine validator answered each input, as part A's configuration says (decision D26). Eval-family
inputs are also run as eval requests under E0 to E7's flags in configuration V.

**The commits, named before they exist.**
- **The rule commit** carries this file.
- **The vectors commit** is the default-branch merge that adds the `syntax` area to `conformance/vectors/` at its
  corpus revision. It descends from the rule commit, and it is an ancestor of the first merge that adds a file under
  `Broiler.ECMAScript/Broiler.ECMAScript.Frontend/`.
- **The library commit** is the default-branch merge that completes SJL-13.2 (its done-when), named in bundle
  `sjl-13-001`. Every part is run there, from a clean checkout.

A part run at any other commit, or whose vectors commit fails those conditions, is void.

## Part A - the parse corpus and test262's syntax tests at library level, as classified

```rule
evidence class: conformance (not a measurement)
population: every input of conformance/parse/corpus.manifest as the rule commit holds it, under its goal and each of its variants, in configurations V and J; the eval-family inputs also as eval requests under E0 to E7 in configuration V; the test262 inputs read from test262 at ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e and each checked against the manifest's SHA-256, the others from conformance/parse/inputs/
configuration: the library's corpus runner (Broiler.ECMAScript.Frontend.Tests' corpus mode, or docs/evidence/sjl-13-001/tools/frontend-corpus) at the library commit, built in Release, on one thread of the stack the library documents for its tree-depth bound, no LANG and TZ=UTC; for each input, variant and configuration: the outcome (accepted, refused or crashed), and for a refusal the first diagnostic's shared code, phase and span, and for a refusal at either limit, nesting or tree depth, the depth the library reports; in configuration V and in configuration J the runner's regular-expression provider answers as that configuration's engine validator answered the input in its SJL-12 after baseline, as the classification records it (decision D26): it refuses a literal, naming the first row the classification names that the stage table bases on the provider, exactly when the classification expects the input refused in that configuration with such a row among the rows it names first, and admits every other literal; a json variant is read under the Script goal, as SJL-12's tool read it
completeness: every input, variant and configuration reported; the manifest's digest and the classification's equal to the rule commit's
controls: a copy of the library commit's report with one outcome flipped and one first code changed to a code no row allows, which the comparison must report as two unexplained inputs; retained
comparison: by a script committed to this bundle's tools/ after this file and before any run of this part is taken, which reads the runner's report in the fields this part's configuration lists, the classification and the catalogue's shared codes, and holds no expectation of its own; per input, variant and configuration: the outcome against the classification's expectation, and for a refusal the first code against the classification's rows
admitted outcomes, each by the classification and listed by reason in the bundle: an expected acceptance accepted; an expected refusal refused with a first code of the rows it names; an input the classification marks semantics-only accepted or refused; and L, an input the classification expects accepted that is refused at a limit, with the contract's nesting diagnostic (EX0011's shared code) at a reported depth past the nesting parameter the configuration sets, or with its tree-depth diagnostic (EX0012's shared code) at a reported depth past the library's tree-depth constant (decision D14)
every other outcome is a failure: an input crashed; an expected acceptance refused outside L; an expected refusal accepted unless semantics-only; a refusal whose first code no row of the classification allows
MET if and only if the run is complete, the control is reported, and every input in both configurations has an admitted outcome.
NOT MET otherwise.
```

**How it is read.** The classification was fixed before the library existed, so the library cannot shape its own
expectations. Its comparison script, which reads what the runner writes and so is committed once the runner's report
exists, reads its expectations from the classification and the catalogue alone, so it cannot move one either. A
failure is a library defect, fixed before the library commit, or a classification error, which only a new dated rule
file may correct, quoting this one. The per-reason lists and their tallies stay in the bundle.

## Part B - the syntax vectors at library level (template T3)

```rule
evidence class: conformance (not a measurement)
population: every vector of the syntax area at the vectors commit's corpus revision
configuration: the library-level runner, through its frontend adapter, at the library commit, Release: each vector's in parsed under the goal its op names (ParseScript, or eval code under PerformEval), with the library's default request; error: SyntaxError read as a refusal and any other outcome as an acceptance; a divergence vector compared with its out, and an impl-defined vector with its out unless it names the library
controls: a copy of one vector with its expected outcome changed to the opposite, which the runner must report; retained
MET if and only if every vector's outcome at library level is its expectation, and the control is reported.
NOT MET otherwise.
```

## Part C - determinism (layer L13 and rule SL9)

```rule
evidence class: determinism (not a measurement)
population: rule SL9's, as registered: the whole vector corpus, read and validated afresh each time, through the library-level runner, syntax vectors included; and every input and variant of part A's population in configuration V
configuration: rule SL9's test at the library commit; and the corpus runner's dump mode, which writes per input the SHA-256 of a canonical dump of the tree (each node's kind, span, raw facts and children in tree order), the side tables in their own order, and the ordered diagnostics; in three processes - run 1 in the manifest's order, run 2 in the reverse order, and run 3 on two threads, each input parsed twice concurrently, once on each
controls: two patches to scratch copies of the library commit, never merged, retained with their output: K1 a process-wide counter written into a node's raw fact - run 2 must differ from run 1, and rule SL2's witness must refuse it; K2 a side table filled from a HashSet<string>, whose order follows the process's string-hash seed - run 1 against run 2 must differ, and rule SL9 must fail; each reverted, and the revert checked
admitted difference classes: none
MET if and only if rule SL9 passes, every input gives one hash in all four parses, and both controls behave as stated and agree again after their revert.
NOT MET otherwise.
```

## Part D - the Native AOT probe (layer L10)

```rule
evidence class: publish and run (not a measurement)
population: Broiler.ECMAScript.Probe at the library commit, which references the frontend and runs the syntax vectors
configuration: the probe published with Native AOT and with JIT, with Broiler.VM's root settings (TrimMode full, warnings as errors), by .github/workflows/shared-libraries.yml at the library commit, and the same locally on linux-x64; each output retained
MET if and only if both publications succeed with no trim, AOT or single-file warning, and the two runs' answers are identical, line for line, and pass.
NOT MET otherwise.
```

## Part E - the shared libraries' rules (rule runs)

```rule
evidence class: rules (not a comparison)
population: rules SL1 to SL10 of Broiler.ECMAScript/Broiler.ECMAScript.Architecture.Tests/rules.register.json with their witnesses, SL1's layering row for Broiler.ECMAScript.Frontend (Algorithms and Broiler.Unicode.Algorithms), the library's PublicAPI.txt, package validation, and the assurance check over assurance.config.json's projects
configuration: dotnet test of the architecture tests in Release at the library commit; the API baseline and package validation by eng/pack.ps1 -ProjectSet Shared; the assurance check as the shared workflow runs it; logs retained
MET if and only if every rule passes with the frontend in scope, every witness is refused, the public API equals PublicAPI.txt in both directions, package validation passes, and the assurance check passes.
NOT MET otherwise.
```

## Part F - fuzzing at library level (layer L9)

```rule
evidence class: robustness (not a measurement)
population: the seeds docs/evidence/sjl-13-001/inputs/fuzz-seeds.txt names, each with its goal, its configuration and its count of mutants, and the time bound per parse that file states
configuration: the library's fuzz target at the library commit, Release, on the documented stack; each mutant parsed under its request
reading: a parse returns a tree or diagnostics; a finding is an exception escaping the library, a lost process, a parse past the time bound, or a diagnostic with no shared code of the catalogue
controls: a scratch copy in which the parser throws on one production the seeds reach; the target must report a finding; reverted, and the revert checked
MET if and only if no finding is reported and the control is reported as stated.
NOT MET otherwise.
```

## Part G - the limits (the contract's "limits checked before the stack is")

```rule
evidence class: robustness (not a measurement)
population: the deep-nesting corpus docs/evidence/sjl-13-001/inputs/deep/ as committed with this rule: for each of nesting through brackets, parentheses, blocks, functions, templates and their substitutions, and the four left-operand shapes of row R14 of the correspondence in Broiler.VM's bundle sjl-13-001 (a binary chain whose first operand is a parenthesised chain, a template whose substitution holds one, a binary chain whose first operand is a member or call chain, and a member or call chain whose object is a parenthesised binary chain), sources at the bound, one past it, and far past it, each with its expected outcome under the parameters it names
configuration: the corpus runner at the library commit, on the stack the library documents, each source parsed alone in its own process; and the library's derivation of the tree-depth constant, as JSD-0022 derived Broiler.VM's, retained with its measured frames per level and its margin
MET if and only if every source at or below its bound is accepted, every source past it is refused with its kind's diagnostic, EX0011's shared code for a nesting source and EX0012's for a tree-depth source, no process ends without its exit code, and the derivation admits the constant the library states on the stack it documents.
NOT MET otherwise.
```

## Part H - the contract's checklist (clause 1; record consistency)

```rule
evidence class: record consistency (not a comparison)
population: Broiler.ECMAScript.Frontend's PublicAPI.txt and its project file at the library commit; JSD-0080's output table, request, providers and limits as Broiler.VM's commit the bundle names holds them; the stage table
configuration: a script committed to this bundle's tools/ after this file and before any run of this part is taken, run at the library commit, that lists for each row of JSD-0080's output table and each request, provider and limit the contract names the public member that carries it, or "absent"; lists every public type of the library whose namespace, base type, field or parameter names a type of Broiler.VM or Broiler.JS (none is expected); and lists every engine row of the stage table for which the library has a diagnostic producer (none is expected)
controls: a scratch copy of the library commit with one contract member removed from PublicAPI.txt and the code, and one with a public stub type declared in a namespace of the script's engine-namespace list (Broiler.VM., Broiler.JavaScript.); the script must report both
MET if and only if every row and item the contract names has a member, except the caller's lexical names, which the script lists as absent with decision D16's reason, no public type names an engine type, no engine row has a producer, and both controls are reported.
NOT MET otherwise.
```

## Revisions

A rule revised after any run it judges has been seen is a new dated file in this directory. It quotes this one whole
and says it was written afterwards, and its decision is then weaker evidence.

## What this file does not decide

- **Broiler.VM's clauses** (4 to 10): its bundles `sjl-13-001` and `-002` decide them.
- **Broiler.JS's front end**, which SJL-14 changes, and this repository's bundle `sjl-14-001`.
- **The semantic model**, SJL-15's.
- **A speed claim of any kind.** The library is not measured here.
