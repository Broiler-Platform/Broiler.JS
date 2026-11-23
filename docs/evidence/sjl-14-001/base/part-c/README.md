# Part C at the base commit: every difference classified, or found unclassified

**Runs:** the base commit `69a8a13bc446a1714d3c8be9be3cfd742aebee6e` (the merge of Broiler-Platform/Broiler.JS#1128),
taken 2026-11-22 to 2026-11-23 during W2's hold and retained in [`../local/`](../local/README.md). **Classified:**
2026-11-23, on branch `sjl-14-w2-base`, by the scripts in [`scripts/`](scripts/), from the retained runs alone: nothing
was built, run or dispatched for it. [`../../decision-rule.md`](../../decision-rule.md) is not edited, and no row is
added to [`../../corrections.md`](../../corrections.md) or its tables.

## What part C asks

Part C reads, in its configuration, "at the base commit (W2's first parent, the last state before the cut-over, every
dark commit included), one build in Release: the side-by-side tool docs/evidence/sjl-14-001/tools/side-by-side at entry
points P (the old parser), B (the bridge) and K (the whole compile, the switch off and on), with the canonical dump of
bundle sjl-12-001's tool applied to P's and B's trees; every test project with the switch on through its internal
options member; scripts/compliance/run_test262.py locally on the programme's machine through the shell built at that
commit with its hidden switch off and on, in UTC, with negative tests, one worker per core"; its comparison "per input
and variant, P against B and K-off against K-on: outcome, exception type, message and dump hash; per test, its result
off and on; per test262 path and variant, its verdict off and on"; its classes "part A's C1 (by K1 to K10), C3 and T; E,
by this file's header; and M ... and S ..."; "every other difference is unclassified"; and "MET if and only if every run
at the base commit is complete, and every difference is in C1, C3, E, T, M or S and is listed."

**The corrections it classifies by** are [`../../corrections.md`](../../corrections.md)'s K1 to K10 as the base commit
holds them. Neither `corrections.md` nor `corrections/` has changed since `232230dc` (2026-11-03, rows W1a to W1c), before
the first part C base step (2026-11-22T14:51:15Z, [`../local/driver.log`](../local/driver.log)), and this branch adds
none: reading B9's taken bullet allowed K10 rows "before the first part C base run", and that moment has passed. A
difference no class admits is listed below as unclassified.

## What is here

Each written by [`scripts/read_base.sh`](scripts/read_base.sh), run twice through the machine's lock and gate with the
same bytes both times ([`logs/`](logs/runs.tsv)):
- [`counts.txt`](counts.txt), [`messages-p-b.tsv.gz`](messages-p-b.tsv.gz), [`messages-k.tsv.gz`](messages-k.tsv.gz):
  the tool's `compare.py`, as at W1 (the pairs' counts; class M's generated message tables, decision D46, with both
  texts); [`text-run-identity.txt`](text-run-identity.txt): the `--text` run's P and B rows are the first run's, byte
  for byte.
- [`differences.tsv.gz`](differences.tsv.gz): every difference of the tool's run the comparison names, one row each,
  with its class or "unclassified" and its basis, by [`scripts/classify_c.py`](scripts/classify_c.py) (whose docstring
  states each class as it reads it); [`span-readings.tsv.gz`](span-readings.tsv.gz): every span line `spancheck.py`
  listed `both-agree`, with its field, its parent node and the reading that decides it; [`classes.txt`](classes.txt):
  the counts; [`unclassified.tsv`](unclassified.tsv): every unclassified difference with both sides.
- [`tests-and-test262/`](tests-and-test262/counts.txt): the local runs' per-test and per-case tables, by W1's
  `read_local.py` unchanged ([`../../side-by-side/w1/tests-and-test262/scripts/read_local.py`](../../side-by-side/w1/tests-and-test262/scripts/read_local.py)),
  and their classes, by [`scripts/classify_local.py`](scripts/classify_local.py)
  ([`tests-and-test262/local-differences.tsv`](tests-and-test262/local-differences.tsv),
  [`tests-and-test262/classes.txt`](tests-and-test262/classes.txt)).
- [`scripts/readings.tsv`](scripts/readings.tsv): the readings by hand the classifier uses, each the row whose
  production an input's refused construct is, with that construct.

[`hashes.txt`](hashes.txt) gives the SHA-256 of every file here. Each count is in these files, not in this README. The
classifier's earlier runs while it was written, over the same inputs, are not retained; the two runs in `logs/` are of
the scripts as they are here.

## How each class is read

- **Outcome moves** (P/B and K/K-on) are read by the classification bundle `sjl-13-001` made for configuration J,
  through [`../../corrections/test262-paths.tsv`](../../corrections/test262-paths.tsv) for a test262 input, and, for any
  other input, by the same reading of the classification that file makes ("a K when the input's basis in the
  classification names a row of `k-rows.tsv`", `corrections.md`), with P and K taken at the base. A move to the outcome
  it expects that a row of K1 to K10 explains is C1; a refusal for nesting past the library's bound is C3; a move it
  explains as class E's (E, E-if-coded, E-partly, or the correspondence's D31 bucket, "same in outcome, Broiler.JS
  refusing only in its compiler"), with K refusing on both sides, is E when the row of the library's code qualifies by
  the rule's header. **The library's code is not in the run**: the reading aid meant to give it failed at the base
  ([`../local/retained.txt`](../local/retained.txt)). The row is read from B's text instead: the catalogue rows whose
  message identifier renders it, narrowed to those the classification lists first when any are. When every such row
  qualifies, the move is E; when only some do, the row the input's construct matches is read by hand
  ([`scripts/readings.tsv`](scripts/readings.tsv)); otherwise it is unclassified. This is a reader's reading, weaker
  than the code would be.
- **Message differences** of a refusal with the same type are M, each in the generated tables. No exception type
  differs at either pair; the switch-on compile's crashes that the switch-off compile shares have the same type and
  message.
- **Tree differences.** The inputs whose trees differ outside spans are exactly those W1a to W1c cite: C1 (K10). Every
  other tree difference is of spans alone; `spancheck.py` reads most lines S and lists the rest `both-agree`, which its
  check cannot decide. Each `both-agree` line is read here from its kind, its field and both texts (the readings are
  named in `classify_c.py`'s docstring): P's span ending at the end-of-input token where B's ends at the node's last
  token; a declaration in a `for` head whose P span takes the head's `;`; P's span beginning inside the node's first
  operand (after `new X`, `[` or `{`); P's span taking punctuation or a comment that follows the node; a `break` or
  `continue` whose P span leaves out its own `;`; and a member, optional chain or import call whose end the AST type
  takes from a child whose own line is one of those. In each, P's span disagrees with the node's source text and B's
  agrees, so each line is S, and no `both-agree` line is left undecided.
- **Tests** that pass off and fail on, refused for nesting past the library's bound, are C3; they are the tests
  `corrections/k-rows.tsv`'s row R12 names ("class C3 or revised at SJL-14"). **test262 cases** that fail off and pass
  on, whose path `corrections/k-paths.tsv` lists for a row of K1 to K10, are C1.

## The open items W1's run left, at the base

- **`J000557`, `J001072` and `J001780`** move as at W1, P and K-off accepting and B and K-on refusing. The
  classification expects each refused in configuration J and names its correspondence row: `J000557` P13 ("~ is unary
  only"), which is K8's, and `J001072` and `J001780` D37 ("yield and await as names, strict reserved words, a static
  block's await"), which is K7's. Each is C1.
- **The outcome moves the corrections' tables do not list** (inputs outside test262, which `test262-paths.tsv` does not
  hold) are each C1, C3 or E by the classification, but for those listed unclassified below.
- **The `both-agree` spans** are each S, by the readings above.
- **W1's unlisted test262 moves** appear at the base as at W1: both variants of
  `test/staging/sm/regress/regress-610026.js` pass off and fail on, and the Terser variant of
  `test/language/expressions/dynamic-import/syntax/valid/new-covered-expression-is-valid.js` passes off and is skipped
  on. Both are unclassified below.

## Unclassified

Every one is in [`unclassified.tsv`](unclassified.tsv) or [`tests-and-test262/local-differences.tsv`](tests-and-test262/local-differences.tsv)
with both sides. With each, the reading of its cause, which is a reading and not a finding:
1. **A getter with a parameter**, P accepting, B refusing with "Expected )", K refusing on both sides (with the switch
   off, "Getter must not have any formal parameters"): `T035220` (`test/language/expressions/class/getter-param-dflt.js`),
   `T039055` (`test/language/expressions/object/getter-param-dflt.js`), `T047023`
   (`test/language/statements/class/getter-param-dflt.js`), `J000058` and `J001174`, each in both variants. B's text is
   the missing-token row EX0009's; the classification lists the unexpected-token row EX0008 first, so the header's class
   E does not admit it. The library refuses as the grammar requires; the bridge carries its refusal. Not a bridge defect
   and not the harness: the code the library gives is not the one the classification expects first, which is the
   library's or the classification's to answer.
2. **An arrow function as a class heritage that names a private name**, P accepting, B refusing with "Unexpected token
   (", K refusing on both sides ("Private name is not declared in an enclosing class"): `T034966`, `T034969`, `T046767`
   and `T046770` (`test/language/{expressions,statements}/class/elements/syntax/early-errors/grammar-private-environment-on-class-heritage-{array,obj}-literal.js`),
   each in both variants. B's text renders the unexpected-token row (or an identifier the bridge's table does not name);
   the classification lists EE0087, EE0155 and EX0009 first. The same reading as 1.
3. **A parenthesized literal as an assignment target**, P accepting, B refusing with "Invalid left-hand side in
   assignment", K refusing on both sides ("Invalid parenthesized destructuring pattern"): `J000040`
   (`([a, b]) = [1, 2]`) and `J001147` (`({a, b}) = { a: 1, b: 2 }`), each in both variants. By the construct the row is
   EE0033, which Broiler.JS checks partly in its parser and which the classification does not list first (it lists the
   cover-grammar rows), so class E does not admit it. The same reading as 1.
4. **The architecture test `BridgeSwitchTests.The_compilers_test_only_switch_is_off_in_this_build`** passes off and
   fails on, by its own message: the switch-on pass builds with the switch on, which this test exists to refuse. The
   harness's, not the product's: the test checks the build's configuration. W2 deletes the switch.
5. **`test/language/statements/for-of/head-await-using-bound-names-in-stmt.js`, original variant**, fails off ("This
   statement should not be evaluated") and passes on. It has C1's form, but no row of K1 to K10 lists it:
   `test262-paths.tsv` explains the path by the correspondence's D31 bucket, and `k-paths.tsv` does not list it. Part A's
   C1 line reads "a C1 row that none explains is recorded as a finding against the correspondence". The library refuses
   as the text requires; the gap is the correspondence's listing. The stage table's rows for the early errors the
   classification lists first (EE0051, EE0056) name D35 and D36, which are K7's rows; that is a reading only, and no
   class is taken from it.
6. **`test/staging/sm/regress/regress-610026.js`, both variants**, pass off and fail on, with no output on either stream,
   taking longer than with the switch off and ending near the runner's bound, though not recorded as timed out. The test
   evaluates a source of empty blocks it builds by doubling, and then longer ones. The retained records do not show the
   cause: a slower or heavier parse through the bridge for that source (the bridge's or the library's), or the runner's
   handling of a process near its bound (the harness's). W1's run shows the same move.
7. **`test/language/expressions/dynamic-import/syntax/valid/new-covered-expression-is-valid.js`, Terser variant**,
   passes off and is skipped on: with the switch on, the shell refuses the minified body with a SyntaxError, and the
   runner, finding that node cannot parse it either, skips the case as "minifier-invalid-output". The harness's: the
   minifier's output is source no engine accepts, which the switch-off shell accepted.

## Part C's reading

**Read NOT MET on the retained runs**, for the unclassified differences above. Every run at the base commit is complete
(the tool's report holds every input, variant and entry; both test runs ran every test project; both test262 runs hold
every shard, merged, with the re-run), and every other difference is in C1, C3, E, M or S and is listed. Not judged
beyond that (EX-30). Part C's "How it is read" reads "An unclassified difference is a bridge or library defect, fixed as
a dark merge and run again before W2 merges"; what follows from each item above is the orchestrator's and the owner's
to decide, not this record's.
