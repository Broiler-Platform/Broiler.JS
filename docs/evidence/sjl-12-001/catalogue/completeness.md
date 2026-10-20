# The early-error catalogue's completeness (SJL-12.3, task 3.11)

**Filed:** 2026-10-20, with the merge of the fill. **Catalogue:**
[`Broiler.ECMAScript/docs/frontend/early-errors.tsv`](../../../../Broiler.ECMAScript/docs/frontend/early-errors.tsv)
at this commit, 278 rows. **Edition:** ES2026, `tc39/ecma262@0248456c` (`spec.html` SHA-256 `ce7bc301...`).
**Heads read:** Broiler.VM `d82b07a` (its compiler and format sources equal `main` `046357b`), Broiler.JS `ec494f66`
(`Broiler.JS/` equals `main` `1f91b8f1`), test262 `ccaac100`. **Owner and reviewer:** as the bundle's README says;
nothing here has been read by anyone but its author.

This is the milestone's done-when, "every early error has a row", checked three ways (plan task 3.11, risk R20). It
judges the catalogue, not the engines; what it found about the engines is in section 7.

## 1. Result

| Check | Population | Misses |
|---|---|---|
| (a) every rule the extractor finds is a row | 41 "Static Semantics: Early Errors" clauses, 213 rules; 29 other clauses of the edition that name SyntaxError | none |
| (b) every Broiler.VM emission site maps to a row | 217 lines of the compiler that name a registry code (wide parser, lowering, slice); 61 regular-expression refusals behind one of them | none |
| (b) every Broiler.JS raise site maps to a row | 404 throws in the parser, the compiler and the Ast project: 400 of a SyntaxError, and 4 of a ReferenceError or a TypeError | none (the one TypeError, in emitted code, refuses no source and names no row) |
| (c) every test262 parse-negative test is explained by a row | 4,595 tests, 8,535 variants | none |

**Rows added:** EX0025 to EX0031, in the tool's table (`scripts/frontend/ee_extract.py`), each with its block's next
code and filled here (section 5). **Row corrected:** EX0005's `vm_wide_site` lost `SliceTokenizer.cs:814`, which is
EX0025's. No other cell of the fill was changed.

## 2. The merge

- `group-1.tsv` to `group-9.tsv` (the fill agents' scratch files) hold 271 rows: every row of the skeleton exactly
  once, in its group's order, with the committed header. Columns 1-11 equal the skeleton's in every row, and
  `ee_extract.py check` over the merged file finds no difference. No cell is empty or `unfilled`.
- **The fill's vocabulary is kept as the groups wrote it, and `early-errors.md` section 4 now says so:** the committed
  `status` scheme (an engine that refuses part of a row counts, and the row's notes say "partial" or "gap");
  `runtime` and `lowering+runtime` stages for the EA steps the executors check; `-` in `vm_code` where Broiler.VM
  emits no registry code; executor sites written from `src/`.
- **Message identifiers.** 134 in all: 40 bound before the fill and 94 coined by it under rule 5. No two name one
  condition. The coined ones that span rows of different text are listed in `early-errors.md` section 6. The groups'
  open questions are settled as they proposed: EE0127 to EE0129 keep three identifiers; EE0179 takes
  `scope.lexical.duplicate`; EE0064 and EG0003 take `strict.labelled-function`; EG0005 and EG0007 stay in the
  `annexb` namespace.

## 3. Direction (a): from the rules to the rows

- **The extraction.** `ee_extract.py check` (edition and `main` as its header names them) over the committed file:
  278 rows extracted, 0 differences in the identifying columns. The 41 sections whose heading says Early Errors each
  hold at least one extracted rule, and the tool's own count check holds (every "Syntax Error" sentence of those
  clauses lies inside an extracted rule). `diff` over the es2026-errata tag (`d89c03f2`): no rule added, removed or
  changed.
- **Rules outside the Early Errors clauses (R20).** [`spec-syntaxerror-clauses.tsv`](spec-syntaxerror-clauses.tsv)
  lists every other clause of the edition whose own text names SyntaxError, 29 of them, with the rows or the reason
  that covers each: PerformEval, EvalDeclarationInstantiation and CreateDynamicFunction are EA0001 to EA0012, step by
  step; ParseText, ParseScript, ParseModule and ParsePattern carry the grammar and early errors that are rows (Annex
  B's ParsePattern is EG0002 and EE0209 to EE0213); the rest are run-time operations, module loading and linking, or
  prose, which `early-errors.md` section 2 excludes, and clause 17, which bounds what an engine may refuse early.
- **One refusal the edition does not make.** test262's `test/built-ins/Function/private-identifiers-not-empty.js`
  expects `new Function("o.#f")` to throw a SyntaxError and cites CreateDynamicFunction steps the edition does not
  have (nor does `main` at `4feb0272`); AllPrivateIdentifiersValid is applied only by ScriptBody, ModuleBody and
  PerformEval. Both engines refuse it, by compiling the assembled source as a Script. It is EX0031 (group 8 found it).

## 4. Direction (b): from the engines' sites to the rows

**How the sites were found, at the heads, not from a list.** [`check.py`](check.py) re-derives each population:
- **Broiler.VM** ([`vm-sites.tsv`](vm-sites.tsv)): every line of `src/Broiler.VM.Profile.JavaScript.Compiler/*.cs`
  that names a registry code (`SliceSourceDiagnosticCode.<name>`), except the enum's own file and
  `SliceSourcePrograms.cs` (the slice's sample programs, which name the codes they expect). Every refusal helper
  (`Refuse`, `RefuseNumeric`, `RefuseStrictReservedIdentifier`, the manifest helper at `JsParser.cs:7364`,
  `JsNumericAdmission`'s) names its code inside itself, so these 217 lines are every emission. The population equals
  the survey's `vm_sites.py` at the same head. The two relays of a template substitution's diagnostics
  (`JsParser.cs:6918`, `:6959`) re-emit another site's code and are not sites.
- **Broiler.VM's regular expressions** ([`vm-regexp-sites.tsv`](vm-regexp-sites.tsv)): every
  `new JsRegExpSyntaxError(` of `src/Broiler.VM.Profile.JavaScript.Format/JsRegExpMatcher.cs`, 61 lines, each reported
  by the lowering as 2101 at `JsCompiler.Literals.cs:447`.
- **Broiler.JS** ([`js-sites.tsv`](js-sites.tsv)): every `throw` of `Broiler.JavaScript.Parser`,
  `Broiler.JavaScript.Compiler` and `Broiler.JavaScript.Ast`, and every call of the three `ThrowIf*` helpers: 479
  lines. The survey's `js_sites.py` matched a list of idioms in two projects; this counts every `throw`, which adds
  the Ast project's two refusals (`AstUnaryExpression.cs:31`, `VariableDeclarator.cs:43`, group 2). 400 raise a
  SyntaxError (383 throws and 17 helper calls); 4 raise a ReferenceError or a TypeError, one at compile time
  (`FastCompiler.VisitMeta.cs:66`, EX0024) and three in code the compiler emits (B.3.9's run-time ReferenceErrors,
  EG0008, and an iterator-protocol TypeError, no row); 75 are not refusals and say why (68 internal .NET exceptions,
  5 keyword-table entries, 2 in `ErrorHandler.cs`, which nothing calls). The regular-expression validators of
  Broiler.JS raise nothing: they answer `false`, `FastScanner.cs:1453` declines the token, and the refusal surfaces
  at a parser site.

**How each site maps to rows** (the `basis` column):
- `cell`: a row's `vm_slice`, `vm_wide_site` or `js_site` cell cites the line (Broiler.VM 148 sites, Broiler.JS
  147);
- `assigned`: this check opened the site and assigned it, with the reason in the `reason` column (Broiler.VM 53,
  Broiler.JS 248, of which 133 are the parser's generic `throw stream.Unexpected()` and `throw Unexpected()` sites,
  assigned to EX0008);
- `cell+assigned`: cited by a row and given another row too, where the site also refuses something else (Broiler.VM
  16, Broiler.JS 9). For example `JsParser.cs:6604`, the binding-name fall-through, is EE0005 to EE0009 and also
  refuses `var if`, a grammar failure (EX0008); `JsParser.cs:4312` is EE0064 and refuses every declaration as a
  statement body.

Every site names at least one row, and every row it names exists. The assigned sites are mostly grammar (EX0008,
EX0009), second checks behind a parser check (the module collisions at `JsCompiler.Modules.cs:368`, `:398`, `:406`;
`FastCompiler.cs:440`), defensive checks (`JsCompiler.Patterns.cs:173`, the slice lowering's
`SliceSourceCompiler.cs:458`, `:474`, `:844`), and the seven new rows' sites. The three 2104-coded language rules
of JSD-0080 decision D12 are `JsCompiler.Expressions.cs:314` (EX0008, EE0140), `JsCompiler.Operators.cs:770`
(EE0035) and `JsCompiler.ResourceScopes.cs:278` (EP0006 to EP0008); 2207 for `return` in a static block
(`JsCompiler.Exceptions.cs:275`) is EX0008, a grammar failure.

**Outside the population, and why.** Run-time SyntaxErrors of the executors are not parse or early-error refusals
and are not rows unless an EA row names them: Broiler.VM's `JsEngine.Eval.cs` and `JsEngine.EvalDeclarations.cs`
(EA0001, EA0006, EA0007, EA0009 cite theirs; GlobalDeclarationInstantiation's at `:296`, `:301`, `:366` are section
2's exclusion), `JsEngine.Imports.cs:419` (module linking), the RegExp, JSON, BigInt and base64 built-ins; Broiler.JS's
`JSContext.cs:1026`, `:1042` (GlobalDeclarationInstantiation), `JSModuleContext.cs` (loading and linking) and its
built-ins. The executors' EvalErrors (`JsEngine.Eval.cs:80` and the rest) are not SyntaxErrors.

## 5. Rows added by this check

| Id | Code | Identifier | What it is | Sites |
|---|---|---|---|---|
| EX0025 | JSE9111 | `syntax.identifier.escape.invalid` | An identifier's `\u` escape that names no code point | VM `SliceTokenizer.cs:814` (2005); JS `FastScanner.cs:1751`, `:1757`, `:1761`, `:1767`, `:1778` |
| EX0026 | JSE9207 | `limit.regexp.too-deep` | Broiler.VM's 512-deep bound on a pattern's groups and classes | VM `JsRegExpMatcher.cs:1818`, `:2617`, reported at `JsCompiler.Literals.cs:447` (2101) |
| EX0027 | JSE9208 | `limit.json-module.too-deep` | Broiler.VM's 1024-deep bound on a JSON module | VM `JsCompiler.cs:958` (2103) |
| EX0028 | JSE9209 | `limit.bigint-literal.too-wide` | A BigInt literal wider than the format's 8192-byte constant | VM `JsParser.cs:7364` from `:6331`, `:6348` (2104) |
| EX0029 | JSE9309 | `engine.module-specifier.ill-formed` | A module specifier holding a lone surrogate, which the edition admits | VM `JsParser.cs:1418` (2101) |
| EX0030 | JSE9310 | `engine.request.unsupported` | A compile request the engine cannot serve (an output form, a backend) | VM `JsCompiler.Assembly.cs:456`, `:470`, `:481`, `:497` (2104) |
| EX0031 | JSE9311 | `engine.dynamic-function.private-name` | A private name in a dynamic function's source, which test262 expects refused and the edition does not refuse | VM `JsCompiler.Classes.cs:420` (2206); JS `SyntaxValidation.cs:1234` |

EX0025, EX0026 and EX0029 to EX0031 were proposed by groups 6, 7, 8 and 9; this check added EX0027, EX0028 and EX0030
from group 9's list of unexplained sites. Each row's cells are filled like the groups' rows, with the sites opened at
the heads.

**Not rows: defects.** A check that refuses more than its rule is an engine defect, not an engine-only refusal of
the kind EX holds (`early-errors.md` section 2). Its site maps to the row of the rule it implements, and it is listed
in section 7.

## 6. Direction (c): test262's parse-negative tests

**The population.** Every test of test262 `ccaac100` whose front matter says `phase: parse`: 4,595 tests, the same
list as the critic's `parse-neg.txt`. Variants follow `INTERPRETING.md` with block-style flag lists read (8,535: 4,262
strict, 4,046 sloppy, 205 module, 22 raw).

**How each test was explained.** Both engines were built from the heads (`git archive` of `d82b07a` and `ec494f66`
into a scratch directory) and run over every variant by the two probes in [`probes/`](probes/):
- Broiler.VM's wide front end (`JsCompiler.Compile`, wide manifest, bytecode form; module graphs loaded as the
  survey's probe did), with the compiler instrumented by [`probes/vm/instrument.py`](probes/vm/instrument.py) so that
  each diagnostic records the stack that made it. Every edit stays within one line, so every line number is the
  head's. The first diagnostic's site is read from the stack, and a regular-expression refusal also gives its
  `JsRegExpMatcher.cs` line. Codes and positions equal the survey's VM run on all 8,528 variants both ran.
- Broiler.JS's parser alone and `CoreScript.Compile`, with a first-chance exception hook giving the line of the
  original throw. Outcomes equal the survey's Broiler.JS run on the same 8,528 variants.

A test's explanation is the union of the rows of its first refusing site in each engine (from the two inventories)
and the rows whose `test262` cell names it. [`parse-negative.tsv`](parse-negative.tsv) holds every test with its
variants, its `esid`, those three sets, and both engines' first sites.

**Result.** Every one of the 4,595 tests is explained by at least one row: 356 are named by a row's `test262` cell;
3,909 more are refused at a site of an EE, EA, EG, EP or engine-limit or policy row; the remaining 330 are grammar
failures (EX0001 to EX0010, EX0025), and each was read: numeric and BigInt literals that match no production (EX0003),
`import()` with no argument or under `new`, imports and exports below a module's top level, private names where no
production admits them, unary operators before `**`, `??` beside `||`, and the like. No test is explained only by an
engine's limit or policy.

**The review of the `esid`.** 857 tests name, in `esid`, a clause where the catalogue has rows. 331 of them are
explained by rows of another clause. Every one was read, by group:
- 192 class-body tests of `delete` of a private reference are EE0030 and EE0031 (the delete operator's rules; the
  `esid` names the class clause);
- 79 regular-expression tests are pattern grammar failures, EE0019 (modifiers with no colon or with an escape,
  quantified lookbehinds);
- 28 `ident-ref-*`, label and keyword tests are grammar failures (EX0008, EX0009);
- 12 object-literal methods named by a private name are EE0016, a clause apart from method definitions';
- 11 `await` bindings in a class static block are EE0009 (the block's statement list is [+Await]);
- 4 are grammar failures that test262 files under a field initialiser's rules (an arrow with a block body as the left
  operand of `==`);
- the last 5 are EE0005 (`arguments` as a strict async generator's parameter), EE0008 (`yield` naming an async
  generator expression), grammar failures (`for (#x in [])`, a `-->` after a comment's extra text) and Annex B's
  `for (a = 0 in {})`, which is no B.3.5 form (Broiler.VM refuses it as EE0054's invalid target).

None is an early error the catalogue lacks.

## 7. What the check found about the engines

These are findings, not rows; each names the row whose rule it bears on. The groups' notes found most of them; the
probes confirmed those test262 reaches.

**Refusals the edition does not make (defects):**
- Broiler.JS refuses duplicate parameters in every generator and async function, sloppy simple lists included
  (`SyntaxValidation.cs:787-791`; EE0068, EE0097, EE0111, EE0147; group 4).
- Broiler.JS refuses a `var` in a class static block whose name an outer lexical declaration has
  (`FastScopeItem.cs:188`, the site of EE0134 and its siblings; group 5), `export { async }` and `export { using }`
  (`FastParser.Export.cs:208`; EE0182; group 6), `using` and `await using` at a module's top level
  (`FastParser.Statement.cs:652-653`; EP0008;
  test262's positive `using/syntax/using-allowed-at-top-level-of-module.js` and its siblings; group 9), a line break
  after a comma in an import attribute list (`FastParser.Import.cs:279`; EX0008; group 6), a sloppy direct eval's
  `var` named like a destructuring catch parameter (`FastFunctionScope.cs:771`; EA0007; group 8), and lexical
  declarations named `arguments` at the top of a parameter-initialiser eval (`DirectEvalSupport.cs:774-788`; EA0007,
  EE0005; group 8).
- Both engines refuse `L1: L2: while (c) { continue L1; }` (Broiler.VM `JsCompiler.Exceptions.cs:477`, `:509`;
  Broiler.JS `SyntaxValidation.cs:539`, `:559`; EE0082 and its siblings; group 6).
- Broiler.VM refuses a regular-expression group name repeated in separate alternatives
  (`JsRegExpMatcher.cs:1612`; EE0184; group 7), which refuses positive test262 tests such as
  `test/built-ins/RegExp/named-groups/duplicate-names-*.js`.

**Gaps.** 42 rows are `vm-only` (Broiler.JS does not refuse what they refuse) and 4 `neither`; no row is `js-only`.
Partial checks are in the rows' notes ("partial", "gap"), including Broiler.JS's switch-case blind spot in its strict
validator (`SyntaxValidation.cs:683`, group 3), and Broiler.VM's, all by reading: import specifiers without `as`
(EE0006, EE0007, EE0012 to EE0014), legacy-octal numeric property keys (EE0003), `delete o?.#x` (EE0030, EE0031),
EE0037, EE0039, a default export re-exported as `default` (EE0171, EE0182), the Function constructor's parts parsed
only together (EA0010, EA0011) and quantifier bounds above `int.MaxValue` (EE0185). Grammar failures Broiler.JS
admits are recorded on the EX rows' and the groups' notes (a `const` declaration as a statement body, a class heritage
parsed as an Expression, `if (a {}`, `\u{00_76}`, quantified lookbehinds and v-mode reserved punctuators).

**Crashes, which are not refusals.** Broiler.JS's compile throws `NotImplementedException` on destructuring forms with
a rest initialiser or a literal rest target (`[...x = 1] = []`, `([...x = []]) => {}`, `({...{a}} = o)`) and on
`for (a = 0 in {})` (`FastCompiler.VisitAssignmentExpression.cs:1116`; groups 2, 4 and 8). Such tests are explained by
Broiler.VM's refusal.

## 8. Files

| File | What it holds |
|---|---|
| [`check.py`](check.py) | Re-derives every population above from the trees and the suite and checks the inventories against them and against the catalogue; exits 1 on any miss |
| [`vm-sites.tsv`](vm-sites.tsv) | Broiler.VM's 217 emission sites: method, code, message, rows, basis, reason |
| [`vm-regexp-sites.tsv`](vm-regexp-sites.tsv) | Broiler.VM's 61 regular-expression refusals behind `JsCompiler.Literals.cs:447` |
| [`js-sites.tsv`](js-sites.tsv) | Broiler.JS's 479 throw sites: what each throws, rows, basis, reason |
| [`spec-syntaxerror-clauses.tsv`](spec-syntaxerror-clauses.tsv) | The edition's 29 other clauses that name SyntaxError, with rows or the reason |
| [`parse-negative.tsv`](parse-negative.tsv) | test262's 4,595 parse-negative tests: variants, `esid`, the rows that name each, both engines' first sites |
| [`probes/`](probes/) | The two scratch probes and the instrumentation that produced the first sites; never built in this tree |

To re-run the check (standard library only; the probes need the .NET 10 SDK and scratch copies as their headers say):

```
python3 -I docs/evidence/sjl-12-001/catalogue/check.py --vm <Broiler.VM at d82b07a> --js . \
    --test262 <test262 at ccaac100> --catalogue Broiler.ECMAScript/docs/frontend/early-errors.tsv \
    --edition <es2026 spec.html>
```

On 2026-10-20 it printed "217 VM emission sites, 61 VM regular-expression sites, 479 Broiler.JS throw sites, 4595
parse-negative tests; 0 problems". A copy with one VM site removed and one Broiler.JS site's rows emptied was refused
with exactly those two problems.
