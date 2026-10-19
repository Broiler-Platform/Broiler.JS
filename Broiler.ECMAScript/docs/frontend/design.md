# The shared frontend: design

[← Shared libraries](../../README.md) · [Conventions](../conventions.md) ·
[This repository's plan](../../../docs/roadmap/SharedLibraries.md)

**Library:** `Broiler.ECMAScript.Frontend`, which the programme's milestone SJL-13 creates. **Status:** design.
**Filed:** 2026-10-19, at milestone SJL-12 (work package SJL-12.1), before any of the library's code exists.
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
decisions and skip waiting for review.", which changes no rule of the programme.

**Why there is no project yet.** SJL-12 writes no library code (the milestone's "Does not do"), so
`Broiler.ECMAScript/` has no `Broiler.ECMAScript.Frontend/` folder. A project there would bring the library under
[the conventions](../conventions.md)' scope and rule SL1's layering table before it exists (decision D5). SJL-13
creates it. The early-error catalogue of SJL-12.3 sits beside this document (decision D31).

## 1. The contract

From the area design's section 4, with decision D41's specifics.

### Input: an immutable parse request

| Field | Content |
|---|---|
| Source | Text, source name, base offset, base line and column (for sub-parses) |
| Goal | `Script` or `Module`. `Eval`, `FormalParameters` and `FunctionBody` are reserved in both engines (decision D41); the last two let `CreateDynamicFunction` parse its pieces as the specification requires, which neither engine does today |
| Switches | Forced strictness; top-level `await` allowed; Annex B on or off; HTML-like comments on or off (script goal only) |
| Inherited evaluation context | Broiler.VM's evaluation flags (`JsFormat.EvalRequestFlags`): `Strict`, `InFunction`, `InMethod`, `InDerivedConstructor`, `InClassFieldInitializer` and `InClassBody`; the visible private names; the caller's lexical names, which Broiler.JS needs for direct `eval` |
| Proposal flags | Decorators, `accessor`, import phases. Off by default; an engine admits what its own manifest admits |
| Lexer extensions | A narrow parameter for non-standard literal suffixes. Broiler.JS's `m` is the only one, and decision SJL-D-17 keeps its `Decimal` meaning in Broiler.JS |
| Limits | The nesting depth, a bounded parameter; the tree depth, a constant of the library; a diagnostic cap; first-error or all-errors. Broiler.VM's today are `SliceParseOptions.DefaultMaximumNestingDepth` within `MaximumSupportedNestingDepth`, the constant `SliceParseOptions.MaximumTreeDepth`, and the cap its wide parser, its numeric admission and its lowering apply. SJL-13 derives the library's tree depth as Broiler.VM's JSD-0022 derives Broiler.VM's |
| Providers | Unicode lexical classes, from Broiler.Unicode (SJL-7); regular-expression syntax validation through a seam, the engine's own matcher until SJL-11's Broiler.Regex |

### Output

| Part | Content |
|---|---|
| Tree | Immutable nodes with full spans (offset, length, line, column) and **raw facts**: directive raw text, legacy-octal and escape flags, cooked and raw template chunks, regular-expression pattern and flags, BigInt literal text, parenthesisation, trailing commas. Side facts sit in identity-keyed tables. **No desugaring** (section 2) |
| Diagnostics | Section 3 |
| Semantic model | Section 4 |

### Properties

- **Deterministic:** the same request gives the same output, across runs and threads.
- **No ambient state and no mutable statics** (rule SL2, the shape of Broiler.VM's rule N12; Broiler.VM's JSD-0014
  decision 4, a requirement on the shared front end).
- **Limits are checked before the stack is.** No unbounded recursion: explicit stacks, or depth checks with the large
  thread the caller supplies (`CompilationStack` in Broiler.VM).
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

## 6. What SJL-12 prepares in each engine

Neither engine's front end is the library yet. SJL-12 prepares both in place, so that SJL-13 and SJL-14 can move
them onto it:
- **Broiler.JS, SJL-12.5** (decisions D19 to D29): explicit, immutable parse options in place of `CoreScript`'s
  ambient state; one temporary counter per parse; an end-of-file token per scanner; no reference from `Parser` to
  `Runtime`, the Unicode and regular-expression types it used moved to `Broiler.JavaScript.Ast` behind Broiler.Unicode's
  `IUnicodeTables`; the `for`-loop desugaring moved into the compiler's preparation pass; and an architecture test
  with a witness. This repository's plan records each decision.
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

## 8. What this document does not decide

- **The library's names, types and API.** SJL-13 creates the library, under the contract and the conventions.
- **The registry revision**, which is SJL-13's.
- **The `Ast` and `Parser` disposition**, which is SJL-14.4's.
- **The semantic model's implementation**, which is SJL-15's.
- **The nesting bound's value**, which is SJL-13's measurement.

## Corrections

None.
