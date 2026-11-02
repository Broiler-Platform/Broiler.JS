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
`dc9e09e5ae9583552f8f42a3879527fec7396f3d`, classifies every difference of both engines' first runs on it,
corrects the vectors those runs and review showed to be defective, and adds 31 regular-expression delimiting vectors
*(revised 2026-10-27, after review; read "Revision 30, `27139f9e52fe68b86d25c590c6c3b6ac2052f062`, classifies every
difference of both engines' first runs on it but one, and corrects the vectors those runs showed to be defective.":
`27139f9e` was revision 30 before review, and `535b9e34` corrects it in place before its merge)* *(revised again
2026-10-27, under the owner's decisions D-asi and D-t24; read "Revision 30, `535b9e3469a0d74a0c23244e3271c7853785f00c`,
classifies every difference of both engines' first runs on it but one,": `dc9e09e5` completes revision 30 in place
before its merge, and classifies the difference no row of the correspondence held until then)*. These are the runs [`SharedLibraries.md`](../../roadmap/SharedLibraries.md)'s "The work, in order",
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
| 30 | Broiler.JS | `dc9e09e5` | 107,357 | 105,554, 11 of them divergences that no longer hold here (the Intl area's, as at revision 29) | 1,803 (the syntax area's 55, the Intl area's 1,744 and the number area's 4) | 0 | 0 |
| 30 | Broiler.VM | `dc9e09e5` | 107,357 | 107,294 | 63 (the syntax area's 22 and the Intl area's 41) | 0 | 0 |

*(Revised 2026-10-27, after review: the revision 30 rows were the runs on `27139f9e`, revision 30 before review, which
the runs on `535b9e34` replace. They read "| 30 | Broiler.JS | `27139f9e` | 107,326 | 105,525, 11 of them divergences
that no longer hold here (the Intl area's, as at revision 29) | 1,800 (the syntax area's 52, the Intl area's 1,744 and
the number area's 4) | 1, `syntax/lexical/asi/0009` (below) | 0 |" and "| 30 | Broiler.VM | `27139f9e` | 107,326 |
107,264 | 62 (the syntax area's 21 and the Intl area's 41) | 0 | 0 |".)*

*(Revised again 2026-10-27, under the owner's decisions D-asi and D-t24: the revision 30 rows were the runs on
`535b9e34`, which the runs on `dc9e09e5` replace. They read "| 30 | Broiler.JS | `535b9e34` | 107,357 | 105,554, 11 of
them divergences that no longer hold here (the Intl area's, as at revision 29) | 1,802 (the syntax area's 54, the Intl
area's 1,744 and the number area's 4) | 1, `syntax/lexical/asi/0009` (below) | 0 |" and "| 30 | Broiler.VM |
`535b9e34` | 107,357 | 107,294 | 63 (the syntax area's 22 and the Intl area's 41) | 0 | 0 |".)*

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
| Broiler.JS | D39 (6); T12, T16, D42, P15a (3 each); D33, D35, D38, P18, P31, T18, T24b, T24c, T24e, T28 (2 each); D34, D36, N26, P09, P10, P11, P12, P13, P14, P16b, P22, P23, P24, T21, T24a, T24d, T24f (1 each) | 55 |
| Broiler.VM | T13 (4); T16 (3); T6, T8, D32, P15b, P31, T24e (2 each); T7, T24d, T24f (1 each) | 22 |

*(Revised 2026-10-27, after review, with P14 and the second T24e for Broiler.JS and the second T24e for Broiler.VM,
from the vectors revision 30 rewrote and added; the totals read 52 and 21.)* *(Revised again 2026-10-27, decision
D-asi, with P16b for Broiler.JS, `syntax/lexical/asi/0009`; Broiler.JS's total read 54.)*

- **Broiler.JS.** Every row is a correction of Broiler.JS that SJL-14, its cut-over, answers (JSD-0083, decision
  D45), or of both engines (P31, T16, T24d to T24f), but T18, the `m` suffix, which JSD-0083 classes engine policy
  (the lexer-extension parameter) and decision D25 keeps a `divergence` field; this engine's record is decision
  SJL-D-17 of [`SharedLibraries.md`](../../roadmap/SharedLibraries.md), under which the extension stays here as a
  parameter of the shared lexer. Two rows hold their difference with a reading the run adds to: T24b and T24f record
  a regular expression where the specification reads a division, refused at compilation when an operand follows it;
  on the vectors' sources, where `;` or nothing follows the literal, the program completes with the left operand's
  value. Their reasons say so. The owner's decision D-t24 of 2026-10-27 accepts that reading (Broiler.VM's JSD-0083,
  "The owner's decisions of 2026-10-27", taken as recommended under the owner's direction of 2026-10-18, "Please do all
  recommended decisions and skip waiting for review"): both rows record that answer beside the one they held, from a
  probe of both engines, with the same classes (Broiler.VM divides after `}` and refuses after `await`), and the
  reasons `js-t24b`, `js-t24f` and `vm-t24f` quote it *(revised 2026-10-27, decision D-t24; after "Their reasons say
  so." this read "That reading matches the rows' mechanism and class, not their recorded answer, and it stands until
  the plan's owner accepts it or the correspondence records a literal followed by `;` or the end of input (added
  2026-10-27, after review).")*. The vectors of P31 render `abort:` (System.NotImplementedException), as P31
  records. Two rows the vectors name do not reach this engine's correction, and the runs show it refusing both, as the
  specification does: T23's vector, `syntax/expressions/ee0012/0003`, an escaped `let` bound in strict code (T23's
  sites are reached by the escaped `yield` and `await` of D33's two vectors), and, at revision 29 and before review,
  P14's, `switch => 1;` at a statement's start; revision 30 rewrites it to row P14's input, `var f = switch => 1;`, which
  this engine accepts *(added 2026-10-27, after review)*.
- **Broiler.VM.** Every row is a correction that decision D25 lists, with P15b, which step 1 of SJL-13.3 makes
  (JSD-0083, "The corrections step 1 makes"). No difference is a refusal of the wide manifest. 7 vectors differ in
  both engines (P31, T16, T24d to T24f) *(revised 2026-10-27, after review; read "6 vectors")*.
- **The difference no row held, now row P16b's.** Broiler.JS accepts `syntax/lexical/asi/0009`, `for (var i = 0` LF `i
  < 1; i++) ;`, which the specification refuses (12.10.1: no semicolon is inserted where it would become one of the
  two semicolons of a for statement's head) and the reference refuses at parse time. The correspondence's row for
  automatic semicolon insertion, P16, recorded this engine's answer as the specification's and classed the row the
  same, from test262's `test/language/asi` tests, none of which puts a declaration before the line break, so no row
  held the difference. The owner's decision D-asi of 2026-10-27 (JSD-0083, the same section, under the same direction)
  answers it: it is no falsification of JSD-0083 (none of its "Falsified if" clauses), but a gap in the
  correspondence, which narrows P16 by a dated note to automatic semicolon insertion outside a for statement's head
  and adds row P16b, automatic semicolon insertion in a for statement's head, with both engines' answers from code and
  a probe. Broiler.VM refuses a line break where either of the head's semicolons belongs, as the specification does;
  Broiler.JS accepts one after a `var`, `let` or `const` declaration, and the loop runs as though the `;` were
  written, and refuses one after an expression and where the second semicolon belongs. P16b's class is correction of
  Broiler.JS, which SJL-14 answers as decision D45's other corrections of Broiler.JS (K10's rows, hand-off H22).
  Revision 30 at `dc9e09e5` classifies the vector for Broiler.JS under P16b (the reason `js-p16b`), and its note names
  P16b where it named P16. The row is on Broiler.VM's branch `sjl-13-corr-p16`, at
  `6636798fe6ae78d0a082c027c0bc9518c253e711`, which merges into Broiler.VM before this change merges *(revised
  2026-10-27, decision D-asi; the bullet read "One difference is left unclassified. Broiler.JS accepts
  `syntax/lexical/asi/0009`, `for (var i = 0` LF `i < 1; i++) ;`, which the specification refuses (12.10.1: no
  semicolon is inserted where it would become one of the two semicolons of a for statement's head) and the reference
  refuses at parse time. The correspondence's row for automatic semicolon insertion, P16, records this engine's answer
  as the specification's and classes the row the same, from test262's `test/language/asi` tests, none of which puts a
  `var` declaration before the line break. So the difference contradicts P16's class, and no row of the correspondence
  holds it; none is made here. It is reported for the correspondence to answer, and until it is, this engine's run on
  revision 30 reports it.")*.
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

**On revision 30** both engines were run again, as for revision 29, from scratch archives of `dc9e09e5`, revision 30
as corrected after review and completed under the owner's decisions D-asi and D-t24 (Broiler.JS built again in a
scratch tree of that commit), and the runs replace those on `535b9e34`, which had replaced those on `27139f9e`
*(revised 2026-10-27, after review; the paragraph read "both engines were run again, as for revision 29, from scratch
archives of `27139f9e` (Broiler.JS built again in a scratch tree of that commit).", and went on as it does)*
*(revised again 2026-10-27, under decisions D-asi and D-t24; read "from scratch archives of `535b9e34`, revision 30 as
corrected after review (Broiler.JS built again in a scratch tree of that commit), and the runs replace those on
`27139f9e`")*. Broiler.VM classifies every difference, and no divergence field reports that it no longer holds. Broiler.JS
classifies every difference, `syntax/lexical/asi/0009` under row P16b, and its corpus test passes on revision 30; its
two zones' reports are byte for byte the same again *(revised 2026-10-27, decision D-asi; read "Broiler.JS classifies
every difference but `syntax/lexical/asi/0009`, which revision 30 leaves unclassified, so this engine's corpus test
fails on revision 30 until the correspondence answers it;")*. Against the runs on `535b9e34`, each report differs in
one vector's line and its header alone: Broiler.JS's in `asi/0009`'s class and verdict, now a classified divergence,
and in the header's counts; Broiler.VM's, and its negative control's, in `asi/0009`'s class and in the corpus commit
and digest the header names. Broiler.JS's negative controls are byte for byte the same as before. Each of the 33 vectors rewritten for `source` gives each engine the verdict its revision 29 form gave, but
`regexp-delimiting/0044`, which now matches in Broiler.VM; the two that differ (`t8/0002` in Broiler.VM,
`regexp-delimiting/0048` in Broiler.JS) are classified under T8 and T24c. Of the vectors review rewrote and added,
`t8/0001` differs in Broiler.VM (T8), `grammar/0004` in Broiler.JS (P14) and `regexp-delimiting/0083` in both (T24e),
each classified, and every other matches in both. Each negative control is reported as the one more difference it
should be. The runs read the corpus at `dc9e09e5`; no later commit of this change touches `conformance/vectors/`, so
the corpus at the branch's head is the one they read *(added 2026-10-27, after review: the runs on `27139f9e` read a
corpus that a later commit's rewording of `REVISIONS.md`'s row 29 changed)* *(revised 2026-10-27, under decisions
D-asi and D-t24; read "The runs read the corpus at `535b9e34`;")*.

## The rule's parts at stage SJL-13.2f's head, 2026-11-02

SJL-13.2's last stage, 4f (the done-when), took every part of [`decision-rule.md`](decision-rule.md) locally at its
branch's head, `920f18f4579d729819bfcf3c02d18b75c78fd7b5` (branch `sjl-13-2f-done`), from a clean tree written by
`git archive` of that commit, built in Release with `--disable-build-servers`, with no `LANG` and `TZ=UTC`, on one host
(Linux 6.18.44, x86_64, 4 logical CPUs; .NET SDK 10.0.401), on 2026-11-02. The commits after it on the branch add this
section and the runs' files under this bundle and change no other path.

**These runs are not the rule's verdict.** The rule judges every part at **the library commit**, "the default-branch
merge that completes SJL-13.2 (its done-when)", run there from a clean checkout, and "a part run at any other commit ...
is void". The library commit is this stage's merge into the default branch: it does not exist yet, and this README names
it by all forty digits when it lands, as the bundle has named each commit by its merge. Each part is then taken again at
it. What follows is each part as its text reads the runs at the branch's head; nothing here is a verdict.

| Part | Configuration as run | Outcome at the head, by the rule's text |
|---|---|---|
| A, the parse corpus and test262's syntax tests as classified | [`tools/frontend-corpus/`](tools/frontend-corpus/README.md), the library's corpus runner, and [`tools/compare_corpus.py`](tools/compare_corpus.py), both committed in `920f18f4` before any run of this part; every input, variant and configuration (V and J) and every eval request under E0 to E7 in V, test262 at `ccaac100` checked against the manifest's SHA-256; the provider answering from the classification (decision D26) | **Not met.** The run is complete (no input absent or crashed; the manifest's and the classification's digests equal the rule commit's, which the comparison pins) and the control is reported (two more failures); but 1,922 inputs, variants and configurations have no admitted outcome (below). *(Revised 2026-11-02, after review; it read "the manifest's and the classification's digests equal the rule commit's) ... but 1,916 inputs": the comparison first committed admitted L at a reported depth equal to the bound, and compared the classification with the report's record of it, not with the rule commit's; both are corrected, and the comparison is taken again over the same report, below)* |
| B, the `syntax` vectors (template T3) | the Native AOT probe under the JIT, whose library-level runner parses each vector through the frontend adapter under the library's default request; the vectors at corpus revision 34, whose `syntax` area is the vectors commit's (`ed494b2e`, unchanged since) | **Met**: every one of the 549 vectors agrees at the parse; the control (`syntax/statements/ee0040/0002`'s expected refusal changed to an acceptance in a copy of the corpus) is reported as the one failure |
| C, determinism (L13, SL9) | rule SL9's test (the architecture tests); the probe's dump mode `--digest-corpus` in three processes: run 1 in the manifest's order, run 2 reversed, run 3 on two threads, each parse twice at once; controls K1 and K2 as scratch patches | **Not met.** SL9 passes, and every input gives one hash in all four parses (the three files are the same bytes); K1 behaves as stated (run 2 differs from run 1 in 194,361 lines; SL2 refuses the patched library) and agrees again after its revert; **K2 does not**: run 2 differs from run 1 in 132,685 lines, but rule SL9 passes, so "rule SL9 must fail" does not hold. SL9 compares its runs inside one process, where a string-hash seed is one, over the vector corpus, whose `syntax` vectors read an outcome and no side table; a patch of this kind cannot make it fail. K2 agrees again after its revert |
| D, the Native AOT probe (L10) | `dotnet publish -c Release -r linux-x64` with Broiler.VM's root settings, and the JIT build, each running the vector corpus | **Not met**, as configured: the Native AOT publication and the JIT build succeed with no warning, both runs pass and their reports are the same bytes, but the configuration's run by `.github/workflows/shared-libraries.yml` at the library commit is not taken (it is the merge's, an orchestration step), and at this head the JIT publication was not taken: the JIT run is the solution build's output, as the shared workflow's is. *(Revised 2026-11-02, after review; it read "both local publications succeed with no warning", where one publication and one build were taken. The JIT publication is taken after review, with the fix, in the section below)* |
| E, the shared libraries' rules | the architecture tests in Release (SL1 to SL10 with their witnesses, the API baseline in both directions, the register); package validation by a bash rendering of `eng/pack.ps1 -ProjectSet Shared`; the assurance check as the shared workflow runs it | **Met**: 32 of 32 architecture tests pass, every witness refused, the frontend in scope of every rule and of SL1's row; four packages packed and validated at `0.1.0-preview.9`, `Broiler.ECMAScript.Frontend` among them; "no violations" |
| F, fuzzing (L9) | the probe's `--fuzz` over [`inputs/fuzz-seeds.txt`](inputs/fuzz-seeds.txt) (548 lines, 64 mutants each, 2,000 ms a parse) on a thread of `ParseLimits.StackBytes`, under the JIT and as the Native AOT image; the control a scratch copy whose parser throws on a ConditionalExpression | **Met**: no finding under either runtime; the control reports 238 findings, and the reverted copy's run is the run's, line for line |
| G, the limits | [`tools/deep_nesting.py`](tools/deep_nesting.py) `run` over [`inputs/deep/`](inputs/deep/manifest.tsv), each of the 54 sources alone in its own process, under the JIT and as the image; the derivation, [`Broiler.ECMAScript/docs/frontend/limits/derivation.md`](../../../Broiler.ECMAScript/docs/frontend/limits/derivation.md), with its frames per level measured again five ways (`limits/frames-*.tsv`) | **Met**: 54 of 54 rows hold under both runtimes, every exit its row's; the measurement gives the costliest construct at 512 levels the same stack as the derivation states (its section 4's margins stand) |
| H, the contract's checklist | [`tools/contract_check.py`](tools/contract_check.py), committed in `920f18f4` before any run of this part, over the library's `PublicAPI.txt`, its project file, `Rows.cs` and the stage table, and JSD-0080 as Broiler.VM `d1c56275aa787f71a1f5ad0fa21e0a8cc3b07207` holds it (`contract/jsd-0080.sha256`); two scratch controls | **Met**: every row and item of JSD-0080's input and output tables, limits and providers has a member, but the caller's lexical names, listed absent with decision D16's reason; no public type or reference names an engine; none of the stage table's 17 `engine` rows has a producer; control 1 (`ParseLimits.DiagnosticCap` removed) and control 2 (a public `Broiler.VM.Witness.EngineStub`) are each reported |

Read by clause, as the rule reads them: clause 1 (parts H and E, with G) would read met at the head; clause 2 (A and B)
and clause 3 (C and D) would not.

**Part A's failures, by cause** (`corpus/failure-groups.tsv` groups every one by reason, configuration, the rows the
classification names and the row of the library's first code; `corpus/comparison.tsv.gz` lists every input, variant and
configuration with its reason, and `corpus/comparison-summary.txt` the tallies by reason):
- **710 regular-expression literals whose refusal the classification records under rows the stage table does not base
  on the provider**, so that the provider, which the rule has refuse a literal only "naming the first row the
  classification names that the stage table bases on the provider", admits them and the library accepts: in
  configuration J, 255 test262 inputs (510 variants) whose rows are Broiler.JS's scanner codes alone (EX0001 or
  EX0008), and in both configurations 50 inputs of Broiler.JS's corpus (200 variants), regular-expression literals both
  engines' validators refuse, whose rows are every `syntax` row of Broiler.VM's first registry code, 2101. A
  classification of the provider's answer, which only a new dated rule file may correct.
- **26 refusals the library does not make**: an `import.meta`, a `this` or a parenthesised non-target as a `for`-`in`,
  `for`-`of` or `for await` head's target (T037840 to T037842, T048863, T048869, T049556, T049562), and a module that
  exports a name it does not declare (T041636, T041638). Library defects, fixed before the library commit.
- **1,180 refusals whose first code is not one the classification's rows allow**: the library refuses where the
  classification expects it to, but with another row's code, most often EX0001's where it names EX0009 or EX0008, EX0009's
  where it names EX0008, and EX0008's where it names a cover's rows (EE0087, EE0155); the groups name each pair. Each is
  a library defect (a site that reports the wrong row) or a classification error, decided per group.
- **6 refusals at a limit reported at the bound, not past it**: in configuration J, J000243 and J000799 refused with
  JSE9201 at 512, the nesting parameter, and J000451 with JSE9202 at 10,000, the tree-depth constant, strict and sloppy,
  all of Broiler.JS's corpus. Part A admits L "at a reported depth past" the bound; the library reports the bound it
  refused past (the diagnostic's argument), not the depth it reached, so as the rule is written no refusal at a limit
  can be admitted. Before the library commit, either the runner reports the depth the parse reached (the library
  refuses a level of nesting, or a chain node's height, one past its bound), or a new dated rule file reads the
  reported bound as the rule's "past"; until one of them, these six stand as failures.

*(Revised 2026-11-02, after review; the paragraph that stood here read "Part A's admitted outcomes include L in
configuration J (two inputs refused at the nesting parameter and one at the tree-depth constant, each reported with its
bound), all of them in Broiler.JS's corpus.", counting the six as admitted by a reading of "past" as "equal to the bound
refused past", which the rule's text does not give. The corrected comparison, [`tools/compare_corpus.py`](tools/compare_corpus.py)
as revised, taken again over the same report, counts them as failures: `corpus/comparison.tsv.gz`,
`corpus/comparison-summary.txt`, `corpus/failure-groups.tsv` and `corpus/control/` are its output, 1,922 failures and
the control's 1,924.)*

**What stage 4f changed that the runs read.** The tree-depth charge of every shape of the tree (the owner's decision of
2026-11-02 on decision D14; the derivation's section 5): the probe's report of the vector corpus at the head is the same
bytes as at `origin/main` (`6d9ded70`) (`probe/sha256.txt`), and the dump of every one of the parse corpus's 295,795
parses in configuration V is the same as stage 4e's (`determinism/stage-4e-6d9ded70.sha256` and its log: run 1 of
the dump mode taken the same way with the probe built at `6d9ded70`, the same SHA-256 as this head's run 1): no parse of
the corpus in configuration V moves, and part A's refusal of J000451 in configuration J is of a chain past the constant,
which stage 4e refused too. *(Revised 2026-11-02, after review; it read "is the same as stage 4e's (its verifier's
dump)", which the bundle did not retain, and "part A's L in configuration J is a chain past the constant".)*

**The files.** `corpus/` (part A: the runner's report, the comparison, its control), `vectors/` (B), `determinism/` (C:
run 1's dump, the three runs' digests and logs, SL2 and SL9 from the architecture tests, and K1 and K2 each with its
patch, its runs' digests, the first lines where they differ, the rule its patch must fail and its revert), `probe/` (D),
`rules/` (E: the architecture tests, the assurance check, the pack and the pristine consumer), `fuzz/` (F, with the
control's findings by name), `limits/` (G), `contract/` (H), and `review/` (the checks taken again after review, the
section below). The dump files of run 2, run 3 and the controls' runs are not kept, only their SHA-256: run 2's and run
3's are run 1's bytes. *(Noted 2026-11-02, after review: for K1 and K2 this departs from part C's "retained with their
output": each control keeps its runs' SHA-256 and the first lines where they differ, not its runs' dumps, which were not
kept and cannot be retaken at this head as they were. At the library commit each control's runs are retained whole,
gzipped as `determinism/run-1.tsv.gz` is.)*

### After review, 2026-11-02: the charge by height alone, and the checks it touches

*(Added 2026-11-02, at SJL-13.2f, after review.)* The review of the branch found that the tree-depth charge as first made
refused trees within the constant (a chain began at the depth the parse had reached, so an argument, a key or a right
operand beside a deep left operand was charged both), and stage 4e's charge some too. Commit
`25678837cab780edf4cb166b40eacdf9c3e01b56` refuses a chain node by its height alone (the derivation's section 5, revised
the same day), so the commits after `920f18f4` change the library's parser and tests as well as this bundle: the
sentence above, "The commits after it on the branch add this section and the runs' files under this bundle and change no
other path", no longer holds. The parts taken at `920f18f4` stand as they were taken, none a verdict; the checks the
change touches were taken again at `25678837`, from the branch's worktree (uncommitted edits to records only), built in
Release with `--disable-build-servers`, no `LANG`, `TZ=UTC`, and retained in [`review/`](review/):

| Check | What it gives at `25678837` |
|---|---|
| The solution filter's build and every test project | no warning; every test passes, the Frontend's 946 among them (`review/build.txt`, `review/tests.txt`) |
| The probe over the vector corpus (part B's and D's runs) | the JIT build, the Native AOT image and, taken now, the JIT publication (`dotnet publish -c Release -p:PublishAot=false`), each with no warning and exit 0; the three reports are the same bytes, and the same as at `origin/main` and at `920f18f4` (`review/probe-sha256.txt` against `probe/sha256.txt`) |
| Part A's runner, and the comparison as revised | the report is the same as at `920f18f4` but its commit line (`review/report.tsv.gz`); the comparison over it gives the same tallies as over `920f18f4`'s, 1,922 failures (`review/comparison-summary.txt`) |
| Part C's dump mode, run 1 | the same SHA-256 as at `920f18f4` and at stage 4e (`review/digest-sha256.txt`, `determinism/sha256.txt`, `determinism/stage-4e-6d9ded70.sha256`): no parse of the corpus moves in configuration V |
| Part G's runner over the deep-nesting corpus | 54 of 54 rows hold under the JIT and as the image, every line as at `920f18f4` (`review/deep-*.tsv`) |
| The first tier's frames per level | every row the same frames; every nesting row a little more stack left, none less (`review/frames-jit-first-tier.tsv` against `limits/frames-jit-first-tier.tsv`) |
| `--parse-one` over the derivation's probes and the review's sources | stage 4e's seven probes past the constant refused, its control refused; the sources within the constant that stage 4e or the first charge refused accepted, and one past them refused (`review/probes.tsv`, at stage 4e, at `197b204b` and at `25678837`; the sources from `review/probe-sources.py`) |
| Part F's fuzzing, under the JIT and as the image | no finding under either runtime; the two tallies are the same, and differ from `920f18f4`'s in four mutants, each refused at the tree-depth constant there and by another row now (in J one by JSE9108 and two by JSE9109, in V one by JSE9201): the first charge refused a summed depth before the parse reached that row's error, and the charge by height refuses only a chain node, once built, whose height passes the constant (`review/fuzz-*.txt` against `fuzz/fuzz-*.txt`) |
| Packing, the pristine consumer, the assurance check | four packages validated at `0.1.0-preview.9`; the consumer passes; the assurance check (Broiler.Code `afd4ffb2`, from a folder holding it and this tree side by side) finds no violations (`review/pack.log`, `review/pristine.log`, `review/assurance-check.log`) |

Not taken again: part C's runs 2 and 3 and its controls, part F's control, and part H's check, which reads the public
API, the project file, `Rows.cs` and the stage table, none of which the change moves; part E's architecture tests ran
with every test project.

## State

**2026-10-26, at SJL-13.1: rules only. Nothing here is a verdict, and nothing has been taken.**

*2026-10-27, at the `syntax` vectors:* they are written, corpus revisions 29 and 30, and both engines' first
engine-level runs on them are taken and classified (the section above) *(revised 2026-10-27, after review; read "are
taken and classified (the section above)")* *(revised again 2026-10-27, decision D-asi; read "are taken, and
classified but one difference (the section above)")*. None of those runs is a run of the rule's
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
  library's, written by SJL-13.2 (stages 4a and 4e). *(Noted 2026-11-02, at SJL-13.2f: "Part A's corpus runner, its dump
  mode for part C and part F's fuzz target are the library's, written by SJL-13.2 (stages 4a and 4e)" held of part C's
  dump mode and part F's fuzz target, the probe's, and not of part A's runner, which no stage had written: stage 4f
  commits it, [`tools/frontend-corpus/`](tools/frontend-corpus/README.md), the second place the rule names, with part A's
  comparison, [`tools/compare_corpus.py`](tools/compare_corpus.py), and part H's check,
  [`tools/contract_check.py`](tools/contract_check.py), in `920f18f4`, before any run of either part.)* *(2026-11-02, at SJL-13.2e:* part G's runner,
  [`tools/deep_nesting.py`](tools/deep_nesting.py), is committed here before any run of part G: it starts one process
  per row of [`inputs/deep/manifest.tsv`](inputs/deep/manifest.tsv), the Native AOT probe's `--parse-one` mode under the
  JIT or as its image, and reads each process's exit code; its `stack` mode measures the least stack each source needs,
  for the tree-depth derivation, retained in
  [`Broiler.ECMAScript/docs/frontend/limits/`](../../../Broiler.ECMAScript/docs/frontend/limits/derivation.md). The
  dump mode part C reads is the probe's `--digest-corpus`, and part F's fuzz target the probe's `--fuzz`; both are the
  library's stage 4e's, in `Broiler.ECMAScript/Broiler.ECMAScript.Probe/`. Nothing run with them at stage 4e is a run of
  a part: the parts are run at the library commit.)*

**The commits named so far:**
- **The classification's commit**, `684667e20ab73de68364eb0289dfc59c818e95a3`, whose stage table and classification revise those first
  committed in `70bc4899f3e578cdf2759d7519e64c384ab67ba4`.
- **The rule commit** carries [`decision-rule.md`](decision-rule.md), this README and [`inputs/`](inputs/README.md).
  It is named here, by all forty digits, when the change that carries it has merged.

The `syntax` vectors' commits on this change's branch are named in the section above: revision 29,
`a532f41866a7ac080020d8b3c0c8c38907f9c546`, and revision 30, `27139f9e52fe68b86d25c590c6c3b6ac2052f062`, as corrected
after review by `535b9e3469a0d74a0c23244e3271c7853785f00c` *(the last clause added 2026-10-27)* and completed under
the owner's decisions D-asi and D-t24 by `dc9e09e5ae9583552f8f42a3879527fec7396f3d` *(this clause added 2026-10-27)*. The vectors
commit, the default-branch merge that carries them, is named here, by all forty digits, when that change has merged.
No other commit is named yet. *(Revised 2026-10-27, at the `syntax` vectors; read "No other commit is named yet.")* *(Added 2026-11-02, at SJL-13.2f: **the library commit** is
the default-branch merge of SJL-13.2f, the done-when, branch `sjl-13-2f-done`; it is named here by all forty digits when
that merge lands, and every part is taken there. The parts were taken at the branch's head before it, at
`920f18f4579d729819bfcf3c02d18b75c78fd7b5` (the section "The rule's parts at stage SJL-13.2f's head"), which the rule
counts as no run of a part.)* *(Added 2026-11-02, at SJL-13.2f, after review: the two commits this
section names "when the change has merged" have merged, and are named here. **The rule commit** is
`238069ab8150858a112461b42b95c1f49c1f4eb1`, the merge of Broiler.JS#1081; its `classification/classification.tsv.gz`
has SHA-256 `753b54fee1a530a91d2c58400eb52e700221fddbb4ed9670d4362f6d268a512f` and `conformance/parse/corpus.manifest`
`1c5342104766f14d0c17d950537e4ed078a0c438c3652f8c65bff8c6c347e9cc`, which part A's comparison pins. **The vectors
commit** is `ed494b2e3953b03eae305dc736b8be23dcff3cf9`, the merge of Broiler.JS#1082, which adds the `syntax` area at
its corpus revision. The rule's conditions hold, by `git merge-base --is-ancestor`: the rule commit is an ancestor of
the vectors commit (its first parent), and the vectors commit an ancestor of the first default-branch merge that adds a
file under `Broiler.ECMAScript/Broiler.ECMAScript.Frontend/`, `61cb15b5472955079f3b0bf98c600f4128094614` (Broiler.JS#1085,
SJL-13.2a, whose first such commit is `7b96d43e8cda306afefd0cfd6c0117e82f4b5c0c`); and `git diff` of
`conformance/vectors/syntax/` from the vectors commit to `origin/main` (`6d9ded70`) is empty. The sentence "No other
commit is named yet." above no longer holds: the rule commit and the vectors commit are named here, and the library
commit is named when it lands.)*

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

**It holds no figure until a run is taken**, and then only in the run's own files and this README's reading of them. *(Noted 2026-11-02, at SJL-13.2f: the runs at the branch's head are taken, and the folders above hold them; the
section "The rule's parts at stage SJL-13.2f's head" reads them.)*
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
