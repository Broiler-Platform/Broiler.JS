# Assembly restructure — two back ends, one runtime

Re-lay Broiler.JS as `Broiler.JS.Base` / `.Core` / `.IL` / `.Bytecode` and their satellites,
so that **an application references the IL back end, the bytecode back end, or both** — and
**an application that references only the bytecode back end compiles and publishes as
Native AOT.**

> The plan half of [`Assemblies.status.md`](Assemblies.status.md), which carries the
> measured facts about the graph as it is today. Part of the
> [performance and benchmark roadmap](Roadmap.md), and **the hard precondition for
> [track two](Roadmap.md#track-two--the-vm-tier-phases-69)** — phases 6–9 cannot deliver
> their headline capability without it.

---

## Why this is required, and it is not a tidiness exercise

**Today, referencing the JavaScript runtime forces you to reference the IL emitter.** That
is not a policy that can be relaxed; it is the shape of the dependency graph, and it makes
the bytecode back end's entire purpose unreachable.

`Broiler.JavaScript.ExpressionCompiler` — 11 863 lines, `System.Reflection.Emit` across
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

**An AST assembly depends on an IL emitter.** So does property storage. There is no subset
of the current graph that runs JavaScript without dynamic code, which is why
`Broiler.JavaScript.Portable` had to be written as a **separate 272-line, 20-opcode,
`double`-only island** with zero references — it is the only way anything AOT-safe could
exist at all.

**The good news is that the fusion is superficial.** `ExpressionCompiler` is two things in
one project, and they separate cleanly along directory lines:

| Half | Directories | ~LOC | Uses `Reflection.Emit` |
|---|---|---|---|
| **The expression-tree model** — what `Ast`, `Storage`, `Runtime` and `Parser` actually consume | `Expressions/`, `Converters/`, `SL/`, `ClosureSeparator/`, most of `Core/` | **≈ 5 200** | **no** |
| **The IL emitter** | `Generator/` (33 of 33 files), `LambdaMethodBuilder.cs`, `ExpressionCompiler.cs`, `TypeExtensions.cs`, 3 files in `Core/`, 3 in `Runtime/` | **≈ 4 600** | **yes** |

**Splitting that one project is the change that unlocks the requirement.** Everything else
in this document is consequence and hygiene.

> ### That split now has its own roadmap
>
> **[`AssemblySplit.md`](AssemblySplit.md)** plans it end to end — the decisions, the
> file-by-file assignment, seven steps, the exit gate — and
> [`AssemblySplit.status.md`](AssemblySplit.status.md) carries the symbol analysis behind it.
> **It is separated because it is self-contained**: it needs no decision about the rest of
> this document, changes no behaviour, breaks no consumer, and pays off on its own.
>
> **The analysis says the split is clean.** The emitter is a closed cluster; the model names
> no emitter type; the expression nodes have no `Compile()`; `Ast`, `Storage`, `Parser` and
> `Runtime` name **zero** emitter types between them; and only **3 of 19 root files** carry
> `Reflection.Emit`. **It also found that the back-end contract already exists** —
> `IExpressionCompilationBackend` is public, and both implementations are `internal` — which
> makes item A-3 mostly already done.
>
> **This is symbol analysis, not a build.** `AssemblySplit.md`'s step S-0 is what finds out.

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

## The target

Fifteen assemblies in five tiers. **The dependency direction is strictly downward, and the
only assembly permitted to reference `System.Reflection.Emit` is `Broiler.JS.IL`.**

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

**No assembly exceeds ~36 000 lines**, against 64 432 today, and the largest three are the
ones that genuinely are large bodies of specification.

### The rule that makes the requirement true

> **`Broiler.JS.Core` and everything below it must not reference `Broiler.JS.IL`, and must
> not reference `System.Reflection.Emit`.** Both back ends sit *above* the runtime and
> implement a contract the runtime declares. Neither is referenced by anything below tier 2.

**That contract is the design's one genuinely new piece.** `Broiler.JS.Core` declares
something on the order of "given a parsed program and a realm, produce something callable",
and both back ends implement it. Two consequences that are easy to get wrong:

- **The back end must be *registered*, not *discovered*.** Any resolution by assembly or
  type name defeats trimming and Native AOT — and `Component.md` already tracks *legacy
  magic-name assembly probing* for retirement. **That retirement is now a blocker rather
  than hygiene.**
- **`Broiler.JS.Hosting` must not reference either back end.** The moment bootstrap
  hard-references `Broiler.JS.IL`, every consumer references it too. Composition happens in
  tier 4 or in the application.

## Items

| # | Item | Size | Blocks |
|---|---|---|---|
| **A-0** | **Prove the target graph is achievable** — a spike, not a rewrite | S–M | everything |
| **A-1** | **Split `ExpressionCompiler` into model and emitter** | **L** | A-3, A-6 |
| **A-2** | Split `BuiltIns` into core + Temporal + Intl + RegExp | M | packaging |
| **A-3** | Declare the back-end contract in `Broiler.JS.Core` | M | A-6, phase 6 |
| **A-4** | Retire reflective discovery; register back ends explicitly | M | A-7 |
| **A-5** | Fold `Engine` and `LinqExpressions` into `Broiler.JS.Core` | M–L | A-3 |
| **A-6** | Form `Broiler.JS.IL` and re-point the IL path at the contract | L | A-7 |
| **A-7** | **The AOT gate** — a bytecode-only app that publishes and *runs* | M | the requirement |
| **A-8** | Architecture tests that lock the graph | S | keeping it |
| **A-9** | Rename `Broiler.JavaScript.*` → `Broiler.JS.*` | M–L | — |

### A-0 · Prove the target graph is achievable — **do this first**

**A spike, not a rewrite**, and it exists because the tables above are read off directory
sizes and `using` directives rather than off a compiler.

It must establish, by building rather than by argument:

1. **That the model/emitter split in A-1 is clean.** Move `Generator/` and the five
   Emit-using root files into a new project, and see what breaks. The five `Core/` and
   `Runtime/` files that use `Reflection.Emit` are the ones to look at first — if the model
   genuinely needs them, the split is not along directory lines and A-1 is larger.
2. **What `Runtime`, `Storage`, `Ast` and `Parser` actually consume.** The counted
   `using` directives are `ExpressionCompiler.Core` (21 in `Ast`), `.Expressions` (5),
   `.Runtime` (2) — small numbers, but count *types*, not directives.
3. **Whether `Broiler.JS.Core` can be AOT-clean at all.** Turn on `IsAotCompatible` and the
   trim analyzer over `Runtime` + `Engine` today and record the warning count. **If the
   runtime is deeply reflective for reasons unrelated to the emitter, the requirement is
   harder than this document assumes and the plan must be re-scoped.**

**Record the answers in [`Assemblies.status.md`](Assemblies.status.md) before A-1 starts.**

### A-1 · Split `ExpressionCompiler` into model and emitter — **the unlock**

> **This item has its own roadmap: [`AssemblySplit.md`](AssemblySplit.md).** It is planned in
> full there — two decisions, a file-by-file assignment, steps S-0 … S-7, and an exit gate —
> and **that document, not this one, is what to act from.** What follows is its shape, for
> readers of this plan.

Two projects out of one.

- **`Broiler.JS.Base`** takes `Expressions/`, `Converters/`, `SL/`, `ClosureSeparator/` and
  the non-emitting part of `Core/` — the expression *model*. It keeps
  `ExpressionCompiler`'s existing rule that it stays independent of JavaScript runtime
  types.
- **`Broiler.JS.IL`** takes `Generator/` (33 files), `ExpressionCompiler.cs`,
  `LambdaMethodBuilder.cs`, `Core/ILWriter.cs`, `Core/ILTryBlock.cs`,
  `Core/ILWriterLabel.cs`, and `Runtime/`'s four emitter files.
  **`TypeExtensions.cs` is the only file that has to be cut**, and the cut is one method:
  `CreateMethod(this TypeBuilder, …)` goes to IL, the other ~105 lines stay in Base.

**Two things this moves that are worth knowing about**, because phases 1 and 3 own them:
`LambdaRewriter.cs` (416 lines — item 1-4's quadratic scope fix, and the mechanism item
1-1's remaining half is blocked on) and `DeferredCaptureLayout.cs` (499 lines — item 1-1's
capture layout) are both **Emit-free and go to `Broiler.JS.Base`.** Item 1-1 can be finished
before, during or after this split without interference.

**This is behaviour-preserving by construction** — no code changes, only project membership
and namespaces — which makes it the one item here with a cheap correctness argument: the
full suite and the pinned test262 manifests must be **byte-identical**, not merely green.

### A-2 · Split `BuiltIns`

`Temporal`, `Intl` and `RegExp` become satellite assemblies registered through the existing
`IBuiltInRegistry` / `BuiltInManifest` / `BuiltInFeatureDescriptor` seams — the mechanism is
already there and already used.

**Keep the bootstrap profiles' observable surface identical.** `Full` must still realize
Intl and Temporal lazily and still be conformant; a smaller package is **not** a conformance
win if a required global is absent, and [`Measurement.md`](Measurement.md) says so.

### A-3 · Declare the back-end contract — **smaller than it looks; the seam exists**

`Broiler.JS.Core` declares the interface both back ends implement, and every tier-0/1
assembly is re-pointed at it. **This is where the IL dependency actually leaves the
runtime**, and it is the item to design carefully because it fixes what a back end is
allowed to be.

**It is a generalization, not an invention.** `Runtime/JSCode.cs`'s `JSCode` /
`JSCodeCompiler` / `JSCompilationOptions` and `Runtime/DictionaryCodeCache.cs` already *are*
the runtime's seam to the compiler: the runtime hands a compiler a program and caches what
comes back. **The seam is not missing — it is typed to one back end**, via a
`ExpressionCompilationBackend Backend = DynamicMethod` parameter sitting inside the
code-cache key. Those two files are also the *entire* residue A-1 leaves behind
([`Assemblies.status.md`](Assemblies.status.md)), so A-1 and A-3 meet at exactly one place.

**And more of it exists than that.** `IExpressionCompilationBackend` is **already a public
interface** with both implementations `internal`
([`AssemblySplit.status.md`](AssemblySplit.status.md) §4). *The engine already has a back-end
abstraction; it has one back end's worth of implementations behind it.* **What actually
remains of A-3 is [`AssemblySplit.md`](AssemblySplit.md)'s step S-3** — moving the contract
model-side and replacing the hard-coded factory `switch` with registration. Do that there;
what is left here is re-pointing the rest of tier 0–1 at it.

**Note for the cache key:** `Backend` participates in it today because two IL back ends
(`DynamicMethod`, `CollectibleAssembly`) produce different code. A bytecode back end is a
third value, not a special case — and phase 8's item 8-6 will want to persist that cache,
so keep the key extensible rather than boolean.

**Design it against the bytecode back end, not the IL one.** A contract derived from what
`FastCompiler` happens to do will encode IL assumptions — the same way the tiering
scaffolding encoded them and forced item 4-3 to be re-specified.

### A-4 · Retire reflective discovery

Any `Assembly.Load`/`Type.GetType` by name is an AOT and trim hazard and must go before A-7
can pass. `Component.md` §4 already lists *"remove legacy magic-name assembly probing after
a documented compatibility window"* — **this item is that removal, promoted from hygiene to
blocker.**

### A-5 · Fold `Engine` and `LinqExpressions` into `Broiler.JS.Core`

`Engine` (5 536) is contexts, realms, bootstrap policy and execution services; `Runtime`
(17 920) is the object model. They are one layer in practice — `Engine → Runtime`,
`LinqExpressions → Engine + Runtime`, and the lowering assemblies already "bridge Engine and
Runtime", which `dependencies.md` records as the graph not being a linear stack.

**`LinqExpressions` needs splitting, not moving**: its backend-neutral part goes to `Core`,
its IL-targeting part to `Broiler.JS.IL`. Size that split in A-0.

### A-6 · Form `Broiler.JS.IL`

`Compiler` (18 608 — `FastCompiler` and the numeric-local machinery phases 1 and 3 built)
plus A-1's emitter half plus A-5's IL part, re-pointed at A-3's contract.

**Everything phases 1–5 built lives here**, and none of it changes. The item is a move plus
an interface implementation.

### A-7 · The AOT gate — **the item the requirement reduces to**

A sample application that references **only** `Broiler.JS.Base`, `.Ast`, `.Parser`, `.Core`,
`.BuiltIns`, `.Bytecode`, `.Bytecode.Compiler` and `.Hosting`, and:

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

- **no assembly other than `Broiler.JS.IL` references `System.Reflection.Emit`**;
- `Broiler.JS.Core` and everything below it does not reference either back end;
- `Broiler.JS.Hosting` references neither back end;
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
A-0 spike ← may re-scope everything below   (its sub-question 1 is answered: AssemblySplit.md)
  ├→ A-1 = AssemblySplit.md ────────┬→ A-3 back-end contract → A-5 fold Core → A-6 form IL ─┐
  │                                 └→ A-8 architecture tests (write them here)             │
  ├→ A-2 split BuiltIns                                                                     │
  ├→ A-4 retire reflective discovery ─────────────────────────────────────────────────────┬─┘
  │                                                                                       ▼
  └────────────────────────────────────────────────────────────────────→ A-7 the AOT gate
                                                                                          │
                                                                    A-9 rename (last, or never)
```

## Exit gate

1. **A bytecode-only application publishes with `PublishAot=true` and zero trim/AOT
   warnings, and runs a script** — in CI, on every commit. This is the requirement, stated
   as a build.
2. **An architecture test asserts that only `Broiler.JS.IL` references
   `System.Reflection.Emit`**, and that nothing at tier 0–1 references a back end.
3. **The pinned test262 manifests are unchanged on the IL path**, manifest by manifest.
   A-1, A-5 and A-6 are moves; anything but an identical count means something was not a
   move.
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
- **Risk: the runtime is reflective for reasons unrelated to the emitter.** This is the
  assumption most likely to be wrong, and A-0 exists to find out cheaply. If `Broiler.JS.Core`
  cannot be made AOT-clean, the requirement needs re-scoping and phases 6–9 need re-pricing.
- **Risk: a move that is not a move.** A-1, A-5 and A-6 are large diffs that must not change
  behaviour. Gate them on identical test262 counts rather than on a green suite.
- **Risk: `Broiler.JS.Clr` is inherently reflective** and cannot be in the AOT profile. That
  is correct and should be stated in `public-api.md` rather than worked around.
