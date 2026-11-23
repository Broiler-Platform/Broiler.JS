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
holds them. Neither `corrections.md` nor `corrections/` has changed since `232230dc`
(`232230dcd2160803a3d97123abfb7e856441b442`, 2026-11-03, rows W1a to W1c), before the first part C base step
(2026-11-22T14:51:15Z, [`../local/driver.log`](../local/driver.log)), and this branch adds none: reading B9's taken
bullet allowed K10 rows "before the first part C base run", and that moment has passed. A difference no class admits is
listed below as unclassified. *(Changed the same day, after review, before this record merged: the commit was named
`232230dc` alone.)*

## What is here

Each written by [`scripts/read_base.sh`](scripts/read_base.sh), run twice through the machine's lock and gate with the
same bytes both times ([`logs/`](logs/runs.tsv), runs `w2c-read-base-3` and `w2c-read-base-4`):
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
classifier's earlier runs while it was written, over the same inputs, are not retained; runs `w2c-read-base-3` and
`w2c-read-base-4` in `logs/` are of the scripts as they are here. *(Changed the same day, after review, before this record merged: the sentence before read "the two runs in
`logs/` are of the scripts as they are here". Runs `w2c-read-base-1` and `w2c-read-base-2`, kept in `logs/`, are of
`classify_c.py` as commit `d58b0097bedbed2864c175410cd7b437da49e54a` holds it, which read two span readings as S that
are not (below) and did not mark class E provisional; that commit holds their outputs. `counts.txt`, the message tables,
`text-run-identity.txt` and `tests-and-test262/` are the same bytes in both; `differences.tsv.gz`,
`span-readings.tsv.gz`, `unclassified.tsv` and `classes.txt` are written again.)*

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
  than the code would be. **Every E is therefore provisional**, and its basis in `differences.tsv.gz` says so. Where
  B's text also renders rows that would not qualify, which only the narrowing to the classification's first rows sets
  aside (reading the library's code as one the classification expects first, the very condition the header's
  `partly`/`no` branch tests), the basis names those rows and `classes.txt` counts the moves. Each E is listed with the
  rows B's text gives, not with its one row, and **the unclassified list below is a lower bound**: a move read E here
  may be unclassified by its code. The run that would settle it is the `tools` step's reading aid
  ([`../local/scripts/bridge-codes/`](../local/scripts/bridge-codes/Program.cs)) with the bridge's type looked up in
  the assembly that holds it (`Broiler.JavaScript.Parser`, where `SharedSyntaxBridge` is, not `Broiler.JavaScript.Ast`),
  on a build of the base commit taken behind the gate when the orchestrator decides; E is then read by code, each move
  with its single row. A build is needed, as the base build's `bin` and `obj` trees are deleted
  ([`../local/retained.txt`](../local/retained.txt)). *(Changed the same day, after review, before this record merged: the bullet ended at "weaker than the code would
  be."; the sentences after it are added.)* *(Noted 2026-11-23, records part 4: that run is taken, with the aid
  corrected ([`scripts/bridge-codes/`](scripts/bridge-codes/Program.cs)), and the tables are written again with class E
  read by the library's code: every move read E above holds by its code, and no E is provisional now. "Every E is
  therefore provisional" and "the unclassified list below is a lower bound" read the tables before it; see "Records
  part 4" below.)*
- **Message differences** of a refusal with the same type are M, each in the generated tables. No exception type
  differs at either pair; the switch-on compile's crashes that the switch-off compile shares have the same type and
  message. The rule applies the canonical dump to P's and B's trees only, and no K or K-on row carries a dump, so K-off
  against K-on is compared on outcome, exception type and message: the "same-dump" `counts.txt` gives that pair
  compares no dump. *(Added the same day, after review, before this record merged.)*
- **Tree differences.** The inputs whose trees differ outside spans are exactly those W1a to W1c cite: C1 (K10). Every
  other tree difference is of spans alone; `spancheck.py` reads most lines S and lists the rest `both-agree`, which its
  check cannot decide. Each `both-agree` line is read here from its kind, its field and both texts (the readings are
  named in `classify_c.py`'s docstring). Four readings are S, P's span disagreeing with the node's source text and B's
  agreeing: a declaration in a `for` head whose P span takes the head's `;`; P's span beginning inside the node's first
  operand (after `new X`, `[` or `{`); P's span taking punctuation or a comment that follows the node; and a `break` or
  `continue` whose P span leaves out its own `;`. Two are not S, as P's span agrees with the node's text: P's span
  ending at the end-of-input token where B's gives the same offset as a number, so that both give exactly the node's
  text (`P-eof-same-text`); and a member, optional chain or import call whose end the AST type takes from a child whose
  own P span takes the bracket that follows it, where P's span ends after the node's closing bracket and B's leaves it
  out (`B-short-bracket`). A tree difference with a line of either is unclassified (items 8 and 9 below), and no
  `both-agree` line is left undecided. `spancheck.py`'s own S lines are taken as it reads them: for an edge the AST
  type fixes, it admits a B text that leaves a bracket open or unopened, so in some of those lines B's text is not the
  node's balanced text. *(Changed the same day, after review, before this record merged: from "named in" the bullet read "named in `classify_c.py`'s docstring): P's span
  ending at the end-of-input token where B's ends at the node's last token; [the four readings above]; and a member,
  optional chain or import call whose end the AST type takes from a child whose own line is one of those. In each, P's
  span disagrees with the node's source text and B's agrees, so each line is S, and no `both-agree` line is left
  undecided." Checked against the text, P's span agrees with the node's text in the first and the last of those, so
  neither is S by the rule's words, as W1's README already read a `both-agree` row; the sentence on `spancheck.py`'s
  own S lines is added.)*
- **Tests** that pass off and fail on, refused for nesting past the library's bound, are C3; they are the tests
  `corrections/k-rows.tsv`'s row R12 names ("class C3 or revised at SJL-14"). **test262 cases** that fail off and pass
  on, whose path `corrections/k-paths.tsv` lists for a row of K1 to K10, are C1.

## The open items W1's run left, at the base

- **`J000557`, `J001072` and `J001780`** move as at W1, P and K-off accepting and B and K-on refusing. The
  classification expects each refused in configuration J and names its correspondence row: `J000557` P13 ("~ is unary
  only"), which is K8's, and `J001072` and `J001780` D37 ("yield and await as names, strict reserved words, a static
  block's await"), which is K7's. Each is C1.
- **The outcome moves the corrections' tables do not list** (inputs outside test262, which `test262-paths.tsv` does not
  hold) are each C1, C3 or E (provisional, above) by the classification, but for those listed unclassified below.
- **The `both-agree` spans** are each S by the readings above, but for the lines of the two readings that are not S,
  whose tree differences are unclassified below (items 8 and 9). *(Changed the same day, after review, before this record merged: the bullet read "are each S, by the
  readings above."; and the bullet before read "C1, C3 or E by the classification".)*
- **W1's unlisted test262 moves** appear at the base as at W1: both variants of
  `test/staging/sm/regress/regress-610026.js` pass off and fail on, and the Terser variant of
  `test/language/expressions/dynamic-import/syntax/valid/new-covered-expression-is-valid.js` passes off and is skipped
  on. Both are unclassified below.

## Unclassified

Every one is in [`unclassified.tsv`](unclassified.tsv) or [`tests-and-test262/local-differences.tsv`](tests-and-test262/local-differences.tsv)
with both sides. With each, the reading of its cause, which is a reading and not a finding: *(Noted 2026-11-23,
records part 4: items 1 to 3 are read by the library's code, item 6 by the one case run again, and items 5, 8 and 9 at
their sites, in "Records part 4" below; the items here are left as written.)*
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
6. **`test/staging/sm/regress/regress-610026.js`, both variants**, pass off and fail on, with no output on either stream
   and no reason, taking longer than with the switch off and ending near the runner's bound, though not recorded as
   timed out. In the base commit's `run_test262.py`, a failure with no reason is its last branch: the shell exited
   non-zero with no output; a time-out records "timed out after". The runner also limits each test process's CPU time
   (`RLIMIT_CPU`) to the test's timeout, and a process that limit stops exits on a signal with no output, which that
   branch records as a plain failure; the records do not keep the exit status. The test evaluates a source of empty
   blocks it builds by doubling, and then longer ones. The reading: likely the bridge's or the library's parse of the
   doubled-block source, slower with the switch on and ending near the bound, perhaps at the CPU-time limit; not shown
   by the records. The check that would decide it: the one case run again alone through the switch-on shell, recording
   its exit status or signal, its CPU time and its peak resident memory. W1's run shows the same move. *(Changed the same day, after review, before this record merged: from
   "The test evaluates" the item read "The retained records do not show the cause: a slower or heavier parse through
   the bridge for that source (the bridge's or the library's), or the runner's handling of a process near its bound
   (the harness's).")*
7. **`test/language/expressions/dynamic-import/syntax/valid/new-covered-expression-is-valid.js`, Terser variant**,
   passes off and is skipped on: with the switch on, the shell refuses the minified body with a SyntaxError, and the
   runner, finding that node cannot parse it either, skips the case as "minifier-invalid-output". The harness's: the
   minifier's output is source that node and the switch-on shell refuse and the switch-off shell accepted.
   *(Changed the same day, after review, before this record merged: the last sentence read "the minifier's output is source no engine accepts, which the switch-off shell
   accepted.")*
8. **Tree differences with a line whose P span ends at the end-of-input token** (basis "span lines not S:
   P-eof-same-text" in [`unclassified.tsv`](unclassified.tsv)): every input variant whose dumps differ in a
   `both-agree` line where P's span ends at `eof` and B's at the same offset written as a number, as in `J000001`'s
   last statement (`r.join(',');` in both). Both spans give exactly the node's text, so neither disagrees with it, the
   line is not S, and no other class admits a tree difference. The reading of its cause: the old parser's dump names the
   end-of-input token where the bridge's gives its offset, a difference in how the dump writes the span and not in the
   span; not a bridge or library defect and not the harness. *(Added the same day, after review, before this record
   merged; these were read S, "S-eof".)*
9. **A member, an optional chain or an import call whose B span leaves out the closing bracket**: `T036087`,
   `T039546` and `T051374`, each in both variants (basis "span lines not S: B-short-bracket"). P's span ends after the
   node's `]` (`arr?.[0, 1]`) and B's before it (`arr?.[0, 1`): the AST type takes the node's end from its child, a
   comma expression, whose B span rightly ends before the bracket and whose P span takes it. By the text P agrees and B
   does not, the opposite of S; `spancheck.py` passes B only by its admission for a node whose end the AST type takes
   from a child. The reading of its cause: the span B's tree carries for the node (the bridge's, or the AST type's
   derivation it relies on), not the library's parse and not the harness. *(Added the same day, after review, before
   this record merged; these were read S, "S-derived".)*

## Part C's reading

**Read NOT MET on the retained runs**, for the unclassified differences listed above. Not judged beyond that (EX-30).
*(Noted 2026-11-23, records part 4: class E is now read by the library's code, and the unclassified list is the same
set; the next sentence read the tables before it.)* The list is a lower bound, as class E is read from B's text and is provisional until it is read by the library's code
(above). *(Changed the same day, after review, before this record merged: after "for the unclassified differences above." the paragraph read "Every run at the base commit is
complete (the tool's report holds every input, variant and entry; both test runs ran every test project; both test262
runs hold every shard, merged, with the re-run), and every other difference is in C1, C3, E, M or S and is listed.",
which went beyond the reading the brief asks, and which items 8 and 9 and class E's reading contradict.)* Part C's "How it is read" reads "An unclassified difference is a bridge or library defect, fixed as
a dark merge and run again before W2 merges"; what follows from each item above is the orchestrator's and the owner's
to decide, not this record's.

## Records part 4: the reading aids and single-case re-runs of 2026-11-23

**Written:** 2026-11-23, on branch `sjl-14-w2-base`, with the default branch's head read as
`69a8a13bc446a1714d3c8be9be3cfd742aebee6e` by `git ls-remote` before each step and before the commit. **What these
are:** reading aids and single-case re-runs, not runs of the rule. None replaces a retained run, and each is recorded
as a reading aid, as W1's records name theirs. Nothing here is a verdict: part C's reading above stands as taken,
"read NOT MET on the retained runs". [`../../decision-rule.md`](../../decision-rule.md) is not edited, and no row is
added to [`../../corrections.md`](../../corrections.md) or its tables.

**How they were taken.** Every step by [`aids/scripts/stepD.sh`](aids/scripts/stepD.sh), driven by
[`aids/scripts/driveD.sh`](aids/scripts/driveD.sh) ([`aids/driveD.log`](aids/driveD.log)), each through the machine-wide
lock and idle gate (`run.sh`, as [`../local/scripts/run.sh`](../local/scripts/run.sh)), with the default branch's head
read and the free disk checked before it; each step's command line, exit and machine records are in
[`aids/logs/`](aids/logs/runs.tsv). The base commit's tree is a clean `git archive` of
`69a8a13bc446a1714d3c8be9be3cfd742aebee6e` on disk outside every working tree
([`aids/base/archive.txt`](aids/base/archive.txt)), built as [`../local/scripts/step.sh`](../local/scripts/step.sh)'s
`build-off` builds it, `dotnet build Broiler.JS.slnx -c Release --disable-build-servers`, the switch at its default,
exit 0 with no warning ([`aids/base/build.log`](aids/base/build.log); the shell's binaries' digests in
[`aids/base/shell-binaries.sha256`](aids/base/shell-binaries.sha256)); Terser as `step.sh`'s `terser` step. Every
engine run is under `env -i` with `TZ=UTC` and no `LANG`, `LC_*` or `BROILER_*` variable. The build trees, with their
`bin` and `obj` folders and the aids' build outputs, were removed when the runs ended.

*(Added 2026-11-23, records part 4, after review, before this record merged.)* **Not the same bytes.** The rebuilt
shell's assemblies built from the repository's sources (`BroilerJS.dll`, `Broiler.JavaScript.Parser.dll`,
`Broiler.ECMAScript.Frontend.dll` and every other `Broiler.*` assembly) differ by digest from those of the shell the
retained runs used ([`../local/shell-binaries-off.sha256`](../local/shell-binaries-off.sha256), which every retained
test262 shard's digest file also matches) against [`aids/base/shell-binaries.sha256`](aids/base/shell-binaries.sha256);
only the package assemblies match. The likely cause, not confirmed, is the build tree's path, which is compiled into
them: `/home/user/js-w2baseD-build/69a8a13b/tree` here ([`aids/base/build.log`](aids/base/build.log)) and
`/home/user/js-w2base-build/69a8a13b/tree` for the retained runs ([`../local/build-off.log`](../local/build-off.log)).
The bridge-codes and parse-time aids ran on their own builds of the library from the same sources
([`aids/base/codes-binaries.sha256`](aids/base/codes-binaries.sha256),
[`aids/base/ptime-binaries.sha256`](aids/base/ptime-binaries.sha256)), whose `Broiler.ECMAScript.Frontend.dll`
digests differ again from both shells' and from each other's (their reference assemblies match each other). So the
re-runs and the aids' outputs are read as from the same commit's sources, not from the bytes the retained runs ran.
**The table re-runs** (`d-read-nocodes` and `d-read-codes-1` to `d-read-codes-4`) were not taken by `stepD.sh`, as
the sentence above reads "Every step by", but by [`aids/scripts/readD.sh`](aids/scripts/readD.sh) under the same
`run.sh` ([`aids/logs/commands.log`](aids/logs/commands.log)); `driveD.log` lists only the `d-base-*` and `d-w2-*`
steps.

### Class E by the library's code

- **The aid, corrected.** [`scripts/bridge-codes/`](scripts/bridge-codes/Program.cs) is the `tools` step's reading aid
  ([`../local/scripts/bridge-codes/`](../local/scripts/bridge-codes/Program.cs), retained as it ran) with one change:
  it finds `SharedSyntaxBridge` and `BridgeRequest` in `FastParser`'s assembly, `Broiler.JavaScript.Parser`, where the
  side-by-side tool finds them, not in `FastParseException`'s, `Broiler.JavaScript.Ast`. It builds the bridge's own
  request for each input variant and gives the library's diagnostics in order; the bridge refuses with the first. Run
  over every input variant P or B refused in the base commit's retained side-by-side report
  ([`aids/base/codes-ids.tsv`](aids/base/codes-ids.tsv); the report's SHA-256 in
  [`aids/base/codes-input.sha256`](aids/base/codes-input.sha256),
  `443fbdbf08d0e4f778b91a174f0b8ba1c384fd3df0bf046a02364aabc2808e72`, as retained), step `d-base-codes`, exit 0, every
  listed variant answered ([`aids/base/codes-run.stderr.txt`](aids/base/codes-run.stderr.txt)):
  [`aids/base/bridge-codes.tsv`](aids/base/bridge-codes.tsv),
  `48c299af425bda5a104f74ab84ef1940778afe0a2425b5b7a07a16b2a15c3f43`. The variants of that list the library accepts
  are refusals of the bridge's own (an import attribute key, which P refuses with the same text) and module inputs P
  refuses and B accepts; none is a move to a refusal at B.
- **The tables, written again.** [`scripts/classify_c.py`](scripts/classify_c.py) reads class E by the code when the
  aid's output is given (its docstring, "By code"): the row is the stage table's row whose `shared_code` is the first
  diagnostic's code, and the move is E when that row qualifies by the rule's header as the docstring reads it, else
  unclassified; [`scripts/readings.tsv`](scripts/readings.tsv) is then not read. [`scripts/read_base.sh`](scripts/read_base.sh)
  passes the file as its fifth argument. Without it the scripts write every earlier table byte for byte (run
  `d-read-nocodes`); with it, runs `d-read-codes-1` to `d-read-codes-4` (the last two with the file at its path here;
  the first two with it at `aids/bridge-codes/bridge-codes.tsv`, from where it was moved to `aids/base/` before run
  `d-read-codes-3`; its digest at the first path was not recorded then, and the aid's output in the notes folder from
  which it was copied, and the file here, are `48c299af425bda5a104f74ab84ef1940778afe0a2425b5b7a07a16b2a15c3f43`)
  wrote the same bytes each time, and those are the tables in this folder now. *(Changed the same day, after review,
  before this record merged: the parenthesis read "(the last two with the file at its path here)".)* Only three changed:
  [`differences.tsv.gz`](differences.tsv.gz) (`e918f46254ae5de50a2705927864cd8af311e8c026b5ae30dc23ecb20cfbee0e`; it
  was `b6fcb0389ec57f68987bd97d3895cd846575d2d281b8631ba908e5c5791f6c7d`), [`classes.txt`](classes.txt)
  (`2bb2c4659bf01cbaa383f92f12dfc596540f30b6ec2a6e0d97717ebcffccda76`; it was
  `caa7dee42609fe9c44116b57b4393da16325d0213a4b24ba0b1d0fbed3a1786f`) and [`unclassified.tsv`](unclassified.tsv)
  (`1622df7102aa57fa94730542b05df91843f5c2f8099feeefc8dc26166820194f`; it was
  `afce208f34568ed0d6db4af0667646bb4ccab07a14eb18c7721fddd39d93dc4b`); the earlier bytes are at commit
  `d8760ee7` of this branch. This folder's `hashes.txt` read
  `b9a8fd92c477cb64d7ee91695ba7dc7700b336501bd6acb07cec989dc4660c47` before.
- **What it shows**, read and not judged: every outcome move read E from B's text holds by the library's code,
  including those read E only by narrowing to the classification's first rows; none becomes unclassified, no E is
  provisional, and the unclassified differences are the same set as before (only their bases name the code). Items 1
  to 3 read by code: the getter with a parameter, row EX0009 (code `JSE9109`, `syntax.token.missing`), where the
  classification lists EX0008 first; the arrow function as a class heritage, row EX0008 (`JSE9108`,
  `syntax.token.unexpected`), where it lists EE0087, EE0155 and EX0009; the parenthesized literal as an assignment
  target, row EE0033 (`JSE1329`, `assignment.target.invalid`), `partly`, where it lists the cover-grammar rows. Each
  is the row B's text gave, none is among the rows the classification lists first, and each stays unclassified.
  `classes.txt` also counts the moves whose first code's message identifier is not one B's text renders: each is a
  module input refused for a lexical redeclaration, which the bridge's message table renders in a module as "has
  already been declared" (`BridgeMessages.cs`'s summary); the code is the one the row names.

### `regress-610026.js`, one case

- **The re-run.** `scripts/compliance/run_test262.py` restricted to `test/staging/sm/regress/regress-610026.js` by
  `--path-file`, with the options the shards used otherwise (`--test-timeout-seconds 30 --minifier terser
  --minifier-timeout-seconds 30 --memory-limit-mb 0 --max-workers 4 --include-negative`, both variants), through the
  base build's shell with the hidden switch off (step `d-base-case-off`) and on, `--shell-arg=--parse-through-bridge`
  (step `d-base-case-on`), each exit 0; then once at W2's head `029bab4c4f2457fed1896ab6c4d224350d809b40` (branch
  `sjl-14-w2`, read-only from its worktree), a clean `git archive` built the same way, with no switch argument, as W2
  removes the switch (step `d-w2-case-w2`; [`aids/w2/`](aids/w2/archive.txt)). The runner's own records:
  [`aids/base/case-off/result.json`](aids/base/case-off/result.json)
  (`63b7724fb6d0ad791a5240dc108cfe33c19133f3ff42414debd0ad7a884e068f`),
  [`aids/base/case-on/result.json`](aids/base/case-on/result.json)
  (`7d01f624cd9c3b1bdb4c31ceb223822ca09a8f041db4bc79b01b5794a6afb0d1`) and
  [`aids/w2/case-w2/result.json`](aids/w2/case-w2/result.json)
  (`409f96180839aabb702f8233a5b94167e3143282fe8f967220e999ac73f6638f`). A recording shim found first on `PATH`
  ([`aids/scripts/shim/dotnet`](aids/scripts/shim/dotnet)) ran the real `dotnet` for each test process with the same
  arguments, descriptors and inherited limits, and wrote how it ended, its processor time, its peak resident set, its
  wall time and the CPU limit it ran under ([`aids/base/case-off/shim.log`](aids/base/case-off/shim.log),
  `064591ad9b1b0220361d9b25f5542cfc4c0b7c9eb2b9956a339e5e5d3dabae79`;
  [`aids/base/case-on/shim.log`](aids/base/case-on/shim.log),
  `88b753a29f0af65ea57c77e6522c225606307498384d54d01cd25e831b8ef4b8`;
  [`aids/w2/case-w2/shim.log`](aids/w2/case-w2/shim.log),
  `18f0bd9d16caebd3bf2378532d85a1a662d1ecba78b11299d16908b1a3b75ceb`); `timedv.py` timed the runner
  (`runner.time.txt` beside each).
- **A parse-time aid.** [`scripts/parse-time/`](scripts/parse-time/Program.cs) parses the test's three eval sources,
  built as the test builds them, at the old parser (P), the library alone (L, `SyntaxParser.Parse` of the bridge's own
  request) and the bridge (B, `SharedSyntaxBridge.Parse`: the library's parse with the bridge's work around it, the
  token chain's scan and link (`BridgeTokens`, `tokens.Link()`), the hop to the parse thread and the tree's
  construction), each entry and source in its own process, under the script goal (step `d-base-ptime`, exit 0):
  [`aids/base/ptime.tsv`](aids/base/ptime.tsv),
  `054bec53d5bd1cb7a79d08463c79a235d9d72504176cfea271d3734813ca4ff3`, and each process's own record in
  [`aids/base/ptime.time.txt`](aids/base/ptime.time.txt). The test parses those sources by direct eval inside the case;
  the aid parses them under the script goal (its comment reads the eval goal's inherited context as empty there), each
  in a fresh process. *(Changed the same day, after review, before this record merged: B's parenthesis read "(B, the
  library's parse and the tree's construction)"; the last sentence is added.)*
- **What it shows**, read and not judged: with the switch off both variants pass alone, as in the retained runs. With
  it on, each variant's shell process ends on `SIGXCPU`, the signal of the CPU-time limit (`RLIMIT_CPU`) the runner
  sets to the test's timeout, with no output on either stream, so the runner records a plain failure with no reason,
  the same record the retained runs give (from a rebuilt shell, not the same bytes; above); its peak resident set is
  far above the switch-off run's (the figures are in the files). At W2's head the same
  holds: W2 does not change it. The parse-time aid shows where the processor time goes: the library's parse alone
  takes about what the old parser takes, and the bridge's parse, which adds the bridge's work beyond the library's
  parse, takes markedly more time and memory (the figures are in the files). So the records show **the runner's limit**
  stopping the case, reached because of **the bridge's** cost on these sources (its work beyond the library's parse,
  the token chain and the tree's construction, which the aid does not separate; not the library's parse); not the
  library. The aid parses under the script goal, each source in a fresh process, not inside the case. This is a
  reading of single-case re-runs on an idle machine, not of the shards'
  runs, which ran beside other cases. *(Changed the same day, after review, before this record merged: after "no
  reason," the item read "as the retained runs record it;"; after "the bridge's parse, which adds" it read "the tree's
  construction from the library's tree, takes"; the parenthesis after "on these sources" read "(its tree construction,
  not the library's parse)"; the sentence on the script goal is added.)*

### Items 9 and 5, read at their sites

- **`B-short-bracket`** (item 9): six variants, three inputs. The nodes are an `AstMemberExpression` whose property is
  a comma expression (`a()[0, 1]` inside `import(...)` in `T036087`; `arr?.[0, 1]`, `arr?.[1, 0]` and
  `arr?.[{}, NaN, undefined, 2, 0, 10 / 10]` under an `AstOptionalChain` in `T039546`; `of [0, 1, 2]` as a `for`-`of`
  head's target in `T051374`), and the `AstOptionalChain` and `AstImportCall` that take their end from it; both spans
  and texts are in [`span-readings.tsv.gz`](span-readings.tsv.gz) (P's ends after the `]`, B's before it). B's end is
  set by the AST type, at the base commit `Broiler.JS/Broiler.JavaScript.Ast/Expressions/AstMemberExpression.cs:9`
  (`AstExpression(target.End, FastNodeType.MemberExpression, node.End)`: a member ends where its property ends), built
  by the bridge at `Broiler.JS/Broiler.JavaScript.Parser/Bridge/BridgeTreeBuilder.Expressions.cs:331` from the
  property the bridge builds, whose end token is the library's span end (`BridgeTreeBuilder.cs:88`); the optional chain
  takes the member's end (`AstOptionalChain.cs:14`, built at `BridgeTreeBuilder.Expressions.cs:49`) and the import call
  its argument's (`BridgeTreeBuilder.Expressions.cs:76`, as the old parser's `FastParser.SingleMemberExpression.cs:301`
  builds it). The same derivation ends every computed member before its `]` in both trees (`arr?.[0]` in `T039546`
  has the same span at P and B, without the bracket); P's span takes the `]` only here, because P's comma expression
  takes it (that line is S, `S-punct`). The reading: **a span the bridge's tree carries by the AST type's derivation**,
  in `Broiler.JS/`, not the library's (`Broiler.ECMAScript/`), so a change inside W2 by B8 could address it; it is the
  same rule P follows for every other computed member.
- **`for-of/head-await-using-bound-names-in-stmt.js`** (item 5, `T049530`, a module test). The early error is EE0056
  (`ForInOfStatement`: "any element of the BoundNames of ForDeclaration also occurs in the VarDeclaredNames of
  Statement"); the library's code is `JSE1417`, EE0056's ([`aids/base/bridge-codes.tsv`](aids/base/bridge-codes.tsv)).
  With the switch off the shell's module host compiles the module with top-level await permitted; the old parser,
  which checks EE0056 by the stage table's row (`Broiler.ECMAScript/docs/frontend/stages.tsv`: at
  `Broiler.JavaScript.Parser/FastScopeItem.cs:188` and `:121`, not in the compiler alone), misses it for this input
  (an `await using` head), as P's acceptance of `T049530` in the side-by-side report and the retained switch-off
  test262 record show; and the body runs to `$DONOTEVALUATE()` ("This statement should not be evaluated");
  with it on the library refuses the parse with EE0056, as the test expects. (The library's row table,
  `Broiler.ECMAScript/docs/frontend/early-errors.tsv`, says in EE0056's notes that "The `using` heads
  (for-of/head-using-bound-names-in-stmt.js, head-await-using-bound-names-in-stmt.js) are EP rows", this test by name;
  the code the library gives here is EE0056's, `JSE1417` (`Broiler.ECMAScript/Broiler.ECMAScript.Frontend/Rows.cs:289`),
  and the table's EP rows on `using` declarations state their own bindings' bound names (EP0001 to EP0005), not a
  collision with the statement's var-declared names.) In the side-by-side report, K at the base
  refuses this input with "Unexpected await" (`Broiler.JS/Broiler.JavaScript.Compiler/SyntaxValidation.cs:31-32`, the
  check, and the throw, of a program that awaits at its top level without the permission, which the K entry does not
  give), so
  [`../../corrections/test262-paths.tsv`](../../corrections/test262-paths.tsv) reads the move as the stage kind and
  explains it by the correspondence's D31 bucket ("Broiler.JS refusing only in its compiler"), and
  [`../../corrections/k-paths.tsv`](../../corrections/k-paths.tsv), written from those tables, lists no K row for it.
  As written, no K row lists it: K7's D35 ("Lexical redeclarations Broiler.JS misses", which the stage table names for
  EE0051 and EE0056) describes what the shell shows, but the tables explain the path by D31, and a K10 row can no
  longer be added before the first part C base run. Part A's C1 line, "a C1 row that none explains is recorded as a
  finding against the correspondence", is the record it fits. *(Changed the same day, after review, before this
  record merged: after "top-level await permitted," the bullet read "the old parser and compiler do not check that
  rule, and the body runs"; the parenthesis on the library's row table is added; the site read "(`SyntaxValidation.cs:32`,
  the check of a program".)*

### Item 8, the canonical dump

Bundle `sjl-12-001`'s canonical dump ([`../../../sjl-12-001/tools/parse-corpus/CanonicalDump.cs`](../../../sjl-12-001/tools/parse-corpus/CanonicalDump.cs),
unchanged) writes every node's span in one place, `Span(FastToken? start, FastToken? end)` (lines 283 to 292 at the base
commit): an end token of type `TokenTypes.EOF` is written `eof`, any other end token as its offset plus its length.
P's tree ends the nodes of item 8 at the old parser's end-of-file token, so P's dump writes `eof`; the bridge ends them
at the token that ends the node's last character (`BridgeTreeBuilder.cs:88`, `tokens.EndingAt`), so B's dump writes the
offset. The tool's comment at lines 294 and 295 says why the end-of-file token is written by name ("at the corpus commit
it is one object shared by every parse (FastScanner.EOF), and nothing of it is read"), and its README's "The canonical
dump" says `<end>` is "the last token's offset plus its length ... or `eof` for the end-of-file token". Neither the
tool's README nor bundle `sjl-12-001`'s rule says how a dump that writes `eof` is read against one that writes the same
offset as a number: the tool compares dumps by hash, and the two hashes differ. That tool is not changed. This
bundle's own span check does read `eof` as the end of the source:
[`../../tools/side-by-side/spancheck.py`](../../tools/side-by-side/spancheck.py) takes an end of `eof` as the source's
length (lines 163 to 166, and line 203 where it shows a span's text), which is why these lines come out `both-agree`
with identical texts; that supports reading them as a difference of representation, not of span, and is a reading
only. *(Added the same day, after review, before this record merged.)*
