# Assembly restructure — two back ends, one runtime

Establish a verified, acyclic set of backend-neutral foundations, shared frontend semantics,
and mutually optional IL and bytecode back ends, so that **an application references the IL
back end, the bytecode back end, or both** — and a bytecode-only application can be proved by
publish-and-run evidence under Native AOT.

> The plan half of [`Assemblies.status.md`](Assemblies.status.md), which carries the current
> interpretation and dated evidence about the graph. Part of the
> [performance and benchmark roadmap](Roadmap.md), and **the hard precondition for
> [track two](Roadmap.md#track-two--the-vm-tier-phases-69)** — phases 6–9 cannot deliver
> their headline capability without it.

---

## Why this is required, and it is not a tidiness exercise

**Historical baseline, before A-1 landed:** referencing the JavaScript frontend forced a
reference to the IL emitter. The model/emitter split has since removed that edge from the
portable compiler closure; [`Assemblies.status.md`](Assemblies.status.md) records the current
state. The preserved baseline below explains why the split was required.

In the then-current 2026-08-07 graph, `Broiler.JavaScript.ExpressionCompiler` — 11 863 lines,
`System.Reflection.Emit` across
~40 files — **has no project references at all.** It is at the *bottom* of the graph, and
these depend on it:

```
Broiler.JavaScript.Ast          ──┐
Broiler.JavaScript.Storage      ──┤
Broiler.JavaScript.Runtime      ──┼──▶  Broiler.JavaScript.ExpressionCompiler
Broiler.JavaScript.Parser       ──┤        (System.Reflection.Emit)
Broiler.JavaScript.Engine       ──┤
Broiler.JavaScript.LinqExpressions┘
```

**An AST assembly depends on an IL emitter.** So does property storage. There was no subset
of that 2026-08-07 graph that ran JavaScript without dynamic code, which is why
`Broiler.JavaScript.Portable` had to be written as a **separate 272-line, 20-opcode,
`double`-only island** with zero references — it is the only way anything AOT-safe could
exist at all.

**The good news is that the fusion is superficial.** `ExpressionCompiler` is two things in
one project, and they separate cleanly along directory lines:

| Half | Directories | ~LOC | Uses `Reflection.Emit` |
|---|---|---|---|
| **The expression-tree model** — what `Ast`, `Storage`, `Runtime` and `Parser` actually consume | `Expressions/`, `Converters/`, `SL/`, `ClosureSeparator/`, most of `Core/` | **≈ 5 200** | **no** |
| **The IL emitter** | `Generator/` (33 of 33 files), `LambdaMethodBuilder.cs`, `ExpressionCompiler.cs`, `TypeExtensions.cs`, 3 files in `Core/`, 3 in `Runtime/` | **≈ 4 600** | **yes** |

**Splitting that one project was a necessary unlock, not the complete requirement.** It has
landed structurally through S-6. A verified wider graph, complete dynamic-code census, shared
FrontEnd/Semantics boundary, and publish-and-run gate remain.

> ### That split now has its own roadmap
>
> **[`AssemblySplit.md`](AssemblySplit.md)** plans it end to end — the decisions, the
> file-by-file assignment, seven steps, the exit gate — and
> [`AssemblySplit.status.md`](AssemblySplit.status.md) carries the symbol analysis behind it.
> **It is separated because it is self-contained**: its structural implementation has
> landed, it preserves the historical implementation plan and deviations, and final S-7
> validation remains visible in its status record.
>
> **The analysis says the split is clean.** The emitter is a closed cluster; the model names
> no emitter type; the expression nodes have no `Compile()`; `Ast`, `Storage`, `Parser` and
> `Runtime` name **zero** emitter types between them; and only **3 of 19 root files** carry
> `Reflection.Emit`. **It also found that the back-end contract already exists** —
> `IExpressionCompilationBackend` is public, and both implementations are `internal` — which
> makes item A-3 mostly already done.
>
> The paragraph above is the original symbol analysis. S-0 subsequently built the split and
> found four bounded corrections; read `AssemblySplit.status.md` §0 before relying on counts.

## The second problem: one assembly is 43% of the engine

`Broiler.JavaScript.BuiltIns` is **64 432 lines** — five times the next largest, and larger
than `Base`, `Core`, `Parser` and both back ends put together would be. Its internal
structure already shows the seams:

| Inside BuiltIns | LOC | |
|---|---|---|
| `Temporal/` | 12 597 | a whole date/time library |
| `Intl/` | 8 612 | a whole i18n library |
| `RegExp/` | 7 029 | already has a two-backend seam of its own |
| `Generated/` | 6 897 | source-generator output |
| `Array/` | 6 758 | |
| everything else | ~22 500 | the actual core of ECMAScript |

**Temporal and Intl are 21 209 lines that most hosts do not need**, and
[`Measurement.md`](Measurement.md) already documents that the `Full` bootstrap profile
realizes them *lazily* — the split into satellite assemblies is the packaging half of a
decision the engine has already made at run time.

> **This is a packaging problem, not a duplication problem — with one exception.** The
> reason `BuiltIns` defines `JSArray`, `JSString` and `JSMap` rather than using
> `System.Array`, `System.String` and `Dictionary<K,V>` is analyzed in
> [Why the built-ins are not .NET types](../architecture/builtins-vs-clr-types.md): they
> **wrap** those types rather than duplicating them, and deriving is impossible — `class
> JSArray : System.Array` does not compile (CS0644) and `String` is `sealed`. Splitting the
> assembly reduces what a host must **ship**; it does not and should not reduce what the
> engine **implements**.
>
> **The exception is §6 of that document**: `Temporal` and `Intl` are 21 209 lines that
> re-implement things .NET has, for reasons that are defensible (spec-exactness,
> determinism, and `InvariantGlobalization=true` under Native AOT) — but **nobody has
> measured how much of the 21 209 is spec-driven and how much is re-invention.** A-2 should
> not be presented as answering that question.

## The target — original sketch superseded pending MOD-M2 verification

> **Do not implement the diagram or merge table below.** It is retained as design history,
> but the real project edges invalidate two of its central merges: `Storage → Ast →
> Expressions` makes `Storage + Expressions` cyclic, while `Engine → Parser → Runtime` makes
> `Engine + Runtime` cyclic. Moving all of `Compiler` to IL would also put binding, scope,
> early-error, hoisting, and analysis work needed by both back ends behind the IL boundary.
> Modernization MOD-M2 must replace this sketch with project shells that build.

### Superseded fifteen-assembly sketch

The intended invariant was strictly downward dependencies with only `Broiler.JS.IL`
permitted to reference `System.Reflection.Emit`; the concrete grouping was not validated.

```
                         ┌──────────────────────────────┐
  tier 4  composition    │ Broiler.JS.All   (IL + BC)   │
                         │ Broiler.JS.Aot   (BC only)   │  ← the AOT profile
                         └──────────────┬───────────────┘
                                        │
  tier 3  hosts          Clr · Modules · ModuleExtensions · Debugger · Network · NodePolyfill
                                        │
  tier 2  back ends      ┌──────────────┴───────────────┐
    (mutually optional)  │ Broiler.JS.IL │ Broiler.JS.Bytecode(+.Compiler) │
                         └──────────────┬───────────────┘
                                        │  both implement Core's backend contract
  tier 1  semantics      Broiler.JS.Core  ·  Broiler.JS.BuiltIns(+.Intl .Temporal .RegExp)
                                        │
  tier 0  foundation     Broiler.JS.Base  ·  Broiler.JS.Ast  ·  Broiler.JS.Parser
```

| Target assembly | From | ~LOC | AOT |
|---|---|---:|:---:|
| **`Broiler.JS.Base`** | `Storage` + `ExpressionCompiler`'s **model** half | ≈ 7 900 | ✅ |
| **`Broiler.JS.Ast`** | `Ast` | ≈ 2 900 | ✅ |
| **`Broiler.JS.Parser`** | `Parser` | ≈ 9 900 | ✅ |
| **`Broiler.JS.Core`** | `Runtime` + `Engine` + `LinqExpressions`' backend-neutral part | ≈ 25 000 | ✅ |
| **`Broiler.JS.BuiltIns`** | `BuiltIns` minus the three satellites | ≈ 36 000 | ✅ |
| **`Broiler.JS.BuiltIns.Temporal`** | `BuiltIns/Temporal` | ≈ 12 600 | ✅ |
| **`Broiler.JS.BuiltIns.Intl`** | `BuiltIns/Intl` | ≈ 8 600 | ✅ |
| **`Broiler.JS.BuiltIns.RegExp`** | `BuiltIns/RegExp` | ≈ 7 000 | ✅ |
| **`Broiler.JS.IL`** | `ExpressionCompiler`'s **emitter** half + `Compiler` + `LinqExpressions`' IL part | ≈ 25 000 | ❌ **by design** |
| **`Broiler.JS.Bytecode`** | `Portable`, grown by [phase 6](Phase-6.md) | 272 → ? | ✅ |
| **`Broiler.JS.Bytecode.Compiler`** | `Portable.Compiler`, grown by [phase 6](Phase-6.md) | 391 → ? | ✅ |
| **`Broiler.JS.Hosting`** | `Broiler.JavaScript` + `Globals` + `Extensions` | ≈ 6 500 | ✅ |
| **`Broiler.JS.Clr` / `.Modules` / `.ModuleExtensions` / `.Debugger` / `.Network` / `.NodePolyfill`** | unchanged in role | ≈ 5 000 | mixed — `Clr` is inherently reflective |
| **`Broiler.JS.All`** | meta-package: IL + Bytecode + every satellite | — | ❌ |
| **`Broiler.JS.Aot`** | **new** meta-package: Bytecode only, no reflective satellite | — | ✅ |

The size estimates are historical inputs, not an approved decomposition or baseline.

### Canonical boundary hypotheses for MOD-M2

The verified graph may choose different names, but it must test these ownership rules:

- keep `Expressions`, `Storage`, `Ast`, `Runtime`, and `Engine` separate unless project
  shells prove a consolidation acyclic;
- extract a backend-neutral **FrontEnd/Semantics** boundary for parsing, binding, scope,
  early errors, hoisting, numeric-local analysis, and any shared IR;
- put only IL-specific lowering, emission, `AssemblyCodeCache`, and `ILPack` in the IL
  profile;
- make bytecode compilation depend on FrontEnd/Semantics and never on IL; and
- choose a back end only through explicit composition/registration, never name-based load.

`Broiler.JavaScript.Expressions` is the one structural boundary already established. Every
other row in the superseded table is a hypothesis until MOD-M2's source-edge census, project
shells, builds, and architecture tests agree.

### The rule that makes the requirement true

> **The verified shared FrontEnd/Semantics and runtime foundations must reference neither
> concrete back end nor `System.Reflection.Emit`.** Both back ends sit above those shared
> layers and implement a contract owned at the boundary MOD-M2 proves acyclic.

That contract is a generalization of the existing compilation seam: given backend-neutral
program/semantic artifacts and explicit execution inputs, produce an installable callable
artifact. Two consequences are easy to get wrong:

- **The back end must be *registered*, not *discovered*.** Any resolution by assembly or
  type name defeats trimming and Native AOT — and `Component.md` already tracks *legacy
  magic-name assembly probing* for retirement. **That retirement is now a blocker rather
  than hygiene.**
- **Backend-neutral hosting must not reference either back end.** The moment bootstrap
  hard-references `Broiler.JS.IL`, every consumer references it too. Composition happens in
  tier 4 or in the application.

## Items

| # | Item | Size | Blocks |
|---|---|---|---|
| **A-0** | **Replace the superseded target with MOD-M2's verified project graph** | M | everything after the landed split |
| **A-1** | **Split `ExpressionCompiler` into model and emitter** — structurally landed; S-7 validation open | **L** | A-3, revised A-6 |
| **A-2** | Split `BuiltIns` into core + Temporal + Intl + RegExp | M | packaging |
| **A-3** | Generalize the existing back-end contract at the verified Runtime/FrontEnd boundary | M | revised A-6, phase 6 |
| **A-4** | Complete a whole-tree reflective/dynamic-code census; retire name-based discovery | M | A-7 |
| **A-5** | **Superseded:** do not fold `Engine` into `Runtime/Core`; extract FrontEnd/Semantics and size `LinqExpressions` by real edges | M–L | revised A-0 |
| **A-6** | Form the IL profile from emitter and IL-only lowering after shared semantics are extracted | L | A-7 |
| **A-7** | **The AOT gate** — a bytecode-only app that publishes and *runs* | M | the requirement |
| **A-8** | Architecture tests that lock the graph | S | keeping it |
| **A-9** | Rename `Broiler.JavaScript.*` → `Broiler.JS.*` | M–L | — |

### A-0 · Replace the superseded target with a verified graph — **do this first**

**A project-shell spike, not a rewrite.** The first A-0 answered whether the model/emitter
split could build and collected a limited analyzer census. It did not validate the wider
merge table, and the real project edges now show that table would create cycles. MOD-M2 is the
replacement A-0; its output, not the historical diagram, authorizes later moves.

It must establish, by building rather than by argument:

1. **The complete source and project edge graph**, including return edges through `Ast`,
   `Parser`, runtime callbacks, generated code, module initializers, and host composition.
2. **A shared FrontEnd/Semantics shell** containing every backend-neutral parser, binder,
   scope, early-error, hoisting, analysis, and IR service required by IL and bytecode.
3. **Separate IL and bytecode shells** that both compile against shared semantics while the
   bytecode shell has no IL or `System.Reflection.Emit` closure.
4. **A profile-classified dynamic-code census**, including `Assembly.Load`, `Type.GetType`,
   `AssemblyCodeCache`, `ILPack`, generated sources, and satellite/host profiles—not only
   analyzer warnings in the portable compiler closure.
5. **Architecture-testable rules and package roots** for full IL, bytecode-only AOT, and
   mixed composition. If a shell needs a cycle, redesign the ownership boundary rather than
   adding a reciprocal reference.

**Record the verified graph in [`Assemblies.status.md`](Assemblies.status.md) before A-2,
A-5, or A-6 moves resume.** A-1's final S-7 validation can close independently.

### A-1 · Split `ExpressionCompiler` into model and emitter — **implemented, validation open**

> **This item has its own roadmap: [`AssemblySplit.md`](AssemblySplit.md).** Its structural
> steps S-0 … S-6 landed with deviations recorded in `AssemblySplit.status.md`; S-7 remains
> the acceptance step. What follows preserves the intended shape and is not a request to
> repeat the move.

The landed structure is two projects out of one; the final `Broiler.JS.*` names remain
provisional.

- **`Broiler.JavaScript.Expressions`** owns `Expressions/`, `Converters/`, `SL/`,
  `ClosureSeparator/` and the non-emitting utilities — the expression *model*. It keeps
  `ExpressionCompiler`'s existing rule that it stays independent of JavaScript runtime
  types.
- **`Broiler.JavaScript.ExpressionCompiler`** retains the emitter. The build required four
  file cuts and additional visibility/registration changes; the exact deviations and
  current file counts are authoritative in `AssemblySplit.status.md` §0.

**Two things this moved are worth knowing about**, because phases 1 and 3 own them:
`LambdaRewriter.cs` (416 lines — item 1-4's quadratic scope fix, and the mechanism item
1-1's remaining half is blocked on) and `DeferredCaptureLayout.cs` (499 lines — item 1-1's
capture layout) are both **Emit-free and moved to `Broiler.JavaScript.Expressions`.** Item 1-1 can be finished
before, during or after this split without interference.

**The intended change is behaviour-preserving**, but the build required more than project
membership changes. That is why S-7 remains an explicit acceptance gate: the full suite and
the pinned test262 manifests must be identical, not merely green.

### A-2 · Split `BuiltIns`

`Temporal`, `Intl` and `RegExp` become satellite assemblies registered through the existing
`IBuiltInRegistry` / `BuiltInManifest` / `BuiltInFeatureDescriptor` seams — the mechanism is
already there and already used.

**Keep the bootstrap profiles' observable surface identical.** `Full` must still realize
Intl and Temporal lazily and still be conformant; a smaller package is **not** a conformance
win if a required global is absent, and [`Measurement.md`](Measurement.md) says so.

### A-3 · Generalize the back-end contract at the verified boundary

The verified Runtime or FrontEnd/Semantics owner declares the interface both back ends
implement; do not select `Broiler.JS.Core` by assumption. **This seam fixes what a back end
is allowed to be**, so its inputs must express backend-neutral program/semantic artifacts,
not IL expression trees or emitter policy.

**It is a generalization, not an invention.** `Runtime/JSCode.cs`'s `JSCode` /
`JSCodeCompiler` / `JSCompilationOptions` and `Runtime/DictionaryCodeCache.cs` already *are*
the runtime's seam to the compiler: the runtime hands a compiler a program and caches what
comes back. **The seam is not missing — it is typed to one back end**, via a
`ExpressionCompilationBackend Backend = DynamicMethod` parameter sitting inside the
code-cache key. Those two files are also the *entire* residue A-1 leaves behind
([`Assemblies.status.md`](Assemblies.status.md)), so A-1 and A-3 meet at exactly one place.

**And more of it exists than that.** `IExpressionCompilationBackend` is a public interface
with IL implementations behind it, and S-3's explicit registry has landed
([`AssemblySplit.status.md`](AssemblySplit.status.md) §0). That seam proves registration can
be non-reflective; it does **not** by itself define the backend-neutral program and result
contract a full bytecode engine needs. A-3 now owns that generalization at MOD-M2's verified
FrontEnd/Runtime boundary and the re-pointing of shared layers.

**Note for the cache key:** `Backend` participates in it today because two IL back ends
(`DynamicMethod`, `CollectibleAssembly`) produce different code. A bytecode back end is a
third value, not a special case — and phase 8's item 8-6 will want to persist that cache,
so keep the key extensible rather than boolean.

**Design it against the bytecode back end, not the IL one.** A contract derived from what
`FastCompiler` happens to do will encode IL assumptions — the same way the tiering
scaffolding encoded them and forced item 4-3 to be re-specified.

### A-4 · Census dynamic code and retire reflective discovery

Any `Assembly.Load`/`Type.GetType` by name is an AOT and trim hazard and must go before A-7
can pass. The census is whole-tree and profile-classified: it includes generated source,
shells, satellites, `AssemblyCodeCache`, and `ILPack`, rather than treating two known
Engine probes or analyzer warnings as exhaustive. `Component.md` §4 already lists *"remove
legacy magic-name assembly probing after a documented compatibility window"* — **this item
is that removal, promoted from hygiene to blocker.**

### A-5 · Superseded Core fold; extract shared FrontEnd/Semantics

**Do not fold `Engine` into `Runtime/Core` as originally proposed.** `Engine → Parser →
Runtime` creates a return edge if `Engine` and `Runtime` become one project. Keep them
separate unless the MOD-M2 shells prove another ownership cut acyclic.

Split `LinqExpressions` and `Compiler` by semantic ownership, not directory or current
backend: binding, scope, early errors, hoisting, numeric-local analysis, and shared IR belong
in FrontEnd/Semantics; only IL-specific lowering/emission belongs in the IL profile. The
bytecode compiler consumes the same semantic artifacts rather than referencing IL or
duplicating frontend behavior.

### A-6 · Form `Broiler.JS.IL`

After A-0/A-5 establish shared semantics, form the IL profile from A-1's emitter,
IL-specific lowering, `AssemblyCodeCache`, `ILPack`, and only the compiler code proved to be
backend-specific. Re-point it at A-3's contract.

This is not a wholesale move of `Compiler`: phases 1–5 also built semantic analysis that the
bytecode path must share. Project shells and dual-backend tests determine the split.

### A-7 · The AOT gate — **the item the requirement reduces to**

A sample application that references **only the project set approved by MOD-M2 for the
bytecode/AOT profile**—never the IL profile—and:

```xml
<PublishAot>true</PublishAot>
<IsDynamicCodeSupported>false</IsDynamicCodeSupported>
<TrimmerSingleWarn>false</TrimmerSingleWarn>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
```

**It must publish with zero trim/AOT warnings and then *run a real script*** — not a numeric
expression. `samples/Broiler.JavaScript.NativeAotSample` already sets `PublishAot=true` and
is the place to grow this.

**Wire it into CI as a build gate.** An AOT guarantee that is not checked by a build is a
comment.

> **What this gate can prove before phase 6 exists.** With today's 20-opcode `Bytecode`, the
> "real script" is a numeric one and the gate proves only that *the graph* is AOT-clean.
> **That is worth having on its own** — it is the half that phases 6–9 cannot deliver and
> this restructure can — and it turns phase 6 from "build a VM and hope the packaging works"
> into "grow an assembly that already publishes."

### A-8 · Architecture tests that lock the graph

Assertions, in the existing architecture-test project:

- **no assembly outside the approved IL profile references `System.Reflection.Emit`**;
- the verified FrontEnd/Semantics and runtime foundations reference neither back end;
- backend-neutral hosting references neither concrete back end;
- no assembly exceeds an agreed line or type budget (the check that keeps A-2 from undoing
  itself).

**Write these with A-1, not after A-9.** The graph is only worth restoring once.

### A-9 · Rename `Broiler.JavaScript.*` → `Broiler.JS.*`

**A breaking change to every assembly name, namespace, and package id**, affecting ~30
projects, the aggregate repository's bridge, `public-api.md`, and every consumer.

**It is separable from everything above and should be done last or not at all.** The
capability the requirement asks for comes from A-1 … A-7; the names are ergonomics. Two
options worth pricing in A-0:

- **assembly and package ids only**, keeping `Broiler.JavaScript.*` namespaces — cheap, and
  source-compatible for consumers;
- **assembly, package and namespace** — the full rename, and a documented breaking change
  under `Component.md` §5's release rules.

## Order

```
A-1 structural split landed ──→ S-7 final validation

A-0 / MOD-M2 verified graph
  ├→ A-5 FrontEnd/Semantics extraction ─→ A-3 backend contract ─→ A-6 IL profile ─┐
  ├→ A-4 whole-tree dynamic-code census and discovery retirement ────────────────┤
  ├→ A-2 evidence-led BuiltIns packaging ────────────────────────────────────────┤
  └→ A-8 architecture tests ─────────────────────────────────────────────────────┤
                                                                                 ▼
                                                                       A-7 AOT gate
                                                                                 │
                                                                  A-9 rename (last, or never)
```

## Exit gate

1. **A bytecode-only application publishes with `PublishAot=true` and zero trim/AOT
   warnings, and runs a script** — in CI, on every commit. This is the requirement, stated
   as a build.
2. **Architecture tests assert that only the approved IL profile references
   `System.Reflection.Emit`**, that shared FrontEnd/Semantics and runtime foundations
   reference neither back end, and that the bytecode profile has no IL closure.
3. **The pinned test262 manifests are unchanged on the IL path**, manifest by manifest.
   Structural moves preserve the count; semantic extraction additionally passes the
   dual-backend and conformance gates because it is not assumed to be a pure move.
4. **The bootstrap profiles' observable surface is unchanged.** `Full` still realizes Intl
   and Temporal; `Minimal` is still documented as deliberately non-conformant.
5. **Pristine-consumer tests for every package intended for external use**, per
   `Component.md` §5 — and one of them must be the AOT profile.
6. **No performance claim.** This is a packaging change; if it moves a number, that number
   is measured under [`Measurement.md`](Measurement.md) like any other.

## Dependencies and risks

- **Blocks [phase 6](Phase-6.md)'s headline capability.** A VM in an assembly that transitively
  references `Reflection.Emit` cannot be published AOT, so phase 6 would deliver an
  interpreter and not the thing the interpreter is *for*. **Do A-0 and A-1 before phase 6's
  item 6-2.**
- **Serves track one too.** `Component.md` §4 and §5 owe trimmed-support scoping, linker
  warning resolution, the magic-name probing retirement, and a decision on whether feature
  satellites improve startup and working set. **A-2 and A-4 are those items**, and A-7
  answers the satellite question with a build rather than an opinion.
- **Risk: the runtime is reflective for reasons unrelated to the emitter.** The initial
  analyzer census found bounded warning sites but was not a whole-program publish. If the
  verified shared/runtime profile cannot be made AOT-clean, the requirement needs re-scoping
  and phases 6–9 need re-pricing.
- **Risk: treating semantic extraction as a move.** A-1 was mostly structural, but the
  FrontEnd/Semantics split and backend contract can change analysis behavior. Gate them on
  identical IL results plus dual-backend/conformance evidence, not merely a green suite.
- **Risk: `Broiler.JS.Clr` is inherently reflective** and cannot be in the AOT profile. That
  is correct and should be stated in `public-api.md` rather than worked around.
