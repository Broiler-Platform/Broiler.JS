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
and its cooked template tokens inform the new tree. *(Noted 2026-11-03, at SJL-14.1: the disposition is decided, type by type and member
by member, in bundle `sjl-14-001`'s [`dispositions.md`](../../../docs/evidence/sjl-14-001/dispositions.md) by
decision D50 of 2026-11-03 (`docs/roadmap/SharedLibraries.md`, "Broiler.JS on the shared syntax library (SJL-14)"):
`Parser` a facade over the bridge, `Ast`'s tree types obsolete, both with removal version `1.0.0`, and the non-tree
types moved to `Broiler.JavaScript.Expressions` with forwarders. SJL-14.4 applies it, and records it in
`docs/public-api.md`.)*

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
- **The `Ast` and `Parser` disposition**, which SJL-14.1 decides and SJL-14.4 applies *(revised 2026-11-03, at
  SJL-14.1; read "**The `Ast` and `Parser` disposition**, which is SJL-14.4's.")*.
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

*(Noted 2026-11-04, by the owner's decision of that day, "Expose + preview.10", on the table's parts, which name no
switch for the result's tokens.)* The request gains `ParseRequest.RetainTokens`, false by default, which decides only
whether the result publishes the token stream the parse read as `ParseResult.Tokens`, for Broiler.VM's
tokenizers-agreement row (decision D21); the parse is the same either way. Section 9.3's note of 2026-11-04 records it.

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

*(Noted 2026-11-04, by the owner's decision of that day, "Expose + preview.10", on the note of 2026-11-01 at SJL-13.2c,
"it is not public at stage 4c: which of its raw facts the result publishes, for Broiler.VM's tokenizers-agreement row and
Broiler.JS's bridged token chain (decisions D21 and D44), is fixed when the first consumer reads it", and on the note of
2026-11-02 at SJL-13.2e, "The dump reads the tree's children and the token stream, which stay internal".)* The first
consumer is Broiler.VM's SJL-13.3 step 1, whose tokenizers-agreement row (its bundle `sjl-13-001`'s rule part F, decision
D21) compares its slice tokenizer's stream, token by token, with the stream the library's parse of the same text under
the same goal produced, so that each `/` is read as the parser's goal read it. The published 0.1.0-preview.9 keeps that
stream internal, so the owner decided to publish it with the least surface the row needs, as a library change of its own
that the shared stream's next version, 0.1.0-preview.10, carries:
- **The switch.** `ParseRequest.RetainTokens`, false by default. The parse reads the same tokens under the same goals and
  makes the same tree, side tables and diagnostics either way, and keeps the stream beside the tree as before; the switch
  decides only whether the result hands it out. The result holds the stream it kept and a flag beside its outcome, where
  its layout has room, so that it allocates the same objects of the same sizes as at 0.1.0-preview.9, switch off or on.
- **The stream.** `ParseResult.Tokens`: with the switch on, the tokens an accepted parse consumed, in source order, each
  read under the goal the parser named - no comment, no end-of-input token and no refusal token; empty with the switch
  off, and empty for a refused source. A template is its run of pieces (`TemplateHead`, each substitution's tokens,
  `TemplateMiddle`s, `TemplateTail`), or one `NoSubstitutionTemplate`.
- **The token.** `Token`, an immutable struct whose constructor is internal (the library makes every token a result
  holds; a default `Token`, of kind `EndOfInput` with no facts, is no token of any stream), `TokenKind`, `TokenFlags`
  and `Word` are public, with the facts section 9.3's note of 2026-11-01 at SJL-13.2b lists: the kind, which that note
  defines as an identifier name with the reserved or contextual word its cooked name spells, published as `Kind` with
  `Word`, so that the library's kind the row's fixed kind map is applied to (with the escaped flag, an escaped reserved
  word mapping to an identifier) is the whole of it, a keyword such as `if` included; `Start` and `End`, offsets into
  the request's text without the source's base offset, unlike a node's span, whose text is the raw spelling; `Line`
  and `Column`, 1-based with the source's base line and, on the text's first line, its base column;
  `Value`, an identifier's or private name's StringValue, a string's SV or a template piece's TV (null where it holds a
  `NotEscapeSequence`); `Raw`, a template piece's TRV; `Number`, a numeric literal's Number value (NaN for a BigInt or a
  literal with the `m` suffix); and `Flags`, with `NewLineBefore` and `IsEscaped` read from them: a line terminator
  before the token (for the first token, one before it, the terminator after a hashbang included; the text's start
  alone marks nothing), an escape in a name, the legacy octal and non-octal decimal literals and escapes, a
  `NotEscapeSequence`, the BigInt and `m` suffixes, and an escape or line continuation in a string. A
  regular-expression literal's body and flags are its raw spelling's two parts, split at its last `/`, so the literal's
  split offset stays internal: a consumer reads it from the raw spelling.
- **The dump** reads the stream the parse read whatever the switch says, so the canonical dump of a parse, and every
  digest of it, is the same bytes with the switch off or on; the tree's children stay internal.

Broiler.JS's bridge (decision D44) still reads a boundary token's extent from the source text, as SJL-14.2 left it; that
it may read the published stream instead is SJL-14's to decide.

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

*(Noted 2026-11-02, at SJL-13.2f, after the triage of part A's first codes in bundle `sjl-13-001`, on the rows two
sites report; no row's stage moves.)* An `await` BindingIdentifier in a class static block, whatever declares it, is
refused by EE0009, the row of a BindingIdentifier under [+Await], which the block's statements are read under; an escaped
one by EE0011, or EE0013 in a module; EE0140, "ClassStaticBlockStatementList Contains `await`", is the row of an `await`
expression or an unescaped reference there. The walk refused every `await` name in a static block by EE0140, which
looks into no function (`function await() {}` there) and finds no `await` terminal in an escaped name. And an
assignment inside an arrow's head whose array or object literal covers no AssignmentPattern is refused by the arrow's
cover row, EE0087, or EE0155 at an async head: the literal's refusal waits for the cover around it, as a
CoverInitializedName's does (EE0017), and is EE0032's where no arrow's head takes it. The parser refused it by EE0032
when the assignment was read, before `=>` was seen, though an arrow's parameters are no AssignmentExpression.

*(Noted 2026-11-02, at SJL-13.2f, after review of the note above, which quotes as it stands: "and is EE0032's where no
arrow's head takes it".)* That held only where a cover around the literal resolves. A class heritage, which no assignment
encloses at a statement's level, now makes its literals' deferred refusals when it has been read: EE0032 for a literal
target (`class C extends [[...x, y] = 1] {}`, which the walk refused by EE0033, whose condition does not hold of a
literal) and EE0017 for a CoverInitializedName (`class C extends {a = 1} {}`, which was accepted). And an enclosing
literal that the parse converts to an assignment pattern takes a deferred literal as a DestructuringAssignmentTarget,
whose row is EE0038 (`[[...x, y] = 1] = 2;`, `for ([[...x, y] = 1] of []);`, `({ a: [...x, y] = 1 } = 2);`): the inner
literal is no AssignmentExpression's target in the pattern, so EE0032 does not hold of it. EE0032 stays the row where
nothing converts the outer literal.

## 10. The library at SJL-15

*(Added 2026-11-24, at SJL-15.2 stage 1.)* The semantic model's entry, its types and its scopes as the library spells
them, for the shape Broiler.VM's area design sketches ("A sketch of the shape", `static-semantics.md` section 3) and
JSD-0080's section "The semantic model's design" decides. Like section 9 it restates and decides nothing: the decisions
are this repository's of 2026-11-24, recorded in `docs/roadmap/SharedLibraries.md`'s section "Shared static semantics
(SJL-15)" (D-js-entry, D-js-default-off, D-js-instances, D-js-kinds, D-js-scope-shape, D-js-parameters,
D-js-declaration-of, D-js-function-kind and D-js-names among them), and where this section differs from them or from the
records that win (the header), it is corrected to match. Section 4 stays the sketch as restated at SJL-12.1. Stage 1 is
the model's foundation; the members the sketch names beyond it (the module record, named evaluation, captures and the
early errors) are its later stages', and none is declared before its stage. Stage 2 adds the early errors of the
function context (section 10.4), stage 3 the jump targets with their early errors (section 10.5), stage 4 the
private-name environments with their early errors (section 10.6), and stage 5a resolution and the function environment
facts (section 10.7); the early errors of the module record stay their stage's.
*(Revised 2026-11-25, at SJL-15.2 stage 2; the paragraph ended at "and none is declared before its
stage".)* *(Revised 2026-11-25, at SJL-15.2 stage 3, when the jump targets landed; it read "(resolution, jump targets,
private names, the module record, named evaluation, captures and the early errors)" and "Stage 2 adds the early errors
of the function context (section 10.4); the early errors of jump targets, private names and the module record stay their
stages'".)* *(Revised 2026-11-25, at SJL-15.2 stage 4, when the private-name environments landed; it read "(resolution,
private names, the module record, named evaluation, captures and the early errors)" and "and stage 3 the jump targets
with their early errors (section 10.5); the early errors of private names and the module record stay their stages'".)*
*(Revised 2026-11-26, at SJL-15.2 stage 5a, when resolution landed; it read "(resolution, the module record, named
evaluation, captures and the early errors)" and "and stage 4 the private-name environments with their early errors
(section 10.6); the early errors of the module record stay their stage's".)*
Stage 5b adds Annex B's block-function aliases (section 10.8), which the model's fact list names ("Annex B's
block-function aliases", section 4) and the sketch gives no member. *(Added 2026-11-26, at SJL-15.2 stage 5b.)*
Stage 5c adds the caller's records to the request, with the model members that answer them and the rows they decide
(section 10.9). *(Added 2026-11-26, at SJL-15.2 stage 5c.)*

### 10.1 The entry

| Item | What | Decisions |
|---|---|---|
| The request | `ParseRequest.BuildSemanticModel`, init-only and false by default: a parameter of the request, as `RetainTokens` and the first-error parameter are (section 3), not a switch of the language. With it off the parse is exactly SJL-13's and SJL-14's | D-js-entry, D-js-default-off |
| The walk | The declarations walk builds the model as it goes, in the same visit and order, with explicit stacks; it starts no second walk (the milestone's words). Its own counter numbers the model's scopes and facts, so the walk's sequence, which orders the diagnostics and the tables, is the same with the model on or off on every parse the member's on path accepts. A partial walk before a grammar refusal builds none, and with the member on reports the model's rows (section 10.4). *(Revised 2026-11-25, at SJL-15.2 stage 2; it read "is the same with the model on or off. A partial walk before a grammar refusal builds none".)* | D-js-entry, D-js-scope-shape, D-js-channel |
| The result | `ParseResult.Model`, computed from the side tables (`Declarations?.Model`), so that the result keeps the layout it had at `0.1.0-preview.9`: null unless the member is on and the source is accepted, as `Declarations` is null for a refused source. With the member on, the model's rows also refuse a source (section 10.4); on every source the on path accepts, the tree, the side tables and the diagnostics are those of the parse with it off. *(Revised 2026-11-25, at SJL-15.2 stage 2; it read "With the member on, the tree, the side tables and the diagnostics are those of the parse with it off".)* | D-js-entry, D-js-channel |
| Who sets it | No engine before its own cut-over: Broiler.JS's bridge does not set it before SJL-15.5, and Broiler.VM reads the model only from a published version its pin commit admits | D-js-default-off |

### 10.2 The types

| Sketch | The library's | What |
|---|---|---|
| `SemanticModel` | `Broiler.ECMAScript.Semantics.SemanticModel`, sealed, its constructor internal | `Scopes`; `Functions`; `ScopeOf(SyntaxNode)`; `ParentOf(Scope)`; `DeclarationOf(SyntaxNode)`; `FactsOf(SyntaxNode)`; `TargetOf(BreakStatement)` and `TargetOf(ContinueStatement)` (section 10.5); `PrivateEnvironmentOf(ClassNode)` and `Resolve(PrivateIdentifier)` (section 10.6); `Resolve(Identifier)`, `Resolve(BindingIdentifier)` and `EnvironmentOf(SyntaxNode)` (section 10.7); `AnnexBAlias(Declaration)` (section 10.8); `DirectEvalCalls` and `CallerScopesOf(CallExpression)` (section 10.9) *(revised 2026-11-25, at SJL-15.2 stage 3; the cell ended at "`FactsOf(SyntaxNode)`")* *(revised 2026-11-25, at SJL-15.2 stage 4; the cell ended at "`TargetOf(ContinueStatement)` (section 10.5)")* *(revised 2026-11-26, at SJL-15.2 stage 5a; the cell ended at "`Resolve(PrivateIdentifier)` (section 10.6)")* *(revised 2026-11-26, at SJL-15.2 stage 5b; the cell ended at "`EnvironmentOf(SyntaxNode)` (section 10.7)")* *(revised 2026-11-26, at SJL-15.2 stage 5c; the cell ended at "`AnnexBAlias(Declaration)` (section 10.8)")* |
| `Scope` | The tables' `Scope`, the same instances | The model's `Scopes` are a superset of `DeclarationTables.Scopes` in the same relative order, adding the kinds `With`, `ClassBody`, `FunctionName` and `Parameters`. Eval code is not a kind: its program's scope is a `Script` scope and its facts' kind `FunctionKind.EvalCode`; the separate variable environment is a function fact, `FunctionFacts.HasSeparateVariableEnvironment` (section 10.7) *(revised 2026-11-26, at SJL-15.2 stage 5a; it read "the separate variable environment is a function fact of a later stage")* |
| `Declaration` | The tables' `Declaration`, the same instances | `BindingKind` gains `Parameter`, `FunctionName` (a named function expression's own name) and `ClassName` (a class's inner name), each with its three flags false, and `Declaration.IsMutable`, from the kind; and `Arguments`, the `arguments` binding, the model's alone and in no scope's declarations (section 10.7); and `BlockFunctionAlias`, a block function's var-scoped alias, the model's alone and in no scope's declarations (section 10.8) *(revised 2026-11-26, at SJL-15.2 stage 5a; the cell ended at "`Declaration.IsMutable`, from the kind")* *(revised 2026-11-26, at SJL-15.2 stage 5b; the cell ended at "in no scope's declarations (section 10.7)")* |
| `FunctionFacts` | The tables' `FunctionFacts`, the same instances | `Kind` (`FunctionKind`), `IsAsync`, `IsGenerator`, `HasSimpleParameterList` and `HasParameterExpressions`, the same on every request; the model's `Functions` add one for every static block and every field with an initialiser; and `HasSeparateVariableEnvironment`, computed from them, the same on every request (section 10.7) *(revised 2026-11-26, at SJL-15.2 stage 5a; the cell ended at "every field with an initialiser")* |
| `IdentifierReference`, `BreakOrContinue`, `PrivateNameReference`, `Diagnostic` | `Identifier`, `BreakStatement` and `ContinueStatement`, `PrivateIdentifier`, `SyntaxDiagnostic` | The tree's types, at the stages that read them; `LabelTarget` is stage 3's new type (section 10.5), `PrivateNameBinding`, with `PrivateEnvironment` and `PrivateNameKind`, stage 4's (section 10.6), `Resolution`, with `ResolutionKind`, `DynamicReason` and `EnvironmentFacts`, stage 5a's (section 10.7), and `ModuleRecord` is a new type of its stage *(revised 2026-11-25, at SJL-15.2 stage 3; it read "`Resolution`, `LabelTarget`, `PrivateNameBinding` and `ModuleRecord` are new types of those stages")* *(revised 2026-11-25, at SJL-15.2 stage 4; it read "and `Resolution`, `PrivateNameBinding` and `ModuleRecord` are new types of their stages")* *(revised 2026-11-26, at SJL-15.2 stage 5a; it read "and `Resolution` and `ModuleRecord` are new types of their stages")* |

Every collection is in source order: the scopes and the facts by the start of their node and, for one start, by the order
the walk made them in, which puts a scope before the scopes inside it; every fact is keyed by node identity. A node
that is not in the model's tree is a caller's error: `ScopeOf`, `DeclarationOf`, `FactsOf`, both `TargetOf` overloads,
`PrivateEnvironmentOf`, the `Resolve` overloads of `Identifier`, `BindingIdentifier` and `PrivateIdentifier`, and
`EnvironmentOf` throw `ArgumentException` for it, and `FactsOf` and `EnvironmentOf` for a node of the tree that has no
facts. *(Revised 2026-11-25, at SJL-15.2 stage 3; it read "`ScopeOf`, `DeclarationOf` and `FactsOf`
throw `ArgumentException` for it".)* *(Revised 2026-11-25, at SJL-15.2 stage 4; it read "`ScopeOf`, `DeclarationOf`,
`FactsOf` and both `TargetOf` overloads throw".)* *(Revised 2026-11-26, at SJL-15.2 stage 5a; it read
"`PrivateEnvironmentOf` and `Resolve(PrivateIdentifier)` throw `ArgumentException` for it, and `FactsOf` for a node of
the tree that has no facts".)*
`AnnexBAlias` throws `ArgumentException` for a declaration whose node is not in the model's tree. *(Added 2026-11-26,
at SJL-15.2 stage 5b.)*
`CallerScopesOf` throws `ArgumentException` for a call that is not in the model's tree or is not a direct-eval candidate.
*(Added 2026-11-26, at SJL-15.2 stage 5c.)*

### 10.3 Scopes and `ScopeOf`

Every class has a `ClassBody` scope, an anonymous class's too; every function a `Parameters` scope, an arrow's and a
function's without parameters too, and for the dynamic function's goals the program's `Parameters` scope is the model's
first; a named function expression has a `FunctionName` scope. For one function the scopes run `FunctionName`,
`Parameters`, `Function`, each the parent of the next, and an `if` clause's function's implicit `Block` comes before its
function's scopes. A node that owns a scope is in the scope around it and its children are in its own, except:

| Node | `ScopeOf` |
|---|---|
| The `ProgramNode` | The model's first scope |
| A dynamic function's parameters (patterns, initialisers, computed keys) / body | Its `Parameters` scope / its `Function` scope |
| A `FunctionNode` | The scope around it; an `if` clause's function's, its implicit `Block` |
| A function declaration's name / a named function expression's name | The scope that binds it / its `FunctionName` scope |
| A function's parameters, their initialisers and computed keys / its body, an arrow's expression body | `Parameters` / `Function` |
| A `ClassNode`, its decorators, a class declaration's name | The scope around it |
| A class expression's name; the heritage; every element, its key, computed or not, its decorators and a field's initialiser | `ClassBody` |
| A method's `FunctionNode` | `ClassBody`, its own scopes below |
| A static block's statements | Its `StaticBlock` scope |
| A `switch`'s discriminant / each case, its test and its statements | The scope around it / `Switch` |
| A `with` statement's object / body | The scope around it / `With` |
| A `catch` parameter and its initialisers / the body's `BlockStatement` | `Catch` / `Catch`, its statements in the body's `Block` |
| A `for` with a lexical head: the head, the test, the update, the body; a `for`-`in` or `for`-`of` with one: the head, the expression, the body | `For` |
| A label, a `break`'s or a `continue`'s label | The scope of the statement holding it |

`DeclarationOf` answers one declaration: for a class declaration's name the outer `Class` declaration the tables hold,
the inner `ClassName` declaration, with the same node, being in the class's `ClassBody` scope; for a class
expression's name its `ClassName` declaration.

### 10.4 The early errors

*(Added 2026-11-25, at SJL-15.2 stage 2.)* The model's early errors as the library reports them. Like the rest of this
section it restates and decides nothing: the decisions are D-js-channel, D-js-semantic-rows, D-js-adapter-form and
D-js-probe-semantics of 2026-11-25, in `docs/roadmap/SharedLibraries.md`'s section "Shared static semantics (SJL-15)".

**The channel.** With `ParseRequest.BuildSemanticModel` on, the model's rows join `ParseResult.Diagnostics`; there is no
list of early errors of the model's own. They refuse the parse as every walk diagnostic does, so that a refused source has
no model. With the member off the parse is exactly the one before: no row is reported, and the walk runs no statement
that reports, sorts or numbers anything it did not before.

**The table.** `SemanticRows`, internal, beside `Rows`: one entry per row of the stage table's `semantics` rows that the
library reports, each the catalogue's shared code and message identifier. The Frontend tests hold it a subset of those
rows, to be equal at stage 8 but for EA0006, and hold that no code in it is one of `Rows`'. `Rows` is unchanged, and the
test holding it equal to the stage table's `syntax` rows stands.

**Decision D11's order and the walk's sequence.** Each row is reported through the walk's one report, at its construct's
offset, numbered by the walk's one sequence; so a `syntax` row and a `semantics` row at one place keep the order the walk
found them in (an assignment's target refused before its `arguments` reference is visited, for example). On the on path
the sequence moves where a row is reported, and that parse is refused; on every parse the on path accepts the sequence is
the off path's.

**First error or all errors** stays the request's existing parameter (`ParseLimits.Reporting`, section 3). Under first
error an earlier row of the model's displaces the diagnostic the parse with the member off would give: JSD-0080's one
early-error stage.

**The partial walk.** Before a grammar refusal, the walk over the top-level statements the parse completed reports the
model's rows, with the member on, and builds no model. Every row is decided inside its construct, from the request and the
enclosing function, class element or literal, so it is complete within those statements; the dynamic function's
parameters goal hands the partial walk no statement, its body goal its statements under the program's context.

**The rows by group, their facts and their places.**

| Group | Rows | Fact | Place |
|---|---|---|---|
| A function's parameters or body | EE0074 to EE0077, EE0102 to EE0105, EE0117 to EE0120, EE0150 to EE0153 | The non-arrow function's shape and the side; an arrow is its enclosing code's, parameters included | The SuperProperty (a `MemberExpression` on `super`) or the SuperCall (a `CallExpression` on `super`), each reported |
| HasDirectSuper of methods | EE0093, EE0142, EE0106 (by shape, first); EE0015, EE0124, EE0126, EE0121 (by home) | The method's shape, then its home: an object literal's, a class's, a static one, the class's constructor (the first element named `"constructor"`) with or without a heritage; a later element of that name admits the call, EE0122 or EE0125 already refusing it | The SuperCall |
| Class elements | EE0131, EE0139 | A field initialiser or a static block admits a SuperProperty, not a SuperCall | The SuperCall |
| A program's own code | EE0162 (Script), EE0172 (Module) | The goal | The SuperProperty or the SuperCall |
| Eval code | EA0003, EA0004, EA0005 | The request's `InheritedContext`, for every eval request, direct or indirect, which the request does not tell apart | The SuperProperty, the SuperCall, each `arguments` reference |
| ContainsArguments | EE0130, EE0138 | The anchors that enclose an `arguments` reference: fields and static blocks push one, arrows and a nested class's heritage and computed keys keep them, every non-arrow function and every method's parameters and body clear them | Once per member, at the `FieldDefinition` or the `StaticBlock`, every enclosing member reporting |
| Expressions | EE0018, EE0021, EE0054 | An object literal's `PropertyName : value` entries named `__proto__`; a template's TemplateStrings at 2^32, which no source reaches; a for-in or for-of head target whose AssignmentTargetType is invalid, EG0008 in its place for a non-optional call | Every `__proto__` entry after the first; the template; the head target |
| ShadowRealm | EP0017, EP0018 | No goal is ShadowRealm's; EA0003, EA0004 or EE0162 refuses their sources first | No report of their own |

Each place is the one Broiler.VM's binder names (section 3, "a check taken from Broiler.VM's binder at the node the
binder names"), but where the library differs from either engine, as `docs/roadmap/SharedLibraries.md` lists for
SJL-15.3: Broiler.JS refuses a duplicate `__proto__` entry at the second entry only; Broiler.VM does not search an
arrow's parameters for `arguments`, and reports only the innermost member when members nest; and the library reports
EA0005 at each reference, a real place. It also differs in what it refuses: Broiler.JS admits a SuperProperty in a
function expression that is an object literal's property value and not a method, which EE0075 refuses
(`Broiler.JS/Broiler.JavaScript.Compiler.Tests/PropertyStoreCacheTests.cs`,
`ASuperAssignmentStillTargetsTheHomeObjectsPrototype`, runs such a program); and Broiler.VM drops EA0005 when the eval
code itself declares `arguments`, where ContainsArguments reads every reference, which shows only in sources already
refused (eval code in a field initialiser is strict, and a strict binding of `arguments` is refused).

**The vectors.** The library-level adapter (`runners/AlgorithmsLibrary.Semantics.cs`) answers the `semantics` vectors
whose identifier names a row in its own list, held equal to `SemanticRows` by a Conformance test, and, from stage 3, the
vectors of the correspondence groups in a second list of its own (section 10.5), and reads them at the parse; the Native
AOT probe judges those vectors at the parse on a line of its own. *(Revised 2026-11-25, at SJL-15.2 stage 3; it read
"whose identifier names a row in its own list, held equal to `SemanticRows` by a Conformance test, and reads them at the
parse".)*

### 10.5 Jump targets and labels

*(Added 2026-11-25, at SJL-15.2 stage 3.)* Each jump's target, and the early errors of jump targets, as the library
answers and reports them. Like the rest of this section it restates and decides nothing: the decisions are
D-js-jump-targets, D-js-jump-rows and D-js-adapter-groups of 2026-11-25, in `docs/roadmap/SharedLibraries.md`'s section
"Shared static semantics (SJL-15)".

**`TargetOf` and `LabelTarget`.** `SemanticModel.TargetOf(BreakStatement)` and `TargetOf(ContinueStatement)`, the
sketch's `TargetOf(BreakOrContinue)` on the tree's two jump types (D-js-names), answer a `LabelTarget`, public and
sealed, its constructor internal: `Statement`, the statement the jump leaves or continues, and `LabelSet`, that
statement's own label set, the labels of the chain of `LabelledStatement`s directly over it, outermost first, empty
when it has none.

| Jump | `Statement` | `LabelSet` |
|---|---|---|
| `break;` | The innermost iteration statement or `switch` enclosing it within its function, static block or program | That statement's own |
| `continue;` | The innermost iteration statement so enclosing it | Its own |
| `continue L;` | The innermost such iteration statement whose own label set holds `L`, wherever `L` stands in it, so that `A: B: for (;;) continue A;` targets the `for` with [A, B] (hand-off H20, by the specification) | Its own |
| `break L;` | The statement `L` labels, looked at through every further label on it: the labelled item, never the `LabelledStatement`, so that `A: B: { break A; }` targets the block with [A, B] and `L: break L;` the `break` itself with [L] | The labels directly over that item |

The search reads the whole chain of enclosing statements, not only the first that carries the label. The answer is
never null, where the sketch writes `LabelTarget?` (section 4): a model exists only for a source the early errors
accept, and in one every jump has a target. One instance per target statement is shared by every jump that targets it,
as the model is keyed by node identity. A node that is not in the model's tree is refused as `ScopeOf` refuses it.

**The rows**, each in `SemanticRows`, reported with the member on at the jump, keyed at its offset, where both engines
place a jump's refusal:

| Rows | Fact | Owner | Argument |
|---|---|---|---|
| EE0058 (`continue` with no iteration statement), EE0059 (`break;` with no iteration statement or `switch`) | The statements the jump may leave or continue, up to the boundary | Any | None |
| EE0081, EE0082 | ContainsUndefinedBreakTarget, ContainsUndefinedContinueTarget of a function body, every function's, arrows' included, and the dynamic function's body goal | Function | The label |
| EE0136, EE0137 | The same of a class static block | Static block | The label |
| EE0165, EE0166 | The same of a Script's statements, eval code's included | Script | The label |
| EE0175, EE0176 | The same of a Module's items, moved here from stage 6 | Module | The label |

The walk carries the jump state in its row context: the statements a jump may leave or continue, innermost first, with
each one's label set, and the labels directly over the statement being visited. Every non-arrow function, the program,
the dynamic goals, a field initialiser and a static block make a fresh row context, which resets it, and an arrow resets
it itself: every function is a boundary. A labelled statement's item takes its label set from the labels over it: a
further label, an iteration statement or a `switch` takes them over, and any other item is a statement a labelled
`break` may leave.

**One row per construct.** A labelled `continue` that no iteration statement encloses meets EE0058, whose productions
include `continue LabelIdentifier ;`, and its owner's undefined-continue row; it reports the owner's row alone, as the
catalogue's note on EE0058 reads it ("The `continue L` forms that name no iteration label are EE0082 and its
siblings"), and as both engines make one refusal per jump. A `break` never meets two, since EE0059 covers `break ;`
alone. The duplicate-label rows, `syntax` rows, stay at the label.

**The order.** A jump's row is keyed at the jump, before its label's name row, which is keyed at the label: under first
error `"use strict"; break yield;` answers EE0165 with the member on and the name row with it off. The one `syntax`
place a jump's row can share is a scope's close, where the scope's declared-name conflicts are keyed, when a jump is
written right after it (`{ let a; let a; }break;`): D11's sub-key, the second declaration's place, puts the conflict
first.

**The vectors.** The adapter answers the jump rows' vectors by their identifiers, and the vectors of group `lb1`, row
LB1, by a second list of its own, the correspondence groups the model decides, each with the rows it rests on: `lb1` on
EE0082 and EE0166, `ScriptEvaluation` only. Its vectors are the acceptances that show H20 read by the specification.

**Where the library differs from each engine**, named for SJL-15.3, as `docs/roadmap/SharedLibraries.md` lists it: in
what it refuses, H20 (`continue` to any label of an enclosing iteration statement's label set is accepted), against
Broiler.VM's binder, which refuses `continue` to the outer label of a loop's label set, and the same as Broiler.JS since
Broiler-Platform/Broiler.JS#1075; in the target's mapping, Broiler.VM's `JumpTarget` names the `LabelledStatement` for a
`break` to any label not written directly on the loop or `switch` it labels, both a target that is no loop or `switch`
and an outer label of a loop's or a `switch`'s label set (`A: B: while (1) break A;`), and carries only the label
written directly on a loop or `switch`, where the library names the labelled item with its whole label set; in places,
none known. The catalogue's notes on EE0166 and EE0176 read Broiler.JS as over-refusing `L1: L2: while (c) continue
L1;`, which is stale since #1075.

### 10.6 Private names

*(Added 2026-11-25, at SJL-15.2 stage 4.)* Each class's private environment, each private name's binding, and the early
errors of private names, as the library answers and reports them. Like the rest of this section it restates and decides
nothing: the decisions are D-js-private-environments, D-js-private-rows and D-js-adapter-visible-names of 2026-11-25, in
`docs/roadmap/SharedLibraries.md`'s section "Shared static semantics (SJL-15)".

**`PrivateEnvironmentOf`, `Resolve` and the types.** `SemanticModel.PrivateEnvironmentOf(ClassNode)` answers every
class's `PrivateEnvironment`, empty or not: `Class`; `Outer`, the environment the class is evaluated in; and `Names`,
its PrivateBoundIdentifiers as `PrivateNameBinding`s in source order, once each, `#constructor` among them where a class
writes it. `SemanticModel.Resolve(PrivateIdentifier)`, the sketch's `Resolve(PrivateNameReference)` on the tree's
`PrivateIdentifier` (D-js-names), answers a `PrivateNameBinding`: `Name`, the StringValue with its `#`; `Environment`;
`Kind`, a `PrivateNameKind` (`Field`, `Method` or `Accessor`, an auto-accessor's too); `IsStatic`; and `Elements`, the
`MethodDefinition`s and `FieldDefinition`s that declare it, a getter and setter of one name and placement being one
binding with both elements. The types are public, the classes sealed with internal constructors. `Resolve` keeps the
sketch's nullable `PrivateNameBinding?` (section 4), where stage 3's `TargetOf` departed from the sketch the other way:
here a null occurs on accepted trees. It is the first of the sketch's `Resolve` overloads to land (section 4's
`Resolve(IdentifierReference)` and `Resolve(PrivateNameReference)`); with stage 5a's `Resolve(Identifier)` and
`Resolve(BindingIdentifier)` beside it (section 10.7), a caller passing a null literal names the overload by a cast
*(revised 2026-11-26, at SJL-15.2 stage 5a; it read "once the other lands at stage 5a, a caller passing a null literal
names the overload by a cast")*. One instance per binding is shared by its keys and every reference to it.
No brand member: a class's [[PrivateMethods]] brands are read from its names, a binding not of `Field`, instance or
static.

| `Resolve`'s node | Answer |
|---|---|
| A class element's key | The binding it declares in its class's environment |
| A member's `.#x` (an optional link's among them) or `#x in o` | The binding of the innermost class whose environment holds the reference and whose names hold `#x`, whatever the placement of either |
| Eval code's reference to a name its caller sees and no class of the eval code declares | Null |
| Any reference under the `FormalParameters` and `FunctionBody` goals that no class of the unit declares | Null |
| An object literal's private key | Null: it declares nothing; EE0016 refuses it, so that no model holds one |

**The heritage in the environment around the class.** AllPrivateIdentifiersValid checks ClassHeritage with the names
around the class, and ClassDefinitionEvaluation evaluates it in the outer private environment, as both engines do; the
class's computed keys, field initialisers, static blocks, methods and element decorators are inside its own environment
(the class's decorators outside). `ScopeOf` puts the heritage in the class's `ClassBody` scope (section 10.3), which
stays true; so the private environment is a relation of its own, and a class in another class's heritage has that
class's outer environment as its `Outer`, not that class's.

**The rows**, each in `SemanticRows`, reported with the member on at the member or `in` expression, keyed at its offset,
where Broiler.VM's binder places them, once per undeclared reference, with the name and its `#` as the argument:

| Goal | Root names | Row |
|---|---|---|
| Script | None | EE0167 (AllPrivateIdentifiersValid of a Script's statements) |
| Module | None | EE0177 (of a Module's items), moved here from stage 6, needing only the Module goal |
| Eval, `InheritedContext.VisiblePrivateNames` a list | The list | EA0009 (EvalDeclarationInstantiation step 7) |
| Eval, the list null | No check | None |
| `FormalParameters`, `FunctionBody` | No check | None: the catalogue's EX0031 is an engine row |

Eval code never takes EE0167: the request does not tell a direct eval from an indirect one, and a list implies a
caller's private environment. The walk carries the names in its row context, innermost class first, and carries them,
not resets them, through every function, parameter list, field initialiser and static block, the opposite of stage 3's
jump state; it numbers nothing in the walk's sequence. With the member off nothing reads the row context or the
request's visible names, although Broiler.JS's bridge sends a list while a direct eval runs; a Frontend test holds the
off path's outcome, diagnostics and dump the same with no list, an empty one and one holding a name.

**The order.** A row is keyed at its member or `in` expression and reported before the node's children are visited, so
it comes before a `syntax` row a child reports at the same place: in `class C { m() { return implements.#y; } }` EE0167
precedes the strict-reserved name row at `implements`. A scope's declared-name conflicts, keyed at its close, come
before a row of a member written right after it (`{ let a; let a; }this.#x;`), by D11's sub-key. More generally, a row
anywhere inside a scope, before its close, comes before that scope's conflicts, although the conflicting declarations
come earlier in the source (section 10.5's keying at the close): in `class C { m() { let a; let a; this.#y; } }` EE0167
precedes the method body's conflict, and under first error the on path names EE0167 where the off path names the
conflict, as stage 2's narrowed identity test admits. A chain of undeclared names (`this.#a.#b`) gives a row for each at
one place, the outer reference's first, as the walk visits them. Against the specification's order for eval code: EA0009
is EvalDeclarationInstantiation's step 7, after every early error of ParseText and PerformEval's rows; under first error
the library, ordering by place, can name EA0009 where the specification raises another SyntaxError first (`this.#y; let
a; let a;` as eval code, whose ScriptBody conflict is keyed at its close, or `arguments.#y` with the field-initialiser
context). The refusal is the same; only the code differs.

**Eval code's visible names.** A null list leaves the check to the engine; a list is every name visible at the call. The
library-level adapter passes one for every `DirectEval` vector, read from the Script's model as
EvalDeclarationInstantiation builds it: the innermost class whose elements hold the call and the environments around it,
innermost first, each name once, and an empty list where no class holds the call. Group `pn1` stays not implemented:
`pn1/0004` and `pn1/0005` expect a value the Script computes after catching the eval code's refusal, which a reading at
the parse cannot match.

**Where the library differs from each engine**, named for SJL-15.3 and SJL-15.5, as `docs/roadmap/SharedLibraries.md`
lists it: Broiler.JS throws a member form's refusal at the private name, the library at the member, as Broiler.VM's
binder does (for `#x in o` the places coincide); Broiler.VM's binder skips eval code whose caller is in a class body and
its executor refuses at run time, where the library refuses at the eval code's parse when the request carries the names
(class T for row PN1); the library reports every undeclared reference, where Broiler.JS throws at the first;
Broiler.JS's validator does not reach a `case` clause's references, which the library refuses; Broiler.JS passes no list
where no name is in scope; an indirect eval given an empty list is refused with EA0009's code where the specification's
is EE0167's; and under first error EA0009 can be named before a row the specification raises first.

### 10.7 Resolution and the function environment

*(Added 2026-11-26, at SJL-15.2 stage 5a.)* Each reference's and each binding identifier's resolution, and each
function's environment facts, as the library answers them. Like the rest of this section it restates and decides
nothing: the decisions are D-js-resolution, D-js-environment-facts, D-js-arguments-binding and D-js-environment-vectors
of 2026-11-26, in `docs/roadmap/SharedLibraries.md`'s section "Shared static semantics (SJL-15)". No row is added: no
`semantics` row of the stage table names resolution, and with the member off nothing moves.

**The members and types.** `SemanticModel.Resolve(Identifier)` answers every reference and
`SemanticModel.Resolve(BindingIdentifier)` every binding identifier with a `Resolution`, never null: `Kind`, a
`ResolutionKind` (`Static`, `Dynamic`, `Global` or `Inherited`); `Scope` and `Declaration`; `DynamicScopes`, innermost
first; `Reason`, a `DynamicReason` (`With`, `SloppyDirectEval` or `BlockFunction`); and `Fallback`. *(Revised
2026-11-26, at SJL-15.2 stage 5b, when `BlockFunction` joined the reasons (section 10.8); it read "`Reason`, a
`DynamicReason` (`With` or `SloppyDirectEval`)".)*
`SemanticModel.EnvironmentOf(SyntaxNode)` accepts the nodes `FactsOf` accepts and answers `EnvironmentFacts`: `Node`,
`Arguments`, `ArgumentsNeeded`, `ArgumentsMapped`, `DirectEvalInParameters`, `DirectEvalInBody` and `ContainsWith`.
`BindingKind` gains `Arguments`, and `FunctionFacts` gains the computed `HasSeparateVariableEnvironment`. The types are
public, the classes sealed with internal constructors; every answer is computed when the model is built and is
immutable, and no instance is shared by guarantee. With stage 4's `Resolve(PrivateIdentifier)`, `Resolve` has overloads
for `Identifier`, `BindingIdentifier` and `PrivateIdentifier`, so a caller passing a null literal names the overload by
a cast.

**Against the sketch.** Section 4's `Resolve(IdentifierReference)` answers "Static(scope, declaration) | Dynamic(reason)
| Global". The library's `Dynamic` also carries the scopes searched and the answer when none of them binds the name,
which an engine's lowering needs (a search with a bound, then a static fallback); `Inherited` is eval code's, whose
caller's environments the request does not describe; and the `BindingIdentifier` overload answers where a binding the
language resolves when it runs writes. JSD-0080's resolution forms are not among the decisions that bind this
repository, and these go beyond them.
From stage 5c the request may describe the caller's records up to the variable environment; they narrow only eval code's
carried declarations and aliases, and `Inherited` stays (section 10.9). *(Added 2026-11-26, at SJL-15.2 stage 5c.)*

**The lookup**, finished after the walk over the references it recorded in visit order, since a later direct `eval`
makes an earlier reference dynamic (`function f() { x; eval("var x"); }`); it is a pass over a list, not a second walk.
From the reference's `ScopeOf`, outward by `ParentOf`:

| At a scope | The lookup |
|---|---|
| `With` | A dynamic entry, reason `With` (the statement's evaluation, NewObjectEnvironment): an object environment may bind any name |
| A scope that binds the name | Ends: `Static` with the scope and the name's first declaration in it, or for sloppy eval code's own var-scoped declaration `Inherited` with it and no scope |
| A scope a sloppy direct `eval` may add to | A dynamic entry, reason `SloppyDirectEval`, after its own declarations miss |
| A scope a block function's evaluation may give an `arguments` binding (section 10.8) | For the name `arguments` alone, a dynamic entry, reason `BlockFunction`, after its own declarations miss; a scope that may also gain by a sloppy direct `eval` is that row's, reason `SloppyDirectEval` *(added 2026-11-26, at SJL-15.2 stage 5b)* |
| The chain's end | `Inherited` for eval code, `Global` for a Script, a Module and the dynamic function's goals |

A non-empty list of dynamic entries makes the answer `Dynamic`, its `Reason` the innermost entry's and its `Fallback`
the answer the lookup ended with, never itself dynamic. The lookup is iterative and memoised by its starting scope and
name, the memo only looked up; the chain's depth is bounded by a small constant times the request's nesting bound, a
named function expression adding its `FunctionName`, `Parameters` and `Function` scopes and a class its `ClassBody`.

**What a scope binds.** Its `Declarations` by name, and several declarations of one name make one binding: the answer
names the first in source order, so that `Declaration` names the binding, not its value. Where several function
declarations share a name the value is the last one's (FunctionDeclarationInstantiation step 14.a.iii.2, as
GlobalDeclarationInstantiation and EvalDeclarationInstantiation note too). A `Parameters` scope also binds its
function's `arguments`, where `EnvironmentOf(...).Arguments` is non-null (step 22), and a `StaticBlock` scope its
block's. A top-level `var` or function declaration of a parameter's name, or of `arguments`, in a function's `Function`
scope answers its own declaration: where the variable environment is not separate it is the parameter's binding (steps
29.b and 29.c, and step 37 for a function's object), and otherwise a binding of its own, a `var` initialised from the
parameter (steps 30.e.i.4 and 30.e.i.6) and a function declaration to undefined first (step 30.e.i.3). A Script's
declarations are `Static` to its scope, lexical ones in the global declarative record and the others in the object
record (GlobalDeclarationInstantiation); storage stays each engine's. A Module's imports are `Static` to its scope.
A sloppy function's `Function` scope also binds each Annex B alias of its block functions, and sloppy eval code's program
scope its own, which answers `Inherited` carrying the alias; a Script's aliases bind no reference, which stays `Global`
(section 10.8). *(Added 2026-11-26, at SJL-15.2 stage 5b.)*

**The constructs.** A parameter's initialiser or computed key is in the `Parameters` scope, whose chain skips the body's
`Function` scope (step 30's note: closures in the formal parameter list do not see the body's declarations); a body
reference meets `Function`, then `Parameters`, then a named function expression's `FunctionName`, then the scopes
outside. A class's heritage, keys and initialisers see its inner name in its `ClassBody` scope. A `catch` parameter's
`Catch` scope is outside its block, so that with Annex B.3.4 `catch (e) { var e; e }` resolves `e` to the parameter.
`for`, `for`-`in`/`of`, `switch` and `with` follow section 10.3's table.

**The scopes a sloppy direct `eval` may add to.** A direct-`eval` candidate in non-strict code declares its `var`s in
the running VariableEnvironment (PerformEval step 16.b); strict eval code has its own (step 18). For each function, an
arrow's among them, and the dynamic function's goals:

| Where the candidate stands | The entry |
|---|---|
| In the body, the variable environment separate | The `Function` scope: the function's own VariableEnvironment (FunctionDeclarationInstantiation step 30) |
| In the body, the variable environment shared | The `Parameters` scope: the parameters' environment is the VariableEnvironment (step 29.d); a name `Parameters` binds stays `Static`, the eval's `var` of it being the same binding |
| In the parameters | The `Parameters` scope, standing for calleeEnv, just outside the parameters' environment (step 20 and its note) |

The two `Parameters` cases never meet in one entry: a call in the parameters implies parameter expressions, so a
separate variable environment, and a call in the body then makes the `Function` scope gain. A caller tells which
environment a `Parameters` entry stands for by `EnvironmentOf(entry.Node).DirectEvalInParameters`. A Script's top level,
a Module, eval code's own program scope, a static block and a field initialiser gain nothing that matters: a Script's
eval adds to the object record `Global` consults, and the others are strict or inherit already. An arrow's call makes
the arrow's scopes gain, not its enclosing function's.
A block function named `arguments` whose evaluation creates the `arguments` binding makes the scope standing for the
variable environment gain that name alone, kept apart from these scopes, which gain every name (section 10.8). *(Added
2026-11-26, at SJL-15.2 stage 5b.)*

**The lexical and variable environments of one function.** A sloppy function's one `Function` scope stands for its
LexicalEnvironment over its VariableEnvironment (step 32.b): its `IsLexical` declarations are the LexicalEnvironment's,
the others the VariableEnvironment's, which where `HasSeparateVariableEnvironment` is false is also the `Parameters`
scope's environment; a strict function's scope is one environment (step 31.a); sloppy eval code's program scope holds
its own LexicalEnvironment's declarations, its `var`s going to the caller's. For resolution the split cannot be
observed: a reference meets the LexicalEnvironment first, and a direct `eval`'s `var` beside one of its names is
EvalDeclarationInstantiation's SyntaxError. Stage 5c's caller scopes read the `IsLexical` split.
`SemanticModel.CallerScopesOf` reads it, a sloppy function's record between a call in its body and its
VariableEnvironment holding the `IsLexical` declarations alone (section 10.9). *(Added 2026-11-26, at SJL-15.2 stage
5c.)*

**Eval code.** The request does not tell a direct `eval` from an indirect one, and the caller's records it may carry
stop at the variable environment, leave out `with` objects and are no scopes of the model, so a name eval code does not
declare is `Inherited`, with no declaration; sloppy eval code's own var-scoped declarations are `Inherited` with the
declaration and no scope, the binding being created in the caller's VariableEnvironment; strict eval code's
declarations and sloppy eval code's lexical ones are `Static`. The caller's records narrow only the carried declaration
and eval code's aliases, and `Inherited` stays: where a record the request gives binds the name, a reference, a binding
the language resolves when it runs and a dynamic answer's fallback carry no declaration, a caller's catch parameter
standing first, and every other binding keeps its own; without them the carried declaration is the eval body's, as the
member's contract says. A caller's `with` object of the name can still stand before the binding, a run-time condition
that no stage closes (section 10.9). *(Revised 2026-11-26, at SJL-15.2 stage 5c, when the caller's records joined the
request; it read "The request does not tell a direct `eval` from an indirect one and carries no caller's names, so a
name eval code does not declare is `Inherited`, with no declaration; sloppy eval code's own var-scoped declarations are
`Inherited` with the declaration and no scope, the binding being created in the caller's VariableEnvironment, before
which a caller's `with` object or `catch` parameter of the name can stand; strict eval code's declarations and sloppy
eval code's lexical ones are `Static`. Stage 5c's caller scopes may narrow `Inherited`.")*
A name only an Annex B alias of sloppy eval code binds is `Inherited` too, carrying the alias, which the caller's
environments can still withhold (section 10.8). *(Added 2026-11-26, at SJL-15.2 stage 5b.)*
With the caller's records given, a record that is not a Catch clause's and binds the name withholds it, and under a Catch clause's record the
alias is kept and the answer carries no declaration (section 10.9). *(Added 2026-11-26, at SJL-15.2 stage 5c.)*

**The dynamic function's goals.** CreateDynamicFunction's function has the realm's global environment as its scope, so
an undeclared name is `Global`. Each goal is answered with the other piece empty: under the `FunctionBody` goal `return
a` in the body of `new Function("a", "return a")` answers `Global`, although the real function's parameter binds `a`, so
that `Global` there means "bound neither by this piece nor known to the parse"; `Arguments`, `ArgumentsMapped` and
`HasSeparateVariableEnvironment` are computed with the parameters empty too.
So under the `FunctionBody` goal a `Static` answer naming an Annex B alias can be wrong too, where the real parameters
bind the name and withhold the alias (section 10.8). *(Added 2026-11-26, at SJL-15.2 stage 5b.)*

**The binding overload.** A binding the language resolves when it runs is looked up as a reference standing there: each
BoundName of a `var` declarator with an initialiser (VariableDeclaration evaluation's ResolveBinding, a pattern's
PutValue with an undefined environment repeating it) and of a `for`-`in` or `for`-`of` statement's `var` head, with or
without an initialiser, so that `with (o) { var x = 2 }` writes `o.x` and `catch (e) { var e = 1 }` writes the
parameter. Duplicate sloppy parameters are resolved at run time too (step 25.a), but a simple sloppy list leaves no
expression, `with` or `eval` between a parameter and its environment, so the lookup ends there on the first declaration
of the name. Every other binding answers as a reference would where the lookup reaches the scope that holds its
`DeclarationOf`. So the overloads agree: in `var v; var v = 1; v` both bindings and the reference answer the first
`var`.

**The facts**, each a member, with its clause:

| Fact | Member | Clause |
|---|---|---|
| The `arguments` binding | `EnvironmentFacts.Arguments`, a `BindingKind.Arguments` declaration whose node is the function's, the static block's or the dynamic function's program, in no scope's declarations, so that `DeclarationOf` of its node stays null | FunctionDeclarationInstantiation steps 15 to 18 and 22: not for an arrow, nor where a parameter is named `arguments`, nor, without parameter expressions, where a top-level function or a lexical declaration of the body is; a static block's body is instantiated with no parameters; a field initialiser has none, its evaluation calling no FunctionDeclarationInstantiation ("Even though field initializers constitute a function boundary, calling FunctionDeclarationInstantiation does not have any observable effect and so is omitted") |
| Its mutability | `Declaration.IsMutable`, true by kind | Step 22.c.ii's note: strict code's early errors make its immutability unobservable |
| The need (row FF3) | `ArgumentsNeeded` | The observable need, below |
| The mapping (row FF3) | `ArgumentsMapped`: needed, not strict and a simple parameter list | Steps 22.a and 22.b |
| The separate variable environment (row FF4) | `FunctionFacts.HasSeparateVariableEnvironment`: `HasParameterExpressions` for every function kind, the dynamic goals' included, and `IsStrict` for eval code | Step 30; PerformEval step 18 |
| Direct `eval` (row FF5) | `DirectEvalInParameters`, `DirectEvalInBody` | A candidate in non-strict own code: the call's evaluation, "direct eval" |
| `with` | `ContainsWith` | A `WithStatement` in own code, arrows excluded |
| Resolution (rows RS1 to RS3) | `Resolve` | ResolveBinding, GetIdentifierReference |
| A constant's assignment (row RS4) | `Resolve` with `Declaration.IsMutable` | Whether a reference writes is not the model's |

A separate parameter environment (steps 19 and 20) is `!IsStrict && HasParameterExpressions`, and a separate
LexicalEnvironment `!IsStrict` for a function (steps 31 and 32) and always present for eval code (PerformEval steps 16.a
and 17.a); neither is a member.

**The candidate and own code.** A direct-`eval` candidate is a call that is not optional whose callee, through any
parentheses, is the name `eval`: the call's evaluation reads a Reference named `"eval"` and the grouping operator keeps
one, so `(eval)(s)` counts, and `eval?.(s)`, `(0, eval)(s)`, `new eval()`, a tagged template and a member call do not.
Each only may be direct, SameValue with %eval% deciding at run time; its strictness is the call's own. Own code is
outside every nested function, arrow, method, field initialiser and static block, and class code is strict; the walk
attributes each call and each `with` by a model-only member of its context naming the function whose code holds it, as
stage 1's model scope is carried.

**The precise need.** `ArgumentsNeeded` is true where `Arguments` is non-null and a reference resolves to it, or to a
top-level `var` named `arguments` in the function's own `Function` scope (the binding itself or initialised from it), as
`Static` or as a `Dynamic` answer's `Fallback`; or a direct-`eval` candidate of any strictness stands in the function's
own code, in an arrow's parameters or body within it, or in a nested class's heritage, computed keys or decorators,
since eval code may name `arguments`. Only references count, and it is reference-based, not flow-sensitive. It is not
FunctionDeclarationInstantiation's argumentsObjectNeeded, which is wider than Broiler.VM's base binder for almost every
function: Broiler.VM's decision D-precision reads the model's precise facts in its window 1, and its class P admits only
moves where the model's fact is narrower than the base's approximation (an inference). Against Broiler.VM's
`UsesArguments`, a mention of `arguments` or `eval` in a non-arrow function no parameter of which binds the name, the
model's need is narrower for `var e = eval; e(...)` and for `let arguments` and `function arguments(){}` without
parameter expressions, and wider where an arrow's parameters name `arguments` or hold a candidate, which Broiler.VM's
search, entering an arrow's body only, misses (an inference, not probed).

**What `DirectEvalIn*` does not say.** They say whether a node's own environments may gain bindings, not whether a
nested direct `eval` can read its bindings by name: in `function f() { var x; (() => eval("x = 1"))(); }` f's
`DirectEvalInBody` is false, yet the arrow's call reaches f's `x` at run time. That reach is stage 7's, with captures
and the rebuild of Broiler.JS's `FreeNameScan.Dynamic`, which is transitive and ignores strictness.

**An unknown: a class decorator's candidate.** A direct-`eval` candidate in a class's own decorators is read at the
enclosing code's strictness, as the walk reads decorators: it visits them in the enclosing context, and the class's
heritage, keys and elements in strict code. Decorators are a proposal outside the pinned edition, behind a proposal
flag; whether the proposal makes them strict code is not settled here, and the stage that settles proposal decorators
decides it. Until then such a candidate in a sloppy function's own code counts towards its `DirectEvalInBody` or
`DirectEvalInParameters`.

**Annex B's block-function aliases.** The tables hold no block function's var alias (FunctionDeclarationInstantiation
step 32.a), only `IsBlockFunction`; the model answers it, `SemanticModel.AnnexBAlias` (section 10.8). With Annex B on
(`ParseSwitches.AnnexB`), a name that only a block function's alias binds answers that alias: in function code `Static`,
to the function's `Function` scope and the alias, so that in `var g; function f() { { function g() {} } return g; }` the
reference answers f's alias, not the Script's `var g`; in sloppy eval code `Inherited`, carrying the alias; in a Script
`Global`; and an `arguments` binding a block function's evaluation creates is a dynamic entry, reason `BlockFunction`.
Eval code's alias is decided when the request carries the caller's records (`InheritedContext.CallerScopes`), a Catch
clause's record exempt. Without them the request does not describe the caller, and the eval body alone decides, as
`CallerScopes`' contract says. *(Revised 2026-11-26, at SJL-15.2 stage 5c, when the caller's records joined the request,
closing the interim; it read "What remains: the interim, eval code's alias conditional on its caller's environments until
stage 5c describes them; and" before the run-time conditions, which stay as written.)* What remains:
the run-time conditions, which no stage closes: a Script's alias depends on GlobalDeclarationInstantiation's checks, and
eval code with a global variable environment on GlobalDeclarationInstantiation-like checks. *(Revised 2026-11-26, at SJL-15.2 stage 5b, when the aliases joined the model; it
read "**Annex B.3.3's aliases until stage 5b.** The tables hold no block function's var alias (FunctionDeclarationInstantiation
step 32.a), only `IsBlockFunction`. With Annex B on (`ParseSwitches.AnnexB`), a name that only a block function's Annex
B.3.3 alias binds is answered as the scopes alone answer it until stage 5b: `Global`, or a `Static` or `Dynamic` answer
naming an outer binding, which the alias shadows at run time. So in `var g; function f() { { function g() {} } return
g; }` the scopes answer the Script's `var g`, and the alias is f's. Nothing reads `Resolve` before stage 5b (an
inference: no engine reads the model or sets the member, and publication is stage 8's); no test pins such an answer. A
Script-level alias is a run-time global `var`, so `Global` is already right there.")*

**The vectors left unanswered.** The value vectors of rows FF3, FF4, FF5 and RS2, groups `ff3`, `ff4`, `ff5` and `rs2`,
stay not implemented, as group `pn1` does (section 10.6): they name no catalogue row, and read at the parse each would
match by acceptance alone, showing nothing of resolution. The Frontend tests cover their sources instead.

**The dump.** The canonical dump's model section writes, after section 10.6's lines, one `model-environment` line per
facts entry and one `model-resolution` line per `Identifier` and per `BindingIdentifier`, an `arguments` binding named
by its node; a dump without a model is unchanged, and no corpus runner or probe requests the model before stage 8. It
writes a line for every `BindingIdentifier`, not only for those the language resolves when it runs: the model does not
say which bindings those are, and the dump computes no fact of its own.

**Where the library differs from each engine**, named for SJL-15.3 and SJL-15.5, as `docs/roadmap/SharedLibraries.md`
lists it. FF4: Broiler.JS separates the environment only where a sloppy function holds a direct `eval`, so a closure in
the parameters reads the body's `var` (probe V010), and Broiler.VM's body environment is narrower than step 30. FF5:
Broiler.JS declares an arrow's direct `eval`'s `var` in the enclosing function (probe V015), and Broiler.VM counts a
mention of `eval`, arrows included, which covers the reach the model leaves to stage 7; whether Broiler.JS's detector
reads `(eval)(...)` as a direct call is unknown. FF3: Broiler.JS materialises the object on reference and eagerly for a
direct `eval`; Broiler.VM's need is the mention above. RS1 to RS4: both engines are the same, Broiler.VM's slice
refusals being its admission policy, and eval code's free names are inherited, beyond JSD-0080's three forms. The
correspondence files' "FunctionDeclarationInstantiation's step 28" is step 30 in the pinned edition.

**For part F's checklist** (stage 8): "reference resolution with dynamic markers" is `Resolve` and `Resolution`;
"per-function flags" are `EnvironmentOf`, `EnvironmentFacts` and `FunctionFacts.HasSeparateVariableEnvironment`.

### 10.8 Annex B's block-function aliases

*(Added 2026-11-26, at SJL-15.2 stage 5b.)* The var-scoped binding the pinned edition gives a block function, as the
library answers it. Like the rest of this section it restates and decides nothing: the decisions are D-js-annexb-aliases
and D-js-annexb-vectors of 2026-11-26, in `docs/roadmap/SharedLibraries.md`'s section "Shared static semantics
(SJL-15)". No row is added: the Annex B rows (EE0040, EE0062, EE0067, EG0004 to EG0006) are `syntax` rows, reported on
both paths, and with the member off nothing moves.

**The numbering.** The pinned ES2026 numbers the feature Annex B.3.2, "Block-Level Function Declarations Web Legacy
Compatibility Semantics", which has no subclauses: its steps stand inline, each marked normative-optional, at
FunctionDeclarationInstantiation step 32.a, BlockDeclarationInstantiation steps 3.a.ii.1 and 3.b.iii,
GlobalDeclarationInstantiation step 12 and EvalDeclarationInstantiation step 13, with the early-error rules for Block and
for SwitchStatement. Annex B.3.3 is the `if`-clause form, which reads the clause as the sole item of a block.
Correspondence row AB1's name, "Annex B.3.3 block-function aliases", and the bundle rule's "Annex B.3.3 aliases" are an
earlier edition's number for the same feature, and are quoted as written.

**The member and the kind.** `SemanticModel.AnnexBAlias(Declaration)` answers, for a block function the pinned edition
gives a var-scoped binding, the declaration that names the binding its evaluation sets (FunctionDeclarationInstantiation
step 32.a.i.2.c, GlobalDeclarationInstantiation step 12.b.ii.2.a.ii.3, EvalDeclarationInstantiation step 13.b.ii.7.b),
and null for every other declaration. The answer is per declaration, so a caller iterating `Scope.Declarations` needs
no node lookup. It is one of:

| Answer | When | Clause |
|---|---|---|
| An existing `var` or top-level function declaration | The variable scope's first var-scoped declaration of the name | Step 32.a.i.2.a's note, and GlobalDeclarationInstantiation's: the block function creates no binding and writes that one |
| The function's `EnvironmentOf(...).Arguments` | A block function named `arguments` in function code, the VariableEnvironment holding the arguments binding (no separate variable environment) | Step 32.a.i.2.b makes no binding for `arguments`, but step 32.a.i.2.c still writes it |
| A `BindingKind.BlockFunctionAlias` declaration | Otherwise: one per variable scope and name, shared by every admitted block function of that name there, its node the earliest admitted one's name by its start | One binding per name: instantiatedVarNames, and declaredFunctionOrVarNames |

The alias kind belongs to the model alone, in no scope's declarations, as `Arguments` is, so that the tables stay
byte-identical, `DeclarationOf` of its node keeps answering the block function's own declaration, and the bridge's reads
of the tables' scopes are unchanged. Its `IsLexical`, `IsBlockFunction` and `IsIfClauseFunction` are false, and
`IsMutable` true by kind; whether it can be deleted differs by clause and is not a model fact. The variable scope is the
first `Function`, `Script`, `StaticBlock` or `Module` scope on `ParentOf`'s chain from the block function's scope; for
eval code and the dynamic function's goals it is the program's scope, a `Script` scope for eval code and the `Function`
scope keyed by the program for the dynamic goals. Every answer is computed when the model is built, in `Model()`, from the
model's scopes and facts and the walk's record of plain functions; the maps are only looked up.

**The null list**, an inference from the pinned text, since the only early errors a replacing `var` of the name can add
are the lexical-against-var conflicts, and FunctionDeclarationInstantiation tests the parameters itself:

| Withheld where | Clause |
|---|---|
| A generator, an async function or an async generator (`IsBlockFunction` is true for each) | The clauses iterate "each FunctionDeclaration"; BlockDeclarationInstantiation names the other forms apart |
| Annex B is off | `ParseSwitches.AnnexB` |
| Strict code, a Module's, a class's and a static block's included | FunctionDeclarationInstantiation steps 31 and 32, GlobalDeclarationInstantiation step 12.a, EvalDeclarationInstantiation step 13 |
| In function code, a parameter binds the name, an arrow's among them | Step 32.a.i.2 |
| A `Block`, `Switch` or `For` scope between the function's scope and the variable scope declares the name, before or after it in the source, an enclosing block's own block function of the name among them | The replacing `var` would break the Block, CaseBlock and `for` rules |
| A `Catch` scope between them declares the name and its parameter is not a lone BindingIdentifier | The Catch rule, with Annex B.3.4's exemption for a simple parameter, which still shadows the binding inside the clause |
| The variable scope's own lexical declarations hold the name | The FunctionStatementList and ScriptBody rules |

A `with` scope adds no name. GlobalDeclarationInstantiation and EvalDeclarationInstantiation have no parameter test, and
EvalDeclarationInstantiation no `arguments` exclusion.

**The readings**, each the session's, recorded with the alternative not taken:
- Duplicate plain block functions of one name in one block or CaseBlock are all admitted, sharing one alias, the block
  binding's value being the last one's (BlockDeclarationInstantiation step 3.b.iii). Read literally, replacing either
  with a `var` would conflict with the other, so neither would be aliased; but the syntax vector
  `syntax/annex-b/eg0004/0001` expects the Script `{ function f() { return 1; } function f() { return 2; } } f()` to give
  the later function's value for both engines, which the literal reading would turn into a ReferenceError, and both
  engines admit the name.
  test262's `annexB/language/function-code/function-redeclaration-block.js` and `function-redeclaration-switch.js` assert
  acceptance alone.
- A labelled block function is admitted, as the walk declares it in its block and Broiler.VM admits it. Read literally,
  a LabelledStatement does not directly contain a FunctionDeclaration, so it would not be aliased. test262's
  `annexB/language/statements/labeled/function-declaration.js` has labelled functions at the top level only and asserts
  acceptance alone.
- test262's `annexB/language/function-code/block-decl-nested-blocks-with-fun-decl.js` asserts, of nested block functions
  of one name, the outer admitted and the inner withheld, which the null list gives.

**When the binding is set.** Each admitted declaration's evaluation sets the binding to the block binding's value, so
the last one evaluated wins. Before that, an alias exists from instantiation, undefined (step 32.a.i.2.b,
GlobalDeclarationInstantiation's CreateGlobalVarBinding, EvalDeclarationInstantiation step 13.b.ii.7.a); an existing
`var` holds what it holds; an existing top-level function starts as its object; an `Arguments` answer starts as the
arguments object.

**The `arguments` cases.** A block function named `arguments` in function code writes the arguments binding where the
VariableEnvironment holds it: `function f() { { function arguments() {} } return typeof arguments; }` gives
`"function"` (vector `ab1/0005`, probe V028), and the answer is f's `Arguments`. The plan's "null (a block function named
`arguments` ...)" is not taken: the pinned edition's evaluation step still writes the binding, and Broiler.VM's decision
D-ab1 reads it. Where the VariableEnvironment holds no `arguments` binding, an arrow's or a function's with parameter
expressions, the answer is an alias whose binding the evaluation creates (a declarative record's SetMutableBinding
creates a missing binding when S is false; an inference from the text). Before the block runs the name resolves to the
parameters' `Arguments`, or outward for an arrow; after it, to the created binding. So the scope standing for the
variable environment, the `Function` scope where it is separate and otherwise the `Parameters` scope, may gain the name
`arguments` alone at run time: `Resolve` answers `Dynamic` with that scope, reason `DynamicReason.BlockFunction`, and the
fallback the answer without it, as in `Dynamic([Function f], BlockFunction, Static(Parameters f, Arguments))`. The gain
is per name, kept apart from section 10.7's scopes a sloppy direct `eval` may add to, which gain every name; a scope that
may gain both ways has `SloppyDirectEval`. In a Script and in eval code a block function named `arguments` has an alias
of its own. test262 at `ccaac100` asserts an earlier edition's reading in `block-decl-func-skip-arguments.js`, that the
arguments object survives; the model follows the pinned edition there, as both correspondence files and D-ab1 do.

**How `Resolve` answers the names aliases bind.** Stage 5a's lookup keeps its steps; what a scope binds gains the
aliases. A sloppy function's `Function` scope binds the name of each of its aliases, so that a reference only an alias
binds answers `Static` to that scope and the alias, where stage 5a answered as the scopes alone answer it; in
`var g; function f() { { function g() {} } return g; }` the reference now answers f's alias, not the Script's `var g`.
Sloppy eval code's program scope binds its aliases too, and the answer stays `Inherited`, now carrying the alias where
stage 5a carried none. A Script's references stay `Global`: the alias may silently not exist where an earlier script's
lexical declaration or the global object refuses it, where a top-level `var` never does, GlobalDeclarationInstantiation
throwing instead. An existing `var`, top-level function or `Arguments` answer moves nothing, `Resolve` already naming it;
no binding identifier's answer moves; `DeclarationOf` is unchanged. With Annex B off every answer is null and no gain is
added, so stage 5a's own answers stand; the Frontend tests compare the answers with Annex B on and off over the
population.

**What stays conditional.** A Script's alias depends on GlobalDeclarationInstantiation's run-time checks (step
12.b.ii.2.a, HasLexicalDeclaration and CanDeclareGlobalVar): where either fails no binding is made, and a later script's
lexical declaration of the name then throws (test262's `annexB/language/global-code/script-decl-lex-collision.js`).
Eval code's alias is decided when the request carries the caller's records (`InheritedContext.CallerScopes`), a Catch
clause's record exempt: a caller's record that binds the name between the eval and its variable environment, and is not
a Catch clause's, withholds it (step 13.b.ii.4), and an object record never does (section 10.9). Without them the
request does not describe the caller, and the eval body alone decides, as `CallerScopes`' contract says. *(Revised
2026-11-26, at SJL-15.2 stage 5c, when the caller's records joined the request, closing the interim; it read "Eval code's
alias depends on its caller's environments, which the request does not describe: a caller's lexical binding of the name
between the eval and its variable environment, a Catch clause's excepted, withholds it (step 13.b.ii.4). That is the
interim stage 5c's caller scopes narrow, by a dated revision.")* With a global variable environment, for an indirect
eval or a direct eval at a Script's top level, GlobalDeclarationInstantiation-like checks apply (step 13.b.ii.5): a
run-time condition no parse-time input can close, which stays after stage 5c (an inference). *(Revised 2026-11-26, at
SJL-15.2 stage 5c, closing the interim; it read "What remains: the interim, eval code's alias conditional on its
caller's environments until stage 5c describes them; and" before the run-time conditions, which stay as written.)* What
remains: the run-time conditions,
which no stage closes: a Script's alias depends on GlobalDeclarationInstantiation's checks, and eval code with a global
variable environment on GlobalDeclarationInstantiation-like checks. In eval code the binding the
alias writes can be the caller's parameter or `arguments` binding (step 13.b.ii.7.a), which the model does not see. Under
the `FunctionBody` goal the body is answered with the parameters empty, so an alias a real parameter would withhold is
answered as admitted, and `Resolve` then names it where the real function binds the parameter: an extension of section
10.7's limitation of the dynamic goals.

**The vectors left unanswered.** The value vectors of row AB1, group `ab1`, stay not implemented, as groups `pn1`, `ff3`,
`ff4`, `ff5` and `rs2` do: they rest on no `semantics` row, and read at the parse each would match by acceptance alone,
showing nothing of the alias. The Frontend tests cover their sources, and the probes', as a Script and as eval code.

**The dump.** The canonical dump's model section writes, after section 10.7's lines, one `model-annexb` line per block
function in the model's scopes' and declarations' order: the declaration, and its answer's declaration or `-`. An alias,
which no scope holds, is named `alias:` and its node there and in the `model-resolution` lines' declaration and fallback
fields, as an `arguments` binding is named `args:`. A dump without a model is unchanged, and no corpus runner or probe
requests the model before stage 8.

**Where the library differs from each engine**, named for SJL-15.3 and SJL-15.5, as `docs/roadmap/SharedLibraries.md`
lists it. Broiler.VM decides membership by declaration and differs on the inputs D-ab1 names: a block function named
`arguments`, whose arguments object it keeps, its blocking set taking `arguments` for an earlier edition's reason; and,
in eval code, a block function under an intermediate lexical declaration of its name, or in strict eval code, which it
aliases. Broiler.JS decides membership by name and gives the specification's answer on the probes; where its rule may
differ from the pinned text is named there, each an inference not probed. Broiler.VM's decision D-ab1 is Broiler.VM's,
cited and unchanged: the model meets its `arguments` case by the `Arguments` answer, its intermediate-lexical case by the
null list's scopes between, and its strict-eval case by strictness (an inference).

**For part F's checklist** (stage 8): the rule's "Annex B.3.3 aliases" is `AnnexBAlias`, `BindingKind.BlockFunctionAlias`
and `Resolve`.

### 10.9 The caller's records in eval code's inherited context

*(Added 2026-11-26, at SJL-15.2 stage 5c.)* The caller's records a direct `eval` receives, the model members that answer
them, and the rows they decide, as the library answers them. Like the rest of this section it restates and decides
nothing: the decisions are D-js-caller-scopes, D-js-caller-scope-rows and D-js-adapter-caller-scopes of 2026-11-26, in
`docs/roadmap/SharedLibraries.md`'s section "Shared static semantics (SJL-15)".

**The member and its contract.** `InheritedContext.CallerScopes`, init-only and null by default, is a list of the new
`Broiler.ECMAScript.Syntax.CallerScope`, each with `Names` and `IsCatch`, both init-only. A list describes the
declarative Environment Records from a direct `eval`'s caller's running LexicalEnvironment outwards, innermost first, up
to and not including its VariableEnvironment (PerformEval steps 16.a and 16.b). Object records are left out, since
EvalDeclarationInstantiation's step 3.d and step 13.b.ii.4 skip them. Each entry carries every name its record binds, as
HasBinding sees it, not only the names it declares lexically: for a call in a sloppy function's parameters the
parameters and `arguments` (FunctionDeclarationInstantiation steps 20 and 22.d.i), a class's inner name, a `catch`
parameter of either form, a block's functions. A sloppy function's own `var`s and top-level functions are in its
VariableEnvironment and are never listed (step 32.b). `IsCatch` marks "the Environment Record for a |Catch| clause",
whatever the parameter's form (Annex B.3.4 changes both steps with no condition on it). Null means the request does not
describe the caller: the library refuses no `var` for it, decides eval code's aliases from the eval body alone, and
leaves EvalDeclarationInstantiation's step 3 and step 13.b.ii.4 to the engine, as a null `VisiblePrivateNames` leaves
step 7. An empty list is a caller whose LexicalEnvironment is its VariableEnvironment; the request does not tell an
indirect `eval` from a direct one, so an indirect one passes an empty list or null, and both decide the same. Even with
a list these stay the engine's, since they read the realm: step 3.a.i.1 (row EA0006), the TypeErrors of steps 10.a.iv.1
and 12.a.i.1.a, step 13.b.ii.5, and whether a caller's `with` object binds a name. Strict eval code, by its directive or
by `ParseSwitches.ForcedStrict`, reads none of it. The list and each entry's names are copied, so the context stays
immutable. `InheritedContext.TopLevel` carries no list, so that its "that check stays the engine's" stays true of step 3
too. With `ParseRequest.BuildSemanticModel` off nothing reads the member.

**Beyond the contract's words.** JSD-0080's request row names "the caller's lexical names, which Broiler.JS needs for
direct `eval` (decision D41)". `CallerScope` and its per-record shape, every name a record binds with a Catch flag, are
this repository's decision D-js-caller-scopes, beyond those words, as D-js-resolution's inherited form goes beyond
JSD-0080's resolution forms (section 10.7). JSD-0080's words win over this document where they differ (the header's
"What wins"); this section restates the library's own decision and does not reword them. The member is the one
Broiler.VM's decision D16 leaves "until SJL-15's model"; D16, D44 and JSD-0080 are Broiler.VM's and left as written,
and part F reads the member against JSD-0080's input row at stage 8.

**The model members.** `SemanticModel.DirectEvalCalls` lists every direct-`eval` candidate (section 10.7's definition) of
every strictness, in source order. `SemanticModel.CallerScopesOf(CallExpression)` answers the list a direct `eval` at one
of them would receive, as the records are, whatever the call's strictness; it throws `ArgumentNullException` for null and
`ArgumentException` for a call outside the tree or one that is no candidate. Each model scope has a `CallerScope`
instance of its own, shared by the answers that hold it, each name once in declaration order, and every answer is computed in
`Model()` before the alias pass and only looked up. From `ScopeOf(call)` outward by `ParentOf`:

| Scope reached | Entry | Then |
|---|---|---|
| `With` | none, an object record | continue |
| `Block`, `Switch`, `For` | every name it declares, block functions and class declarations among them; an empty one an empty entry | continue |
| `ClassBody` inside the code that holds the call | its inner name, or none | continue |
| `Catch` with a parameter | the parameter's names, `IsCatch` true | continue |
| `Catch` without a parameter | none: it has no environment | continue |
| `Function`, sloppy | its `IsLexical` declarations, the LexicalEnvironment over the VariableEnvironment (step 32.b) | stop |
| `Function`, strict | none (step 31.a) | stop |
| `Parameters`, sloppy, the call in the parameters | every parameter, those after the call included, then `arguments` where `EnvironmentOf(...).Arguments` is non-null, an arrow having none | stop: the variable environment is calleeEnv (steps 19 and 20) |
| `Parameters`, strict | none | stop |
| `StaticBlock`, `Module`, a Script's top level, strict eval code's program | none: strict, or the global record | stop |
| The class around a field initialiser that holds the call | none: the initialiser's lexical environment is its variable environment, and it has no scope | stop |
| Sloppy eval code's program | its `IsLexical` declarations, then the request's own list appended as given; the answer null where the request gives none | end |

So `class C { x = eval("") }` answers an empty list, while in `x = class { [eval("")]() {} }` the inner class is listed
before the stop. A named function expression's `FunctionName` scope lies outside calleeEnv and is never reached, so
`(function a(x = eval("var a")) {})` is accepted. A `switch` discriminant stands outside its `Switch` scope, so
`switch (eval("var x")) { case 0: let x; }` is accepted; a `for` head's initialiser and a for-in or for-of head's
expression stand in the head's `For` scope, and a catch pattern's initialiser in its `Catch` scope (section 10.3). A
Script's top-level call answers an empty list, never the Script's scope, which is the global record of row EA0006's
step, so that `let x; eval('var x;')` is accepted under an empty list.

**EvalDeclarationInstantiation step 3.** For sloppy eval code (step 3), with a global variable environment step 3.a.i.1
throws for a global lexical declaration (EA0006); step 3.b sets thisEnv to lexEnv; step 3.d walks while thisEnv is not
varEnv, the variable environment itself never checked; step 3.d.i skips object records; step 3.d.i.2.a asks
HasBinding of each name of varNames; a host that supports Annex B.3.4 throws at step 3.d.i.2.a.i.1 unless thisEnv is a
Catch clause's record (EA0007), and any other host throws at step 3.d.i.2.a.ii.1 (EA0008). Step 13.b.ii.4, the alias
walk from lexEnv to varEnv, skips object records and exempts a Catch clause's record at step 13.b.ii.4.a.i.1.a.
Both exemptions follow the steps' text, which asks only whether thisEnv is the Environment Record for a Catch clause, so
they hold for every call whose chain meets that record, a call in the catch parameter's own initialisers included:
`try {} catch ({ a = eval('var a') }) {}` is accepted with Annex B on. Annex B.3.4's prose names only "direct eval calls
contained within the Block of a Catch clause"; the library reads the steps (an inference).

**The rows.** EA0007 (`JSE1907`) and EA0008 (`JSE1908`), `eval.var.lexical-collision`, join `SemanticRows`. With the
member on, for sloppy eval code whose request gives the caller's records, the walk reports each var-declared name of the
eval body (its VarDeclaredNames: `var`s and for-var heads at any depth outside functions, and top-level functions,
labelled ones included, but no block function) that a record binds, once, at its first declaration in source order,
with the name as the argument: EA0007 where the record is not a Catch clause's and Annex B is on, EA0008 for every
record with Annex B off, the switch read as the host's support of Annex B.3.4 as the static Catch rule reads it. The row
is a construct's refusal placed at its construct (D-js-channel), since neither engine places it: Broiler.VM throws from
its executor and Broiler.JS at the eval program's start or at the statement. The partial walk reports the rows over the
top-level statements it finished. **EA0006 stays a row only a run reaches**, named as decision rule R's line on such
rows allows: the realm's global lexical declarations are no input of a parse. D-js-semantic-rows' "equal at stage 8
except EA0006" (section 10.4) stands.

**The interims, closed, and what stays.** Stage 5a's and stage 5b's eval-code interims are closed. Eval code's alias is
decided when the request carries the caller's records, a Catch clause's record exempt: a record that is not a Catch
clause's and binds the name withholds it (step 13.b.ii.4), and an object record never does. An inherited answer whose
carried declaration's name a record binds carries no declaration, for a reference, for a binding the language resolves
when it runs (a `var` with an initialiser, a for-in or for-of `var` head) and for a dynamic answer's fallback: a caller's
catch parameter stands before the binding (Annex B.3.4's "PutValue" through it), and a withheld alias leaves the name to
the caller. Every other binding keeps its declaration, since EvalDeclarationInstantiation instantiates it in the
variable environment without passing those records: under a catch parameter `x`, `var x;` and `function x(){}` keep
theirs, and `var x = 2` and the reference `x` carry none. With Annex B on only a Catch clause's record can hold a var
name of an accepted source, a non-Catch record refusing it with EA0007. `Resolve`'s kinds do not change, and eval code's
outside stays `Inherited`: the records stop at the variable environment, leave out `with` objects and are no scopes of
the model. Without the records the request does not describe the caller, and the eval body's answer is the member's
contract, not an interim. The run-time conditions stay, as stage 5b words them: a Script's alias depends on
GlobalDeclarationInstantiation's checks, and eval code with a global variable environment on
GlobalDeclarationInstantiation-like checks; and a caller's `with` object of the name can stand before the binding, a
property that exists only when the code runs.

**The vectors and the nested parse.** The adapter passes every `DirectEval` request the caller's records from
`CallerScopesOf` on the Script's model (D-js-adapter-caller-scopes), so a Script's top-level call passes an empty list,
and the vectors of EA0007 are answered by id: `ea0007/0001` to `ea0007/0005`. `ea0007/0005`'s eval code itself holds a
direct `eval` of a string literal: the adapter follows it, parsing the inner eval code in the context the eval code's
model gives it, the outer request's flags where the eval code itself holds the call, its private names and records
appended, and answers the innermost parse, a refusal at any level being the answer. That applies the owner's answer of
2026-11-25, "Keep the reading at the parse (Recommended)", to a nested parse, and is no revision of it. The vectors of
EA0006 stay not implemented, and no vector exercises EA0008: the generator lists it uncovered, since only a host
without Annex B.3.4 reaches its step.

**The order, against the specification** (an inference). The specification raises ParseText's early errors, then
PerformEval's steps 11.b to 11.h (EA0002 to EA0005), then EvalDeclarationInstantiation's step 3.a (EA0006, at run time),
step 3.d (EA0007, EA0008) innermost record first over varNames in order, step 7 (EA0009), and then the TypeErrors. The
library orders by place (decision D11), so under first error it can name EA0009 at an earlier place before EA0007,
EA0007 before a later-placed ParseText row or a later-placed EA0003, EA0004 or EA0005 (in `var x; new.target` under a
record binding `x` with `InFunction` false, the specification raises the `new.target` row first), or a different name
than the specification would. Under all errors the partial walk adds a row the specification never raises: where
ParseText fails PerformEval throws at step 11.b and EvalDeclarationInstantiation never runs, yet the partial walk reports
EA0007 or EA0008 over its finished statements before the grammar's refusal, as it reports EA0003 to EA0005. Where the
variable environment is global at run time, the specification raises EA0006 on one name before EA0007 on another; only
the code differs. Stage 4's note on EA0009 against EA0006 to EA0008 meets EA0007 here: Broiler.VM checks private names
before the declarations, the reverse of the specification's order, and Broiler.JS raises in the specification's order.

**Where the library differs from each engine**, named for SJL-15.3 and SJL-15.5, as `docs/roadmap/SharedLibraries.md`
lists it. Broiler.JS checks a direct eval's `var` against a flat list of the caller's lexical names, captured where the
call is compiled, a simple catch parameter excepted in sloppy code; it refuses a destructuring catch parameter's name,
which the pinned step does not refuse; its conflict test reads only the eval code's top-level statements (an inference,
not probed); and it misses the nested eval of probe V046. Broiler.VM checks when the eval runs, through its eval map,
skipping `with` rows and continuing through an enclosing evaluation; its map marks a Catch clause's record per record,
whatever the parameter's form; and passing the records would change its eval request's format (an inference). EA0008's
site with Annex B off is a refusal neither engine makes, both engines asking with Annex B on.

**D16 in this document is left as written.** Section 1's row reads "**no caller's lexical names at SJL-13 or SJL-14**: the
conflict they serve is EvalDeclarationInstantiation's, a run-time `SyntaxError` each engine raises today, until SJL-15's
model", and section 9's row reads "No caller's lexical names at SJL-13 or SJL-14": both stay true of those milestones.
Section 9.2's note of 2026-11-01 reads "`InheritedContext` carries the four flags and the optional private names", which
describes SJL-13.2a's types. Each is left as written.

**For part F's checklist** (stage 8): JSD-0080's "the caller's lexical names" is `InheritedContext.CallerScopes`,
`CallerScope`, `SemanticModel.DirectEvalCalls` and `SemanticModel.CallerScopesOf`.

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

*(Added 2026-11-04, by the owner's decision of that day.)* Section 9.2 gains the note on the request's `RetainTokens`,
and section 9.3 the note on the published token stream: the request's `RetainTokens`, the result's `Tokens`, the public
`Token`, `TokenKind`, `TokenFlags` and `Word`, and the canonical dump unchanged by the switch.

*(Added 2026-11-02, at SJL-13.2f, after part A's first codes were triaged.)* Section 9.4 gains the note on the rows of an
`await` binding in a static block (EE0009, EE0011, EE0013 for EE0140) and of an arrow head's literal that covers no
AssignmentPattern (EE0087 or EE0155 for EE0032).

*(Added 2026-11-02, at SJL-13.2f, after review.)* Section 9.4 gains a second note: a class heritage's deferred literal
refusals (EE0032, EE0017) and a deferred literal an assignment pattern takes (EE0038).

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
