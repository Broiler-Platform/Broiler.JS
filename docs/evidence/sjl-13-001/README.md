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
  (`request`), the engine that keeps it (`engine`) or SJL-15's model (`semantics`) (decision D10), all three as they stand in
  `684667e20ab73de68364eb0289dfc59c818e95a3`, which this change merges unchanged (the stage table and the classification
  were first committed in `70bc4899f3e578cdf2759d7519e64c384ab67ba4`; `684667e2` fills the goal rows'
  compiler-only and lazy cells and three Broiler.VM site cells, and the classification differs only in the stage
  table's digest);
- [`inputs/`](inputs/README.md): part F's seeds and part G's deep-nesting corpus, written by
  [`scripts/frontend/write_home_inputs.py`](../../../scripts/frontend/write_home_inputs.py), whose `check` mode
  rebuilds them byte for byte.

**The pins.** ECMA-262's ES2026 edition at `tc39/ecma262@0248456c`; test262 `ccaac100`; the early-error catalogue
`Broiler.ECMAScript/docs/frontend/early-errors.tsv`, SHA-256
`7a9109b0db685df98a7327cd95b9337cc538337f834fe4f22692b249058aa4f1`; the parse corpus's manifest
`conformance/parse/corpus.manifest`, SHA-256 `1c5342104766f14d0c17d950537e4ed078a0c438c3652f8c65bff8c6c347e9cc`; the
stage table, SHA-256 `3c9c49d97162650a48acfeec33b3d1d084dddda6bf649ad648c2e5ff1f1ce6a6`, which the classification's
table records.

**Owner:** the Broiler.JS maintainer. One person holds every role (the programme's EX-30, recorded in Broiler.VM).
**Reviewer:** none, and nothing here has been read by anyone but its author.

## The `syntax` vectors and both engines' first runs, 2026-10-27

Corpus revision 29, `a532f41866a7ac080020d8b3c0c8c38907f9c546`, adds the `syntax` area: 518 vectors in six files in
[`conformance/vectors/syntax/`](../../../conformance/vectors/README.md), written by
[`scripts/conformance/generate_syntax_vectors.py`](../../../scripts/conformance/generate_syntax_vectors.py) under
Broiler.VM's JSD-0083, decision D25, with schema 1 unchanged. Revision 30,
`535b9e3469a0d74a0c23244e3271c7853785f00c`, classifies every difference of both engines' first runs on it but one,
corrects the vectors those runs and review showed to be defective, and adds 31 regular-expression delimiting vectors
*(revised 2026-10-27, after review; read "Revision 30, `27139f9e52fe68b86d25c590c6c3b6ac2052f062`, classifies every
difference of both engines' first runs on it but one, and corrects the vectors those runs showed to be defective.":
`27139f9e` was revision 30 before review, and `535b9e34` corrects it in place before its merge)*. These are the runs [`SharedLibraries.md`](../../roadmap/SharedLibraries.md)'s "The work, in order",
item 2, asks for, the basis of revision 30's `divergence` fields. They are no part of the rule's parts A to H: part B reads the same vectors at library level,
at the library commit. Nothing here is a verdict. [`engine/commands.txt`](engine/commands.txt) records how each run was
taken, and every run here was taken on one host, Ubuntu 24.04.5 LTS (Linux 6.18.44, x86_64, 4 logical CPUs).

**The vectors.** Each pins one source text's parse: `op` is `ParseScript` (the source as a Script) or `PerformEval`
(the source as the eval code of a top-level direct eval in a fresh realm); `in` holds the source; `script` holds it as
the one-line Script the engine-level runners evaluate, or a top-level direct `eval` of it; `out` is `error:
SyntaxError` or the completion value. Every expected value is the pinned edition's, ECMA-262 ES2026 at `0248456c`, but
those of the 10 explicit-resource-management vectors, which are ECMA-262 main's at `4feb0272` (the pinned edition has no
UsingDeclaration); and at revision 30, 549 vectors, 172 of the 230 refusals and 34 of the 319 acceptances also cite
a test262 test at `ccaac100` that asserts the outcome *(revised 2026-10-27, after review; read "Every expected value is
the pinned edition's, ECMA-262 ES2026 at `0248456c`, and 177 of the 230 refusals and 34 of the 288 acceptances also
cite the test262 tests at `ccaac100` that assert the outcome.", the counts of revision 29's 518 vectors, some of whose
citations asserted another source or rule)*. Of the stage table's 211 `syntax` rows, 145 have an accepted and a refused vector, 11 one half and 55 none, and
the generator's `UNCOVERED` table names each missing half with its reason (the Module goal, the regular-expression
provider, the proposal flags, the dynamic-function and ShadowRealm goals, the bounds, and the explicit-resource-
management halves no citable source asserts; decision D25 leaves out only Module-goal and switch-dependent rows, and
`REVISIONS.md`'s row 29 names where the table goes beyond it). The 13 `impl-defined` vectors are Annex B's, whose outcome holds only
with it; both engines' requests turn it on for scripts, and so does the library's (the design document's request).
Every vector was checked against the reference Node.js v26.12.0 that bundle `sjl-9-001`'s `reference.txt` names: it
agrees but on one, `syntax/annex-b/eg0008/0002`, a call as an assignment target in strict code, which the
specification refuses and the reference parses, throwing a ReferenceError when the assignment runs; the vector follows
the specification.

**This engine's runs** of `Broiler.JS/Broiler.JavaScript.Vectors.Tests` are in [`engine/`](engine/). Each was built
once in Release with `--disable-build-servers`, in a scratch tree written by `git archive` of the corpus commit, and
run with `--no-build` in one evaluating process, with no `LANG`, under `TZ=UTC` and under `TZ=America/New_York`. No
commit of this change touches a path under `Broiler.JS/`: the engine is the branch base's, `238069ab`. The two zones'
reports are byte for byte the same.

**Broiler.VM** was run locally at `faa8af78cddc32d460a7299f088b18b9e4407d7a` (Broiler-Platform/Broiler.VM#172, the
SJL-13.1 records), its `VectorsCommand` unchanged. Its conformance root was built in Release with
`--disable-build-servers` from a scratch `git archive` of that commit, and run on a scratch archive of each revision
made as Broiler.VM's `src/tests/vectors/pins/README.md` says, with a scratch pin of that archive (`--vectors <archive>
--expect <pin>`). The runs and the scratch pins are retained as `engine/broiler-vm-*`, as the basis of the fields;
they are not a reading of Broiler.VM's clauses, which its own bundles judge.

| Revision | Engine | Corpus commit | Vectors | Match | Classified divergences | Unclassified | Not implemented |
|---|---|---|---|---|---|---|---|
| 29 | Broiler.JS | `a532f418` | 107,326 | 105,525, 11 of them divergences that no longer hold here | 1,748 (the Intl area's 1,744 and the number area's 4) | 53, all in the syntax area | 0 |
| 29 | Broiler.VM | `a532f418` | 107,326 | 107,263 | 41 (the Intl area's) | 22, all in the syntax area | 0 |
| 30 | Broiler.JS | `535b9e34` | 107,357 | 105,554, 11 of them divergences that no longer hold here (the Intl area's, as at revision 29) | 1,802 (the syntax area's 54, the Intl area's 1,744 and the number area's 4) | 1, `syntax/lexical/asi/0009` (below) | 0 |
| 30 | Broiler.VM | `535b9e34` | 107,357 | 107,294 | 63 (the syntax area's 22 and the Intl area's 41) | 0 | 0 |

*(Revised 2026-10-27, after review: the revision 30 rows were the runs on `27139f9e`, revision 30 before review, which
the runs on `535b9e34` replace. They read "| 30 | Broiler.JS | `27139f9e` | 107,326 | 105,525, 11 of them divergences
that no longer hold here (the Intl area's, as at revision 29) | 1,800 (the syntax area's 52, the Intl area's 1,744 and
the number area's 4) | 1, `syntax/lexical/asi/0009` (below) | 0 |" and "| 30 | Broiler.VM | `27139f9e` | 107,326 |
107,264 | 62 (the syntax area's 21 and the Intl area's 41) | 0 | 0 |".)*

Each Broiler.JS run's negative control changes `numbers/to-fixed/0001` in a copy of the corpus, and each Broiler.VM
run's changes `syntax/scripts/ex0008/0001` in memory (`--mutate`); each is reported as the one more, unclassified,
difference. The files are `run-revision-<N>-utc.log` and `.tsv.gz`, `run-revision-<N>-new-york.log` and `.tsv.gz`,
`negative-control-revision-<N>-utc.tsv` and `-new-york.tsv`, `broiler-vm-run-revision-<N>.log` and `.tsv.gz`,
`broiler-vm-negative-control-revision-<N>.log` and `.tsv.gz` and `broiler-vm-revision-<N>.pin`, for N 29 and 30. Each
log was written whole by the run's script: a header that names the commits, the build, the corpus and its pin, the
host and the command, the runner's output, and a footer with the exit code and the end time.

**Revision 30** classifies the differences under the correspondence's rows (Broiler.VM's
`docs/evidence/sjl-13-001/correspondence.md`). The reasons are in
[`scripts/conformance/syntax-divergences.txt`](../../../scripts/conformance/syntax-divergences.txt). Each names the row
whose answer the engine gives in the difference, which the vector's note names, the engine's answer as that row
records it, the class JSD-0083 gives the row, and the record that answers it.

| Engine | Rows (vectors) | Vectors |
|---|---|---|
| Broiler.JS | D39 (6); T12, T16, D42, P15a (3 each); D33, D35, D38, P18, P31, T18, T24b, T24c, T24e, T28 (2 each); D34, D36, N26, P09, P10, P11, P12, P13, P14, P22, P23, P24, T21, T24a, T24d, T24f (1 each) | 54 |
| Broiler.VM | T13 (4); T16 (3); T6, T8, D32, P15b, P31, T24e (2 each); T7, T24d, T24f (1 each) | 22 |

*(Revised 2026-10-27, after review, with P14 and the second T24e for Broiler.JS and the second T24e for Broiler.VM,
from the vectors revision 30 rewrote and added; the totals read 52 and 21.)*

- **Broiler.JS.** Every row is a correction of Broiler.JS that SJL-14, its cut-over, answers (JSD-0083, decision
  D45), or of both engines (P31, T16, T24d to T24f), but T18, the `m` suffix, which JSD-0083 classes engine policy
  (the lexer-extension parameter) and decision D25 keeps a `divergence` field; this engine's record is decision
  SJL-D-17 of [`SharedLibraries.md`](../../roadmap/SharedLibraries.md), under which the extension stays here as a
  parameter of the shared lexer. Two rows hold their difference with a reading the run adds to: T24b and T24f record
  a regular expression where the specification reads a division, refused at compilation when an operand follows it;
  on the vectors' sources, where `;` or nothing follows the literal, the program completes with the left operand's
  value. Their reasons say so. That reading matches the rows' mechanism and class, not their recorded answer, and it
  stands until the plan's owner accepts it or the correspondence records a literal followed by `;` or the end of input
  *(added 2026-10-27, after review)*. The vectors of P31 render `abort:` (System.NotImplementedException), as P31
  records. Two rows the vectors name do not reach this engine's correction, and the runs show it refusing both, as the
  specification does: T23's vector, `syntax/expressions/ee0012/0003`, an escaped `let` bound in strict code (T23's
  sites are reached by the escaped `yield` and `await` of D33's two vectors), and, at revision 29 and before review,
  P14's, `switch => 1;` at a statement's start; revision 30 rewrites it to row P14's input, `var f = switch => 1;`, which
  this engine accepts *(added 2026-10-27, after review)*.
- **Broiler.VM.** Every row is a correction that decision D25 lists, with P15b, which step 1 of SJL-13.3 makes
  (JSD-0083, "The corrections step 1 makes"). No difference is a refusal of the wide manifest. 7 vectors differ in
  both engines (P31, T16, T24d to T24f) *(revised 2026-10-27, after review; read "6 vectors")*.
- **One difference is left unclassified.** Broiler.JS accepts `syntax/lexical/asi/0009`, `for (var i = 0` LF `i < 1;
  i++) ;`, which the specification refuses (12.10.1: no semicolon is inserted where it would become one of the two
  semicolons of a for statement's head) and the reference refuses at parse time. The correspondence's row for
  automatic semicolon insertion, P16, records this engine's answer as the specification's and classes the row the
  same, from test262's `test/language/asi` tests, none of which puts a `var` declaration before the line break. So
  the difference contradicts P16's class, and no row of the correspondence holds it; none is made here. It is
  reported for the correspondence to answer, and until it is, this engine's run on revision 30 reports it.
- **A vector defect.** Broiler.VM's 22nd difference on revision 29, `syntax/lexical/regexp-delimiting/0044`, renders
  `/[/]/.source` as `"[\/]"`. EscapeRegExpPattern leaves the form of `source` to the implementation ("S may or may
  not differ from P"), so that answer is as conforming as the expected `"[/]"`. Revision 30 rewrites the 33 vectors
  whose expected value read a regular expression's `source` or its string form to read an outcome the specification
  fixes (`test`, `exec`, `lastIndex`), keeping each one's id, row, class and what it delimits; they were checked again
  against the reference and on this engine, which answers each as at revision 29.
- **What review corrected** *(added 2026-10-27)*. `syntax/lexical/t8/0001`, `${typeof /}/}`, held only with Annex
  B's ExtendedPatternCharacter (`}` is a SyntaxCharacter of the main pattern grammar) and was classed `normative`;
  revision 30 rewrites it to `${typeof /[}]/}`, whose `}` is a ClassAtom, and Broiler.VM still refuses it under T8
  (it accepts `${typeof /[a]/}` and `${(/[}]/).test("}")}`, which places the refusal at the guess after `typeof`).
  `syntax/lexical/asi/0010` becomes `var f = ()` LF `=> 1`, which row EE0025 decides; `syntax/scripts/grammar/0004`
  becomes `var f = switch => 1;`, which Broiler.JS accepts as P14 records. 13 test262 citations change or go where
  the test asserted another source or rule, a note keeps its comma, and 31 vectors delimit a regular expression after
  every punctuator revision 29 left out; of them, `regexp-delimiting/0083`, a prefix `--`, differs in both engines
  under T24e. `REVISIONS.md`'s rows 29 and 30 list each change.

**On revision 30** both engines were run again, as for revision 29, from scratch archives of `535b9e34`, revision 30
as corrected after review (Broiler.JS built again in a scratch tree of that commit), and the runs replace those on
`27139f9e` *(revised 2026-10-27, after review; the paragraph read "both engines were run again, as for revision 29,
from scratch archives of `27139f9e` (Broiler.JS built again in a scratch tree of that commit).", and went on as it
does)*. Broiler.VM classifies every difference, and no divergence field reports that it no longer holds. Broiler.JS
classifies every difference but `syntax/lexical/asi/0009`, which revision 30 leaves unclassified, so this engine's
corpus test fails on revision 30 until the correspondence answers it; its two zones' reports are byte for byte the
same again. Each of the 33 vectors rewritten for `source` gives each engine the verdict its revision 29 form gave, but
`regexp-delimiting/0044`, which now matches in Broiler.VM; the two that differ (`t8/0002` in Broiler.VM,
`regexp-delimiting/0048` in Broiler.JS) are classified under T8 and T24c. Of the vectors review rewrote and added,
`t8/0001` differs in Broiler.VM (T8), `grammar/0004` in Broiler.JS (P14) and `regexp-delimiting/0083` in both (T24e),
each classified, and every other matches in both. Each negative control is reported as the one more difference it
should be. The runs read the corpus at `535b9e34`; no later commit of this change touches `conformance/vectors/`, so
the corpus at the branch's head is the one they read *(added 2026-10-27, after review: the runs on `27139f9e` read a
corpus that a later commit's rewording of `REVISIONS.md`'s row 29 changed)*.

## State

**2026-10-26, at SJL-13.1: rules only. Nothing here is a verdict, and nothing has been taken.**

*2026-10-27, at the `syntax` vectors:* they are written, corpus revisions 29 and 30, and both engines' first
engine-level runs on them are taken, and classified but one difference (the section above) *(revised 2026-10-27,
after review; read "are taken and classified (the section above)")*. None of those runs is a run of the rule's
parts, and nothing here is a verdict; the line above stands for the rule's parts.

- The vectors commit adds the `syntax` area to [`conformance/vectors/`](../../../conformance/vectors/README.md) at its
  corpus revision, before the first file under `Broiler.ECMAScript/Broiler.ECMAScript.Frontend/` (the rule's
  conditions).
- SJL-13.2 builds the library in stages, one merge each; the merge that completes it (its done-when) is the library
  commit, where every part is run from a clean checkout.
- **Part A's comparison script and part H's record-consistency script** are committed to this bundle's `tools/`
  after the rule commit and before any run of their parts is taken, as the rule's parts A and H say, and each is
  retained with its output. Each reads what the library writes: part A's the corpus runner's report, in the fields
  part A's configuration lists, with the classification and the catalogue, and no expectation of its own; part H's the
  library's `PublicAPI.txt`. Part A's corpus runner, its dump mode for part C and part F's fuzz target are the
  library's, written by SJL-13.2 (stages 4a and 4e).

**The commits named so far:**
- **The classification's commit**, `684667e20ab73de68364eb0289dfc59c818e95a3`, whose stage table and classification revise those first
  committed in `70bc4899f3e578cdf2759d7519e64c384ab67ba4`.
- **The rule commit** carries [`decision-rule.md`](decision-rule.md), this README and [`inputs/`](inputs/README.md).
  It is named here, by all forty digits, when the change that carries it has merged.

The `syntax` vectors' commits on this change's branch are named in the section above: revision 29,
`a532f41866a7ac080020d8b3c0c8c38907f9c546`, and revision 30, `27139f9e52fe68b86d25c590c6c3b6ac2052f062`, as corrected
after review by `535b9e3469a0d74a0c23244e3271c7853785f00c` *(the last clause added 2026-10-27)*. The vectors
commit, the default-branch merge that carries them, is named here, by all forty digits, when that change has merged.
No other commit is named yet. *(Revised 2026-10-27, at the `syntax` vectors; read "No other commit is named yet.")*

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
- `tools/`: the scripts parts A and H name, and part A's corpus runner if it is kept here;
- `engine/`: both engines' engine-level runs on the `syntax` vectors, the basis of revision 30's fields, which no part
  reads *(added 2026-10-27, at the `syntax` vectors; the item before ended with a full stop)*.

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
runs. *2026-10-27, at the `syntax` vectors:* it holds Broiler.VM's own engine-level runs on the `syntax` vectors, taken here from a
scratch archive of `faa8af78`, as the basis of revision 30's `divergence.broiler-vm` fields, and not as a reading of
any clause of Broiler.VM's.
