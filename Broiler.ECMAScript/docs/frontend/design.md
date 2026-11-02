# The shared frontend: design

[← Shared libraries](../../README.md) · [Conventions](../conventions.md) ·
[This repository's plan](../../../docs/roadmap/SharedLibraries.md)

**Library:** `Broiler.ECMAScript.Frontend`, which the programme's milestone SJL-13 creates. **Status:** design.
**Filed:** 2026-10-19, at milestone SJL-12 (work package SJL-12.1), before any of the library's code exists.
**Revised:** 2026-10-26, at milestone SJL-13 (work package SJL-13.1), still before any of the library's code exists, to
the library's names, request and result that Broiler.VM's JSD-0083 decides (section 9, and dated revisions of the
sections it changes, each quoting what it replaces; "Corrections" lists them).
**Owner:** the Broiler.JS maintainer, with the JavaScript profile owner. One person holds both roles (Broiler.VM's
exception EX-30), and nothing here has been read by anyone but its author.

**What this document is.** The frontend's design document in its home, one of the three places SJL-12.1 files the
contract: Broiler.VM's JSD-0080, this repository's plan
([`SharedLibraries.md`](../../../docs/roadmap/SharedLibraries.md), "The frontend contract (SJL-12)"), and here. It
records the contract and the decisions behind it, and decides nothing itself.

**What wins.** The programme's area designs, `Broiler.VM/docs/javascript-shared-libraries/areas/frontend.md` and
`areas/static-semantics.md` beside it, and Broiler.VM's JSD-0080,
`Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0080-the-shared-frontend-contract-and-the-in-place-preparation.md`,
are the contract's records. **Where this document and they differ, they win, and this document is corrected to
match**, by a dated note that quotes what it replaces. A decision named "decision Dn" below is JSD-0080's, one of
the owner's decisions of 2026-10-19, taken under the owner's direction of 2026-10-18, "Please do all recommended
decisions and skip waiting for review.", which changes no rule of the programme. *(Noted 2026-10-26, at SJL-13.1:
from SJL-13.1, JSD-0083,
`Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0083-the-shared-syntax-library-and-the-wide-path.md`,
the frontend's area verdict, is among the records that win, with its owner's decisions of 2026-10-26, taken under the
same direction. In section 9 and in every note dated 2026-10-26, "decision Dn" is JSD-0083's, and a decision of
JSD-0080 is named "JSD-0080's decision Dn".)*

**Why there is no project yet.** SJL-12 writes no library code (the milestone's "Does not do"), so
`Broiler.ECMAScript/` has no `Broiler.ECMAScript.Frontend/` folder. A project there would bring the library under
[the conventions](../conventions.md)' scope and rule SL1's layering table before it exists (decision D5). SJL-13
creates it. The early-error catalogue of SJL-12.3 sits beside this document (decision D31). *(Noted 2026-10-26, at
SJL-13.1: there is still no project. SJL-13.2's stage 4a creates `Broiler.ECMAScript/Broiler.ECMAScript.Frontend/` and
`Broiler.ECMAScript.Frontend.Tests/` with SL1's row (decision D6), after the `syntax` vectors' corpus revision, and
the stage table [`stages.tsv`](stages.tsv) now sits beside the catalogue.)* *(Noted 2026-11-01, at SJL-13.2a: "there
is still no project" no longer holds. Stage 4a created
[`Broiler.ECMAScript.Frontend/`](../../Broiler.ECMAScript.Frontend/Broiler.ECMAScript.Frontend.csproj) and
`Broiler.ECMAScript.Frontend.Tests/` with SL1's row, after the `syntax` vectors' corpus revision 30: the request, the
diagnostics' types and a parse entry that answers "not implemented" for every source, under the conventions, the API
baseline and assurance from its first commit; its `PublicAPI.txt` fixes the names section 9 records.)* *(Noted
2026-11-01, at SJL-13.2b: "a parse entry that answers "not implemented" for every source" no longer holds. Stage 4b
added the tokenizer, internal to the library, and the parse entry now refuses a source the lexical grammar refuses, with
the tokenizer's diagnostic, and answers not implemented for every other until the parser of stage 4c; section 9.3's note
says what the tokenizer gives.)* *(Noted 2026-11-01, at SJL-13.2c: "and answers not implemented for every other until the
parser of stage 4c" no longer holds. Stage 4c added the tree, public, and the parser, internal: the parse entry answers
accepted, with the tree, or refused, with the first refusal, for every source; section 9.3's note of 2026-11-01 at
SJL-13.2c says what the tree and the refusals carry.)* *(Noted 2026-11-02, at SJL-13.2d: "or refused, with the first refusal" now holds of the
static-semantic early errors too. Stage 4d added the declarations walk, in `Broiler.ECMAScript.Semantics`: the parse
entry refuses a source any `syntax` row of the stage table refuses, with the row's catalogue code, and answers an accepted
source with the walk's side tables; section 9.3's note of 2026-11-02 says what they carry.)*

## 1. The contract

From the area design's section 4, with decision D41's specifics.

### Input: an immutable parse request

| Field | Content |
|---|---|
| Source | Text, source name, base offset, base line and column (for sub-parses). A leading byte-order mark is the host's to drop *(added 2026-10-26, at SJL-13.1, decision D15)* |
| Goal | `Script` or `Module`. `Eval`, `FormalParameters` and `FunctionBody` are reserved in both engines (decision D41); the last two let `CreateDynamicFunction` parse its pieces as the specification requires, which neither engine does today. *(Noted 2026-10-26, at SJL-13.1: the library carries all five goals from SJL-13.2's stage 4a. No planned Broiler.VM change routes its dynamic function through the last two yet, which the correspondence hands on as H18, and Broiler.JS's bridge passes them at SJL-14.)* |
| Switches | Forced strictness; top-level `await` allowed, **for whichever goal the request sets it**: Broiler.VM sets it for the Module goal only, Broiler.JS for a module and for a script whose top level may await (its `ScriptWithTopLevelAwait` preset, `JSContext.EvalWithTopLevelAwaitAsync` and the shell's retry); with it off, the library reads a top level as a non-async function's body of the same goal (decision D10); Annex B on or off, on for scripts; HTML-like comments on or off (script goal only) *(revised 2026-10-26, at SJL-13.1, decisions D10 and D17; read "Forced strictness; top-level `await` allowed; Annex B on or off; HTML-like comments on or off (script goal only)")* *(Noted 2026-11-01, at SJL-13.2a: "for whichever goal the request sets it" is, in the library's types, for whichever of the Script and Module goals the request sets it, and "on for scripts" is on by default for every goal; section 9.2's note says why.)* |
| Inherited evaluation context | Broiler.VM's evaluation flags (`JsFormat.EvalRequestFlags`): `Strict`, `InFunction`, `InMethod`, `InDerivedConstructor`, `InClassFieldInitializer` and `InClassBody`; an optional set of the visible private names, whose absence leaves the check to the engine, and which Broiler.VM does not pass at SJL-13, since its executor checks an eval program's private names at run time; **no caller's lexical names at SJL-13 or SJL-14**: the conflict they serve is EvalDeclarationInstantiation's, a run-time `SyntaxError` each engine raises today, until SJL-15's model *(revised 2026-10-26, at SJL-13.1, decision D16; read "Broiler.VM's evaluation flags (`JsFormat.EvalRequestFlags`): `Strict`, `InFunction`, `InMethod`, `InDerivedConstructor`, `InClassFieldInitializer` and `InClassBody`; the visible private names; the caller's lexical names, which Broiler.JS needs for direct `eval`")* |
| Proposal flags | Off by default, **one flag per form**, each its own catalogue row: decorators; `accessor`; the import phases' declaration forms `import source x from` and `import defer * as ns from` and their call forms `import.source(…)` and `import.defer(…)`. An engine admits what its own manifest admits: Broiler.VM none (a decorator is refused where the parse reaches it); Broiler.JS what it parses today, decorators, `accessor` and the two call forms *(revised 2026-10-26, at SJL-13.1, decision D17, which refines JSD-0080's single "import phases" parameter; read "Decorators, `accessor`, import phases. Off by default; an engine admits what its own manifest admits")* |
| Lexer extensions | A narrow parameter for non-standard literal suffixes. Broiler.JS's `m` is the only one, and decision SJL-D-17 keeps its `Decimal` meaning in Broiler.JS |
| Limits | The nesting depth, a bounded parameter; the tree depth, a constant of the library; a diagnostic cap; first-error or all-errors. Broiler.VM's today are `SliceParseOptions.DefaultMaximumNestingDepth` within `MaximumSupportedNestingDepth`, the constant `SliceParseOptions.MaximumTreeDepth`, and the cap its wide parser, its numeric admission and its lowering apply. SJL-13 derives the library's tree depth as Broiler.VM's JSD-0022 derives Broiler.VM's. *(Noted 2026-10-26, at SJL-13.1, decisions D11 and D14: the derivation, at SJL-13.2's stage 4e, must admit Broiler.VM's bound on the stack Broiler.VM's `CompilationStack` declares, or the library is reworked with explicit stacks, and `SliceParseOptions.MaximumTreeDepth` stays equal to the library's constant; the nesting depth keeps each engine's default, Broiler.JS passing the library's supported maximum; both engines ask for the first error at SJL-13 and SJL-14.)* *(Noted 2026-11-02, at SJL-13.2e: "SJL-13 derives the library's tree depth as Broiler.VM's JSD-0022 derives Broiler.VM's" is done, in [`limits/derivation.md`](limits/derivation.md): the library spends no stack per level of tree depth, so the constant is Broiler.VM's bound, 10,000; the stack it documents, `ParseLimits.StackBytes`, is `CompilationStack`'s sixteen mebibytes, which the nesting bound is what pays for.)* |
| Providers | Unicode lexical classes, from Broiler.Unicode (SJL-7); regular-expression syntax validation through a seam, the engine's own matcher until SJL-11's Broiler.Regex. *(Noted 2026-10-26, at SJL-13.1, decisions D7, D8 and D32: the Unicode provider is Broiler.Unicode's `IUnicodeTables`, required, with no default; the seam is a validator interface with no package edge, each engine supplying its own; and the request also takes an optional `WorkCharge`. Section 9 says what each answers.)* |

### Output

| Part | Content |
|---|---|
| Tree | Immutable nodes with full spans (offset, length, line, column) and **raw facts**: directive raw text, legacy-octal and escape flags, cooked and raw template chunks, regular-expression pattern and flags, BigInt literal text, parenthesisation, trailing commas. Side facts sit in identity-keyed tables. **No desugaring** (section 2). *(Noted 2026-10-26, at SJL-13.1, decisions D10 and D15: offsets are 0-based and lines and columns 1-based, all in UTF-16 code units, CR LF one line terminator; a module's tree carries its top-level-`await` fact as the specification defines it; and the side tables of the declarations walk are ordered by source position. Section 9 lists them.)* |
| Diagnostics | Section 3 |
| Semantic model | Section 4 |

### Properties

- **Deterministic:** the same request gives the same output, across runs and threads. *(Noted 2026-11-02, at
  SJL-13.2e: held by tests over the parse corpus's stored inputs, a canonical dump of the tree, the token stream, the
  diagnostics and the side tables compared across repeated parses, threads, the reverse order and the cultures tr-TR,
  lt-LT and the invariant culture, and by the Native AOT probe's `--digest-corpus` mode over the whole corpus under the
  JIT and Native AOT; section 9.3's note of this stage.)*
- **No ambient state and no mutable statics** (rule SL2, the shape of Broiler.VM's rule N12; Broiler.VM's JSD-0014
  decision 4, a requirement on the shared front end).
- **Limits are checked before the stack is.** No unbounded recursion: explicit stacks, or depth checks with the large
  thread the caller supplies (`CompilationStack` in Broiler.VM). *(Noted 2026-11-02, at SJL-13.2e: the library takes
  both: depth checks for the parser's recursion, which the nesting bound charges, and loops and an explicit stack where
  the tree is deep or the grammar would recurse between two charged levels, the chain builders, the precedence climb
  and the declarations walk. The thread the caller supplies is of
  `ParseLimits.StackBytes`, `CompilationStack`'s sixteen mebibytes.)*
- **Native AOT compatible** and trimmable; no reflection, no dynamic code.
- **Never a source-to-verified-artifact path.** The frontend produces trees, never artifacts; Broiler.VM's ADR 0010
  forbids such members there.

### The deep-nesting policy

Decided at SJL-12, as the area design's risks table asks (decision D27):
- the tree depth is a constant of the library, derived as JSD-0022 derives Broiler.VM's, against the caller's large
  thread, and the nesting depth is a bounded parameter;
- in both engines, every input deeper than the bound is refused with the contract's nesting diagnostic, which keeps
  Broiler.VM's rule N19 and JSD-0014's decision 5;
- the inputs Broiler.JS accepts today past the bound, through stack segmentation, become listed refusals at SJL-14,
  of class C3 (an implementation limit), enumerated from the parse corpus's baseline;
- the bound's value is SJL-13's measurement. **SJL-12 changes no acceptance of Broiler.JS's.**

## 2. The canonical tree

Decision SJL-D-12, taken on 2026-10-11 and recorded at SJL-12.2: **(a) a new immutable tree seeded from Broiler.VM's
wide design**, because it already meets the properties above and its side tables suit both lowerings; and **(i)**
Broiler.JS's public `Ast` and `Parser` packages become adapter packages that build the old tree from the new one,
marked obsolete and removed in a later major version. Their disposition is SJL-14.4's, in this repository's
[`docs/public-api.md`](../../../docs/public-api.md). Broiler.JS's feature coverage (`accessor`, decorator parsing)
and its cooked template tokens inform the new tree.

**What the tree leaves raw.** Broiler.JS's parser rewrites three things while it parses today (decision D23):
- `for (let …)` and `for (const …)` loops, desugared into blocks with temporaries. SJL-12.5 moves this into a
  preparation pass of Broiler.JS's compiler (decision D22), and the milestone's "does not desugar" means it;
- the synthetic block around a function declaration that is the whole of an `if` clause, under Annex B
  (`FastParser.Statement.cs`);
- a regular-expression literal's `\u{…}` escapes under the `u` and `v` flags (`FastScanner.cs`).

The last two introduce no temporary and restructure no control flow. They stay in Broiler.JS's parser until SJL-14,
and the shared tree does none of the three.

*(Noted 2026-10-26, at SJL-13.1: "They stay in Broiler.JS's parser until SJL-14" no longer holds for the
regular-expression literal's `\u{…}` escapes, the third item.
Broiler.VM's JSD-0081, decision D22, has SJL-11.5 delete the rewrite of a regular-expression literal's `\u{…}`
escapes, with the scanner's parenthesis-balance patch, or SJL-14 if SJL-11 has not landed; the notes JSD-0081 filed in
JSD-0080 and in this repository's plan did not reach this document. The synthetic block Annex B gives a function
declaration that is the whole of an `if` clause stays in Broiler.JS's parser until SJL-14, whose bridge builds it from
the shared tree, as JSD-0080's decision D23 lists it for SJL-14. The shared tree does none of the three.)*

*(Noted 2026-10-25, at SJL-12.5's records: the first has moved. Since item 5 of SJL-12.5
(Broiler-Platform/Broiler.JS#1068, `c29d653f`), Broiler.JS's parser returns a `for` loop as written, and the
compiler's preparation pass, `ForLoopPreparation`, rewrites it before any validation reads the tree; the parser
rewrites the other two, as above.)*

## 3. Diagnostics

Decision SJL-D-11, taken on 2026-10-11 and recorded at SJL-12.2. Each diagnostic carries:
- a stable code;
- a phase: tokenizer, parser or semantics;
- a span and a source name;
- a message identifier with its arguments;
- optionally, a default English rendering.

They come in a deterministic order. **Each engine renders its own text**: Broiler.JS its V8-style messages from the
identifier and arguments; Broiler.VM, whose guests see a generic `SyntaxError`, grades by the code. Neither engine's
guest-visible text is part of the contract.

**The shared codes and identifiers** (decision D30): codes are `JSE` and four digits, grouped by clause; message
identifiers are dotted, such as `scope.lexical.duplicate`.

**The early-error catalogue** (decisions D30 and D31) sits beside this document from SJL-12.3, as
`early-errors.tsv` with `early-errors.md`:
- one row per early-error rule of ECMA-262's ES2026 edition at `tc39/ecma262@0248456c`, in document order, its
  content key the rule's anchor, its productions and a digest of its text, and its id never reused;
- the families EA (eval and dynamic function), EG (Annex B rules that are grammar only, and the parser relaxations
  Annex B allows), EP (proposals either engine parses or test262 tests, each marked admitted or not per engine) and
  EX (an engine's own refusals);
- for each row, where each engine checks it today and the shared code it maps to.

**The mapping onto Broiler.VM's diagnostic registry is Broiler.VM's**, in
`src/Broiler.VM.Profile.JavaScript/docs/diagnostics/frontend-mapping.tsv`, which names the catalogue by its commit
and SHA-256. Under Broiler.VM's JSD-0009, a registry code whose meaning would change gets a new number, never a
changed meaning. At SJL-12 the registry does not change: the mapping records the codes that would split, and SJL-13
mints them (decision D13).

*(Noted 2026-10-26, at SJL-13.1, decisions D11, D12 and D15.)*
- **First error or all errors** stays a parameter, and both engines ask for the first error at SJL-13 and SJL-14: the
  library stops at its first refusal, in its own order, which is a refusal as the parse reaches its construct, and a
  scope's declared-name conflicts when the scope closes, in the source order of the conflicting declarations. All
  errors stays a parameter for an engine that asks for it later, SJL-15's model among them. The library does not
  reproduce either engine's recovery after a refusal.
- **The message identifiers and their arguments** are defined at SJL-13.2's stage 4d, from the catalogue's section 6
  (`early-errors.md`). The cap is a parameter.
- **A diagnostic's span** starts, for each refusal the library takes from Broiler.VM's wide front end, where that front
  end anchored it (a lexical refusal at the construct's start; a parse refusal at the token or the production's start
  its site names; a check taken from Broiler.VM's binder at the node the binder names). Broiler.VM's bundle
  `sjl-13-001` lists the sites. Broiler.JS renders its own text and positions from the span, its texts that hosts and
  tests read kept, "Unexpected await" among them.
- **Broiler.VM's registry takes revision 19** at its step 1's registry commit: one new code per meaning a `split` row
  of the mapping leaves, a meaning being a registry meaning group and not a catalogue condition, and no `split`
  relation left. The catalogue's shared codes keep its conditions apart; the meanings are Broiler.VM's.

## 4. The semantic model

The design the area design's `static-semantics.md` section 3 sketches, filed at SJL-12.1 here and in JSD-0080. The
model is the single source of the language-level facts about a parsed program: scopes, declarations and their
conflicts, Annex B's block-function aliases, the per-function flags, reference resolution with dynamic markers where
`with` or sloppy direct `eval` makes it dynamic, labels, private names, module records, named-evaluation names, free
names and captures, and **every early error that is not purely grammatical**. Each lowering keeps its back-end
decisions: slots and environment records, evaluation scope-map rows, boxing and closure representation, admission,
emission and format ceilings.

```csharp
namespace Broiler.ECMAScript.Semantics;

public sealed class SemanticModel
{
    public IReadOnlyList<Scope> Scopes { get; }                    // in source order
    public Scope ScopeOf(SyntaxNode node);
    public Declaration? DeclarationOf(SyntaxNode bindingIdentifier);
    public Resolution Resolve(IdentifierReference reference);      // Static(scope, declaration) | Dynamic(reason) | Global
    public FunctionFacts FactsOf(FunctionNode function);
    public LabelTarget? TargetOf(BreakOrContinue statement);
    public PrivateNameBinding? Resolve(PrivateNameReference reference);
    public ModuleRecord? Module { get; }
    public string? NamedEvaluationName(SyntaxNode node);
    public IReadOnlyList<Declaration> Captured(FunctionNode function);
    public IReadOnlyList<Diagnostic> EarlyErrors { get; }          // deterministic order
}
```

- **Ordering.** Every collection is in source order. No order comes from a hash table.
- **Identity.** Facts are immutable and keyed by node identity. An ordered list keeps a declaration's multiplicity
  where the language has more than one. The model is computed per compilation unit, after the parse.
- **Inherited context.** Evaluation code takes the caller's scope facts from the request. Broiler.VM passes its
  evaluation flags and the names its scope map records; Broiler.JS passes them from its own direct-`eval` support.
- **One early-error stage.** Broiler.VM's JSD-0014 decision 1, one static-semantics stage over the tree and the only
  place an early error is reported, is a requirement on the shared front end, which SJL-13 and SJL-15 meet.
- **Until SJL-15, each engine keeps its own binder.** At SJL-12.6, Broiler.VM's wide path gains a binder separated
  from its emission, internal to its compiler, deterministic and reproducing today's facts exactly; it is temporary,
  and SJL-15's model replaces it (decision D11). After parsing, the wide path's early errors run in that one stage,
  and the wide parser keeps its own checks until SJL-13 deletes it (decision D10). Broiler.VM's slice path is not
  touched at SJL-12; SJL-15 rebuilds it on the shared tree and model, under decision SJL-D-13 (a).

*(Noted 2026-10-26, at SJL-13.1, decisions D10 and D33.)* **The model begins at SJL-13.2.** The library's syntax half
reports the grammar and every early error either engine's parser reports at parse time today, through a declarations
walk in `Broiler.ECMAScript.Semantics` that exposes each scope's kind and its declarations as side tables (section 9);
SJL-15.2 grows the model from that walk. Until SJL-15, each engine keeps everything else (Broiler.VM's binder,
Broiler.JS's compiler stages), so the requirement "one early-error stage" is met in its shared half at SJL-13 (one
producer per check, and no copy in Broiler.VM's binder) and whole at SJL-15, when the model is the one stage.

## 5. What each engine keeps

| Broiler.VM keeps | Broiler.JS keeps |
|---|---|
| Manifest admission and its "unsupported" refusals (code 2104) | Its `for` per-iteration lowering, moved out of the parser into a compiler preparation pass |
| The diagnostic registry and its mapping from shared codes | Its V8-style message rendering |
| Format ceilings (locals, constants, stack, arguments, BigInt width) | Numeric speculation, boxing and deferral |
| Slots, environment records, evaluation scope-map rows | Its expression-tree emission |
| Byte determinism of artifacts, verification | Run-time evaluation support |
| The slice path's deliberate refusal of unresolvable identifiers (as an admission rule) | The `m` literal's `Decimal` meaning |
| `CompilationStack`'s thread | Stack segmentation, if it keeps it |

*(Noted 2026-10-26, at SJL-13.1, decisions D10 and D18.)* The catalogue's engine-only Policy and Limit rows stay with
their engines, and the library never reports one: Broiler.VM's manifest admission, its format ceilings, its eval
scope depth, its module keys and requests, and an import attribute the host cannot honour (EX0021), which Broiler.VM's
lowering keeps as module-load policy and Broiler.JS's bridge refuses at SJL-14 as its parser does today. The one
Policy row a switch of the request already governs, top-level `await` (EX0023), the library applies. Broiler.VM makes
its BigInt refusals and the module specifier's well-formedness refusal in an admission walk over the shared tree,
after a parse that reported nothing.

## 6. What SJL-12 prepares in each engine

Neither engine's front end is the library yet. SJL-12 prepares both in place, so that SJL-13 and SJL-14 can move
them onto it:
- **Broiler.JS, SJL-12.5** (decisions D19 to D29): explicit, immutable parse options in place of `CoreScript`'s
  ambient state; one temporary counter per parse; an end-of-file token per scanner; no reference from `Parser` to
  `Runtime`, the Unicode and regular-expression types it used moved to `Broiler.JavaScript.Ast` behind Broiler.Unicode's
  `IUnicodeTables`; the `for`-loop desugaring moved into the compiler's preparation pass; and an architecture test
  with a witness. This repository's plan records each decision. *(Noted 2026-10-25, at SJL-12.5's records: every
  item is merged, the last in Broiler-Platform/Broiler.JS#1068 (`c29d653f`), and this repository's plan,
  "The frontend contract (SJL-12)", names each item's change. The reference is absent by assembly: `Parser`'s
  metadata names no `Runtime` assembly, but it still names the moved types' `Broiler.JavaScript.Runtime`
  namespaces, which their forwarders keep, and `Expressions`, which it references, looks up `Runtime`'s `JSValue`
  by name (`JSClassBuilder.cs:26`), as it did before SJL-12.)*
- **Broiler.VM, SJL-12.6** (decisions D10 to D18): deterministic ordering first, in a merge of its own, with the
  standing checks that stage JSB-1 asks for (decision D14); the binder separated from emission; the lowering's
  language early errors moved into it, a binder refusal skipping the lowering; structured template tokens, with the
  same parser parsing substitutions (decision D15); and the property rule N19 states, held by iterative walks and
  checks rows (decisions D16 and D17).

## 7. The evidence

- **The parse corpus** (decisions D32 and D33) lives in this repository at `conformance/parse/` from SJL-12.4: every
  test262 input referenced by path and digest, every other input stored. Broiler.VM holds a byte-identical copy at
  `src/tests/corpus/parse-1/`. Both engines' baselines over it are retained in their bundles.
- **The rules**, each committed before any change it judges: this repository's
  [`docs/evidence/sjl-12-001/decision-rule.md`](../../../docs/evidence/sjl-12-001/decision-rule.md), and Broiler.VM's
  `docs/evidence/sjl-12-001/decision-rule-area.md`.
- **SJL-13's** *(added 2026-10-26, at SJL-13.1)*: the stage table [`stages.tsv`](stages.tsv) beside the catalogue,
  which assigns every catalogue row to the syntax half (`syntax`), a switch of the request the library applies
  (`request`), the engine that keeps it (`engine`) or SJL-15's model (`semantics`); the classification of the parse
  corpus at the syntax half,
  [`docs/evidence/sjl-13-001/classification/`](../../../docs/evidence/sjl-13-001/classification/README.md); the home's
  rule, [`docs/evidence/sjl-13-001/decision-rule.md`](../../../docs/evidence/sjl-13-001/decision-rule.md), which judges
  the library at its library commit; and Broiler.VM's rules for its two steps, `docs/evidence/sjl-13-001/` and
  `sjl-13-002/` there.

## 8. What this document does not decide

- **The library's type and member names.** SJL-13.2's stage 4a fixes them in the library's `PublicAPI.txt`, under the
  contract and the conventions; section 9 records the names, the request and the result JSD-0083 decides *(revised
  2026-10-26, at SJL-13.1; read "**The library's names, types and API.** SJL-13 creates the library, under the
  contract and the conventions.")*.
- **The registry revision**, which is Broiler.VM's: revision 19, at its step 1's registry commit (JSD-0083's decision
  D12) *(revised 2026-10-26, at SJL-13.1; read "**The registry revision**, which is SJL-13's.")*.
- **The `Ast` and `Parser` disposition**, which is SJL-14.4's.
- **The semantic model's implementation**, which is SJL-15's.
- **The tree-depth constant's value**, which SJL-13.2's stage 4e derives, as Broiler.VM's JSD-0022 derived
  Broiler.VM's, and which must admit Broiler.VM's bound; the nesting depth stays each engine's parameter (JSD-0083's
  decision D14) *(revised 2026-10-26, at SJL-13.1; read "**The nesting bound's value**, which is SJL-13's
  measurement.")*. *(Noted 2026-11-02, at SJL-13.2e: stage 4e has derived it, [`limits/derivation.md`](limits/derivation.md):
  10,000, Broiler.VM's bound, admitted on `CompilationStack`'s stack.)*

## 9. The library at SJL-13

*(Added 2026-10-26, at SJL-13.1.)* The names, the request and the result Broiler.VM's JSD-0083 decides, which
SJL-13.2 builds; "decision Dn" is JSD-0083's. Each is the owner's decision of 2026-10-26, taken under the direction of
2026-10-18 quoted above, which changes no rule; the table names the decision, as revised at SJL-13.1 where the
correspondence's findings changed it.

### 9.1 Names (decisions D6 and D9)

| Item | What |
|---|---|
| Project and package | `Broiler.ECMAScript.Frontend`, in `Broiler.ECMAScript/Broiler.ECMAScript.Frontend/`, with `Broiler.ECMAScript.Frontend.Tests/` beside it |
| Namespaces | `Broiler.ECMAScript.Syntax`: the request, the tokenizer, the tree, the parser and the diagnostics. `Broiler.ECMAScript.Semantics`: the declarations walk from SJL-13.2, the model from SJL-15 |
| References | `Broiler.ECMAScript.Algorithms` and `Broiler.Unicode.Algorithms` only: rule SL1's layering row `Frontend → [Algorithms, Broiler.Unicode.Algorithms]`. No `Broiler.Regex` edge (decision D7) |
| Public API | `PublicAPI.txt`, held by the API baseline in both directions, and assurance from the first commit |
| Packing | `IsPackable` false until SJL-13.2's done-when; then published in the shared stream with the owner's approval (decision D30). *(Noted 2026-11-02, at SJL-13.2f, the done-when: `IsPackable` is true, on the shared stream's `0.1.0-preview.N` scheme, the owner's words of 2026-11-01, with package validation as the other shared packages run it; nothing is published.)* |
| `Broiler.ECMAScript.Algorithms` | Unchanged at SJL-13, an added member included: a helper the library needs is internal to it. Literal values come from `NumericLiterals.Evaluate`, of unbounded length; an engine applies its own ceilings |

### 9.2 The request

| Part | What it carries | Decisions |
|---|---|---|
| Source | Text in UTF-16, a source name, a base offset, line and column. A leading byte-order mark is the host's to drop | D15 |
| Goal | Script, Module, eval code, and the dynamic function's parameters and its body | D10 (JSD-0080's decision D41) |
| Switches | Forced strictness. Top-level `await`, which applies to whichever goal the request sets it for; with it off, a top level is read as a non-async function's body of the same goal. Annex B, on for scripts. HTML-like comments, by goal | D10, D17 |
| Inherited context of eval code | Strictness; the function, method, derived-constructor and field-initialiser flags; an optional set of visible private names, whose absence leaves that check to the engine. No caller's lexical names at SJL-13 or SJL-14 | D16 |
| Proposal flags | Off by default, one per form, each its own catalogue row: decorators; `accessor`; `import source x from`; `import defer * as ns from`; `import.source(…)`; `import.defer(…)` | D17 |
| Lexer extension | The only way a non-standard literal suffix reaches the library, Broiler.JS's `m`; Broiler.VM passes none | D17 |
| Limits | The nesting depth, a bounded parameter with each engine's default (Broiler.JS passes the library's supported maximum); the tree depth, a constant of the library derived at stage 4e, with the stack it requires stated beside it, which admits Broiler.VM's bound on Broiler.VM's `CompilationStack`; the diagnostic cap; first error or all errors | D11, D14 |
| Unicode provider | Broiler.Unicode's `IUnicodeTables`, required, with no default. Broiler.VM passes its compiler's tables; Broiler.JS its own | D8 |
| Regular-expression provider | A validator interface, with no package edge; each engine supplies its own (Broiler.VM's over the validator its binder calls today; Broiler.JS's at SJL-14). Its answer is valid, or invalid with the failing condition's message identifier where the validator can tell it and EE0019's `regexp.literal.invalid` where it cannot, with the validator's message as its argument. The library reports it at the literal's span | D7 |
| Meter | An optional `WorkCharge`: nullable, charged before the work, its units and block size documented. Both engines pass none at SJL-13, and the library polls for no cancellation, its work being linear in the source | D32 |

*(Noted 2026-11-01, at SJL-13.2a, where the library's types differ from this table's words; JSD-0083 wins, and the
types follow it.)*
- **Inherited context**, "Strictness; the function, method, derived-constructor and field-initialiser flags": the
  caller's strictness travels as the request's forced-strictness switch, as JSD-0083's decision D16 maps Broiler.VM's
  `Strict` evaluation flag ("The six eval flags map to forced strictness and the function context"), so
  `InheritedContext` carries the four flags and the optional private names, and no strictness of its own. Broiler.VM's
  sixth flag, `InClassBody`, has no field: its meaning, "an enclosing class declares private names the scope map does
  not describe", is the private-name set's absence, which Broiler.VM passes at SJL-13 whatever the flag (decision D16).
- **Goal**, "the dynamic function's parameters and its body": the request also carries the dynamic function's kind
  (`DynamicFunctionKind`: normal, generator, async, async generator), which CreateDynamicFunction's `kind` makes the
  `[Yield]` and `[Await]` parameters of both goals, and which every other goal refuses but its default.
- **Limits**, "the tree depth, a constant of the library derived at stage 4e": the constant is set at stage 4a to
  Broiler.VM's bound, `ParseLimits.MaximumTreeDepth`, since decision D14 holds the two equal and forbids a lower one;
  stage 4e derives the stack it requires and states it beside it. The library's supported maximum nesting,
  `ParseLimits.MaximumSupportedNestingDepth`, which Broiler.JS passes (decision D47), is set to Broiler.VM's supported
  maximum, the least it may be; stage 4e's measurement confirms or raises it.
- **Meter**, "its units and block size documented": one unit per UTF-16 code unit of the source text, the whole text in
  one block, charged before the parse reads it (`ParseRequest.Meter`); at stage 4a the parse reads nothing and charges
  nothing. *(Noted 2026-11-01, at SJL-13.2b: "at stage 4a the parse reads nothing and charges nothing" no longer
  describes the parse. From stage 4b it charges the meter the text's length, in one block, before it reads the first
  code unit, and the tokenizer charges nothing of its own: its work is linear in the text, a BigInt literal being checked
  for its form and its value left to the engine.)*
- **Switches**, "Top-level `await`, which applies to whichever goal the request sets it for" (section 1's switches row
  reads "for whichever goal the request sets it"): the request admits the switch for the Script and Module goals
  alone, the two goals decision D10 names an engine setting it for, and refuses it for eval code and the dynamic
  function's parameters and body when the request is made (`ParseRequest.Switches`). ECMA-262 parses eval code as a
  Script without the `[Await]` parameter, and a dynamic function's two goals take theirs from its kind. Neither engine
  sets it for those goals: Broiler.VM sets it for the Module goal only, and Broiler.JS's
  `JSContext.EvalWithTopLevelAwaitAsync` sets it for its program, a Script, and compiles the eval code and the dynamic
  functions that program reaches without it.
- **Switches**, "Annex B, on for scripts" (section 1's switches row reads "Annex B on or off, on for scripts"): the
  switch is on for scripts, as decision D17 says, and `ParseSwitches.For` sets it on by default for every other goal
  too, the Module goal included, which is the library's own choice beyond D17's words. ECMA-262 applies Annex B's
  regular-expression grammar (B.1.2) in every goal, and the strict-only exclusions (the sloppy-code relaxations, which
  a module's code, being strict, does not take) follow from strictness, whatever the switch.

*(Noted 2026-11-02, at SJL-13.2e, on the table's "Limits" row, "the tree depth, a constant of the library derived at
stage 4e, with the stack it requires stated beside it, which admits Broiler.VM's bound on Broiler.VM's
`CompilationStack`", and on the note above's "stage 4e derives the stack it requires and states it beside it" and
"stage 4e's measurement confirms or raises it".)*
- **The derivation** is [`limits/derivation.md`](limits/derivation.md), with its measurements beside it, taken as
  JSD-0022 took Broiler.VM's: the frames and the bytes each level of the parser's recursion costs, under the JIT and
  under Native AOT, observed at the refusal that ends it, and the least stack each source of the home rule's
  deep-nesting corpus needs, each of its 54 sources parsed alone in its own process. A level of tree depth costs the
  library no frame: the chain builders (binary, member and call, and a decorator's member chain) are loops, charging
  each node before it is built, and the declarations walk keeps its own stack, so every tree of the corpus is parsed and
  walked on 64 KiB. A level of nesting costs from 1 to 10 frames and from 303 to 5,168 bytes, by construct, at the JIT's
  first tier, its optimised tier and under Native AOT. The precedence climb, which read each right operand of a higher
  precedence by an uncharged recursion, up to ten frame pairs between two levels of nesting, reads them with an explicit
  stack since this stage's review, which measured it (the derivation's section 3).
- **The tree-depth constant**, `ParseLimits.MaximumTreeDepth`, stays **10,000**, Broiler.VM's bound, now derived and not
  provisional: nothing in the library bounds it, and decision D14 holds it equal to Broiler.VM's, a test Broiler.VM
  writes when it references the library.
- **The stack the library documents** is a new constant beside it, `ParseLimits.StackBytes`, **16 MiB**, the stack
  `CompilationStack` declares: at the supported maximum nesting the costliest construct measured, alone or after the
  grammar's ten binary precedences, needs 3,416,352 bytes under the JIT (every method at its optimised tier with
  profile-guided optimisation; 2,638,720 at its first tier and in its default runs) and 1,682,944 under Native AOT, a
  margin of about 4.9 and 10.0. Every engine parses on a thread of it,
  as decision D47 has Broiler.JS do at SJL-14.
- **The supported maximum nesting**, `ParseLimits.MaximumSupportedNestingDepth`, is confirmed at **512** and not raised.
- **The left-operand charge** is stage 4c's, in decision D14's four shapes, now tested on the documented stack. The
  derivation's section 5 records what it does not reach: the charge counts the depth along a chain and its left
  operands' spines, not the depth at which a right operand's subtree hangs, so a chain read as the right operand of a
  chain's early nodes (`1 + 2*...*2 + 3+...+3`, or parenthesised, which multiplies with nesting), a chain that begins with
  a prefix operator's operand, an array literal's element or a call's argument, and a parenthesised sequence or
  conditional as a left operand each reach past the constant; Broiler.VM's base does not charge them either, and the
  decision is D14's.

*(Noted 2026-11-02, at SJL-13.2f, on the note above's last item, "each reach past the constant; Broiler.VM's base does
not charge them either, and the decision is D14's".)* **The owner's decision of 2026-11-02**, taken as recommended under
the direction of 2026-10-18 quoted above: every such shape is charged against the tree depth, so that a tree deeper than
`ParseLimits.MaximumTreeDepth` is refused with the contract's tree-depth diagnostic (EX0012) wherever it is built. The
plan's decision D14 reads "the left-operand gap is closed in the library", because keeping it "would put a known hole in
a shared library"; JSD-0083's decision D14 as filed names four shapes, which the charge still refuses past the constant.
Each chain node is now charged its height, one more than the highest of all its children, and refused when that height
passes the constant; nothing else is charged, so the answer is the tree's depth against the constant: every tree deeper
is refused, wherever it is built, and every tree within it is accepted. Any other node's height is its highest child's,
computed once with an explicit stack, so the charge adds no frame per level. The shapes it reaches beyond the four, each
read from the parser and tested at the bound, one past and far past on the documented stack, are listed in the
derivation's section 5 as revised that day: a right operand of a chain's early node, a prefix operator's, `await`'s or
`yield`'s operand, an update's operand, an array or object literal's element, value, spread or computed key, a call's,
`new`'s or `import(…)`'s argument, a computed member's key, a tagged template's substitution, a decorator's expression, a
parenthesised sequence, conditional or assignment, an arrow's or a function's body, a class's heritage or field
initialiser, and `#x in`'s right operand. Chains side by side are charged their deepest, not their sum, and so are a
chain node's own children, a left operand beside a right operand, an argument or a key. **What moves**: a source whose
tree passes the constant in one of these shapes, accepted at stage 4e, is refused; and a source whose tree is within the
constant, which stage 4e refused because it summed a right operand's height with the depth its chain had reached
(`x = a+...+a + (b+...+b)`, 5,001 operands on each side, 5,002 deep), is accepted. No source of the parse corpus moves.
**The effect on Broiler.VM** (its bundle `sjl-13-001`'s part B class L and part A's C3): its base does not charge these
shapes, so an input of one of them past the bound, accepted at its base, moves to refused at SJL-13's step 1, an
implementation limit of class L, never a correction; and its base sums a right operand with its chain's depth as stage
4e did, so an input whose tree is within the bound and which the base refuses for that sum moves to accepted.
Broiler.VM's area rule names the four shapes alone in C3, L and part C's fifth row and admits no move to accepted, and
its JSD-0083 decision D14 takes a dated note of this decision. *(Revised 2026-11-02, after review; the note as first
written read "which the charge still refuses where it did. Each chain node is now charged its height, one more than the
highest of all its children, and a chain begins at its first operand's height;" and "No source within the constant
changes its answer.", and gave Broiler.VM only the move to refused: a chain began at the depth the parse had reached, so
an argument or a right operand beside a deep left operand was charged both, and trees within the constant were
refused.)*

### 9.3 The result

| Part | What it carries | Decisions |
|---|---|---|
| Tree | Immutable nodes with full spans and raw facts, and no desugaring (sections 1 and 2). Offsets are 0-based and lines and columns 1-based, all in UTF-16 code units; every line terminator of the specification starts a line, and CR LF is one. A module's tree carries its top-level-`await` fact as the specification defines it | D15 |
| Tokens | The token stream the parse produced, each token with its raw facts: Broiler.VM's step 1 compares it with its slice tokenizer's (a field map, not the library taking that tokenizer's conventions), and Broiler.JS's bridge builds its token chain from it at SJL-14 | D21 |
| Side tables | From the declarations walk, ordered by source position: each scope's kind and its declarations; each block-level function declaration, with whether it is an `if` clause's; each function's parameter names; each program's and each function's strictness. Neither engine computes them again | D10 |
| Diagnostics | Section 3: a shared code, a phase, a span, the source name, a message identifier with its arguments. Under first error, the first refusal in the library's order; a scope's declared-name conflicts are checked when the scope closes | D11, D15 |

*(Noted 2026-11-01, at SJL-13.2b, on the table's "Tokens" row, "The token stream the parse produced, each token with its
raw facts".)* The tokenizer exists from stage 4b, internal to the library, and the result carries no token stream yet:
which input element a `/` or a `}` begins is the syntactic grammar's goal (ECMA-262 clause 12), which the tokenizer takes
from its caller and never from the tokens it has read, so a whole stream exists only where the parser names each goal,
from stage 4c. Each token carries its kind (an identifier name with the reserved or contextual word its cooked name
spells, never decided a keyword), its offsets, line and column, its cooked value (an identifier's name, a string's SV, a
template piece's TV) with a template piece's TRV, a numeric literal's Number value from `NumericLiterals.Evaluate`, a
regular-expression literal's split between body and flags, and the facts a parser reads: a line terminator before it,
an escape in a name, the legacy octal and non-octal decimal literals and escapes, a template's `NotEscapeSequence`, the
BigInt and `m` suffixes, and an escape or line continuation in a string. A lexical refusal is a diagnostic of phase
tokenizer with the catalogue's shared code (EX0001 to EX0007, EX0025, EE0001, EE0002), at the construct's start.

*(Noted 2026-11-01, at SJL-13.2c, on the table's "Tree", "Tokens" and "Diagnostics" rows, and on the note above's "a
whole stream exists only where the parser names each goal, from stage 4c".)*
- **Tree.** The tree is public from stage 4c, in `Broiler.ECMAScript.Syntax`: `SyntaxNode`, with its `SyntaxKind` and its
  full `SourceSpan`, the source's base applied as a diagnostic's is; `ProgramNode` for every goal, which carries a
  module's top-level-`await` fact as ParseModule's [[HasTLA]] reads it (an `await`, `for await` or `await using` outside
  every function, arrow, field initialiser and static block), and the same fact for a script whose request sets the
  switch; and one sealed class per production the tree keeps, in the `Expression`, `Statement` and `Pattern` families,
  with `FunctionNode` and `ClassNode` shared by their forms. A parenthesis is a `ParenthesizedExpression` node, a raw
  fact that charges no tree depth; an assignment pattern's expression target is a `TargetPattern`, as Broiler.VM's
  `JsTargetPattern`; a method's `FunctionNode` spans the method's source text from its first token after `static`, the
  text `Function.prototype.toString` answers. Its constructors are the library's; its lists are read-only.
- **Tokens.** "From stage 4c" holds of the stream, which the parser reads whole, each token under the goal the grammar
  stands at, and keeps beside the tree inside the library; it is not public at stage 4c: which of its raw facts the
  result publishes, for Broiler.VM's tokenizers-agreement row and Broiler.JS's bridged token chain (decisions D21 and
  D44), is fixed when the first consumer reads it.
- **Diagnostics.** A grammar refusal's code is provisional at stage 4c: JSE9108 `syntax.token.unexpected` or JSE9109
  `syntax.token.missing`, catalogue rows EX0008's and EX0009's, with the offending token's spelling or the expected one
  as a provisional argument, until stage 4d gives each site the row it serves; a limit's refusal has its row's code,
  EX0011's or EX0012's, with its bound as its argument. The static-semantic early errors and the regular-expression
  provider's answer are stage 4d's, so a source only they refuse is accepted at stage 4c.
- **One reading beyond ES2026's words.** A class field's Initializer is read under [~Yield, ~Await], as both engines and
  test262 read it (`x = await;` inside an async function names `await`, and `x = await 1` is refused), where ES2026's
  production is `Initializer[+In, ?Yield, ?Await]`; the parse corpus's classification expects the engines' reading.

*(Noted 2026-11-02, at SJL-13.2d, on the table's "Side tables" and "Diagnostics" rows, and on the note above's "A grammar
refusal's code is provisional at stage 4c" and "The static-semantic early errors and the regular-expression provider's
answer are stage 4d's".)*
- **Diagnostics.** Every refusal carries its catalogue row's shared code and message identifier: the provisional JSE9108
  and JSE9109 are EX0008's and EX0009's, kept where no other row explains the site, and each site that serves a row of its
  own reports that row (the changelog's entry of this stage names them). The arguments each identifier takes are defined
  in the catalogue's section 6 (`early-errors.md`, its note of 2026-11-02). The phase is the pass that finds the refusal:
  `Parser` for a grammar refusal, a limit's, and the early errors the parser checks as it reads (a cover that covers
  nothing, EE0017, EE0025, EE0032, EE0037, EE0038, EE0053, EE0087 and EE0155; an optional chain's template, EE0026; an
  operand that is no LeftHandSideExpression, EE0033 to EE0035; and the rows the changelog's entry names with them);
  `Semantics` for every early error the declarations walk reports, and the provider's refusal. A row two passes check
  carries the phase of the one that reports it: EE0033 to EE0035 are `Parser`'s where the operand is no
  LeftHandSideExpression (`a + b = 1`) and `Semantics`' where it is one whose AssignmentTargetType is invalid (`1 = 2`). The library renders no text:
  each engine renders its own from the identifier and its arguments (section 3), Broiler.JS's texts that hosts and tests
  read, "Unexpected await" among them, kept by its bridge at SJL-14 and every other text class M there (JSD-0083's
  decision D46, which binds SJL-14).
- **The library's order** (decision D11): a construct's refusal is placed at the construct, a scope's declared-name
  conflicts at the scope's close, in the source order of the second declaration; the first in that order is reported
  under the first error, and all of them, up to the request's cap, under all errors. The walk runs over a tree the grammar
  admitted. When the grammar refuses, the walk runs over the top-level statements completed before the refusal, whose
  early errors come first; an early error inside the statement the grammar refuses is not reported, that statement
  having no tree.
- **Side tables.** "From the declarations walk, ordered by source position" holds from stage 4d, public in
  `Broiler.ECMAScript.Semantics`: `ParseResult.Declarations` is a `DeclarationTables` whose `Scopes` give each scope's
  `ScopeKind`, its node and its `Declaration`s (name, `BindingKind`, binding node, whether lexical, whether a block-level
  function and whether an `if` clause's), and whose `Functions` give the program's and every function's strictness and
  parameter names (`FunctionFacts`); null for a refused source.
- **The regular-expression provider** is asked about every literal of a tree the grammar admitted, once each, in source
  order, and a refusal is reported at the literal's span by the row whose message identifier the verdict names (the
  first in catalogue order where rows share it, so that twelve rows are reported by another's code; the changelog's entry
  of this stage lists them), or by
  EE0019 (`regexp.literal.invalid`, with the verdict's identifier as its argument) where no row has it. A verdict of the
  validator's own nesting bound is reported the same way, as EE0019's: its catalogue row, EX0026, is an `engine` row,
  which the library never reports.
- **The `request` row** (EX0023) is the top level's [Await] parameter, as the parser reads it from stage 4c; the walk
  reads it the same way and reports nothing of its own for it.

*(Noted 2026-11-02, at SJL-13.2e, on the contract's "Deterministic" property and on the evidence the home rule's parts C,
F and G read.)*
- **Determinism.** The library's tests compare a canonical dump of each parse (`runners/SyntaxDump.cs`: every node's
  kind, span, raw facts and children in tree order, the token stream, the ordered diagnostics and the side tables) over
  every input the parse corpus stores, under each variant in both of the home rule's configurations and as eval code:
  the same bytes in a repeated parse, on another thread, in the reverse order, on two threads at once, and under the
  cultures tr-TR, lt-LT and the invariant culture. The Native AOT probe's `--digest-corpus` mode writes the dump's
  SHA-256 for every parse of the whole corpus, test262 included, and its output under the JIT and from the Native AOT
  image are the same bytes, in the manifest's order, the reverse order and on two threads. The dump reads the tree's
  children and the token stream, which stay internal: the library grants the probe its internals, as it grants its
  tests, for this alone.
- **The home's fuzz target** (decision D22) is the probe's `--fuzz` mode, no new project: it reads the seeds the home
  rule's bundle commits, makes each line's mutants from a fixed deterministic stream, parses each under its request on a
  thread of `ParseLimits.StackBytes`, and reports as a finding an exception escaping the library, a parse past the seeds
  file's time bound, a result with neither a tree nor a diagnostic, or a diagnostic whose code no catalogue row carries.
- **The deep-nesting corpus's runner** is the probe's `--parse-one` mode, one source parsed alone in its process with
  an exit code for its outcome, driven per row by `docs/evidence/sjl-13-001/tools/deep_nesting.py`.

### 9.4 What the syntax half reports (decision D10, the union)

- **The grammar, and every early error either engine's parser reports at parse time today**: Broiler.VM's `JsParser`
  family, and Broiler.JS's `FastScopeItem.AddVariable` and its parser's other checks; a regular-expression literal's
  validity, asked of the provider.
- **The catalogue's engine-only rows, by their kind**: Grammar rows and the contract's two limits are the library's; a
  Policy row a switch of the request governs is applied by the library (EX0023, top-level `await`, which has no
  diagnostic of its own); every other Policy or Limit row stays with its engine, and the library never reports it.
- **Everything else** stays with each engine until SJL-15's model.

The stage table [`stages.tsv`](stages.tsv) records the assignment for every catalogue row (`syntax`, `request`,
`engine` or `semantics`), with each engine's current stage and site.

## Corrections

*(Revised 2026-10-26, at SJL-13.1; it read "None.")* Each revision of 2026-10-26 above quotes what it replaced:
- the header gains a "Revised" line, and "What wins" names JSD-0083 among the records that win;
- "Why there is no project yet" says what creates the project, and that the stage table sits beside the catalogue;
- in section 1's input table, the source row gains the byte-order mark, the goal row a note, the switches, inherited
  context and proposal-flag rows are revised, and the limits and providers rows gain notes; the output table's tree row
  gains a note;
- section 2 gains the note on "They stay in Broiler.JS's parser until SJL-14" (the literal's braced escapes are
  SJL-11.5's, or SJL-14's if SJL-11 has not landed: Broiler.VM's JSD-0081, decision D22);
- sections 3, 4 and 5 gain notes; section 7 gains SJL-13's records; section 8's first, second and last items are
  revised;
- section 9 is new.

*(Added 2026-11-02, at SJL-13.2f.)* Section 9.1's packing row gains the note that the library is packable; section 9.2
gains the note on the owner's decision of 2026-11-02 on decision D14's residual shapes, each chain node charged its whole
height, and its effect on Broiler.VM's class L. The derivation's section 5 is revised the same day.

*(Added 2026-11-02, at SJL-13.2e.)* Section 1's limits row, its "Deterministic" and "Limits are checked before the stack
is" properties and section 8's last item gain notes that the derivation is made; section 9.2 a note on the derivation,
the tree-depth constant, the documented stack, the supported maximum nesting and the left-operand charge; section 9.3 a
note on determinism, the fuzz target and the deep-nesting corpus's runner. The derivation is new,
[`limits/derivation.md`](limits/derivation.md), with its measurements.

*(Added 2026-11-02, at SJL-13.2d.)* "Why there is no project yet" gains the note that the parse entry reports the early
errors, and section 9.3 a note on the diagnostics' codes, identifiers and phases, the library's order, the side tables,
the regular-expression provider and the `request` row.

*(Added 2026-11-01, at SJL-13.2c.)* "Why there is no project yet" gains the note that the parse entry no longer answers
not implemented, and section 9.3 a note on the tree, the token stream the result still does not publish, the provisional
codes of the grammar's refusals, and the class field initialiser's reading.

*(Added 2026-11-01, at SJL-13.2b.)* "Why there is no project yet" gains the note that the parse entry no longer answers
not implemented for every source, section 9.2's meter note a note that the parse charges from stage 4b, and section 9.3
a note on the token stream the result does not carry yet and what each token gives.

*(Added 2026-11-01, at SJL-13.2a.)* "Why there is no project yet" gains the note that the project exists, and
section 9.2 a note on the six places the library's types differ from its table, each quoting its words, and section
1's switches row a note on the two of them that are in that row too.
