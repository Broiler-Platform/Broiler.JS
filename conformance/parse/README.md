# The parse corpus (milestone SJL-12.4)

**Written:** 2026-10-20, at milestone SJL-12 of the shared JavaScript libraries programme, work package SJL-12.4
(the SJL-12 plan's tasks 4.1 to 4.3), on a branch from the Broiler.JS rule commit
`4d813f2ac863895b9be5c4619b7886c9ddd71077`. It is the population of bundle `sjl-12-001`'s corpus parts in both
repositories: this repository's rule, `docs/evidence/sjl-12-001/decision-rule.md`, part C (and part D, which takes
part C's after run), and Broiler.VM's rule, `docs/evidence/sjl-12-001/decision-rule-area.md`, part B (and part C).
It holds inputs only. No outcome, artifact or verdict is here: the baselines are the bundles'.

**Its home is here**, and Broiler.VM holds a byte-identical copy of `corpus.manifest` and `inputs/` at
`src/tests/corpus/parse-1/`, whose own README names this repository's commit and the manifest's SHA-256 (JSD-0080,
decision D32). No project item names the corpus.

## What is here

| Path | What |
|---|---|
| `corpus.manifest` | one row per input, and a header that fixes names, flags, prologue and pins (section "The manifest") |
| `inputs/broiler-vm/` | Broiler.VM's inputs, stored |
| `inputs/broiler-js/` | Broiler.JS's test inputs, stored |
| `provenance/broiler-vm.tsv` | where each stored Broiler.VM input was taken from |
| `provenance/broiler-js.tsv` | every test method whose run parsed each stored Broiler.JS input, with how many times and whether a text it parsed held a CR |
| `scripts/conformance/build_parse_corpus.py` | the builder that wrote all of it (outside this folder) |

test262's inputs are not copied: each is named by its path in the suite at the pin and held by its SHA-256.

## The manifest

**Lexical form.** UTF-8, no byte-order mark, LF line endings with a final LF, no CR, no empty line. The header comes
first; every header line begins with `#`. A line `#% <key>\t<value>...` is a field; a line `# ...` is a comment. The
first line that does not begin with `#` is the column line, exactly
`id\torigin\tpath\tsha256\tgoal\tvariants\tnegative\teval-family\tprovenance`. Every later line is one row of nine
tab-separated fields, none empty (`-` where there is no value). Rows are ordered by origin (`test262`,
`broiler-vm`, `broiler-js`), then by `path` in code-point order. The manifest's digest, which both rules compare
between runs, is the SHA-256 of the file's bytes.

**Header fields.**
- `format`: `broiler-parse-corpus 1`. `corpus`: `parse-1`. `rows`: the number of rows.
- `virtual-root` `/test262/` and `stored-root` `/parse-1/`: the fixed names below.
- `strict-prologue`: a JSON string literal, `"use strict";` and LF: test262's INTERPRETING.md prologue, which is
  also Broiler.VM's `Test262Adapter.StrictPrologue`.
- `eval-flags`, eight lines, E0 to E7: the set's name, its value as Broiler.VM's `EvalRequestFlags` (None 0, Strict
  1, InFunction 2, InMethod 4, InDerivedConstructor 8, InClassFieldInitializer 16, InClassBody 32) and its flags.
  E0 None; E1 Strict; E2 InFunction; E3 InFunction and Strict; E4 InFunction and InMethod; E5 InFunction, InMethod
  and InDerivedConstructor; E6 InFunction, InMethod and InClassFieldInitializer; E7 InFunction, InMethod and
  InClassBody. They are Broiler.VM's rule's, part B, sub-population B3.
- `eval-family-markers`: `eval`, `arguments`, `super`, `new.target`, `#`.
- `pin-test262` (the suite's revision), `pin-test262-archive` (Broiler.VM's archive of it, by repository path and
  SHA-256), `pin-broiler-vm` (the Broiler.VM commit its inputs were taken at, the VM rule commit) and
  `pin-broiler-js` (the commit the capture ran at, this repository's rule commit); `builder`.

**Columns.**

| Column | Values |
|---|---|
| `id` | `T`, `V` or `J` and six digits: test262, broiler-vm or broiler-js, numbered from 000001 in row order within the origin. Unique; the key every report joins on. |
| `origin` | `test262`, `broiler-vm` or `broiler-js` |
| `path` | test262: relative to the suite's root, beginning `test/`. Stored inputs: relative to this folder, beginning `inputs/`. |
| `sha256` | of the input's bytes as read: the suite's file unchanged, or the stored file |
| `goal` | `script`, `module`, or `json` (the text of a module imported `with { type: "json" }`) |
| `variants` | `strict,sloppy`, `strict`, `sloppy`, `raw`, `module` or `json` |
| `negative` | test262's front matter `negative`, as `<phase>:<type>`; `-` otherwise |
| `eval-family` | `yes` if and only if the goal is `script` and the text contains a marker as a substring |
| `provenance` | test262: `test`, `fixture` or `json-fixture`. broiler-vm: `js-1:`, `js-1-unwritten:` or `generated:`, with the row of Broiler.VM's `source.manifest` (kind, name, answer). broiler-js: the first test method in code-point order, `<test assembly>!<type>.<method>`, and ` (+N)` where N other methods parsed it too. |

**Variants** follow test262's INTERPRETING.md: no flag gives `strict,sloppy`; `onlyStrict` gives `strict`,
`noStrict` `sloppy`, `raw` `raw`, and `module` `module`. Where flags combine, Broiler.VM's `Test262Run` precedence
is taken: `module`, then `raw`, then `onlyStrict`, then `noStrict`. A stored script input has no front matter and
takes `strict,sloppy`. `sloppy` is the text as it is; `strict` is the strict prologue followed by the text where a
tool composes it by text (Broiler.VM's rule, part B, has its wide path set the unit's ForceStrict and keep the text
unchanged); `raw` is the text with no prologue and no harness; `module` and `json` are the text as it is.

**A `json` row under this repository's rule.** The corpus has 13 rows of goal `json`: test262's 12 `_FIXTURE.json`
files and one Broiler.VM input. They are inputs because the plan (JSD-0080 decision D33) and Broiler.VM's rule (part
B, B1, whose variants include JSON) require JSON inputs. This repository's rule, part C, takes every input under its
goal and each of its variants, but it names entry points for two goals only: P is a `FastParser` over a
`FastTokenStream` and `ParseProgram`, or `FastParser.ParseModule` for the module goal, and K is `CoreScript.Compile`
with the module flag. It gives three outcomes, accepted, refused and crashed, and requires every input and variant
at both entry points. It names no JSON entry point and no fourth outcome. This is a difference between the plan and
that rule, and the rule wins. Part C therefore reads a `json` row as its text says of every goal but the module
goal: the text as it is, at P through `ParseProgram`, and at K through `CoreScript.Compile` with the module flag
clear, answering accepted, refused or crashed like any other row. The text is parsed as a script, not as JSON. A
part C tool reports these 13 rows that way and gives no row an outcome part C does not name. Broiler.VM's rule
raises the same question for some rows: its B4 compiles each input by the slice front end under its goal, and the
slice front end has only the script and module goals; its B2 composes each test262 input as the conformance root
does, and the root never composes a fixture alone. That rule's bundle records how it reads those rows.

**Text.** Every input is strict UTF-8 with no byte-order mark; a tool passes the decoded text on unchanged.
Stored inputs hold no CR. test262 inputs are as the suite has them, and some hold CR, CRLF, U+2028 or U+2029 on
purpose.

**Fixed names.** A test262 input's source name, module key and script referrer is `/test262/` and its path; a stored
input's is `/parse-1/` and its path. A relative module specifier resolves against the importing module's name as a
URL path does. Broiler.VM's rule (part B) fixes the names here because a module's key and a script's referrer reach
its artifact's bytes. Only the input's own names are fixed. A harness file in a B2 composition keeps what the
conformance root gives it at `889ea15` (`Test262Run`): the unit name `harness0`, `harness1` and so on, no referrer,
and ForceStrict clear, because B2 composes each test as the root composes it and takes the root's own compile
requests.

## The population

| Origin | Inputs | Goal script, module, json | Variants | Eval-family |
|---|---|---|---|---|
| test262 at `ccaac100` | 53,481 | 52,362, 1,107, 12 | 102,454 | 23,416 |
| broiler-vm at `889ea15` | 66 | 62, 3, 1 | 128 | 1 |
| broiler-js at `4d813f2a` | 2,204 | 2,177, 27, 0 | 4,381 | 187 |
| **All** | **55,751** | | **106,963** | |

**test262.** Every regular file under `test/` whose name ends `.js` and holds no `_FIXTURE` is a test: 53,194
files, of which 48,599 are not parse-negative ("every positive parse test", JSD-0080 decision D33, which includes
the 34 resolution-negative and 40 runtime-negative tests) and 4,595 are parse-negative. They give 102,167
variants, the count a YAML reading of the suite gives. Front matter is the first `/*---` ... `---*/` block, read with
PyYAML's `safe_load`. The 275 `_FIXTURE.js` files are module inputs and the 12 `_FIXTURE.json` files JSON inputs.
Two tests say both `module` and `raw` and are module inputs:
`test/language/comments/hashbang/module.js` and `test/language/module-code/import-attributes/allow-nlt-before-with.js`.
Seven files under `test/` are not inputs, because they are neither JavaScript nor JSON or are not named as a test:
`test/language/import/import-attributes/text-empty_FIXTURE`, `text-string_FIXTURE` and `text-via-namespace_FIXTURE`
(text-module fixtures), `test/language/import/import-bytes/bytes-from-empty_FIXTURE.bin`, `bytes-from-png_FIXTURE.png`
and `bytes-from-txt_FIXTURE.txt` (bytes-module fixtures), and
`test/language/statements/using/initializer-disposed-at-end-of-each-iteration-of-forofstatementjs` (no `.js`
extension, so no runner takes it as a test). Nothing outside `test/` is an input; `harness/` is read by compositions.

**broiler-vm.** The 64 files of Broiler.VM's retained source corpus `src/tests/corpus/js-1/source/accepted/` and
`refused/` at `889ea1568f59bf37aa21c0035b423d0d030eb87f`, byte for byte, goal by extension as the slice compiler's
`--write` records it (`.js` script, `.mjs` module, `.json` JSON), and the texts of the two `SliceSourcePrograms`
members `source.manifest` marks `generated`: `refuse-more-locals-than-the-frame-admits` (`Declarations(65536)`,
775,324 bytes) and `refuse-more-constants-than-the-pool-admits` (`DistinctConstants(65536)`, 447,642 bytes), each
normalised as `--write` normalises a text. Every file's and every generated text's SHA-256 equals its
`source.manifest` row, and every file but two is byte-identical to what `--write` writes at that commit. The two,
`refused/refuse-a-name-exported-twice.js` and `refused/refuse-an-export-of-a-name-nothing-declares.js`, are older
script-goal copies of the module sources `--write` now writes as `.mjs`; they are in the directory, so they are
inputs, read as scripts, and marked `js-1-unwritten`.

**broiler-js.** Every distinct text a `FastTokenStream` was constructed over while `Broiler.JavaScript.Parser.Tests`
(545 tests) and `Broiler.JavaScript.Compiler.Tests` (1,443 tests) ran in Release at
`4d813f2ac863895b9be5c4619b7886c9ddd71077`: 2,716 constructions (555 and 2,161), 2,203 distinct texts. The goal is
`module` where the module goal was in force (`CoreScript.IsModuleGoal`) and `script` otherwise. Each text is
LF-normalised (CRLF and lone CR become LF), as plan task 4.1 requires, and the corpus holds each (normalised text,
goal) once: 2,204 inputs, stored as `inputs/broiler-js/<SHA-256>.js` or `.mjs`. One input begins with U+FFFD on
purpose (`ParserTests.ParseProgram_ReplacementCharacter_SaysTheSourceWasMisdecoded`).

**What LF normalisation changed.** It changed 191 of the 2,203 captured texts, which give 188 inputs. 182 of the 191
hold CRLF: 178 are C# raw string literals in twelve test files whose lines end, wholly or in part, with CRLF, and 4
are deliberate line-terminator cases. The other 9 hold a lone CR, and each is a deliberate line-terminator case.
Normalising also made 10 captured texts equal to another, so 2,203 distinct texts became 2,193; 11 of those were
captured under both goals, which gives the 2,204 inputs. Six of the 10 come from tests that pass one text with
different line terminators on purpose, and those forms now share one stored input:
`SeparatorAndLiteralScanningTests.EveryLineStartsAtColumnOne` and
`TruncatedInputAndLineTerminatorTests.EveryLineTerminatorAdvancesTheLine` (each LF, CRLF and lone CR),
`TruncatedInputAndLineTerminatorTests.ParseErrorsAfterLoneTerminatorsNameTheirOwnLine` (CRLF and lone CR) and
`SeparatorAndLiteralScanningTests.MissingListSeparatorsAreASyntaxError` (LF and CRLF). The other 4 are CRLF copies,
in `DeferredBodyReentryTests`, of programs that `CaptureLayoutTests` and `DeferralPopulationTests` hold with LF. The
6 lone-CR texts that were not merged keep an input of their own, which holds LF where the test had CR: for example,
with `\r` standing for the CR and `\\` for one backslash, `"open\rstring"`
(`TruncatedInputAndLineTerminatorTests.TruncatedInputIsRejected`), and `/\\\rn/;` and `eval("'foo\\\\\r")`
(`CompilerTests.Compile_Syntax_Errors_Are_Reported_For_Strict_And_Direct_Eval_Cases`). A CR, alone or before LF, is
therefore in the corpus only where test262's inputs hold one. `provenance/broiler-js.tsv` says, for each test method
and stored input, whether a text that method's run captured held a CR.

**Not deduplicated across origins.** A text may be an input of two origins, or of two goals; each is its own row.

## How it was made

The builder's modes are deterministic, and the committed files are what they wrote. In the order they ran:

1. **Broiler.VM's inputs (plan task 4.3).** In a scratch detached worktree of Broiler.VM at `889ea15`, the slice
   compiler was built in Release and run with `--write <dir>`; its `source/` was compared with the committed
   `src/tests/corpus/js-1/source/` (identical but for the two files named above). A scratch console program loaded
   that build's `Broiler.VM.Profile.JavaScript.Compiler.dll` by reflection and wrote the text of every member of
   `SliceSourcePrograms.Accepted`, `Refused` and `RefusedModules`, normalised as `--write` normalises (LF, one
   trailing LF), with no size limit; all 64 SHA-256s equal `source.manifest`'s rows, and the two longer than
   `--write`'s 4,096-character limit were matched to the generator calls `Declarations(65536)` and
   `DistinctConstants(65536)` by equality of text. Then:

       python3 -I scripts/conformance/build_parse_corpus.py import-vm --vm <worktree> \
           --commit 889ea1568f59bf37aa21c0035b423d0d030eb87f --generated <dump> --write-output <dir>/source \
           --corpus conformance/parse

2. **Broiler.JS's test inputs (plan task 4.2).** In a scratch detached worktree at `4d813f2a`, a capture hook was
   added and never committed: a recorder in the Parser assembly, called first in both `FastTokenStream`
   constructors, writes each text (UTF-8) under its SHA-256 and appends a record of its length, the goal in force
   and the outermost frame of a `*.Tests` assembly on the stack (the test method; a lambda's or async state
   machine's generated name read back to its method). `CompilationStack.Run` calls the recorder once per flow to
   leave the test method in an `AsyncLocal`, which a compile moved to its worker thread sees under the caller's
   execution context. The two projects were built in Release and run with `dotnet test` and the capture directory
   set. One record still had no test method on its stack, from an async test whose compile ran in a task:
   `CompilerTests.Compile_Private_Method_Call_Allows_Following_Call_Chain`. Its text is that test's literal, and
   its site was written into the record by hand before the import. A second run of both projects gave the same records.
   The worktree was then removed. Then:

       python3 -I scripts/conformance/build_parse_corpus.py import-js --capture <dir>/Parser \
           --capture <dir>/Compiler --commit 4d813f2ac863895b9be5c4619b7886c9ddd71077 --corpus conformance/parse

3. **The manifest (plan task 4.1).**

       python3 -I scripts/conformance/build_parse_corpus.py manifest \
           --test262 <Broiler.VM>/src/tests/conformance/pins/test262-ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e.tar.gz \
           --test262-also <test262 checkout at ccaac100> --corpus conformance/parse

   `--test262` takes the archive (checked against its pinned SHA-256) or a checkout at the pin (checked by `git
   rev-parse`, read only); the archive is also the one `test262.pin`'s `archive` URL serves. `--test262-also` read
   the other one, and every file under `test/` was byte-identical in both.

**Reproducing it.** `python3 -I scripts/conformance/build_parse_corpus.py check --test262 <archive or checkout>`
rebuilds the manifest from the suite, `inputs/` and `provenance/` and compares it with the committed file byte for
byte (exit 0 when identical); it also checks every stored input against its provenance. A second build of the whole
corpus from scratch, steps 1 to 3 into an empty folder, the manifest from the checkout instead of the archive, was
byte-identical to the first, `inputs/` and `provenance/` included. The builder needs PyYAML for test262's front
matter (6.0.1 here; the compliance workflows install 6.0.2). Its tests are
`scripts/conformance/tests/test_build_parse_corpus.py`.

## What this does not do

- **It decides nothing.** Outcomes, diagnostics, artifact hashes, census and dumps are the bundles' baselines,
  taken by each repository's tool at its corpus commit.
- **It is not a test262 run.** No harness file is in it, and its variants are parse variants, not the runners'
  configurations.
- **It is not revised in place.** A different population is a new corpus with a new name and a new manifest.
