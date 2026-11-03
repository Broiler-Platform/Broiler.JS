# Bundle SJL-14-001's corrections: K1 to K10, the stage classes E and T, and the inherited-context probes

**Written:** 2026-11-03, at milestone SJL-14 (work package SJL-14.1, task 1.1) of the shared JavaScript libraries
programme, with this bundle's [rule](decision-rule.md) and before any change it judges exists. **Read by:** the rule's
parts A (C1 by K1 to K10; T), C and D (C1, C3, E, T, M, S), and `Broiler.JS/CHANGELOG.md` at the cut-over, which
lists each correction by name (decision D45). **Sources:** Broiler.VM's `docs/evidence/sjl-13-001/correspondence.md`
at `c5bde2b` (its section "The rows by class" and each row's table; hand-off H22), Broiler.VM's JSD-0083 (decisions
D44 and D45 as revised on 2026-10-26, D-asi and D-t24 of 2026-10-27), this repository's stage table
`Broiler.ECMAScript/docs/frontend/stages.tsv` and early-error catalogue `early-errors.tsv`, bundle `sjl-13-001`'s
classification (configuration J), bundle `sjl-12-001`'s after baseline (`after/parse-corpus-c.tsv.gz`, entries P and
K), and this bundle's inherited-context probes ([`inherited-context/`](inherited-context/README.md)). Nothing here is
a run of the shared library, and nothing here is a verdict.

## What is here

| File | What | Written by |
|---|---|---|
| this file | the corrections K1 to K10 and what each names; the classes E and T; finding S14-F1; the probes' rows | hand |
| [`corrections/k-rows.tsv`](corrections/k-rows.tsv) | every correction of Broiler.JS by row, with its K, what it corrects, Broiler.JS's answer at the base and its evidence | hand, from the sources above |
| [`corrections/k-paths.tsv`](corrections/k-paths.tsv) | each K with its rows and the test262 paths that show each row: the inputs a row cites, and the inputs `test262-paths.tsv` explains by it; `none` where only probes or vectors reach it | `tools/records/write_records.py` |
| [`corrections/class-e.tsv`](corrections/class-e.tsv) | class E's rows, each with its shared code, its Broiler.JS stage, its sites and the correspondence's rows | the same |
| [`corrections/class-t.tsv`](corrections/class-t.tsv) | class T's rows, each with its sites | the same |
| [`corrections/partly.tsv`](corrections/partly.tsv) | the `syntax` rows Broiler.JS checks partly in its parser and partly in its compiler (finding S14-F1) | the same |
| [`corrections/test262-paths.tsv`](corrections/test262-paths.tsv) | every test262 input and variant of the parse corpus whose expectation at the syntax half (configuration J) differs from this engine's SJL-12 after baseline, with what explains it | the same |

`python3 -I docs/evidence/sjl-14-001/tools/records/write_records.py check`, from the repository root, rebuilds every
generated file byte for byte from its inputs; `write` writes them.

## The corrections K1 to K10 (decision D45)

Each correction is a difference where the shared library follows the pinned text (ES2026, `tc39/ecma262@0248456c`) and
this engine did not (SJL-D-16). Each names its rows in `corrections/k-rows.tsv`, and each row its test262 paths in
`corrections/k-paths.tsv`.

| K | What | Rows |
|---|---|---|
| **K1** | The strict-mode `NonOctalDecimalIntegerLiteral` (`strict-non-octal-decimal`): strict `08`, `09` and `08.5` refused; a legacy octal literal as a strict object key or class element name refused at the parse | T16 (Broiler.JS's half); vectors `numbers/literal/0082`, `0084`, `0088`, `0090` (hand-off H12); probes X08, X09 |
| **K2** | The digit lookahead after `?.` and `.` reads an ASCII digit only (`char.IsDigit` admits others) | T19 (hand-off H6) |
| **K3** | A tagged template's raw segments are not trimmed | T28 (hand-off H8) |
| **K4** | Line terminators in the lexer move to the library; the same in outcome, so no verdict is expected to move | T2 (hand-off H7) |
| **K5** | An invalid regular-expression literal is refused, not read as division (JSD-0081's row S9; the division fallback) | T24i (hand-off H9) |
| **K6** | The braced-escape decoding (`DecodeBracedUnicodeEscapes`) and the parenthesis-balance patch, **only if SJL-11.5 has not landed** at the cut-over (decision D51) | T26, T25 (hand-off H9) |
| **K7** | Early errors the parser missed: the catalogue's `syntax` rows Broiler.JS does not check, or checks on fewer inputs | T23, D33 to D39, D41, D46, D55, B06, P31 (Broiler.JS's half); the catalogue's `not-checked` rows of families EE, EA, EG and EP |
| **K8** | The grammar failures Broiler.JS admits (`docs/evidence/sjl-12-001/catalogue/completeness.md`, section 7) | P07, P09 to P14, P15a, D49 (Broiler.JS's half), P18, P21 to P24, T21, T12 |
| **K9** | The regular-expression rows EE0186 to EE0190 and EE0208, **only if the provider wraps Broiler.Regex's validate entry** | R20 |
| **K10** | The rows SJL-14's correspondence adds | N11, N25, N26, R24, R36, T24a to T24f, D40, D42, D63, P02, P05c, P06a (the call forms' negatives under the flag), P16b (decision D-asi), P26 and D61 (Broiler.JS's half), R12 and R13 (Broiler.JS's tests and corpus inputs that assert acceptance past the limits: class C3, or revised at SJL-14), D11 (the scope-close timing of a declared-name conflict), and this bundle's probe rows IC1 to IC7 |

**Not corrections.** JSD-0080's decision D27's listed refusals are an implementation limit, class C3, enumerated by
differencing the corpus outcomes on the 256 MiB thread the SJL-12 baselines used (decision D47). The rows the
correspondence classes "same in outcome, Broiler.JS refusing only in its compiler" (T15, T27, P08, D12-2, D12-7, D31,
D44, D45, D51 to D54, D59) are class E's, not corrections. A row of engine policy or engine-kept (R04, P05b, P34, P35,
P36 among the corpus's) is no correction: its differences are the request's or the tool's, and `test262-paths.tsv`
names them `row:<row>`.

**Which K a test262 path belongs to** is read from `corrections/test262-paths.tsv`'s last column: a K when the input's
basis in the classification names a row of `k-rows.tsv`; otherwise, for an input expected refused that P and K both
accepted, K9 if every candidate row is a regular-expression row, K8 if every one is a grammar row (family EX), K7
otherwise; and for an input P accepted and K refused, E or T (below), `E-if-coded` (some candidate rows are class E's:
it is class E if the code the library gives is one of them), or `E-partly` (finding S14-F1).

## The stage and timing classes (decisions D45 and D64)

**Class E**, the rows the library checks at the parse and Broiler.JS checks only in its compiler: every `syntax` row of
the stage table whose catalogue `js_stage` names only compiler sites (`SyntaxValidation`, `FastCompiler.EarlyErrors`,
`Visit*`, `DirectEvalSupport`). Each row, with its sites at Broiler.JS `8a95076c` (unchanged at `d6795fc8`), is in
[`corrections/class-e.tsv`](corrections/class-e.tsv): EE0003, EE0004, EE0010, EE0016, EE0017, EE0020, EE0022 to
EE0024, EE0027, EE0030, EE0031, EE0036, EE0045 to EE0049, EE0060, EE0064, EE0068 to EE0071, EE0072, EE0080, EE0085,
EE0088, EE0090, EE0091, EE0095, EE0097 to EE0099, EE0109, EE0111 to EE0113, EE0122, EE0123, EE0127 to EE0129,
EE0135, EE0141, EE0145, EE0147, EE0148, EE0159, EE0163, EE0164, EE0173, EE0174, EE0179, EA0001, EA0002, EG0003,
EG0005, EG0007, EG0008 and EP0016.

**Class T**, those of them whose check ran lazily or after other code ran (`FastCompiler.EarlyErrors`, `Visit*` or
`DirectEvalSupport`, which run as a function body is compiled, deferred bodies when first called, or when eval runs;
the stage table's `js_lazy` yes or partly), each with its sites in
[`corrections/class-t.tsv`](corrections/class-t.tsv): EE0003, EE0004, EE0017, EE0020, EE0022 to EE0024, EE0027,
EE0072, EE0085, EE0088, EE0091, EE0095, EE0099, EE0109, EE0113, EE0141, EE0145, EE0159, EE0163, EE0173, EA0001,
EA0002 and EP0016.

**D63's rows are class F on Broiler.JS's side, not class E**: `StrictModeValidator` never enters a `case` or `default`
clause, so an input inside one is accepted at P and at K today and the library's refusal is a correction (K10), as the
correspondence's F28 says.

### Finding S14-F1: class E as decision D45 worded it misses the rows Broiler.JS checks partly in its parser

Decision D45, as JSD-0083 revised it, defines class E by the row ("the `syntax` rows whose catalogue `js_stage` names
only compiler sites ... for an input P accepts and P or B refuses, K refusing on both sides"). The stage table also has
`syntax` rows whose check Broiler.JS makes partly in its parser and partly in its compiler (`js_compiler_only`
`partly`): EE0005, EE0006, EE0008, EE0012, EE0028, EE0029, EE0033 to EE0035, EE0039, EE0040, EE0062, EE0125, EE0168,
EE0169, EA0010 to EA0012, EG0004, EP0015 and EX0010 ([`corrections/partly.tsv`](corrections/partly.tsv)), and parser
rows whose parser check misses some inputs that the compiler then refuses. For such an input P accepts at the base and
K refuses, and the library refuses at P: exactly what class E is for ("the library checks at the parse what this engine
checked only in its compiler"), but no class of section 4.4's rule admits it, so parts C and D would read each as
unclassified. `corrections/test262-paths.tsv` finds such inputs among test262's parse-negative tests (its `E-partly`
and `E-if-coded` lines, and the correspondence's D31 bucket). **Answer, recorded with the rule (2026-11-03):** decision
D45 is revised the same day, under the owner's direction of 2026-10-18, as JSD-0083 revised D41 and D44 to D47 on the
correspondence's findings: class E also admits an input that bundle `sjl-13-001`'s classification expects refused in
configuration J with that row among the rows whose codes may come first, when the row is one of `partly.tsv`'s or a
parser row the parser missed for that input. The rows of `class-e.tsv` are unchanged. The text replaced, in decision
D45 and in the rule's header, read: "Class E (early-error stage): the `syntax` rows whose catalogue `js_stage` names
only compiler sites (`SyntaxValidation`, `FastCompiler.EarlyErrors`, `Visit*`, `DirectEvalSupport`), for an input P
accepts and P or B refuses, K refusing on both sides". `docs/roadmap/SharedLibraries.md`'s SJL-14 section records the
revision with the decisions.

## The inherited-context probes (decision D44)

Run on 2026-11-03 on both engines, each built from a clean `git archive` of its default branch (Broiler.JS `33d3093b`,
Broiler.VM `c5bde2b`): direct `eval` inside functions, methods, derived and base constructors, field initialisers,
static blocks and a class element's computed key, in strict and sloppy callers, with `new.target`, a super property, a
super call, `arguments`, a declared and an undeclared private name and a strict reserved word; nested and indirect
eval, positions, a hashbang; and `Function`, `GeneratorFunction` and `AsyncFunction` with bad parameters and bodies.
Every probe has both engines' answers, in two forms (the context written in the program, and the context compiled by
an indirect eval), beside the specification's, in [`inherited-context/answers.tsv`](inherited-context/answers.tsv);
[`inherited-context/README.md`](inherited-context/README.md) reads them. The rows they add to K10:

| Row | Broiler.JS today | Probes |
|---|---|---|
| IC1 | In a function that is itself eval code, a direct eval of `new.target` is refused, and `Function` admits a super property (and fails with an `ArgumentNullException` on a super call in its body) | the in-eval form: every `*-NT` probe but global code's, X01, X12, Y02, Y08, Y09 |
| IC2 | Eval in a field initialiser (instance, derived, static, or an arrow in one) is sloppy | IC119, IC126, IC133, IC140 |
| IC3 | `new.target` in an arrow in a field initialiser is the constructor | IC127 |
| IC4 | A static field initialiser's and a computed key's eval do not see the class's private names | IC138, IC152 |
| IC5 | A computed key's eval does not see the enclosing function's `arguments` | IC151 |
| IC6 | `eval('super()')` in a derived constructor after its own `super()`, in a class with fields, is refused at the parse | IC101, IC108 |
| IC7 | Eval in a class element's computed key is sloppy (Broiler.VM answers the same: a correction of both) | IC154 |

They also confirm the correspondence's R24 (IC94), N26 (X05), R36 (X07) and K1 (X09) on today's heads, and show that
**Broiler.JS checks the visible private names at the parse of eval code today**, so the bridge passes them in the
request at W1 (decision D44's condition). Their message texts, positions included, are retained for the generated
message table of class M (decision D46).
