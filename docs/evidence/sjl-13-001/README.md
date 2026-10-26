# Evidence bundle SJL-13-001 (Broiler.JS) - the shared syntax library in its home

**Milestone:** SJL-13 of the shared JavaScript libraries programme,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-13-shared-syntax-and-vm.md`: its home clauses 1 ("The
frontend's syntax half is implemented under the contract and the conventions."), 2 ("The parse corpus and test262's
syntax tests pass at library level, as classified.") and 3 ("Rule SL9 (determinism) and the Native AOT probe pass.").

**Rules only. Nothing here is a verdict.**

**Rule:** [`decision-rule.md`](decision-rule.md), committed at SJL-13.1 before any code of the library exists:
- part A is the parse corpus and test262's syntax tests at library level, against the classification, in the
  configurations V (Broiler.VM's request) and J (Broiler.JS's);
- part B is the `syntax` vectors at library level (template T3);
- part C is determinism (layer L13 and rule SL9);
- part D is the Native AOT probe (layer L10);
- part E is the shared libraries' rules SL1 to SL10, the API baseline, package validation and the assurance check;
- part F is fuzzing at library level (layer L9), over the seeds in [`inputs/`](inputs/README.md);
- part G is the contract's limits, over the deep-nesting corpus in [`inputs/`](inputs/README.md);
- part H is the contract's checklist, a record-consistency check of the library against the contract.

Clause 1 is read from parts H and E, with part G for the contract's limits; clause 2 from parts A and B; clause 3 from
parts C and D. There is no base and no measurement part: the library is new, and every part is judged at one commit,
the library commit.

**Area verdict:** [`SharedLibraries.md`](../../roadmap/SharedLibraries.md), "Area verdicts", "The frontend's syntax
half (SJL-13.1)", with Broiler.VM's JSD-0083,
`Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0083-the-shared-syntax-library-and-the-wide-path.md`,
and its correspondence from code, retained in Broiler.VM's bundle of the same name
(`Broiler.VM/docs/evidence/sjl-13-001/correspondence.md`, with every probe and each engine's answers). The home's
design document is [`Broiler.ECMAScript/docs/frontend/design.md`](../../../Broiler.ECMAScript/docs/frontend/design.md),
and this repository's record of the library's work is [`SharedLibraries.md`](../../roadmap/SharedLibraries.md), "The
shared syntax library (SJL-13)".

**Committed with the rule:**
- [`classification/`](classification/README.md): the parse corpus, classified at the syntax half for every input,
  variant and configuration, and for every eval request, which part A reads. It was written on 2026-10-26 by
  [`scripts/frontend/classify_parse_corpus.py`](../../../scripts/frontend/classify_parse_corpus.py) from the stage table
  [`Broiler.ECMAScript/docs/frontend/stages.tsv`](../../../Broiler.ECMAScript/docs/frontend/stages.tsv), which assigns
  every row of the early-error catalogue to the syntax half (`syntax`), a switch of the request the library applies
  (`request`), the engine that keeps it (`engine`) or SJL-15's model (`semantics`) (decision D10), all three in
  `70bc4899f3e578cdf2759d7519e64c384ab67ba4`, which this change merges unchanged;
- [`inputs/`](inputs/README.md): part F's seeds and part G's deep-nesting corpus, written by
  [`scripts/frontend/write_home_inputs.py`](../../../scripts/frontend/write_home_inputs.py), whose `check` mode
  rebuilds them byte for byte.

**The pins.** ECMA-262's ES2026 edition at `tc39/ecma262@0248456c`; test262 `ccaac100`; the early-error catalogue
`Broiler.ECMAScript/docs/frontend/early-errors.tsv`, SHA-256
`7a9109b0db685df98a7327cd95b9337cc538337f834fe4f22692b249058aa4f1`; the parse corpus's manifest
`conformance/parse/corpus.manifest`, SHA-256 `1c5342104766f14d0c17d950537e4ed078a0c438c3652f8c65bff8c6c347e9cc`; the
stage table, SHA-256 `4b9552bc91c4abb07550ea2e91b20ed8e59950c15147260ae358bea5726eaa2c`, which the classification's
table records.

**Owner:** the Broiler.JS maintainer. One person holds every role (the programme's EX-30, recorded in Broiler.VM).
**Reviewer:** none, and nothing here has been read by anyone but its author.

## State

**2026-10-26, at SJL-13.1: rules only. Nothing here is a verdict, and nothing has been taken.**
- The vectors commit adds the `syntax` area to [`conformance/vectors/`](../../../conformance/vectors/README.md) at its
  corpus revision, before the first file under `Broiler.ECMAScript/Broiler.ECMAScript.Frontend/` (the rule's
  conditions).
- SJL-13.2 builds the library in stages, one merge each; the merge that completes it (its done-when) is the library
  commit, where every part is run from a clean checkout.
- **Two readings, stated before any run.** Part A's comparison script and part H's record-consistency script are each
  "a script committed to the bundle with this file": they are committed to this bundle before the library commit's
  runs are taken, and each is retained with its output; neither exists at the rule commit, since each reads what the
  library writes (part A the corpus runner's report, part H the library's `PublicAPI.txt`). Part A's corpus runner, its
  dump mode for part C and part F's fuzz target are the library's, written by SJL-13.2 (stages 4a and 4e).

**The commits named so far:**
- **The classification's commit**, `70bc4899f3e578cdf2759d7519e64c384ab67ba4`.
- **The rule commit** carries [`decision-rule.md`](decision-rule.md), this README and [`inputs/`](inputs/README.md).
  It is named here, by all forty digits, when the change that carries it has merged.

No other commit is named yet.

**What the bundle will hold:**
- `corpus/`: part A's report in both configurations, the comparison's output with every admitted outcome listed by
  reason, and its control;
- `vectors/`: part B's run of the `syntax` vectors and its control;
- `determinism/`: part C's three runs, rule SL9's log, and controls K1 and K2 with their reverts;
- `probe/`: part D's two publications and runs, and the shared workflow's run at the library commit;
- `rules/`: part E's logs;
- `fuzz/`: part F's findings, or none, and its control;
- `limits/`: part G's runs over the deep-nesting corpus and the tree-depth constant's derivation;
- `contract/`: part H's listing and its two controls;
- `tools/`: the scripts the two readings above name, and part A's corpus runner if it is kept here.

**It holds no figure until a run is taken**, and then only in the run's own files and this README's reading of them.
A test262 suite total is never quoted (rule H5): part A is decided per input, variant and configuration, and totals
decide nothing.

**Exclusions.** None.

| Input or part | Excluded | Why |
|---|---|---|

The owner's direction of 2026-10-18, "Please do all recommended decisions and skip waiting for review.", under which
JSD-0083 records its decisions, changes no rule: nothing is accepted without review, and SJL-13 may not meet its gate
before SJL-12, SJL-3 and SJL-7 meet theirs.

Broiler.VM judges its clauses 4 to 10 in its own bundles `sjl-13-001` and `sjl-13-002`. This bundle reads none of their
runs.
