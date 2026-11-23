# Bundle SJL-14-001's decision rule, revised after part C's base runs were seen: parts C and D admit the differences the owner's answers 10 and 11 of 2026-11-23 name, by name, and nothing wider

**Written:** 2026-11-23, at milestone SJL-14 (W2, the after commit of SJL-14.3 to SJL-14.5, not yet merged), during
W2's hold, with the default branch's head at the base commit `69a8a13bc446a1714d3c8be9be3cfd742aebee6e`. It is written
**after part C's base runs were seen**, and after part C was read on them: the base commit's local runs of part C,
retained in [`base/local/`](base/local/README.md), their classification in [`base/part-c/`](base/part-c/README.md), and
the bundle README's reading of them, "Part C: read NOT MET on the retained runs", with the reading aids and single-case
re-runs of records part 4. The other runs this rule judges or names that had been seen: part B's runs at W1 and the
dark commits, judged in the README; part C's runs at W1, read and not judged; part A's base run, `test262-evidence.yml`
run 44639106095, in [`base/part-a/`](base/part-a/imported.txt); and the base runs of parts D, E, G and H in
`base/local/`; with the single case of `test/staging/sm/regress/regress-610026.js` run at W2's head on branch
`sjl-14-w2`. No after run of any part has been taken, and the after commit does not exist. **Owner:** the Broiler.JS
maintainer. **Reviewer:** none; every role is one person (the programme's EX-30), and this revision does not claim that
anyone independent has read it.

**Whose decision.** This revision is filed by the owner's answers 10 and 11 of 2026-11-23, answers of the owner's own,
not ones taken under the owner's direction of 2026-10-18, quoted verbatim.

Answer 10. Asked (2026-11-23): "SJL-14 W2: part C reads NOT MET at the base. After investigation (verified), no library
defect was found, so no dark merge is needed and the base runs can stand. The remaining unclassified differences:
(1-3) 11 inputs where the old parser wrongly accepted and the bridge correctly refuses, but with a different first
error row than the classification expects; (4) the switch's own architecture test; (5) a for-of `await using` test that
moves fail→pass (an improvement) with no correction row; (7) one Terser-minified variant the runner skips; (8) 3,625
dump lines where P writes the end of input as `eof` and B writes the same offset as a number; (9) 6 computed-member
spans the AST type derives the same way in both trees. Groups 1-3, 8 and 9 will also reappear in part D at the after
run. Admitting them after the runs were seen needs a dated rule revision, which the precedents (your SJL-11 answers 7
and 8) treat as yours. How should I proceed?" Chosen: "Revise rule (Recommended)" — "I draft one dated revision of
sjl-14-001's rule from your answer, admitting groups 1-5, 7, 8 (and 9 if you choose C below) for part C and part D,
marked as weaker evidence written after the runs were seen. Part C is then re-read on the same retained runs, nothing
re-run, and W2 proceeds to its PR." Not taken: "Merge as NOT MET" — "Change no rule. Record part C as read NOT MET at
the base, with the unclassified list, and proceed to W2's PR and merge anyway (nothing is accepted under EX-30). Part D
at the after run will then also read these as regressions."; "Hold W2" — "Stop before W2's PR. Leave the hold on
Broiler.JS main and the base runs as they are until you review the decision summary yourself."

Answer 11. Asked (2026-11-23): "Group 9 (6 variants): the bridge ends a computed member like `a[b, c]` where its
property ends, before the `]`, because AstMemberExpression derives its end that way (the old parser included the `]`
only by its comma-expression overshoot). Fix it in W2 or admit it?" Chosen: "Admit by revision (Recommended)" —
"Include group 9 in the dated revision. W2's trees stay as they are; no other span changes." Not taken: "Fix in W2" —
"Change the after tree so a computed member ends at its `]`. More faithful to the source text, but it changes every
computed member's span in the after tree (part D would read those as class S). Needs the parse-corpus check and the
tests re-run."

This file is that dated revision. It changes no rule of Broiler.VM's or of any other bundle.

**The rule revised** is [`decision-rule.md`](decision-rule.md) as blob `e0e954631f798d38ca67d0b808c53c3d3c38f44e`, the
file the base commit holds. It is quoted whole at the end of this file, as bundle `sjl-11-001`'s revisions quote theirs
(each line prefixed with "> ", a blank line a bare ">"), and is not edited, as its section "Revisions" requires. This
bundle has no earlier revision. **Its name** is the rule's file name with `-revised-` and the date, as bundle
`sjl-11-001`'s revisions are named; the name decides nothing.

## Why it is written

Part C's admitted difference classes and MET line read, whole:

```rule
admitted difference classes: part A's C1 (by K1 to K10), C3 and T; E, by this file's header; and M - a refusal with the same exception type at the same entry point whose message differs, each listed in the bundle's generated message table with both texts (decision D46); and S - a dump difference only in a node's span, where P's span disagrees with the node's source text and B's agrees, listed with both spans checked against the text
every other difference is unclassified
MET if and only if every run at the base commit is complete, and every difference is in C1, C3, E, T, M or S and is listed.
```

Part D's admitted difference classes and MET line read, whole:

```rule
admitted difference classes: part C's C1, C3, E, T, M and S
every other difference is a regression
MET if and only if both runs are complete, the control is reported, and every difference is in C1, C3, E, T, M or S and is listed.
```

and part D's "How it is read" ends: "class S is the only tree difference admitted outside the corrections."

Part C at the base commit is read NOT MET on the retained runs in the bundle's README, "W2's base, recorded on
2026-11-23", for the differences [`base/part-c/README.md`](base/part-c/README.md) lists as unclassified, items 1 to 9,
with records part 4's readings of them. The owner's question numbers them as groups, and this file keeps its numbers:
- **Groups 1 to 3**: inputs P accepts and B refuses, K refusing with the switch off and on, whose first diagnostic by the
  library's code is a row the header's class E does not admit (a `syntax` row with `js_compiler_only` `no` or `partly`
  that bundle `sjl-13-001`'s classification does not list among the rows whose codes may come first): a getter with a
  parameter (EX0009), an arrow function as a class heritage that names a private name (EX0008), and a parenthesized
  literal as an assignment target (EE0033).
- **Group 4**: the architecture test of the test-only switch itself, failing in the switch-on build by its own design.
- **Group 5**: a test262 case failing with the switch off and passing with it on that no row of K1 to K10 lists, which
  has part A's C1 form, and which part C's C1 does not admit ("each row one of corrections K1 to K10 explains").
- **Group 6**: a test262 path passing with the switch off and failing with it on, stopped by the runner's processor
  limit through the bridge's cost.
- **Group 7**: a test262 case passing with the switch off and skipped with it on, the runner finding the minifier's
  output invalid.
- **Group 8**: tree differences with a span line whose end P's dump writes `eof` and B's the same offset as a number,
  both giving exactly the node's text, so P's span does not disagree with the text and the line is not S.
- **Group 9**: tree differences with the span line of a member whose property is a comma expression, or of the optional
  chain or import call that takes its end from it, where P's span ends after the member's `]` and B's before it, as
  `AstMemberExpression` derives a member's end in both trees; P's span agrees with the text and B's does not, so the
  line is not S.

The question's count for group 8 is of the input variants [`base/part-c/unclassified.tsv`](base/part-c/unclassified.tsv)
lists, one row each, not of dump lines, and its count for group 9 is of input variants, as answer 11 counts them.

Part D's after comparison is the base commit's old parser against the after commit's `FastParser` facade, which parses
through the bridge, over the same corpus, so the differences of groups 1 to 3, 8 and 9 are the ones it will meet, and
part D admits only part C's listed classes. Groups 4 to 7 are tests and test262 cases, which part D's population does
not hold.

## What changed

1. **Part C's admitted difference classes.** After the line "admitted difference classes: part A's C1 (by K1 to K10), C3
   and T; E, by this file's header; and M - ...; and S - ...", and before "every other difference is unclassified",
   part C now also admits these six classes, each only for the inputs, tests or paths it names, each listed with them:
   - **rev-code** - an outcome move at P against B, P accepting and B refusing, K refusing with the switch off and on, of
     one of these inputs of `conformance/parse/corpus.manifest`, in its strict or sloppy variant, whose first diagnostic
     by the library's code (read as part C reads it at the base, by the bundle's corrected reading aid,
     [`base/part-c/scripts/bridge-codes/`](base/part-c/scripts/bridge-codes/Program.cs)) is the one named for it:
     row EX0009, code `JSE9109`, for `T035220` (`test/language/expressions/class/getter-param-dflt.js`), `T039055`
     (`test/language/expressions/object/getter-param-dflt.js`), `T047023`
     (`test/language/statements/class/getter-param-dflt.js`), `J000058`
     (`inputs/broiler-js/06edc9ffbd09c000774e9a042996190dfe96320c8fd17938fda2709c907f0be7.js`) and `J001174`
     (`inputs/broiler-js/835bebfffb419915bee38bce9e400a249c1b905b25062c9c0a101a7fcb403a0b.js`); row EX0008, code
     `JSE9108`, for `T034966`
     (`test/language/expressions/class/elements/syntax/early-errors/grammar-private-environment-on-class-heritage-array-literal.js`),
     `T034969`
     (`test/language/expressions/class/elements/syntax/early-errors/grammar-private-environment-on-class-heritage-obj-literal.js`),
     `T046767`
     (`test/language/statements/class/elements/syntax/early-errors/grammar-private-environment-on-class-heritage-array-literal.js`)
     and `T046770`
     (`test/language/statements/class/elements/syntax/early-errors/grammar-private-environment-on-class-heritage-obj-literal.js`);
     row EE0033, code `JSE1329`, for `J000040`
     (`inputs/broiler-js/04edcf210d282800769b995d9de9aef961c9a8502b9e0dfd0fe13aa2d7185922.js`) and `J001147`
     (`inputs/broiler-js/80b3f39b7fbfb06b35dd0c6b72d3df705ff5b46af45366ff12182291706b7e28.js`); listed with the input,
     its variant, the row and the code. Groups 1 to 3.
   - **rev-eof** - a dump difference of an input variant that [`base/part-c/unclassified.tsv`](base/part-c/unclassified.tsv)
     lists with the reason "span lines not S: P-eof-same-text", as blob `4cb883804fcea72f29621a05d8476dc0168f5b47`
     (SHA-256 `1622df7102aa57fa94730542b05df91843f5c2f8099feeefc8dc26166820194f`) holds it, the file this revision
     was written from, whose every differing line is S or a line where P's span ends at `eof` and B's, beginning at the
     same offset, ends at the source's length written as a number: the end written `eof` is read as the same offset as
     the source's length, and the two as one span; listed with the input, its variant and those lines. Group 8.
   - **rev-bracket** - a dump difference of `T036087`
     (`test/language/expressions/dynamic-import/assignment-expression/call-expr-expr.js`), `T039546`
     (`test/language/expressions/optional-chaining/optional-chain-prod-expression.js`) or `T051374`
     (`test/language/statements/using/syntax/using-for-using-of-of.js`), in its strict or sloppy variant, whose every
     differing line is S or the line of an `AstMemberExpression` whose property is a comma expression, or of the
     `AstOptionalChain` or `AstImportCall` that takes its end from that member, where P's span ends after the member's
     `]` and B's where the member's property ends, before the `]`, the end `AstMemberExpression` derives for a member
     in both trees; listed with both spans and texts. Group 9.
   - **rev-switch-test** - the test `Broiler.JavaScript.Architecture.Tests.BridgeSwitchTests.The_compilers_test_only_switch_is_off_in_this_build`
     of `Broiler.JavaScript.Architecture.Tests`, passing with the switch off and failing with it on by its own message
     that the build has the switch on ("Broiler.JavaScript.Runtime was built with BROILER_PARSE_THROUGH_BRIDGE");
     listed. Group 4.
   - **rev-for-of** - the test262 path `test/language/statements/for-of/head-await-using-bound-names-in-stmt.js`,
     original variant, failing with the switch off and passing with it on; listed with K7 and its row D35 ("Lexical
     redeclarations Broiler.JS misses"), with the note that the library's row table,
     `Broiler.ECMAScript/docs/frontend/early-errors.tsv`, reads in EE0056's notes that "The `using` heads
     (for-of/head-using-bound-names-in-stmt.js, head-await-using-bound-names-in-stmt.js) are EP rows", while the library
     refuses this input with EE0056's code, `JSE1417`. It is not read as C1, and no row is added to
     [`corrections.md`](corrections.md) or its tables. Answer 10 names no row for this group; the listing with K7 and
     D35 follows the decision summary the question rested on, D35 being the row whose description fits what the shell
     shows. Group 5.
   - **rev-minifier** - the test262 path
     `test/language/expressions/dynamic-import/syntax/valid/new-covered-expression-is-valid.js`, Terser variant,
     passing with the switch off and skipped with it on, the runner's record giving `minifier-invalid-output`; listed.
     Group 7.
2. **Part C's MET line.** "every difference is in C1, C3, E, T, M or S and is listed" now reads "every difference is in
   C1, C3, E, T, M or S, or in rev-code, rev-eof, rev-bracket, rev-switch-test, rev-for-of or rev-minifier, and is
   listed". Nothing else of the line changes: "every run at the base commit is complete" stands, and so does the NOT
   MET line.
3. **Part D's admitted difference classes.** "admitted difference classes: part C's C1, C3, E, T, M and S" now reads
   "admitted difference classes: part C's C1, C3, E, T, M and S; and part C's rev-code, rev-eof and rev-bracket, read
   at part D's comparison as follows". At part D each reads the base commit's entry point P against the after
   commit's, and admits only the inputs it names at part C:
   - **rev-code** - an outcome difference at entry point P of one of the inputs named for rev-code above, in its strict
     or sloppy variant, accepted at the base commit and refused at the after commit, K refusing at both commits, whose
     first diagnostic by the library's code at the after commit is the one named for it above; listed with the input,
     its variant, the row and the code.
   - **rev-eof** - a dump hash difference of an input variant named for rev-eof above, whose every differing line is
     S or a line where the base commit's dump ends the span at `eof` and the after commit's, beginning at the same
     offset, at the source's length written as a number; listed with the input, its variant and those lines.
   - **rev-bracket** - a dump hash difference of `T036087`, `T039546` or `T051374`, in its strict or sloppy variant,
     whose every differing line is S or the line of a member, optional chain or import call as named above, where the
     base commit's span ends after the member's `]` and the after commit's where the member's property ends; listed
     with both spans and texts.
4. **Part D's MET line.** "every difference is in C1, C3, E, T, M or S and is listed" now reads "every difference is in
   C1, C3, E, T, M or S, or in rev-code, rev-eof or rev-bracket, and is listed". Nothing else of the line changes, and
   the NOT MET line stands.
5. **Part D's "How it is read".** "class S is the only tree difference admitted outside the corrections." now reads
   "class S, and rev-eof and rev-bracket for the input variants they name, are the only tree differences admitted
   outside the corrections."
6. **Nothing else changes**: parts A, B and E to K; every other line of parts C and D (their evidence class,
   population, configuration, completeness, controls and comparison, C1, C3, E, T, M and S as worded, "every other
   difference is unclassified" and "every other difference is a regression" for every difference these classes do not
   name, and part C's "How it is read"); the header, its classes E and T and the commits it names.

## What it does not admit

- **Group 6**, `test/staging/sm/regress/regress-610026.js`, both variants, passing with the switch off and failing with
  it on, is not named in the owner's question or answers and is not admitted. Part C at the base reads it as the rule
  reads it: unclassified. It is a bridge defect the bundle's decision B8, recorded with W2 on branch `sjl-14-w2`
  (its bundle README, "B8, a bridge defect found at the base"), sends inside W2: "One part C finds unclassified at the
  base commit is fixed inside W2, on this branch, and part C reads NOT MET for that input unless a class admits it."
  W2 fixes it by `e0b2d17bda6e578c0a8213f5263374f0fdb13ded` ("the bridge no longer builds the empty statements and
  blocks a statement list drops (decision B8)") with its review fix `80b04d26fca6138c4337b000bc621876ec78cd7e`; what
  W2's after runs read for that path is read there as the rule reads it.
- **Anything beyond the entries named.** Another input, another variant, another code, another kind of span line, or
  another test or path is read as the rule reads it: in C1, C3, E, T, M or S, or unclassified at part C and a
  regression at part D.

## How it reads

- **Only the named entries.** Each class names its inputs, tests or paths, and the difference it admits for them; it
  admits no other difference of the same inputs, and no input it does not name. rev-eof names its input variants by
  the blob above, the list of part C's base reading; at part D it admits no other input variant.
- **Not E, not C1 and not S.** rev-code's inputs are not read as class E: the header's class E is not widened, and its
  `partly`/`no` branch still asks for a row the classification lists first. rev-for-of is not C1: part C's and part
  A's C1 still ask for a row of K1 to K10 that explains the path, and `corrections.md` is not edited. rev-eof and
  rev-bracket are not S: S still asks that P's span disagree with the node's source text and B's agree.
- **Part A is not changed.** Part A's C1 line ("a C1 row that none explains is recorded as a finding against the
  correspondence and is a regression") and its last line ("every other difference is a regression, including a row
  that passes at the base and fails, times out or is skipped after outside C3") stand; what part A's after comparison
  reads for the paths of groups 5, 6 and 7 is read there as written.
- **The runs are not taken again.** Part C is read again on the runs already retained, with nothing rebuilt or rerun,
  as answer 10 says. The first reading, "read NOT MET on the retained runs", stays on record in the README beside the
  second. Part D is read at the after commit under this revision, with the runs the rule names.

## What it decides, and what it does not

- **It decides** that part C admits the differences of groups 1 to 5, 7, 8 and 9 by the six classes above, each for the
  entries it names, and that part D admits those of groups 1 to 3, 8 and 9 by three of them, and that both parts' MET
  lines, and part D's "How it is read", read with them.
- **It does not change** any other line of the rule. It widens no tolerance and no class the rule names, adds no row to
  the corrections, and admits group 6 by nothing.
- **It is weaker evidence** in the sense of the rule's section "Revisions": "A rule revised after any run it judges has
  been seen is a new dated file in this directory. It quotes this one whole and says it was written afterwards, and its
  decision is then weaker evidence." It was written after part C's base runs, their classification and their first
  reading had been seen, and after the runs named at the top of this file; for part D, after part D's base runs were
  taken and before any after run.
- **It takes no reading** of any part and declares nothing met; the bundle's README reads part C again under it.
- **The alternatives not taken** are the owner's: "Merge as NOT MET" and "Hold W2" (answer 10), and "Fix in W2"
  (answer 11), quoted above.

## The rule it revises, quoted whole

> # Bundle SJL-14-001's decision rule: Broiler.JS on the shared syntax library, judged by test262 per path, by the bridge's identity with the switch off, by the side-by-side run, by the parse corpus across the cut-over, by the portable compiler and its Native AOT sample, by the packages, by the downstream consumers, by the public API, by its parse measurement, by fuzzing and by the deletion search
>
> **Written:** 2026-11-03, at milestone SJL-14 (work package SJL-14.1) of the shared JavaScript libraries programme,
> with this repository's `docs/roadmap/SharedLibraries.md` section "Broiler.JS on the shared syntax library (SJL-14)" and
> Broiler.VM's JSD-0083, before any change it judges exists: no bridge, no switch, no adapter package and
> no deletion. Committed in the same change: [`corrections.md`](corrections.md), [`dispositions.md`](dispositions.md)
> and [`inputs/`](inputs/README.md) (part I's workloads, part J's seeds). **Milestone judged:** SJL-14,
> `Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-14-broiler-js-syntax.md`: its Broiler.JS clauses 1 to 5,
> clauses 2 and 5 as revised on 2026-11-03 before any evidence (decisions D50 and D49), and clause 6 ("Broiler.JS's
> parse measurement meets its rule.") as the milestone file states it when part I is read: what clause 6 becomes while
> no controlled lane exists is the owner's own choice (decision D48), and this file does not make it. **Owner:** the Broiler.JS maintainer. **Reviewer:** none. Every role this rule names is
> held by one person (the programme records this as Broiler.VM's exception EX-30), and this rule does not claim that
> anyone independent has read it.
>
> **What this file is.** Eleven predeclared decisions, one per part. Part A is template T2 and part I template T5. Part
> B is an identity, part C a side-by-side comparison, part D the parse corpus across the cut-over with bundle
> `sjl-12-001`'s tool, part E layer L10, part F layer L11, part G layer L8, part H layer L12, part J layer L9 and part K
> the deletion search. Each part decides alone. It holds no figure. The bundle has a `manifest.json` (decision D56).
>
> **Bundle `sjl-12-001`'s tool** (parts B and D) is used byte-unchanged. From the after commit it builds with the
> obsolete marks' diagnostic identifier suppressed by the `NoWarn` of the repository root's `Directory.Build.props`, which
> it inherits (decision D50), and the `FastParser` facade it calls throws today's exception types, `FastParseException`
> for a refusal. A build of the tool that fails at any commit makes the part that needs it void, not MET.
>
> **The corrections this rule classifies by** are [`corrections.md`](corrections.md)'s K1 to K10, each with its rows of
> Broiler.VM's `docs/evidence/sjl-13-001/correspondence.md`, or of this bundle's inherited-context probes
> ([`inherited-context/`](inherited-context/README.md), rows IC1 to IC7 of K10), and its test262 paths, listed in `Broiler.JS/CHANGELOG.md` by
> name at the cut-over (decision D45). **JSD-0080 decision D27's listed refusals** are not corrections: they are an
> implementation limit, class C3, enumerated by differencing the corpus outcomes on the 256 MiB thread the SJL-12
> baselines used (decision D47).
>
> **The stage and timing classes** (decisions D45 and D64), named before any run with their rows in
> [`corrections.md`](corrections.md):
> - **E, early-error stage** - an input accepted at entry point P (the parser alone) at the side the base stands for,
>   and refused at P or B on the other side with a SyntaxError whose shared code is a `syntax` row of the stage table,
>   K refusing on both sides, where either the row's catalogue `js_stage` names only compiler sites
>   (`SyntaxValidation`, `FastCompiler.EarlyErrors`, `Visit*`, `DirectEvalSupport`; the stage table's
>   `js_compiler_only` `yes`, [`corrections/class-e.tsv`](corrections/class-e.tsv)), or the row's `js_compiler_only` is
>   `partly` ([`corrections/partly.tsv`](corrections/partly.tsv)) or `no`, and bundle `sjl-13-001`'s classification
>   expects the input refused in configuration J with that row among the rows whose codes may come first (decision D45
>   as revised on 2026-11-03, finding S14-F1 in [`corrections.md`](corrections.md)); a row the stage table marks
>   `not-checked` is never class E; listed with its row. The library checks at the parse what this engine checked only
>   in its compiler.
> - **T, timing** - for those of the E rows whose check ran lazily or after other code ran (a `Visit*` check at a
>   function's first call), an input or test262 row whose SyntaxError this engine raised later than the specification's
>   early-error point, or not at all when the function was never called, and raises before evaluation after; listed with
>   its row.
>
> **The product paths** (decision D42): `Broiler.JS/` except `Broiler.JS/benchmarks/` and `Broiler.JS/CHANGELOG.md` -
> which includes `Broiler.JS/Broiler.JavaScript/`, the engine's main project, whose folder has no suffix -
> `Broiler.ECMAScript/` outside `Broiler.ECMAScript/docs/`, and the root's `Directory.Packages.props`,
> `Directory.Build.props`, `Directory.Build.targets`, `global.json` and `NuGet.config`.
>
> **The commits, named before they exist.**
> - **The rule commit** carries this file.
> - **The bridge commit (W1)** is the default-branch merge that lands SJL-14.2's bridge with the switch off by default,
>   one merge. **Its base** is its first parent, which descends from the rule commit; W1 is the only first-parent commit
>   that carries any part of the bridge or the switch.
> - **The after commit (W2)** is the default-branch merge that lands SJL-14.3 to SJL-14.5, one merge. **The base commit**
>   is its first parent, which descends from W1 and from the rule commit; W2 is the only first-parent commit that
>   carries any part of SJL-14.3 to SJL-14.5 (the switch's removal, the adapter packages, the portable compiler's and the
>   module analysis's move, the deletions).
> - If either window had to land in more than one merge, each is named in this bundle, its base is the first one's first
>   parent, and its after runs are taken at the last one.
> - **A dark commit** is any default-branch merge between W1 and the base commit that changes only
>   `Broiler.ECMAScript/Broiler.ECMAScript.Frontend*/` (decision D52). Each carries part B's identity check with its own
>   first parent, retained in this bundle; its switch-on outputs are retained and read, and part C, taken at the base
>   commit, judges them.
> - Each run names its tested commit; hosted runs are dispatched at any time after their commits exist.
>
> A part whose base was taken at any other commit, or whose commits fail those conditions, is void.
>
> ## Part A - test262 parity per path, in UTC, with negative tests (template T2)
>
> ```rule
> evidence class: conformance parity (not a measurement)
> population: the whole pinned suite - test262 at revision ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e - every path the runner's --all-script-host-verifiable selection admits with --include-negative, in both variants the canonical configuration runs, original and terser
> configuration: scripts/compliance/run_test262.py through the shell BroilerJS built in Release, by .github/workflows/test262-evidence.yml as the base commit carries it and unchanged at the after commit, dispatched with the tested commit's forty-digit identifier, suite_ref at the revision above, max_workers 4, test_timeout_seconds 30, rerun_timeouts true, time_zone empty and include_negative true: eight shards merged by scripts/compliance/merge_test262_shards.py, --minifier terser at the version its lock file pins, no memory limit, negative-metadata tests included and their expected errors verified, a --self-check before each shard, every shard running the same shell, and every shard in UTC, shown by its host-zone record; both runs' host-zone records read alike; the base run at the base commit and the after run at the after commit, on the same RID; each artifact imported unchanged, with the zip's digest
> comparison: per path and variant, through scripts/compliance/compare_test262_paths.py as committed at SJL-0.9, the classification file retained beside its report
> timeouts: a row that timed out in either run is the one the workflow ran again alone, with one worker; the script takes the re-run through --base-rerun or --after-rerun, and both are retained
> admitted difference classes:
>   C1 correction - a row failing at the base that passes after, because the shared frontend follows the pinned text where this engine did not: each row one of corrections K1 to K10 explains; listed with its path, its variant and its correction, and in the changelog (decision SJL-D-16); a C1 row that none explains is recorded as a finding against the correspondence and is a regression
>   C3 implementation limit (JSD-0080 decision D27) - a row passing at the base and failing after because its source nests past the bound the frontend applies for this engine, refused with a SyntaxError for nesting; listed with its path, its variant and the depth the frontend reports; expected empty
>   T timing (decision D64) - a negative row failing at the base that passes after because the SyntaxError of a class T row of corrections.md, which this engine raised late or not at all, is raised before evaluation; listed with its path, its variant and its row
> no C5: no data, pin or policy moves
> every other difference is a regression, including a row that passes at the base and fails, times out or is skipped after outside C3
> MET if and only if both runs are complete (every expected shard reported, one suite revision, one shell, every shard in the zone, includeNegative true in both), the two runs' host-zone records read alike, the script finds them comparable, and every changed verdict is in C1, C3 or T and is listed.
> NOT MET otherwise; and if the two runs' host-zone records differ, the part is not judged until the base is taken again at the same commit in the after run's zone record.
> ```
>
> ## Part B - the bridge's identity with the switch off (W1, and each dark commit)
>
> ```rule
> evidence class: identity (not a measurement)
> population: every input and variant of conformance/parse/corpus.manifest
> configuration: bundle sjl-12-001's tool docs/evidence/sjl-12-001/tools/parse-corpus, byte-unchanged and built as this file's header says, in Release against each commit's projects, on one thread with a 256 MB stack, no LANG and TZ=UTC: entry points P and K, and the --dump mode (before and after the compiler's for-loop pass), at W1's first parent and at W1, the switch at its default; the same at each dark commit and its first parent; and every test project of Broiler.JS.slnx at W1 by its CI run; at each dark commit and its first parent, the side-by-side tool's entry points B and K with the switch on are also run, retained and read, not judged here (part C judges the switch-on outputs at the base commit)
> comparison: per input, variant and entry point, the outcome, the exception type and the message; per input, the dumps' hashes
> admitted difference classes: none
> MET if and only if, at W1 and at every dark commit, both runs are complete and identical in every field, and W1's CI run of every test project passed.
> NOT MET otherwise.
> ```
>
> ## Part C - the side-by-side run (SJL-14.2)
>
> ```rule
> evidence class: conformance parity (not a measurement)
> population: every input and variant of the parse corpus; every test of Broiler.JS's test projects; the whole pinned suite as part A selects it
> configuration: at the base commit (W2's first parent, the last state before the cut-over, every dark commit included), one build in Release: the side-by-side tool docs/evidence/sjl-14-001/tools/side-by-side at entry points P (the old parser), B (the bridge) and K (the whole compile, the switch off and on), with the canonical dump of bundle sjl-12-001's tool applied to P's and B's trees; every test project with the switch on through its internal options member; scripts/compliance/run_test262.py locally on the programme's machine through the shell built at that commit with its hidden switch off and on, in UTC, with negative tests, one worker per core; the same runs at W1, retained and read, not judged
> comparison: per input and variant, P against B and K-off against K-on: outcome, exception type, message and dump hash; per test, its result off and on; per test262 path and variant, its verdict off and on
> admitted difference classes: part A's C1 (by K1 to K10), C3 and T; E, by this file's header; and M - a refusal with the same exception type at the same entry point whose message differs, each listed in the bundle's generated message table with both texts (decision D46); and S - a dump difference only in a node's span, where P's span disagrees with the node's source text and B's agrees, listed with both spans checked against the text
> every other difference is unclassified
> MET if and only if every run at the base commit is complete, and every difference is in C1, C3, E, T, M or S and is listed.
> NOT MET otherwise.
> ```
>
> **How it is read.** This is SJL-14.2's done-when, "the side-by-side run is retained with every difference
> classified", judged where it last matters: at the base commit, after every dark merge. The run at W1 is retained so
> that a library fix found there can be traced to its dark merge. An unclassified difference is a bridge or library
> defect, fixed as a dark merge and run again before W2 merges.
>
> ## Part D - the parse corpus across the cut-over
>
> ```rule
> evidence class: conformance parity (not a measurement)
> population: part B's
> configuration: bundle sjl-12-001's tool, byte-unchanged and built as this file's header says, at the base commit and at the after commit, entry points P (at the after commit, the adapter Parser package's FastParser facade, which keeps every member the tool names and throws today's exception types; decision D50) and K, and the --dump mode
> completeness: every input and variant at both entry points at both commits; the manifest's digest equal
> controls: a copy of the base report with one outcome flipped and one message changed, which the comparison must report as two differences; retained
> comparison: scripts/compliance/compare_parse_corpus.py, per input, variant and entry point: outcome, exception type and message; per input, the dump hashes
> admitted difference classes: part C's C1, C3, E, T, M and S
> every other difference is a regression
> MET if and only if both runs are complete, the control is reported, and every difference is in C1, C3, E, T, M or S and is listed.
> NOT MET otherwise.
> ```
>
> **How it is read.** This is SJL-14.1's "old-tree-against-new-tree comparison over the parse corpus". The dump reads
> non-public fields of every `Broiler.*` type, so a field the bridge carries differently shows; class S is the only tree
> difference admitted outside the corrections.
>
> ## Part E - the portable compiler and its Native AOT sample (clause 4; layer L10)
>
> ```rule
> evidence class: publish and run (not a measurement)
> population: Broiler.JS/Broiler.JavaScript.Portable.Tests, which holds Broiler.JavaScript.Portable.Compiler's tests (it has no test project of its own), including the tests the after commit adds for the early errors the portable compiler now reports; the sample Broiler.JS/samples/Broiler.JavaScript.NativeAotSample
> configuration: dotnet test of that project in Release at the after commit; scripts/performance/collect_phase5.py, run locally on linux-x64 at the base commit and at the after commit - the portable compiler run on fibonacci.js with Program.cs's arguments, its output compared byte for byte with the checked-in FibonacciProgram.g.cs, the sample published with Native AOT and run; and a closure listing of Broiler.JavaScript.Portable.Compiler at the after commit (its transitive project and package references, and the assemblies its publish output holds)
> MET if and only if every test passes at the after commit; at the after commit the regenerated program is identical to the checked-in copy, the sample publishes with Native AOT, runs and answers as the collector requires, with no dynamic code; and the closure holds no Broiler.JavaScript.Runtime and no Broiler.JavaScript.Storage.
> NOT MET otherwise.
> ```
>
> ## Part F - the packages (clause 5's first half; layer L11)
>
> ```rule
> evidence class: packages (not a comparison)
> population: every package eng/pack.ps1 packs at the after commit, Broiler.ECMAScript.Frontend among them
> configuration: eng/pack.ps1 and eng/verify-feed.ps1 at the after commit into a local feed; scripts/packaging/test_pristine_consumer.py against that feed; a metadata reader, committed to the bundle, listing the [Obsolete] attribute, its diagnostic identifier and its message on every public type of Broiler.JavaScript.Ast and Broiler.JavaScript.Parser
> controls: the pristine consumer restored with Broiler.ECMAScript.Frontend removed from the feed, which must fail with the missing-package error
> MET if and only if packing and feed verification pass, the pristine consumer passes, the control fails as stated, and every public type dispositions.md marks obsolete carries the mark with the removal version decision D50 names.
> NOT MET otherwise.
> ```
>
> ## Part G - the downstream consumers (clause 5's second half; layer L8)
>
> ```rule
> evidence class: consumer parity (not a measurement)
> population: Broiler.JSeal's test suites and Broiler.HtmlBridge's, at the commits the bundle names, read before the base commit exists
> configuration: two candidate feeds, packed by eng/pack.ps1 at the base commit and at the after commit; Broiler.JSeal's eng/test-package-consumer.ps1 with -CandidateFeed and -CandidateVersion for each, its pins unchanged; Broiler.HtmlBridge in a scratch copy whose package source and Broiler.JavaScript versions are overridden for each candidate, never committed; every test's result retained
> comparison: per test, its result against the base candidate and against the after candidate
> admitted difference classes: none
> MET if and only if both suites ran against both candidates, and every test that passes against the base candidate passes against the after candidate (decision D49).
> NOT MET otherwise; and the part is not judged while either suite has not been run against both candidates.
> ```
>
> **How it is read.** Clause 5, as revised under update rule 5 before any evidence (decision D49), asks that the
> downstream candidate-feed runs lose no test to the cut-over. A test that fails against both candidates is recorded with its cause; the
> JSeal module adapter's reflection on members SJL-12.5 removed is such a cause, and a finding of SJL-12.5's.
>
> ## Part H - the public API (layer L12)
>
> ```rule
> evidence class: API baseline (not a comparison of runs)
> population: every public type and member of every package eng/pack.ps1 packs
> configuration: the API lister docs/evidence/sjl-0-001/api/api-lister.csproj at the base commit and at the after commit
> comparison: per member, present at the base, at the after, or at both with a changed signature or attribute
> MET if and only if every member removed, changed or newly obsolete has a row of docs/public-api.md at the after commit with its SJL-D-25 disposition - (a) forwarder, (b) obsolete with the removal version, or (c) removal with a recorded consumer check - and dispositions.md agrees with it.
> NOT MET otherwise.
> ```
>
> ## Part I - Broiler.JS's parse measurement (clause 6; template T5)
>
> ```rule
> evidence class: measurement
> population: the workloads of docs/evidence/sjl-14-001/inputs/workloads as committed with this rule, the real corpora of Broiler.JS/benchmarks' CompilePhaseMetrics; quantity: the time from source text to the tree the compiler reads, measured on the calling thread (at the base: FastParser.ParseProgram; after: the hop to the parse thread, the frontend's parse, the bridge and the return; decision D47)
> arms: B the base commit's build; A the after commit's; A-again, the A/A lane
> configuration: the harness Broiler.JS/docs/roadmap/Measurement.md names, on the lane the bundle names with its effective configuration; interleaved arms; repetitions and warm-up as Measurement.md fixes them; each lane's condition, that every workload parses to the outcome the base gives, checked before and after
> quantity, noise floor and comparison: Q the median per workload and arm; N = |Q(A) - Q(A-again)|; D = Q(A) - Q(B) against N
> lane: the part is judged only on a controlled lane as Measurement.md requires; on a lane that is not controlled the part is void - neither MET nor NOT MET - and its runs are retained and recorded as void
> MET if and only if the lane is controlled, every condition passes, and for every workload D <= N.
> NOT MET if and only if the lane is controlled and a condition fails or some workload has D > N.
> ```
>
> **What part I decides.** Clause 6 ("Broiler.JS's parse measurement meets its rule.") is read from this part as the
> milestone file states the clause when the part is read: as written, the clause is met only by a MET on a controlled
> lane, and a void part leaves it unmet; what the clause becomes while no controlled lane exists is the owner's own
> choice (decision D48), which this file does not make. No speed claim.
>
> ## Part J - fuzzing (layer L9)
>
> ```rule
> evidence class: robustness (not a measurement)
> population: the seeds docs/evidence/sjl-14-001/inputs/fuzz-seeds.txt names, each with its goal, its count of mutants and the time bound per compile
> configuration: a seeded mutation session, committed to the bundle, compiling each mutant through CoreScript.Compile at the after commit in Release
> reading: each mutant is accepted, or refused with a SyntaxError; a finding is any other exception type, a lost process or a compile past the bound
> controls: a scratch copy in which the bridge throws on one node kind the seeds produce; the session must report a finding; reverted
> MET if and only if no finding is reported and the control is reported.
> NOT MET otherwise.
> ```
>
> ## Part K - the deletion search (clause 2)
>
> ```rule
> evidence class: code fact (not a comparison)
> population: SJL-14.5's deletion list as clause 2 reads it after its revision of 2026-11-03 (decision D50), member by member as dispositions.md lists it: FastParser's partial files, FastScanner's and FastTokenStream's scanning code, FastKeywordMap's table, CharExtensions' scanner members, the parser's half of FastScope, RegExpValidator, DecodeBracedUnicodeEscapes and the parenthesis-balance patch (each deleted, or relocated behind the provider as decision D51 allows and dispositions.md records), the division fallback, the two token re-scans and the direct-eval prefix decision D41 deletes at the cut-over (SyntaxValidation.ContainsLegacyOctalLiteral, DirectEvalSupport.ContainsStrictReservedWordUsage, and DirectEvalSupport's "use strict" prefix with the two places that skip it), and the test-only switch
> configuration: the deletion search script, committed to the bundle with the after commit's change, as bundle sjl-11-001's part E runs its E-1 search: at the after commit, over Broiler.JS/ and Broiler.ECMAScript/ outside docs/, each listed member's declarations and references, every hit listed with its file and line
> controls: a scratch copy of the after commit in which one deleted member is declared again; the search must report it; reverted, and the revert checked
> MET if and only if no listed implementation is found, every hit is a facade member dispositions.md lists as an obsolete forwarder over the bridge or a relocation decision D51 allows, and the control is reported.
> NOT MET otherwise.
> ```
>
> ## Revisions
>
> A rule revised after any run it judges has been seen is a new dated file in this directory. It quotes this one whole
> and says it was written afterwards, and its decision is then weaker evidence.
>
> ## What this file does not decide
>
> - **Clause 1's code fact** ("parse with the shared frontend"), read from the tree at the after commit, which the bundle
>   retains; clause 2 is part K's.
> - **The library**, which the home's bundle `sjl-13-001` judges.
> - **Broiler.VM's clauses.**
> - **A speed claim of any kind.**
